// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestMultiply<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestMultiply()
        {
            this.ProductActivity = new Multiply<TLeft, TRight, TResult>() { Checked = false };
        }

        public TestMultiply(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }

        public TRight Right
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }

        public bool Checked
        {
            set
            {
                ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Checked = value;
            }
            get
            {
                return ((Multiply<TLeft, TRight, TResult>)this.ProductActivity).Checked;
            }
        }
    }
}
