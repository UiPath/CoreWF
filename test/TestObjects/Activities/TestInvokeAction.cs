// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    internal class TestInvokeAction : TestActivity
    {
        private InvokeAction _productInvokeAction;
        private TestActivity _handler;

        public TestInvokeAction()
        {
            _productInvokeAction = new InvokeAction();
            this.ProductActivity = _productInvokeAction;
            this.DisplayName = typeof(InvokeAction).Name;
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
                _productInvokeAction.Action = new ActivityAction
                {
                    Handler = _handler.ProductActivity,
                };
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.Handler != null)
            {
                yield return this.Handler;
            }
        }
    }

    internal class TestInvokeAction<T> : TestActivity
    {
        private InvokeAction<T> _productInvokeAction;
        private TestActivity _handler;

        public TestInvokeAction()
        {
            _productInvokeAction = new InvokeAction<T>();
            this.ProductActivity = _productInvokeAction;
            this.DisplayName = typeof(InvokeAction<T>).Name;
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
                _productInvokeAction.Action = new ActivityAction<T>
                {
                    Handler = _handler.ProductActivity,
                };
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_handler != null)
            {
                yield return this.Handler;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (_handler != null && ExpectedOutcome != Outcome.Canceled)
            {
                CurrentOutcome = this.Handler.GetTrace(traceGroup);
            }
        }
    }
}
