// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System;
    using CoreWf.Runtime;

    [Fx.Tag.XamlVisible(false)]
    public sealed class NativeActivityFaultContext : NativeActivityContext
    {
        private bool isFaultHandled;
        private readonly Exception exception;
        private readonly ActivityInstanceReference source;

        internal NativeActivityFaultContext(ActivityInstance executingActivityInstance,
            ActivityExecutor executor, BookmarkManager bookmarkManager, Exception exception, ActivityInstanceReference source)
            : base(executingActivityInstance, executor, bookmarkManager)
        {
            Fx.Assert(exception != null, "There must be an exception.");
            Fx.Assert(source != null, "There must be a source.");

            this.exception = exception;
            this.source = source;
        }

        internal bool IsFaultHandled
        {
            get
            {
                return this.isFaultHandled;
            }
        }

        public void HandleFault()
        {
            ThrowIfDisposed();

            this.isFaultHandled = true;
        }

        internal FaultContext CreateFaultContext()
        {
            Fx.Assert(!this.IsDisposed, "We must not have been disposed.");

            return new FaultContext(this.exception, this.source);
        }
    }
}
