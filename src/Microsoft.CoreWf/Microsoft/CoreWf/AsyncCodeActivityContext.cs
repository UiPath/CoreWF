// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class AsyncCodeActivityContext : CodeActivityContext
    {
        private AsyncOperationContext _asyncContext;

        internal AsyncCodeActivityContext(AsyncOperationContext asyncContext, ActivityInstance instance, ActivityExecutor executor)
            : base(instance, executor)
        {
            _asyncContext = asyncContext;
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
                return _asyncContext.UserState;
            }
            set
            {
                ThrowIfDisposed();
                _asyncContext.UserState = value;
            }
        }

        public void MarkCanceled()
        {
            ThrowIfDisposed();

            // This is valid to be called while aborting or while canceling
            if (!this.CurrentInstance.IsCancellationRequested && !_asyncContext.IsAborting)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.MarkCanceledOnlyCallableIfCancelRequested));
            }

            this.CurrentInstance.MarkCanceled();
        }
    }
}
