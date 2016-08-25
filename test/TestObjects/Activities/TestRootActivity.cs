// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestRootActivity : TestActivity
    {
        private TestActivity _activity;
        private IList<Directive> _compensationHint;

        public TestRootActivity()
        {
            _compensationHint = new List<Directive>();
        }

        public TestActivity Activity
        {
            get { return _activity; }
            set
            {
                _activity = value;
                if (value == null)
                {
                    this.ProductActivity = null;
                }
                else
                {
                    this.ProductActivity = _activity.ProductActivity;
                    this.ProductActivity.DisplayName = _activity.DisplayName;
                }
            }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_activity != null)
            {
                yield return _activity;
            }
        }

        public override ExpectedTrace GetExpectedTrace()
        {
            this.ResetForValidation();

            OrderedTraces orderedTrace = new OrderedTraces();
            Outcome outcome = _activity.GetTrace(orderedTrace);
            ExpectedTrace baseTrace = new ExpectedTrace(orderedTrace);

            baseTrace.AddIgnoreTypes(typeof(WorkflowExceptionTrace));
            baseTrace.AddIgnoreTypes(typeof(WorkflowAbortedTrace));
            baseTrace.AddIgnoreTypes(typeof(SynchronizeTrace));
            baseTrace.AddIgnoreTypes(typeof(BookmarkResumptionTrace));

            bool compensate = outcome.DefaultPropogationState != OutcomeState.Completed;

            foreach (Directive directive in this.CompensationHint)
            {
                TestActivity target = _activity.FindChildActivity(directive.Name);

                if (compensate)
                {
                    target.GetCompensationTrace(baseTrace.Trace);
                }
                else
                {
                    target.GetConfirmationTrace(baseTrace.Trace);
                }
            }

            return baseTrace;
        }
    }

    //
    // Directives instruct the results building process on exactly what to do and when
    //

    // [Serializable]
    public class Directive
    {
        public const string Wildcard = "*";
        public const string Confirm = "Confirm";
        public const string Compensate = "Compensate";

        private string _name;
        private string _action;

        public Directive(string name)
            : this(name, Directive.Wildcard)
        {
        }

        public Directive(string name, string action)
        {
            _name = name;
            _action = action;
        }

        public string Name
        {
            get { return _name; }
        }
        public string Action
        {
            get { return _action; }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0},{1}]", _name, _action);
        }
    }

    //
    // ActivityAnchor is a helper to be used along-side FindChildActivity when
    // the activity invoking FindChildActivity needs to potentially find it's siblings
    //

    public class ActivityAnchor
    {
        public TestActivity Activity
        {
            get;
            set;
        }
    }
}
