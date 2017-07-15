// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationAbortedEventArgs : WorkflowApplicationEventArgs
    {
        internal WorkflowApplicationAbortedEventArgs(WorkflowApplication application, Exception reason)
            : base(application)
        {
            this.Reason = reason;
        }

        public Exception Reason
        {
            get;
            private set;
        }
    }
}
