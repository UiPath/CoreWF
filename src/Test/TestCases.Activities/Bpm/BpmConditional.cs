// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmConditional : IDisposable
    {
        /// <summary>
        /// True evaluation of a flowchart decision
        /// </summary>        
        [Fact]
        public void DecisionTrueEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);


            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                ConditionExpression = ((env) => (counter.Get(env) == 3))
            };

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// False evaluation of a flowchart decision
        /// </summary>        
        [Fact]
        public void DecisionFalseEvaluation()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                ConditionExpression = (context => counter.Get(context) > 3)
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Condition set to true of a flowchart decision
        /// </summary>        
        [Fact]
        public void ExpressionSetToTrue()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            Variable<bool> trueVar = new Variable<bool>("trueVar", true);

            flowchart.Variables.Add(trueVar);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                ConditionVariable = trueVar
            };

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Condition set to false of a flowchart decision
        /// </summary>        
        [Fact]
        public void ExpressionSetToFalse()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                Condition = false
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Only true link connected to target activity. Decision evaluation true
        /// </summary>        
        [Fact]
        public void DecisionTruePinConnectedConditionEvaluationTrue()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Only true link connected to target activity. Decision evaluation false
        /// </summary>        
        [Fact]
        public void DecisionTruePinConnectedConditionEvaluationFalse()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                Condition = false
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, (TestActivity)null, writeLine2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Condition expression on variable defined in flowchart.
        /// </summary>        
        [Fact]
        public void ConditionExpressionOnFlowchartVariable()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);

            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                ConditionExpression = (context => counter.Get(context) == 3)
            };

            flowchart.AddStartLink(writeLine1);
            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Only false link connected to target activity. Decision evaluation false
        /// </summary>        
        [Fact]
        public void DecisionFalsePinConnectedConditionEvaluationFalse()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.False)
            {
                Condition = false
            };

            flowchart.AddConditionalLink(writeLine1, flowDecision, (TestActivity)null, writeLine2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Only false link connected to target activity. Decision evaluation true
        /// </summary>        
        [Fact]
        public void DecisionFalsePinConnectedConditionEvaluationTrue()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// True and False pins of flow decision connected in OR Join.
        /// </summary>        
        [Fact]
        public void TrueFalsePinsInOrJoin()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional
            {
                Condition = true
            };

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Have divide by zero condition in condition expression.
        /// </summary>        
        [Fact]
        public void ConditionExpressionDivideByZero()
        {
            Variable<int> counter1 = VariableHelper.CreateInitialized<int>("counter1", 0);
            Variable<int> counter2 = VariableHelper.CreateInitialized<int>("counter2", 2);

            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flowchart")
            {
                Variables =
                {
                    counter1,
                    counter2
                },
                ExpectedOutcome = Outcome.UncaughtException(typeof(DivideByZeroException))
            };
            flowchart.ExpectedOutcome.IsOverrideable = true;

            TestAssign<int> assign = new TestAssign<int>("Assign")
            {
                ValueExpression = ((env) => (counter1.Get(env) + 1)),
                ToExpression = ((env) => counter2.Get(env))
            };

            TestBpmFlowConditional flowDecision = new TestBpmFlowConditional(HintTrueFalse.Exception)
            {
                ConditionExpression = ((env) => counter1.Get(env) / (counter2.Get(env) - 1) >= 0),
            };

            flowchart.AddLink(new TestWriteLine("Start", "Flowchart Started"), assign);
            flowchart.AddConditionalLink(assign, flowDecision, assign, (TestActivity)null);

            TestRuntime.RunAndValidateAbortedException(flowchart, typeof(DivideByZeroException), null);
        }

        /// <summary>
        /// Condtion evaluation on custom ValueExpression
        /// Set the condition to expression activity.
        /// </summary>        
        [Fact]
        public void DecisionWithConditionSetToExpressionActivity()
        {
            TestExpressionEvaluator<bool> myExpression = new TestExpressionEvaluator<bool>(true);

            TestBpmFlowConditional flowConditinoal = new TestBpmFlowConditional
            {
                ConditionValueExpression = (TestActivity)myExpression
            };
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            flowchart.AddConditionalLink(new TestWriteLine("Start", "FLowchart started"),
                                         flowConditinoal,
                                         new TestWriteLine("True Action", "True Action"),
                                         new TestWriteLine("False Action", "False Action"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Condtion expression set to null
        /// Set condition expression to null. Validation error expected.
        /// </summary>        
        [Fact]
        public void ConditionExpressionSetToNull()
        {
            TestBpmFlowConditional conditional = new TestBpmFlowConditional();
            conditional.ProductFlowConditional.Condition = null;

            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            flowchart.AddConditionalLink(null, conditional, new TestWriteLine("True", "True"), new TestWriteLine("False", "False"));

            TestRuntime.ValidateInstantiationException(flowchart, string.Format(ErrorStrings.FlowDecisionRequiresCondition, flowchart.DisplayName));
        }

        /// <summary>
        /// None of the link connected to any target activity. Validation error expected.
        /// </summary>        
        [Fact]
        public void DecisionWithNoPinConnected()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestBpmFlowConditional decision = new TestBpmFlowConditional
            {
                Condition = true
            };
            flowchart.AddConditionalLink(new TestWriteLine("Start", "Flowchart started"), decision);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Set condition expression that point to existing variable.
        /// </summary>        
        [Fact]
        public void ConditionExpressionOnExistingVariable()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<bool> flag = VariableHelper.CreateInitialized<bool>(true);
            flag.Name = "flag";
            flowchart.Variables.Add(flag);

            TestBpmFlowConditional decision = new TestBpmFlowConditional { ConditionExpression = e => flag.Get(e) };

            flowchart.AddConditionalLink(new TestWriteLine("Start", "Start"), decision, new TestWriteLine("True", "True"), new TestWriteLine("false", "false"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
