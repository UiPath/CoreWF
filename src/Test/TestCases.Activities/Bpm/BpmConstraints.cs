// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Statements;
using System.Collections.Generic;
using Act = System.Activities.Statements;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Xunit;
using System.Activities;

namespace TestCases.Activities.Bpm
{
    public class BpmConstraints : IDisposable
    {
        [Fact]
        public void MissingStartNode()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddLink(new TestWriteLine("Hello", "Hello"), new TestWriteLine("Hi", "Hi"));
            ((Act.BpmFlowchart)flowchart.ProductActivity).StartNode = null;

            List<string> errors = new List<string>();
            errors.Add(string.Format(ErrorStrings.FlowchartMissingStartNode, flowchart.DisplayName));

            Validate(flowchart, errors);
        }

        /// <summary>
        /// Verify StartNode need not be in Nodes collection (no constraint violation)
        /// </summary>        
        [Fact]
        public void StartNodeNotInNodesCollection()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            flowchart.AddLink(new TestWriteLine("Start", "Start"), new TestWriteLine("One", "One"));
            ((Act.BpmFlowchart)flowchart.ProductActivity).Nodes.RemoveAt(0);

            Validate(flowchart, null); //No constraint violation
        }

        /// <summary>
        /// A->B->C->D, StartNode = B, Nodes = {A, A, A}. Make all A, B & C invalid flownodes of different types. Verify that all the constraint violations are reported (unreachableNodes, incompleteNodes, invalid nodes A, B, C & D)
        /// </summary>        
        [Fact]
        public void ComplexScenario()
        {
            //TestFlowchart OM makes it difficult to construct such invalid flowchart, hence using product and wrapping it in TestFlowchart
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Act.BpmFlowchart prod = flowchart.ProductActivity as Act.BpmFlowchart;

            BpmStep A = new BpmStep();
            BpmSwitch<object> B = new BpmSwitch<object>();
            BpmStep C = new BpmStep() { Action = new WriteLine { Text = "Dummy" } };
            BpmDecision D = new BpmDecision();

            //A->B->C->D, StartNode = B, Nodes = {A, A, A}
            A.Next = B;
            B.Default = C;
            C.Next = D;

            prod.StartNode = B;

            prod.Nodes.Add(A);
            prod.Nodes.Add(A);
            prod.Nodes.Add(A);

            List<TestConstraintViolation> constraints = new()
            {
                new(string.Format(ErrorStrings.FlowSwitchRequiresExpression, prod.DisplayName), B),
                new(string.Format(ErrorStrings.FlowDecisionRequiresCondition, prod.DisplayName), D),
            };
            TestRuntime.ValidateConstraints(flowchart, constraints, null);
        }

        [Fact]
        public void FlowSwitchRequiresExpression()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Act.BpmFlowchart prod = flowchart.ProductActivity as Act.BpmFlowchart;

            BpmStep step1;
            BpmStep step2;
            BpmSwitch<object> flowSwitch = new BpmSwitch<object>
            {
                Cases = { { 2, step2 = new BpmStep { Action = new WriteLine { Text = "Dummy" } } } },
                Default = step1 = new BpmStep { Action = new WriteLine { Text = "Dummy" } }
            };

            prod.StartNode = flowSwitch;
            prod.Nodes.Add(step1);
            prod.Nodes.Add(step2);

            List<string> errors = new()
            {
                string.Format(ErrorStrings.FlowSwitchRequiresExpression, prod.DisplayName)
            };

            Validate(flowchart, errors, flowSwitch);
        }

        [Fact]
        public void FlowDecisionConditionMustBeSet()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Act.BpmFlowchart prod = flowchart.ProductActivity as Act.BpmFlowchart;

            BpmStep step;
            BpmDecision decision = new BpmDecision { True = step = new BpmStep { Action = new WriteLine { Text = "Dummy" } } };
            prod.StartNode = decision;
            prod.Nodes.Add(step);

            List<string> errors = new List<string>();
            errors.Add(string.Format(ErrorStrings.FlowDecisionRequiresCondition, prod.DisplayName));

            Validate(flowchart, errors, decision);
        }

        private void Validate(TestActivity activity, List<string> errors, Activity source = null)
        {
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();

            if (errors != null)
            {
                foreach (string error in errors)
                {
                    constraints.Add(new TestConstraintViolation(error, source ?? activity.ProductActivity));
                }
            }

            TestRuntime.ValidateConstraints(activity, constraints, null);
        }

        public void Dispose()
        {
        }
    }
}
