// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace TestCases.Activities.Common
{
    public class TestCaseException : Exception
    {
        public TestCaseException()
        {
        }

        public TestCaseException(string message)
            : base(message)
        {
        }

        public TestCaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public TestCaseException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }
}
