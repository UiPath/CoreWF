// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using CoreWf;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public interface ITestBinaryExpression<TLeft, TRight, TResult>
    {
        TLeft Left { set; }
        TRight Right { set; }
        Variable<TResult> Result { set; }
    }
}
