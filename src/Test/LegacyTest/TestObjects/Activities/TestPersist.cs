// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestPersist : TestActivity
    {
        public TestPersist()
        {
            this.ProductActivity = new Persist();
        }

        public TestPersist(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        private TestPersist(Persist productPersist)
        {
            this.ProductActivity = productPersist;
        }

        public static TestPersist CreateFromProduct(Persist persist)
        {
            return new TestPersist(persist);
        }

        protected override void GetActivitySpecificTrace(LegacyTest.Test.Common.TestObjects.Utilities.Validation.TraceGroup traceGroup)
        {
            if (this.ExpectedOutcome.DefaultPropogationState == OutcomeState.Completed)
            {
                TrackPersistence(traceGroup);
            }
        }
    }
}
