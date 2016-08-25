// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
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
