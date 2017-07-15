// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using CoreWf;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public interface ITestBoolReturningActivity
    {
        Variable<bool> Result { set; }
    }
}
