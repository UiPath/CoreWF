// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class NativeActivityFaultContext : NativeActivityContext
{
    private bool _isFaultHandled;
    private readonly Exception _exception;
    private readonly ActivityInstanceReference _source;

    internal NativeActivityFaultContext(ActivityInstance executingActivityInstance,
        ActivityExecutor executor, BookmarkManager bookmarkManager, Exception exception, ActivityInstanceReference source)
        : base(executingActivityInstance, executor, bookmarkManager)
    {
        Fx.Assert(exception != null, "There must be an exception.");
        Fx.Assert(source != null, "There must be a source.");

        _exception = exception;
        _source = source;
    }

    internal bool IsFaultHandled => _isFaultHandled;

    public void HandleFault()
    {
        ThrowIfDisposed();

        _isFaultHandled = true;
    }

    internal FaultContext CreateFaultContext()
    {
        Fx.Assert(!IsDisposed, "We must not have been disposed.");

        return new FaultContext(_exception, _source);
    }
}
