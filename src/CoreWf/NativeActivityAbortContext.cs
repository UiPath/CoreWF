// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Activities.Runtime;

    [Fx.Tag.XamlVisible(false)]
    public sealed class NativeActivityAbortContext : ActivityContext
    {
        private readonly Exception reason;

        internal NativeActivityAbortContext(ActivityInstance instance, ActivityExecutor executor, Exception reason)
            : base(instance, executor)
        {
            this.reason = reason;
        }

        public Exception Reason
        {
            get
            {
                ThrowIfDisposed();

                return this.reason;
            }
        }
    }
}
