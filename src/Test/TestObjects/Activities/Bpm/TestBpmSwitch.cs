// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public abstract class TestBpmSwitchBase : TestBpmElement
    {
        private List<int> _hints; // List of hints of executing element in switch. -1 is for default element.
        private int _iterationNumber = 0;

        protected BpmNode productFlowSwitch;
        protected TestActivity expressionActivity;
        protected List<TestBpmElement> caseElements;
        protected TestBpmElement defaultElement;
        protected ExpressionType expressionType;

        internal IEnumerable<TestActivity> GetChildren<T>()
        {
            switch (expressionType)
            {
                case ExpressionType.Activity:
                    yield return expressionActivity;
                    break;
                case ExpressionType.Literal:
                    yield return new TestDummyTraceActivity(typeof(Literal<T>), Outcome.Completed);
                    break;

                case ExpressionType.VisualBasicValue:
                    yield return new TestDummyTraceActivity(typeof(LambdaValue<T>), Outcome.Completed);
                    break;

                case ExpressionType.VariableValue:
                    yield return new TestDummyTraceActivity(typeof(VariableValue<T>), Outcome.Completed);
                    break;

                default: break;
            }
            var element = GetNextElement();
            if (element != null)
            {
                yield return element;
            }
        }

        internal override TestBpmElement GetNextElement()
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

        public override BpmNode GetProductElement()
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


    public class TestBpmSwitch<T> : TestBpmSwitchBase
    {
        public TestBpmSwitch()
        {
            this.productFlowSwitch = new BpmSwitch<T>();
            this.caseElements = new List<TestBpmElement>();
        }

        public TestBpmElement Default
        {
            get { return this.defaultElement; }
            set
            {
                this.defaultElement = value;
                (this.productFlowSwitch as BpmSwitch<T>).Default = value.GetProductElement();
            }
        }

        internal T Expression
        {
            set
            {
                (this.productFlowSwitch as BpmSwitch<T>).Expression = new Literal<T>(value);
                expressionType = ExpressionType.Literal;
            }
        }

        internal Expression<Func<ActivityContext, T>> LambdaExpression
        {
            set
            {
                (this.productFlowSwitch as BpmSwitch<T>).Expression = new LambdaValue<T>(value);
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
                    (this.productFlowSwitch as BpmSwitch<T>).Expression = null;
                }
                else
                {
                    (this.productFlowSwitch as BpmSwitch<T>).Expression = (Activity<T>)(value.ProductActivity);
                }
            }
        }

        internal Variable<T> ExpressionVariable
        {
            set
            {
                (this.productFlowSwitch as BpmSwitch<T>).Expression = new VariableValue<T>(value);
                expressionType = ExpressionType.VariableValue;
            }
        }

        internal void AddCase(T expression, TestBpmElement element)
        {
            (this.productFlowSwitch as BpmSwitch<T>).Cases.Add(expression, element == null ? null : element.GetProductElement());
            this.caseElements.Add(element);
        }

        /// <summary>
        /// Update the activity to execute of the given caseExpression/caseIndex
        /// </summary>
        /// <param name="caseExpression">used for locating the case in the product</param>
        /// <param name="caseIndex">used for locating the case in the test object</param>
        /// <param name="newElement">new node to be added to BpmSwitch</param>
        internal void UpdateCase(T caseExpression, int caseIndex, TestBpmElement newElement)
        {
            if (caseIndex < 0 || caseIndex >= this.caseElements.Count)
            {
                throw new ArgumentException("Given caseIndex is out of range.");
            }

            if (caseExpression == null)
            {
                throw new ArgumentException("Given caseExpression is null.");
            }

            if (!(this.productFlowSwitch as BpmSwitch<T>).Cases.ContainsKey(caseExpression))
            {
                throw new ArgumentException("Given caseExpression cannot be found in the set of cases.");
            }

            (this.productFlowSwitch as BpmSwitch<T>).Cases[caseExpression] = (newElement == null) ? null : newElement.GetProductElement();

            this.caseElements.RemoveAt(caseIndex);
            this.caseElements.Insert(caseIndex, newElement);
        }
        internal override IEnumerable<TestActivity> GetChildren() => GetChildren<T>();
        public void AddNullCase(TestActivity activity)
        {
            TestBpmStep step = null;
            BpmNode node = null;

            if (activity != null)
            {
                step = new TestBpmStep(activity);
                node = step.GetProductElement();
            }
            (this.productFlowSwitch as BpmSwitch<T>).Cases.Add(default(T), node);
            this.caseElements.Add(step);
        }
    }
}
