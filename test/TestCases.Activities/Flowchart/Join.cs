// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Xunit;

namespace TestCases.Activities.Flowchart
{
    public class Join : IDisposable
    {
        /// <summary>
        /// Three activities connected by AND join in flowchart.
        /// </summary>        
        [Fact(Skip = "To be compared with Desktop testcase")]
        public void ThreeActivitiesInAndJoin()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");
            TestWriteLine writeLine4 = new TestWriteLine("hello4", "Hello4");

            TestParallel parallel = new TestParallel()
            {
                Branches =
                {
                    writeLine1, writeLine2, writeLine3
                }
            };

            flowchart.AddLink(parallel, writeLine4);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Three activities connected by OR join in flowchart.
        /// </summary>        
        [Fact(Skip = "To be compared with Desktop testcase")]
        public void ThreeActivitiesInOrJoin()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>(0);
            counter.Name = "counter";
            flowchart.Variables.Add(counter);

            TestIncrement inc1 = new TestIncrement { CounterVariable = counter, IncrementCount = 1 };
            TestIncrement inc2 = new TestIncrement { CounterVariable = counter, IncrementCount = 1 };
            TestIncrement inc3 = new TestIncrement { CounterVariable = counter, IncrementCount = 1 };

            TestParallel parallel = new TestParallel { Branches = { inc1, inc2, inc3 }, CompletionConditionExpression = env => counter.Get(env) == 1, HintNumberOfBranchesExecution = 1 };

            flowchart.AddLink(parallel, new TestWriteLine("End", "The End"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
