// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class NativeActivityAbortContext : ActivityContext
{
    private readonly Exception _reason;

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
