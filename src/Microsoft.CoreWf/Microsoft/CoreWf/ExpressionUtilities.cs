// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CoreWf.Internals;

namespace Microsoft.CoreWf
{
    internal static class ExpressionUtilities
    {
        public static ParameterExpression RuntimeContextParameter = Expression.Parameter(typeof(ActivityContext), "context");
        private static Assembly s_linqAssembly = typeof(Func<>).GetTypeInfo().Assembly;
        private static MethodInfo s_createLocationFactoryGenericMethod = typeof(ExpressionUtilities).GetMethod("CreateLocationFactory");
        private static MethodInfo s_propertyDescriptorGetValue; // PropertyDescriptor.GetValue


        // Types cached for use in TryRewriteLambdaExpression
        private static Type s_inArgumentGenericType = typeof(InArgument<>);
        private static Type s_outArgumentGenericType = typeof(OutArgument<>);
        private static Type s_inOutArgumentGenericType = typeof(InOutArgument<>);
        private static Type s_variableGenericType = typeof(Variable<>);
        private static Type s_delegateInArgumentGenericType = typeof(DelegateInArgument<>);
        private static Type s_delegateOutArgumentGenericType = typeof(DelegateOutArgument<>);
        private static Type s_activityContextType = typeof(ActivityContext);
        private static Type s_locationReferenceType = typeof(LocationReference);
        private static Type s_runtimeArgumentType = typeof(RuntimeArgument);
        private static Type s_argumentType = typeof(Argument);
        private static Type s_variableType = typeof(Variable);
        private static Type s_delegateArgumentType = typeof(DelegateArgument);

        // MethodInfos cached for use in TryRewriteLambdaExpression
        private static MethodInfo s_activityContextGetValueGenericMethod = typeof(ActivityContext).GetMethod("GetValue", new Type[] { typeof(LocationReference) });
        private static MethodInfo s_activityContextGetLocationGenericMethod = typeof(ActivityContext).GetMethod("GetLocation", new Type[] { typeof(LocationReference) });
        private static MethodInfo s_locationReferenceGetLocationMethod = typeof(LocationReference).GetMethod("GetLocation", new Type[] { typeof(ActivityContext) });
        private static MethodInfo s_argumentGetLocationMethod = typeof(Argument).GetMethod("GetLocation", new Type[] { typeof(ActivityContext) });
        private static MethodInfo s_variableGetMethod = typeof(Variable).GetMethod("Get", new Type[] { typeof(ActivityContext) });
        private static MethodInfo s_delegateArgumentGetMethod = typeof(DelegateArgument).GetMethod("Get", new Type[] { typeof(ActivityContext) });

        //static MethodInfo PropertyDescriptorGetValue
        //{
        //    get
        //    {
        //        if (propertyDescriptorGetValue == null)
        //        {
        //            propertyDescriptorGetValue = typeof(PropertyDescriptor).GetMethod("GetValue");
        //        }

        //        return propertyDescriptorGetValue;
        //    }
        //}

        public static Expression CreateIdentifierExpression(LocationReference locationReference)
        {
            return Expression.Call(RuntimeContextParameter, s_activityContextGetValueGenericMethod.MakeGenericMethod(locationReference.Type), Expression.Constant(locationReference, typeof(LocationReference)));
        }

        // If we ever expand the depth to which we'll look through an expression for a location,
        // then we also need to update the depth to which isLocationExpression is propagated in
        // ExpressionUtilities.TryRewriteLambdaExpression and VisualBasicHelper.Rewrite.
        public static bool IsLocation(LambdaExpression expression, Type targetType, out string extraErrorMessage)
        {
            extraErrorMessage = null;
            Expression body = expression.Body;

            if (targetType != null && body.Type != targetType)
            {
                // eg) LambdaReference<IComparable>((env) => strVar.Get(env))
                // you can have an expressionTree whose LambdaExpression.ReturnType == IComparable,
                // while its LambdaExpression.Body.Type == String
                // and not ever have Convert node in the tree.
                extraErrorMessage = SR.MustMatchReferenceExpressionReturnType;
                return false;
            }

            switch (body.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    return true;

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)body;
                    FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
                    if (fieldInfo != null)
                    {
                        return !fieldInfo.IsInitOnly;
                    }

                    PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        return propertyInfo.CanWrite;
                    }

                    break;
                //// This also handles variables, which are emitted as "context.GetLocation<T>("v").Value"
                //MemberExpression memberExpression = (MemberExpression)body;
                //MemberTypes memberType = memberExpression.Member.MemberType;
                //if (memberType == MemberTypes.Field)
                //{
                //    FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                //    if (fieldInfo.IsInitOnly)
                //    {
                //        // readOnly field
                //        return false;
                //    }
                //    return true;
                //}
                //else if (memberType == MemberTypes.Property)
                //{
                //    PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
                //    if (!propertyInfo.CanWrite)
                //    {
                //        // no Setter
                //        return false;
                //    }
                //    return true;
                //}
                //break;

                case ExpressionType.Call:
                    // Depends on the method being called.
                    //     System.Array.Get --> multi-dimensional array
                    //     get_Item --> might be an indexer property if it's special name & default etc.

                    MethodCallExpression callExpression = (MethodCallExpression)body;
                    MethodInfo method = callExpression.Method;

                    Type declaringType = method.DeclaringType;
                    if (declaringType.GetTypeInfo().BaseType == TypeHelper.ArrayType && method.Name == "Get")
                    {
                        return true;
                    }
                    else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    else if (method.Name == "GetValue" && declaringType == s_activityContextType)
                    {
                        return true;
                    }
                    else if (method.Name == "Get" && declaringType.GetTypeInfo().IsGenericType)
                    {
                        Type declaringTypeGenericDefinition = declaringType.GetGenericTypeDefinition();

                        if (declaringTypeGenericDefinition == s_inOutArgumentGenericType ||
                            declaringTypeGenericDefinition == s_outArgumentGenericType)
                        {
                            return true;
                        }
                    }
                    break;

