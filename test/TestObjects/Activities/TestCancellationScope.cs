// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf.Statements;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestCancellationScope : TestActivity
    {
        private TestActivity _body;
        private TestActivity _handler;

        public TestCancellationScope()
        {
            this.ProductActivity = new CancellationScope();
        }

        public TestCancellationScope(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public CancellationScope ProductCancellationScope
        {
            get
            {
                return (CancellationScope)this.ProductActivity;
            }
        }

        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                this.ProductCancellationScope.Body = value.ProductActivity;
            }
        }

        public TestActivity Handler
        {
            get
            {
                return _handler;
            }
            set
            {
                _handler = value;
                this.ProductCancellationScope.CancellationHandler = value.ProductActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            Outcome bodyOutcome = this.Body.GetTrace(traceGroup);

            if (bodyOutcome.DefaultPropogationState == OutcomeState.Completed)
            {
                return;
            }
            else if (bodyOutcome.DefaultPropogationState == OutcomeState.Canceled)
            {
                if (this.Handler != null)
                {
                    this.Handler.GetTrace(traceGroup);
                }
                this.CurrentOutcome = Outcome.Canceled;
            }
            else
            {
                throw new NotSupportedException(
                    String.Format("Current CancellationScope test object doesn't support Body.OutCome='{0}'.", bodyOutcome.DefaultPropogationState.ToString()));
            }
        }
    }
}
