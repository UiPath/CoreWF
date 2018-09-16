// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace CoreWf.Runtime
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class CollapseTemporaryResolutionLocationWorkItem : WorkItem
    {
        private Location location;

        public CollapseTemporaryResolutionLocationWorkItem(Location location, ActivityInstance instance)
            : base(instance)
        {
            this.location = location;
        }

        public override bool IsValid
        {
            get { return true; }
        }

        public override ActivityInstance PropertyManagerOwner
        {
            get { return null; }
        }

        [DataMember(EmitDefaultValue = false, Name = "location")]
        internal Location SerializedLocation
        {
            get { return this.location; }
            set { this.location = value; }
        }
       
        public override void TraceScheduled()
        {
            TraceRuntimeWorkItemScheduled();
        }

        public override void TraceStarting()
        {
            TraceRuntimeWorkItemStarting();
        }

        public override void TraceCompleted()
        {
            TraceRuntimeWorkItemCompleted();
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            this.location.TemporaryResolutionEnvironment.CollapseTemporaryResolutionLocation(this.location);
            return true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            return;
        }
    }
}
