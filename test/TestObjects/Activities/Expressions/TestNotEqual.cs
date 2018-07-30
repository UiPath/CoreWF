// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf.Expressions;
using CoreWf;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestNotEqual<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestNotEqual()
        {
            this.ProductActivity = new NotEqual<TLeft, TRight, TResult>();
        }

        public TestNotEqual(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((NotEqual<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((NotEqual<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }


        public TRight Right
        {
            set
            {
                ((NotEqual<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((NotEqual<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((NotEqual<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}
