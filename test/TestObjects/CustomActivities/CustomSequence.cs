// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.CustomActivities
{
    public class CustomSequence : CustomSequenceBase
    {
        private const string OnSequenceCompleteMessageFormat = "Custom Sequence {0} Complete";
        public string OnSequenceCompleteMessage
        {
            get
            {
                return String.Format(OnSequenceCompleteMessageFormat, this.DisplayName);
            }
        }

        protected override void OnSequenceComplete(NativeActivityContext executionContext)
        {
            TestTraceListenerExtension listenerExtension = executionContext.GetExtension<TestTraceListenerExtension>();
            UserTrace.Trace(listenerExtension, executionContext.WorkflowInstanceId, this.OnSequenceCompleteMessage);
            base.OnSequenceComplete(executionContext);
        }
    }
}
