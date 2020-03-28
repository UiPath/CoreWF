// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Flowchart
{
    public class ObjectModel : IDisposable
    {
        /// <summary>
        /// Display name null.
        /// </summary>        
        [Fact]
        public void DisplayNameNull()
        {
            TestFlowchart flowchart = new TestFlowchart();

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

            FlowStep flowStep1 = new FlowStep { Action = writeLine1 };
            FlowStep flowStep2 = new FlowStep { Action = writeLine2, Next = flowStep1 };
            FlowStep flowStep3 = new FlowStep { Action = writeLine3, Next = flowStep2 };

            System.Activities.Statements.Flowchart flowchart = new System.Activities.Statements.Flowchart
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
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestFlowConditional conditional1 = new TestFlowConditional
            {
                Condition = true
            };

            TestFlowConditional conditional2 = new TestFlowConditional
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
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestFlowConditional conditional1 = new TestFlowConditional(HintTrueFalse.False)
            {
                Condition = false
            };

            TestFlowConditional conditional2 = new TestFlowConditional
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
            TestFlowchart flowchart = new TestFlowchart();

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
            TestFlowConditional conditional1 = new TestFlowConditional(hints.ToArray())
            {
                ConditionExpression = (e => counter.Get(e) == 5)
            };

            TestFlowConditional conditional2 = new TestFlowConditional
            {
                Condition = true
            };

            TestFlowConditional conditional3 = new TestFlowConditional
            {
                Condition = true
            };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), assign);
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
            TestFlowchart flowchart = new TestFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            flowchart.Variables.Add(counter);

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");

            TestAssign<int> assign = new TestAssign<int>("assign")
            {
                ValueExpression = (e => counter.Get(e) + 1),
                ToVariable = counter
            };

            TestFlowConditional conditional1 = new TestFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (e => counter.Get(e) == 5)
            };

            TestFlowConditional conditional2 = new TestFlowConditional(HintTrueFalse.False)
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
        /// Flowchart without an explicit or implicit start event.
        /// </summary>        
        [Fact]
        public void FlowchartWithoutExplicitOrImplicitStartEvent()
        {
            TestFlowchart flowchart = new TestFlowchart();

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

            TestFlowConditional conditional = new TestFlowConditional(hintsList.ToArray())
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
            TestFlowchart flow = new TestFlowchart();
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
            TestFlowConditional conditional = new TestFlowConditional
            {
                Condition = false
            };
            TestFlowchart flowchart = new TestFlowchart();

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
            TestFlowchart flow = new TestFlowchart();

            flow.AddStartLink(delay);

            TestRuntime.RunAndValidateWorkflow(flow);
        }

        /// <summary>
        /// Add same element twice to the elements collection.
        /// </summary>        
        [Fact]
        public void AddSameElementTwiceToFlowchartElementsCollection()
        {
            TestFlowchart flowchart = new TestFlowchart();
            TestFlowStep step = new TestFlowStep
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
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.Elements.Add(new TestWriteLine("StartAndEnd", "StartAndEnd"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Clear elements collection at design time.
        /// </summary>        
        [Fact]
        public void ClearElementsCollectionAtDesignTime()
        {
            TestFlowchart flowchart = new TestFlowchart();

            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Add(new System.Activities.Statements.FlowStep { Action = new Delay() });
            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Add(new System.Activities.Statements.FlowStep { Action = new Delay() });

            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add element to elements collection at design time.
        /// </summary>        
        [Fact]
        public void AddElementToCollectionAtDesignTime()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.Elements.Add(new TestFlowStep() { ActionActivity = new TestWriteLine("StartAndEnd", "StartAndEnd") });

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Remove element from elements collection at design time.
        /// </summary>        
        [Fact]
        public void RemoveElementFromCollectionAtDesignTime()
        {
            TestFlowchart flowchart = new TestFlowchart();
            FlowStep step = new FlowStep() { Action = new Delay() };

            flowchart.Elements.Add(new TestFlowStep() { ActionActivity = new TestWriteLine("Start", "Start") });
            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Add(step);
            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Remove(step);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to another flow step.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowStep()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddLink(new TestWriteLine("Start", "Start"), new TestWriteLine("End", "End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to flow decision.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowDecision()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine w1 = new TestWriteLine("Start", "Start");

            TestFlowConditional conditional = new TestFlowConditional() { Condition = true };

            flowchart.AddConditionalLink(w1, conditional, new TestWriteLine("True", "True"), (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect flow step to flow switch.
        /// </summary>        
        [Fact]
        public void FlowStepConnectedToFlowSwitch()
        {
            TestFlowchart flowchart = new TestFlowchart();

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
            TestFlowchart parentFlowchart = new TestFlowchart("Parent");
            TestFlowchart childFlowchart = new TestFlowchart("Child");

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);


            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.True)
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
            TestFlowchart flowchart = new TestFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine w1 = new TestWriteLine("WriteLine1", "Executing WriteLine1");
            TestWriteLine w2 = new TestWriteLine("WriteLine2", "Executing WriteLine2");
            TestWriteLine w3 = new TestWriteLine("WriteLine3", "Executing WriteLine3");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (context => counter.Get(context) > 4)
            };

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);
            cases.Add("Three", w3);

            List<int> hints = new List<int>();
            hints.Add(1);

            TestFlowElement switchElement = flowchart.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, switchElement);
            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Connect a case element in flow switch to flow decision.
        /// </summary>        
        [Fact]
        public void FlowSwitchConnectedToFlowDecision()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine wStart = new TestWriteLine("Start", "Flowchart started");
            TestWriteLine wDefault = new TestWriteLine("Default", "Default");
            TestWriteLine w1 = new TestWriteLine("One", "One wont execute");
            TestWriteLine w3 = new TestWriteLine("Three", "Three wont execute");
            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            TestFlowStep fs1 = new TestFlowStep(w1);
            TestFlowStep fs3 = new TestFlowStep(w3);

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart.Variables.Add(margin);
            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            flowchart.AddConditionalLink(null, flowDecision, w2True, w2False);

            Dictionary<string, TestFlowElement> cases = new Dictionary<string, TestFlowElement>();
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestFlowStep flowStep1 = new TestFlowStep();
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestFlowStep flowStep1 = new TestFlowStep(new TestWriteLine("W1", "W1"));
            TestFlowStep flowStep2 = new TestFlowStep(new TestWriteLine("W2", "W2"));
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine w3 = new TestWriteLine("W3", "Executing W3");

            TestFlowConditional flowCond1 = new TestFlowConditional(HintTrueFalse.True)
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestWriteLine start1 = new TestWriteLine("Start", "Executing Start");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine w3 = new TestWriteLine("W3", "Executing W3");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            TestFlowStep flowStep1 = new TestFlowStep(w1);

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", w2);
            cases.Add("Two", w3);

            List<int> hints = new List<int>();
            hints.Add(1);

            TestFlowElement flowSwitch1 = flowchart1.AddSwitchLink<string>(null, cases, hints, "Two", wDefault);

            flowchart1.Elements.Add(flowStep1);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Add flow decision and flow switch to flowchart which are not connected. No validation error.
        /// </summary>        
        [Fact]
        public void FlowDecisionAndFlowSwitchNotConnected()
        {
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
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
            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, w2True, w2False);
            flowchart1.Elements.Add(tCond);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Start execution of flowchart from flow decision.
        /// </summary>        
        [Fact]
        public void FlowDecisionAsStartElement()
        {
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");

            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart1.Variables.Add(margin);
            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, w2True, w2False);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// Start execution of flowchart from flow switch.
        /// </summary>        
        [Fact]
        public void FlowSwitchAsStartElement()
        {
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");

            TestWriteLine w2True = new TestWriteLine("True", "True will execute");
            TestWriteLine w2False = new TestWriteLine("False", "False wont execute");

            Variable<int> margin = new Variable<int> { Name = "margin", Default = 10 };

            flowchart1.Variables.Add(margin);
            TestFlowConditional flowDecision = new TestFlowConditional((HintTrueFalse[])null)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            }; // null here means neither True or False will happen as the action is null
            TestFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, null, w2False);

            TestRuntime.RunAndValidateWorkflow(flowchart1);
        }

        /// <summary>
        /// FlowSwitch with cases and default element null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithCasesAndDefaultNull()
        {
            // This is a valid testcase and we don't expect error.
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");

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
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestWriteLine start1 = new TestWriteLine("Start", "Executing Start");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestWriteLine w2 = new TestWriteLine("W2", "Executing W2");
            TestWriteLine wDefault = new TestWriteLine("wDefault", "Executing wDefault");

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);

            List<int> hints = new List<int>();
            hints.Add(-1);

            TestFlowSwitch<object> fSwitch = (TestFlowSwitch<object>)flowchart1.AddSwitchLink<object>(start1, cases, hints, (object)null, wDefault);
            ((FlowSwitch<object>)fSwitch.GetProductElement()).Expression = null; // I had to use the product to set a null value to Expression

            TestRuntime.ValidateInstantiationException(flowchart1, string.Format(ErrorStrings.FlowSwitchRequiresExpression, flowchart1.DisplayName));
        }

        /// <summary>
        /// Add same flow step to parent and child flowchart. Error expected.
        /// </summary>        
        [Fact]
        public void AddSameElementToParentAndChild()
        {
            TestFlowchart flowchart1 = new TestFlowchart("flowChart1");
            TestFlowchart flowchart2 = new TestFlowchart("flowChart2");
            TestWriteLine w1 = new TestWriteLine("W1", "Executing W1");
            TestFlowElement fStep = new TestFlowStep(w1);
            flowchart2.Elements.Add(fStep);

            Variable<int> margin = VariableHelper.CreateInitialized<int>("Margin", 10);
            flowchart1.Variables.Add(margin);
            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.True)
            {
                ConditionExpression = (context => margin.Get(context) > 0)
            };
            TestFlowElement tCond = flowchart1.AddConditionalLink(null, flowDecision, fStep, flowchart2);

            TestRuntime.ValidateInstantiationException(flowchart1, string.Format(ErrorStrings.FlowNodeCannotBeShared, flowchart1.DisplayName, flowchart2.DisplayName));
        }

        /// <summary>
        /// Start execution of flowchart from flow step.
        /// </summary>        
        [Fact]
        public void FlowStepAsStartNode()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddStartLink(new TestWriteLine("Begin", "End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Verify that StartNode need not be in nodes collection.
        /// </summary>        
        [Fact]
        public void StartNodeNotInNodesCollection()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddStartLink(new TestWriteLine("Hello", "Hello"));

            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Validation error expected when start node is null.
        /// </summary>        
        [Fact]
        public void StartNodeNullNodeCollectionNotNull()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddLink(new TestWriteLine("One", "One"), new TestWriteLine("Two", "Two"));

            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).StartNode = null;

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
            TestFlowchart flowchart = new TestFlowchart();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute flowchart which has valid start node and node collection null.
        /// </summary>        
        [Fact]
        public void ValidStartNodeWithNodeCollectionEmpty()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddLink(new TestWriteLine("One", "One"), new TestWriteLine("Two", "Two"));

            ((System.Activities.Statements.Flowchart)flowchart.ProductActivity).Nodes.Clear();

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
