// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal abstract class ActivityExecutionWorkItem : WorkItem
    {
        private bool _skipActivityInstanceAbort;

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
            _skipActivityInstanceAbort = false;
        }

        protected void SetExceptionToPropagateWithoutAbort(Exception exception)
        {
            this.ExceptionToPropagate = exception;
            _skipActivityInstanceAbort = true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            if (this.ExceptionToPropagate != null && !_skipActivityInstanceAbort)
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
