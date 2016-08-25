// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestCast<TOperand, TResult> : TestActivity, ITestUnaryExpression<TOperand, TResult>

    {
        public TestCast()
        {
            this.ProductActivity = new Cast<TOperand, TResult>();
        }

        public TOperand Operand
        {
            set
            {
                ((Cast<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Variable<TOperand> OperandVariable
        {
            set
            {
                ((Cast<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((Cast<TOperand, TResult>)this.ProductActivity).Result = new OutArgument<TResult>(value);
            }
        }

        public bool Checked
        {
            set
            {
                ((Cast<TOperand, TResult>)this.ProductActivity).Checked = value;
            }
            get
            {
                return ((Cast<TOperand, TResult>)this.ProductActivity).Checked;
            }
        }
    }
}
