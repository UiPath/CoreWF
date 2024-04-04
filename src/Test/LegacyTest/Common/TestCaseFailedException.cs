// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace LegacyLegacyTest.Test.Common
{
    public class TestCaseFailedException : TestCaseException
    {
        public TestCaseFailedException()
        {
        }

        public TestCaseFailedException(string message)
            : base(message)
        {
        }

        public TestCaseFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public TestCaseFailedException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }
}
