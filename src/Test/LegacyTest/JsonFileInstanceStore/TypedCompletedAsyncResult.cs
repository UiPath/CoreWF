// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace LegacyTest.JsonFileInstanceStore
{
    /// <summary>
    /// A strongly typed AsyncResult that completes as soon as it is instantiated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TypedCompletedAsyncResult<T> : TypedAsyncResult<T>
    {
        public TypedCompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(data, true);
        }

        public new static T End(IAsyncResult result)
        {

            if (!(result is TypedCompletedAsyncResult<T> completedResult))
            {
                throw new ArgumentException("InvalidAsyncResult");
            }

            return TypedAsyncResult<T>.End(completedResult);
        }
    }
}
