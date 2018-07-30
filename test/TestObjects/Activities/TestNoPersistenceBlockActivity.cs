// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestNoPersistenceBlockActivity : TestCustomSequenceBase
    {
        public TestNoPersistenceBlockActivity()
        {
            this.ProductActivity = new NoPersistenceBlockActivity();
        }

        public TestNoPersistenceBlockActivity(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        private NoPersistenceBlockActivity ProductNoPersistenceBlockActivity
        {
            get { return (NoPersistenceBlockActivity)this.ProductActivity; }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // Due to how the activities are scheduled from inside the Activity, these traces appear first
            traceGroup.Steps.Add(new UserTrace(NoPersistenceBlockActivity.NoPersistenceBlockEntered));
            traceGroup.Steps.Add(new UserTrace(NoPersistenceBlockActivity.NoPersistenceBlockExited));

            base.GetActivitySpecificTrace(traceGroup);
        }
    }
}
