// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public class TestRethrow : TestActivity
    {
        public TestRethrow()
        {
            this.ProductActivity = new Rethrow();
        }
        private Rethrow ProductRethrow
        {
            get { return (Rethrow)this.ProductActivity; }
        }
    }
}
