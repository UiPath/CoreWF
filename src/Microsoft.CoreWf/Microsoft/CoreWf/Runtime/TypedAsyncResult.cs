// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Runtime
{
    internal abstract class TypedAsyncResult<T> : AsyncResult
    {
        private T _data;

        public TypedAsyncResult(AsyncCallback callback, object state)
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
            TypedAsyncResult<T> completedResult = AsyncResult.End<TypedAsyncResult<T>>(result);
            return completedResult.Data;
        }
    }
}
