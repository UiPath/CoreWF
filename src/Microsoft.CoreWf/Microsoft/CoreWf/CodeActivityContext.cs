// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Tracking;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public class CodeActivityContext : ActivityContext
    {
        // This is called by the Pool.
        internal CodeActivityContext()
        {
        }

        // This is only used by base classes which do not take
        // part in pooling.
        internal CodeActivityContext(ActivityInstance instance, ActivityExecutor executor)
            : base(instance, executor)
        {
        }

        internal void Initialize(ActivityInstance instance, ActivityExecutor executor)
        {
            base.Reinitialize(instance, executor);
        }

        public THandle GetProperty<THandle>() where THandle : Handle
        {
            ThrowIfDisposed();
            if (this.CurrentInstance.PropertyManager != null)
            {
                return (THandle)this.CurrentInstance.PropertyManager.GetProperty(Handle.GetPropertyName(typeof(THandle)), this.Activity.MemberOf);
            }
            else
            {
                return null;
            }
        }

        public void Track(CustomTrackingRecord record)
        {
            ThrowIfDisposed();

            if (record == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("record");
            }

            base.TrackCore(record);
        }
    }
}
