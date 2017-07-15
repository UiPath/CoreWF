// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    //[Serializable]
    public class ValidationException : Exception
    {
        public ValidationException()
        {
        }

        public ValidationException(string message)
            : base(message)
        {
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        //protected ValidationException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}
    }
}
