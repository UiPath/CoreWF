// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidWorkflowException : Exception
    {
        public InvalidWorkflowException()
            : base(SR.DefaultInvalidWorkflowExceptionMessage)
        {
        }

        public InvalidWorkflowException(string message)
            : base(message)
        {
        }

        public InvalidWorkflowException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidWorkflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
