// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace TestCases.Runtime.Common.Activities
{
    public class WaitForEventActivity : AsyncCodeActivity
    {
        private ManualResetEvent _mre = new ManualResetEvent(false);
        public const string UserTraceMessageBeforeWait = "Executing WaitForEventActivity";

        public WaitForEventActivity()
        {
            this.AutomaticSetLock = true;
            this.SetEventTime = TimeSpan.FromSeconds(2);
        }

        public bool AsyncWait { get; set; }
        public bool AutomaticSetLock { get; set; }
        public TimeSpan SetEventTime { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            //None
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //UserTrace.Trace(context.WorkflowInstanceId, UserTraceMessageBeforeWait);
            TestTraceManager.Instance.AddTrace(context.WorkflowInstanceId, new UserTrace(UserTraceMessageBeforeWait));
            Action<bool> waitMethodDelegate = new Action<bool>(AsyncCallback);
            context.UserState = waitMethodDelegate;

            if (!AsyncWait)
            {
                SetLockAutomatic();
                WaitOne();
                return waitMethodDelegate.BeginInvoke(false, callback, state);
            }
            else
            {
                IAsyncResult result = waitMethodDelegate.BeginInvoke(true, callback, state);
                SetLockAutomatic();
                return result;
            }
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Action<bool>)context.UserState).EndInvoke(result);
        }


        private void AsyncCallback(bool waitInCallback)
        {
            if (waitInCallback)
            {
                this.WaitOne();
            }
        }

        private void WaitOne()
        {
            _mre.WaitOne();
        }

        private void SetLockAutomatic()
        {
            if (AutomaticSetLock)
            {
                Action setLockDelegate = new Action(SetLock);
                setLockDelegate.BeginInvoke(null, null);
            }
        }

        private void SetLock()
        {
            Thread.CurrentThread.Join((int)SetEventTime.TotalMilliseconds);
            _mre.Set();
        }

        public void WaitForLockSet()
        {
            Thread.CurrentThread.Join((int)(SetEventTime + TimeSpan.FromSeconds(1)).TotalMilliseconds);
        }
    }

    public class TestWaitForEventActivity : TestActivity
    {
        private WaitForEventActivity _productwaitActivity;

        public TestWaitForEventActivity(string displayName)
        {
            _productwaitActivity = new WaitForEventActivity();
            this.ProductActivity = _productwaitActivity;

            this.ProductActivity.DisplayName = displayName;
            this.DisplayName = displayName;
            this.ActivitySpecificTraces.Add(new UserTrace(WaitForEventActivity.UserTraceMessageBeforeWait));
        }

        public TestWaitForEventActivity() :
            this(String.Empty)
        {
        }

        public bool AsyncWait
        {
            get
            {
                return _productwaitActivity.AsyncWait;
            }
            set
            {
                _productwaitActivity.AsyncWait = value;
            }
        }

        public bool AutomaticSetLock
        {
            get
            {
                return _productwaitActivity.AutomaticSetLock;
            }
            set
            {
                _productwaitActivity.AutomaticSetLock = value;
            }
        }

        public TimeSpan SetEventTime
        {
            get
            {
                return _productwaitActivity.SetEventTime;
            }
            set
            {
                _productwaitActivity.SetEventTime = value;
            }
        }

        public void WaitForLockSet()
        {
            _productwaitActivity.WaitForLockSet();
        }
    }
}
