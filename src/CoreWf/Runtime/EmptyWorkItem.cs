// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System.Runtime.Serialization;


    [DataContract]
    internal class EmptyWorkItem : ActivityExecutionWorkItem
    {
        // Called by the Pool.
        public EmptyWorkItem()
        {
            this.IsPooled = true;

            // Empty doesn't need to be cleared/reinitialized so we set it here
            this.IsEmpty = true;
        }

        public void Initialize(ActivityInstance activityInstance)
        {
            base.Reinitialize(activityInstance);
        }

        protected override void ReleaseToPool(ActivityExecutor executor)
        {
            base.ClearForReuse();

            executor.EmptyWorkItemPool.Release(this);
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
    }
}
