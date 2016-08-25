// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
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
