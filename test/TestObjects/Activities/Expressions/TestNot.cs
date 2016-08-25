// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestNot<TOperand, TResult> : TestActivity, ITestUnaryExpression<TOperand, TResult>
    {
        public TestNot()
        {
            this.ProductActivity = new Not<TOperand, TResult>();
        }

        public Variable<TResult> Result
        {
            set
            {
                ((Not<TOperand, TResult>)this.ProductActivity).Result = new OutArgument<TResult>(value);
            }
        }

        public TOperand Operand
        {
            set
            {
                ((Not<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((Not<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }
    }
}