                case ExpressionType.Convert:
                    // a would-be-valid Location expression that is type converted is treated invalid
                    extraErrorMessage = SR.MustMatchReferenceExpressionReturnType;
                    return false;
            }
            return false;
        }

        public static LocationFactory<T> CreateLocationFactory<T>(LambdaExpression expression)
        {
            Expression body = expression.Body;

            switch (body.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    return new ArrayLocationFactory<T>(expression);

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)body;
                    FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
                    if (fieldInfo != null)
                    {
                        return new FieldLocationFactory<T>(expression);
                    }

                    PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        return new PropertyLocationFactory<T>(expression);
                    }

                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NotSupportedException("Lvalues must be fields or properties"));
                //// This also handles variables, which are emitted as "context.GetLocation<T>("v").Value"
                //MemberTypes memberType = ((MemberExpression)body).Member.MemberType;
                //if (memberType == MemberTypes.Field)
                //{
                //    return new FieldLocationFactory<T>(expression);
                //}
                //else if (memberType == MemberTypes.Property)
                //{
                //    return new PropertyLocationFactory<T>(expression);
                //}
                //else
                //{
                //    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NotSupportedException("Lvalues of member type " + memberType));
                //}

                case ExpressionType.Call:
                    // Depends on the method being called.
                    //     System.Array.Get --> multi-dimensional array
                    //     get_Item --> might be an indexer property if it's special name & default etc.

                    MethodCallExpression callExpression = (MethodCallExpression)body;
                    MethodInfo method = callExpression.Method;

                    Type declaringType = method.DeclaringType;
                    if (declaringType.GetTypeInfo().BaseType == TypeHelper.ArrayType && method.Name == "Get")
                    {
                        return new MultidimensionalArrayLocationFactory<T>(expression);
                    }
                    else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal))
                    {
                        return new IndexerLocationFactory<T>(expression);
                    }
                    else if (method.Name == "GetValue" && declaringType == s_activityContextType)
                    {
                        return new LocationReferenceFactory<T>(callExpression.Arguments[0], expression.Parameters);
                    }
                    else if (method.Name == "Get" && declaringType.GetTypeInfo().IsGenericType)
                    {
                        Type declaringTypeGenericDefinition = declaringType.GetGenericTypeDefinition();

                        if (declaringTypeGenericDefinition == s_inOutArgumentGenericType ||
                            declaringTypeGenericDefinition == s_outArgumentGenericType)
                        {
                            return new ArgumentFactory<T>(callExpression.Object, expression.Parameters);
                        }
                    }

                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExpressionForLocation(body.NodeType)));

                default:
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExpressionForLocation(body.NodeType)));
            }
        }

        internal static bool TryGetInlinedReference(CodeActivityPublicEnvironmentAccessor publicAccessor, LocationReference originalReference,
            bool isLocationExpression, out LocationReference inlinedReference)
        {
            if (isLocationExpression)
            {
                return publicAccessor.TryGetReferenceToPublicLocation(originalReference, true, out inlinedReference);
            }
            else
            {
                return publicAccessor.TryGetAccessToPublicLocation(originalReference, ArgumentDirection.In, true, out inlinedReference);
            }
        }

        private static LocationFactory CreateParentReference(Expression expression, ReadOnlyCollection<ParameterExpression> lambdaParameters)
        {
            // create a LambdaExpression to get access to the expression
            int parameterCount = lambdaParameters.Count;
            Type genericFuncType = s_linqAssembly.GetType("System.Func`" + (parameterCount + 1), true, false);
            Type[] delegateParameterTypes = new Type[parameterCount + 1];

            for (int i = 0; i < parameterCount; ++i)
            {
                delegateParameterTypes[i] = lambdaParameters[i].Type;
            }
            delegateParameterTypes[parameterCount] = expression.Type;
            Type funcType = genericFuncType.MakeGenericType(delegateParameterTypes);
            LambdaExpression parentLambda = Expression.Lambda(funcType, expression, lambdaParameters);

            // call CreateLocationFactory<parentLambda.Type>(parentLambda);
            MethodInfo typedMethod = s_createLocationFactoryGenericMethod.MakeGenericMethod(expression.Type);
            return (LocationFactory)typedMethod.Invoke(null, new object[] { parentLambda });
        }

        private static Func<ActivityContext, T> Compile<T>(Expression objectExpression, ReadOnlyCollection<ParameterExpression> parametersCollection)
        {
            ParameterExpression[] parameters = null;
            if (parametersCollection != null)
            {
                parameters = parametersCollection.ToArray<ParameterExpression>();
            }

            Expression<Func<ActivityContext, T>> objectLambda = Expression.Lambda<Func<ActivityContext, T>>(objectExpression, parameters);
            return objectLambda.Compile();
        }

        private static T Evaluate<T>(Expression objectExpression, ReadOnlyCollection<ParameterExpression> parametersCollection, ActivityContext context)
        {
            Func<ActivityContext, T> objectFunc = Compile<T>(objectExpression, parametersCollection);
            return objectFunc(context);
        }

        // for single-dimensional arrays 
        private class ArrayLocationFactory<T> : LocationFactory<T>
        {
            private Func<ActivityContext, T[]> _arrayFunction;
            private Func<ActivityContext, int> _indexFunction;

            public ArrayLocationFactory(LambdaExpression expression)
            {
                Fx.Assert(expression.Body.NodeType == ExpressionType.ArrayIndex, "ArrayIndex expression required");
                BinaryExpression arrayIndexExpression = (BinaryExpression)expression.Body;

                _arrayFunction = ExpressionUtilities.Compile<T[]>(arrayIndexExpression.Left, expression.Parameters);
                _indexFunction = ExpressionUtilities.Compile<int>(arrayIndexExpression.Right, expression.Parameters);
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                return new ArrayLocation(_arrayFunction(context), _indexFunction(context));
            }

            [DataContract]
            internal class ArrayLocation : Location<T>
            {
                private T[] _array;

                private int _index;

                public ArrayLocation(T[] array, int index)
                    : base()
                {
                    _array = array;
                    _index = index;
                }

                public override T Value
                {
                    get
                    {
                        return _array[_index];
                    }
                    set
                    {
                        _array[_index] = value;
                    }
                }

                [DataMember(Name = "array")]
                internal T[] SerializedArray
                {
                    get { return _array; }
                    set { _array = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "index")]
                internal int SerializedIndex
                {
                    get { return _index; }
                    set { _index = value; }
                }
            }
        }

        private class FieldLocationFactory<T> : LocationFactory<T>
        {
            private FieldInfo _fieldInfo;
            private Func<ActivityContext, object> _ownerFunction;
            private LocationFactory _parentFactory;

            public FieldLocationFactory(LambdaExpression expression)
            {
                Fx.Assert(expression.Body.NodeType == ExpressionType.MemberAccess, "field expression required");
                MemberExpression memberExpression = (MemberExpression)expression.Body;

                //Fx.Assert(memberExpression.Member.MemberType == MemberTypes.Field, "member field expected");
                _fieldInfo = (FieldInfo)memberExpression.Member;

                if (_fieldInfo.IsStatic)
                {
                    _ownerFunction = null;
                }
                else
                {
                    _ownerFunction = ExpressionUtilities.Compile<object>(
                    Expression.Convert(memberExpression.Expression, TypeHelper.ObjectType), expression.Parameters);
                }

                if (_fieldInfo.DeclaringType.GetTypeInfo().IsValueType)
                {
                    // may want to set a struct, so we need to make an expression in order to set the parent
                    _parentFactory = CreateParentReference(memberExpression.Expression, expression.Parameters);
                }
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                object owner = null;
                if (_ownerFunction != null)
                {
                    owner = _ownerFunction(context);
                }

                Location parent = null;
                if (_parentFactory != null)
                {
                    parent = _parentFactory.CreateLocation(context);
                }
                return new FieldLocation(_fieldInfo, owner, parent);
            }

            [DataContract]
            internal class FieldLocation : Location<T>
            {
                private FieldInfo _fieldInfo;

                private object _owner;

                private Location _parent;

                public FieldLocation(FieldInfo fieldInfo, object owner, Location parent)
                    : base()
                {
                    _fieldInfo = fieldInfo;
                    _owner = owner;
                    _parent = parent;
                }

                //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotRaiseReservedExceptionTypes,
                //Justification = "Need to raise NullReferenceException to match expected failure case in workflows.")]
                public override T Value
                {
                    get
                    {
                        if (_owner == null && !_fieldInfo.IsStatic)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_fieldInfo.Name)));
                        }

                        return (T)_fieldInfo.GetValue(_owner);
                    }
                    set
                    {
                        if (_owner == null && !_fieldInfo.IsStatic)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_fieldInfo.Name)));
                        }

                        _fieldInfo.SetValue(_owner, value);
                        if (_parent != null)
                        {
                            // Looks like we are trying to set a field on a struct
                            // Calling SetValue simply sets the field on the local copy of the struct, which is not very helpful
                            // Since we have a copy, assign it back to the parent
                            _parent.Value = _owner;
                        }
                    }
                }

                [DataMember(Name = "fieldInfo")]
                internal FieldInfo SerializedFieldInfo
                {
                    get { return _fieldInfo; }
                    set { _fieldInfo = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "owner")]
                internal object SerializedOwner
                {
                    get { return _owner; }
                    set { _owner = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "parent")]
                internal Location SerializedParent
                {
                    get { return _parent; }
                    set { _parent = value; }
                }
            }
        }

        private class ArgumentFactory<T> : LocationFactory<T>
        {
            private Func<ActivityContext, Argument> _argumentFunction;

            public ArgumentFactory(Expression argumentExpression, ReadOnlyCollection<ParameterExpression> expressionParameters)
            {
                _argumentFunction = ExpressionUtilities.Compile<Argument>(argumentExpression, expressionParameters);
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                Argument argument = _argumentFunction(context);

                return argument.RuntimeArgument.GetLocation(context) as Location<T>;
            }
        }

        private class LocationReferenceFactory<T> : LocationFactory<T>
        {
            private Func<ActivityContext, LocationReference> _locationReferenceFunction;

            public LocationReferenceFactory(Expression locationReferenceExpression, ReadOnlyCollection<ParameterExpression> expressionParameters)
            {
                _locationReferenceFunction = ExpressionUtilities.Compile<LocationReference>(locationReferenceExpression, expressionParameters);
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                LocationReference locationReference = _locationReferenceFunction(context);
                return locationReference.GetLocation(context) as Location<T>;
            }
        }

        private class IndexerLocationFactory<T> : LocationFactory<T>
        {
            private MethodInfo _getItemMethod;
            private string _indexerName;
            private MethodInfo _setItemMethod;
            private Func<ActivityContext, object>[] _setItemArgumentFunctions;
            private Func<ActivityContext, object> _targetObjectFunction;

            public IndexerLocationFactory(LambdaExpression expression)
            {
                Fx.Assert(expression.Body.NodeType == ExpressionType.Call, "Call expression required.");

                MethodCallExpression callExpression = (MethodCallExpression)expression.Body;
                _getItemMethod = callExpression.Method;

                Fx.Assert(_getItemMethod.IsSpecialName && _getItemMethod.Name.StartsWith("get_", StringComparison.Ordinal), "Special get_Item method required.");

                //  Get the set_Item accessor for the same set of parameter/return types if any.
                _indexerName = _getItemMethod.Name.Substring(4);
                string setItemName = "set_" + _indexerName;
                ParameterInfo[] getItemParameters = _getItemMethod.GetParameters();
                Type[] setItemParameterTypes = new Type[getItemParameters.Length + 1];

                for (int i = 0; i < getItemParameters.Length; i++)
                {
                    setItemParameterTypes[i] = getItemParameters[i].ParameterType;
                }
                setItemParameterTypes[getItemParameters.Length] = _getItemMethod.ReturnType;

                _setItemMethod = _getItemMethod.DeclaringType.GetMethod(
                    setItemName, BindingFlags.Public | BindingFlags.Instance, setItemParameterTypes);

                if (_setItemMethod != null)
                {
                    //  Get the target object and all the setter's arguments 
                    //  (minus the actual value to be set).
                    _targetObjectFunction = ExpressionUtilities.Compile<object>(callExpression.Object, expression.Parameters);

                    _setItemArgumentFunctions = new Func<ActivityContext, object>[callExpression.Arguments.Count];
                    for (int i = 0; i < callExpression.Arguments.Count; i++)
                    {
                        // convert value types to objects since Linq doesn't do it automatically
                        Expression argument = callExpression.Arguments[i];
                        if (argument.Type.GetTypeInfo().IsValueType)
                        {
                            argument = Expression.Convert(argument, TypeHelper.ObjectType);
                        }
                        _setItemArgumentFunctions[i] = ExpressionUtilities.Compile<object>(argument, expression.Parameters);
                    }
                }
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                object targetObject = null;
                object[] setItemArguments = null;

                if (_setItemMethod != null)
                {
                    targetObject = _targetObjectFunction(context);

                    setItemArguments = new object[_setItemArgumentFunctions.Length];

                    for (int i = 0; i < _setItemArgumentFunctions.Length; i++)
                    {
                        setItemArguments[i] = _setItemArgumentFunctions[i](context);
                    }
                }

                return new IndexerLocation(_indexerName, _getItemMethod, _setItemMethod, targetObject, setItemArguments);
            }

            [DataContract]
            internal class IndexerLocation : Location<T>
            {
                private string _indexerName;

                private MethodInfo _getItemMethod;

                private MethodInfo _setItemMethod;

                private object _targetObject;

                private object[] _setItemArguments;

                public IndexerLocation(string indexerName, MethodInfo getItemMethod, MethodInfo setItemMethod,
                    object targetObject, object[] getItemArguments)
                    : base()
                {
                    _indexerName = indexerName;
                    _getItemMethod = getItemMethod;
                    _setItemMethod = setItemMethod;
                    _targetObject = targetObject;
                    _setItemArguments = getItemArguments;
                }

                //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotRaiseReservedExceptionTypes,
                //Justification = "Need to raise NullReferenceException to match expected failure case in workflows.")]
                public override T Value
                {
                    get
                    {
                        if (_targetObject == null && !_getItemMethod.IsStatic)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_getItemMethod.Name)));
                        }

                        return (T)_getItemMethod.Invoke(_targetObject, _setItemArguments);
                    }

                    set
                    {
                        if (_setItemMethod == null)
                        {
                            string targetObjectTypeName = _targetObject.GetType().Name;
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(
                                SR.MissingSetAccessorForIndexer(_indexerName, targetObjectTypeName)));
                        }

                        if (_targetObject == null && !_setItemMethod.IsStatic)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_setItemMethod.Name)));
                        }

                        object[] localSetItemArguments = new object[_setItemArguments.Length + 1];
                        Array.ConstrainedCopy(_setItemArguments, 0, localSetItemArguments, 0, _setItemArguments.Length);
                        localSetItemArguments[localSetItemArguments.Length - 1] = value;

                        _setItemMethod.Invoke(_targetObject, localSetItemArguments);
                    }
                }

                [DataMember(Name = "indexerName")]
                internal string SerializedIndexerName
                {
                    get { return _indexerName; }
                    set { _indexerName = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "getItemMethod")]
                internal MethodInfo SerializedGetItemMethod
                {
                    get { return _getItemMethod; }
                    set { _getItemMethod = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "setItemMethod")]
                internal MethodInfo SerializedSetItemMethod
                {
                    get { return _setItemMethod; }
                    set { _setItemMethod = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "targetObject")]
                internal object SerializedTargetObject
                {
                    get { return _targetObject; }
                    set { _targetObject = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "setItemArguments")]
                internal object[] SerializedSetItemArguments
                {
                    get { return _setItemArguments; }
                    set { _setItemArguments = value; }
                }
            }
        }

        private class MultidimensionalArrayLocationFactory<T> : LocationFactory<T>
        {
            private Func<ActivityContext, Array> _arrayFunction;
            private Func<ActivityContext, int>[] _indexFunctions;

            public MultidimensionalArrayLocationFactory(LambdaExpression expression)
            {
                Fx.Assert(expression.Body.NodeType == ExpressionType.Call, "Call expression required.");
                MethodCallExpression callExpression = (MethodCallExpression)expression.Body;

                _arrayFunction = ExpressionUtilities.Compile<Array>(
                    callExpression.Object, expression.Parameters);

                _indexFunctions = new Func<ActivityContext, int>[callExpression.Arguments.Count];
                for (int i = 0; i < _indexFunctions.Length; i++)
                {
                    _indexFunctions[i] = ExpressionUtilities.Compile<int>(
                        callExpression.Arguments[i], expression.Parameters);
                }
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                int[] indices = new int[_indexFunctions.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = _indexFunctions[i](context);
                }
                return new MultidimensionalArrayLocation(_arrayFunction(context), indices);
            }

            [DataContract]
            internal class MultidimensionalArrayLocation : Location<T>
            {
                private Array _array;

                private int[] _indices;

                public MultidimensionalArrayLocation(Array array, int[] indices)
                    : base()
                {
                    _array = array;
                    _indices = indices;
                }

                public override T Value
                {
                    get
                    {
                        return (T)_array.GetValue(_indices);
                    }

                    set
                    {
                        _array.SetValue(value, _indices);
                    }
                }

                [DataMember(Name = "array")]
                internal Array SerializedArray
                {
                    get { return _array; }
                    set { _array = value; }
                }

                [DataMember(Name = "indices")]
                internal int[] SerializedIndicess
                {
                    get { return _indices; }
                    set { _indices = value; }
                }
            }
        }

        private class PropertyLocationFactory<T> : LocationFactory<T>
        {
            private Func<ActivityContext, object> _ownerFunction;
            private PropertyInfo _propertyInfo;
            private LocationFactory _parentFactory;

            public PropertyLocationFactory(LambdaExpression expression)
            {
                Fx.Assert(expression.Body.NodeType == ExpressionType.MemberAccess, "member access expression required");
                MemberExpression memberExpression = (MemberExpression)expression.Body;

                //Fx.Assert(memberExpression.Member.MemberType == MemberTypes.Property, "property access expression expected");
                _propertyInfo = (PropertyInfo)memberExpression.Member;

                if (memberExpression.Expression == null)
                {
                    // static property
                    _ownerFunction = null;
                }
                else
                {
                    _ownerFunction = ExpressionUtilities.Compile<object>(
                        Expression.Convert(memberExpression.Expression, TypeHelper.ObjectType), expression.Parameters);
                }

                if (_propertyInfo.DeclaringType.GetTypeInfo().IsValueType)
                {
                    // may want to set a struct, so we need to make an expression in order to set the parent
                    _parentFactory = CreateParentReference(memberExpression.Expression, expression.Parameters);
                }
            }

            public override Location<T> CreateLocation(ActivityContext context)
            {
                object owner = null;
                if (_ownerFunction != null)
                {
                    owner = _ownerFunction(context);
                }

                Location parent = null;
                if (_parentFactory != null)
                {
                    parent = _parentFactory.CreateLocation(context);
                }
                return new PropertyLocation(_propertyInfo, owner, parent);
            }

            [DataContract]
            internal class PropertyLocation : Location<T>
            {
                private object _owner;

                private PropertyInfo _propertyInfo;

                private Location _parent;

                public PropertyLocation(PropertyInfo propertyInfo, object owner, Location parent)
                    : base()
                {
                    _propertyInfo = propertyInfo;
                    _owner = owner;
                    _parent = parent;
                }

                //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotRaiseReservedExceptionTypes,
                //Justification = "Need to raise NullReferenceException to match expected failure case in workflows.")]
                public override T Value
                {
                    get
                    {
                        // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                        // from the other's perspective, not internal properties, so they're okay as a special case.
                        // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                        // at an implementation level it is.
                        MethodInfo getMethodInfo = _propertyInfo.GetGetMethod();
                        if (getMethodInfo == null && !TypeHelper.AreTypesCompatible(_propertyInfo.DeclaringType, typeof(Location)))
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteonlyPropertyCannotBeRead(_propertyInfo.DeclaringType, _propertyInfo.Name)));
                        }

                        if (_owner == null && (getMethodInfo == null || !getMethodInfo.IsStatic))
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_propertyInfo.Name)));
                        }

                        // Okay, it's public
                        return (T)_propertyInfo.GetValue(_owner, null);
                    }

                    set
                    {
                        // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                        // from the other's perspective, not internal properties, so they're okay as a special case.
                        // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                        // at an implementation level it is.
                        MethodInfo setMethodInfo = _propertyInfo.GetSetMethod();
                        if (setMethodInfo == null && !TypeHelper.AreTypesCompatible(_propertyInfo.DeclaringType, typeof(Location)))
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ReadonlyPropertyCannotBeSet(_propertyInfo.DeclaringType, _propertyInfo.Name)));
                        }

                        if (_owner == null && (setMethodInfo == null || !setMethodInfo.IsStatic))
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(_propertyInfo.Name)));
                        }

                        // Okay, it's public
                        _propertyInfo.SetValue(_owner, value, null);
                        if (_parent != null)
                        {
                            // Looks like we are trying to set a property on a struct
                            // Calling SetValue simply sets the property on the local copy of the struct, which is not very helpful
                            // Since we have a copy, assign it back to the parent
                            _parent.Value = _owner;
                        }
                    }
                }

                [DataMember(EmitDefaultValue = false, Name = "owner")]
                internal object SerializedOwner
                {
                    get { return _owner; }
                    set { _owner = value; }
                }

                [DataMember(Name = "propertyInfo")]
                internal PropertyInfo SerializedPropertyInfo
                {
                    get { return _propertyInfo; }
                    set { _propertyInfo = value; }
                }

                [DataMember(EmitDefaultValue = false, Name = "parent")]
                internal Location SerializedParent
                {
                    get { return _parent; }
                    set { _parent = value; }
                }
            }
        }

        // Returns true if it changed the expression (newExpression != expression).
        // If it returns false then newExpression is set equal to expression.
        // This method uses the publicAccessor parameter to generate violations (workflow
        // artifacts which are not visible) and to generate inline references
        // (references at a higher scope which can be resolved at runtime).
        public static bool TryRewriteLambdaExpression(Expression expression, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression = false)
        {
            newExpression = expression;

            if (expression == null)
            {
                return false;
            }

            // Share some local declarations across the switch
            Expression left = null;
            Expression right = null;
            Expression other = null;
            bool hasChanged = false;
            IList<Expression> expressionList = null;
            IList<ElementInit> initializerList = null;
            IList<MemberBinding> bindingList = null;
            MethodCallExpression methodCall = null;
            BinaryExpression binaryExpression = null;
            NewArrayExpression newArray = null;
            UnaryExpression unaryExpression = null;

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    binaryExpression = (BinaryExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(binaryExpression.Left, out left, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpression(binaryExpression.Right, out right, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpression(binaryExpression.Conversion, out other, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.MakeBinary(
                            binaryExpression.NodeType,
                            left,
                            right,
                            binaryExpression.IsLiftedToNull,
                            binaryExpression.Method,
                            (LambdaExpression)other);
                    }
                    break;

                case ExpressionType.Conditional:
                    ConditionalExpression conditional = (ConditionalExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(conditional.Test, out other, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpression(conditional.IfTrue, out left, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpression(conditional.IfFalse, out right, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.Condition(
                            other,
                            left,
                            right);
                    }
                    break;

                case ExpressionType.Constant:
                    break;

                case ExpressionType.Invoke:
                    InvocationExpression invocation = (InvocationExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(invocation.Expression, out other, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpressionCollection(invocation.Arguments, out expressionList, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.Invoke(
                            other,
                            expressionList);
                    }
                    break;

                case ExpressionType.Lambda:
                    LambdaExpression lambda = (LambdaExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(lambda.Body, out other, publicAccessor, isLocationExpression);

                    if (hasChanged)
                    {
                        newExpression = Expression.Lambda(
                            lambda.Type,
                            other,
                            lambda.Parameters);
                    }
                    break;

                case ExpressionType.ListInit:
                    ListInitExpression listInit = (ListInitExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(listInit.NewExpression, out other, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpressionInitializersCollection(listInit.Initializers, out initializerList, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.ListInit(
                            (NewExpression)other,
                            initializerList);
                    }
                    break;

                case ExpressionType.Parameter:
                    break;

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)expression;

                    // When creating a location for a member on a struct, we also need a location
                    // for the struct (so we don't just set the member on a copy of the struct)
                    bool subTreeIsLocationExpression = isLocationExpression && memberExpression.Member.DeclaringType.GetTypeInfo().IsValueType;

                    hasChanged |= TryRewriteLambdaExpression(memberExpression.Expression, out other, publicAccessor, subTreeIsLocationExpression);

                    if (hasChanged)
                    {
                        newExpression = Expression.MakeMemberAccess(
                            other,
                            memberExpression.Member);
                    }
                    break;

                case ExpressionType.MemberInit:
                    MemberInitExpression memberInit = (MemberInitExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(memberInit.NewExpression, out other, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpressionBindingsCollection(memberInit.Bindings, out bindingList, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.MemberInit(
                            (NewExpression)other,
                            bindingList);
                    }
                    break;

                case ExpressionType.ArrayIndex:
                    // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                    methodCall = expression as MethodCallExpression;
                    if (methodCall != null)
                    {
                        hasChanged |= TryRewriteLambdaExpression(methodCall.Object, out other, publicAccessor);
                        hasChanged |= TryRewriteLambdaExpressionCollection(methodCall.Arguments, out expressionList, publicAccessor);

                        if (hasChanged)
                        {
                            newExpression = Expression.ArrayIndex(
                                other,
                                expressionList);
                        }
                    }
                    else
                    {
                        binaryExpression = (BinaryExpression)expression;

                        hasChanged |= TryRewriteLambdaExpression(binaryExpression.Left, out left, publicAccessor);
                        hasChanged |= TryRewriteLambdaExpression(binaryExpression.Right, out right, publicAccessor);

                        if (hasChanged)
                        {
                            newExpression = Expression.ArrayIndex(
                                left,
                                right);
                        }
                    }
                    break;

                case ExpressionType.Call:
                    methodCall = (MethodCallExpression)expression;

                    // TryRewriteMethodCall does all the real work
                    hasChanged = TryRewriteMethodCall(methodCall, out newExpression, publicAccessor, isLocationExpression);
                    break;

                case ExpressionType.NewArrayInit:
                    newArray = (NewArrayExpression)expression;

                    hasChanged |= TryRewriteLambdaExpressionCollection(newArray.Expressions, out expressionList, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.NewArrayInit(
                            newArray.Type.GetElementType(),
                            expressionList);
                    }
                    break;

                case ExpressionType.NewArrayBounds:
                    newArray = (NewArrayExpression)expression;

                    hasChanged |= TryRewriteLambdaExpressionCollection(newArray.Expressions, out expressionList, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.NewArrayBounds(
                            newArray.Type.GetElementType(),
                            expressionList);
                    }
                    break;

                case ExpressionType.New:
                    NewExpression objectCreationExpression = (NewExpression)expression;

                    if (objectCreationExpression.Constructor == null)
                    {
                        // must be creating a valuetype
                        Fx.Assert(objectCreationExpression.Arguments.Count == 0, "NewExpression with null Constructor but some arguments");
                    }
                    else
                    {
                        hasChanged |= TryRewriteLambdaExpressionCollection(objectCreationExpression.Arguments, out expressionList, publicAccessor);

                        if (hasChanged)
                        {
                            newExpression = objectCreationExpression.Update(expressionList);
                        }
                    }
                    break;

                case ExpressionType.TypeIs:
                    TypeBinaryExpression typeBinary = (TypeBinaryExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(typeBinary.Expression, out other, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.TypeIs(
                            other,
                            typeBinary.TypeOperand);
                    }
                    break;

                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    unaryExpression = (UnaryExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(unaryExpression.Operand, out left, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.MakeUnary(
                            unaryExpression.NodeType,
                            left,
                            unaryExpression.Type,
                            unaryExpression.Method);
                    }
                    break;

                case ExpressionType.UnaryPlus:
                    unaryExpression = (UnaryExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(unaryExpression.Operand, out left, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.UnaryPlus(
                            left,
                            unaryExpression.Method);
                    }
                    break;

                // Expression Tree V2.0 types. This is due to the hosted VB compiler generating ET V2.0 nodes.

                case ExpressionType.Block:
                    BlockExpression block = (BlockExpression)expression;

                    hasChanged |= TryRewriteLambdaExpressionCollection(block.Expressions, out expressionList, publicAccessor);

                    if (hasChanged)
                    {
                        // Parameter collections are never rewritten
                        newExpression = Expression.Block(block.Variables, expressionList);
                    }
                    break;

                case ExpressionType.Assign:
                    binaryExpression = (BinaryExpression)expression;

                    hasChanged |= TryRewriteLambdaExpression(binaryExpression.Left, out left, publicAccessor);
                    hasChanged |= TryRewriteLambdaExpression(binaryExpression.Right, out right, publicAccessor);

                    if (hasChanged)
                    {
                        newExpression = Expression.Assign(left, right);
                    }
                    break;
            }

            return hasChanged;
        }

        private static bool TryRewriteLambdaExpressionBindingsCollection(IList<MemberBinding> bindings, out IList<MemberBinding> newBindings, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            IList<MemberBinding> temporaryBindings = null;

            for (int i = 0; i < bindings.Count; i++)
            {
                MemberBinding binding = bindings[i];

                MemberBinding newBinding;
                if (TryRewriteMemberBinding(binding, out newBinding, publicAccessor))
                {
                    if (temporaryBindings == null)
                    {
                        // We initialize this list with the unchanged bindings
                        temporaryBindings = new List<MemberBinding>(bindings.Count);

                        for (int j = 0; j < i; j++)
                        {
                            temporaryBindings.Add(bindings[j]);
                        }
                    }
                }

                // At this point newBinding is either the updated binding (if
                // rewrite returned true) or the original binding (if false
                // was returned)
                if (temporaryBindings != null)
                {
                    temporaryBindings.Add(newBinding);
                }
            }

            if (temporaryBindings != null)
            {
                newBindings = temporaryBindings;
                return true;
            }
            else
            {
                newBindings = bindings;
                return false;
            }
        }

        private static bool TryRewriteMemberBinding(MemberBinding binding, out MemberBinding newBinding, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            newBinding = binding;

            bool hasChanged = false;
            Expression other = null;
            IList<ElementInit> initializerList = null;
            IList<MemberBinding> bindingList = null;

            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    MemberAssignment assignment = (MemberAssignment)binding;

                    hasChanged |= TryRewriteLambdaExpression(assignment.Expression, out other, publicAccessor);

                    if (hasChanged)
                    {
                        newBinding = Expression.Bind(assignment.Member, other);
                    }
                    break;

                case MemberBindingType.ListBinding:
                    MemberListBinding list = (MemberListBinding)binding;

                    hasChanged |= TryRewriteLambdaExpressionInitializersCollection(list.Initializers, out initializerList, publicAccessor);

                    if (hasChanged)
                    {
                        newBinding = Expression.ListBind(list.Member, initializerList);
                    }
                    break;

                case MemberBindingType.MemberBinding:
                    MemberMemberBinding member = (MemberMemberBinding)binding;

                    hasChanged |= TryRewriteLambdaExpressionBindingsCollection(member.Bindings, out bindingList, publicAccessor);

                    if (hasChanged)
                    {
                        newBinding = Expression.MemberBind(member.Member, bindingList);
                    }
                    break;
            }

            return hasChanged;
        }


        private static bool TryRewriteLambdaExpressionCollection(IList<Expression> expressions, out IList<Expression> newExpressions, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            IList<Expression> temporaryExpressions = null;

            for (int i = 0; i < expressions.Count; i++)
            {
                Expression expression = expressions[i];

                Expression newExpression;
                if (TryRewriteLambdaExpression(expression, out newExpression, publicAccessor))
                {
                    if (temporaryExpressions == null)
                    {
                        // We initialize the list by copying all of the unchanged
                        // expressions over
                        temporaryExpressions = new List<Expression>(expressions.Count);

                        for (int j = 0; j < i; j++)
                        {
                            temporaryExpressions.Add(expressions[j]);
                        }
                    }
                }

                // newExpression will either be set to the new expression (true was
                // returned) or the original expression (false was returned)
                if (temporaryExpressions != null)
                {
                    temporaryExpressions.Add(newExpression);
                }
            }

            if (temporaryExpressions != null)
            {
                newExpressions = temporaryExpressions;
                return true;
            }
            else
            {
                newExpressions = expressions;
                return false;
            }
        }

        private static bool TryRewriteLambdaExpressionInitializersCollection(IList<ElementInit> initializers, out IList<ElementInit> newInitializers, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            IList<ElementInit> temporaryInitializers = null;

            for (int i = 0; i < initializers.Count; i++)
            {
                ElementInit elementInit = initializers[i];

                IList<Expression> newExpressions;
                if (TryRewriteLambdaExpressionCollection(elementInit.Arguments, out newExpressions, publicAccessor))
                {
                    if (temporaryInitializers == null)
                    {
                        // We initialize the list by copying all of the unchanged
                        // initializers over
                        temporaryInitializers = new List<ElementInit>(initializers.Count);

                        for (int j = 0; j < i; j++)
                        {
                            temporaryInitializers.Add(initializers[j]);
                        }
                    }

                    elementInit = Expression.ElementInit(elementInit.AddMethod, newExpressions);
                }

                if (temporaryInitializers != null)
                {
                    temporaryInitializers.Add(elementInit);
                }
            }

            if (temporaryInitializers != null)
            {
                newInitializers = temporaryInitializers;
                return true;
            }
            else
            {
                newInitializers = initializers;
                return false;
            }
        }

        private static bool TryGetInlinedArgumentReference(MethodCallExpression originalExpression, Expression argumentExpression, out LocationReference inlinedReference, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            inlinedReference = null;

            Argument argument = null;
            object tempArgument;

            if (CustomMemberResolver(argumentExpression, out tempArgument) && tempArgument is Argument)
            {
                argument = (Argument)tempArgument;
            }
            else
            {
                try
                {
                    Expression<Func<Argument>> argumentLambda = Expression.Lambda<Func<Argument>>(argumentExpression);
                    Func<Argument> argumentFunc = argumentLambda.Compile();
                    argument = argumentFunc();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(argumentExpression.Type, originalExpression, e));
                    return false;
                }
            }

            if (argument == null)
            {
                if (argumentExpression.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression memberExpression = (MemberExpression)argumentExpression;
                    PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        RuntimeArgument runtimeArgument = ActivityUtilities.FindArgument(memberExpression.Member.Name, publicAccessor.ActivityMetadata.CurrentActivity);

                        if (runtimeArgument != null && TryGetInlinedReference(publicAccessor, runtimeArgument, isLocationExpression, out inlinedReference))
                        {
                            return true;
                        }
                    }
                    //MemberExpression memberExpression = (MemberExpression)argumentExpression;
                    //if (memberExpression.Member.MemberType == MemberTypes.Property)
                    //{
                    //    RuntimeArgument runtimeArgument = ActivityUtilities.FindArgument(memberExpression.Member.Name, publicAccessor.ActivityMetadata.CurrentActivity);

                    //    if (runtimeArgument != null && TryGetInlinedReference(publicAccessor, runtimeArgument, isLocationExpression, out inlinedReference))
                    //    {
                    //        return true;
                    //    }
                    //}
                }

                publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(argumentExpression.Type, originalExpression, SR.SubexpressionResultWasNull(argumentExpression.Type)));
                return false;
            }
            else
            {
                if (argument.RuntimeArgument == null || !TryGetInlinedReference(publicAccessor, argument.RuntimeArgument, isLocationExpression, out inlinedReference))
                {
                    publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(argumentExpression.Type, originalExpression, SR.SubexpressionResultWasNotVisible(argumentExpression.Type)));
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private static bool TryRewriteArgumentGetCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            // We verify that this is a method we are expecting (single parameter
            // of type ActivityContext).  If not, we won't rewrite it at all
            // and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression contextExpression = argumentExpressions[0];

                if (contextExpression.Type == s_activityContextType)
                {
                    LocationReference inlinedReference;
                    if (TryGetInlinedArgumentReference(originalExpression, originalExpression.Object, out inlinedReference, publicAccessor, isLocationExpression))
                    {
                        newExpression = Expression.Call(contextExpression, s_activityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                        return true;
                    }
                }
            }

            newExpression = originalExpression;
            return false;
        }

        private static bool TryRewriteArgumentGetLocationCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            // We verify that this is a method we are expecting (single parameter
            // of type ActivityContext).  If not, we won't rewrite it at all
            // and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression contextExpression = argumentExpressions[0];

                if (contextExpression.Type == s_activityContextType)
                {
                    LocationReference inlinedReference;
                    if (TryGetInlinedArgumentReference(originalExpression, originalExpression.Object, out inlinedReference, publicAccessor, true))
                    {
                        if (returnType == null)
                        {
                            newExpression = Expression.Call(Expression.Constant(inlinedReference, typeof(LocationReference)), s_locationReferenceGetLocationMethod, contextExpression);
                        }
                        else
                        {
                            newExpression = Expression.Call(contextExpression, s_activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                        }

                        return true;
                    }
                }
            }

            newExpression = originalExpression;
            return false;
        }

        private static bool TryRewriteLocationReferenceSubclassGetCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            // We verify that this is a method we are expecting (single parameter
            // of type ActivityContext).  If not, we won't rewrite it at all
            // and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression contextExpression = argumentExpressions[0];

                if (contextExpression.Type == s_activityContextType)
                {
                    LocationReference inlinedReference;
                    if (TryGetInlinedLocationReference(originalExpression, originalExpression.Object, out inlinedReference, publicAccessor, isLocationExpression))
                    {
                        newExpression = Expression.Call(contextExpression, s_activityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                        return true;
                    }
                }
            }

            newExpression = originalExpression;
            return false;
        }

        private static bool TryRewriteLocationReferenceSubclassGetLocationCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            // We verify that this is a method we are expecting (single parameter
            // of type ActivityContext).  If not, we won't rewrite it at all
            // and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression contextExpression = argumentExpressions[0];

                if (contextExpression.Type == s_activityContextType)
                {
                    LocationReference inlinedReference;
                    if (TryGetInlinedLocationReference(originalExpression, originalExpression.Object, out inlinedReference, publicAccessor, true))
                    {
                        if (returnType == null)
                        {
                            newExpression = Expression.Call(Expression.Constant(inlinedReference, typeof(LocationReference)), s_locationReferenceGetLocationMethod, originalExpression.Arguments[0]);
                        }
                        else
                        {
                            newExpression = Expression.Call(contextExpression, s_activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                        }

                        return true;
                    }
                }
            }

            newExpression = originalExpression;
            return false;
        }

        private static bool CustomMemberResolver(Expression expression, out object memberValue)
        {
            memberValue = null;

            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    ConstantExpression constantExpression = expression as ConstantExpression;
                    memberValue = constantExpression.Value;
                    // memberValue = null means:
                    // 1. The expression does not follow the common patterns(local, field or property)
                    // which we optimize(do not compile using Linq compiler) and try to resolve directly in this method 
                    // OR 2. The expression actually resolved to null.
                    // In both these cases, we compile the expression and run it so that we have a single error path.
                    return memberValue != null;

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = expression as MemberExpression;
                    if (memberExpression.Expression != null)
                    {
                        CustomMemberResolver(memberExpression.Expression, out memberValue);
                        memberValue = GetMemberValue(memberExpression.Member, memberValue);
                    }
                    return memberValue != null;

                default:
                    return false;
            }
        }

        private static object GetMemberValue(MemberInfo memberInfo, object owner)
        {
            if (owner == null)
            {
                // We do not want to throw any exceptions here. We 
                // will just do the regular compile in this case.
                return null;
            }

            //MemberTypes memberType = memberInfo.MemberType;
            //if (memberType == MemberTypes.Property)
            //{
            //    PropertyInfo propertyInfo = memberInfo as PropertyInfo;
            //    return propertyInfo.GetValue(owner, null);

            //}
            //else if (memberType == MemberTypes.Field)
            //{
            //    FieldInfo fieldInfo = memberInfo as FieldInfo;
            //    return fieldInfo.GetValue(owner);
            //}

            PropertyInfo propertyInfo = memberInfo as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(owner, null);
            }
            else
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(owner);
                }
            }

            return null;
        }

        private static bool TryGetInlinedLocationReference(MethodCallExpression originalExpression, Expression locationReferenceExpression, out LocationReference inlinedReference, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            inlinedReference = null;

            LocationReference locationReference = null;
            object tempLocationReference;
            if (CustomMemberResolver(locationReferenceExpression, out tempLocationReference) && tempLocationReference is LocationReference)
            {
                locationReference = (LocationReference)tempLocationReference;
            }
            else
            {
                try
                {
                    Expression<Func<LocationReference>> locationReferenceLambda = Expression.Lambda<Func<LocationReference>>(locationReferenceExpression);
                    Func<LocationReference> locationReferenceFunc = locationReferenceLambda.Compile();
                    locationReference = locationReferenceFunc();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(locationReferenceExpression.Type, originalExpression, e));
                    return false;
                }
            }

            if (locationReference == null)
            {
                publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(locationReferenceExpression.Type, originalExpression, SR.SubexpressionResultWasNull(locationReferenceExpression.Type)));
                return false;
            }
            else if (!TryGetInlinedReference(publicAccessor, locationReference, isLocationExpression, out inlinedReference))
            {
                publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(locationReferenceExpression.Type, originalExpression, SR.SubexpressionResultWasNotVisible(locationReferenceExpression.Type)));
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool TryRewriteActivityContextGetValueCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            newExpression = originalExpression;

            LocationReference inlinedReference = null;

            // We verify that this is a method we are expecting (single parameter
            // of either LocationReference or Argument type).  If not, we won't
            // rewrite it at all and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression parameterExpression = argumentExpressions[0];

                if (TypeHelper.AreTypesCompatible(parameterExpression.Type, typeof(Argument)))
                {
                    if (!TryGetInlinedArgumentReference(originalExpression, parameterExpression, out inlinedReference, publicAccessor, isLocationExpression))
                    {
                        publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(parameterExpression.Type, originalExpression, SR.SubexpressionResultWasNotVisible(parameterExpression.Type)));
                        return false;
                    }
                }
                else if (TypeHelper.AreTypesCompatible(parameterExpression.Type, typeof(LocationReference)))
                {
                    if (!TryGetInlinedLocationReference(originalExpression, parameterExpression, out inlinedReference, publicAccessor, isLocationExpression))
                    {
                        publicAccessor.ActivityMetadata.AddValidationError(SR.ErrorExtractingValuesForLambdaRewrite(parameterExpression.Type, originalExpression, SR.SubexpressionResultWasNotVisible(parameterExpression.Type)));
                        return false;
                    }
                }
            }

            if (inlinedReference != null)
            {
                newExpression = Expression.Call(originalExpression.Object, s_activityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                return true;
            }

            return false;
        }

        private static bool TryRewriteActivityContextGetLocationCall(MethodCallExpression originalExpression, Type returnType, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            // We verify that this is a method we are expecting (single parameter
            // of LocationReference type).  If not, we won't rewrite it at all
            // and will just let it fail at runtime.
            ReadOnlyCollection<Expression> argumentExpressions = originalExpression.Arguments;

            if (argumentExpressions.Count == 1)
            {
                Expression locationReference = argumentExpressions[0];

                if (TypeHelper.AreTypesCompatible(locationReference.Type, s_locationReferenceType))
                {
                    LocationReference inlinedReference;
                    if (TryGetInlinedLocationReference(originalExpression, originalExpression.Arguments[0], out inlinedReference, publicAccessor, true))
                    {
                        newExpression = Expression.Call(originalExpression.Object, s_activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
                        return true;
                    }
                }
            }

            newExpression = originalExpression;
            return false;
        }

        // Local perf testing leads to the following preference for matching method infos:
        //   * object.ReferenceEquals(info1, info2) is the fastest
        //   * info1.Name == "MethodName" is a close second
        //   * object.ReferenceEquals(info1, type.GetMethod("MethodName")) is very slow by comparison
        //   * object.ReferenceEquals(info1, genericMethodDefinition.MakeGenericMethod(typeParameter)) is also very
        //     slow by comparison
        private static bool TryRewriteMethodCall(MethodCallExpression methodCall, out Expression newExpression, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            // NOTE: Here's the set of method call conversions/rewrites that we are
            // performing.  The left hand side of the "=>" is the pattern that from
            // the original expression using the following shorthand for instances of
            // types:
            //    ctx = ActivityContext
            //    inArg = InArgument<T>
            //    inOutArg = InOutArgument<T>
            //    outArg = OutArgument<T>
            //    arg = Argument
            //    runtimeArg = RuntimeArgument
            //    ref = LocationReference (and subclasses)
            // 
            // The right hand side of the "=>" shows the rewritten method call.  When
            // the same symbol shows up on both sides that means we will use the same
            // expression (IE - ref.Get(ctx) => ctx.GetValue<T>(inline) means that the
            // expression for ctx on the left side is the same expression we should use
            // on the right side).
            //
            // "inline" is used in the right hand side to signify the inlined location
            // reference.  Except where explicitly called out, this is the inlined
            // version of the LocationReference (or subclass) from the left hand side.
            //
            // If the left-hand-side method is Get/GetValue methods, and isLocationExpression
            // is false, we create a read-only InlinedLocationReference, which will produce
            // a RuntimeArgument<T> with ArgumentDirection.In.
            // Otherwise, we create a full-access InlinedLocationReference, which will produce
            // a RuntimeArgument<Location<T>> with ArgumentDirection.In.
            //
            // Finally, "(new)" signifies that the method we are looking for hides a
            // method with the same signature on one of the base classes.
            //
            // ActivityContext
            //    ctx.GetValue<T>(inArg) => ctx.GetValue<T>(inline)  inline = Inline(inArg.RuntimeArgument)
            //    ctx.GetValue<T>(inOutArg) => ctx.GetValue<T>(inline)  inline = Inline(inOutArg.RuntimeArgument)
            //    ctx.GetValue<T>(outArg) => ctx.GetValue<T>(inline)  inline = Inline(outArg.RuntimeArgument)
            //    ctx.GetValue(arg) => ctx.GetValue<object>(inline)  inline = Inline(arg.RuntimeArgument)
            //    ctx.GetValue(runtimeArg) => ctx.GetValue<object>(inline)
            //    ctx.GetValue<T>(ref) => ctx.GetValue<T>(inline)
            //    ctx.GetLocation<T>(ref) => ctx.GetLocation<T>(inline)
            //
            // LocationReference
            //    ref.GetLocation(ctx) => inline.GetLocation(ctx)
            //
            // RuntimeArgument : LocationReference
            //    ref.Get(ctx) => ctx.GetValue<object>(inline)
            //    ref.Get<T>(ctx) => ctx.GetValue<T>(inline)
            //
            // Argument
            //    arg.Get(ctx) => ctx.GetValue<object>(inline)  inline = Inline(arg.RuntimeArgument)
            //    arg.Get<T>(ctx) => ctx.GetValue<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //    arg.GetLocation(ctx) => inline.GetLocation(ctx)  inline = Inline(arg.RuntimeArgument)
            //
            // InArgument<T> : Argument
            //    (new)  arg.Get(ctx) => ctx.GetValue<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //
            // InOutArgument<T> : Argument
            //    (new)  arg.Get(ctx) => ctx.GetValue<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //    (new)  arg.GetLocation<T>(ctx) => ctx.GetLocation<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //
            // OutArgument<T> : Argument
            //    (new)  arg.Get(ctx) => ctx.GetValue<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //    (new)  arg.GetLocation<T>(ctx) => ctx.GetLocation<T>(inline)  inline = Inline(arg.RuntimeArgument)
            //
            // Variable : LocationReference
            //    ref.Get(ctx) => ctx.GetValue<object>(inline)
            //
            // Variable<T> : Variable
            //    (new)  ref.Get(ctx) => ctx.GetValue<T>(inline)
            //    (new)  ref.GetLocation(ctx) => ctx.GetLocation<T>(inline)
            //
            // DelegateArgument : LocationReference
            //    ref.Get(ctx) => ctx.GetValue<object>(inline)
            //
            // DelegateInArgument<T> : DelegateArgument
            //    (new) ref.Get(ctx) => ctx.GetValue<T>(inline)
            //
            // DelegateOutArgument<T> : DelegateArgument
            //    (new) ref.Get(ctx) => ctx.GetValue<T>(inline)
            //    (new) ref.GetLocation(ctx) => ctx.GetLocation<T>(inline)

            MethodInfo targetMethod = methodCall.Method;
            Type targetObjectType = targetMethod.DeclaringType;

            if (targetObjectType.GetTypeInfo().IsGenericType)
            {
                // All of these methods are non-generic methods (they don't introduce a new
                // type parameter), but they do make use of the type parameter of the 
                // generic declaring type.  Because of that we can't do MethodInfo comparison
                // and fall back to string comparison.
                Type targetObjectGenericType = targetObjectType.GetGenericTypeDefinition();

                if (targetObjectGenericType == s_variableGenericType)
                {
                    if (targetMethod.Name == "Get")
                    {
                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                    }
                    else if (targetMethod.Name == "GetLocation")
                    {
                        return TryRewriteLocationReferenceSubclassGetLocationCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor);
                    }
                }
                else if (targetObjectGenericType == s_inArgumentGenericType)
                {
                    if (targetMethod.Name == "Get")
                    {
                        return TryRewriteArgumentGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                    }
                }
                else if (targetObjectGenericType == s_outArgumentGenericType || targetObjectGenericType == s_inOutArgumentGenericType)
                {
                    if (targetMethod.Name == "Get")
                    {
                        return TryRewriteArgumentGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                    }
                    else if (targetMethod.Name == "GetLocation")
                    {
                        return TryRewriteArgumentGetLocationCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor);
                    }
                }
                else if (targetObjectGenericType == s_delegateInArgumentGenericType)
                {
                    if (targetMethod.Name == "Get")
                    {
                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                    }
                }
                else if (targetObjectGenericType == s_delegateOutArgumentGenericType)
                {
                    if (targetMethod.Name == "Get")
                    {
                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                    }
                    else if (targetMethod.Name == "GetLocation")
                    {
                        return TryRewriteLocationReferenceSubclassGetLocationCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor);
                    }
                }
            }
            else
            {
                if (targetObjectType == s_variableType)
                {
                    if (object.ReferenceEquals(targetMethod, s_variableGetMethod))
                    {
                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, TypeHelper.ObjectType, out newExpression, publicAccessor, isLocationExpression);
                    }
                }
                else if (targetObjectType == s_delegateArgumentType)
                {
                    if (object.ReferenceEquals(targetMethod, s_delegateArgumentGetMethod))
                    {
                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, TypeHelper.ObjectType, out newExpression, publicAccessor, isLocationExpression);
                    }
                }
                else if (targetObjectType == s_activityContextType)
                {
                    // We use the string comparison for these two because
                    // we have several overloads of GetValue (some generic,
                    // some not) and GetLocation is a generic method
                    if (targetMethod.Name == "GetValue")
                    {
                        Type returnType = TypeHelper.ObjectType;

                        if (targetMethod.IsGenericMethod)
                        {
                            returnType = targetMethod.GetGenericArguments()[0];
                        }

                        return TryRewriteActivityContextGetValueCall(methodCall, returnType, out newExpression, publicAccessor, isLocationExpression);
                    }
                    else if (targetMethod.IsGenericMethod && targetMethod.Name == "GetLocation")
                    {
                        return TryRewriteActivityContextGetLocationCall(methodCall, targetMethod.GetGenericArguments()[0], out newExpression, publicAccessor);
                    }
                }
                else if (targetObjectType == s_locationReferenceType)
                {
                    if (object.ReferenceEquals(targetMethod, s_locationReferenceGetLocationMethod))
                    {
                        return TryRewriteLocationReferenceSubclassGetLocationCall(methodCall, null, out newExpression, publicAccessor);
                    }
                }
                else if (targetObjectType == s_runtimeArgumentType)
                {
                    // We use string comparison here because we can
                    // match both overloads with a single check.
                    if (targetMethod.Name == "Get")
                    {
                        Type returnType = TypeHelper.ObjectType;

                        if (targetMethod.IsGenericMethod)
                        {
                            returnType = targetMethod.GetGenericArguments()[0];
                        }

                        return TryRewriteLocationReferenceSubclassGetCall(methodCall, returnType, out newExpression, publicAccessor, isLocationExpression);
                    }
                }
                else if (targetObjectType == s_argumentType)
                {
                    // We use string comparison here because we can
                    // match both overloads with a single check.
                    if (targetMethod.Name == "Get")
                    {
                        Type returnType = TypeHelper.ObjectType;

                        if (targetMethod.IsGenericMethod)
                        {
                            returnType = targetMethod.GetGenericArguments()[0];
                        }

                        return TryRewriteArgumentGetCall(methodCall, returnType, out newExpression, publicAccessor, isLocationExpression);
                    }
                    else if (object.ReferenceEquals(targetMethod, s_argumentGetLocationMethod))
                    {
                        return TryRewriteArgumentGetLocationCall(methodCall, null, out newExpression, publicAccessor);
                    }
                }
            }

            // Here's the code for a method call that isn't on our "special" list
            newExpression = methodCall;

            Expression objectExpression;
            IList<Expression> expressionList;

            bool hasChanged = TryRewriteLambdaExpression(methodCall.Object, out objectExpression, publicAccessor);
            hasChanged |= TryRewriteLambdaExpressionCollection(methodCall.Arguments, out expressionList, publicAccessor);

            if (hasChanged)
            {
                newExpression = Expression.Call(objectExpression, targetMethod, expressionList);
            }

            return hasChanged;
        }
    }
}
