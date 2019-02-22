// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Runtime;
    using System;
    using System.Collections.Generic;

    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationEventArgs : EventArgs
    {
        internal WorkflowApplicationEventArgs(WorkflowApplication application)
        {
            this.Owner = application;
        }

        internal WorkflowApplication Owner
        {
            get;
            private set;
        }

        public Guid InstanceId
        {
            get
            {
                return this.Owner.Id;
            }
        }

        public IEnumerable<T> GetInstanceExtensions<T>()
            where T : class
        {
            return this.Owner.InternalGetExtensions<T>();
        }
    }
}
