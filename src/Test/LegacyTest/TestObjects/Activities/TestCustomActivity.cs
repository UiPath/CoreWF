// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;
using LegacyTest.Test.Common.TestObjects.Utilities;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestCustomActivity : TestActivity
    {
        internal List<TestActivity> childActivities;
        protected List<Variable> variables = new List<Variable>();

        // TestCustomActivityDesign createdFrom = null;

        private readonly List<WorkflowTraceStep> _thisActivitySpecificTraces = new List<WorkflowTraceStep>();

        public List<WorkflowTraceStep> CustomActivityTraces
        {
            get
            {
                return _thisActivitySpecificTraces;
            }
        }

        public TestCustomActivity()
        {
            this.childActivities = new List<TestActivity>();
        }

        public TestCustomActivity(string displayName)
            : this()
        {
            if (displayName != null)
            {
                this.DisplayName = displayName;
            }
            if (this.childActivities != null)
            {
                this.childActivities = new List<TestActivity>();
            }
        }

        //public TestCustomActivityDesign CreatedFrom
        //{
        //    get { return this.createdFrom; }
        //    set { this.createdFrom = value; }
        //}

        public static TestCustomActivity CreateFromProduct(Activity customActivity, string displayName = null)
        {
            TestCustomActivity testCustomActivity = new TestCustomActivity
            {
                ProductActivity = customActivity
            };
            if (!String.IsNullOrEmpty(displayName))
            {
                testCustomActivity.DisplayName = displayName;
            }

            return testCustomActivity;
        }


        internal override IEnumerable<TestActivity> GetChildren()
        {
            return this.childActivities;
        }

        internal IEnumerable<Variable> GetEnvironmentVariables()
        {
            return this.variables;
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            base.GetActivitySpecificTrace(traceGroup);
        }

        protected override void GetFaultTrace(TraceGroup traceGroup)
        {
            base.GetFaultTrace(traceGroup);
        }

        private object GetProperty(string propertyName)
        {
            PropertyInfo propertyInfo = null;
            // (this.CreatedFrom.CustomActivityType).GetProperty(propertyName);

            if (propertyInfo == null)
            {
                throw new Exception("Unknown property name: " + propertyName);
            }

            return propertyInfo.GetValue(this.ProductActivity, null);
        }

        public void SetProperty(string propertyName, object value)
        {
            Type type = this.ProductActivity.GetType();
            PropertyInfo propertyInfo = (type).GetProperty(propertyName);

            if (propertyInfo == null)
            {
                throw new Exception("Unknown property name: " + propertyName);
            }

            propertyInfo.SetValue(this.ProductActivity, value, null);
        }
    }


    public class TestCustomActivity<TActivityType> : TestCustomActivity
       where TActivityType : Activity
    {
        public TestCustomActivity()
            : this(null, null)
        {
        }

        public TestCustomActivity(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestCustomActivity(string displayName, params object[] args)
        {
            this.ProductActivity = (Activity)Activator.CreateInstance(typeof(TActivityType), args);
            if (displayName != null)
            {
                this.DisplayName = displayName;
            }
            this.childActivities = new List<TestActivity>();
        }

        public TActivityType Activity
        {
            get { return (TActivityType)this.ProductActivity; }
        }

        public static TestCustomActivity<TActivityType> CreateFromProduct(TActivityType customActivity)
        {
            TestCustomActivity<TActivityType> testCustomActivity = new TestCustomActivity<TActivityType>();
            string tempDisplayName = customActivity.DisplayName;
            testCustomActivity.ProductActivity = customActivity;
            testCustomActivity.DisplayName = tempDisplayName;
            return testCustomActivity;
        }

        private object GetProperty(string propertyName)
        {
            PropertyInfo propertyInfo = typeof(TActivityType).GetProperty(propertyName);

            if (propertyInfo == null)
            {
                throw new Exception("Unknown property name: " + propertyName);
            }

            return propertyInfo.GetValue(this.ProductActivity, null);
        }

        public new void SetProperty(string propertyName, object value)
        {
            PropertyInfo propertyInfo = typeof(TActivityType).GetProperty(propertyName);

            if (propertyInfo == null)
            {
                throw new Exception("Unknown property name: " + propertyName);
            }

            propertyInfo.SetValue(this.ProductActivity, value, null);
        }

        public void AddActivity(string propertyName, TestActivity activity)
        {
            MethodInfo methodInfo = typeof(IList).GetMethod("Add");
            object productProperty = GetProperty(propertyName);

            if (productProperty == null)
            {
                throw new Exception("Property " + propertyName + " is null. ");
            }

            // Add activity to actual product activity
            PartialTrustMethodInfo.Invoke(methodInfo, productProperty, new object[] { activity.ProductActivity });

            this.childActivities.Add(activity);
        }

        public void SetActivity(string propertyName, TestActivity activity)
        {
            SetProperty(propertyName, activity.ProductActivity);
            this.childActivities.Add(activity);
        }

        public void AddVariable(string propertyName, Variable variable)
        {
            MethodInfo methodInfo = typeof(IList).GetMethod("Add");
            object productProperty = GetProperty(propertyName);

            if (productProperty == null)
            {
                throw new Exception("Property " + propertyName + " is null. ");
            }

            // Add variable to the actual product activity
            PartialTrustMethodInfo.Invoke(methodInfo, productProperty, new object[] { variable });
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            base.GetActivitySpecificTrace(traceGroup);

            if (ExpectedOutcome.DefaultPropogationState == OutcomeState.Completed || ExpectedOutcome.DefaultPropogationState == OutcomeState.Faulted)
            {
                foreach (WorkflowTraceStep step in CustomActivityTraces)
                {
                    traceGroup.Steps.Add(step);
                }
            }
            return;
        }
    }
}
