// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public class TestVariableValue<T> : TestActivity
    {
        public TestVariableValue()
        {
            this.ProductActivity = new VariableValue<T>();
            this.ExpectedOutcome = Outcome.None;
        }

        public Variable Variable
        {
            set
            {
                ((VariableValue<T>)this.ProductActivity).Variable = value;
            }
        }
    }
}
