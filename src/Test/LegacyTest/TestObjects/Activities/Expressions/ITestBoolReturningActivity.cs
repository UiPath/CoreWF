// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public interface ITestBoolReturningActivity
    {
        Variable<bool> Result { set; }
    }
}
