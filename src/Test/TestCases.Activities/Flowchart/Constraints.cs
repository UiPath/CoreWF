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

namespace TestCases.Activities.Flowchart
{
    public class Constraints : IDisposable
    {
        [Fact]
        public void MissingStartNode()
        {
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddLink(new TestWriteLine("Hello", "Hello"), new TestWriteLine("Hi", "Hi"));
            ((Act.Flowchart)flowchart.ProductActivity).StartNode = null;

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
            TestFlowchart flowchart = new TestFlowchart();

            flowchart.AddLink(new TestWriteLine("Start", "Start"), new TestWriteLine("One", "One"));
            ((Act.Flowchart)flowchart.ProductActivity).Nodes.RemoveAt(0);

            Validate(flowchart, null); //No constraint violation
        }

        /// <summary>
        /// A->B->C->D, StartNode = B, Nodes = {A, A, A}. Make all A, B & C invalid flownodes of different types. Verify that all the constraint violations are reported (unreachableNodes, incompleteNodes, invalid nodes A, B, C & D)
        /// </summary>        
        [Fact]
        public void ComplexScenario()
        {
            //TestFlowchart OM makes it difficult to construct such invalid flowchart, hence using product and wrapping it in TestFlowchart
            TestFlowchart flowchart = new TestFlowchart();

            Act.Flowchart prod = flowchart.ProductActivity as Act.Flowchart;

            FlowStep A = new FlowStep();
            FlowSwitch<object> B = new FlowSwitch<object>();
            FlowStep C = new FlowStep() { Action = new WriteLine { Text = "Dummy" } };
            FlowDecision D = new FlowDecision();

            //A->B->C->D, StartNode = B, Nodes = {A, A, A}
            A.Next = B;
            B.Default = C;
            C.Next = D;

            prod.StartNode = B;

            prod.Nodes.Add(A);
            prod.Nodes.Add(A);
            prod.Nodes.Add(A);

            List<string> errors = new List<string>();
            errors.Add(string.Format(ErrorStrings.FlowSwitchRequiresExpression, prod.DisplayName));
            errors.Add(string.Format(ErrorStrings.FlowDecisionRequiresCondition, prod.DisplayName));

            Validate(flowchart, errors);
        }

        [Fact]
        public void FlowSwitchRequiresExpression()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Act.Flowchart prod = flowchart.ProductActivity as Act.Flowchart;

            FlowStep step1;
            FlowStep step2;
            FlowSwitch<object> flowSwitch = new FlowSwitch<object>
            {
                Cases = { { 2, step2 = new FlowStep { Action = new WriteLine { Text = "Dummy" } } } },
                Default = step1 = new FlowStep { Action = new WriteLine { Text = "Dummy" } }
            };

            prod.StartNode = flowSwitch;
            prod.Nodes.Add(step1);
            prod.Nodes.Add(step2);

            List<string> errors = new List<string>();
            errors.Add(string.Format(ErrorStrings.FlowSwitchRequiresExpression, prod.DisplayName));

            Validate(flowchart, errors);
        }

        [Fact]
        public void FlowDecisionConditionMustBeSet()
        {
            TestFlowchart flowchart = new TestFlowchart();

            Act.Flowchart prod = flowchart.ProductActivity as Act.Flowchart;

            FlowStep step;
            FlowDecision decision = new FlowDecision { True = step = new FlowStep { Action = new WriteLine { Text = "Dummy" } } };
            prod.StartNode = decision;
            prod.Nodes.Add(step);

            List<string> errors = new List<string>();
            errors.Add(string.Format(ErrorStrings.FlowDecisionRequiresCondition, prod.DisplayName));

            Validate(flowchart, errors);
        }

        private void Validate(TestActivity activity, List<string> errors)
        {
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();

            if (errors != null)
            {
                foreach (string error in errors)
                {
                    constraints.Add(new TestConstraintViolation(error, activity.ProductActivity));
                }
            }

            TestRuntime.ValidateConstraints(activity, constraints, null);
        }

        public void Dispose()
        {
        }
    }
}
