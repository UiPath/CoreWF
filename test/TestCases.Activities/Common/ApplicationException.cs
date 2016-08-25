// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace TestCases.Activities.Common
{
    public class ApplicationException : Exception
    {
        public ApplicationException()
        {
        }

        public ApplicationException(string message)
            : base(message)
        {
        }

        public ApplicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ApplicationException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }
}
