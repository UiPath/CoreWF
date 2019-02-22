// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System;

    [Fx.Tag.XamlVisible(false)]
    public sealed class AsyncCodeActivityContext : CodeActivityContext
    {
        private readonly AsyncOperationContext asyncContext;

        internal AsyncCodeActivityContext(AsyncOperationContext asyncContext, ActivityInstance instance, ActivityExecutor executor)
            : base(instance, executor)
        {
            this.asyncContext = asyncContext;
        }

        public bool IsCancellationRequested
        {
            get
            {
                ThrowIfDisposed();
                return this.CurrentInstance.IsCancellationRequested;
            }
        }

        public object UserState
        {
            get
            {
                ThrowIfDisposed();
                return this.asyncContext.UserState;
            }
            set
            {
                ThrowIfDisposed();
                this.asyncContext.UserState = value;
            }
        }

        public void MarkCanceled()
        {
            ThrowIfDisposed();

            // This is valid to be called while aborting or while canceling
            if (!this.CurrentInstance.IsCancellationRequested && !this.asyncContext.IsAborting)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MarkCanceledOnlyCallableIfCancelRequested));
            }

            this.CurrentInstance.MarkCanceled();
        }
    }
}
