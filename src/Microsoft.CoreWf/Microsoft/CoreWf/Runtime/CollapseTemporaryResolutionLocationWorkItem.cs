// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class CollapseTemporaryResolutionLocationWorkItem : WorkItem
    {
        private Location _location;

        public CollapseTemporaryResolutionLocationWorkItem(Location location, ActivityInstance instance)
            : base(instance)
        {
            _location = location;
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
            get { return _location; }
            set { _location = value; }
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
            _location.TemporaryResolutionEnvironment.CollapseTemporaryResolutionLocation(_location);
            return true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            return;
        }
    }
}
