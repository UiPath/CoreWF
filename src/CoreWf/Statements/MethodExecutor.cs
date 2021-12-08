// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Activities.Statements;

// Inverted Template Method pattern. MethodExecutor is the base class for executing a method; created by MethodResolver.
// Private concrete implementations are created by MethodResolver, but this is the "public" API used by InvokeMethod.
internal abstract class MethodExecutor
{
    // Used for creating tracing messages w/ DisplayName
    protected Activity _invokingActivity;

    // We may still need to know targetType if we're autocreating targets during ExecuteMethod
    private readonly InArgument _targetObject;
    private readonly Collection<Argument> _parameters;
    private readonly RuntimeArgument _returnObject;

    public MethodExecutor(Activity invokingActivity, Type targetType, InArgument targetObject,
        Collection<Argument> parameters, RuntimeArgument returnObject)
    {
        Fx.Assert(invokingActivity != null, "Must provide invokingActivity");
        Fx.Assert(targetType != null || (targetObject != null), "Must provide targetType or targetObject");
        Fx.Assert(parameters != null, "Must provide parameters");

        _invokingActivity = invokingActivity;
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
            ParameterInfo last = parameters[^1];
            return last.GetCustomAttributes(typeof(ParamArrayAttribute), true).GetLength(0) > 0;
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

    //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.InstantiateArgumentExceptionsCorrectly, Justification = "TargetObject is a parameter to InvokeMethod, rather than this specific method.")]
    public IAsyncResult BeginExecuteMethod(AsyncCodeActivityContext context, AsyncCallback callback, object state)
    {
        object targetInstance = null;

        if (!MethodIsStatic)
        {
            targetInstance = _targetObject.Get(context);
            if (targetInstance == null)
            {
                throw FxTrace.Exception.ArgumentNull("TargetObject");
            }
        }

        return BeginMakeMethodCall(context, targetInstance, callback, state); // defer to concrete instance for sync/async variations
    }

    public void EndExecuteMethod(AsyncCodeActivityContext context, IAsyncResult result) => EndMakeMethodCall(context, result); // defer to concrete instance for sync/async variations

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
                TD.InvokedMethodThrewException(_invokingActivity.DisplayName, e.ToString());
            }
            throw FxTrace.Exception.AsError(e);
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

        _returnObject?.Set(context, state);
    }

    public void Trace(Activity parent)
    {
        if (MethodIsStatic)
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
