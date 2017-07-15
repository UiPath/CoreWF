// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    //[Serializable]
    public class WorkflowApplicationUnloadedException : WorkflowApplicationException
    {
        public WorkflowApplicationUnloadedException()
        {
        }

        public WorkflowApplicationUnloadedException(string message)
            : base(message)
        {
        }

        public WorkflowApplicationUnloadedException(string message, Guid instanceId)
            : base(message, instanceId)
        {
        }

        public WorkflowApplicationUnloadedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkflowApplicationUnloadedException(string message, Guid instanceId, Exception innerException)
            : base(message, instanceId, innerException)
        {
        }

        //protected WorkflowApplicationUnloadedException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}
    }
}
