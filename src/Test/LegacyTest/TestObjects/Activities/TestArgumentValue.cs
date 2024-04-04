// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestArgumentValue<T> : TestActivity
    {
        public TestArgumentValue(string argumentName)
        {
            this.ProductActivity = new ArgumentValue<T> { ArgumentName = argumentName };
            this.ExpectedOutcome = Outcome.None;
        }
    }
}
