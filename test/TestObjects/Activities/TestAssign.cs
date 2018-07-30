// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public class TestAssign<T> : TestActivity
    {
        private TestActivity _locationActivity;
        private TestActivity _valueActivity;

        public TestAssign()
        {
            this.ProductActivity = new Assign<T>();
        }

        public TestAssign(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        private Assign<T> ProductAssign
        {
            get
            {
                return (Assign<T>)this.ProductActivity;
            }
        }

        // Assign<T>.To
        public Variable<T> ToVariable
        {
            set { this.ProductAssign.To = new OutArgument<T>(value); }
        }

        public Expression<Func<ActivityContext, T>> ToExpression
        {
            set { this.ProductAssign.To = new OutArgument<T>(value); }
        }

        public TestActivity ToLocation
        {
            set
            {

                if (!(value.ProductActivity is Activity<Location<T>> we))
                {
                    throw new Exception("TestActivity should be for Activity<Location<T>> for conversion");
                }

                this.ProductAssign.To = we;
                _locationActivity = value;
            }
        }

        // Assign<T>.Value
        public T Value
        {
            set { this.ProductAssign.Value = new InArgument<T>(value); }
        }

        public Variable<T> ValueVariable
        {
            set { this.ProductAssign.Value = new InArgument<T>(value); }
        }

        public Expression<Func<ActivityContext, T>> ValueExpression
        {
            set { this.ProductAssign.Value = new InArgument<T>(value); }
        }

        public TestActivity ValueActivity
        {
            set
            {
                this.ProductAssign.Value = new InArgument<T>((Activity<T>)value.ProductActivity);
                _valueActivity = value;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_valueActivity != null && !(_valueActivity.ExpectedOutcome == Outcome.None))
                yield return _valueActivity;

            if (_locationActivity != null && !(_locationActivity.ExpectedOutcome == Outcome.None))
                yield return _locationActivity;
        }
    }
}
