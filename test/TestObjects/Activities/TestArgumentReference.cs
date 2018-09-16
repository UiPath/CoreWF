// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Expressions;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public class TestArgumentReference<T> : TestActivity
    {
        public TestArgumentReference(string argumentName)
        {
            this.ProductActivity = new ArgumentReference<T> { ArgumentName = argumentName };
            this.ExpectedOutcome = Outcome.None;
        }
    }
}
