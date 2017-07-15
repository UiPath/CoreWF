// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.CustomActivities;

namespace Test.Common.TestObjects.Activities
{
    public class TestResultScope<TResult> : TestActivity
    {
        private TestActivity _body;

        public TestResultScope()
        {
            this.ProductActivity = new ResultScope<TResult>();
        }

        public TestResultScope(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        private ResultScope<TResult> ProductResultScope
        {
            get { return (ResultScope<TResult>)this.ProductActivity; }
        }

        public Variable<TResult> ResultVariable
        {
            get { return this.ProductResultScope.ResultVariable; }
            set { this.ProductResultScope.ResultVariable = value; }
        }

        public TestActivity Body
        {
            get { return _body; }
            set
            {
                _body = value;
                this.ProductResultScope.Body = (value == null) ? null : value.ProductActivity;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            yield return this.Body;
        }
    }
}
