// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Xunit;

namespace TestCases.Activities.Flowchart
{
    public class Switch : IDisposable
    {
        /// <summary>
        /// Simple FlowSwitch with three elements
        /// </summary>        
        [Fact]
        public void SimpleFlowSwitchWithThreeElements()
        {
            TestFlowchart flowchart = new TestFlowchart();
            Variable<object> expression = new Variable<object>("expression", context => "Two");

            flowchart.Variables.Add(expression);

            TestWriteLine w1 = new TestWriteLine("One", "One wont execute");
            TestWriteLine w2 = new TestWriteLine("Two", "Two will execute");
            TestWriteLine w3 = new TestWriteLine("Three", "Three wont execute");

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", w1);
            cases.Add("Two", w2);
            cases.Add("Three", w3);

            List<int> hints = new List<int>();
            hints.Add(1);
            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"), cases, hints, expression, new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

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

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart started"), cases, hints, "OnlyOne");

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
        /// FlowSwitch with single default element which gets executed.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithOnlyDefaultElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestWriteLine defaultWrite = new TestWriteLine("Default", "Default");

            Dictionary<string, TestActivity> cases = new Dictionary<string, TestActivity>();
            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink<string>(new TestWriteLine("Start", "Flowchart Started"), cases, hints, "Anything", defaultWrite);

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
            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(1, writeHello);
            cases.Add(2, writeHello);
            cases.Add(3, writeHello);

            List<int> hints = new List<int>() { 0, 1, 2, -1 };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), increment);
            flowchart.AddSwitchLink(increment, cases, hints, e => counter.Get(e));
            flowchart.AddLink(writeHello, increment);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with a case having null flow element.
        /// </summary>        
        [Fact]
        public void FlowSwitchWithNullFlowElement()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<object> expression = new Variable<object>("expression", context => 1);
            flowchart.Variables.Add(expression);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(1, null);

            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"), cases, hints, expression);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with null evaluation.
        /// </summary>        
        [Fact]
        public void FlowSwitchExpressionEvaluationNullExecuteDefault()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<int> expVariable = new Variable<int> { Name = "ExpVar" };
            flowchart.Variables.Add(expVariable);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();

            List<int> hints = new List<int>() { -1 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => expVariable.Get(e), new TestWriteLine("Default", "Default Activity"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// FlowSwitch with default flowchart element in addition to other cases defined. Default gets executed.
        /// </summary>        
        [Fact]
        public void FlowSwitchDefaultExecutionWithMultipleCases()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<object> expressionVariable = new Variable<object>("expression", context => "Four");
            flowchart.Variables.Add(expressionVariable);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(1, new TestWriteLine("ONE", "ONE"));
            cases.Add(2, new TestWriteLine("Two", "Two"));
            cases.Add(3, new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int> { -1 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"),
                cases,
                hints,
                expressionVariable,
                new TestWriteLine("Default", "I am gonna execute"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
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

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));
            cases.Add("Three", new TestWriteLine("Three", "Three"));
            cases.Add("Four", new TestWriteLine("Four", "Four"));
            cases.Add("Five", new TestWriteLine("Five", "Five"));

            List<int> hints = new List<int> { 1 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => stringVar.Get(e), new TestWriteLine("Default", "Default Activity"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
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

        /// <summary>
        /// Simple FlowSwitch with cases collection having key type a user defined type.
        /// </summary>        
        [Fact]
        public void CasesWithAllKeysCustomType()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<object> expressionVariable = new Variable<object>("expression", context => new SwitchExpressionClass(1));
            flowchart.Variables.Add(expressionVariable);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(new SwitchExpressionClass(1), new TestWriteLine("ONE", "ONE"));
            cases.Add(new SwitchExpressionClass(2), new TestWriteLine("Two", "Two"));
            cases.Add(new SwitchExpressionClass(3), new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int> { 0 };

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Flowchart started"),
                cases,
                hints,
                expressionVariable,
                new TestWriteLine("Default", "I am not gonna execute"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add case in flow switch with key null and valid flow element. Evaluate to null.
        /// </summary>        
        [Fact(Skip = "Dependency on Null Handler in TD")]
        public void AddNullKeyInFlowSwitch()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<string> stringVar = VariableHelper.CreateInitialized<string>("stringVar", (string)null);
            flowchart.Variables.Add(stringVar);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));
            cases.Add("Three", new TestWriteLine("Three", "Three"));
            cases.Add("Four", new TestWriteLine("Four", "Four"));

            List<int> hints = new List<int> { 3 };

            TestFlowSwitch<object> flowSwitch = flowchart.AddSwitchLink<object>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => stringVar.Get(e), new TestWriteLine("Default", "Default Activity")) as TestFlowSwitch<object>;
            flowSwitch.AddNullCase(new TestWriteLine("Five", "Five"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Add case in flow switch with both key and value null. Evaluate to null.
        /// </summary>        
        [Fact(Skip = "Dependency on Null Handler in TD")]
        public void FlowSwitchWithBothKeyAndValueNull()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<string> stringVar = VariableHelper.CreateInitialized<string>("stringVar", (string)null);
            flowchart.Variables.Add(stringVar);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));
            cases.Add("Three", new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int> { 3 };

            TestFlowSwitch<object> flowSwitch = flowchart.AddSwitchLink<object>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => stringVar.Get(e)) as TestFlowSwitch<object>;
            flowSwitch.AddNullCase(null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        [Fact]
        public void FlowSwitchHavingCaseWithNullKeyEvaluateNull()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<string> stringVar = VariableHelper.CreateInitialized<string>("stringVar", (string)null);
            flowchart.Variables.Add(stringVar);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));
            cases.Add("Three", new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int>() { 3 };

            TestFlowSwitch<object> flowSwitch = flowchart.AddSwitchLink<object>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => stringVar.Get(e), new TestWriteLine("Default", "Default Activity")) as TestFlowSwitch<object>;
            flowSwitch.AddNullCase(new TestWriteLine("Four", "Four"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }

    public class SwitchExpressionClass
    {
        private int _i;

        public SwitchExpressionClass() { }

        public SwitchExpressionClass(int index)
        {
            _i = index;
        }

        public int Index
        {
            get { return _i; }
            set { _i = value; }
        }

        public override bool Equals(object obj)
        {

            if (!(obj is SwitchExpressionClass s)) return false;

            if (s._i == _i) return true;

            return false;
        }

        public override int GetHashCode()
        {
            return _i;
        }
    }
}


