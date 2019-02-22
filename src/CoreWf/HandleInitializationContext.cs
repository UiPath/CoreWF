// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Activities.Runtime;
    using System.Activities.Internals;

    [Fx.Tag.XamlVisible(false)]
    public sealed class HandleInitializationContext 
    {
        private readonly ActivityExecutor executor;
        private readonly ActivityInstance scope;
        private bool isDiposed;

        internal HandleInitializationContext(ActivityExecutor executor, ActivityInstance scope)
        {
            this.executor = executor;
            this.scope = scope;
        }

        internal ActivityInstance OwningActivityInstance
        {
            get
            {
                return this.scope;
            }
        }

        internal ActivityExecutor Executor
        {
            get
            {
                return this.executor;
            }
        }

        public THandle CreateAndInitializeHandle<THandle>() where THandle : Handle
        {
            ThrowIfDisposed();
            THandle value = Activator.CreateInstance<THandle>();

            value.Initialize(this);

            // If we have a scope, we need to add this new handle to the LocationEnvironment.
            if (this.scope != null)
            {
                this.scope.Environment.AddHandle(value);
            }
            // otherwise add it to the Executor.
            else
            {
                this.executor.AddHandle(value);
            }

            return value;
        }

        public T GetExtension<T>() where T : class
        {
            return this.executor.GetExtension<T>();
        }

        public void UninitializeHandle(Handle handle)
        {
            ThrowIfDisposed();
            handle.Uninitialize(this);
        }

        internal object CreateAndInitializeHandle(Type handleType)
        {
            Fx.Assert(ActivityUtilities.IsHandle(handleType), "This should only be called with Handle subtypes.");

            object value = Activator.CreateInstance(handleType);

            ((Handle)value).Initialize(this);

            // If we have a scope, we need to add this new handle to the LocationEnvironment.
            if (this.scope != null)
            {
                this.scope.Environment.AddHandle((Handle)value);
            }
            // otherwise add it to the Executor.
            else
            {
                this.executor.AddHandle((Handle)value);
            }

            return value;
        }

        internal BookmarkScope CreateAndRegisterBookmarkScope()
        {
            return this.executor.BookmarkScopeManager.CreateAndRegisterScope(Guid.Empty);
        }

        internal void UnregisterBookmarkScope(BookmarkScope bookmarkScope)
        {
            Fx.Assert(bookmarkScope != null, "The sub instance should not equal null.");

            this.executor.BookmarkScopeManager.UnregisterScope(bookmarkScope);
        }

        private void ThrowIfDisposed()
        {
            if (this.isDiposed)
            {
                throw FxTrace.Exception.AsError(new ObjectDisposedException(SR.HandleInitializationContextDisposed));
            }
        }

        internal void Dispose()
        {
            this.isDiposed = true;
        }
    }
}


