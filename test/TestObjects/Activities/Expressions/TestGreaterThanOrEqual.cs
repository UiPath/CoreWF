// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf;
using Test.Common.TestObjects.Utilities.Validation;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestGreaterThanOrEqual<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestGreaterThanOrEqual()
        {
            this.ProductActivity = new GreaterThanOrEqual<TLeft, TRight, TResult>();
        }

        public TestGreaterThanOrEqual(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((GreaterThanOrEqual<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((GreaterThanOrEqual<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }

        public TRight Right
        {
            set
            {
                ((GreaterThanOrEqual<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((GreaterThanOrEqual<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((GreaterThanOrEqual<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}
