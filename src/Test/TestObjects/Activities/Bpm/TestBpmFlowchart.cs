// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Activities;
using System.Activities.Statements;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestBpmFlowchart : TestActivity
    {
        public bool HintExceptionWillBeHandled = false;
        private MemberCollection<TestBpmElement> _elements;
        private IList<Directive> _compensationHint;
        private TestBpmElement _startElement;

        public TestBpmFlowchart()
        {
            this.ProductActivity = new BpmFlowchart();
            _elements = new MemberCollection<TestBpmElement>(AddFlowElement)
            {
                RemoveItem = RemoveFlowElementItem,
                RemoveAtItem = RemoveFlowElementAt
            };
            _compensationHint = new List<Directive>();
        }

        public TestBpmFlowchart(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public IList<Variable> Variables
        {
            get
            {
                return this.ProductFlowchart.Variables;
            }
        }

        public MemberCollection<TestBpmElement> Elements
        {
            get { return _elements; }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        private BpmFlowchart ProductFlowchart
        {
            get
            {
                return (BpmFlowchart)this.ProductActivity;
            }
        }

        public bool ValidateUnconnectedNodes
        {
            get
            {
                return this.ProductFlowchart.ValidateUnconnectedNodes;
            }
            set
            {
                this.ProductFlowchart.ValidateUnconnectedNodes = value;
            }
        }

        private void SetStartNode(TestBpmElement startElement)
        {
            _startElement = startElement;
            ((BpmFlowchart)ProductActivity).StartNode = startElement.ProductActivity;
        }

        protected void AddFlowElement(TestBpmElement item)
        {
            if (_startElement == null)
            {
                SetStartNode(item);
            }
            ((BpmFlowchart)ProductActivity).Nodes.Add(item.ProductActivity);
        }

        protected bool RemoveFlowElementItem(TestBpmElement item)
        {
            return ((BpmFlowchart)ProductActivity).Nodes.Remove(item.ProductActivity);
        }

        protected void RemoveFlowElementAt(int index)
        {
            ((BpmFlowchart)ProductActivity).Nodes.RemoveAt(index);
        }

        // This overload is for more complicated scenarios
        private TestBpmElement AddTestFlowLink(TestBpmElement flowchartElement)
        {
            if (!_elements.Contains(flowchartElement))
            {
                _elements.Add(flowchartElement);
            }
            return flowchartElement;
        }

        public TestBpmElement AddLink(TestActivity sourceActivity, TestActivity targetActivity)
        {
            //Search in the elements collection if the flowstep exists and just needs to be
            //connected to next element
            TestBpmStep sourceStep = GetFlowStepContainingActionActivity(sourceActivity);
            TestBpmStep targetStep = GetFlowStepContainingActionActivity(targetActivity);

            if (sourceStep == null)
            {
                sourceStep = new TestBpmStep(sourceActivity);
                SetStartNode(sourceStep);
                AddTestFlowLink(sourceStep);
            }

            if (targetStep == null)
            {
                targetStep = new TestBpmStep(targetActivity);
            }
            sourceStep.NextElement = targetStep;
            AddTestFlowLink(targetStep);

            //We need to return the source step since the IsFaulting has to be set in case
            //of fault
            return sourceStep;
        }

        public TestBpmElement AddLink(TestActivity sourceActivity, TestBpmElement flowElement)
        {
            TestBpmStep sourceStep = GetFlowStepContainingActionActivity(sourceActivity);

            if (sourceStep == null)
            {
                sourceStep = new TestBpmStep(sourceActivity);
                SetStartNode(sourceStep);
                AddTestFlowLink(sourceStep);
            }

            sourceStep.NextElement = flowElement;
            AddTestFlowLink(flowElement);

            return sourceStep;
        }

        /// <summary>
        /// Modify the FlowStep's Next to point to a different node
        /// </summary>
        /// <param name="flowStep">FlowStep to be updated</param>
        /// <param name="newNextActivity">new activity to be executed next</param>
        /// <param name="removeOldNextNode">wehther we want to remove the old Next node from BpmFlowchart</param>
        public void ChangeFlowStepNextNode(TestBpmStep flowStep, TestActivity newNextActivity, bool removeOldNextNode = true)
        {
            TestBpmStep newNextNode = null;
            if (newNextActivity != null)
            {
                newNextNode = this.GetFlowStepContainingActionActivity(newNextActivity);
                if (newNextNode == null)
                {
                    newNextNode = new TestBpmStep(newNextActivity);
                }
            }

            this.ChangeFlowStepNextNode(flowStep, newNextNode, removeOldNextNode);
        }

        /// <summary>
        /// Modify the FlowStep's Next to point to a different node. 
        /// 
        /// This function is useful when we try to swap nodes because GetFlowStepContainingActionActivity()
        /// use the GetNextElement() to loop through all elements, as a result, it won't be able to loop
        /// through all the this.Elements in the middle of swapping.
        /// </summary>
        /// <param name="flowStep">FlowStep to be updated</param>
        /// <param name="newNextNode">new node to be executed next</param>
        /// <param name="removeOldNextNode">wehther we want to remove the old Next node from BpmFlowchart</param>
        public void ChangeFlowStepNextNode(TestBpmStep flowStep, TestBpmElement newNextNode, bool removeOldNextNode = true)
        {
            // remove old Next node from BpmFlowchart
            if (removeOldNextNode)
            {
                this.Elements.Remove(flowStep.NextElement);
            }

            // add new Next node to BpmFlowchart
            if (newNextNode != null)
            {
                this.AddTestFlowLink(newNextNode);
            }

            // set new Next node
            flowStep.NextElement = newNextNode;
        }

        public TestBpmElement AddStartLink(TestActivity targetActivity)
        {
            TestBpmStep flowStep = new TestBpmStep(targetActivity);
            SetStartNode(flowStep);
            return AddTestFlowLink(flowStep);
        }
        public TestBpmElement AddFaultLink(TestActivity sourceActivity, TestActivity targetActivity, Variable<Exception> exception, bool isExceptionHandler)
        {
            throw new NotImplementedException();
        }

        public TestBpmElement AddConditionalLink(TestActivity sourceActivity, TestBpmFlowConditional flowConditional, TestActivity trueActivity, TestActivity falseActivity)
        {
            if (sourceActivity != null)
            {
                TestBpmStep flowStep = GetFlowStepContainingActionActivity(sourceActivity);
                if (flowStep == null)
                {
                    flowStep = new TestBpmStep(sourceActivity)
                    {
                        NextElement = flowConditional
                    };
                    AddTestFlowLink(flowStep);
                    SetStartNode(flowStep);
                }
                else
                {
                    flowStep.NextElement = flowConditional;
                }
            }
            AddTestFlowLink(flowConditional);

            if (trueActivity != null)
            {
                //For loops we need to check if the element already exists with the target activity
                TestBpmStep trueStep = GetFlowStepContainingActionActivity(trueActivity);
                if (trueStep != null)
                {
                    flowConditional.TrueAction = trueStep;
                }
                else
                {
                    flowConditional.TrueAction = new TestBpmStep(trueActivity);
                }
                AddTestFlowLink(flowConditional.TrueAction);
            }
            if (falseActivity != null)
            {
                //For loops we need to check if the element already exists with the target activity
                TestBpmStep falseStep = GetFlowStepContainingActionActivity(falseActivity);
                if (falseStep != null)
                {
                    flowConditional.FalseAction = falseStep;
                }
                else
                {
                    flowConditional.FalseAction = new TestBpmStep(falseActivity);
                }
                AddTestFlowLink(flowConditional.FalseAction);
            }

            if (_startElement == null)
            {
                SetStartNode(flowConditional);
                AddTestFlowLink(flowConditional);
            }
            return flowConditional;
        }

        public TestBpmElement AddConditionalLink(TestActivity sourceActivity, TestBpmFlowConditional flowConditional)
        {
            return AddConditionalLink(sourceActivity, flowConditional, (TestActivity)null, (TestActivity)null);
        }

        public TestBpmElement AddConditionalLink(TestActivity sourceActivity, TestBpmFlowConditional flowConditional, TestActivity trueActivity, TestBpmElement falseFlowElement)
        {
            AddConditionalLink(sourceActivity, flowConditional, trueActivity, (TestActivity)null);
            flowConditional.FalseAction = falseFlowElement;
            AddTestFlowLink(falseFlowElement);
            return flowConditional;
        }

        public TestBpmElement AddConditionalLink(TestActivity sourceActivity, TestBpmFlowConditional flowConditional, TestBpmElement trueFlowElement, TestActivity falseActivity)
        {
            AddConditionalLink(sourceActivity, flowConditional, (TestActivity)null, falseActivity);
            flowConditional.TrueAction = trueFlowElement;
            AddTestFlowLink(trueFlowElement);
            return flowConditional;
        }

        public TestBpmElement AddConditionalLink(TestActivity sourceActivity, TestBpmFlowConditional flowConditional, TestBpmElement trueFlowElement, TestBpmElement falseFlowElement)
        {
            AddConditionalLink(sourceActivity, flowConditional, (TestActivity)null, (TestActivity)null);
            flowConditional.TrueAction = trueFlowElement;
            flowConditional.FalseAction = falseFlowElement;
            AddTestFlowLink(trueFlowElement);
            AddTestFlowLink(falseFlowElement);
            return flowConditional;
        }

        /// <summary>
        /// Modify the FlowDecision's Condition and its corresponding hint.
        /// </summary>
        public void ChangeFlowDecisionCondition(TestBpmFlowConditional flowDecision, bool newCondition, List<HintTrueFalse> newHint)
        {
            flowDecision.Condition = newCondition;
            flowDecision.TrueOrFalse = newHint;
        }

        /// <summary>
        /// Modify the FlowDecision's Condition activity and its corresponding hint.
        /// </summary>
        public void ChangeFlowDecisionCondition(TestBpmFlowConditional flowDecision, TestActivity newConditionActivity, List<HintTrueFalse> newHint)
        {
            flowDecision.ConditionValueExpression = newConditionActivity;
            flowDecision.TrueOrFalse = newHint;
        }

        /// <summary>
        /// Modify the FlowDecision's True or False path to point to a different node
        /// </summary>
        /// <param name="flowConditional">FlowDecision to be updated</param>
        /// <param name="changeTrueAction">true if TrueAction to be changed, false if FalseAction to be changed</param>
        /// <param name="newAction">new activity to be executed on the specified FlowDecision's path</param>
        /// <param name="removeOldNode">wehther we want to remove the old node from BpmFlowchart</param>
        public void ChangeFlowDecisionAction(TestBpmFlowConditional flowConditional, bool changeTrueAction, TestActivity newAction, bool removeOldNode = true)
        {
            // remove old node from BpmFlowchart
            if (removeOldNode)
            {
                if (changeTrueAction)
                {
                    this.Elements.Remove(flowConditional.TrueAction);
                }
                else
                {
                    this.Elements.Remove(flowConditional.FalseAction);
                }
            }

            // add new node to BpmFlowchart
            TestBpmStep newNode = null;
            if (newAction != null)
            {
                newNode = this.GetFlowStepContainingActionActivity(newAction);
                if (newNode == null)
                {
                    newNode = new TestBpmStep(newAction);
                }

                this.AddTestFlowLink(newNode);
            }

            // set new Action
            if (changeTrueAction)
            {
                flowConditional.TrueAction = newNode;
            }
            else
            {
                flowConditional.FalseAction = newNode;
            }
        }

        public TestBpmElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestBpmElement> cases, List<int> hintsExecutingActivityIndex, T expression, params TestActivity[] defaultActivity)
        {
            TestBpmSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, null, hintsExecutingActivityIndex, defaultActivity, true) as TestBpmSwitch<T>;
            flowSwitch.Expression = expression;

            foreach (KeyValuePair<T, TestBpmElement> flowCase in cases)
            {
                flowSwitch.AddCase(flowCase.Key, flowCase.Value);
                AddTestFlowLink(flowCase.Value);
            }
            return AddTestFlowLink(flowSwitch);
        }

        public TestBpmElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, T expression, params TestActivity[] defaultActivity)
        {
            TestBpmSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestBpmSwitch<T>;
            flowSwitch.Expression = expression;
            return AddTestFlowLink(flowSwitch);
        }

        public TestBpmElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, Variable<T> expressionVariable, params TestActivity[] defaultActivity)
        {
            TestBpmSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestBpmSwitch<T>;
            flowSwitch.ExpressionVariable = expressionVariable;
            return AddTestFlowLink(flowSwitch);
        }

        public TestBpmElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, Expression<Func<ActivityContext, T>> lambdaExpression, params TestActivity[] defaultActivity)
        {
            TestBpmSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestBpmSwitch<T>;
            flowSwitch.LambdaExpression = lambdaExpression;
            return AddTestFlowLink(flowSwitch);
        }

        public TestBpmElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, TestActivity expressionActivity, params TestActivity[] defaultActivity)
        {
            TestBpmSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestBpmSwitch<T>;
            flowSwitch.ExpressionActivity = expressionActivity;
            return AddTestFlowLink(flowSwitch);
        }

        private TestBpmSwitchBase CreateSwitchElement<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, TestActivity[] defaultActivity, bool genericSwitch = false)
        {
            TestBpmStep flowStep = null;
            List<TestBpmStep> newAddedSteps = new List<TestBpmStep>();

            TestBpmSwitchBase flowSwitch;
            flowSwitch = new TestBpmSwitch<T>();

            flowSwitch.SetHints(hintsExecutingActivityIndex);

            if (sourceActivity != null)
            {
                flowStep = GetFlowStepContainingActionActivity(sourceActivity);
                if (flowStep == null)
                {
                    flowStep = new TestBpmStep(sourceActivity);
                    AddTestFlowLink(flowStep);
                }
            }

            if (cases != null)
            {
                foreach (KeyValuePair<T, TestActivity> flowCase in cases)
                {
                    TestBpmStep targetStep = GetFlowStepContainingActionActivity(flowCase.Value);
                    if (targetStep == null && flowCase.Value != null)
                    {
                        //Need this to find identical FlowSteps in the cases locally as they are not
                        //added yet to the linked list
                        if ((targetStep = FindMatchingCase(newAddedSteps, flowCase.Value.DisplayName)) == null)
                        {
                            targetStep = new TestBpmStep(flowCase.Value);
                            newAddedSteps.Add(targetStep);
                        }
                        AddTestFlowLink(targetStep);
                    }

                    (flowSwitch as TestBpmSwitch<T>).AddCase(flowCase.Key, targetStep);
                }
            }

            if (defaultActivity != null && defaultActivity.Length != 0)
            {
                TestBpmStep defaultStep = GetFlowStepContainingActionActivity(defaultActivity[0]);
                if (defaultStep == null)
                {
                    if ((defaultStep = FindMatchingCase(newAddedSteps, defaultActivity[0].DisplayName)) == null)
                    {
                        defaultStep = new TestBpmStep(defaultActivity[0]);
                    }
                }

                (flowSwitch as TestBpmSwitch<T>).Default = defaultStep;
                AddTestFlowLink(defaultStep);
            }

            if (flowStep != null)
            {
                flowStep.NextElement = flowSwitch;
            }
            else
            {
                SetStartNode(flowSwitch);
            }

            return flowSwitch;
        }

        /// <summary>
        /// Modify the FlowSwitch's Expression and its corresponding hint.
        /// </summary>
        public void ChangeFlowSwitchExpression<T>(TestBpmSwitch<T> flowSwitch, T newExpression, List<int> newHint)
        {
            flowSwitch.Expression = newExpression;
            flowSwitch.SetHints(newHint);
        }

        /// <summary>
        /// Modify the FlowSwitch's Expression activity and its corresponding hint.
        /// </summary>
        public void ChangeFlowSwitchExpression<T>(TestBpmSwitch<T> flowSwitch, TestActivity newExpressionActivity, List<int> newHint)
        {
            flowSwitch.ExpressionActivity = newExpressionActivity;
            flowSwitch.SetHints(newHint);
        }

        /// <summary>
        /// Modify FlowSwitch's Case to point to a different activity
        /// </summary>
        /// <param name="flowSwitch">FlowSwitch to be updated</param>
        /// <param name="flowSwitchCase">case whose action to be updated</param>
        /// <param name="newCaseActivity">new activity to be executed in the given FlowSwitch Case</param>
        /// <param name="removeOldCaseNode">wehther we want to remove the old Case node from BpmFlowchart</param>
        public void ChangeFlowSwitchCaseAction<T>(TestBpmSwitch<T> flowSwitch, T caseExpression, int caseIndex, TestActivity oldCaseActivity, TestActivity newCaseActivity, bool removeOldCaseNode = true)
        {
            TestBpmStep oldCaseNode = null;
            TestBpmStep newCaseNode = null;

            // remove old Case node from BpmFlowchart
            if (removeOldCaseNode)
            {
                oldCaseNode = this.GetFlowStepContainingActionActivity(oldCaseActivity);
                if (oldCaseNode == null)
                {
                    throw new ArgumentException("Failed to find the node corresponding to the given oldCaseActivity.");
                }
                this.Elements.Remove(oldCaseNode);
            }

            // add new Case node to BpmFlowchart
            if (newCaseActivity != null)
            {
                newCaseNode = this.GetFlowStepContainingActionActivity(newCaseActivity);
                if (newCaseNode == null)
                {
                    newCaseNode = new TestBpmStep(newCaseActivity);
                }

                this.AddTestFlowLink(newCaseNode);
            }

            // set new Case action
            flowSwitch.UpdateCase(caseExpression, caseIndex, newCaseNode);
        }

        private TestBpmStep FindMatchingCase(List<TestBpmStep> steps, string displayName)
        {
            foreach (TestBpmStep step in steps)
            {
                if (step.ActionActivity.DisplayName == displayName)
                {
                    return step;
                }
            }
            return null;
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            //start element will be null in case of empty flowchart
            if (_startElement != null)
            {
                CurrentOutcome = _startElement.GetTrace(traceGroup);
            }
            else if (_elements.Count == 1)
            {
                CurrentOutcome = _elements[0].GetTrace(traceGroup); //If just one element is added directly to elements collection in test
            }

            ResetElements();
        }

        protected override void GetCancelTrace(TraceGroup traceGroup)
        {
            UnorderedTraces finalCancelTraces = new UnorderedTraces();

            GetCompensationTrace(finalCancelTraces);
            finalCancelTraces.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));

            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            List<TestBpmFlowConditional> conditionals = new List<TestBpmFlowConditional>();
            List<TestBpmSwitchBase> switches = new List<TestBpmSwitchBase>();
            List<TestActivity> activities = new List<TestActivity>();

            TestBpmElement current = _startElement;

            while (current != null)
            {
                if (current is TestBpmFlowConditional && !conditionals.Contains((TestBpmFlowConditional)current))
                {
                    conditionals.Add((TestBpmFlowConditional)current);
                }
                else if (current is TestBpmSwitchBase && !switches.Contains((TestBpmSwitchBase)current))
                {
                    switches.Add((TestBpmSwitchBase)current);
                }
                else if (current is TestBpmStep && ((TestBpmStep)current).ActionActivity != null)
                {
                    activities.Add(((TestBpmStep)current).ActionActivity);
                }
                current = current.GetNextElement();
            }

            ResetConditionalsIterationCounter(conditionals);
            ResetSwitchesIterationCounter(switches);
            return activities;
        }

        //
        // BpmFlowchart Split Compensation/Confirmation
        // - needs to participate in the results building because, again, the traces
        // will be unordered
        //

        internal override void GetCompensationTrace(TraceGroup traceGroup)
        {
            ProcessCompensationHints(this.CompensationHint, Directive.Compensate, traceGroup);
        }

        internal override void GetConfirmationTrace(TraceGroup traceGroup)
        {
            ProcessCompensationHints(this.CompensationHint, Directive.Confirm, traceGroup);
        }

        //
        // Example hint list describing 3 branches and the order of processing for CA's on each Branch
        // { Branch, CA2, CA1, Branch, CA3, Branch, CA6, CA5, CA4 }
        //

        private void ProcessCompensationHints(IList<Directive> hints, string defaultAction, TraceGroup traceGroup)
        {
            // A splited flowchart Confirmation/Compensation are collections of unordered branch traces 
            // (similar to Parallel)
            UnorderedTraces unordered = new UnorderedTraces();

            OrderedTraces ordered = null;
            foreach (Directive directive in hints)
            {
                // If we encounter a Branch directive that means we need to start a new OrderedTraces group
                if (directive.Name == "Branch")
                {
                    if (ordered != null) // Already had one, so add it to our collection before we create a new one
                    {
                        if (ordered.Steps.Count > 0) // There's a chance we didn't produce any output
                        {
                            unordered.Steps.Add(ordered);
                        }
                    }

                    ordered = new OrderedTraces();
                }
                else
                {
                    TestActivity target = FindChildActivity(directive.Name);

                    TestCompensableActivity.ProcessDirective(target, directive, defaultAction, traceGroup);
                }
            }

            // Was there one left over? (From the last branch directive)
            if (ordered != null)
            {
                if (ordered.Steps.Count > 0) // There's a chance we didn't produce any output
                {
                    unordered.Steps.Add(ordered);
                }
            }

            if (unordered.Steps.Count > 0)
            {
                traceGroup.Steps.Add(unordered);
            }
        }

        private TestBpmStep GetFlowStepContainingActionActivity(TestActivity activity)
        {
            if (_startElement == null || activity == null)
            {
                return null;
            }

            List<TestBpmFlowConditional> conditionals = new List<TestBpmFlowConditional>();
            List<TestBpmSwitchBase> switches = new List<TestBpmSwitchBase>();
            TestBpmElement current = _startElement;

            while (current != null)
            {
                if (current is TestBpmFlowConditional && !conditionals.Contains((TestBpmFlowConditional)current))
                {
                    conditionals.Add((TestBpmFlowConditional)current);
                }
                else if (current is TestBpmSwitchBase && !switches.Contains((TestBpmSwitchBase)current))
                {
                    switches.Add((TestBpmSwitchBase)current);
                }
                else if (current is TestBpmStep && ((TestBpmStep)current).ActionActivity.DisplayName == activity.DisplayName)
                {
                    //Need to reset the iteration numbers of
                    //flowconditionals and flowSwitched since they will be increment 
                    //on GetNextElement()
                    ResetConditionalsIterationCounter(conditionals);
                    ResetSwitchesIterationCounter(switches);
                    return current as TestBpmStep;
                }
                current = current.GetNextElement();
            }

            ResetConditionalsIterationCounter(conditionals);
            ResetSwitchesIterationCounter(switches);
            return null;
        }

        /// <summary>
        /// Return the node that the given FlowSwitch will execute based on the hint and cases set. 
        /// 
        /// Note that this function works only for FlowSwitch that is NOT in a loop.
        /// </summary>
        public TestBpmElement GetFlowSwitchChosenNode(TestBpmSwitchBase flowSwitch)
        {
            TestBpmElement chosenCase = flowSwitch.GetNextElement();
            ResetSwitchesIterationCounter(new List<TestBpmSwitchBase>() { flowSwitch });  // reset the iteration counter to erase the side-effect caused by GetNextElement();
            return chosenCase;
        }

        private void ResetElements()
        {
            foreach (TestBpmElement element in _elements)
            {
                if (element is TestBpmFlowConditional)
                {
                    ((TestBpmFlowConditional)element).ResetIterationNumber();
                }
                else if (element is TestBpmSwitchBase)
                {
                    ((TestBpmSwitchBase)element).ResetIterationNumber();
                }
            }
        }

        private void ResetConditionalsIterationCounter(List<TestBpmFlowConditional> conditionals)
        {
            foreach (TestBpmFlowConditional conditional in conditionals)
            {
                conditional.ResetIterationNumber();
            }
        }

        private void ResetSwitchesIterationCounter(List<TestBpmSwitchBase> switches)
        {
            foreach (TestBpmSwitchBase flowSwitch in switches)
            {
                flowSwitch.ResetIterationNumber();
            }
        }
    }
}
