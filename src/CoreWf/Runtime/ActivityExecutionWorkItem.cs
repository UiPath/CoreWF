// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
internal abstract class ActivityExecutionWorkItem : WorkItem
{
    private bool _skipActivityInstanceAbort;

    // Used by subclasses in the pooled case
    protected ActivityExecutionWorkItem() { }

    public ActivityExecutionWorkItem(ActivityInstance activityInstance)
        : base(activityInstance) { }

    public override bool IsValid => ActivityInstance.State == ActivityInstanceState.Executing;

    public override ActivityInstance PropertyManagerOwner => ActivityInstance;

    protected override void ClearForReuse()
    {
        base.ClearForReuse();
        _skipActivityInstanceAbort = false;
    }

    protected void SetExceptionToPropagateWithoutAbort(Exception exception)
    {
        ExceptionToPropagate = exception;
        _skipActivityInstanceAbort = true;
    }

    public override void PostProcess(ActivityExecutor executor)
    {
        if (ExceptionToPropagate != null && !_skipActivityInstanceAbort)
        {
            executor.AbortActivityInstance(ActivityInstance, ExceptionToPropagate);
        }
        else if (ActivityInstance.UpdateState(executor))
        {
            // NOTE: exceptionToPropagate could be non-null here if this is a Fault work item.
            // That means that the next line could potentially overwrite the exception with a
            // new exception.
            Exception newException = executor.CompleteActivityInstance(ActivityInstance);

            if (newException != null)
            {
                ExceptionToPropagate = newException;
            }
        }
    }
}
