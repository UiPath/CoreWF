// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using LegacyTest.Test.Common.TestObjects.CustomActivities;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestAsyncOperationBlockActivity : TestActivity
    {
        private readonly UserTrace _completedTrace = new UserTrace(AsyncOperationBlockActivity.AsyncOperationBlockExited);

        public TestAsyncOperationBlockActivity()
        {
            this.ProductActivity = new AsyncOperationBlockActivity();
        }

        public TestAsyncOperationBlockActivity(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TimeSpan Duration
        {
            set
            {
                this.ProductAsyncOperationBlockActivity.Duration = value;
            }
        }

        private AsyncOperationBlockActivity ProductAsyncOperationBlockActivity
        {
            get { return (AsyncOperationBlockActivity)this.ProductActivity; }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // Due to how the activities are scheduled from inside the Activity, these traces appear first
            traceGroup.Steps.Add(new UserTrace(AsyncOperationBlockActivity.AsyncOperationBlockEntered));
            traceGroup.Steps.Add(new UserTrace(AsyncOperationBlockActivity.AsyncOperationBlockExited));

            base.GetActivitySpecificTrace(traceGroup);
        }
    }
}
