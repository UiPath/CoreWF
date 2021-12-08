// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Security;

namespace System.Activities.Runtime;

[DataContract]
internal class DelegateCompletionCallbackWrapper : CompletionCallbackWrapper
{
    private static readonly Type callbackType = typeof(DelegateCompletionCallback);
    private static readonly Type[] callbackParameterTypes = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance), typeof(IDictionary<string, object>) };
    private Dictionary<string, object> _results;

    public DelegateCompletionCallbackWrapper(DelegateCompletionCallback callback, ActivityInstance owningInstance)
        : base(callback, owningInstance)
    {
        NeedsToGatherOutputs = true;
    }

    [DataMember(EmitDefaultValue = false, Name = "results")]
    internal Dictionary<string, object> SerializedResults
    {
        get => _results;
        set => _results = value;
    }

    protected override void GatherOutputs(ActivityInstance completedInstance)
    {
        if (completedInstance.Activity.HandlerOf == null)
        {
            return;
        }

        IList<RuntimeDelegateArgument> runtimeArguments = completedInstance.Activity.HandlerOf.RuntimeDelegateArguments;
        LocationEnvironment environment = completedInstance.Environment;

        for (int i = 0; i < runtimeArguments.Count; i++)
        {
            RuntimeDelegateArgument runtimeArgument = runtimeArguments[i];

            if (runtimeArgument.BoundArgument != null && ArgumentDirectionHelper.IsOut(runtimeArgument.Direction))
            {
                Location parameterLocation = environment.GetSpecificLocation(runtimeArgument.BoundArgument.Id);

                if (parameterLocation != null)
                {
                    _results ??= new Dictionary<string, object>();
                    _results.Add(runtimeArgument.Name, parameterLocation.Value);
                }
            }
        }
    }

    [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
        Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
    [SecuritySafeCritical]
    protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
    {
        EnsureCallback(callbackType, callbackParameterTypes);
        DelegateCompletionCallback completionCallback = (DelegateCompletionCallback)Callback;

        IDictionary<string, object> returnValue = _results;
        returnValue ??= ActivityUtilities.EmptyParameters;
        completionCallback(context, completedInstance, returnValue);
    }

}
