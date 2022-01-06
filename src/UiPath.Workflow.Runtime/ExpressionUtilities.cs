// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Activities;
using Internals;
using Runtime;
using XamlIntegration;

internal static class ExpressionUtilities
{
    public static ParameterExpression RuntimeContextParameter = Expression.Parameter(typeof(ActivityContext), "context");
    private static readonly Assembly linqAssembly = typeof(Func<>).Assembly;
    private static readonly MethodInfo createLocationFactoryGenericMethod = typeof(ExpressionUtilities).GetMethod("CreateLocationFactory");

    // Types cached for use in TryRewriteLambdaExpression
    private static readonly Type inArgumentGenericType = typeof(InArgument<>);
    private static readonly Type outArgumentGenericType = typeof(OutArgument<>);
    private static readonly Type inOutArgumentGenericType = typeof(InOutArgument<>);
    private static readonly Type variableGenericType = typeof(Variable<>);
    private static readonly Type delegateInArgumentGenericType = typeof(DelegateInArgument<>);
    private static readonly Type delegateOutArgumentGenericType = typeof(DelegateOutArgument<>);
    private static readonly Type activityContextType = typeof(ActivityContext);
    private static readonly Type locationReferenceType = typeof(LocationReference);
    private static readonly Type runtimeArgumentType = typeof(RuntimeArgument);
    private static readonly Type argumentType = typeof(Argument);
    private static readonly Type variableType = typeof(Variable);
    private static readonly Type delegateArgumentType = typeof(DelegateArgument);

    // MethodInfos cached for use in TryRewriteLambdaExpression
    public static MethodInfo ActivityContextGetValueGenericMethod = typeof(ActivityContext).GetMethod("GetValue", new Type[] { typeof(LocationReference) });
    private static readonly MethodInfo activityContextGetLocationGenericMethod = typeof(ActivityContext).GetMethod("GetLocation", new Type[] { typeof(LocationReference) });
    private static readonly MethodInfo locationReferenceGetLocationMethod = typeof(LocationReference).GetMethod("GetLocation", new Type[] { typeof(ActivityContext) });
    private static readonly MethodInfo argumentGetLocationMethod = typeof(Argument).GetMethod("GetLocation", new Type[] { typeof(ActivityContext) });
    private static readonly MethodInfo variableGetMethod = typeof(Variable).GetMethod("Get", new Type[] { typeof(ActivityContext) });
    private static readonly MethodInfo delegateArgumentGetMethod = typeof(DelegateArgument).GetMethod("Get", new Type[] { typeof(ActivityContext) });

    public static Expression CreateIdentifierExpression(LocationReference locationReference)
        => Expression.Call(RuntimeContextParameter, ActivityContextGetValueGenericMethod.MakeGenericMethod(locationReference.Type), Expression.Constant(locationReference, typeof(LocationReference)));

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
                // This also handles variables, which are emitted as "context.GetLocation<T>("v").Value"
                MemberExpression memberExpression = (MemberExpression)body;
                MemberTypes memberType = memberExpression.Member.MemberType;
                if (memberType == MemberTypes.Field)
                {
                    FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                    if (fieldInfo.IsInitOnly)
                    {
                        // readOnly field
                        return false;
                    }
                    return true;
                }
                else if (memberType == MemberTypes.Property)
                {
                    PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
                    if (!propertyInfo.CanWrite)
                    {
                        // no Setter
                        return false;
                    }
                    return true;
                }
                break;

            case ExpressionType.Call:
                // Depends on the method being called.
                //     System.Array.Get --> multi-dimensional array
                //     get_Item --> might be an indexer property if it's special name & default etc.

                MethodCallExpression callExpression = (MethodCallExpression)body;
                MethodInfo method = callExpression.Method;

