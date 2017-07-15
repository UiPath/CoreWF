// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Flowchart
{
    public class Conditional : IDisposable
    {
        /// <summary>
        /// True evaluation of a flowchart decision
        /// </summary>        
        [Fact]
        public void DecisionTrueEvaluation()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);


            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.ConditionExpression = ((env) => (counter.Get(env) == 3));

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// False evaluation of a flowchart decision
        /// </summary>        
        [Fact]
        public void DecisionFalseEvaluation()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);
            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.False);
            flowDecision.ConditionExpression = (context => counter.Get(context) > 3);

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            Variable<bool> trueVar = new Variable<bool>("trueVar", true);

            flowchart.Variables.Add(trueVar);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.ConditionVariable = trueVar;

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, writeLine3);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Condition set to false of a flowchart decision
        /// </summary>        
        [Fact]
        public void ExpressionSetToFalse()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.False);
            flowDecision.Condition = false;

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.Condition = true;

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.False);
            flowDecision.Condition = false;

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 3);

            flowchart.Variables.Add(counter);

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.ConditionExpression = (context => counter.Get(context) == 3);

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
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.False);
            flowDecision.Condition = false;

            flowchart.AddConditionalLink(writeLine1, flowDecision, (TestActivity)null, writeLine2);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Only false link connected to target activity. Decision evaluation true
        /// </summary>        
        [Fact]
        public void DecisionFalsePinConnectedConditionEvaluationTrue()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.Condition = true;

            flowchart.AddConditionalLink(writeLine1, flowDecision, writeLine2, (TestActivity)null);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// True and False pins of flow decision connected in OR Join.
        /// </summary>        
        [Fact]
        public void TrueFalsePinsInOrJoin()
        {
            TestFlowchart flowchart = new TestFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestFlowConditional flowDecision = new TestFlowConditional();
            flowDecision.Condition = true;

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

            TestFlowchart flowchart = new TestFlowchart("Flowchart")
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

            TestFlowConditional flowDecision = new TestFlowConditional(HintTrueFalse.Exception)
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

            TestFlowConditional flowConditinoal = new TestFlowConditional
            {
                ConditionValueExpression = (TestActivity)myExpression
            };
            TestFlowchart flowchart = new TestFlowchart();
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
            TestFlowConditional conditional = new TestFlowConditional();
            conditional.ProductFlowConditional.Condition = null;

            TestFlowchart flowchart = new TestFlowchart();
            flowchart.AddConditionalLink(null, conditional, new TestWriteLine("True", "True"), new TestWriteLine("False", "False"));

            TestRuntime.ValidateInstantiationException(flowchart, string.Format(ErrorStrings.FlowDecisionRequiresCondition, flowchart.DisplayName));
        }

        /// <summary>
        /// None of the link connected to any target activity. Validation error expected.
        /// </summary>        
        [Fact]
        public void DecisionWithNoPinConnected()
        {
            TestFlowchart flowchart = new TestFlowchart();

            TestFlowConditional decision = new TestFlowConditional();
            decision.Condition = true;
            flowchart.AddConditionalLink(new TestWriteLine("Start", "Flowchart started"), decision);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Set condition expression that point to existing variable.
        /// </summary>        
        [Fact]
        public void ConditionExpressionOnExistingVariable()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Variable<bool> flag = VariableHelper.CreateInitialized<bool>(true);
            flag.Name = "flag";
            flowchart.Variables.Add(flag);

            TestFlowConditional decision = new TestFlowConditional { ConditionExpression = e => flag.Get(e) };

            flowchart.AddConditionalLink(new TestWriteLine("Start", "Start"), decision, new TestWriteLine("True", "True"), new TestWriteLine("false", "false"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        public void Dispose()
        {
        }
    }
}
