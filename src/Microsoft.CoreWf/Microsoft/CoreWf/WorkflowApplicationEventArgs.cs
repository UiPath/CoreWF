// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;

namespace Microsoft.CoreWf
{
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
