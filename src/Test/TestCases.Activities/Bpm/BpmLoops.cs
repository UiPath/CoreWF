// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Bpm;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmLoops : IDisposable
    {
        /// <summary>
        /// Execute multiple activities in a loop
        /// </summary>        
        [Fact]
        public void MultipleActivitiesInLoop()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestAssign<int> assign = new TestAssign<int>("Assign1")
            {
                ValueExpression = ((env) => counter.Get(env) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            hints.Add(HintTrueFalse.False);
            hints.Add(HintTrueFalse.False);
            hints.Add(HintTrueFalse.True);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (context => counter.Get(context) == 3)
            };

            flowchart.AddLink(writeLine1, assign);

            flowchart.AddLink(assign, writeLine2);

            flowchart.AddConditionalLink(writeLine2, flowDecision, writeLine3, assign);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// For loop modelled in flowchart.
        /// </summary>        
        [Fact]
        public void Flowchart_Forloop()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestAssign<int> assign = new TestAssign<int>("Assign1")
            {
                ValueExpression = ((env) => ((int)counter.Get(env)) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            for (int i = 0; i < 49; i++)
            {
                hints.Add(HintTrueFalse.True);
            }
            hints.Add(HintTrueFalse.False);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (context => counter.Get(context) < 50)
            };

            flowchart.AddLink(writeLine1, assign);

            flowchart.AddConditionalLink(assign, flowDecision, assign, writeLine2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Foreach modelled in flowchart.
        /// </summary>        
        [Fact]
        public void Flowchart_ForEach()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            List<int> tenInts = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            Variable<List<int>> listOfInts = new Variable<List<int>>("listOfInts",
                (env) => new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            flowchart.Variables.Add(listOfInts);

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            flowchart.Variables.Add(counter);

            Variable<bool> boolVar = VariableHelper.CreateInitialized<bool>("boolVar", true);
            flowchart.Variables.Add(boolVar);

            Variable<IEnumerator<int>> intEnumerator = VariableHelper.Create<IEnumerator<int>>("intEnumerator");
            flowchart.Variables.Add(intEnumerator);

            TestAssign<IEnumerator<int>> assign1 = new TestAssign<IEnumerator<int>>("Assign1")
            {
                ValueExpression = ((env) => ((List<int>)listOfInts.Get(env)).GetEnumerator()),
                ToVariable = intEnumerator
            };

            TestAssign<bool> assign2 = new TestAssign<bool>("Assign2")
            {
                ValueExpression = ((env) => ((IEnumerator<int>)intEnumerator.Get(env)).MoveNext()),
                ToVariable = boolVar
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            for (int i = 0; i < 10; i++)
            {
                hints.Add(HintTrueFalse.True);
            }
            hints.Add(HintTrueFalse.False);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (context => boolVar.Get(context) == true)
            };

            flowchart.AddStartLink(assign1);

            flowchart.AddLink(assign1, assign2);

            flowchart.AddConditionalLink(assign2, flowDecision, assign2, new TestWriteLine("End", "Flowchart ended"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Variable defined on flowchart being used in a loop.
        /// </summary>        
        [Fact]
        public void FlowchartVariableUseInLoop()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            flowchart.Variables.Add(counter);

            TestAssign<int> assign = new TestAssign<int>("Assign1")
            {
                ValueExpression = ((env) => ((int)counter.Get(env)) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            for (int i = 0; i < 9; i++)
            {
                hints.Add(HintTrueFalse.True);
            }
            hints.Add(HintTrueFalse.False);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (context => counter.Get(context) < 10)
            };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart started"), assign);

            flowchart.AddConditionalLink(assign, flowDecision, assign, new TestWriteLine("End", "Flowchart ended"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Nested loops execution in flowchart.
        /// </summary>        
        [Fact]
        public void NestedLoopsExecution()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            Variable<int> innerLoopcounter = VariableHelper.CreateInitialized<int>("InnerLoopCounter", 0);
            flowchart.Variables.Add(innerLoopcounter);

            Variable<int> outerLoopCounter = VariableHelper.CreateInitialized<int>("OuterLoopCounter", 0);
            flowchart.Variables.Add(outerLoopCounter);

            TestAssign<int> outerAssign = new TestAssign<int>("OuterAssign")
            {
                ValueExpression = ((env) => ((int)outerLoopCounter.Get(env)) + 1),
                ToVariable = outerLoopCounter
            };

            TestAssign<int> innerAssign = new TestAssign<int>("InnerAssign")
            {
                ValueExpression = (env) => ((int)innerLoopcounter.Get(env)) + 1,
                ToVariable = innerLoopcounter
            };

            TestAssign<int> resetInnerCounter = new TestAssign<int>("ResetInnerCounter")
            {
                Value = 0,
                ToVariable = innerLoopcounter
            };

            List<HintTrueFalse> outerHints = new List<HintTrueFalse>();
            for (int i = 0; i < 9; i++)
            {
                outerHints.Add(HintTrueFalse.True);
            }
            outerHints.Add(HintTrueFalse.False);
            TestBpmFlowConditional outerFlowDecision = new TestBpmFlowConditional(outerHints.ToArray())
            {
                ConditionExpression = (context => outerLoopCounter.Get(context) < 10)
            };

            List<HintTrueFalse> innerHints = new List<HintTrueFalse>();
            for (int i = 0; i < 4; i++)
            {
                innerHints.Add(HintTrueFalse.True);
            }
            innerHints.Add(HintTrueFalse.False);
            TestBpmFlowConditional innerFlowDecision = new TestBpmFlowConditional(innerHints.ToArray())
            {
                ConditionExpression = (context => innerLoopcounter.Get(context) < 5),
                ResetHints = true
            };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart started"), outerAssign);

            flowchart.AddLink(outerAssign, resetInnerCounter);

            flowchart.AddLink(resetInnerCounter, innerAssign);

            flowchart.AddConditionalLink(innerAssign, innerFlowDecision, innerAssign, outerFlowDecision);

            flowchart.AddConditionalLink(null, outerFlowDecision, outerAssign, new TestWriteLine("End", "Flowchart completed"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute flowchart in procedural while activity in a loop.
        /// </summary>        
        [Fact]
        public void FlowchartInProceduralWhile()
        {
            TestSequence s = new TestSequence("seq1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            s.Variables.Add(counter);

            TestWhile w = new TestWhile("While1");
            s.Activities.Add(w);

            TestBpmFlowchart f = new TestBpmFlowchart("Flow1");

            TestAssign<int> assign = new TestAssign<int>("Assign1")
            {
                ValueExpression = (env => ((int)counter.Get(env)) + 1),
                ToVariable = counter
            };

            w.ConditionExpression = (env => counter.Get(env) < 5);
            w.HintIterationCount = 5;

            f.AddStartLink(assign);

            w.Body = f;

            TestRuntime.RunAndValidateWorkflow(s);
        }

        /// <summary>
        /// Execute nested flowchart in loop.
        /// </summary>        
        [Fact]
        public void NestedFlowchartInLoop()
        {
            TestBpmFlowchart parentFlowchart = new TestBpmFlowchart("ParentFlowchart");

            TestBpmFlowchart childFlowchart = new TestBpmFlowchart("ChildFlowchart");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            parentFlowchart.Variables.Add(counter);

            TestAssign<int> assign = new TestAssign<int>("Assign1")
            {
                ValueExpression = (env => ((int)counter.Get(env)) + 1),
                ToVariable = counter
            };

            List<HintTrueFalse> hints = new List<HintTrueFalse>();
            for (int i = 0; i < 5; i++)
            {
                hints.Add(HintTrueFalse.True);
            }
            hints.Add(HintTrueFalse.False);
            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(hints.ToArray())
            {
                ConditionExpression = (env => counter.Get(env) <= 5)
            };

            parentFlowchart.AddLink(new TestWriteLine("Start", "Parent started"), childFlowchart);

            parentFlowchart.AddConditionalLink(childFlowchart, flowDecision, childFlowchart, new TestWriteLine("End", "Parent ended"));

            childFlowchart.AddStartLink(assign);

            TestRuntime.RunAndValidateWorkflow(parentFlowchart);
        }

        /// <summary>
        /// Execute 5 levels deep nested loops.
        /// </summary>        
        [Fact]
        public void ExecuteFiveLevelDeepNestedLoops()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flowchart1");

            Variable<int> loop1Counter = VariableHelper.CreateInitialized<int>("Loop1Counter", 0);
            flowchart.Variables.Add(loop1Counter);

            Variable<int> loop2Counter = VariableHelper.CreateInitialized<int>("Loop2Counter", 0);
            flowchart.Variables.Add(loop2Counter);

            Variable<int> loop3Counter = VariableHelper.CreateInitialized<int>("Loop3Counter", 0);
            flowchart.Variables.Add(loop3Counter);

            Variable<int> loop4Counter = VariableHelper.CreateInitialized<int>("Loop4Counter", 0);
            flowchart.Variables.Add(loop4Counter);

            Variable<int> loop5Counter = VariableHelper.CreateInitialized<int>("Loop5Counter", 0);
            flowchart.Variables.Add(loop5Counter);

            TestAssign<int> assign1 = new TestAssign<int>("Assign1")
            {
                ValueExpression = ((env) => (int)loop1Counter.Get(env) + 1),
                ToVariable = loop1Counter
            };

            TestAssign<int> assign2 = new TestAssign<int>("Assign2")
            {
                ValueExpression = ((env) => (int)loop2Counter.Get(env) + 1),
                ToVariable = loop2Counter
            };

            TestAssign<int> assign3 = new TestAssign<int>("Assign3")
            {
                ValueExpression = ((env) => (int)loop3Counter.Get(env) + 1),
                ToVariable = loop3Counter
            };

            TestAssign<int> assign4 = new TestAssign<int>("Assign4")
            {
                ValueExpression = ((env) => (int)loop4Counter.Get(env) + 1),
                ToVariable = loop4Counter
            };

            TestAssign<int> assign5 = new TestAssign<int>("Assign5")
            {
                ValueExpression = ((env) => (int)loop5Counter.Get(env) + 1),
                ToVariable = loop5Counter
            };

            List<HintTrueFalse> hintsList = new List<HintTrueFalse>();
            for (int i = 0; i < 5; i++)
            {
                hintsList.Add(HintTrueFalse.True);
            }
            hintsList.Add(HintTrueFalse.False);

            HintTrueFalse[] hints = hintsList.ToArray();
            TestBpmFlowConditional flowDecision1 = new TestBpmFlowConditional(hints)
            {
                ConditionExpression = (env => loop1Counter.Get(env) <= 5)
            };

            TestBpmFlowConditional flowDecision2 = new TestBpmFlowConditional(hints)
            {
                ConditionExpression = (env => loop2Counter.Get(env) <= 5)
            };

            TestBpmFlowConditional flowDecision3 = new TestBpmFlowConditional(hints)
            {
                ConditionExpression = (env => loop3Counter.Get(env) <= 5)
            };

            TestBpmFlowConditional flowDecision4 = new TestBpmFlowConditional(hints)
            {
                ConditionExpression = (env => loop4Counter.Get(env) <= 5)
            };

            TestBpmFlowConditional flowDecision5 = new TestBpmFlowConditional(hints)
            {
                ConditionExpression = (env => loop5Counter.Get(env) <= 5)
            };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart started"), assign1);

            flowchart.AddLink(assign1, assign2);
            flowchart.AddLink(assign2, assign3);
            flowchart.AddLink(assign3, assign4);
            flowchart.AddLink(assign4, assign5);

            flowchart.AddConditionalLink(assign5, flowDecision5, assign5, flowDecision4);

            flowchart.AddConditionalLink((TestActivity)null, flowDecision4, assign4, flowDecision3);

            flowchart.AddConditionalLink((TestActivity)null, flowDecision3, assign3, flowDecision2);

            flowchart.AddConditionalLink((TestActivity)null, flowDecision2, assign2, flowDecision1);

            flowchart.AddConditionalLink((TestActivity)null, flowDecision1, assign1, new TestWriteLine("End", "Flowchart ended"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Create loop using flow switch.
        /// </summary>        
        [Fact]
        public void FlowSwitchInLoopDifferentCaseEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
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

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(1, w1);
            cases.Add(2, w2);
            cases.Add(3, w3);

            List<int> hints = new List<int>();
            hints.Add(0);
            hints.Add(1);
            hints.Add(2);
            hints.Add(-1);

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), increment);

            flowchart.AddSwitchLink(increment, cases, hints, env => counter.Get(env), w4);
            flowchart.AddLink(w1, increment);
            flowchart.AddLink(w2, increment);
            flowchart.AddLink(w3, increment);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Create loop using flow switch.
        /// </summary>        
        [Fact]
        public void FlowSwitchInLoopSameCaseEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> switchVariable = VariableHelper.CreateInitialized<int>("switchVar", 0);
            Variable<int> ifVariable = VariableHelper.CreateInitialized<int>("ifVar", 0);

            flowchart.Variables.Add(switchVariable);
            flowchart.Variables.Add(ifVariable);

            TestIncrement incrementIfVariable = new TestIncrement("Inc", 1)
            {
                CounterVariable = ifVariable
            };

            TestIncrement incrementSwitchVariable = new TestIncrement("IncSwitch", 1)
            {
                CounterVariable = switchVariable
            };

            TestWriteLine writeBegin = new TestWriteLine("Loop", "Looping");

            List<HintTrueFalse> hintsList = new List<HintTrueFalse>();
            for (int i = 0; i < 5; i++)
            {
                hintsList.Add(HintTrueFalse.True);
            }
            hintsList.Add(HintTrueFalse.False);

            TestBpmFlowConditional conditional = new TestBpmFlowConditional(hintsList.ToArray())
            {
                ConditionExpression = env => ifVariable.Get(env) < 5
            };

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(0, writeBegin);

            List<int> hints = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                hints.Add(0);
            }
            hints.Add(-1);

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart started"), writeBegin);
            flowchart.AddConditionalLink(writeBegin, conditional, incrementIfVariable, incrementSwitchVariable);
            TestBpmSwitch<object> flowSwitch = flowchart.AddSwitchLink<object>(incrementIfVariable, cases, hints, env => switchVariable.Get(env), new TestWriteLine("Default", "Default")) as TestBpmSwitch<object>;
            flowchart.AddLink(incrementSwitchVariable, flowSwitch);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Model dowhile in flowchart
        /// Model do while in flowchart
        /// </summary>        
        [Fact]
        public void Flowchart_DoWhile()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>(0);
            counter.Name = "counter";
            flowchart.Variables.Add(counter);

            List<HintTrueFalse> hintsList = new List<HintTrueFalse>();
            for (int i = 0; i < 9; i++)
            {
                hintsList.Add(HintTrueFalse.True);
            }
            hintsList.Add(HintTrueFalse.False);

            TestBpmFlowConditional conditional = new TestBpmFlowConditional(hintsList.ToArray()) { ConditionExpression = env => counter.Get(env) < 10 };

            TestWriteLine start = new TestWriteLine("Start", "Flowchart Started");
            TestIncrement incrementByOne = new TestIncrement() { CounterVariable = counter, IncrementCount = 1 };

            flowchart.AddLink(start, incrementByOne);

            flowchart.AddConditionalLink(incrementByOne, conditional, incrementByOne, new TestWriteLine("Final", "End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute flow switch in loop and evaluate to default each time.
        /// </summary>        
        [Fact]
        public void FlowSwitchInLoopDefaultEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>(0);
            counter.Name = "counter";
            flowchart.Variables.Add(counter);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(10, new TestWriteLine("Ten", "Ten"));
            cases.Add(11, new TestWriteLine("Eleven", "Eleven"));
            cases.Add(12, new TestWriteLine("Twelve", "Twelve"));
            cases.Add(13, new TestWriteLine("Thirteen", "Thirteen"));

            List<int> hints = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                hints.Add(-1);
            }
            hints.Add(0);

            TestIncrement incByOne = new TestIncrement { IncrementCount = 1, CounterVariable = counter };

            TestBpmSwitch<object> flowSwitch = flowchart.AddSwitchLink<object>(new TestWriteLine("Start", "Flowchart started"), cases, hints, e => counter.Get(e), incByOne) as TestBpmSwitch<object>;

            flowchart.AddLink(incByOne, flowSwitch);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
