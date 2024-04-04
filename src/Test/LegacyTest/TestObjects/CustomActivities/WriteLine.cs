// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;
using LegacyTest.Test.Common.TestObjects.Utilities;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    public class WriteLine : CodeActivity
    {
        private InArgument<string> _message;

        public InArgument<string> Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument messageArgument = new RuntimeArgument("Message", typeof(string), ArgumentDirection.In, true);
            metadata.Bind(this.Message, messageArgument);

            metadata.AddArgument(messageArgument);
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            UserTrace userTrace = new UserTrace(executionContext.WorkflowInstanceId,
                this.Id + ":" + executionContext.ActivityInstanceId, this.Message.Get(executionContext));
            //TraceSource ts = new TraceSource("System.Activities.Tracking", SourceLevels.Information);
            //ts.TraceData(TraceEventType.Information, 1, userTrace);
            // PartialTrustTrace.TraceData(ts, TraceEventType.Information, 1, userTrace);

            TestTraceListenerExtension traceExtension = executionContext.GetExtension<TestTraceListenerExtension>();
            if (traceExtension != null)
            {
                traceExtension.TraceData(userTrace);
            }
        }
    }
}
