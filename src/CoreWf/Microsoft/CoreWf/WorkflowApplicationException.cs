// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    //[Serializable]
    public class WorkflowApplicationException : Exception
    {
        private const string InstanceIdName = "instanceId";
        private Guid _instanceId;

        public WorkflowApplicationException()
            : base(SR.DefaultWorkflowApplicationExceptionMessage)
        {
        }

        public WorkflowApplicationException(string message)
            : base(message)
        {
        }

        public WorkflowApplicationException(string message, Guid instanceId)
            : base(message)
        {
            _instanceId = instanceId;
        }

        public WorkflowApplicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkflowApplicationException(string message, Guid instanceId, Exception innerException)
            : base(message, innerException)
        {
            _instanceId = instanceId;
        }

        //protected WorkflowApplicationException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    this.instanceId = (Guid)info.GetValue(InstanceIdName, typeof(Guid));
        //}

        public Guid InstanceId
        {
            get
            {
                return _instanceId;
            }
        }

        //[Fx.Tag.SecurityNote(Critical = "Critical because we are overriding a critical method in the base class.")]
        //[SecurityCritical]
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    base.GetObjectData(info, context);
        //    info.AddValue(InstanceIdName, this.instanceId);
        //}
    }
}
