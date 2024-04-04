// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    public sealed class ImportedChildrenWrapperActivity : NativeActivity
    {
        public ImportedChildrenWrapperActivity()
        {
        }

        public ImportedChildrenWrapperActivity(string displayName)
        {
            this.DisplayName = displayName;
        }

        private List<Activity> _importedChildren;
        public List<Activity> ImportedChildren
        {
            get
            {
                if (_importedChildren == null)
                    _importedChildren = new List<Activity>();

                return _importedChildren;
            }
        }

        public Activity Body { get; set; }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            foreach (Activity act in this.ImportedChildren)
            {
                metadata.AddImportedChild(act);
            }

            if (this.Body != null)
            {
                metadata.AddImplementationChild(this.Body);
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.ScheduleActivity(this.Body);
        }

        // protected override void OnCreateDynamicUpdateMap(System.Activities.DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        // {
        //     metadata.AllowUpdateInsideThisActivity();
        // }
    }
}
