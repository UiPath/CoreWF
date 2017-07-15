// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class NativeActivityAbortContext : ActivityContext
    {
        private Exception _reason;

        internal NativeActivityAbortContext(ActivityInstance instance, ActivityExecutor executor, Exception reason)
            : base(instance, executor)
        {
            _reason = reason;
        }

        public Exception Reason
        {
            get
            {
                ThrowIfDisposed();

                return _reason;
            }
        }
    }
}
