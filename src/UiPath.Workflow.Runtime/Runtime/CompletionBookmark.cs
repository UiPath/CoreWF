// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
public class CompletionBookmark
{
    private CompletionCallbackWrapper _callbackWrapper;

    public CompletionBookmark()
    {
        // Called when we want to use the special completion callback
    }

    public CompletionBookmark(CompletionCallbackWrapper callbackWrapper)
    {
        _callbackWrapper = callbackWrapper;
    }

    [DataMember(EmitDefaultValue = false, Name = "callbackWrapper")]
    public CompletionCallbackWrapper SerializedCallbackWrapper
    {
        get => _callbackWrapper;
        set => _callbackWrapper = value;
    }

    public void CheckForCancelation()
    {
        Fx.Assert(_callbackWrapper != null, "We must have a callback wrapper if we are calling this.");
        _callbackWrapper.CheckForCancelation();
    }

    internal WorkItem GenerateWorkItem(ActivityInstance completedInstance, ActivityExecutor executor)
    {
        if (_callbackWrapper != null)
        {
            return _callbackWrapper.CreateWorkItem(completedInstance, executor);
        }
        else
        {
            // Variable defaults and argument expressions always have a parent
            // and never have a CompletionBookmark
            if (completedInstance.State != ActivityInstanceState.Closed && completedInstance.Parent.HasNotExecuted)
            {
                completedInstance.Parent.SetInitializationIncomplete();
            }

            return new EmptyWithCancelationCheckWorkItem(completedInstance.Parent, completedInstance);
        }
    }
}
