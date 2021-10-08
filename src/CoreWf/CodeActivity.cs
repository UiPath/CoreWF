// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System;
    using System.Runtime.Serialization;

    using System.Activities.DynamicUpdate;
    public abstract class CodeActivity : Activity
    {
        protected CodeActivity()
        {
        }

        protected internal sealed override Version ImplementationVersion
        {
            get
            {
                return null;
            }
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
            get
            {
                return null;
            }
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
            CodeActivityMetadata metadata = new CodeActivityMetadata(this, this.GetParentEnvironment(), createEmptyBindings);
            CacheMetadata(metadata);
            metadata.Dispose();
            if (this.RuntimeArguments == null || this.RuntimeArguments.Count == 0)
            {
                this.SkipArgumentResolution = true;
            }
        }

#if NET45
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
        protected CodeActivity()
        {
        }

        [IgnoreDataMember]
        protected internal sealed override Version ImplementationVersion
        {
            get
            {
                return null;
            }
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
            get
            {
                return null;
            }
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
                this.Result.Set(context, executeResult);
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
            CodeActivityMetadata metadata = new CodeActivityMetadata(this, this.GetParentEnvironment(), createEmptyBindings);
            CacheMetadata(metadata);
            metadata.Dispose();
            if (this.RuntimeArguments == null || this.RuntimeArguments.Count == 0 ||
                // If there's an argument named "Result", we can safely assume it's the actual result
                // argument, because Activity<T> will raise a validation error if it's not.
                (this.RuntimeArguments.Count == 1 && this.RuntimeArguments[0].Name == Argument.ResultValue))
            {
                this.SkipArgumentResolution = true;
            }
        }

        sealed internal override TResult InternalExecuteInResolutionContext(CodeActivityContext context)
        {
            Fx.Assert(this.SkipArgumentResolution, "This method should only be called if SkipArgumentResolution is true");
            return Execute(context);
        }

#if NET45
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
    public class FuncValue<TResult> : CodeActivity<TResult>
    {
        private readonly Func<ActivityContext, TResult> _func;
        public FuncValue(Func<ActivityContext, TResult> func) => _func = func ?? throw new ArgumentNullException(nameof(func));
        protected override TResult Execute(CodeActivityContext context)
        {
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                return _func(context);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
        }
    }
    public class FuncReference<TLocation> : CodeActivity<Location<TLocation>>
    {
        private readonly string _locationName;
        public FuncReference(string locationName) => _locationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
        protected override Location<TLocation> Execute(CodeActivityContext context)
        {
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                return context.GetLocation<TLocation>(_locationName);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
        }
    }
    public class FuncReference<TLocation, TResult> : CodeActivity<Location<TResult>>
    {
        private readonly string _locationName;
        private readonly Func<TLocation, TResult> _get;
        private readonly Func<TLocation, TResult, TLocation> _set;
        public FuncReference(string locationName, Func<TLocation, TResult> get, Func<TLocation, TResult, TLocation> set)
        {
            _locationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
            _get = get ?? throw new ArgumentNullException(nameof(get));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }
        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            Location<TLocation> location;
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                location = context.GetLocation<TLocation>(_locationName);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
            return new FuncLocation(location, _get, _set);
        }
        class FuncLocation : Location<TResult>
        {
            private readonly Location<TLocation> _location;
            private readonly Func<TLocation, TResult> _get;
            private readonly Func<TLocation, TResult, TLocation> _set;
            public FuncLocation(Location<TLocation> location, Func<TLocation, TResult> get, Func<TLocation, TResult, TLocation> set)
            {
                _location = location;
                _get = get;
                _set = set;
            }
            public override TResult Value { get => _get(_location.Value); set => _location.Value = _set(_location.Value, value); }
        }
    }
}