// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestExpressionEvaluatorWithBody<T> : TestActivity
    {
        private TestActivity _body;
        private bool _willBodyExecute = true;

        public TestExpressionEvaluatorWithBody()
        {
            this.ProductActivity = new ExpressionEvaluatorWithBody<T>();
        }
        public TestExpressionEvaluatorWithBody(T constValue)
        {
            this.ProductActivity = new ExpressionEvaluatorWithBody<T>(constValue);
        }

        public T ExpressionResult
        {
            set
            {
                ((ExpressionEvaluatorWithBody<T>)this.ProductActivity).ExpressionResult = new InArgument<T>(value);
            }
        }

        public Expression<Func<ActivityContext, T>> ExpressionResultExpression
        {
            set
            {
                ((ExpressionEvaluatorWithBody<T>)this.ProductActivity).ExpressionResult = new InArgument<T>(value);
            }
        }

        public TestActivity Body
        {
            set
            {
                ((ExpressionEvaluatorWithBody<T>)this.ProductActivity).Body = value.ProductActivity;
                _body = value;
            }
            get
            {
                return _body;
            }
        }

        public bool WillBodyExecute
        {
            get { return _willBodyExecute; }
            set { _willBodyExecute = value; }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (Body != null)
            {
                yield return this.Body;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (WillBodyExecute)
            {
                CurrentOutcome = this.Body.GetTrace(traceGroup);
            }
        }
    }
}
