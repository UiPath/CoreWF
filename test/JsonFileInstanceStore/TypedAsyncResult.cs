// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JsonFileInstanceStore
{
    /// <summary>
    /// A strongly typed AsyncResult.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class TypedAsyncResult<T> : FileStoreAsyncResult
    {
        private T _data;

        protected TypedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public T Data
        {
            get { return _data; }
        }

        protected void Complete(T data, bool completedSynchronously)
        {
            _data = data;
            Complete(completedSynchronously);
        }

        public static T End(IAsyncResult result)
        {
            TypedAsyncResult<T> typedResult = FileStoreAsyncResult.End<TypedAsyncResult<T>>(result);
            return typedResult.Data;
        }
    }
}
