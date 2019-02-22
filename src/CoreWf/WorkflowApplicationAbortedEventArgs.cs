// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Activities.Runtime;

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
