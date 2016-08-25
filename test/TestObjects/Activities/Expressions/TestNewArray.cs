// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf;
using Test.Common.TestObjects.Activities.Collections;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestNewArray<TResult> : TestActivity
    {
        private MemberCollection<Argument> _bounds;
        public TestNewArray()
        {
            _bounds = new MemberCollection<Argument>(AddBound);
            this.ProductActivity = new NewArray<TResult>();
        }

        public Variable<TResult> Result
        {
            set
            {
                ((NewArray<TResult>)this.ProductActivity).Result = value;
            }
        }

        public MemberCollection<Argument> Bounds
        {
            get
            {
                return _bounds;
            }
        }

        private void AddBound(Argument item)
        {
            ((NewArray<TResult>)this.ProductActivity).Bounds.Add(item);
        }
    }
}
