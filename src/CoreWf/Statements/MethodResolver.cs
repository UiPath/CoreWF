// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Activities.Statements;

// Helper class for InvokeMethod.
// Factory for MethodExecutor strategies. Conceptually, resolves to the correct MethodInfo based on target type,
// method name, parameters, and async flags + availability of Begin/End paired methods of the correct static-ness.
internal sealed class MethodResolver
{
    private static readonly BindingFlags staticBindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
    private static readonly BindingFlags instanceBindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
    private static readonly string staticString = "static";     // Used in error messages below. Technical term, not localizable.
    private static readonly string instanceString = "instance"; // Used in error messages below. Technical term, not localizable.
    private MethodInfo _syncMethod;
    private MethodInfo _beginMethod;
    private MethodInfo _endMethod;

    public MethodResolver() { }

    public Collection<Type> GenericTypeArguments { get; set; }

    public string MethodName { get; set; }

    public Collection<Argument> Parameters { get; set; }

    public RuntimeArgument Result { get; set; }

    public InArgument TargetObject { get; set; }

    public Type TargetType { get; set; }

    public bool RunAsynchronously { get; set; }

    public Activity Parent { get; set; }

    // Sometimes we may know the result type even if it won't be used,
    // i.e. it comes from an InvokeMethod<T>. We will want to generate
    // errors if it doesn't match the method's return value. 
    internal Type ResultType { get; set; }

    private static bool HaveParameterArray(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0)
        {
            return false;
        }

