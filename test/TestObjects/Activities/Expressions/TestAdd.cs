// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Microsoft.CoreWf.Expressions;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestAdd<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        public TestAdd()
        {
            this.ProductActivity = new Add<TLeft, TRight, TResult>() { Checked = false };
        }

        public TestAdd(TLeft left, TRight right) : this()
        {
            Left = left;
            Right = right;
        }

        public TLeft Left
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Left = value;
            }
        }

        public Expression<Func<ActivityContext, TLeft>> LeftExpression
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Left = new InArgument<TLeft>(value);
            }
        }

        public TRight Right
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Right = value;
            }
        }

        public Expression<Func<ActivityContext, TRight>> RightExpression
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Right = new InArgument<TRight>(value);
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Result = value;
            }
        }

        public bool Checked
        {
            set
            {
                ((Add<TLeft, TRight, TResult>)this.ProductActivity).Checked = value;
            }
            get
            {
                return ((Add<TLeft, TRight, TResult>)this.ProductActivity).Checked;
            }
        }
    }
}
