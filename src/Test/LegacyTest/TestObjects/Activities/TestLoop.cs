// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestLoop : TestActivity
    {
        private int _hintIterationCount = -1;

        public Outcome ConditionOutcome = Outcome.Completed;

        public int HintIterationCount
        {
            get
            {
                return _hintIterationCount;
            }
            set
            {
                _hintIterationCount = value;
            }
        }

        protected TestActivity body;

        public TestLoop()
        {
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.body != null)
            {
                yield return this.body;
            }
        }
    }
}
