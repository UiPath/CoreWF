// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.CustomActivities
{
    public sealed class AsyncOperationBlockActivity : AsyncCodeActivity
    {
        public const string AsyncOperationBlockEntered = "Entered the AsyncOperationBlock";
        public const string AsyncOperationBlockExited = "Exited the AsyncOperationBlock";

        public AsyncOperationBlockActivity()
        {
        }

        public AsyncOperationBlockActivity(TimeSpan duration)
            : this()
        {
            this.Duration = duration;
        }

        public InArgument<TimeSpan> Duration
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument durationArgument = new RuntimeArgument("Duration", typeof(TimeSpan), ArgumentDirection.In);

            metadata.Bind(this.Duration, durationArgument);

            metadata.AddArgument(durationArgument);
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            TestTraceListenerExtension listenerExtension = context.GetExtension<TestTraceListenerExtension>();
            UserTrace.Trace(listenerExtension, context.WorkflowInstanceId, AsyncOperationBlockEntered);

            AsyncWorkState asyncWorkState = new AsyncWorkState()
            {
                InstanceID = context.WorkflowInstanceId,
                Duration = this.Duration.Get(context),
                ListenerExtension = listenerExtension
            };

            return new AsyncOperationBlockActivityAsyncResult(asyncWorkState, callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            AsyncOperationBlockActivityAsyncResult.End(result);
        }

        private class AsyncOperationBlockActivityAsyncResult : AsyncResult
        {
            public AsyncOperationBlockActivityAsyncResult(AsyncWorkState asyncWorkState, AsyncCallback callback, object state)
                : base(callback, state)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteAsyncBlock), asyncWorkState);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<AsyncOperationBlockActivityAsyncResult>(result);
            }

            private void CompleteAsyncBlock(object state)
            {
                AsyncWorkState asyncWorkState = (AsyncWorkState)state;
                RunTimer(asyncWorkState.Duration);

                UserTrace.Trace(asyncWorkState.ListenerExtension, asyncWorkState.InstanceID, AsyncOperationBlockExited);

                Complete(false);
            }

            private void RunTimer(TimeSpan duration)
            {
                // Force the AsyncOperationBlock to run for a substantially long time...
                Thread.CurrentThread.Join((int)duration.TotalMilliseconds);
            }
        }

        private class AsyncWorkState
        {
            public Guid InstanceID
            {
                get;
                set;
            }

            public TimeSpan Duration
            {
                get;
                set;
            }

            public TestTraceListenerExtension ListenerExtension
            {
                get;
                set;
            }
        }
    }
}
