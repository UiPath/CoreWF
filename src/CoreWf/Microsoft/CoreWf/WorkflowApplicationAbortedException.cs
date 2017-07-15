// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    //[Serializable]
    public class WorkflowApplicationAbortedException : WorkflowApplicationException
    {
        public WorkflowApplicationAbortedException()
        {
        }

        public WorkflowApplicationAbortedException(string message)
            : base(message)
        {
        }

        public WorkflowApplicationAbortedException(string message, Guid instanceId)
            : base(message, instanceId)
        {
        }

        public WorkflowApplicationAbortedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkflowApplicationAbortedException(string message, Guid instanceId, Exception innerException)
            : base(message, instanceId, innerException)
        {
        }

        //protected WorkflowApplicationAbortedException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}
    }
}
