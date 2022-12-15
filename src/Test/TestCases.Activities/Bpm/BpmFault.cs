// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Bpm;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmFault : IDisposable
    {
        /// <summary>
        /// Flowchart fault not handled/linked
        /// </summary>        
        [Fact]
        public void Flowchart_FaultNotHandled()
        {
            Variable<Exception> exception = new Variable<Exception>();
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            flowchart.Variables.Add(exception);

            TestSequence faultySequence = new TestSequence
            {
                Activities =
                {
                    new TestThrow<InvalidOperationException>("Faulty Little Sequence")
                    {
                        ExceptionExpression = (context => new InvalidOperationException("I am a faulty little sequence's exception"))
                    }
                }
            };
            TestWriteLine writeLine1 = new TestWriteLine("WriteStatus", "I wont execute");
            TestBpmFlowElement step = flowchart.AddLink(faultySequence, writeLine1);
            step.IsFaulting = true;

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(InvalidOperationException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Fault handled from flowchart in a try catch block.
        /// </summary>
        /// Disabled in desktop and failing.        
        //[Fact]
        private void FlowchartInTryCatchBlock_FaultHandled()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            flowchart.AddStartLink(new TestThrow<Exception>()
            {
                ExpectedOutcome = Outcome.CaughtException()
            });

            TestTryCatch tryCatchFinally = new TestTryCatch
            {
                Try = flowchart,
                Catches =
                {
                    new TestCatch<Exception>
                    {
                        Body = new TestWriteLine("ExceptionHandler", "Handled"),
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(tryCatchFinally);
        }

        /// <summary>
        /// Exception thrown during expression evaluation.
        /// </summary> 
        /// This test is disabled in desktop and failing too.       
        //[Fact]
        private void FaultWhileExpressionEvaluation()
        {
            const string exceptionString = "I am a faulty little expression's exception";
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestExpressionEvaluatorWithBody<bool> faultyExpression = new TestExpressionEvaluatorWithBody<bool>
            {
                Body = new TestThrow<Exception>("Faulty Little Expression")
                {
                    ExceptionExpression = (context => new Exception(exceptionString))
                }
            };
            TestWriteLine writeLine1 = new TestWriteLine("WriteStatus", "I will execute");

            TestBpmFlowConditional conditional = new TestBpmFlowConditional() { ConditionValueExpression = faultyExpression };

            flowchart.AddConditionalLink(writeLine1, conditional, new TestWriteLine("True", "True"), new TestWriteLine("False", "False"));

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(Exception), new Dictionary<string, string>());
        }

        /// <summary>
        /// Exception thrown during expression evaluation of switch.
        /// Exception thrown during expression evaluation of switch
        /// </summary>
        /// This test is disabled in desktop and failing too.        
        //[Fact]
        private void FaultWhileSwitchExpressionEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestExpressionEvaluatorWithBody<object> faultExp = new TestExpressionEvaluatorWithBody<object>
            {
                Body = new TestThrow<InvalidProgramException>()
            };

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", new TestWriteLine("One", "One"));
            cases.Add("Two", new TestWriteLine("Two", "Two"));

            flowchart.AddSwitchLink(new TestWriteLine("Start", "Start"), cases, new List<int>() { -1 }, faultExp, new TestWriteLine("Default", "Default"));

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(InvalidProgramException), null);
        }

        /// <summary>
        /// Throw exception while executing activity in a loop.
        /// </summary>        
        [Fact]
        public void FaultWhileExecutingInLoop()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>(-3);
            counter.Name = "counter";
            flowchart.Variables.Add(counter);

            List<HintTrueFalse> hints = new List<HintTrueFalse> { HintTrueFalse.True, HintTrueFalse.True, HintTrueFalse.Exception };
            TestBpmFlowConditional conditional = new TestBpmFlowConditional(hints.ToArray()) { ConditionExpression = env => (counter.Get(env) - 1) / counter.Get(env) > 0 };

            TestIncrement decByOne = new TestIncrement { CounterVariable = counter, IncrementCount = 1 };

            flowchart.AddLink(new TestWriteLine("Start", "Start"), decByOne);

            flowchart.AddConditionalLink(decByOne, conditional, decByOne, null);

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(DivideByZeroException), null);
        }

        /// <summary>
        /// Exception thrown from 5 level deep nested flowchart.
        /// Exception thrown from 5 level deep nested flowchart. All the parent flowcharts should get faulted and workflow instance should terminate.
        /// </summary>        
        [Fact]
        public void FaultFromFiveLevelDeepNestedFlowchart()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child1 = new TestBpmFlowchart();
            TestBpmFlowchart child2 = new TestBpmFlowchart();
            TestBpmFlowchart child3 = new TestBpmFlowchart();
            TestBpmFlowchart child4 = new TestBpmFlowchart();

            child4.AddStartLink(new TestThrow<WorkflowApplicationAbortedException>());

            parent.AddStartLink(child1);
            child1.AddStartLink(child2);
            child2.AddStartLink(child3);
            child3.AddStartLink(child4);

            TestRuntime.RunAndValidateAbortedException(parent, typeof(WorkflowApplicationAbortedException), null);
        }

        /// <summary>
        /// Exception thrown from 5 level deep nested flowchart and handled at the top level.
        /// </summary>        
        [Fact]
        public void FaultFromFiveLevelDeepNestedFlowchart_Handled()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child1 = new TestBpmFlowchart();
            TestBpmFlowchart child2 = new TestBpmFlowchart();
            TestBpmFlowchart child3 = new TestBpmFlowchart();
            TestBpmFlowchart child4 = new TestBpmFlowchart();

            child4.AddStartLink(new TestThrow<WorkflowApplicationAbortedException>() { ExpectedOutcome = Outcome.CaughtException() });

            child1.AddStartLink(child2);
            child2.AddStartLink(child3);
            child3.AddStartLink(child4);

            TestTryCatch tryCatchFinally = new TestTryCatch
            {
                Try = child1
            };
            tryCatchFinally.Catches.Add(new TestCatch<WorkflowApplicationAbortedException>());

            parent.AddStartLink(tryCatchFinally);

            TestRuntime.RunAndValidateWorkflow(parent);
        }

        public void Dispose()
        {
        }
    }
}
