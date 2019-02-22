// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class EmptyWithCancelationCheckWorkItem : ActivityExecutionWorkItem
    {
        private ActivityInstance completedInstance;

        public EmptyWithCancelationCheckWorkItem(ActivityInstance activityInstance, ActivityInstance completedInstance)
            : base(activityInstance)
        {
            this.completedInstance = completedInstance;
            this.IsEmpty = true;
        }

        [DataMember(Name = "completedInstance")]
        internal ActivityInstance SerializedCompletedInstance
        {
            get { return this.completedInstance; }
            set { this.completedInstance = value; }
        }

        public override void TraceCompleted()
        {
            TraceRuntimeWorkItemCompleted();
        }

        public override void TraceScheduled()
        {
            TraceRuntimeWorkItemScheduled();
        }

        public override void TraceStarting()
        {
            TraceRuntimeWorkItemStarting();
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            Fx.Assert("Empty work items should never been executed.");

            return true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            if (this.completedInstance.State != ActivityInstanceState.Closed && this.ActivityInstance.IsPerformingDefaultCancelation)
            {
                this.ActivityInstance.MarkCanceled();
            }

            base.PostProcess(executor);
        }
    }
}
