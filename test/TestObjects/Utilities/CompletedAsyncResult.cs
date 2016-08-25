// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Test.Common.TestObjects.Utilities
{
    //An AsyncResult that completes as soon as it is instantiated.
    public class CompletedAsyncResult : AsyncResult
    {
        public CompletedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<CompletedAsyncResult>(result);
        }
    }

    public class CompletedAsyncResult<T> : AsyncResult
    {
        private T _data;

        public CompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _data = data;
            Complete(true);
        }

        public static T End(IAsyncResult result)
        {
            CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
            return completedResult._data;
        }
    }
}
