// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;

namespace System.Activities.Debugger;

internal static class UnitTestUtility
{
    internal static Func<string, Exception> AssertionExceptionFactory { get; set; }

    internal static void TestInitialize(Func<string, Exception> createAssertionException)
    {
        AssertionExceptionFactory = createAssertionException;
    }

    internal static void TestCleanup()
    {
        AssertionExceptionFactory = null;
    }

    internal static void Assert(bool condition, string assertionMessage)
    {
        if (AssertionExceptionFactory != null)
        {
            if (!condition)
            {
                throw FxTrace.Exception.AsError(AssertionExceptionFactory(assertionMessage));
            }
        }
        else
        {
            Fx.Assert(condition, assertionMessage);
        }
    }
}
