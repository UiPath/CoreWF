// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Runtime;
    using System;
    using System.Runtime.Serialization;
    using System.Security;

    [Serializable]
    public class WorkflowApplicationException : Exception
    {
        private const string InstanceIdName = "instanceId";
        private readonly Guid instanceId;

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
            this.instanceId = instanceId;
        }

        public WorkflowApplicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WorkflowApplicationException(string message, Guid instanceId, Exception innerException)
            : base(message, innerException)
        {
            this.instanceId = instanceId;
        }

        protected WorkflowApplicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.instanceId = (Guid)info.GetValue(InstanceIdName, typeof(Guid));
        }

        public Guid InstanceId
        {
            get
            {
                return this.instanceId;
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because we are overriding a critical method in the base class.")]
        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(InstanceIdName, this.instanceId);
        }
    }
}
