// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestSubtract<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestSubtract()
        {
            this.ProductActivity = new Subtract<TLeft, TRight, TResult>() { Checked = false };
        }

        public TestSubtract(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }

        public TRight Right
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }

        public bool Checked
        {
            set
            {
                ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Checked = value;
            }
            get
            {
                return ((Subtract<TLeft, TRight, TResult>)this.ProductActivity).Checked;
            }
        }
    }
}
