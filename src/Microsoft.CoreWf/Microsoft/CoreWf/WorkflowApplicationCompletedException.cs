// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CoreWf
{
    //[Serializable]
    public class WorkflowApplicationCompletedException : WorkflowApplicationException
    {
        public WorkflowApplicationCompletedException()
        {
        }

        public WorkflowApplicationCompletedException(string message)
            : base(message)
        {
        }

        public WorkflowApplicationCompletedException(string message, Guid instanceId)
            : base(message, instanceId)
        {
        }

        public WorkflowApplicationCompletedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkflowApplicationCompletedException(string message, Guid instanceId, Exception innerException)
            : base(message, instanceId, innerException)
        {
        }

        //protected WorkflowApplicationCompletedException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}
    }
}
