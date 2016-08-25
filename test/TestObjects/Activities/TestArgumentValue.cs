// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
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