                Type declaringType = method.DeclaringType;
                if (declaringType.BaseType == TypeHelper.ArrayType && method.Name == "Get")
                {
                    return true;
                }
                else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    return true;
                }
                else if (method.Name == "GetValue" && declaringType == activityContextType)
                {
                    return true;
                }
                else if (method.Name == "Get" && declaringType.IsGenericType)
                {
                    Type declaringTypeGenericDefinition = declaringType.GetGenericTypeDefinition();

                    if (declaringTypeGenericDefinition == inOutArgumentGenericType ||
                        declaringTypeGenericDefinition == outArgumentGenericType)
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
                // This also handles variables, which are emitted as "context.GetLocation<T>("v").Value"
                MemberTypes memberType = ((MemberExpression)body).Member.MemberType;
                if (memberType == MemberTypes.Field)
                {
                    return new FieldLocationFactory<T>(expression);
                }
                else if (memberType == MemberTypes.Property)
                {
                    return new PropertyLocationFactory<T>(expression);
                }
                else
                {
                    throw FxTrace.Exception.AsError(new NotSupportedException("Lvalues of member type " + memberType));
                }

            case ExpressionType.Call:
                // Depends on the method being called.
                //     System.Array.Get --> multi-dimensional array
                //     get_Item --> might be an indexer property if it's special name & default etc.

                MethodCallExpression callExpression = (MethodCallExpression)body;
                MethodInfo method = callExpression.Method;

                Type declaringType = method.DeclaringType;
                if (declaringType.BaseType == TypeHelper.ArrayType && method.Name == "Get")
                {
                    return new MultidimensionalArrayLocationFactory<T>(expression);
                }
                else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    return new IndexerLocationFactory<T>(expression);
                }
                else if (method.Name == "GetValue" && declaringType == activityContextType)
                {
                    return new LocationReferenceFactory<T>(callExpression.Arguments[0], expression.Parameters);
                }
                else if (method.Name == "Get" && declaringType.IsGenericType)
                {
                    Type declaringTypeGenericDefinition = declaringType.GetGenericTypeDefinition();

                    if (declaringTypeGenericDefinition == inOutArgumentGenericType ||
                        declaringTypeGenericDefinition == outArgumentGenericType)
                    {
                        return new ArgumentFactory<T>(callExpression.Object, expression.Parameters);
                    }
                }

                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExpressionForLocation(body.NodeType)));

            default:
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidExpressionForLocation(body.NodeType)));
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
        Type genericFuncType = linqAssembly.GetType("System.Func`" + (parameterCount + 1), true);
        Type[] delegateParameterTypes = new Type[parameterCount + 1];

        for (int i = 0; i < parameterCount; ++i)
        {
            delegateParameterTypes[i] = lambdaParameters[i].Type;
        }
        delegateParameterTypes[parameterCount] = expression.Type;
        Type funcType = genericFuncType.MakeGenericType(delegateParameterTypes);
        LambdaExpression parentLambda = Expression.Lambda(funcType, expression, lambdaParameters);

        // call CreateLocationFactory<parentLambda.Type>(parentLambda);
        MethodInfo typedMethod = createLocationFactoryGenericMethod.MakeGenericMethod(expression.Type);
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

    /// <remarks>
    /// Unused for now, but keep around for possible use during activity validation
    /// </remarks>
    private static T Evaluate<T>(Expression objectExpression, ReadOnlyCollection<ParameterExpression> parametersCollection, ActivityContext context)
    {
        Func<ActivityContext, T> objectFunc = Compile<T>(objectExpression, parametersCollection);
        return objectFunc(context);
    }

    // for single-dimensional arrays 
    private class ArrayLocationFactory<T> : LocationFactory<T>
    {
        private readonly Func<ActivityContext, T[]> arrayFunction;
        private readonly Func<ActivityContext, int> indexFunction;

        public ArrayLocationFactory(LambdaExpression expression)
        {
            Fx.Assert(expression.Body.NodeType == ExpressionType.ArrayIndex, "ArrayIndex expression required");
            BinaryExpression arrayIndexExpression = (BinaryExpression)expression.Body;

            this.arrayFunction = Compile<T[]>(arrayIndexExpression.Left, expression.Parameters);
            this.indexFunction = Compile<int>(arrayIndexExpression.Right, expression.Parameters);
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            return new ArrayLocation(this.arrayFunction(context), this.indexFunction(context));
        }

        [DataContract]
        internal class ArrayLocation : Location<T>
        {
            private T[] array;
            private int index;

            public ArrayLocation(T[] array, int index)
                : base()
            {
                this.array = array;
                this.index = index;
            }

            public override T Value
            {
                get
                {
                    return this.array[this.index];
                }
                set
                {
                    this.array[this.index] = value;
                }
            }

            [DataMember(Name = "array")]
            internal T[] SerializedArray
            {
                get { return this.array; }
                set { this.array = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "index")]
            internal int SerializedIndex
            {
                get { return this.index; }
                set { this.index = value; }
            }
        }
    }

    private class FieldLocationFactory<T> : LocationFactory<T>
    {
        private readonly FieldInfo _fieldInfo;
        private readonly Func<ActivityContext, object> _ownerFunction;
        private readonly LocationFactory _parentFactory;

        public FieldLocationFactory(LambdaExpression expression)
        {
            Fx.Assert(expression.Body.NodeType == ExpressionType.MemberAccess, "field expression required");
            MemberExpression memberExpression = (MemberExpression)expression.Body;

            Fx.Assert(memberExpression.Member.MemberType == MemberTypes.Field, "member field expected");
            this._fieldInfo = (FieldInfo)memberExpression.Member;

            if (this._fieldInfo.IsStatic)
            {
                this._ownerFunction = null;
            }
            else
            {
                this._ownerFunction = Compile<object>(
                Expression.Convert(memberExpression.Expression, TypeHelper.ObjectType), expression.Parameters);
            }

            if (this._fieldInfo.DeclaringType.IsValueType)
            {
                // may want to set a struct, so we need to make an expression in order to set the parent
                _parentFactory = CreateParentReference(memberExpression.Expression, expression.Parameters);
            }
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            object owner = null;
            if (this._ownerFunction != null)
            {
                owner = this._ownerFunction(context);
            }

            Location parent = null;
            if (_parentFactory != null)
            {
                parent = _parentFactory.CreateLocation(context);
            }
            return new FieldLocation(this._fieldInfo, owner, parent);
        }

        [DataContract]
        internal class FieldLocation : Location<T>
        {
            private FieldInfo fieldInfo;
            private object owner;
            private Location parent;

            public FieldLocation(FieldInfo fieldInfo, object owner, Location parent)
                : base()
            {
                this.fieldInfo = fieldInfo;
                this.owner = owner;
                this.parent = parent;
            }

            //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotRaiseReservedExceptionTypes,
            //    Justification = "Need to raise NullReferenceException to match expected failure case in workflows.")]
            public override T Value
            {
                get
                {
                    if (this.owner == null && !this.fieldInfo.IsStatic)
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.fieldInfo.Name)));
                    }

                    return (T)this.fieldInfo.GetValue(this.owner);
                }
                set
                {
                    if (this.owner == null && !this.fieldInfo.IsStatic)
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.fieldInfo.Name)));
                    }

                    this.fieldInfo.SetValue(this.owner, value);
                    if (this.parent != null)
                    {
                        // Looks like we are trying to set a field on a struct
                        // Calling SetValue simply sets the field on the local copy of the struct, which is not very helpful
                        // Since we have a copy, assign it back to the parent
                        this.parent.Value = this.owner;
                    }
                }
            }

            [DataMember(Name = "fieldInfo")]
            internal FieldInfo SerializedFieldInfo
            {
                get { return this.fieldInfo; }
                set { this.fieldInfo = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "owner")]
            internal object SerializedOwner
            {
                get { return this.owner; }
                set { this.owner = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "parent")]
            internal Location SerializedParent
            {
                get { return this.parent; }
                set { this.parent = value; }
            }
        }
    }

    private class ArgumentFactory<T> : LocationFactory<T>
    {
        private readonly Func<ActivityContext, Argument> argumentFunction;

        public ArgumentFactory(Expression argumentExpression, ReadOnlyCollection<ParameterExpression> expressionParameters)
        {
            this.argumentFunction = Compile<Argument>(argumentExpression, expressionParameters);
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            Argument argument = this.argumentFunction(context);

            return argument.RuntimeArgument.GetLocation(context) as Location<T>;
        }
    }

    private class LocationReferenceFactory<T> : LocationFactory<T>
    {
        private readonly Func<ActivityContext, LocationReference> locationReferenceFunction;

        public LocationReferenceFactory(Expression locationReferenceExpression, ReadOnlyCollection<ParameterExpression> expressionParameters)
        {
            this.locationReferenceFunction = Compile<LocationReference>(locationReferenceExpression, expressionParameters);
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            LocationReference locationReference = this.locationReferenceFunction(context);
            return locationReference.GetLocation(context) as Location<T>;
        }
    }

    private class IndexerLocationFactory<T> : LocationFactory<T>
    {
        private readonly MethodInfo getItemMethod;
        private readonly string indexerName;
        private readonly MethodInfo setItemMethod;
        private readonly Func<ActivityContext, object>[] setItemArgumentFunctions;
        private readonly Func<ActivityContext, object> targetObjectFunction;

        public IndexerLocationFactory(LambdaExpression expression)
        {
            Fx.Assert(expression.Body.NodeType == ExpressionType.Call, "Call expression required.");

            MethodCallExpression callExpression = (MethodCallExpression)expression.Body;
            this.getItemMethod = callExpression.Method;

            Fx.Assert(this.getItemMethod.IsSpecialName && this.getItemMethod.Name.StartsWith("get_", StringComparison.Ordinal), "Special get_Item method required.");

            //  Get the set_Item accessor for the same set of parameter/return types if any.
            this.indexerName = this.getItemMethod.Name[4..];
            string setItemName = "set_" + this.indexerName;
            ParameterInfo[] getItemParameters = this.getItemMethod.GetParameters();
            Type[] setItemParameterTypes = new Type[getItemParameters.Length + 1];

            for (int i = 0; i < getItemParameters.Length; i++)
            {
                setItemParameterTypes[i] = getItemParameters[i].ParameterType;
            }
            setItemParameterTypes[getItemParameters.Length] = this.getItemMethod.ReturnType;

            this.setItemMethod = this.getItemMethod.DeclaringType.GetMethod(
                setItemName, BindingFlags.Public | BindingFlags.Instance, null, setItemParameterTypes, null);

            if (this.setItemMethod != null)
            {
                //  Get the target object and all the setter's arguments 
                //  (minus the actual value to be set).
                this.targetObjectFunction = Compile<object>(callExpression.Object, expression.Parameters);

                this.setItemArgumentFunctions = new Func<ActivityContext, object>[callExpression.Arguments.Count];
                for (int i = 0; i < callExpression.Arguments.Count; i++)
                {
                    // convert value types to objects since Linq doesn't do it automatically
                    Expression argument = callExpression.Arguments[i];
                    if (argument.Type.IsValueType)
                    {
                        argument = Expression.Convert(argument, TypeHelper.ObjectType);
                    }
                    this.setItemArgumentFunctions[i] = Compile<object>(argument, expression.Parameters);
                }
            }
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            object targetObject = null;
            object[] setItemArguments = null;

            if (this.setItemMethod != null)
            {
                targetObject = this.targetObjectFunction(context);

                setItemArguments = new object[this.setItemArgumentFunctions.Length];

                for (int i = 0; i < this.setItemArgumentFunctions.Length; i++)
                {
                    setItemArguments[i] = this.setItemArgumentFunctions[i](context);
                }
            }

            return new IndexerLocation(this.indexerName, this.getItemMethod, this.setItemMethod, targetObject, setItemArguments);
        }

        [DataContract]
        internal class IndexerLocation : Location<T>
        {
            private string indexerName;
            private MethodInfo getItemMethod;
            private MethodInfo setItemMethod;
            private object targetObject;
            private object[] setItemArguments;

            public IndexerLocation(string indexerName, MethodInfo getItemMethod, MethodInfo setItemMethod,
                object targetObject, object[] getItemArguments)
                : base()
            {
                this.indexerName = indexerName;
                this.getItemMethod = getItemMethod;
                this.setItemMethod = setItemMethod;
                this.targetObject = targetObject;
                this.setItemArguments = getItemArguments;
            }

            //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotRaiseReservedExceptionTypes,
            //Justification = "Need to raise NullReferenceException to match expected failure case in workflows.")]
            public override T Value
            {
                get
                {
                    if (this.targetObject == null && !this.getItemMethod.IsStatic)
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.getItemMethod.Name)));
                    }

                    return (T)this.getItemMethod.Invoke(this.targetObject, this.setItemArguments);
                }

                set
                {

                    if (this.setItemMethod == null)
                    {
                        string targetObjectTypeName = this.targetObject.GetType().Name;
                        throw FxTrace.Exception.AsError(new InvalidOperationException(
                            SR.MissingSetAccessorForIndexer(this.indexerName, targetObjectTypeName)));
                    }

                    if (this.targetObject == null && !this.setItemMethod.IsStatic)
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.setItemMethod.Name)));
                    }

                    object[] localSetItemArguments = new object[this.setItemArguments.Length + 1];
                    Array.ConstrainedCopy(this.setItemArguments, 0, localSetItemArguments, 0, this.setItemArguments.Length);
                    localSetItemArguments[^1] = value;

                    this.setItemMethod.Invoke(this.targetObject, localSetItemArguments);
                }
            }

            [DataMember(Name = "indexerName")]
            internal string SerializedIndexerName
            {
                get { return this.indexerName; }
                set { this.indexerName = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "getItemMethod")]
            internal MethodInfo SerializedGetItemMethod
            {
                get { return this.getItemMethod; }
                set { this.getItemMethod = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "setItemMethod")]
            internal MethodInfo SerializedSetItemMethod
            {
                get { return this.setItemMethod; }
                set { this.setItemMethod = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "targetObject")]
            internal object SerializedTargetObject
            {
                get { return this.targetObject; }
                set { this.targetObject = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "setItemArguments")]
            internal object[] SerializedSetItemArguments
            {
                get { return this.setItemArguments; }
                set { this.setItemArguments = value; }
            }
        }
    }

    private class MultidimensionalArrayLocationFactory<T> : LocationFactory<T>
    {
        private readonly Func<ActivityContext, Array> arrayFunction;
        private readonly Func<ActivityContext, int>[] indexFunctions;

        public MultidimensionalArrayLocationFactory(LambdaExpression expression)
        {
            Fx.Assert(expression.Body.NodeType == ExpressionType.Call, "Call expression required.");
            MethodCallExpression callExpression = (MethodCallExpression)expression.Body;

            this.arrayFunction = Compile<Array>(
                callExpression.Object, expression.Parameters);

            this.indexFunctions = new Func<ActivityContext, int>[callExpression.Arguments.Count];
            for (int i = 0; i < this.indexFunctions.Length; i++)
            {
                this.indexFunctions[i] = Compile<int>(
                    callExpression.Arguments[i], expression.Parameters);
            }
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            int[] indices = new int[this.indexFunctions.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = this.indexFunctions[i](context);
            }
            return new MultidimensionalArrayLocation(this.arrayFunction(context), indices);
        }

        [DataContract]
        internal class MultidimensionalArrayLocation : Location<T>
        {
            private Array array;
            private int[] indices;

            public MultidimensionalArrayLocation(Array array, int[] indices)
                : base()
            {
                this.array = array;
                this.indices = indices;
            }

            public override T Value
            {
                get
                {
                    return (T)this.array.GetValue(this.indices);
                }

                set
                {
                    this.array.SetValue(value, this.indices);
                }
            }

            [DataMember(Name = "array")]
            internal Array SerializedArray
            {
                get { return this.array; }
                set { this.array = value; }
            }

            [DataMember(Name = "indices")]
            internal int[] SerializedIndicess
            {
                get { return this.indices; }
                set { this.indices = value; }
            }
        }
    }

    private class PropertyLocationFactory<T> : LocationFactory<T>
    {
        private readonly Func<ActivityContext, object> _ownerFunction;
        private readonly PropertyInfo _propertyInfo;
        private readonly LocationFactory _parentFactory;

        public PropertyLocationFactory(LambdaExpression expression)
        {
            Fx.Assert(expression.Body.NodeType == ExpressionType.MemberAccess, "member access expression required");
            MemberExpression memberExpression = (MemberExpression)expression.Body;

            Fx.Assert(memberExpression.Member.MemberType == MemberTypes.Property, "property access expression expected");
            this._propertyInfo = (PropertyInfo)memberExpression.Member;

            if (memberExpression.Expression == null)
            {
                // static property
                this._ownerFunction = null;
            }
            else
            {
                this._ownerFunction = Compile<object>(
                    Expression.Convert(memberExpression.Expression, TypeHelper.ObjectType), expression.Parameters);
            }

            if (this._propertyInfo.DeclaringType.IsValueType)
            {
                // may want to set a struct, so we need to make an expression in order to set the parent
                _parentFactory = CreateParentReference(memberExpression.Expression, expression.Parameters);
            }
        }

        public override Location<T> CreateLocation(ActivityContext context)
        {
            object owner = null;
            if (this._ownerFunction != null)
            {
                owner = this._ownerFunction(context);
            }

            Location parent = null;
            if (_parentFactory != null)
            {
                parent = _parentFactory.CreateLocation(context);
            }
            return new PropertyLocation(this._propertyInfo, owner, parent);
        }

        [DataContract]
        internal class PropertyLocation : Location<T>
        {
            private object owner;
            private PropertyInfo propertyInfo;
            private Location parent;

            public PropertyLocation(PropertyInfo propertyInfo, object owner, Location parent)
                : base()
            {
                this.propertyInfo = propertyInfo;
                this.owner = owner;
                this.parent = parent;
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
                    MethodInfo getMethodInfo = this.propertyInfo.GetGetMethod();
                    if (getMethodInfo == null && !TypeHelper.AreTypesCompatible(this.propertyInfo.DeclaringType, typeof(Location)))
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteonlyPropertyCannotBeRead(this.propertyInfo.DeclaringType, this.propertyInfo.Name)));
                    }

                    if (this.owner == null && (getMethodInfo == null || !getMethodInfo.IsStatic))
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.propertyInfo.Name)));
                    }

                    // Okay, it's public
                    return (T)this.propertyInfo.GetValue(this.owner, null);
                }

                set
                {
                    // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                    // from the other's perspective, not internal properties, so they're okay as a special case.
                    // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                    // at an implementation level it is.
                    MethodInfo setMethodInfo = this.propertyInfo.GetSetMethod();
                    if (setMethodInfo == null && !TypeHelper.AreTypesCompatible(this.propertyInfo.DeclaringType, typeof(Location)))
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ReadonlyPropertyCannotBeSet(this.propertyInfo.DeclaringType, this.propertyInfo.Name)));
                    }

                    if (this.owner == null && (setMethodInfo == null || !setMethodInfo.IsStatic))
                    {
                        throw FxTrace.Exception.AsError(new NullReferenceException(SR.CannotDereferenceNull(this.propertyInfo.Name)));
                    }

                    // Okay, it's public
                    this.propertyInfo.SetValue(this.owner, value, null);
                    if (this.parent != null)
                    {
                        // Looks like we are trying to set a property on a struct
                        // Calling SetValue simply sets the property on the local copy of the struct, which is not very helpful
                        // Since we have a copy, assign it back to the parent
                        this.parent.Value = this.owner;
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "owner")]
            internal object SerializedOwner
            {
                get { return this.owner; }
                set { this.owner = value; }
            }

            [DataMember(Name = "propertyInfo")]
            internal PropertyInfo SerializedPropertyInfo
            {
                get { return this.propertyInfo; }
                set { this.propertyInfo = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "parent")]
            internal Location SerializedParent
            {
                get { return this.parent; }
                set { this.parent = value; }
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

        bool hasChanged = false;
        Expression left;
        Expression right;
        Expression other;
        IList<Expression> expressionList;
        MethodCallExpression methodCall;
        BinaryExpression binaryExpression;
        NewArrayExpression newArray;
        UnaryExpression unaryExpression;

        // Share some local declarations across the switch
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
                IList<ElementInit> initializerList;
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
                bool subTreeIsLocationExpression = isLocationExpression && memberExpression.Member.DeclaringType.IsValueType;

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
                IList<MemberBinding> bindingList;
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

            if (TryRewriteMemberBinding(binding, out MemberBinding newBinding, publicAccessor))
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
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                MemberAssignment assignment = (MemberAssignment)binding;

                Expression other;
                hasChanged |= TryRewriteLambdaExpression(assignment.Expression, out other, publicAccessor);

                if (hasChanged)
                {
                    newBinding = Expression.Bind(assignment.Member, other);
                }
                break;

            case MemberBindingType.ListBinding:
                MemberListBinding list = (MemberListBinding)binding;

                IList<ElementInit> initializerList;
                hasChanged |= TryRewriteLambdaExpressionInitializersCollection(list.Initializers, out initializerList, publicAccessor);

                if (hasChanged)
                {
                    newBinding = Expression.ListBind(list.Member, initializerList);
                }
                break;

            case MemberBindingType.MemberBinding:
                MemberMemberBinding member = (MemberMemberBinding)binding;

                IList<MemberBinding> bindingList;
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

            if (TryRewriteLambdaExpression(expression, out Expression newExpression, publicAccessor))
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

            if (TryRewriteLambdaExpressionCollection(elementInit.Arguments, out IList<Expression> newExpressions, publicAccessor))
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

        if (CustomMemberResolver(argumentExpression, out object tempArgument) && tempArgument is Argument argument1)
        {
            argument = argument1;
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
                if (memberExpression.Member.MemberType == MemberTypes.Property)
                {
                    RuntimeArgument runtimeArgument = ActivityUtilities.FindArgument(memberExpression.Member.Name, publicAccessor.ActivityMetadata.CurrentActivity);

                    if (runtimeArgument != null && TryGetInlinedReference(publicAccessor, runtimeArgument, isLocationExpression, out inlinedReference))
                    {
                        return true;
                    }
                }
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

            if (contextExpression.Type == activityContextType)
            {
                if (TryGetInlinedArgumentReference(originalExpression, originalExpression.Object, out LocationReference inlinedReference, publicAccessor, isLocationExpression))
                {
                    newExpression = Expression.Call(contextExpression, ActivityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

            if (contextExpression.Type == activityContextType)
            {
                if (TryGetInlinedArgumentReference(originalExpression, originalExpression.Object, out LocationReference inlinedReference, publicAccessor, true))
                {
                    if (returnType == null)
                    {
                        newExpression = Expression.Call(Expression.Constant(inlinedReference, typeof(LocationReference)), locationReferenceGetLocationMethod, contextExpression);
                    }
                    else
                    {
                        newExpression = Expression.Call(contextExpression, activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

            if (contextExpression.Type == activityContextType)
            {
                if (TryGetInlinedLocationReference(originalExpression, originalExpression.Object, out LocationReference inlinedReference, publicAccessor, isLocationExpression))
                {
                    newExpression = Expression.Call(contextExpression, ActivityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

            if (contextExpression.Type == activityContextType)
            {
                if (TryGetInlinedLocationReference(originalExpression, originalExpression.Object, out LocationReference inlinedReference, publicAccessor, true))
                {
                    if (returnType == null)
                    {
                        newExpression = Expression.Call(Expression.Constant(inlinedReference, typeof(LocationReference)), locationReferenceGetLocationMethod, originalExpression.Arguments[0]);
                    }
                    else
                    {
                        newExpression = Expression.Call(contextExpression, activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

        MemberTypes memberType = memberInfo.MemberType;
        if (memberType == MemberTypes.Property)
        {
            PropertyInfo propertyInfo = memberInfo as PropertyInfo;
            return propertyInfo.GetValue(owner, null);

        }
        else if (memberType == MemberTypes.Field)
        {
            FieldInfo fieldInfo = memberInfo as FieldInfo;
            return fieldInfo.GetValue(owner);
        }
        return null;
    }

    private static bool TryGetInlinedLocationReference(MethodCallExpression originalExpression, Expression locationReferenceExpression, out LocationReference inlinedReference, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
    {
        inlinedReference = null;

        LocationReference locationReference = null;
        if (CustomMemberResolver(locationReferenceExpression, out object tempLocationReference) && tempLocationReference is LocationReference reference)
        {
            locationReference = reference;
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
            newExpression = Expression.Call(originalExpression.Object, ActivityContextGetValueGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

            if (TypeHelper.AreTypesCompatible(locationReference.Type, locationReferenceType))
            {
                if (TryGetInlinedLocationReference(originalExpression, originalExpression.Arguments[0], out LocationReference inlinedReference, publicAccessor, true))
                {
                    newExpression = Expression.Call(originalExpression.Object, activityContextGetLocationGenericMethod.MakeGenericMethod(returnType), Expression.Constant(inlinedReference, typeof(LocationReference)));
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

        if (targetObjectType.IsGenericType)
        {
            // All of these methods are non-generic methods (they don't introduce a new
            // type parameter), but they do make use of the type parameter of the 
            // generic declaring type.  Because of that we can't do MethodInfo comparison
            // and fall back to string comparison.
            Type targetObjectGenericType = targetObjectType.GetGenericTypeDefinition();

            if (targetObjectGenericType == variableGenericType)
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
            else if (targetObjectGenericType == inArgumentGenericType)
            {
                if (targetMethod.Name == "Get")
                {
                    return TryRewriteArgumentGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                }
            }
            else if (targetObjectGenericType == outArgumentGenericType || targetObjectGenericType == inOutArgumentGenericType)
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
            else if (targetObjectGenericType == delegateInArgumentGenericType)
            {
                if (targetMethod.Name == "Get")
                {
                    return TryRewriteLocationReferenceSubclassGetCall(methodCall, targetObjectType.GetGenericArguments()[0], out newExpression, publicAccessor, isLocationExpression);
                }
            }
            else if (targetObjectGenericType == delegateOutArgumentGenericType)
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
            if (targetObjectType == variableType)
            {
                if (ReferenceEquals(targetMethod, variableGetMethod))
                {
                    return TryRewriteLocationReferenceSubclassGetCall(methodCall, TypeHelper.ObjectType, out newExpression, publicAccessor, isLocationExpression);
                }
            }
            else if (targetObjectType == delegateArgumentType)
            {
                if (ReferenceEquals(targetMethod, delegateArgumentGetMethod))
                {
                    return TryRewriteLocationReferenceSubclassGetCall(methodCall, TypeHelper.ObjectType, out newExpression, publicAccessor, isLocationExpression);
                }
            }
            else if (targetObjectType == activityContextType)
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
            else if (targetObjectType == locationReferenceType)
            {
                if (ReferenceEquals(targetMethod, locationReferenceGetLocationMethod))
                {
                    return TryRewriteLocationReferenceSubclassGetLocationCall(methodCall, null, out newExpression, publicAccessor);
                }
            }
            else if (targetObjectType == runtimeArgumentType)
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
            else if (targetObjectType == argumentType)
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
                else if (ReferenceEquals(targetMethod, argumentGetLocationMethod))
                {
                    return TryRewriteArgumentGetLocationCall(methodCall, null, out newExpression, publicAccessor);
                }
            }
        }

        // Here's the code for a method call that isn't on our "special" list
        newExpression = methodCall;

        bool hasChanged = TryRewriteLambdaExpression(methodCall.Object, out Expression objectExpression, publicAccessor);
        hasChanged |= TryRewriteLambdaExpressionCollection(methodCall.Arguments, out IList<Expression> expressionList, publicAccessor);

        if (hasChanged)
        {
            newExpression = Expression.Call(objectExpression, targetMethod, expressionList);
        }

        return hasChanged;
    }

    internal static Expression RewriteNonCompiledExpressionTree(LambdaExpression originalLambdaExpression)
    {
        ExpressionTreeRewriter expressionVisitor = new();
        return expressionVisitor.Visit(Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(ActivityContext), originalLambdaExpression.ReturnType),
            originalLambdaExpression.Body,
            new ParameterExpression[] { RuntimeContextParameter }));
    }
}
