// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities;
using Hosting;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class WorkflowInvoker
{
    private static AsyncCallback cancelCallback;
    private static AsyncCallback invokeCallback;
    private WorkflowInstanceExtensionManager _extensions;
    private Dictionary<object, AsyncInvokeContext> _pendingInvokes;
    private SendOrPostCallback _raiseInvokeCompletedCallback;
    private readonly object _thisLock;
    private readonly Activity _workflow;

    public WorkflowInvoker(Activity workflow)
    {
        _workflow = workflow ?? throw FxTrace.Exception.ArgumentNull(nameof(workflow));
        _thisLock = new object();
    }

    public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;

    public WorkflowInstanceExtensionManager Extensions
    {
        get
        {
            _extensions ??= new WorkflowInstanceExtensionManager();
            return _extensions;
        }
    }

    private Dictionary<object, AsyncInvokeContext> PendingInvokes
    {
        get
        {
            _pendingInvokes ??= new Dictionary<object, AsyncInvokeContext>();
            return _pendingInvokes;
        }
    }

    private SendOrPostCallback RaiseInvokeCompletedCallback
    {
        get
        {
            _raiseInvokeCompletedCallback ??=
                Fx.ThunkCallback(new SendOrPostCallback(RaiseInvokeCompleted));
            return _raiseInvokeCompletedCallback;
        }
    }

    private object ThisLock => _thisLock;

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    public static IDictionary<string, object> Invoke(Activity workflow) => Invoke(workflow, ActivityDefaults.InvokeTimeout);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public static IDictionary<string, object> Invoke(Activity workflow, TimeSpan timeout) => Invoke(workflow, timeout, null);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public static IDictionary<string, object> Invoke(Activity workflow, IDictionary<string, object> inputs) => Invoke(workflow, inputs, ActivityDefaults.InvokeTimeout, null);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public static IDictionary<string, object> Invoke(Activity workflow, IDictionary<string, object> inputs, TimeSpan timeout) => Invoke(workflow, inputs, timeout, null);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public static TResult Invoke<TResult>(Activity<TResult> workflow) => Invoke(workflow, null);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public static TResult Invoke<TResult>(Activity<TResult> workflow, IDictionary<string, object> inputs) => Invoke(workflow, inputs, ActivityDefaults.InvokeTimeout);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public static TResult Invoke<TResult>(Activity<TResult> workflow, IDictionary<string, object> inputs, TimeSpan timeout) => Invoke(workflow, inputs, out IDictionary<string, object> dummyOutputs, timeout);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Arch approved design. Requires the out argument for extra information provided")]
    public static TResult Invoke<TResult>(Activity<TResult> workflow, IDictionary<string, object> inputs, out IDictionary<string, object> additionalOutputs, TimeSpan timeout)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);
        if (inputs != null)
        {
            additionalOutputs = Invoke(workflow, inputs, timeout, null);
        }
        else
        {
            additionalOutputs = Invoke(workflow, timeout, null);
        }
        if (additionalOutputs.TryGetValue("Result", out object untypedResult))
        {
            additionalOutputs.Remove("Result");
            return (TResult)untypedResult;
        }
        else
        {
            throw Fx.AssertAndThrow("Activity<TResult> should always have a output named \"Result\"");
        }
    }

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public IAsyncResult BeginInvoke(AsyncCallback callback, object state) => BeginInvoke(_workflow, ActivityDefaults.InvokeTimeout, _extensions, callback, state);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public IAsyncResult BeginInvoke(TimeSpan timeout, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return BeginInvoke(_workflow, timeout, _extensions, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public IAsyncResult BeginInvoke(IDictionary<string, object> inputs, AsyncCallback callback, object state) => BeginInvoke(_workflow, inputs, ActivityDefaults.InvokeTimeout, _extensions, callback, state);

    [Fx.Tag.InheritThrows(From = "Invoke")]
    public IAsyncResult BeginInvoke(IDictionary<string, object> inputs, TimeSpan timeout, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return BeginInvoke(_workflow, inputs, timeout, _extensions, callback, state);
    }

    public void CancelAsync(object userState)
    {
        if (userState == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(userState));
        }

        AsyncInvokeContext context = RemoveFromPendingInvokes(userState);
        if (context != null)
        {
            // cancel does not need a timeout since it's bounded by the invoke timeout
            cancelCallback ??= Fx.ThunkCallback(new AsyncCallback(CancelCallback));
            // cancel only throws TimeoutException and shouldnt throw at all if timeout is infinite
            // cancel does not need to raise InvokeCompleted since the InvokeAsync invocation would raise it
            IAsyncResult result = context.WorkflowApplication.BeginCancel(TimeSpan.MaxValue, cancelCallback, context);
            if (result.CompletedSynchronously)
            {
                context.WorkflowApplication.EndCancel(result);
            }
        }
    }

    [Fx.Tag.InheritThrows(From = "Invoke")]
#pragma warning disable CA1822 // Mark members as static
    public IDictionary<string, object> EndInvoke(IAsyncResult result) => WorkflowApplication.EndInvoke(result);
#pragma warning restore CA1822 // Mark members as static

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    public IDictionary<string, object> Invoke() => Invoke(_workflow, ActivityDefaults.InvokeTimeout, _extensions);

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    public IDictionary<string, object> Invoke(TimeSpan timeout)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return Invoke(_workflow, timeout, _extensions);
    }

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    public IDictionary<string, object> Invoke(IDictionary<string, object> inputs) => Invoke(_workflow, inputs, ActivityDefaults.InvokeTimeout, _extensions);

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    public IDictionary<string, object> Invoke(IDictionary<string, object> inputs, TimeSpan timeout)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return Invoke(_workflow, inputs, timeout, _extensions);
    }

    public void InvokeAsync() => InvokeAsync(ActivityDefaults.InvokeTimeout, null);

    public void InvokeAsync(TimeSpan timeout) => InvokeAsync(timeout, null);

    public void InvokeAsync(object userState) => InvokeAsync(ActivityDefaults.InvokeTimeout, userState);

    public void InvokeAsync(TimeSpan timeout, object userState)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        InternalInvokeAsync(null, timeout, userState);
    }

    public void InvokeAsync(IDictionary<string, object> inputs) => InvokeAsync(inputs, null);

    public void InvokeAsync(IDictionary<string, object> inputs, TimeSpan timeout) => InvokeAsync(inputs, timeout, null);

    public void InvokeAsync(IDictionary<string, object> inputs, object userState) => InvokeAsync(inputs, ActivityDefaults.InvokeTimeout, userState);

    public void InvokeAsync(IDictionary<string, object> inputs, TimeSpan timeout, object userState)
    {
        if (inputs == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(inputs));
        }
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        InternalInvokeAsync(inputs, timeout, userState);
    }

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    private static IDictionary<string, object> Invoke(Activity workflow, TimeSpan timeout, WorkflowInstanceExtensionManager extensions)
    {
        if (workflow == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(workflow));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        IDictionary<string, object> outputs = WorkflowApplication.Invoke(workflow, null, extensions, timeout);

        return outputs ?? ActivityUtilities.EmptyParameters;
    }

    [Fx.Tag.Throws.Timeout("A timeout occurred when invoking the workflow")]
    private static IDictionary<string, object> Invoke(Activity workflow, IDictionary<string, object> inputs, TimeSpan timeout, WorkflowInstanceExtensionManager extensions)
    {
        if (workflow == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(workflow));
        }

        if (inputs == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(inputs));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        IDictionary<string, object> outputs = WorkflowApplication.Invoke(workflow, inputs, extensions, timeout);

        return outputs ?? ActivityUtilities.EmptyParameters;
    }

    private void AddToPendingInvokes(AsyncInvokeContext context)
    {
        lock (ThisLock)
        {
            if (PendingInvokes.ContainsKey(context.UserState))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SameUserStateUsedForMultipleInvokes));
            }
            PendingInvokes.Add(context.UserState, context);
        }
    }

    [Fx.Tag.InheritThrows(From = "Invoke")]
    private static IAsyncResult BeginInvoke(Activity workflow, IDictionary<string, object> inputs, TimeSpan timeout, WorkflowInstanceExtensionManager extensions, AsyncCallback callback, object state)
    {
        if (inputs == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(inputs));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return WorkflowApplication.BeginInvoke(workflow, inputs, extensions, timeout, null, null, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "Invoke")]
    private static IAsyncResult BeginInvoke(Activity workflow, TimeSpan timeout, WorkflowInstanceExtensionManager extensions, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return WorkflowApplication.BeginInvoke(workflow, null, extensions, timeout, null, null, callback, state);
    }

    private void CancelCallback(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }
        AsyncInvokeContext context = (AsyncInvokeContext)result.AsyncState;
        // cancel only throws TimeoutException and shouldnt throw at all if timeout is infinite
        context.WorkflowApplication.EndCancel(result);
    }

    private void InternalInvokeAsync(IDictionary<string, object> inputs, TimeSpan timeout, object userState)
    {
        AsyncInvokeContext context = new AsyncInvokeContext(userState, this);
        if (userState != null)
        {
            AddToPendingInvokes(context);
        }
        Exception error = null;
        bool completedSynchronously = false;
        try
        {
            invokeCallback ??= Fx.ThunkCallback(new AsyncCallback(InvokeCallback));
            context.Operation.OperationStarted();
            IAsyncResult result = WorkflowApplication.BeginInvoke(_workflow, inputs, _extensions, timeout, SynchronizationContext.Current, context, invokeCallback, context);
            if (result.CompletedSynchronously)
            {
                context.Outputs = EndInvoke(result);
                completedSynchronously = true;
            }
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            error = e;
        }
        if (error != null || completedSynchronously)
        {
            PostInvokeCompletedAndRemove(context, error);
        }
    }

    private void InvokeCallback(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }
        AsyncInvokeContext context = (AsyncInvokeContext)result.AsyncState;
        WorkflowInvoker thisPtr = context.Invoker;
        Exception error = null;
        try
        {
            context.Outputs = thisPtr.EndInvoke(result);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            error = e;
        }
        thisPtr.PostInvokeCompletedAndRemove(context, error);
    }

    private void PostInvokeCompleted(AsyncInvokeContext context, Exception error)
    {
        bool cancelled;
        if (error == null)
        {
            context.WorkflowApplication.GetCompletionStatus(out error, out cancelled);
        }
        else
        {
            cancelled = false;
        }
        PostInvokeCompleted(context, cancelled, error);
    }

    private void PostInvokeCompleted(AsyncInvokeContext context, bool cancelled, Exception error)
    {
        InvokeCompletedEventArgs e = new(error, cancelled, context);
        if (InvokeCompleted == null)
        {
            context.Operation.OperationCompleted();
        }
        else
        {
            context.Operation.PostOperationCompleted(RaiseInvokeCompletedCallback, e);
        }
    }

    private void PostInvokeCompletedAndRemove(AsyncInvokeContext context, Exception error)
    {
        if (context.UserState != null)
        {
            RemoveFromPendingInvokes(context.UserState);
        }
        PostInvokeCompleted(context, error);
    }

    private void RaiseInvokeCompleted(object state) => InvokeCompleted?.Invoke(this, (InvokeCompletedEventArgs)state);

    private AsyncInvokeContext RemoveFromPendingInvokes(object userState)
    {
        AsyncInvokeContext context;
        lock (ThisLock)
        {
            if (PendingInvokes.TryGetValue(userState, out context))
            {
                PendingInvokes.Remove(userState);
            }
        }
        return context;
    }
}
