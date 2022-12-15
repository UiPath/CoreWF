// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmGenericSwitch : IDisposable
    {
        /// <summary>
        /// FlowSwitch with single element.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithOneElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine writeHello = new TestWriteLine("Hello", "Hello");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("OnlyOne", writeHello);

            List<int> hints = new List<int>();
            hints.Add(0);

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart started"), cases, hints, "OnlyOne", new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }
        /// <summary>
        /// Simple FlowSwitch with three elements
        /// </summary>        
        [Fact]
        public void SimpleFlowSwitchWithThreeElements()
        {
            TestFlowchart flowchart = new TestFlowchart();
            Variable<int> expression = new Variable<int> { Name = "expression", Default = 2 };

            flowchart.Variables.Add(expression);

            TestWriteLine w1 = new TestWriteLine("One", "One wont execute");
            TestWriteLine w2 = new TestWriteLine("Two", "Two will execute");
            TestWriteLine w3 = new TestWriteLine("Three", "Three wont execute");

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, w1);
            cases.Add(2, w2);
            cases.Add(3, w3);

            List<int> hints = new List<int>();
            hints.Add(1);
            flowchart.AddSwitchLink<int>(new TestWriteLine("Start", "Flowchart started"), cases, hints, expression, new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with single element which doesnt execute.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithOneNonExecutingElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine writeHello = new TestWriteLine("Hello", "Hello");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("OnlyOne", writeHello);

            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart started"), cases, hints, "None");

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Switch expression on custom type.
        /// Define expression of custom type
        /// </summary>        
        [Fact]
        public void SwitchExpressionOnCustomtype()
        {
            TestFlowchart flow = new TestFlowchart();

            Complex defaultValue = new Complex(3, 3);

            Variable<Complex> complexVar = new Variable<Complex>("Complex", context => defaultValue);
            flow.Variables.Add(complexVar);

            Dictionary<Complex, TestActivity> cases = new Dictionary<Complex, TestActivity>();
            cases.Add(new Complex(1, 3), new TestWriteLine("Hello1", "Hello1"));
            cases.Add(new Complex(2, 3), new TestWriteLine("Hello2", "Hello2"));
            cases.Add(defaultValue, new TestWriteLine("Hello3", "Hello3"));
            cases.Add(new Complex(4, 3), new TestWriteLine("Hello4", "Hello4"));

            List<int> hints = new List<int> { 2 };

            flow.AddSwitchLink<Complex>(new TestWriteLine("Start", "Start"), cases, hints, complexVar, new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateWorkflow(flow);
        }

        /// <summary>
        /// Define expression on existing variable.
        /// </summary>        
        [Fact]
        public void SwitchExpressionOnExistingVariable()
        {
            TestFlowchart flowchart = new TestFlowchart();

            const string defaultValue = "Two";
            Variable<string> stringVar = VariableHelper.CreateInitialized<string>("stringVar", defaultValue);
            flowchart.Variables.Add(stringVar);

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));
            cases.Add("Three", new TestWriteLine("Three", "Three"));
            cases.Add("Four", new TestWriteLine("Four", "Four"));
            cases.Add("Five", new TestWriteLine("Five", "Five"));

            List<int> hints = new List<int> { 1 };

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => stringVar.Get(e), new TestWriteLine("Default", "Default Activity"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with single default element which gets executed.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithOnlyDefaultElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine defaultWrite = new TestWriteLine("Default", "Default");

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink<int>(new TestWriteLine("Start", "Flowchart Started"), cases, hints, 3, defaultWrite);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with null evaluation.
        /// FlowSwitch with empty evaluation of Expression.
        /// </summary>        
        [Fact]
        public void FlowSwitchExpressionEvaluationEmptyExecuteEmptyCase()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<string> expVariable = new Variable<string> { Name = "ExpVar", Default = string.Empty };
            flowchart.Variables.Add(expVariable);

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add(string.Empty, new TestWriteLine("Hello0", "Hello0"));
            cases.Add("One", new TestWriteLine("Hello1", "Hello1"));

            List<int> hints = new List<int>() { 0 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => expVariable.Get(e), new TestWriteLine("Default", "Default Activity"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with a case having null FlowNode.
        /// FlowSwitch with a case having null flow element.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithNullFlowNode()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<int> expression = new Variable<int> { Name = "expression", Default = 1 };
            flowchart.Variables.Add(expression);

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, null);

            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink<int>(new TestWriteLine("Start", "Flowchart started"), cases, hints, expression);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with default flowchart element in addition to other cases defined. Default gets executed.
        /// </summary>        
        [Fact]
        public void FlowSwitchDefaultExecutionWithMultipleCases()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<int> expressionVariable = new Variable<int> { Name = "expression", Default = 4 };
            flowchart.Variables.Add(expressionVariable);

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, new TestWriteLine("ONE", "ONE"));
            cases.Add(2, new TestWriteLine("Two", "Two"));
            cases.Add(3, new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int> { -1 };

            flowchart.AddSwitchLink<int>(new TestWriteLine("Start", "Flowchart started"),
                cases,
                hints,
                expressionVariable,
                new TestWriteLine("Default", "I am gonna execute"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with null evaluation.
        /// </summary>        
        [Fact]
        public void FlowSwitchExpressionEvaluationNullExecuteDefault()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<string> expVariable = new Variable<string> { Name = "ExpVar" };
            flowchart.Variables.Add(expVariable);

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", new TestWriteLine("Hello1", "Hello1"));
            cases.Add("Two", new TestWriteLine("Hello2", "Hello2"));
            cases.Add("Three", new TestWriteLine("Hello3", "Hello3"));
            cases.Add(string.Empty, new TestWriteLine("Hello4", "Hello4"));

            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => expVariable.Get(e), new TestWriteLine("Default", "Default Activity"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with all cases pointing to same element.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithAllCasesHavingSameElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            flowchart.Variables.Add(counter);

            TestIncrement increment = new TestIncrement("Inc", 1)
            {
                CounterVariable = counter
            };

            TestWriteLine writeHello = new TestWriteLine("Hello", "Ola");
            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, writeHello);
            cases.Add(2, writeHello);
            cases.Add(3, writeHello);

            List<int> hints = new List<int>() { 0, 1, 2, -1 };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), increment);
            flowchart.AddSwitchLink<int>(increment, cases, hints, e => counter.Get(e));
            flowchart.AddLink(writeHello, increment);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        [Fact]
        public void FlowSwitchHavingCaseWithNullKeyEvaluateNull()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<Complex> complexVar = VariableHelper.CreateInitialized<Complex>("complexVar", (Complex)null);
            flowchart.Variables.Add(complexVar);

            Dictionary<Complex, TestActivity> cases = new Dictionary<Complex, TestActivity>();
            cases.Add(new Complex(0, 0), new TestWriteLine("One", "One"));
            cases.Add(new Complex(1, 0), new TestWriteLine("Two", "Two"));
            cases.Add(new Complex(2, 0), new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int>() { -1 };

            TestFlowSwitch<Complex> flowSwitch = flowchart.AddSwitchLink<Complex>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => complexVar.Get(e)) as TestFlowSwitch<Complex>;
            ((FlowSwitch<Complex>)flowSwitch.GetProductElement()).Cases.Add(null, new FlowStep { Action = new BlockingActivity("Blocking") });

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Blocking", TestActivityInstanceState.Executing);
                runtime.ResumeBookMark("Blocking", null);

                runtime.WaitForCompletion(false);
            }
        }

        /// <summary>
        /// Add case in flow switch where the node points to the containing FlowSwitch
        /// </summary>        
        [Fact]
        public void FlowSwitchWithNodePointingToParentFlowSwitch()
        {
            TestFlowchart flowchart = new TestFlowchart();
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            flowchart.Variables.Add(counter);

            TestIncrement increment = new TestIncrement("Inc", 1)
            {
                CounterVariable = counter
            };

            TestWriteLine w1 = new TestWriteLine("One", "Will execute on first iteration");
            TestWriteLine w2 = new TestWriteLine("Two", "Will execute on second iteration");
            TestWriteLine w3 = new TestWriteLine("Three", "Will execute on third iteration");
            TestWriteLine w4 = new TestWriteLine("Four", "Will execute on final iteration");

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, w1);
            cases.Add(2, w2);
            cases.Add(3, w3);

            List<int> hints = new List<int>();
            hints.Add(0);
            hints.Add(1);
            hints.Add(2);
            hints.Add(-1);

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), increment);

            flowchart.AddSwitchLink<int>(increment, cases, hints, env => counter.Get(env), w4);
            flowchart.AddLink(w1, increment);
            flowchart.AddLink(w2, increment);
            flowchart.AddLink(w3, increment);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Throw while executing FlowNode.
        /// </summary>        
        [Fact]
        public void ThrowFromNode()
        {
            TestFlowchart flowchart = new TestFlowchart();
            Variable<int> expression = new Variable<int> { Name = "expression", Default = 3 };

            flowchart.Variables.Add(expression);

            TestWriteLine w1 = new TestWriteLine("One", "One wont execute");
            TestWriteLine w2 = new TestWriteLine("Two", "Two will execute");
            TestThrow<Exception> throwAct = new TestThrow<Exception>();

            Dictionary<int, TestActivity> cases = new Dictionary<int, TestActivity>();
            cases.Add(1, w1);
            cases.Add(2, w2);
            cases.Add(3, throwAct);

            List<int> hints = new List<int>();
            hints.Add(2);
            flowchart.AddSwitchLink<int>(new TestWriteLine("Start", "Flowchart started"), cases, hints, expression, new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(Exception), null);
        }
        /// <summary>
        /// Throw while evaluating expression.
        /// </summary>        
        [Fact]
        public void ThrowWhileEvaluatingExpression()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine writeHello = new TestWriteLine("Hello", "Hello");

            TestWriteLine writeStart = new TestWriteLine("Start", "Start");

            TestExpressionEvaluatorWithBody<string> expressionActivity = new TestExpressionEvaluatorWithBody<string>("One")
            {
                Body = new TestThrow<ArgumentOutOfRangeException>()
            };

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One will not execute"));
            cases.Add("Two", new TestWriteLine("Two", "Two will not execute"));

            List<int> hints = new List<int>() { -1 };

            flowchart.AddStartLink(writeStart);

            flowchart.AddSwitchLink<string>(writeStart, cases, hints, expressionActivity, new TestWriteLine("Default", "Will not execute"));

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(ArgumentOutOfRangeException), null);
        }

        public void Dispose()
        {
        }
    }
}
