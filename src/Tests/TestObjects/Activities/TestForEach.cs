// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestForEach<T> : TestLoop
    {
        public TestForEach()
        {
            this.ProductActivity = new ForEach<T>();
        }

        public TestForEach(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ForEach<T> ProductForEach
        {
            get
            {
                return (ForEach<T>)this.ProductActivity;
            }
        }

        public IEnumerable<T> ValuesT
        {
            set
            {
                _values = value;
                this.ProductForEach.Values = new InArgument<IEnumerable<T>>(context => GetValues());
            }
        }

        public IEnumerable Values
        {
            set
            {
                //TestParameters.DisableXamlRoundTrip = true;
                _values = value;
                this.ProductForEach.Values = new InArgument<IEnumerable<T>>(context => GetValues());
            }
        }

        public IEnumerable<T> GetValues()
        {
            return (IEnumerable<T>)_values;
        }

        public Expression<Func<ActivityContext, IEnumerable<T>>> ValuesExpression
        {
            set
            {
                this.ProductForEach.Values = new InArgument<IEnumerable<T>>(value);
            }
        }


        public TestActivity ValuesActivity
        {
            set
            {
                _valuesActivity = value;
                if (value == null)
                {
                    this.ProductForEach.Values = null;
                }
                else
                {
                    this.ProductForEach.Values = (Activity<IEnumerable<T>>)(value.ProductActivity);
                }
            }
        }

        public Variable<IEnumerable> ValuesVariable
        {
            set
            {
                this.ProductForEach.Values = new InArgument<IEnumerable<T>>(value);
            }
        }

        public Variable<IEnumerable<T>> ValuesVariableT
        {
            set
            {
                this.ProductForEach.Values = new InArgument<IEnumerable<T>>(value);
            }
        }

        public TestActivity Body
        {
            get
            {
                return this.body;
            }
            set
            {
                if (value == null)
                {
                    this.ProductForEach.Body = null;
                    this.body = null;
                    return;
                }

                body = value;

                if (this.ProductForEach.Body == null)
                {
                    this.ProductForEach.Body = new ActivityAction<T>();
                }
                this.ProductForEach.Body.Handler = value.ProductActivity;
            }
        }

        public DelegateInArgument CurrentVariable
        {
            set
            {
                if (this.ProductForEach.Body == null)
                {
                    this.ProductForEach.Body = new ActivityAction<T>();
                }
                this.ProductForEach.Body.Argument = (DelegateInArgument<T>)value;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (this.HintIterationCount < 0)
            {
                return;
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

            if (this.body != null)
            {
                for (int counter = 0; counter < HintIterationCount; counter++)
                {
                    Outcome childOut = body.GetTrace(traceGroup);

                    if (childOut.DefaultPropogationState != OutcomeState.Completed)
                    {
                        CurrentOutcome = childOut;
                        break;
                    }
                }
            }
        }

        private TestActivity _valuesActivity;
        private IEnumerable _values;
    }
}
