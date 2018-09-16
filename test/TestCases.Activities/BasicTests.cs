// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Xunit;

namespace TestCases.Activities
{
    public class BasicTests
    {
        /// <summary>
        /// Basic test case
        /// </summary>		
        [Fact]
        public void HelloWorld()
        {
            TestSequence sequence1 = new TestSequence()
            {
                DisplayName = "sequence1",
                Activities =
                {
                    new TestWriteLine("write hello", "Hello world!"),
                },
            };

            TestRuntime.RunAndValidateWorkflow(sequence1);
        }

        /// <summary>
        /// A simple composition of all procedural activities
        /// </summary>        
        [Fact]
        public void CompositeProcedurals()
        {
            Variable<bool> cond = new Variable<bool> { Default = true };
            Variable<string> value = new Variable<string> { Default = "Apple" };
            DelegateInArgument<string> arg = new DelegateInArgument<string> { Name = "Apple" };
            string[] values = { "a", "b" };

            TestSwitch<string> switchAct = new TestSwitch<string>
            {
                ExpressionVariable = value
            };
            switchAct.AddCase("Apple", new TestWriteLine("Apple", "this is an apple"));
            switchAct.AddCase("Orange", new TestWriteLine("Orange", "this is an orange"));
            switchAct.Hints.Add(0);

            TestIf ifAct = new TestIf(HintThenOrElse.Then)
            {
                ConditionVariable = cond,
                ThenActivity = new TestWriteLine("W", "Yes thats true"),
                ElseActivity = new TestWriteLine("W", "No thats not true")
            };

            TestForEach<string> forEachAct = new TestForEach<string>
            {
                Values = values,
                CurrentVariable = arg,
                HintIterationCount = 2,
                Body = new TestWriteLine { DisplayName = "w1", MessageExpression = context => arg.Get(context), HintMessageList = { "a", "b" } }
            };

            TestSequence seq = new TestSequence
            {
                Variables = { cond, value },
                Activities = { switchAct, ifAct, forEachAct }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }
    }
}
