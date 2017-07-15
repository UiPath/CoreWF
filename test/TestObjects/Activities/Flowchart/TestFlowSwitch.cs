// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoreWf;
using CoreWf.Statements;
using CoreWf.Expressions;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public abstract class TestFlowSwitchBase : TestFlowElement
    {
        private List<int> _hints; // List of hints of executing element in switch. -1 is for default element.
        private int _iterationNumber = 0;

        protected FlowNode productFlowSwitch;
        protected TestActivity expressionActivity;
        protected List<TestFlowElement> caseElements;
        protected TestFlowElement defaultElement;
        protected ExpressionType expressionType;

        protected Outcome GetTrace<T>(TraceGroup traceGroup)
        {
            Outcome outcome = Outcome.Completed;

            switch (expressionType)
            {
                case ExpressionType.Activity:
                    outcome = this.expressionActivity.GetTrace(traceGroup);

                    if (outcome.DefaultPropogationState != OutcomeState.Completed)
                    {
                        return outcome;
                    }
                    break;

                case ExpressionType.Literal:
                    new TestDummyTraceActivity(typeof(Literal<T>), Outcome.Completed).GetTrace(traceGroup);
                    break;

                case ExpressionType.VisualBasicValue:
                    new TestDummyTraceActivity(typeof(LambdaValue<T>), Outcome.Completed).GetTrace(traceGroup);
                    break;

                case ExpressionType.VariableValue:
                    new TestDummyTraceActivity(typeof(VariableValue<T>), Outcome.Completed).GetTrace(traceGroup);
                    break;

                default: break;
            }

            TestFlowElement element = GetNextElement();
            if (element != null)
            {
                return element.GetTrace(traceGroup);
            }
            return Outcome.Completed;
        }

        internal override TestFlowElement GetNextElement()
        {
            if (_hints.Count == 0 || _hints.Count == _iterationNumber)
            {
                throw new Exception("The default element execution or no case execution should be hinted as -1");
            }
            if (_hints[_iterationNumber] == -1)
            {
                _iterationNumber++;
                return this.defaultElement;
            }
            return this.caseElements[_hints[_iterationNumber++]];
        }

        public override FlowNode GetProductElement()
        {
            return this.productFlowSwitch;
        }

        internal void SetHints(List<int> hints)
        {
            _hints = new List<int>(hints);
        }

        internal void ResetIterationNumber()
        {
            _iterationNumber = 0;
        }
    }


    public class TestFlowSwitch<T> : TestFlowSwitchBase
    {
        public TestFlowSwitch()
        {
            this.productFlowSwitch = new FlowSwitch<T>();
            this.caseElements = new List<TestFlowElement>();
        }

        public string DisplayName
        {
            set
            {
                (this.productFlowSwitch as FlowSwitch<T>).DisplayName = value;
            }
        }

        public TestFlowElement Default
        {
            get { return this.defaultElement; }
            set
            {
                this.defaultElement = value;
                (this.productFlowSwitch as FlowSwitch<T>).Default = value.GetProductElement();
            }
        }

        internal T Expression
        {
            set
            {
                (this.productFlowSwitch as FlowSwitch<T>).Expression = new Literal<T>(value);
                expressionType = ExpressionType.Literal;
            }
        }

        internal Expression<Func<ActivityContext, T>> LambdaExpression
        {
            set
            {
                (this.productFlowSwitch as FlowSwitch<T>).Expression = new LambdaValue<T>(value);
                expressionType = ExpressionType.VisualBasicValue;
            }
        }

        internal TestActivity ExpressionActivity
        {
            set
            {
                expressionType = ExpressionType.Activity;
                this.expressionActivity = value;
                if (value == null)
                {
                    (this.productFlowSwitch as FlowSwitch<T>).Expression = null;
                }
                else
                {
                    (this.productFlowSwitch as FlowSwitch<T>).Expression = (Activity<T>)(value.ProductActivity);
                }
            }
        }

        internal Variable<T> ExpressionVariable
        {
            set
            {
                (this.productFlowSwitch as FlowSwitch<T>).Expression = new VariableValue<T>(value);
                expressionType = ExpressionType.VariableValue;
            }
        }

        internal void AddCase(T expression, TestFlowElement element)
        {
            (this.productFlowSwitch as FlowSwitch<T>).Cases.Add(expression, element == null ? null : element.GetProductElement());
            this.caseElements.Add(element);
        }

        /// <summary>
        /// Update the activity to execute of the given caseExpression/caseIndex
        /// </summary>
        /// <param name="caseExpression">used for locating the case in the product</param>
        /// <param name="caseIndex">used for locating the case in the test object</param>
        /// <param name="newElement">new node to be added to FlowSwitch</param>
        internal void UpdateCase(T caseExpression, int caseIndex, TestFlowElement newElement)
        {
            if (caseIndex < 0 || caseIndex >= this.caseElements.Count)
            {
                throw new ArgumentException("Given caseIndex is out of range.");
            }

            if (caseExpression == null)
            {
                throw new ArgumentException("Given caseExpression is null.");
            }

            if (!(this.productFlowSwitch as FlowSwitch<T>).Cases.ContainsKey(caseExpression))
            {
                throw new ArgumentException("Given caseExpression cannot be found in the set of cases.");
            }

            (this.productFlowSwitch as FlowSwitch<T>).Cases[caseExpression] = (newElement == null) ? null : newElement.GetProductElement();

            this.caseElements.RemoveAt(caseIndex);
            this.caseElements.Insert(caseIndex, newElement);
        }

        internal override Outcome GetTrace(TraceGroup traceGroup)
        {
            return base.GetTrace<T>(traceGroup);
        }

        public void AddNullCase(TestActivity activity)
        {
            TestFlowStep step = null;
            FlowNode node = null;

            if (activity != null)
            {
                step = new TestFlowStep(activity);
                node = step.GetProductElement();
            }
            (this.productFlowSwitch as FlowSwitch<T>).Cases.Add(default(T), node);
            this.caseElements.Add(step);
        }
    }
}
