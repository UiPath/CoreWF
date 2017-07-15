// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestCompensate : TestActivity
    {
        private ActivityAnchor _anchor;
        private Variable<CompensationToken> _target;
        private bool _disableTrace = false;

        private IList<Directive> _compensationHint;

        public TestCompensate()
            : this("TestCompensate")
        {
        }

        public TestCompensate(string displayName)
        {
            this.ProductActivity = new Compensate();
            this.DisplayName = displayName;

            _compensationHint = new List<Directive>();
        }

        public ActivityAnchor Anchor
        {
            get { return _anchor; }
            set { _anchor = value; }
        }

        public Variable<CompensationToken> Target
        {
            get { return _target; }
            set
            {
                _target = value;
                if (_target != null)
                {
                    ProductCompensate.Target = _target;
                }
                else
                {
                    ProductCompensate.Target = null;
                }
            }
        }

        public bool DisableTrace
        {
            get { return _disableTrace; }
            set { _disableTrace = value; }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        private Compensate ProductCompensate
        {
            get { return (Compensate)this.ProductActivity; }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // Use the anchor as the starting point for the child search
            if (_anchor != null && !_disableTrace)
            {
                // If the target was valid, use that one. Otherwise we need the hints to determine what to do
                if (_target != null)
                {
                    TestActivity target = _anchor.Activity.FindChildActivity(_target.Name);
                    target.GetCompensationTrace(traceGroup);
                }
                else
                {
                    foreach (Directive directive in _compensationHint)
                    {
                        TestActivity target = _anchor.Activity.FindChildActivity(directive.Name);
                        target.GetCompensationTrace(traceGroup);
                    }
                }
            }
        }
    }
}
