// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal abstract class ActivityExecutionWorkItem : WorkItem
    {
        private bool skipActivityInstanceAbort;

        // Used by subclasses in the pooled case
        protected ActivityExecutionWorkItem()
        {
        }

        public ActivityExecutionWorkItem(ActivityInstance activityInstance)
            : base(activityInstance)
        {
        }

        public override bool IsValid
        {
            get
            {
                return this.ActivityInstance.State == ActivityInstanceState.Executing;
            }
        }

        public override ActivityInstance PropertyManagerOwner
        {
            get
            {
                return this.ActivityInstance;
            }
        }

        protected override void ClearForReuse()
        {
            base.ClearForReuse();
            this.skipActivityInstanceAbort = false;
        }

        protected void SetExceptionToPropagateWithoutAbort(Exception exception)
        {
            this.ExceptionToPropagate = exception;
            this.skipActivityInstanceAbort = true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            if (this.ExceptionToPropagate != null && !skipActivityInstanceAbort)
            {
                executor.AbortActivityInstance(this.ActivityInstance, this.ExceptionToPropagate);
            }
            else if (this.ActivityInstance.UpdateState(executor))
            {
                // NOTE: exceptionToPropagate could be non-null here if this is a Fault work item.
                // That means that the next line could potentially overwrite the exception with a
                // new exception.
                Exception newException = executor.CompleteActivityInstance(this.ActivityInstance);

                if (newException != null)
                {
                    this.ExceptionToPropagate = newException;
                }
            }
        }
    }
}
