// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace TestCases.Activities.Common
{
    public class CustomException : Exception
    {
        public CustomException()
            : base()
        {
        }

        public CustomException(string message)
            : base(message)
        {
        }

        public string Name
        {
            get;
            set;
        }
    }
}
