// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

public abstract class CodeActivity : Activity
{
    protected CodeActivity() { }

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

    protected abstract void Execute(CodeActivityContext context);

    sealed internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        CodeActivityContext context = executor.CodeActivityContextPool.Acquire();
        try
        {
            context.Initialize(instance, executor);
            Execute(context);
        }
        finally
        {
            context.Dispose();
            executor.CodeActivityContextPool.Release(context);
        }
    }

    sealed internal override void InternalCancel(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        Fx.Assert("Cancel should never be called on CodeActivity since it's synchronous");
    }

    sealed internal override void InternalAbort(ActivityInstance instance, ActivityExecutor executor, Exception terminationReason)
    {
        // no-op, this is only called if an exception is thrown out of execute
    }

    sealed internal override void OnInternalCacheMetadata(bool createEmptyBindings)
    {
        CodeActivityMetadata metadata = new(this, GetParentEnvironment(), createEmptyBindings);
        CacheMetadata(metadata);
        metadata.Dispose();
        if (RuntimeArguments == null || RuntimeArguments.Count == 0)
        {
            SkipArgumentResolution = true;
        }
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
}

public abstract class CodeActivity<TResult> : Activity<TResult>
{
    protected CodeActivity() { }

    [IgnoreDataMember]
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

    protected abstract TResult Execute(CodeActivityContext context);

    sealed internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        CodeActivityContext context = executor.CodeActivityContextPool.Acquire();
        try
        {
            context.Initialize(instance, executor);
            TResult executeResult = Execute(context);
            Result.Set(context, executeResult);
        }
        finally
        {
            context.Dispose();
            executor.CodeActivityContextPool.Release(context);
        }
    }

    sealed internal override void InternalCancel(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        Fx.Assert("Cancel should never be called on CodeActivity<T> since it's synchronous");
    }

    sealed internal override void InternalAbort(ActivityInstance instance, ActivityExecutor executor, Exception terminationReason)
    {
        // no-op, this is only called if an exception is thrown out of execute
    }

    sealed internal override void OnInternalCacheMetadataExceptResult(bool createEmptyBindings)
    {
        CodeActivityMetadata metadata = new(this, GetParentEnvironment(), createEmptyBindings);
        CacheMetadata(metadata);
        metadata.Dispose();
        if (RuntimeArguments == null || RuntimeArguments.Count == 0 ||
            // If there's an argument named "Result", we can safely assume it's the actual result
            // argument, because Activity<T> will raise a validation error if it's not.
            (RuntimeArguments.Count == 1 && RuntimeArguments[0].Name == Argument.ResultValue))
        {
            SkipArgumentResolution = true;
        }
    }

    sealed internal override TResult InternalExecuteInResolutionContext(CodeActivityContext context)
    {
        Fx.Assert(SkipArgumentResolution, "This method should only be called if SkipArgumentResolution is true");
        return Execute(context);
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
}
