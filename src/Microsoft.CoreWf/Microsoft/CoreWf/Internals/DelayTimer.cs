// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CoreWf.Internals
{
    internal sealed class DelayTimer : CancellationTokenSource, IDisposable
    {
        private Action<object> _callback;
        private object _state;

        public DelayTimer(Action<object> callback, object state, TimeSpan dueTime)
        {
            Task.Delay(dueTime, Token).ContinueWith((t, s) =>
            {
                var tuple = (Tuple<Action<object>, object>)s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(callback, state), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
        }

        public DelayTimer(Action<object> callback, object state)
        {
            _callback = callback;
            _state = state;
        }

        public void Set(TimeSpan dueTime)
        {
            Task.Delay(dueTime, Token).ContinueWith((t, s) =>
            {
                var tuple = (Tuple<Action<object>, object>)s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(_callback, _state), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }
    }
}
