// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestSwitch<T> : TestActivity
    {
        private TestActivity _expression;

        public TestSwitch()
        {
            _caseBodies = new List<TestActivity>();
            _hints = new List<int>();
            this.ProductActivity = new Switch<T>();
        }

        public TestSwitch(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public T Expression
        {
            set { this.ProductSwitch.Expression = new InArgument<T>(value); }
        }

        public TestActivity ExpressionActivity
        {
            set
            {
                if (value == null)
                {
                    _expression = null;
                    this.ProductSwitch.Expression = null;
                }
                else
                {
                    _expression = value;
                    this.ProductSwitch.Expression = new InArgument<T>(value.ProductActivity as Activity<T>);
                }
            }
        }

        public Variable<T> ExpressionVariable
        {
            set { this.ProductSwitch.Expression = new InArgument<T>(value); }
        }

        public Expression<Func<ActivityContext, T>> ExpressionExpression
        {
            set { this.ProductSwitch.Expression = new InArgument<T>(value); }
        }

        public TestActivity Default
        {
            get
            {
                return _defaultCase;
            }
            set
            {
                if (value == null)
                {
                    this.ProductSwitch.Default = null;
                }
                else
                {
                    this.ProductSwitch.Default = value.ProductActivity;
                }

                _defaultCase = value;
            }
        }

        public Switch<T> ProductSwitch
        {
            get
            {
                return ((Switch<T>)this.ProductActivity);
            }
        }

        public List<TestActivity> CaseBodies
        {
            get
            {
                return _caseBodies;
            }
        }

        public List<int> Hints
        {
            get
            {
                return _hints;
            }
        }

        override internal IEnumerable<TestActivity> GetChildren()
        {
            foreach (TestActivity act in CaseBodies)
            {
                yield return act;
            }

            if (Default != null)
            {
                yield return Default;
            }
        }

        public void AddCase(T expression, TestActivity body)
        {
            this.ProductSwitch.Cases.Add(expression, body.ProductActivity);
            _caseBodies.Add(body);
        }

        public void RemoveCaseAt(T expression, int idx)
        {
            this.ProductSwitch.Cases.Remove(expression);
            _caseBodies.RemoveAt(idx);
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (_expression != null)
            {
                Outcome conditionOutcome = _expression.GetTrace(traceGroup);

                if (conditionOutcome.DefaultPropogationState != OutcomeState.Completed)
                {
                    // propogate the unknown outcome upwards
                    this.CurrentOutcome = conditionOutcome;
                }
            }

            if (this.CurrentCase == -1)
            {
                if (_defaultCase != null)
                {
                    CurrentOutcome = _defaultCase.GetTrace(traceGroup);
                }
            }
            else if (_caseBodies[this.CurrentCase] != null)
            {
                CurrentOutcome = _caseBodies[this.CurrentCase].GetTrace(traceGroup);
            }
        }

        private int CurrentCase
        {
            get
            {
                if (this.Hints.Count == 0 || (this.Hints.Count != 1 && this.Hints.Count == this.iterationNumber))
                {
                    throw new InvalidOperationException("The default case execution or no case execution should be hinted as -1");
                }
                return this.Hints[this.Hints.Count == 1 ? 0 : this.iterationNumber];
            }
        }
        private readonly List<int> _hints; // List of hints of executing cases in switch. -1 is for default case.
        private List<TestActivity> _caseBodies;
        private TestActivity _defaultCase;
    }
}
