// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Activities;
using System.Activities.Statements;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities.Bpm
{
    public class TestFlowchart : TestActivity
    {
        public bool HintExceptionWillBeHandled = false;
        private MemberCollection<TestFlowElement> _elements;
        private IList<Directive> _compensationHint;
        private TestFlowElement _startElement;

        public TestFlowchart()
        {
            this.ProductActivity = new Flowchart();
            _elements = new MemberCollection<TestFlowElement>(AddFlowElement)
            {
                RemoveItem = RemoveFlowElementItem,
                RemoveAtItem = RemoveFlowElementAt
            };
            _compensationHint = new List<Directive>();
        }

        public TestFlowchart(string displayName)
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

        public MemberCollection<TestFlowElement> Elements
        {
            get { return _elements; }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        private Flowchart ProductFlowchart
        {
            get
            {
                return (Flowchart)this.ProductActivity;
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

        private void SetStartNode(TestFlowElement startElement)
        {
            _startElement = startElement;
            ((Flowchart)ProductActivity).StartNode = startElement.GetProductElement();
        }

        protected void AddFlowElement(TestFlowElement item)
        {
            if (_startElement == null)
            {
                SetStartNode(item);
            }
            ((Flowchart)ProductActivity).Nodes.Add(item.GetProductElement());
        }

        protected bool RemoveFlowElementItem(TestFlowElement item)
        {
            return ((Flowchart)ProductActivity).Nodes.Remove(item.GetProductElement());
        }

        protected void RemoveFlowElementAt(int index)
        {
            ((Flowchart)ProductActivity).Nodes.RemoveAt(index);
        }

        // This overload is for more complicated scenarios
        private TestFlowElement AddTestFlowLink(TestFlowElement flowchartElement)
        {
            if (!_elements.Contains(flowchartElement))
            {
                _elements.Add(flowchartElement);
            }
            return flowchartElement;
        }

        public TestFlowElement AddLink(TestActivity sourceActivity, TestActivity targetActivity)
        {
            //Search in the elements collection if the flowstep exists and just needs to be
            //connected to next element
            TestFlowStep sourceStep = GetFlowStepContainingActionActivity(sourceActivity);
            TestFlowStep targetStep = GetFlowStepContainingActionActivity(targetActivity);

            if (sourceStep == null)
            {
                sourceStep = new TestFlowStep(sourceActivity);
                SetStartNode(sourceStep);
                AddTestFlowLink(sourceStep);
            }

            if (targetStep == null)
            {
                targetStep = new TestFlowStep(targetActivity);
            }
            sourceStep.NextElement = targetStep;
            AddTestFlowLink(targetStep);

            //We need to return the source step since the IsFaulting has to be set in case
            //of fault
            return sourceStep;
        }

        public TestFlowElement AddLink(TestActivity sourceActivity, TestFlowElement flowElement)
        {
            TestFlowStep sourceStep = GetFlowStepContainingActionActivity(sourceActivity);

            if (sourceStep == null)
            {
                sourceStep = new TestFlowStep(sourceActivity);
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
        /// <param name="removeOldNextNode">wehther we want to remove the old Next node from Flowchart</param>
        public void ChangeFlowStepNextNode(TestFlowStep flowStep, TestActivity newNextActivity, bool removeOldNextNode = true)
        {
            TestFlowStep newNextNode = null;
            if (newNextActivity != null)
            {
                newNextNode = this.GetFlowStepContainingActionActivity(newNextActivity);
                if (newNextNode == null)
                {
                    newNextNode = new TestFlowStep(newNextActivity);
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
        /// <param name="removeOldNextNode">wehther we want to remove the old Next node from Flowchart</param>
        public void ChangeFlowStepNextNode(TestFlowStep flowStep, TestFlowElement newNextNode, bool removeOldNextNode = true)
        {
            // remove old Next node from Flowchart
            if (removeOldNextNode)
            {
                this.Elements.Remove(flowStep.NextElement);
            }

            // add new Next node to Flowchart
            if (newNextNode != null)
            {
                this.AddTestFlowLink(newNextNode);
            }

            // set new Next node
            flowStep.NextElement = newNextNode;
        }

        public TestFlowElement AddStartLink(TestActivity targetActivity)
        {
            TestFlowStep flowStep = new TestFlowStep(targetActivity);
            SetStartNode(flowStep);
            return AddTestFlowLink(flowStep);
        }
        public TestFlowElement AddFaultLink(TestActivity sourceActivity, TestActivity targetActivity, Variable<Exception> exception, bool isExceptionHandler)
        {
            throw new NotImplementedException();
        }

        public TestFlowElement AddConditionalLink(TestActivity sourceActivity, TestFlowConditional flowConditional, TestActivity trueActivity, TestActivity falseActivity)
        {
            if (sourceActivity != null)
            {
                TestFlowStep flowStep = GetFlowStepContainingActionActivity(sourceActivity);
                if (flowStep == null)
                {
                    flowStep = new TestFlowStep(sourceActivity)
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
                TestFlowStep trueStep = GetFlowStepContainingActionActivity(trueActivity);
                if (trueStep != null)
                {
                    flowConditional.TrueAction = trueStep;
                }
                else
                {
                    flowConditional.TrueAction = new TestFlowStep(trueActivity);
                }
                AddTestFlowLink(flowConditional.TrueAction);
            }
            if (falseActivity != null)
            {
                //For loops we need to check if the element already exists with the target activity
                TestFlowStep falseStep = GetFlowStepContainingActionActivity(falseActivity);
                if (falseStep != null)
                {
                    flowConditional.FalseAction = falseStep;
                }
                else
                {
                    flowConditional.FalseAction = new TestFlowStep(falseActivity);
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

        public TestFlowElement AddConditionalLink(TestActivity sourceActivity, TestFlowConditional flowConditional)
        {
            return AddConditionalLink(sourceActivity, flowConditional, (TestActivity)null, (TestActivity)null);
        }

        public TestFlowElement AddConditionalLink(TestActivity sourceActivity, TestFlowConditional flowConditional, TestActivity trueActivity, TestFlowElement falseFlowElement)
        {
            AddConditionalLink(sourceActivity, flowConditional, trueActivity, (TestActivity)null);
            flowConditional.FalseAction = falseFlowElement;
            AddTestFlowLink(falseFlowElement);
            return flowConditional;
        }

        public TestFlowElement AddConditionalLink(TestActivity sourceActivity, TestFlowConditional flowConditional, TestFlowElement trueFlowElement, TestActivity falseActivity)
        {
            AddConditionalLink(sourceActivity, flowConditional, (TestActivity)null, falseActivity);
            flowConditional.TrueAction = trueFlowElement;
            AddTestFlowLink(trueFlowElement);
            return flowConditional;
        }

        public TestFlowElement AddConditionalLink(TestActivity sourceActivity, TestFlowConditional flowConditional, TestFlowElement trueFlowElement, TestFlowElement falseFlowElement)
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
        public void ChangeFlowDecisionCondition(TestFlowConditional flowDecision, bool newCondition, List<HintTrueFalse> newHint)
        {
            flowDecision.Condition = newCondition;
            flowDecision.TrueOrFalse = newHint;
        }

        /// <summary>
        /// Modify the FlowDecision's Condition activity and its corresponding hint.
        /// </summary>
        public void ChangeFlowDecisionCondition(TestFlowConditional flowDecision, TestActivity newConditionActivity, List<HintTrueFalse> newHint)
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
        /// <param name="removeOldNode">wehther we want to remove the old node from Flowchart</param>
        public void ChangeFlowDecisionAction(TestFlowConditional flowConditional, bool changeTrueAction, TestActivity newAction, bool removeOldNode = true)
        {
            // remove old node from Flowchart
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

            // add new node to Flowchart
            TestFlowStep newNode = null;
            if (newAction != null)
            {
                newNode = this.GetFlowStepContainingActionActivity(newAction);
                if (newNode == null)
                {
                    newNode = new TestFlowStep(newAction);
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

        public TestFlowElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestFlowElement> cases, List<int> hintsExecutingActivityIndex, T expression, params TestActivity[] defaultActivity)
        {
            TestFlowSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, null, hintsExecutingActivityIndex, defaultActivity, true) as TestFlowSwitch<T>;
            flowSwitch.Expression = expression;

            foreach (KeyValuePair<T, TestFlowElement> flowCase in cases)
            {
                flowSwitch.AddCase(flowCase.Key, flowCase.Value);
                AddTestFlowLink(flowCase.Value);
            }
            return AddTestFlowLink(flowSwitch);
        }

        public TestFlowElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, T expression, params TestActivity[] defaultActivity)
        {
            TestFlowSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestFlowSwitch<T>;
            flowSwitch.Expression = expression;
            return AddTestFlowLink(flowSwitch);
        }

        public TestFlowElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, Variable<T> expressionVariable, params TestActivity[] defaultActivity)
        {
            TestFlowSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestFlowSwitch<T>;
            flowSwitch.ExpressionVariable = expressionVariable;
            return AddTestFlowLink(flowSwitch);
        }

        public TestFlowElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, Expression<Func<ActivityContext, T>> lambdaExpression, params TestActivity[] defaultActivity)
        {
            TestFlowSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestFlowSwitch<T>;
            flowSwitch.LambdaExpression = lambdaExpression;
            return AddTestFlowLink(flowSwitch);
        }

        public TestFlowElement AddSwitchLink<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, TestActivity expressionActivity, params TestActivity[] defaultActivity)
        {
            TestFlowSwitch<T> flowSwitch = CreateSwitchElement<T>(sourceActivity, cases, hintsExecutingActivityIndex, defaultActivity, true) as TestFlowSwitch<T>;
            flowSwitch.ExpressionActivity = expressionActivity;
            return AddTestFlowLink(flowSwitch);
        }

        private TestFlowSwitchBase CreateSwitchElement<T>(TestActivity sourceActivity, Dictionary<T, TestActivity> cases, List<int> hintsExecutingActivityIndex, TestActivity[] defaultActivity, bool genericSwitch = false)
        {
            TestFlowStep flowStep = null;
            List<TestFlowStep> newAddedSteps = new List<TestFlowStep>();

            TestFlowSwitchBase flowSwitch;
            flowSwitch = new TestFlowSwitch<T>();

            flowSwitch.SetHints(hintsExecutingActivityIndex);

            if (sourceActivity != null)
            {
                flowStep = GetFlowStepContainingActionActivity(sourceActivity);
                if (flowStep == null)
                {
                    flowStep = new TestFlowStep(sourceActivity);
                    AddTestFlowLink(flowStep);
                }
            }

            if (cases != null)
            {
                foreach (KeyValuePair<T, TestActivity> flowCase in cases)
                {
                    TestFlowStep targetStep = GetFlowStepContainingActionActivity(flowCase.Value);
                    if (targetStep == null && flowCase.Value != null)
                    {
                        //Need this to find identical FlowSteps in the cases locally as they are not
                        //added yet to the linked list
                        if ((targetStep = FindMatchingCase(newAddedSteps, flowCase.Value.DisplayName)) == null)
                        {
                            targetStep = new TestFlowStep(flowCase.Value);
                            newAddedSteps.Add(targetStep);
                        }
                        AddTestFlowLink(targetStep);
                    }

                    (flowSwitch as TestFlowSwitch<T>).AddCase(flowCase.Key, targetStep);
                }
            }

            if (defaultActivity != null && defaultActivity.Length != 0)
            {
                TestFlowStep defaultStep = GetFlowStepContainingActionActivity(defaultActivity[0]);
                if (defaultStep == null)
                {
                    if ((defaultStep = FindMatchingCase(newAddedSteps, defaultActivity[0].DisplayName)) == null)
                    {
                        defaultStep = new TestFlowStep(defaultActivity[0]);
                    }
                }

                (flowSwitch as TestFlowSwitch<T>).Default = defaultStep;
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
        public void ChangeFlowSwitchExpression<T>(TestFlowSwitch<T> flowSwitch, T newExpression, List<int> newHint)
        {
            flowSwitch.Expression = newExpression;
            flowSwitch.SetHints(newHint);
        }

        /// <summary>
        /// Modify the FlowSwitch's Expression activity and its corresponding hint.
        /// </summary>
        public void ChangeFlowSwitchExpression<T>(TestFlowSwitch<T> flowSwitch, TestActivity newExpressionActivity, List<int> newHint)
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
        /// <param name="removeOldCaseNode">wehther we want to remove the old Case node from Flowchart</param>
        public void ChangeFlowSwitchCaseAction<T>(TestFlowSwitch<T> flowSwitch, T caseExpression, int caseIndex, TestActivity oldCaseActivity, TestActivity newCaseActivity, bool removeOldCaseNode = true)
        {
            TestFlowStep oldCaseNode = null;
            TestFlowStep newCaseNode = null;

            // remove old Case node from Flowchart
            if (removeOldCaseNode)
            {
                oldCaseNode = this.GetFlowStepContainingActionActivity(oldCaseActivity);
                if (oldCaseNode == null)
                {
                    throw new ArgumentException("Failed to find the node corresponding to the given oldCaseActivity.");
                }
                this.Elements.Remove(oldCaseNode);
            }

            // add new Case node to Flowchart
            if (newCaseActivity != null)
            {
                newCaseNode = this.GetFlowStepContainingActionActivity(newCaseActivity);
                if (newCaseNode == null)
                {
                    newCaseNode = new TestFlowStep(newCaseActivity);
                }

                this.AddTestFlowLink(newCaseNode);
            }

            // set new Case action
            flowSwitch.UpdateCase(caseExpression, caseIndex, newCaseNode);
        }

        private TestFlowStep FindMatchingCase(List<TestFlowStep> steps, string displayName)
        {
            foreach (TestFlowStep step in steps)
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
            List<TestFlowConditional> conditionals = new List<TestFlowConditional>();
            List<TestFlowSwitchBase> switches = new List<TestFlowSwitchBase>();
            List<TestActivity> activities = new List<TestActivity>();

            TestFlowElement current = _startElement;

            while (current != null)
            {
                if (current is TestFlowConditional && !conditionals.Contains((TestFlowConditional)current))
                {
                    conditionals.Add((TestFlowConditional)current);
                }
                else if (current is TestFlowSwitchBase && !switches.Contains((TestFlowSwitchBase)current))
                {
                    switches.Add((TestFlowSwitchBase)current);
                }
                else if (current is TestFlowStep && ((TestFlowStep)current).ActionActivity != null)
                {
                    activities.Add(((TestFlowStep)current).ActionActivity);
                }
                current = current.GetNextElement();
            }

            ResetConditionalsIterationCounter(conditionals);
            ResetSwitchesIterationCounter(switches);
            return activities;
        }

        //
        // Flowchart Split Compensation/Confirmation
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

        private TestFlowStep GetFlowStepContainingActionActivity(TestActivity activity)
        {
            if (_startElement == null || activity == null)
            {
                return null;
            }

            List<TestFlowConditional> conditionals = new List<TestFlowConditional>();
            List<TestFlowSwitchBase> switches = new List<TestFlowSwitchBase>();
            TestFlowElement current = _startElement;

            while (current != null)
            {
                if (current is TestFlowConditional && !conditionals.Contains((TestFlowConditional)current))
                {
                    conditionals.Add((TestFlowConditional)current);
                }
                else if (current is TestFlowSwitchBase && !switches.Contains((TestFlowSwitchBase)current))
                {
                    switches.Add((TestFlowSwitchBase)current);
                }
                else if (current is TestFlowStep && ((TestFlowStep)current).ActionActivity.DisplayName == activity.DisplayName)
                {
                    //Need to reset the iteration numbers of
                    //flowconditionals and flowSwitched since they will be increment 
                    //on GetNextElement()
                    ResetConditionalsIterationCounter(conditionals);
                    ResetSwitchesIterationCounter(switches);
                    return current as TestFlowStep;
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
        public TestFlowElement GetFlowSwitchChosenNode(TestFlowSwitchBase flowSwitch)
        {
            TestFlowElement chosenCase = flowSwitch.GetNextElement();
            ResetSwitchesIterationCounter(new List<TestFlowSwitchBase>() { flowSwitch });  // reset the iteration counter to erase the side-effect caused by GetNextElement();
            return chosenCase;
        }

        private void ResetElements()
        {
            foreach (TestFlowElement element in _elements)
            {
                if (element is TestFlowConditional)
                {
                    ((TestFlowConditional)element).ResetIterationNumber();
                }
                else if (element is TestFlowSwitchBase)
                {
                    ((TestFlowSwitchBase)element).ResetIterationNumber();
                }
            }
        }

        private void ResetConditionalsIterationCounter(List<TestFlowConditional> conditionals)
        {
            foreach (TestFlowConditional conditional in conditionals)
            {
                conditional.ResetIterationNumber();
            }
        }

        private void ResetSwitchesIterationCounter(List<TestFlowSwitchBase> switches)
        {
            foreach (TestFlowSwitchBase flowSwitch in switches)
            {
                flowSwitch.ResetIterationNumber();
            }
        }
    }
}
