// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestConfirm : TestActivity
    {
        private ActivityAnchor _anchor;
        private Variable<CompensationToken> _target;
        private bool _disableTrace = false;

        private IList<Directive> _confirmationHint;

        public TestConfirm()
            : this("TestConfirm")
        {
        }

        public TestConfirm(string displayName)
        {
            this.ProductActivity = new Confirm();
            this.DisplayName = displayName;

            _confirmationHint = new List<Directive>();
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
                    ProductConfirm.Target = _target;
                }
                else
                {
                    ProductConfirm.Target = null;
                }
            }
        }

        public bool DisableTrace
        {
            get { return _disableTrace; }
            set { _disableTrace = value; }
        }

        public IList<Directive> ConfirmationHint
        {
            get { return _confirmationHint; }
            set { _confirmationHint = value; }
        }

        private Confirm ProductConfirm
        {
            get { return (Confirm)this.ProductActivity; }
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
                    target.GetConfirmationTrace(traceGroup);
                }
                else
                {
                    foreach (Directive directive in _confirmationHint)
                    {
                        TestActivity target = _anchor.Activity.FindChildActivity(directive.Name);
                        target.GetConfirmationTrace(traceGroup);
                    }
                }
            }
        }
    }
}
