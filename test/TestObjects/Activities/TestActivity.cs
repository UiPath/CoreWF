// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public abstract class TestActivity
    {
        protected int iterationNumber = 0;
        private static int s_nameCount = 1;
        private Activity _productActivity = null;
        private readonly List<WorkflowTraceStep> _customActivitySpecificTraces = new List<WorkflowTraceStep>();

        // For transition period this needs to point to uses old tracing
        //  Afterwards should be changed to always Complete
        private Outcome _expectedOutcome = Outcome.Completed;

        // This is the state the object is in during evaluation of expected trace.
        protected Outcome CurrentOutcome;

        public Outcome ExpectedOutcome
        {
            get
            {
                return _expectedOutcome;
            }
            set
            {
                _expectedOutcome = value;
            }
        }

        public Activity ProductActivity
        {
            get { return _productActivity; }
            protected internal set
            {
                _productActivity = value;
                GenerateDefaultDisplayName(_productActivity);
            }
        }

        public void ReplaceLambdasAndCompileVBInProductActivity()
        {
            //this.productActivity = this.productActivity;
            // PartialTrustExpressionHelper.ReplaceLambdasAndCompileVB(this.productActivity);
        }

        internal void SetProductActivityBypassGenerateDisplayName(Activity workflowElement)
        {
            _productActivity = workflowElement;
        }

        public virtual string DisplayName
        {
            get { return this.ProductActivity.DisplayName; }
            set { this.ProductActivity.DisplayName = value; }
        }

        // public XamlTestDriver.ModifyXaml ModifyXamlDelegate
        // {
        //     get;
        //     set;
        // }

        public IList<WorkflowTraceStep> ActivitySpecificTraces
        {
            get
            {
                return _customActivitySpecificTraces;
            }
        }

        private void GenerateDefaultDisplayName(Activity productActivity)
        {
            if (productActivity == null)
            {
                throw new ArgumentNullException("productActivity");
            }

            this.DisplayName = productActivity.GetType().Name + TestActivity.s_nameCount++;
        }

        public TestActivity FindChildActivity(string targetDisplayName)
        {

            if (!TryFindChildActivity(targetDisplayName, out TestActivity found))
            {
                throw new Exception(
                    string.Format("Could not find any TestActivity with DisplayName='{0}'",
                    targetDisplayName));
            }

            return found;
        }

        private bool TryFindChildActivity(string targetDisplayName, out TestActivity found)
        {
            if (this.DisplayName == targetDisplayName)
            {
                found = this;
                return true;
            }

            foreach (TestActivity candidate in this.GetChildren())
            {
                if (candidate.TryFindChildActivity(targetDisplayName, out found))
                {
                    return true;
                }
            }

            found = null;
            return false;
        }

        public virtual ExpectedTrace GetExpectedTrace()
        {
            this.ResetForValidation();

            OrderedTraces orderedTrace = new OrderedTraces();
            GetTrace(orderedTrace);
            ExpectedTrace expected = new ExpectedTrace(orderedTrace);
            AddIgnoreTypes(expected);
            return expected;
        }

        protected ExpectedTrace GetExpectedTraceUnordered()
        {
            this.ResetForValidation();

            OrderedTraces orderedTrace = new OrderedTraces();
            GetTrace(orderedTrace);
            ExpectedTrace expected = new ExpectedTrace(orderedTrace);

            AddIgnoreTypes(expected);

            return expected;
        }

        private void AddIgnoreTypes(ExpectedTrace expected)
        {
            // always ignore WorkflowInstanceTraces
            expected.AddIgnoreTypes(typeof(WorkflowExceptionTrace));
            expected.AddIgnoreTypes(typeof(WorkflowAbortedTrace));
            expected.AddIgnoreTypes(typeof(SynchronizeTrace));
            expected.AddIgnoreTypes(typeof(BookmarkResumptionTrace));
        }

        internal Outcome GetTrace(TraceGroup traceGroup)
        {
            // if None add nothing to trace
            if (ExpectedOutcome.DefaultPropogationState != OutcomeState.None)
            {
                // Add the executing state
                traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Executing));
            }

            // Set current state to the expected, this lets children override and return another result
            this.CurrentOutcome = this.ExpectedOutcome;

            // add activity specific traces
            if (this.ActivitySpecificTraces.Count != 0)
            {
                // allow to specify a single trace when all 
                // iterations should have the same trace
                if (this.ActivitySpecificTraces.Count == 1)
                {
                    traceGroup.Steps.Add(this.ActivitySpecificTraces[0]);
                }
                else if (this.iterationNumber < this.ActivitySpecificTraces.Count)
                {
                    traceGroup.Steps.Add(this.ActivitySpecificTraces[this.iterationNumber]);
                }
            }
            else
            {
                GetActivitySpecificTrace(traceGroup);
            }

            // This allows you to manually override the outcome.
            //  If the outcome is overridable then we return the current state, which could be different at runtime.
            //  This is a cheap mechanism to allow us to both handle faults, etc without setting this everywhere,
            //  but also allow us to tell to not to override in some cases.
            if (!ExpectedOutcome.IsOverrideable)
            {
                CurrentOutcome = ExpectedOutcome;
            }

            // All activities should complete with one of the following
            switch (CurrentOutcome.DefaultPropogationState)
            {
                case OutcomeState.Completed:
                    GetCloseTrace(traceGroup);
                    break;
                case OutcomeState.Canceled:
                    GetCancelTrace(traceGroup);
                    break;
                case OutcomeState.Faulted:
                    GetFaultTrace(traceGroup);
                    break;
                default:
                    break;
            }

            this.iterationNumber++;
            return this.CurrentOutcome.Propogate();
        }

        protected virtual void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            foreach (TestActivity child in GetChildren())
            {
                if (child != null)
                {
                    Outcome childOutcome = child.GetTrace(traceGroup);
                    if (childOutcome.DefaultPropogationState != OutcomeState.Completed)
                    {
                        // if child didnt complete
                        // propogate the unknown outcome upwards
                        this.CurrentOutcome = childOutcome;
                        break;
                    }
                }
            }
        }

        protected virtual void GetFaultTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Faulted));
            //A handled exception, therefore we should output a workflow instance trace
            if (CurrentOutcome is HandledExceptionOutcome)
            {
                traceGroup.Steps.Add(new WorkflowInstanceTrace(WorkflowInstanceState.UnhandledException));
            }
        }

        protected virtual void GetCancelTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
        }

        protected virtual void GetCloseTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Closed));
        }

        internal virtual IEnumerable<TestActivity> GetChildren()
        {
            return new List<TestActivity>();
        }

        internal virtual void ResetForValidation()
        {
            this.iterationNumber = 0;

            foreach (TestActivity childActivity in this.GetChildren())
            {
                if (childActivity != null)
                {
                    childActivity.ResetForValidation();
                }
            }
        }

        internal virtual void GetCompensationTrace(TraceGroup traceGroup)
        {
            // Only activities which affect Compensation/Confirmation should override this
        }
        internal virtual void GetConfirmationTrace(TraceGroup traceGroup)
        {
            // Only activities which affect Compensation/Confirmation should override this
        }

        protected void TrackPersistence(TraceGroup traceGroup)
        {
            // Persistence tracking event does not occur if there are no PersistenceProviders hooked up
            //if (TestParameters.PersistenceProviderFactoryType != null)
            //{
            traceGroup.Steps.Add(new WorkflowInstanceTrace(WorkflowInstanceState.Persisted));
            //}
        }

        #region Obsolete

        public bool HintAborted
        {
            set
            {
                if (value)
                {
                    this.ExpectedOutcome = Outcome.Faulted;
                }
            }
        }

        public bool HintCanceled
        {
            set
            {
                if (value)
                {
                    this.ExpectedOutcome = Outcome.Canceled;
                }
            }
        }

        public Type HintExceptionThrown
        {
            set
            {
                if (value != null)
                {
                    this.ExpectedOutcome = new UncaughtExceptionOutcome(value);
                }
            }
        }

        #endregion
    }
}
