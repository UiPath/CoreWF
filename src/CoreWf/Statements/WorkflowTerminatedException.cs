// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System;
    using System.Runtime.Serialization;
    
    [Serializable]
    public class WorkflowTerminatedException : Exception
    {
        public WorkflowTerminatedException()
            : base(SR.WorkflowTerminatedExceptionDefaultMessage)
        {
        }

        public WorkflowTerminatedException(string message)
            : base(message)
        {
        }

        public WorkflowTerminatedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WorkflowTerminatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
