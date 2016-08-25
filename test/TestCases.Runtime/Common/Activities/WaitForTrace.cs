// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.ObjectModel;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace TestCases.Runtime.Common.Activities
{
    public class WaitForTrace : CodeActivity
    {
        public const string EnterExecute = "Enter WaitForTrace Execute method";

        public InArgument<string> TraceToWait { get; set; }

        public InArgument<TimeSpan> DelayDuration { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("TraceToWait", typeof(string), ArgumentDirection.In));
            runtimeArguments.Add(new RuntimeArgument("DelayDuration", typeof(TimeSpan), ArgumentDirection.In));
            metadata.Bind(this.TraceToWait, runtimeArguments[0]);
            metadata.Bind(this.DelayDuration, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(CodeActivityContext context)
        {
            TestTraceManager.Instance.AddTrace(context.WorkflowInstanceId, new SynchronizeTrace(EnterExecute));
            SynchronizeTrace.Trace(context.WorkflowInstanceId, EnterExecute);

            if (this.TraceToWait.Get(context).Length != 0)
            {
                TestTraceManager.Instance.WaitForTrace(context.WorkflowInstanceId, new SynchronizeTrace(this.TraceToWait.Get(context)), 1);
            }

            Thread.CurrentThread.Join((int)this.DelayDuration.Get(context).TotalMilliseconds);
        }
    }

    public class TestWaitForTrace : TestActivity
    {
        public String TraceToWait
        {
            set
            {
                (this.ProductActivity as WaitForTrace).TraceToWait = new InArgument<string>(value);
            }
        }

        public TimeSpan DelayDuration
        {
            set
            {
                (this.ProductActivity as WaitForTrace).DelayDuration = new InArgument<TimeSpan>(value);
            }
        }

        public TestWaitForTrace()
        {
            this.ProductActivity = new WaitForTrace();
            this.TraceToWait = "";
            this.DelayDuration = TimeSpan.Zero;
        }
    }
}
