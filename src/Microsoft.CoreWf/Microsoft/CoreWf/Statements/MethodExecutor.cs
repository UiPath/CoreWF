// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Microsoft.CoreWf.Statements
{
    // Inverted Template Method pattern. MethodExecutor is the base class for executing a method; created by MethodResolver.
    // Private concrete implementations are created by MethodResolver, but this is the "public" API used by InvokeMethod.
    internal abstract class MethodExecutor
    {
        // Used for creating tracing messages w/ DisplayName
        protected Activity invokingActivity;

        // We may still need to know targetType if we're autocreating targets during ExecuteMethod
        private Type _targetType;
        private InArgument _targetObject;
        private Collection<Argument> _parameters;
        private RuntimeArgument _returnObject;

        public MethodExecutor(Activity invokingActivity, Type targetType, InArgument targetObject,
            Collection<Argument> parameters, RuntimeArgument returnObject)
        {
            Fx.Assert(invokingActivity != null, "Must provide invokingActivity");
            Fx.Assert(targetType != null || (targetObject != null), "Must provide targetType or targetObject");
            Fx.Assert(parameters != null, "Must provide parameters");
            // returnObject is optional 

            this.invokingActivity = invokingActivity;
            _targetType = targetType;
            _targetObject = targetObject;
            _parameters = parameters;
            _returnObject = returnObject;
        }

        public abstract bool MethodIsStatic { get; }

        protected abstract IAsyncResult BeginMakeMethodCall(AsyncCodeActivityContext context, object target, AsyncCallback callback, object state);
        protected abstract void EndMakeMethodCall(AsyncCodeActivityContext context, IAsyncResult result);

        private static bool HaveParameterArray(ParameterInfo[] parameters)
        {
            if (parameters.Length > 0)
            {
                ParameterInfo last = parameters[parameters.Length - 1];
                return last.GetCustomAttributes(typeof(ParamArrayAttribute), true).FirstOrDefault() != null;
            }
            else
            {
                return false;
            }
        }

        protected object[] EvaluateAndPackParameters(CodeActivityContext context, MethodInfo method,
            bool usingAsyncPattern)
        {
            ParameterInfo[] formalParameters = method.GetParameters();
            int formalParamCount = formalParameters.Length;
            object[] actualParameters = new object[formalParamCount];

            if (usingAsyncPattern)
            {
                formalParamCount -= 2;
            }

            bool haveParameterArray = HaveParameterArray(formalParameters);
            for (int i = 0; i < formalParamCount; i++)
            {
                if (i == formalParamCount - 1 && !usingAsyncPattern && haveParameterArray)
                {
                    int paramArrayCount = _parameters.Count - formalParamCount + 1;

                    // If params are given explicitly, that's okay.
                    if (paramArrayCount == 1 && TypeHelper.AreTypesCompatible(_parameters[i].ArgumentType,
                        formalParameters[i].ParameterType))
                    {
                        actualParameters[i] = _parameters[i].Get<object>(context);
                    }
                    else
                    {
                        // Otherwise, pack them into an array for the reflection call.
                        actualParameters[i] =
                            Activator.CreateInstance(formalParameters[i].ParameterType, paramArrayCount);
                        for (int j = 0; j < paramArrayCount; j++)
                        {
                            ((object[])actualParameters[i])[j] = _parameters[i + j].Get<object>(context);
                        }
                    }
                    continue;
                }
                actualParameters[i] = _parameters[i].Get<object>(context);
            }

            return actualParameters;
        }

        //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.InstantiateArgumentExceptionsCorrectly, //Justification = "TargetObject is a parameter to InvokeMethod, rather than this specific method.")]
        public IAsyncResult BeginExecuteMethod(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            object targetInstance = null;

            if (!this.MethodIsStatic)
            {
                targetInstance = _targetObject.Get(context);
                if (targetInstance == null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("TargetObject");
                }
            }

            return BeginMakeMethodCall(context, targetInstance, callback, state); // defer to concrete instance for sync/async variations
        }

        public void EndExecuteMethod(AsyncCodeActivityContext context, IAsyncResult result)
        {
            EndMakeMethodCall(context, result); // defer to concrete instance for sync/async variations
        }

        //[SuppressMessage("Reliability", "Reliability108:IsFatalRule",
        //Justification = "We need throw out all exceptions from method invocation.")]
        internal object InvokeAndUnwrapExceptions(Func<object, object[], object> func, object targetInstance, object[] actualParameters)
        {
            try
            {
                return func(targetInstance, actualParameters);
            }
            catch (Exception e)
            {
                if (TD.InvokedMethodThrewExceptionIsEnabled())
                {
                    TD.InvokedMethodThrewException(this.invokingActivity.DisplayName, e.ToString());
                }
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(e);
            }
        }

        public void SetOutArgumentAndReturnValue(ActivityContext context, object state, object[] actualParameters)
        {
            for (int index = 0; index < _parameters.Count; index++)
            {
                if (_parameters[index].Direction != ArgumentDirection.In)
                {
                    _parameters[index].Set(context, actualParameters[index]);
                }
            }

            if (_returnObject != null)
            {
                _returnObject.Set(context, state);
            }
        }

        public void Trace(Activity parent)
        {
            if (this.MethodIsStatic)
            {
                if (TD.InvokeMethodIsStaticIsEnabled())
                {
                    TD.InvokeMethodIsStatic(parent.DisplayName);
                }
            }
            else
            {
                if (TD.InvokeMethodIsNotStaticIsEnabled())
                {
                    TD.InvokeMethodIsNotStatic(parent.DisplayName);
                }
            }
        }
    }
}
