// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

public abstract class AsyncCodeActivity : Activity, IAsyncCodeActivity
{
    private static AsyncCallback onExecuteComplete;

    protected AsyncCodeActivity() { }

    protected internal sealed override Version ImplementationVersion
    {
        get => null;
        set
        {
            if (value != null)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException());
            }
        }
    }

    [IgnoreDataMember]
    [Fx.Tag.KnownXamlExternal]
    protected sealed override Func<Activity> Implementation
    {
        get => null;
        set
        {
            if (value != null)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException());
            }
        }
    }

    internal static AsyncCallback OnExecuteComplete
    {
        get
        {
            onExecuteComplete ??= Fx.ThunkCallback(new AsyncCallback(CompleteAsynchronousExecution));
            return onExecuteComplete;
        }
    }

    internal override bool InternalCanInduceIdle => true;

    protected abstract IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state);
    protected abstract void EndExecute(AsyncCodeActivityContext context, IAsyncResult result);

    // called on the Cancel and Abort paths to allow cleanup of outstanding async work
    protected virtual void Cancel(AsyncCodeActivityContext context) { }

    sealed internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        // first set up an async context
        AsyncOperationContext asyncContext = executor.SetupAsyncOperationBlock(instance);
        instance.IncrementBusyCount();

        AsyncCodeActivityContext context = new(asyncContext, instance, executor);
        bool success = false;
        try
        {
            IAsyncResult result = BeginExecute(context, OnExecuteComplete, asyncContext);

            if (result == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BeginExecuteMustNotReturnANullAsyncResult));
            }

            if (!ReferenceEquals(result.AsyncState, asyncContext))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BeginExecuteMustUseProvidedStateAsAsyncResultState));
            }

            if (result.CompletedSynchronously)
            {
                EndExecute(context, result);
                asyncContext.CompleteOperation();
            }
            success = true;
        }
        finally
        {
            context.Dispose();
            if (!success)
            {
                asyncContext.CancelOperation();
            }
        }
    }

    void IAsyncCodeActivity.FinishExecution(AsyncCodeActivityContext context, IAsyncResult result) => EndExecute(context, result);

    internal static void CompleteAsynchronousExecution(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }

        // User code may not have correctly passed the AsyncOperationContext thru as the "state" parameter for
        // BeginInvoke. If is null, don't bother going any further. We would have thrown an exception out of the
        // workflow from InternalExecute. In that case, AsyncOperationContext.CancelOperation will be called in
        // InternalExecute.
        if (result.AsyncState is AsyncOperationContext asyncContext)
        {
            asyncContext.CompleteAsyncCodeActivity(new CompleteAsyncCodeActivityData(asyncContext, result));
        }
    }

    sealed internal override void InternalCancel(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (executor.TryGetPendingOperation(instance, out AsyncOperationContext asyncContext))
        {
            AsyncCodeActivityContext context = new(asyncContext, instance, executor);
            try
            {
                asyncContext.HasCalledAsyncCodeActivityCancel = true;
                Cancel(context);
            }
            finally
            {
                context.Dispose();
            }
        }
    }

    sealed internal override void InternalAbort(ActivityInstance instance, ActivityExecutor executor, Exception terminationReason)
    {
        if (executor.TryGetPendingOperation(instance, out AsyncOperationContext asyncContext))
        {
            try
            {
                if (!asyncContext.HasCalledAsyncCodeActivityCancel)
                {
                    asyncContext.IsAborting = true;
                    InternalCancel(instance, executor, null);
                }
            }
            finally
            {
                // we should always abort outstanding contexts
                if (asyncContext.IsStillActive)
                {
                    asyncContext.CancelOperation();
                }
            }
        }
    }

    sealed internal override void OnInternalCacheMetadata(bool createEmptyBindings)
    {
        CodeActivityMetadata metadata = new(this, GetParentEnvironment(), createEmptyBindings);
        CacheMetadata(metadata);
        metadata.Dispose();
    }

#if DYNAMICUPDATE
    internal sealed override void OnInternalCreateDynamicUpdateMap(DynamicUpdateMapBuilder.Finalizer finalizer,
DynamicUpdateMapBuilder.IDefinitionMatcher matcher, Activity originalActivity)
    {
    }

    protected sealed override void OnCreateDynamicUpdateMap(UpdateMapMetadata metadata, Activity originalActivity)
    {
        // NO OP
    } 
