// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CoreWf.Statements
{
    //[Serializable]
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

        //protected WorkflowTerminatedException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}
    }
}
