// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class HandleInitializationContext
    {
        private ActivityExecutor _executor;
        private ActivityInstance _scope;
        private bool _isDiposed;

        internal HandleInitializationContext(ActivityExecutor executor, ActivityInstance scope)
        {
            _executor = executor;
            _scope = scope;
        }

        internal ActivityInstance OwningActivityInstance
        {
            get
            {
                return _scope;
            }
        }

        internal ActivityExecutor Executor
        {
            get
            {
                return _executor;
            }
        }

        public THandle CreateAndInitializeHandle<THandle>() where THandle : Handle
        {
            ThrowIfDisposed();
            THandle value = Activator.CreateInstance<THandle>();

            value.Initialize(this);

            // If we have a scope, we need to add this new handle to the LocationEnvironment.
            if (_scope != null)
            {
                _scope.Environment.AddHandle(value);
            }
            // otherwise add it to the Executor.
            else
            {
                _executor.AddHandle(value);
            }

            return value;
        }

        public T GetExtension<T>() where T : class
        {
            return _executor.GetExtension<T>();
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
            if (_scope != null)
            {
                _scope.Environment.AddHandle((Handle)value);
            }
            // otherwise add it to the Executor.
            else
            {
                _executor.AddHandle((Handle)value);
            }

            return value;
        }

        internal BookmarkScope CreateAndRegisterBookmarkScope()
        {
            return _executor.BookmarkScopeManager.CreateAndRegisterScope(Guid.Empty);
        }

        internal void UnregisterBookmarkScope(BookmarkScope bookmarkScope)
        {
            Fx.Assert(bookmarkScope != null, "The sub instance should not equal null.");

            _executor.BookmarkScopeManager.UnregisterScope(bookmarkScope);
        }

        private void ThrowIfDisposed()
        {
            if (_isDiposed)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new ObjectDisposedException(SR.HandleInitializationContextDisposed));
            }
        }

        internal void Dispose()
        {
            _isDiposed = true;
        }
    }
}


