// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Statements;

namespace Test.Common.TestObjects.CustomActivities
{
    public class ActionActivity : Activity
    {
        public ActionActivity()
        {
            base.Implementation = () => new InvokeAction { Action = Action };
        }

        public ActivityAction Action { get; set; }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            // None
        }
    }
}
