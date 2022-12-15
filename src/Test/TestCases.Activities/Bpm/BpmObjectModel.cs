// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using Test.Common.TestObjects.Activities.Bpm;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmObjectModel : IDisposable
    {
        /// <summary>
        /// Display name null.
        /// </summary>        
        [Fact]
        public void DisplayNameNull()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "Hello1");
            flowchart.AddStartLink(w1);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Sequence modelled in flowchart test case
        /// </summary>        
        [Fact]
        public void FlowchartInitializerSyntax_Sequence()
        {
            Test.Common.TestObjects.CustomActivities.WriteLine writeLine1 = new Test.Common.TestObjects.CustomActivities.WriteLine
            {
                Message = new InArgument<string>("Hello1")
            };
            Test.Common.TestObjects.CustomActivities.WriteLine writeLine2 = new Test.Common.TestObjects.CustomActivities.WriteLine
            {
                Message = new InArgument<string>("Hello2")
            };
            Test.Common.TestObjects.CustomActivities.WriteLine writeLine3 = new Test.Common.TestObjects.CustomActivities.WriteLine
            {
                Message = new InArgument<string>("Hello3")
            };

            BpmStep flowStep1 = new BpmStep { Action = writeLine1 };
            BpmStep flowStep2 = new BpmStep { Action = writeLine2, Next = flowStep1 };
            BpmStep flowStep3 = new BpmStep { Action = writeLine3, Next = flowStep2 };

            System.Activities.Statements.BpmFlowchart flowchart = new System.Activities.Statements.BpmFlowchart
            {
                Nodes =
                {
                    flowStep1, flowStep2, flowStep3
                },
                StartNode = flowStep3
            };

            AutoResetEvent are = new AutoResetEvent(false);

            WorkflowApplication application = new WorkflowApplication(flowchart)
            {
                Completed = delegate (WorkflowApplicationCompletedEventArgs e) { are.Set(); }
            };
            application.Run();

            are.WaitOne();
        }

        /// <summary>
        /// FlowConditional.True connected to another flowconditional
        /// </summary>        
        [Fact]
        public void ConnectFromFlowconditionalTrueToFlowconditional()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestBpmFlowConditional conditional1 = new TestBpmFlowConditional
            {
                Condition = true
            };

            TestBpmFlowConditional conditional2 = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddConditionalLink(w1, conditional1);
            flowchart.AddConditionalLink(null, conditional2, w2, (TestActivity)null);
            flowchart.AddConditionalLink(null, conditional1, conditional2, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowConditional.False connected to another flowconditional
        /// </summary>        
        [Fact]
        public void ConnectFromFlowconditionalFalseToFlowconditional()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestBpmFlowConditional conditional1 = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                Condition = false
            };

            TestBpmFlowConditional conditional2 = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddConditionalLink(w1, conditional1);
            flowchart.AddConditionalLink(null, conditional1, (TestActivity)null, conditional2);
            flowchart.AddConditionalLink(null, conditional2, w2, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Both FlowConditional.False and FlowConditional.True connected to another flowconditional
        /// </summary>        
        [Fact]
        public void ConnectFromFlowconditionalBothTrueAndFalseToDifferentFlowconditional()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            flowchart.Variables.Add(counter);

            TestAssign<int> assign = new TestAssign<int>("assign")
            {
                ValueExpression = (e => counter.Get(e) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            for (int i = 0; i < 4; i++)
            {
                hints.Add(HintTrueFalse.False);
            }
            hints.Add(HintTrueFalse.True);
            TestBpmFlowConditional conditional1 = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (e => counter.Get(e) == 5)
            };

            TestBpmFlowConditional conditional2 = new TestBpmFlowConditional
            {
                Condition = true
            };

            TestBpmFlowConditional conditional3 = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddLink(new TestWriteLine("Start", "BpmFlowchart Started"), assign);
            flowchart.AddConditionalLink(assign, conditional1, conditional3, conditional2);
            flowchart.AddConditionalLink(null, conditional2, assign, (TestActivity)null);
            flowchart.AddConditionalLink(null, conditional3, new TestWriteLine("End", "The End"), (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Both FlowConditional.False and FlowConditional.True connected to same flowconditional
        /// </summary>        
        [Fact]
        public void ConnectFromFlowconditionalBothTrueAndFalseToSameFlowconditional()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            flowchart.Variables.Add(counter);

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestAssign<int> assign = new TestAssign<int>("assign")
            {
                ValueExpression = (e => counter.Get(e) + 1),
                ToVariable = counter
            };

            TestBpmFlowConditional conditional1 = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (e => counter.Get(e) == 5)
            };

            TestBpmFlowConditional conditional2 = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (e => counter.Get(e) == 5)
            };

            flowchart.AddLink(w1, assign);
            flowchart.AddConditionalLink(assign, conditional1);
            flowchart.AddConditionalLink(null, conditional2, assign, w2);
            flowchart.AddConditionalLink(null, conditional1, conditional2, conditional2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// BpmFlowchart without an explicit or implicit start event.
        /// </summary>        
        [Fact]
        public void FlowchartWithoutExplicitOrImplicitStartEvent()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            flowchart.Variables.Add(counter);

            TestAssign<int> assign = new TestAssign<int>("assign")
            {
                ValueExpression = (e => counter.Get(e) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hintsList = new List<HintTrueFalse>();
            hintsList.Add(HintTrueFalse.True);
            hintsList.Add(HintTrueFalse.False);

            TestBpmFlowConditional conditional = new TestBpmFlowConditional(hintsList.ToArray())
            {
                ConditionExpression = (e => counter.Get(e) == 1)
            };
            flowchart.AddConditionalLink(assign, conditional, assign, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add flowchart to itself
        /// </summary>        
        [Fact]
        public void AddFlowchartToItself()
        {
            TestBpmFlowchart flow = new TestBpmFlowchart();
            flow.AddLink(new TestWriteLine("w1", "w1"), flow);

            TestRuntime.ValidateInstantiationException(flow, string.Format(ErrorStrings.ActivityCannotReferenceItself, flow.DisplayName));
        }

        /// <summary>
        /// Add flow conditional with null ture and false branches.
        /// Add flow conditional with null Condition.
        /// </summary>        
        [Fact]
        public void FlowConditionalWithBothTrueAndFalseActionNull()
        {
            TestBpmFlowConditional conditional = new TestBpmFlowConditional
            {
                Condition = false
            };
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddConditionalLink(null, conditional);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Make sure flowchart which includes an activity with : in Display name can be used.
        /// </summary>        
        [Fact]
        public void FlowchartContainingActivityWithColonInDisplayName()
        {
            TestDelay delay = new TestDelay
            {
                DisplayName = "001008 Delay 00:00:02.0000000",
                Duration = new TimeSpan(0, 0, 2)
            };
            TestBpmFlowchart flow = new TestBpmFlowchart();

            flow.AddStartLink(delay);

            TestRuntime.RunAndValidateWorkflow(flow);
        }

        /// <summary>
        /// Add same element twice to the elements collection.
        /// </summary>        
        [Fact]
        public void AddSameElementTwiceToFlowchartElementsCollection()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            TestBpmStep step = new TestBpmStep
            {
                ActionActivity = new TestWriteLine("Start", "Dummy Start")
            };

            flowchart.Elements.Add(step);
            flowchart.Elements.Add(step);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add workflow element elements collection.
        /// </summary>        
        [Fact]
        public void AddWorkflowElementToElementsCollection()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.Elements.Add(new TestWriteLine("StartAndEnd", "StartAndEnd"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Clear elements collection at design time.
        /// </summary>        
        [Fact]
        public void ClearElementsCollectionAtDesignTime()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Add(new System.Activities.Statements.BpmStep { Action = new Delay() });
            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Add(new System.Activities.Statements.BpmStep { Action = new Delay() });

            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add element to elements collection at design time.
        /// </summary>        
        [Fact]
        public void AddElementToCollectionAtDesignTime()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.Elements.Add(new TestBpmStep() { ActionActivity = new TestWriteLine("StartAndEnd", "StartAndEnd") });

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Remove element from elements collection at design time.
        /// </summary>        
        [Fact]
        public void RemoveElementFromCollectionAtDesignTime()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            BpmStep step = new BpmStep() { Action = new Delay() };

            flowchart.Elements.Add(new TestBpmStep() { ActionActivity = new TestWriteLine("Start", "Start") });
            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Add(step);
            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Remove(step);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to another flow step.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowStep()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddLink(new TestWriteLine("Start", "Start"), new TestWriteLine("End", "End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to flow decision.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowDecision()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("Start", "Start");

            TestBpmFlowConditional conditional = new TestBpmFlowConditional() { Condition = true };

            flowchart.AddConditionalLink(w1, conditional, new TestWriteLine("True", "True"), (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to flow switch.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowSwitch()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("Start", "Start");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("1", new TestWriteLine("Will execute", "Executing"));

            List<int> hints = new List<int>() { 0 };

            flowchart.AddSwitchLink<string>(w1, cases, hints, "1");

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Create Flowlink from an activity in flowchart to an activity nested in nested flowchart. Validation error expected.
        /// </summary>        
        [Fact]
        public void CreateFlowlinkToActivityInNestedFlowchart()
        {
            // This testCase is a pair for: LinkFromNestedFlowchartToParent()
            TestBpmFlowchart parentFlowchart = new TestBpmFlowchart("Parent");
            TestBpmFlowchart childFlowchart = new TestBpmFlowchart("Child");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            parentFlowchart.AddLink(childFlowchart, writeLine1);
            parentFlowchart.AddLink(writeLine1, writeLine2);
            childFlowchart.AddStartLink(writeLine2);

            TestRuntime.ValidateInstantiationException(parentFlowchart, string.Format(ErrorStrings.ActivityCannotBeReferencedWithoutTarget, writeLine2.DisplayName, childFlowchart.DisplayName, parentFlowchart.DisplayName));
        }

        /// <summary>
        /// Connect true element of flow decision to flow step
        /// </summary>        
        [Fact]
        public void FlowDecisionConnectedToFlowStep()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);


            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => counter.Get(context) > 0)
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);
            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect false element of flow decision to flow switch
        /// </summary>        
        [Fact]
        public void FlowDecisionConnectedToFlowSwitch()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine w1 = new TestWriteLine("WriteLine1", "Executing WriteLine1");
            TestWriteLine w2 = new TestWriteLine("WriteLine2", "Executing WriteLine2");
            TestWriteLine w3 = new TestWriteLine("WriteLine3", "Executing WriteLine3");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (context => counter.Get(context) > 4)
            };

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);
            cases.Add("Three", w3);

            List<int> hints = new List<int>();
            hints.Add(1);

            TestBpmFlowElement switchElement = flowchart.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, switchElement);
            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect a case element in flow switch to flow decision.
        /// </summary>        
        [Fact]
        public void FlowSwitchConnectedToFlowDecision()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine wStart = new TestWriteLine("Start", "BpmFlowchart started");
            TestWriteLine wDefault = new TestWriteLine("Default", "Default");
            TestWriteLine w1 = new TestWriteLine("One", "One wont execute");
            TestWriteLine w3 = new TestWriteLine("Three", "Three wont execute");
            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            TestBpmStep fs1 = new TestFlowStep(w1);
            TestBpmStep fs3 = new TestFlowStep(w3);

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart.Variables.Add(margin);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            flowchart.AddConditionalLink(null, flowDecision, w2True, w2False);

            Dictionary<string, TestBpmFlowElement> cases = new Dictionary<string, TestBpmFlowElement>();
            cases.Add("One", fs1);
            cases.Add("Two", flowDecision);
            cases.Add("Three", fs3);

            List<int> hints = new List<int>();
            hints.Add(1);

            flowchart.AddStartLink(wStart);
            flowchart.AddSwitchLink<string>(wStart, cases, hints, "Two", wDefault);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add flow step with action property null. The element pointed by next property should get executed after execution of the flow step.
        /// </summary>        
        [Fact]
        public void FlowStepWithNullAction()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine w1 = new TestWriteLine("writeLine1", "Executing writeLine1");
            flowchart1.AddLink(null, w1);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow step with next property null. The flowchart execution should stop after executing action of the flow step.
        /// </summary>        
        [Fact]
        public void FlowStepWithNullNext()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine w1 = new TestWriteLine("writeLine1", "Executing writeLine1");
            flowchart1.AddStartLink(w1);
            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow step with both action and next property null.
        /// Add flow step with both action and next property null. Validation error.
        /// </summary>        
        [Fact]
        public void FlowStepWithBothActionAndNextNull()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestBpmStep flowStep1 = new TestBpmStep();
            flowchart1.Elements.Add(flowStep1);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add two flowsteps in flowchart which are not connected. Validation error.
        /// Add two flowsteps in flowchart which are not connected (multiple starts). Validation error.
        /// </summary>        
        [Fact]
        public void TwoNonConnectedFlowSteps()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestBpmStep flowStep1 = new TestFlowStep(new TestWriteLine("W1", "W1"));
            TestBpmStep flowStep2 = new TestFlowStep(new TestWriteLine("W2", "W2"));
            flowchart1.Elements.Add(flowStep1);
            flowchart1.Elements.Add(flowStep2);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow step and flow decision to flowchart which are not connected. No validation error.
        /// </summary>        
        [Fact]
        public void FlowStepAndFlowDecisionNotConnected()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine w3 = new TestWriteLine("W3", "Executing W3");

            TestBpmFlowConditional flowCond1 = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                Condition = true
            };

            flowchart1.AddStartLink(w1);

            flowchart1.Elements.Add(flowCond1);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow step and flow swicth to flowchart which are not connected. No validation error.
        /// </summary>        
        [Fact]
        public void FlowStepAndFlowSwitchNotConnected()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine start1 = new TestWriteLine("Start", "Executing Start");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine w3 = new TestWriteLine("W3", "Executing W3");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            TestBpmStep flowStep1 = new TestFlowStep(w1);

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w2);
            cases.Add("Two", w3);

            List<int> hints = new List<int>();
            hints.Add(1);

            TestBpmFlowElement flowSwitch1 = flowchart1.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);

            flowchart1.Elements.Add(flowStep1);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow decision and flow switch to flowchart which are not connected. No validation error.
        /// </summary>        
        [Fact]
        public void FlowDecisionAndFlowSwitchNotConnected()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine start1 = new TestWriteLine("Start", "Executing Start");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine w3 = new TestWriteLine("W3", "Executing W3");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);

            List<int> hints = new List<int>();
            hints.Add(1);

            flowchart1.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);

            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart1.Variables.Add(margin);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestBpmFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, w2True, w2False);
            flowchart1.Elements.Add(tCond);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Start execution of flowchart from flow decision.
        /// </summary>        
        [Fact]
        public void FlowDecisionAsStartElement()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");

            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart1.Variables.Add(margin);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestBpmFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, w2True, w2False);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Start execution of flowchart from flow switch.
        /// </summary>        
        [Fact]
        public void FlowSwitchAsStartElement()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);

            List<int> hints = new List<int>();
            hints.Add(1);

            flowchart1.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);
            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flowdecision with null true element and condition evaluation is true. (Validation error if any one of the true or false element is not connected?)
        /// </summary>        
        [Fact]
        public void FlowDecisionWithTrueElementNullEvaluationTrue()
        {
            // This is a valid testcase and we don't expect error.
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");

            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            Variable<int> margin = new Variable<int> { Name = "margin", Default = 10 };

            flowchart1.Variables.Add(margin);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional((HintTrueFalse[])null)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            }; // null here means neither True or False will happen as the action is null
            TestBpmFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, null, w2False);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// FlowSwitch with cases and default element null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithCasesAndDefaultNull()
        {
            // This is a valid testcase and we don't expect error.
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");

            List<int> hints = new List<int>();
            hints.Add(-1); // It was needed to set it to -1, else testObjects would return error.

            flowchart1.AddSwitchLink<string>(null, (Dictionary<string, TestActivity>)null, hints, "Two", null);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow switch with null expression. Validation exception expected.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithExpressionNull()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestWriteLine start1 = new TestWriteLine("Start", "Executing Start");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);

            List<int> hints = new List<int>();
            hints.Add(-1);

            TestBpmSwitch<object> fSwitch = (TestBpmSwitch<object>)flowchart1.AddSwitchLink<object>(start1, cases, hints, (object)null, wDefault);
            ((FlowSwitch<object>)fSwitch.GetProductElement()).Expression = null; // I had to use the product to set a null value to Expression

            TestRuntime.ValidateInstantiationException(flowchart1, string.Format(ErrorStrings.FlowSwitchRequiresExpression, flowchart1.DisplayName));
        }

        /// <summary>
        /// Add same flow step to parent and child flowchart. Error expected.
        /// </summary>        
        [Fact]
        public void AddSameElementToParentAndChild()
        {
            TestBpmFlowchart flowchart1 = new TestBpmFlowchart("flowChart1");
            TestBpmFlowchart flowchart2 = new TestBpmFlowchart("flowChart2");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestBpmFlowElement fStep = new TestFlowStep(w1);
            flowchart2.Elements.Add(fStep);

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart1.Variables.Add(margin);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestBpmFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, fStep, flowchart2);

            TestRuntime.ValidateInstantiationException(flowchart1, string.Format(ErrorStrings.FlowNodeCannotBeShared, flowchart1.DisplayName, flowchart2.DisplayName));
        }

        /// <summary>
        /// Start execution of flowchart from flow step.
        /// </summary>        
        [Fact]
        public void FlowStepAsStartNode()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddStartLink(new TestWriteLine("Begin", "End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Verify that StartNode need not be in nodes collection.
        /// </summary>        
        [Fact]
        public void StartNodeNotInNodesCollection()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddStartLink(new TestWriteLine("Hello", "Hello"));

            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Validation error expected when start node is null.
        /// </summary>        
        [Fact]
        public void StartNodeNullNodeCollectionNotNull()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddLink(new TestWriteLine("One", "One"), new TestWriteLine("Two", "Two"));

            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).StartNode = null;

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(string.Format(ErrorStrings.FlowchartMissingStartNode, flowchart.DisplayName), flowchart.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(flowchart, constraints, string.Format(ErrorStrings.FlowchartMissingStartNode, flowchart.DisplayName));
        }

        /// <summary>
        /// Validation error expected when start element is null.
        /// </summary>        
        [Fact]
        public void StartNodeNullNodeCollectionNull()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute flowchart which has valid start node and node collection null.
        /// </summary>        
        [Fact]
        public void ValidStartNodeWithNodeCollectionEmpty()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddLink(new TestWriteLine("One", "One"), new TestWriteLine("Two", "Two"));

            ((System.Activities.Statements.BpmFlowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
