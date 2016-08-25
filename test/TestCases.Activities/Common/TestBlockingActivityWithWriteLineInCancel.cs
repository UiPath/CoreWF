// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace TestCases.Activities.Common
{
    public class TestBlockingActivityWithWriteLineInCancel : TestActivity
    {
        public TestBlockingActivityWithWriteLineInCancel()
        {
            this.ProductActivity = new BlockingActivityWithWriteLineInCancel { OutcomeOnCancellation = new InArgument<OutcomeState>(OutcomeState.Canceled) };
        }

        public TestBlockingActivityWithWriteLineInCancel(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestBlockingActivityWithWriteLineInCancel(string displayName, OutcomeState outcome)
            : this(displayName)
        {
            ((BlockingActivityWithWriteLineInCancel)this.ProductActivity).OutcomeOnCancellation = new InArgument<OutcomeState>(outcome);
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            base.GetActivitySpecificTrace(traceGroup);

            List<WorkflowTraceStep> w1Trace;
            w1Trace = new TestSequence("w1") { ExpectedOutcome = this.ExpectedOutcome }.GetExpectedTrace().Trace.Steps;
            traceGroup.Steps.AddRange(w1Trace);
        }
    }

    public sealed class BlockingActivityWithWriteLineInCancel : NativeActivity
    {
        private Activity _w1;


        public BlockingActivityWithWriteLineInCancel()
        {
            _w1 = new Microsoft.CoreWf.Statements.WriteLine { DisplayName = "w1", Text = this.DisplayName };
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
        }

        [RequiredArgument]
        public InArgument<OutcomeState> OutcomeOnCancellation
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddImplementationChild(_w1);
            RuntimeArgument exceptionArgument = new RuntimeArgument("OutcomeOnCancellation", typeof(OutcomeState), ArgumentDirection.In, true);
            metadata.Bind(this.OutcomeOnCancellation, exceptionArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { exceptionArgument });
            //base.CacheMetadata(metadata);
        }

        protected override void Cancel(NativeActivityContext context)
        {
            context.ScheduleActivity(_w1);
            OutcomeState outcome = this.OutcomeOnCancellation.Get(context);
            switch (outcome)
            {
                case OutcomeState.Canceled:
                    base.Cancel(context);
                    break;
                case OutcomeState.Completed:
                    context.RemoveAllBookmarks();
                    break;
                case OutcomeState.Faulted:
                    throw new TestCaseException("Exception is intentionally thrown during cancellation");
            }
        }

        private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
            // No-op
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
