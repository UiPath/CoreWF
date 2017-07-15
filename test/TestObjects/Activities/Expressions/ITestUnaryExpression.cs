// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using CoreWf;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public interface ITestUnaryExpression<TOperand, TResult>
    {
        TOperand Operand { set; }
        Variable<TResult> Result { set; }
    }
}