#endif

    protected sealed override void CacheMetadata(ActivityMetadata metadata)
    {
        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WrongCacheMetadataForCodeActivity));
    }

    protected virtual void CacheMetadata(CodeActivityMetadata metadata)
    {
        // We bypass the metadata call to avoid the null checks
        SetArgumentsCollection(ReflectedInformation.GetArguments(this), metadata.CreateEmptyBindings);
    }

    private class CompleteAsyncCodeActivityData : AsyncOperationContext.CompleteData
    {
        private readonly IAsyncResult _result;

        public CompleteAsyncCodeActivityData(AsyncOperationContext context, IAsyncResult result)
            : base(context, false)
        {
            _result = result;
        }

        protected override void OnCallExecutor() => Executor.CompleteOperation(new CompleteAsyncCodeActivityWorkItem(AsyncContext, Instance, _result));

        // not [DataContract] since this workitem will never happen when persistable
        private class CompleteAsyncCodeActivityWorkItem : ActivityExecutionWorkItem
        {
            private readonly IAsyncResult _result;
            private readonly AsyncOperationContext _asyncContext;

            public CompleteAsyncCodeActivityWorkItem(AsyncOperationContext asyncContext, ActivityInstance instance, IAsyncResult result)
                : base(instance)
            {
                _result = result;
                _asyncContext = asyncContext;
                ExitNoPersistRequired = true;
            }

            public override void TraceCompleted()
            {
                if (TD.CompleteBookmarkWorkItemIsEnabled())
                {
                    TD.CompleteBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark), ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark.Scope));
                }
            }

            public override void TraceScheduled()
            {
                if (TD.ScheduleBookmarkWorkItemIsEnabled())
                {
                    TD.ScheduleBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark), ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark.Scope));
                }
            }

            public override void TraceStarting()
            {
                if (TD.StartBookmarkWorkItemIsEnabled())
                {
                    TD.StartBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark), ActivityUtilities.GetTraceString(Bookmark.AsyncOperationCompletionBookmark.Scope));
                }
            }

            public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
            {
                AsyncCodeActivityContext context = null;

                try
                {
                    context = new AsyncCodeActivityContext(_asyncContext, ActivityInstance, executor);
                    IAsyncCodeActivity owner = (IAsyncCodeActivity)ActivityInstance.Activity;
                    owner.FinishExecution(context, _result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    ExceptionToPropagate = e;
                }
                finally
                {
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }

                return true;
            }
        }
    }
}

public abstract class AsyncCodeActivity<TResult> : Activity<TResult>, IAsyncCodeActivity
{
    protected AsyncCodeActivity() { }

    protected internal sealed override Version ImplementationVersion
    {
        get => null;
        set
        {
            if (value != null)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException());
            }
        }
    }

    [IgnoreDataMember]
    [Fx.Tag.KnownXamlExternal]
    protected sealed override Func<Activity> Implementation
    {
        get => null;
        set
        {
            if (value != null)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException());
            }
        }
    }

    internal override bool InternalCanInduceIdle => true;

    protected abstract IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state);
    protected abstract TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result);

    // called on the Cancel and Abort paths to allow cleanup of outstanding async work
    protected virtual void Cancel(AsyncCodeActivityContext context) { }

    sealed internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        // first set up an async context
        AsyncOperationContext asyncContext = executor.SetupAsyncOperationBlock(instance);
        instance.IncrementBusyCount();

        AsyncCodeActivityContext context = new(asyncContext, instance, executor);
        bool success = false;
        try
        {
            IAsyncResult result = BeginExecute(context, AsyncCodeActivity.OnExecuteComplete, asyncContext);

            if (result == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BeginExecuteMustNotReturnANullAsyncResult));
            }

            if (!ReferenceEquals(result.AsyncState, asyncContext))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BeginExecuteMustUseProvidedStateAsAsyncResultState));
            }

            if (result.CompletedSynchronously)
            {
                ((IAsyncCodeActivity)this).FinishExecution(context, result);
                asyncContext.CompleteOperation();
            }
            success = true;
        }
        finally
        {
            context.Dispose();
            if (!success)
            {
                asyncContext.CancelOperation();
            }
        }
    }

    void IAsyncCodeActivity.FinishExecution(AsyncCodeActivityContext context, IAsyncResult result)
    {
        TResult executionResult = EndExecute(context, result);
        Result.Set(context, executionResult);
    }

    sealed internal override void InternalCancel(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (executor.TryGetPendingOperation(instance, out AsyncOperationContext asyncContext))
        {
            AsyncCodeActivityContext context = new(asyncContext, instance, executor);
            try
            {
                asyncContext.HasCalledAsyncCodeActivityCancel = true;
                Cancel(context);
            }
            finally
            {
                context.Dispose();
            }
        }
    }

    sealed internal override void InternalAbort(ActivityInstance instance, ActivityExecutor executor, Exception terminationReason)
    {
        if (executor.TryGetPendingOperation(instance, out AsyncOperationContext asyncContext))
        {
            try
            {
                if (!asyncContext.HasCalledAsyncCodeActivityCancel)
                {
                    asyncContext.IsAborting = true;
                    InternalCancel(instance, executor, null);
                }
            }
            finally
            {
                // we should always abort outstanding contexts
                if (asyncContext.IsStillActive)
                {
                    asyncContext.CancelOperation();
                }
            }
        }
    }

    sealed internal override void OnInternalCacheMetadataExceptResult(bool createEmptyBindings)
    {
        CodeActivityMetadata metadata = new(this, GetParentEnvironment(), createEmptyBindings);
        CacheMetadata(metadata);
        metadata.Dispose();
    }

#if DYNAMICUPDATE
    internal sealed override void OnInternalCreateDynamicUpdateMap(DynamicUpdateMapBuilder.Finalizer finalizer,
DynamicUpdateMapBuilder.IDefinitionMatcher matcher, Activity originalActivity)
    {
    }

    protected sealed override void OnCreateDynamicUpdateMap(UpdateMapMetadata metadata, Activity originalActivity)
    {
        // NO OP
    } 
#endif

    protected sealed override void CacheMetadata(ActivityMetadata metadata)
        => throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WrongCacheMetadataForCodeActivity));

    protected virtual void CacheMetadata(CodeActivityMetadata metadata)
    {
        // We bypass the metadata call to avoid the null checks
        SetArgumentsCollection(ReflectedInformation.GetArguments(this), metadata.CreateEmptyBindings);
    }
}
