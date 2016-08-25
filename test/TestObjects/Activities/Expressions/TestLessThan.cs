// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestLessThan<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestLessThan()
        {
            this.ProductActivity = new LessThan<TLeft, TRight, TResult>();
        }

        public TestLessThan(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((LessThan<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((LessThan<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }

        public TRight Right
        {
            set
            {
                ((LessThan<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((LessThan<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((LessThan<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}