        ParameterInfo last = parameters[^1];
        return last.GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0;
    }

    // The Arguments added by the activity are named according to the method resolved by the MethodResolver.
    public void RegisterParameters(IList<RuntimeArgument> arguments)
    {
        bool useAsyncPattern = RunAsynchronously && _beginMethod != null && _endMethod != null;

        if (_syncMethod != null || useAsyncPattern)
        {
            ParameterInfo[] formalParameters;
            int formalParamCount;
            string paramArrayBaseName = "";
            bool haveParameterArray = false;

            if (useAsyncPattern)
            {
                formalParameters = _beginMethod.GetParameters();
                formalParamCount = formalParameters.Length - 2;
            }
            else
            {
                formalParameters = _syncMethod.GetParameters();
                haveParameterArray = HaveParameterArray(formalParameters);

                if (haveParameterArray)
                {
                    formalParamCount = formalParameters.Length - 1;
                    paramArrayBaseName = formalParameters[formalParamCount].Name;
                }
                else
                {
                    formalParamCount = formalParameters.Length;
                }
            }

            for (int i = 0; i < formalParamCount; i++)
            {
                string name = formalParameters[i].Name;
                //for some methods like int[,].Get(int,int), formal parameters have no names in reflection info
                if (string.IsNullOrEmpty(name))
                {
                    name = "Parameter" + i;
                }

                RuntimeArgument argument = new(name, Parameters[i].ArgumentType, Parameters[i].Direction, true);
                Argument.Bind(Parameters[i], argument);
                arguments.Add(argument);

                if (!useAsyncPattern && haveParameterArray)
                {
                    // Attempt to uniquify parameter names
                    if (name.StartsWith(paramArrayBaseName, false, null))
                    {
                        if (int.TryParse(name.AsSpan(paramArrayBaseName.Length), NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out _))
                        {
                            paramArrayBaseName += "_";
                        }
                    }
                }
            }

            if (!useAsyncPattern && haveParameterArray)
            {
                // RuntimeArgument bindings need names. In the case of params arrays, synthesize names based on the name of the formal params parameter
                // plus a counter.
                int paramArrayCount = Parameters.Count - formalParamCount;

                for (int i = 0; i < paramArrayCount; i++)
                {
                    string name = paramArrayBaseName + i;
                    int index = formalParamCount + i;
                    RuntimeArgument argument = new(name, Parameters[index].ArgumentType, Parameters[index].Direction, true);
                    Argument.Bind(Parameters[index], argument);
                    arguments.Add(argument);
                }
            }
        }
        else
        {
            // We're still at design-time: make up "fake" arguments based on the parameters
            for (int i = 0; i < Parameters.Count; i++)
            {
                string name = "argument" + i;
                RuntimeArgument argument = new(name, Parameters[i].ArgumentType, Parameters[i].Direction, true);
                Argument.Bind(Parameters[i], argument);
                arguments.Add(argument);
            }
        }
    }

    public void Trace()
    {
        bool useAsyncPattern = RunAsynchronously && _beginMethod != null && _endMethod != null;

        if (useAsyncPattern)
        {
            if (TD.InvokeMethodUseAsyncPatternIsEnabled())
            {
                TD.InvokeMethodUseAsyncPattern(Parent.DisplayName, _beginMethod.ToString(), _endMethod.ToString());
            }
        }
        else
        {
            if (RunAsynchronously)
            {
                if (TD.InvokeMethodDoesNotUseAsyncPatternIsEnabled())
                {
                    TD.InvokeMethodDoesNotUseAsyncPattern(Parent.DisplayName);
                }
            }
        }
    }

    // Set methodExecutor, returning an error string if there are any problems (ambiguous match, etc.).
    public void DetermineMethodInfo(CodeActivityMetadata metadata, MruCache<MethodInfo, Func<object, object[], object>> funcCache, ReaderWriterLockSlim locker,
        ref MethodExecutor methodExecutor)
    {
        bool returnEarly = false;

        MethodExecutor oldMethodExecutor = methodExecutor;
        methodExecutor = null;
        if (string.IsNullOrEmpty(MethodName))
        {
            metadata.AddValidationError(SR.ActivityPropertyMustBeSet("MethodName", Parent.DisplayName));
            returnEarly = true;
        }

        Type targetType = TargetType;

        // If TargetType and the type of TargetObject are both set, it's an error.
        if (targetType != null && TargetObject != null && !TargetObject.IsEmpty)
        {
            metadata.AddValidationError(SR.TargetTypeAndTargetObjectAreMutuallyExclusive(Parent.GetType().Name, Parent.DisplayName));
            returnEarly = true;
        }

        // If TargetType was set, look for a static method. If TargetObject was set, look for an instance method. They can't both be set.
        BindingFlags bindingFlags = TargetType != null ? staticBindingFlags : instanceBindingFlags;
        string bindingType = bindingFlags == staticBindingFlags ? staticString : instanceString;

        if (targetType == null)
        {
            if (TargetObject != null && !TargetObject.IsEmpty)
            {
                targetType = TargetObject.ArgumentType;
            }
            else
            {
                metadata.AddValidationError(SR.OneOfTwoPropertiesMustBeSet("TargetObject", "TargetType", Parent.GetType().Name, Parent.DisplayName));
                returnEarly = true;
            }
        }

        // We've had one or more constraint violations already
        if (returnEarly)
        {
            return;
        }

        // Convert OutArgs and InOutArgs to out/ref types before resolution
        Type[] parameterTypes =
            Parameters.Select(argument => argument.Direction == ArgumentDirection.In ? argument.ArgumentType : argument.ArgumentType.MakeByRefType())
                .ToArray();

        Type[] genericTypeArguments = GenericTypeArguments.ToArray();

        InheritanceAndParamArrayAwareBinder methodBinder = new(targetType, genericTypeArguments, Parent);

        // It may be possible to know (and check) the resultType even if the result won't be assigned anywhere.     
        // Used 1.) for detecting async pattern, and 2.) to make sure we selected the correct MethodInfo.
        Type resultType = ResultType;

        if (RunAsynchronously)
        {
            int formalParamCount = parameterTypes.Length;
            Type[] beginMethodParameterTypes = new Type[formalParamCount + 2];
            for (int i = 0; i < formalParamCount; i++)
            {
                beginMethodParameterTypes[i] = parameterTypes[i];
            }
            beginMethodParameterTypes[formalParamCount] = typeof(AsyncCallback);
            beginMethodParameterTypes[formalParamCount + 1] = typeof(object);

            Type[] endMethodParameterTypes = { typeof(IAsyncResult) };

            _beginMethod = Resolve(targetType, "Begin" + MethodName, bindingFlags,
                methodBinder, beginMethodParameterTypes, genericTypeArguments, true);
            if (_beginMethod != null && !_beginMethod.ReturnType.Equals(typeof(IAsyncResult)))
            {
                _beginMethod = null;
            }
            _endMethod = Resolve(targetType, "End" + MethodName, bindingFlags,
                methodBinder, endMethodParameterTypes, genericTypeArguments, true);
            if (_endMethod != null && resultType != null && !TypeHelper.AreTypesCompatible(_endMethod.ReturnType, resultType))
            {
                metadata.AddValidationError(SR.ReturnTypeIncompatible(_endMethod.ReturnType.Name, MethodName, targetType.Name, Parent.DisplayName, resultType.Name));
                _endMethod = null;
                return;
            }

            if (_beginMethod != null && _endMethod != null && _beginMethod.IsStatic == _endMethod.IsStatic)
            {
                if (oldMethodExecutor is not AsyncPatternMethodExecutor executor ||
                    !executor.IsTheSame(_beginMethod, _endMethod))
                {
                    methodExecutor = new AsyncPatternMethodExecutor(metadata, _beginMethod, _endMethod, Parent,
                        TargetType, TargetObject, Parameters, Result, funcCache, locker);
                }
                else
                {
                    methodExecutor = new AsyncPatternMethodExecutor(executor,
                        TargetType, TargetObject, Parameters, Result);
                }
                return;
            }
        }

        MethodInfo result;
        try
        {
            result = Resolve(targetType, MethodName, bindingFlags,
                methodBinder, parameterTypes, genericTypeArguments, false);
        }
        catch (AmbiguousMatchException)
        {
            metadata.AddValidationError(SR.DuplicateMethodFound(targetType.Name, bindingType, MethodName, Parent.DisplayName));
            return;
        }

        if (result == null)
        {
            metadata.AddValidationError(SR.PublicMethodWithMatchingParameterDoesNotExist(targetType.Name, bindingType, MethodName, Parent.DisplayName));
            return;
        }
        else if (resultType != null && !TypeHelper.AreTypesCompatible(result.ReturnType, resultType))
        {
            metadata.AddValidationError(
                SR.ReturnTypeIncompatible(result.ReturnType.Name, MethodName,
                    targetType.Name, Parent.DisplayName, resultType.Name));
            return;
        }
        else
        {
            _syncMethod = result;
            if (RunAsynchronously)
            {
                if (oldMethodExecutor is not AsyncWaitCallbackMethodExecutor executor ||
                    !executor.IsTheSame(_syncMethod))
                {
                    methodExecutor = new AsyncWaitCallbackMethodExecutor(metadata, _syncMethod, Parent,
                        TargetType, TargetObject, Parameters, Result, funcCache, locker);
                }
                else
                {
                    methodExecutor = new AsyncWaitCallbackMethodExecutor(executor,
                        TargetType, TargetObject, Parameters, Result);
                }

            }
            else if (oldMethodExecutor is not SyncMethodExecutor executor ||
                !executor.IsTheSame(_syncMethod))
            {
                methodExecutor = new SyncMethodExecutor(metadata, _syncMethod, Parent, TargetType,
                    TargetObject, Parameters, Result, funcCache, locker);
            }
            else
            {
                methodExecutor = new SyncMethodExecutor(executor, TargetType,
                    TargetObject, Parameters, Result);
            }

        }
    }

    // returns null MethodInfo on failure
    private static MethodInfo Resolve(Type targetType, string methodName, BindingFlags bindingFlags,
        InheritanceAndParamArrayAwareBinder methodBinder, Type[] parameterTypes, Type[] genericTypeArguments, bool suppressAmbiguityException)
    {
        MethodInfo method;
        try
        {
            methodBinder._SelectMethodCalled = false;
            method = targetType.GetMethod(methodName, bindingFlags,
                methodBinder, CallingConventions.Any, parameterTypes, null);
        }
        catch (AmbiguousMatchException)
        {
            if (suppressAmbiguityException) // For Begin/End methods, ambiguity just means no match
            {
                return null;
            }
            else // For a regular sync method, ambiguity is distinct from no match and gets an explicit error message
            {
                throw;
            }
        }

        if (method != null && !methodBinder._SelectMethodCalled && genericTypeArguments.Length > 0)
        // methodBinder is only used when there's more than one possible match, so method might still be generic
        {
            method = Instantiate(method, genericTypeArguments); // if it fails because of e.g. constraints it will just become null
        }
        return method;
    }

    // returns null on failure instead of throwing an exception (okay because it's an internal method)
    private static MethodInfo Instantiate(MethodInfo method, Type[] genericTypeArguments)
    {
        if (method.ContainsGenericParameters && method.GetGenericArguments().Length == genericTypeArguments.Length)
        {
            try
            {
                // Must be a MethodInfo because we've already filtered out constructors                            
                return method.MakeGenericMethod(genericTypeArguments);
            }
            catch (ArgumentException)
            {
                // Constraint violations will throw this exception--don't add to candidates
                return null;
            }
        }
        else
        {
            return null;
        }
    }


    // Store information about a particular asynchronous method call so we can update out/ref parameters, know 
    // when/what to return, etc.
    private class InvokeMethodInstanceData
    {
        public object TargetObject { get; set; }
        public object[] ActualParameters { get; set; }
        public object ReturnValue { get; set; }
        public bool ExceptionWasThrown { get; set; }
        public Exception Exception { get; set; }
    }

    private class InheritanceAndParamArrayAwareBinder : Binder
    {
        private readonly Type[] _genericTypeArguments;
        private readonly Type _declaringType; // Methods declared directly on this type are preferred, followed by methods on its parents, etc.

        internal bool _SelectMethodCalled; // If this binder is actually used in resolution, it gets to do things like instantiate methods.
                                           // Set this flag to false before calling Type.GetMethod. Check this flag after.



        private readonly Activity _parentActivity; // Used for generating AmbiguousMatchException error message

        public InheritanceAndParamArrayAwareBinder(Type declaringType, Type[] genericTypeArguments, Activity parentActivity)
        {
            _declaringType = declaringType;
            _genericTypeArguments = genericTypeArguments;
            _parentActivity = parentActivity;
        }

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
            => throw FxTrace.Exception.AsError(new NotImplementedException());

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
            => throw FxTrace.Exception.AsError(new NotImplementedException());

        public override object ChangeType(object value, Type type, CultureInfo culture)
            => throw FxTrace.Exception.AsError(new NotImplementedException());

        public override void ReorderArgumentArray(ref object[] args, object state)
            => throw FxTrace.Exception.AsError(new NotImplementedException());

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            MethodBase[] methodCandidates;
            _SelectMethodCalled = true;

            if (_genericTypeArguments.Length > 0)
            {
                // Accept only generic methods which can be successfully instantiated w/ these parameters
                Collection<MethodBase> methods = new();
                foreach (MethodBase method in match)
                {
                    // Must be a MethodInfo because we've already filtered out constructors                            
                    MethodInfo instantiatedMethod = Instantiate((MethodInfo)method, _genericTypeArguments);
                    if (instantiatedMethod != null)
                    {
                        methods.Add(instantiatedMethod);
                    }
                }
                methodCandidates = methods.ToArray();
            }
            else
            {
                // Accept only candidates which are already instantiated
                methodCandidates = match.Where(m => m.ContainsGenericParameters == false).ToArray();
            }

            if (methodCandidates.Length == 0)
            {
                return null;
            }

            // Methods declared on this.declaringType class get top priority as matches
            Type declaringType = _declaringType;
            MethodBase result = null;
            do
            {
                MethodBase[] methodsDeclaredHere = methodCandidates.Where(mb => mb.DeclaringType == declaringType).ToArray();
                if (methodsDeclaredHere.Length > 0)
                {
                    // Try to find a match
                    result = FindMatch(methodsDeclaredHere, bindingAttr, types, modifiers);
                }
                declaringType = declaringType.BaseType;
            }
            while (declaringType != null && result == null); // short-circuit as soon as we find a match

            return result; // returns null if no match found                
        }

        private MethodBase FindMatch(MethodBase[] methodCandidates, BindingFlags bindingAttr, Type[] types, ParameterModifier[] modifiers)
        {
            // Try the default binder first. Never gives false positive, but will fail to detect methods w/ parameter array because
            // it will not expand the formal parameter list when checking against actual parameters.
            MethodBase result = Type.DefaultBinder.SelectMethod(bindingAttr, methodCandidates, types, modifiers);

            // Could be false negative, check for parameter array and if so condense it back to an array before re-checking.
            if (result == null)
            {
                foreach (MethodBase method in methodCandidates)
                {
                    MethodInfo methodInfo = method as MethodInfo;
                    ParameterInfo[] formalParams = methodInfo.GetParameters();
                    if (HaveParameterArray(formalParams)) // Check if the last parameter of method is marked w/ "params" attribute
                    {
                        Type elementType = formalParams[^1].ParameterType.GetElementType();

                        bool allCompatible = true;
                        // There could be more actual parameters than formal parameters, because the formal parameter is a params T'[] for some T'.
                        // So, check that each actual parameter starting at position [formalParams.Length - 1] is compatible with T'.
                        for (int i = formalParams.Length - 1; i < types.Length - 1; i++)
                        {
                            if (!TypeHelper.AreTypesCompatible(types[i], elementType))
                            {
                                allCompatible = false;
                                break;
                            }
                        }

                        if (!allCompatible)
                        {
                            continue;
                        }

                        // Condense the actual parameter back to an array.
                        Type[] typeArray = new Type[formalParams.Length];
                        for (int i = 0; i < typeArray.Length - 1; i++)
                        {
                            typeArray[i] = types[i];
                        }
                        typeArray[^1] = elementType.MakeArrayType();

                        // Recheck the condensed array
                        MethodBase newFound = Type.DefaultBinder.SelectMethod(bindingAttr, new MethodBase[] { methodInfo }, typeArray, modifiers);
                        if (result != null && newFound != null)
                        {
                            string type = newFound.ReflectedType.Name;
                            string name = newFound.Name;
                            string bindingType = bindingAttr == staticBindingFlags ? staticString : instanceString;
                            throw FxTrace.Exception.AsError(new AmbiguousMatchException(SR.DuplicateMethodFound(type, bindingType, name, _parentActivity.DisplayName)));
                        }
                        else
                        {
                            result = newFound;
                        }
                    }
                }
            }
            return result;
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            throw FxTrace.Exception.AsError(new NotImplementedException());
        }
    }

    // Executes method synchronously
    private class SyncMethodExecutor : MethodExecutor
    {
        private readonly MethodInfo _syncMethod;
        private readonly Func<object, object[], object> _func;

        public SyncMethodExecutor(CodeActivityMetadata metadata, MethodInfo syncMethod, Activity invokingActivity,
            Type targetType, InArgument targetObject, Collection<Argument> parameters,
            RuntimeArgument returnObject,
                MruCache<MethodInfo, Func<object, object[], object>> funcCache,
            ReaderWriterLockSlim locker)
            : base(invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            Fx.Assert(syncMethod != null, "Must provide syncMethod");
            _syncMethod = syncMethod;
            _func = MethodCallExpressionHelper.GetFunc(metadata, this._syncMethod, funcCache, locker);
        }

        public SyncMethodExecutor(SyncMethodExecutor copy, Type targetType, InArgument targetObject, Collection<Argument> parameters,
            RuntimeArgument returnObject)
            : base(copy._invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            _syncMethod = copy._syncMethod;
            _func = copy._func;
        }

        public bool IsTheSame(MethodInfo newMethod) => !MethodCallExpressionHelper.NeedRetrieve(newMethod, _syncMethod, _func);

        public override bool MethodIsStatic => _syncMethod.IsStatic;

        protected override IAsyncResult BeginMakeMethodCall(AsyncCodeActivityContext context, object target, AsyncCallback callback, object state)
        {
            object[] actualParameters = EvaluateAndPackParameters(context, _syncMethod, false);

            object result = InvokeAndUnwrapExceptions(_func, target, actualParameters);

            SetOutArgumentAndReturnValue(context, result, actualParameters);

            return new CompletedAsyncResult(callback, state);
        }

        protected override void EndMakeMethodCall(AsyncCodeActivityContext context, IAsyncResult result) => CompletedAsyncResult.End(result);
    }

    // Executes method using paired Begin/End async pattern methods
    private class AsyncPatternMethodExecutor : MethodExecutor
    {
        private readonly MethodInfo _beginMethod;
        private readonly MethodInfo _endMethod;
        private readonly Func<object, object[], object> _beginFunc;
        private readonly Func<object, object[], object> _endFunc;

        public AsyncPatternMethodExecutor(CodeActivityMetadata metadata, MethodInfo beginMethod, MethodInfo endMethod,
            Activity invokingActivity, Type targetType, InArgument targetObject,
            Collection<Argument> parameters, RuntimeArgument returnObject,
                MruCache<MethodInfo, Func<object, object[], object>> funcCache,
            ReaderWriterLockSlim locker)
            : base(invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            Fx.Assert(beginMethod != null && endMethod != null, "Must provide beginMethod and endMethod");
            _beginMethod = beginMethod;
            _endMethod = endMethod;
            _beginFunc = MethodCallExpressionHelper.GetFunc(metadata, beginMethod, funcCache, locker);
            _endFunc = MethodCallExpressionHelper.GetFunc(metadata, endMethod, funcCache, locker);
        }

        public AsyncPatternMethodExecutor(AsyncPatternMethodExecutor copy, Type targetType, InArgument targetObject,
            Collection<Argument> parameters, RuntimeArgument returnObject)
            : base(copy._invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            _beginMethod = copy._beginMethod;
            _endMethod = copy._endMethod;
            _beginFunc = copy._beginFunc;
            _endFunc = copy._endFunc;
        }

        public override bool MethodIsStatic => _beginMethod.IsStatic;

        public bool IsTheSame(MethodInfo newBeginMethod, MethodInfo newEndMethod)
            => !(MethodCallExpressionHelper.NeedRetrieve(newBeginMethod, _beginMethod, _beginFunc)
               || MethodCallExpressionHelper.NeedRetrieve(newEndMethod, _endMethod, _endFunc));

        protected override IAsyncResult BeginMakeMethodCall(AsyncCodeActivityContext context, object target, AsyncCallback callback, object state)
        {
            InvokeMethodInstanceData instance = new()
            {
                TargetObject = target,
                ActualParameters = EvaluateAndPackParameters(context, _beginMethod, true),
            };

            instance.ActualParameters[^2] = callback;
            instance.ActualParameters[^1] = state;
            context.UserState = instance;

            return (IAsyncResult)InvokeAndUnwrapExceptions(_beginFunc, target, instance.ActualParameters);
        }

        protected override void EndMakeMethodCall(AsyncCodeActivityContext context, IAsyncResult result)
        {
            InvokeMethodInstanceData instance = (InvokeMethodInstanceData)context.UserState;
            instance.ReturnValue = InvokeAndUnwrapExceptions(_endFunc, instance.TargetObject, new object[] { result });
            SetOutArgumentAndReturnValue(context, instance.ReturnValue, instance.ActualParameters);
        }
    }

    // Executes method asynchronously on WaitCallback thread.
    private class AsyncWaitCallbackMethodExecutor : MethodExecutor
    {
        private readonly MethodInfo _asyncMethod;
        private readonly Func<object, object[], object> _asyncFunc;

        public AsyncWaitCallbackMethodExecutor(CodeActivityMetadata metadata, MethodInfo asyncMethod, Activity invokingActivity,
            Type targetType, InArgument targetObject, Collection<Argument> parameters,
            RuntimeArgument returnObject,
                MruCache<MethodInfo, Func<object, object[], object>> funcCache,
            ReaderWriterLockSlim locker)
            : base(invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            Fx.Assert(asyncMethod != null, "Must provide asyncMethod");
            _asyncMethod = asyncMethod;
            _asyncFunc = MethodCallExpressionHelper.GetFunc(metadata, asyncMethod, funcCache, locker);
        }


        public AsyncWaitCallbackMethodExecutor(AsyncWaitCallbackMethodExecutor copy, Type targetType, InArgument targetObject,
            Collection<Argument> parameters, RuntimeArgument returnObject) :
            base(copy._invokingActivity, targetType, targetObject, parameters, returnObject)
        {
            _asyncMethod = copy._asyncMethod;
            _asyncFunc = copy._asyncFunc;
        }

        public override bool MethodIsStatic => _asyncMethod.IsStatic;

        public bool IsTheSame(MethodInfo newMethodInfo) => !MethodCallExpressionHelper.NeedRetrieve(newMethodInfo, _asyncMethod, _asyncFunc);

        protected override IAsyncResult BeginMakeMethodCall(AsyncCodeActivityContext context, object target, AsyncCallback callback, object state)
        {
            InvokeMethodInstanceData instance = new()
            {
                TargetObject = target,
                ActualParameters = EvaluateAndPackParameters(context, _asyncMethod, false),
            };
            return new ExecuteAsyncResult(instance, this, callback, state);
        }

        protected override void EndMakeMethodCall(AsyncCodeActivityContext context, IAsyncResult result)
        {
            InvokeMethodInstanceData instance = ExecuteAsyncResult.End(result);
            if (instance.ExceptionWasThrown)
            {
                throw FxTrace.Exception.AsError(instance.Exception);
            }
            else
            {
                SetOutArgumentAndReturnValue(context, instance.ReturnValue, instance.ActualParameters);
            }
        }

        private class ExecuteAsyncResult : AsyncResult
        {
            private static readonly Action<object> asyncExecute = new(AsyncExecute);
            private readonly InvokeMethodInstanceData _instance;
            private readonly AsyncWaitCallbackMethodExecutor _executor;

            public ExecuteAsyncResult(InvokeMethodInstanceData instance, AsyncWaitCallbackMethodExecutor executor, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _instance = instance;
                _executor = executor;
                ActionItem.Schedule(asyncExecute, this);
            }

            public static InvokeMethodInstanceData End(IAsyncResult result)
            {
                ExecuteAsyncResult thisPtr = End<ExecuteAsyncResult>(result);
                return thisPtr._instance;
            }

            private static void AsyncExecute(object state)
            {
                ExecuteAsyncResult thisPtr = (ExecuteAsyncResult)state;
                thisPtr.AsyncExecuteCore();
            }

            private void AsyncExecuteCore()
            {
                try
                {
                    _instance.ReturnValue = _executor.InvokeAndUnwrapExceptions(_executor._asyncFunc, _instance.TargetObject, _instance.ActualParameters);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    _instance.Exception = e;
                    _instance.ExceptionWasThrown = true;
                }
                Complete(false);
            }
        }
    }
}
