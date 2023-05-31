// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;
using Tracking;

[Fx.Tag.XamlVisible(false)]
public class CodeActivityContext : ActivityContext
{
    // This is called by the Pool.
    internal CodeActivityContext() { }

    // This is only used by base classes which do not take
    // part in pooling.
    internal CodeActivityContext(ActivityInstance instance, ActivityExecutor executor)
        : base(instance, executor) { }

    internal void Initialize(ActivityInstance instance, ActivityExecutor executor) => Reinitialize(instance, executor);

    public THandle GetProperty<THandle>() where THandle : Handle
    {
        ThrowIfDisposed();
        return (THandle)CurrentInstance.PropertyManager?.GetProperty(Handle.GetPropertyName(typeof(THandle)), Activity.MemberOf);
    }

    public void Track(CustomTrackingRecord record)
    {
        ThrowIfDisposed();

        if (record == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(record));
        }

        TrackCore(record);
    }

}
