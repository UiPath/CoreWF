// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestParallelForEach<T> : TestLoop
    {
        private IEnumerable<T> _values;
        private TestActivity _valuesActivity;
        private TestActivity _expressionActivity;

        /// <summary>
        /// Wrapper on Product ParallelForEach.
        /// </summary>
        public TestParallelForEach()
        {
            this.ProductActivity = new ParallelForEach<T>();
        }

        public TestParallelForEach(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ParallelForEach<T> ProductParallelForEach
        {
            get
            {
                return (ParallelForEach<T>)this.ProductActivity;
            }
        }

        /// <summary>
        /// Collection of values to iterate.
        /// </summary>
        public IEnumerable<T> HintValues
        {
            get { return _values; }
            set
            {
                _values = value;
            }
        }

        public TestActivity ValuesActivity
        {
            set
            {
                _valuesActivity = value;
                if (value == null)
                {
                    this.ProductParallelForEach.Values = null;
                }
                else
                {
                    this.ProductParallelForEach.Values = (Activity<IEnumerable<T>>)(value.ProductActivity);
                }
            }
        }

        /// <summary>
        /// To pass variable of type IEnumerable we will use this property.
        /// </summary>
        public Variable<IEnumerable<T>> ValuesVariable
        {
            set
            {
                this.ProductParallelForEach.Values = new InArgument<IEnumerable<T>>(value);
            }
        }

        /// <summary>
        /// To pass lambda expression of type IEnumerable we will use this property.
        /// </summary>
        public Expression<Func<ActivityContext, IEnumerable<T>>> ValuesExpression
        {
            set
            {
                this.ProductParallelForEach.Values = new InArgument<IEnumerable<T>>(value);
            }
        }

        /// <summary>
        /// Completion condition which will execute after every branch execution
        /// if this is true then workflow will terminate further executuoin.
        /// </summary>
        private bool _completionCondition = false;
        public bool CompletionCondition
        {
            get
            {
                return _completionCondition;
            }
            set
            {
                this.ProductParallelForEach.CompletionCondition = new Literal<bool>(value);
                _completionCondition = value;
            }
        }

        /// <summary>
        /// To pass variable of type bool we will use this property.
        /// </summary>
        private Variable<bool> _completionCondVariable = new Variable<bool>() { Default = false };
        public Variable<bool> CompletionConditionVariable
        {
            get
            {
                return _completionCondVariable;
            }
            set
            {
                this.ProductParallelForEach.CompletionCondition = new VariableValue<bool>(value);
                _completionCondVariable = value;
            }
        }

        /// <summary>
        /// Completion condition is derived from Value expression so we can pass
        /// activity to it as well.
        /// </summary>
        public TestActivity ConditionValueExpression
        {
            set
            {
                if (value == null)
                {
                    this.ProductParallelForEach.CompletionCondition = null;
                    _expressionActivity = null;
                }
                else
                {
                    this.ProductParallelForEach.CompletionCondition = (Activity<bool>)(value.ProductActivity);
                    _expressionActivity = value;
                }
            }

            get
            {
                return _expressionActivity;
            }
        }

        /// <summary>
        /// To pass lambda expression we will use this property.
        /// </summary>
        public Expression<Func<ActivityContext, bool>> CompletionConditionExpression
        {
            set
            {
                this.ProductParallelForEach.CompletionCondition = new LambdaValue<bool>(value);
            }
        }

        /// <summary>
        /// Action we need to perform for each branch. 
        /// </summary>
        public TestActivity Body
        {
            get { return this.body; }
            set
            {
                if (value == null)
                {
                    this.body = null;
                    this.ProductParallelForEach.Body = null;
                    return;
                }

                if (this.ProductParallelForEach.Body == null)
                {
                    this.ProductParallelForEach.Body = new ActivityAction<T>();
                }

                this.ProductParallelForEach.Body.Handler = value.ProductActivity;
                this.body = value;
            }
        }

        /// <summary>
        /// Takes the variable of type T and in this variable we will have current item
        /// we are executing.
        /// </summary>

        private DelegateInArgument<T> _currentVariable = new DelegateInArgument<T>();
        public DelegateInArgument<T> CurrentVariable
        {
            get { return this.ProductParallelForEach.Body.Argument; }
            set
            {
                if (this.ProductParallelForEach.Body == null)
                {
                    this.ProductParallelForEach.Body = new ActivityAction<T>();
                }

                this.ProductParallelForEach.Body.Argument = value;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            UnorderedTraces parallelTraceGroup = new UnorderedTraces();

            if (this.HintIterationCount < 0 && _values == null)
            {
                return;
            }

            if (this.HintIterationCount < 0 && _values != null)
            {
                this.HintIterationCount = _values.Count<T>();
            }

            if (_valuesActivity != null)
            {
                Outcome conditionOutcome = _valuesActivity.GetTrace(traceGroup);

                if (conditionOutcome.DefaultPropogationState != OutcomeState.Completed)
                {
                    // propogate the unknown outcome upwards
                    this.CurrentOutcome = conditionOutcome;
                }
            }

            if (this.Body != null)
            {
                Outcome outc;

                for (int i = 1; i < _values.Count<T>() + 1; i++)
                {
                    OrderedTraces orderedTraceGroup = new OrderedTraces();

                    if (HintIterationCount < i)
                    {
                        TestDummyTraceActivity tdt = new TestDummyTraceActivity(Body.DisplayName)
                        {
                            ExpectedOutcome = Outcome.Canceled
                        };
                        tdt.GetTrace(orderedTraceGroup);
                    }
                    else
                    {
                        outc = this.Body.GetTrace(orderedTraceGroup);
                        if (this.ProductParallelForEach.CompletionCondition != null && outc.DefaultPropogationState != OutcomeState.Canceled)
                        {
                            TestDummyTraceActivity condition = new TestDummyTraceActivity(this.ProductParallelForEach.CompletionCondition, ConditionOutcome);
                            CurrentOutcome = condition.GetTrace(orderedTraceGroup);
                        }
                        if (outc.DefaultPropogationState != OutcomeState.Completed)
                        {
                            CurrentOutcome = outc;
                        }
                    }


                    parallelTraceGroup.Steps.Add(orderedTraceGroup);
                }

                traceGroup.Steps.Add(parallelTraceGroup);
            }
        }
    }
}
