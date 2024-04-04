// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestVariableReference<T> : TestActivity
    {
        public TestVariableReference()
        {
            this.ProductActivity = new VariableReference<T>();
            this.ExpectedOutcome = Outcome.None;
        }

        public Variable Variable
        {
            set
            {
                ((VariableReference<T>)this.ProductActivity).Variable = value;
            }
        }
    }
}
