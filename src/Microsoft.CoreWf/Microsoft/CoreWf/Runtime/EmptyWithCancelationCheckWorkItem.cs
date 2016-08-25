// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class EmptyWithCancelationCheckWorkItem : ActivityExecutionWorkItem
    {
        private ActivityInstance _completedInstance;

        public EmptyWithCancelationCheckWorkItem(ActivityInstance activityInstance, ActivityInstance completedInstance)
            : base(activityInstance)
        {
            _completedInstance = completedInstance;
            this.IsEmpty = true;
        }

        [DataMember(Name = "completedInstance")]
        internal ActivityInstance SerializedCompletedInstance
        {
            get { return _completedInstance; }
            set { _completedInstance = value; }
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
            if (_completedInstance.State != ActivityInstanceState.Closed && this.ActivityInstance.IsPerformingDefaultCancelation)
            {
                this.ActivityInstance.MarkCanceled();
            }

            base.PostProcess(executor);
        }
    }
}
