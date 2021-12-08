// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class AsyncCodeActivityContext : CodeActivityContext
{
    private readonly AsyncOperationContext _asyncContext;

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
            return CurrentInstance.IsCancellationRequested;
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
        if (!CurrentInstance.IsCancellationRequested && !_asyncContext.IsAborting)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MarkCanceledOnlyCallableIfCancelRequested));
        }

        CurrentInstance.MarkCanceled();
    }
}
