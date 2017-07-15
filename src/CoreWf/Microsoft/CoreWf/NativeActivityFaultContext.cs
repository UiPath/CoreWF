// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class NativeActivityFaultContext : NativeActivityContext
    {
        private bool _isFaultHandled;
        private Exception _exception;
        private ActivityInstanceReference _source;

        internal NativeActivityFaultContext(ActivityInstance executingActivityInstance,
            ActivityExecutor executor, BookmarkManager bookmarkManager, Exception exception, ActivityInstanceReference source)
            : base(executingActivityInstance, executor, bookmarkManager)
        {
            Fx.Assert(exception != null, "There must be an exception.");
            Fx.Assert(source != null, "There must be a source.");

            _exception = exception;
            _source = source;
        }

        internal bool IsFaultHandled
        {
            get
            {
                return _isFaultHandled;
            }
        }

        public void HandleFault()
        {
            ThrowIfDisposed();

            _isFaultHandled = true;
        }

        internal FaultContext CreateFaultContext()
        {
            Fx.Assert(!this.IsDisposed, "We must not have been disposed.");

            return new FaultContext(_exception, _source);
        }
    }
}
