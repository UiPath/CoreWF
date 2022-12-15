// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities.Bpm
{
    public class BpmExecution
    {
        /// <summary>
        /// Parallel execution in flowchart test case
        /// </summary>        
        [Fact]
        public void Flowchart_Parallel()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");
            TestWriteLine writeLine4 = new TestWriteLine("hello4", "Hello4");
            TestWriteLine writeLine5 = new TestWriteLine("hello5", "Hello5");

            TestParallel parallel = new TestParallel
            {
                Branches = { writeLine2, writeLine3, writeLine4 }
            };


            flowchart.AddLink(writeLine1, parallel);
            flowchart.AddLink(parallel, writeLine5);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Sequence modelled in flowchart test case
        /// </summary>        
        [Fact]
        public void Flowchart_Sequence()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");
            TestWriteLine writeLine4 = new TestWriteLine("hello4", "Hello4");

            flowchart.AddLink(writeLine1, writeLine2);
            flowchart.AddLink(writeLine2, writeLine3);
            flowchart.AddLink(writeLine3, writeLine4);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute nested flowchart.
        /// </summary>        
        [Fact]
        public void ExecuteNestedFlowchart()
        {
            TestBpmFlowchart parentFlowchart = new TestBpmFlowchart("Parent");

            TestBpmFlowchart childFlowchart = new TestBpmFlowchart("Child");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            parentFlowchart.AddLink(writeLine1, childFlowchart);

            childFlowchart.AddStartLink(writeLine2);

            TestRuntime.RunAndValidateWorkflow(parentFlowchart);
        }

        /// <summary>
        /// Execute nested flowchart.
        /// </summary>        
        [Fact]
        public void ExecuteSingleActivity()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flowchart");

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");

            flowchart.AddStartLink(writeLine1);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Execute empty flowchart.
        /// </summary>        
        [Fact]
        public void ExecuteEmptyFlowchart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flowchart1");

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        [Fact]
        public void CancelExecutingChildActivities()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart("Parent");

            TestBlockingActivity blocking = new TestBlockingActivity("BlockingActivity", "B1");
            TestWriteLine writeLine1 = new TestWriteLine("w1", "w1");
            TestWriteLine writeLine2 = new TestWriteLine("w2", "w2");

            parent.AddLink(writeLine1, blocking);
            TestBpmFlowElement element = parent.AddLink(blocking, writeLine2);
            element.IsCancelling = true;

            blocking.ExpectedOutcome = Outcome.Canceled;
            parent.ExpectedOutcome = Outcome.Canceled;

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parent))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                System.Threading.Thread.Sleep(2000);
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Persist flowchart.
        /// </summary>        
        [Fact]
        public void PersistFlowchart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");

            TestBlockingActivity blocking = new TestBlockingActivity("BlockingActivity");

            flowchart.AddStartLink(writeLine1);
            flowchart.AddLink(writeLine1, blocking);
            flowchart.AddLink(blocking, writeLine2);

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart, null, jsonStore, PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.PersistWorkflow();
                System.Threading.Thread.Sleep(2000);
                testWorkflowRuntime.ResumeBookMark("BlockingActivity", null);
                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");
            TestWriteLine writeLine1 = new TestWriteLine("hello1", "Hello1");
            TestWriteLine writeLine2 = new TestWriteLine("hello2", "Hello2");
            TestWriteLine writeLine3 = new TestWriteLine("hello3", "Hello3");
            TestWriteLine writeLine4 = new TestWriteLine("hello4", "Hello4");
            flowchart.AddLink(writeLine1, writeLine2);
            flowchart.AddLink(writeLine2, writeLine3);

            WorkflowInspectionServices.GetActivities(flowchart.ProductActivity);

            flowchart.AddLink(writeLine3, writeLine4);

            // Now that we've change the tree we need to explicitly recache
            WorkflowInspectionServices.CacheMetadata(flowchart.ProductActivity);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Cancel flowchart with multiple flows executing.
        /// </summary>        
        [Fact]
        public void CancelFlowchart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestBlockingActivity blocking1 = new TestBlockingActivity("B1", "B1")
            {
                ExpectedOutcome = Outcome.Canceled
            };

            flowchart.AddStartLink(blocking1);
            flowchart.ExpectedOutcome = Outcome.Canceled;

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange(blocking1.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.CancelWorkflow();

                testWorkflowRuntime.WaitForCanceled(true);
            }
        }

        /// <summary>
        /// Try linking an activity in a nested flowchart to an activity in the parent
        /// </summary>        
        [Fact]
        public void LinkFromNestedFlowchartToParent()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart("Parent");
            TestBpmFlowchart child = new TestBpmFlowchart("Child");

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");
            TestWriteLine w3 = new TestWriteLine("w3", "w3");

            parent.AddLink(w1, w2);
            parent.AddLink(w2, child);
            child.AddLink(w3, w2);

            TestRuntime.ValidateInstantiationException(parent, string.Format(ErrorStrings.ActivityCannotBeReferencedWithoutTarget, w2.DisplayName, child.DisplayName, parent.DisplayName));
        }

        /// <summary>
        /// Cancel flowchart while executing expression of flow switch.
        /// </summary>        
        [Fact]
        public void CancelFlowchartWhileEvaluatingFlowSwitchExpression()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine writeHello = new TestWriteLine("Hello", "Hello");

            TestWriteLine writeStart = new TestWriteLine("Start", "Start");

            TestExpressionEvaluatorWithBody<object> expressionActivity = new TestExpressionEvaluatorWithBody<object>
            {
                ExpressionResultExpression = context => "One",
                Body = new TestBlockingActivity("B1", "Blocking") { ExpectedOutcome = Outcome.Canceled },
                WillBodyExecute = true
            };

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", writeHello);

            List<int> hints = new List<int>() { -1 };

            flowchart.AddStartLink(writeStart);

            flowchart.AddSwitchLink(writeStart, cases, hints, expressionActivity);

            flowchart.ExpectedOutcome = Outcome.Canceled;
            expressionActivity.ExpectedOutcome = Outcome.Canceled;

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange("B1", TestActivityInstanceState.Executing);

                testWorkflowRuntime.CancelWorkflow();

                testWorkflowRuntime.WaitForCanceled(true);
            }
        }

        /// <summary>
        /// Cancel flowchart while executing condition of flow conditional
        /// </summary>        
        [Fact]
        public void CancelFlowchartWhileEvaluatingFlowConditionalCondition()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestBlockingActivity blocking = new TestBlockingActivity("Block", "Blocked") { ExpectedOutcome = Outcome.Canceled };

            TestExpressionEvaluatorWithBody<bool> expression = new TestExpressionEvaluatorWithBody<bool>(true)
            {
                Body = blocking,
                WillBodyExecute = true
            };

            TestBpmFlowConditional conditional = new TestBpmFlowConditional
            {
                ConditionValueExpression = expression
            };

            flowchart.AddConditionalLink(new TestWriteLine("Start", "Flowchart started"),
                                         conditional,
                                         new TestWriteLine("True", "True Action"),
                                         new TestWriteLine("False", "False Action"));

            expression.ExpectedOutcome = Outcome.Canceled;
            flowchart.ExpectedOutcome = Outcome.Canceled;

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.CancelWorkflow();

                testWorkflowRuntime.WaitForCanceled(true);
            }
        }

        /// <summary>
        /// Try adding flowstep with action activity pointing to nested activity in a sequence.
        /// </summary>        
        [Fact]
        public void FlowStepActionToNestedActivityInSequence()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine w1 = new TestWriteLine("w1", "w1");
            TestWriteLine w2 = new TestWriteLine("w2", "w2");
            TestWriteLine w3 = new TestWriteLine("w3", "w3");

            TestSequence seq = new TestSequence();
            seq.Activities.Add(w2);

            flowchart.AddLink(w1, seq);
            flowchart.AddLink(seq, w3);
            flowchart.AddLink(w3, w2);

            TestRuntime.ValidateInstantiationException(flowchart, string.Format(ErrorStrings.ActivityCannotBeReferencedWithoutTarget, w2.DisplayName, seq.DisplayName, flowchart.DisplayName));
        }

        /// <summary>
        /// Execute same activity in different flowsteps
        /// </summary>        
        [Fact]
        public void ExecuteSameActivityMultipleTimesInDifferentFlowSteps()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();
            TestWriteLine writeLine = new TestWriteLine("Hello", "Hello");

            flowchart.AddLink(writeLine, writeLine);

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Access variable defined on parent flowchart from within nested flowchart.
        /// </summary>        
        [Fact]
        public void AccessVariableOnParentFromNestedFlowchart()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>(2);
            counter.Name = "counter";
            parent.Variables.Add(counter);

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add(1, new TestWriteLine("One", "One"));
            cases.Add(2, new TestWriteLine("Two", "Two"));
            cases.Add(3, new TestWriteLine("Three", "Three"));

            List<int> hints = new List<int> { 1 };

            child.AddSwitchLink(new TestWriteLine("Child Started", "Child Started"), cases, hints, e => counter.Get(e), new TestWriteLine("Default", "Default"));

            parent.AddLink(new TestWriteLine("Start", "Parent started"), child);

            TestRuntime.RunAndValidateWorkflow(parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// Try to access variable defined in nested flowchart from the parent flowchart.
        /// </summary>        
        [Fact]
        public void TryAccessVariableInNestedFlowchartFromParent()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child = new TestBpmFlowchart();

            Variable<int> counter = VariableHelper.CreateInitialized<int>(0);
            child.Variables.Add(counter);

            parent.AddLink(new TestIncrement { IncrementCount = 1, CounterVariable = counter, ExpectedOutcome = Outcome.UncaughtException() }, child);

            child.AddStartLink(new TestWriteLine("Wont execute", "Will not execute"));

            TestRuntime.ValidateInstantiationException(parent, string.Format(ErrorStrings.VariableNotVisible, counter.Name));
        }

        /// <summary>
        /// Execute multiple (5) levels nested flowchart
        /// </summary>        
        [Fact]
        public void ExecuteFiveLevelDeepNestedFlowchart()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child1 = new TestBpmFlowchart();
            TestBpmFlowchart child2 = new TestBpmFlowchart();
            TestBpmFlowchart child3 = new TestBpmFlowchart();
            TestBpmFlowchart child4 = new TestBpmFlowchart();

            parent.AddLink(new TestWriteLine("Parent Start", "Parent started"), child1);
            child1.AddLink(new TestWriteLine("Child1 Start", "Child1 started"), child2);
            child2.AddLink(new TestWriteLine("Child2 Start", "Child2 started"), child3);
            child3.AddLink(new TestWriteLine("Child3 Start", "Child3 started"), child4);
            child4.AddLink(new TestWriteLine("Child4 Start", "Child4 started"), new TestWriteLine("Flowchart end", "The End"));

            TestRuntime.RunAndValidateWorkflow(parent);
        }

        /// <summary>
        /// Have blocking activity (Receive) in flowchart and raise the event unblocking it.
        /// </summary> 
        /// /// Disabled and failed in desktop         
        //[Fact]
        private void BlockingActivityInFlowchart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestBlockingActivity blocking = new TestBlockingActivity("Block");

            flowchart.AddLink(new TestWriteLine("Start", "Start"), blocking);

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.ResumeBookMark("Block", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Execute five level deep empty nested flowchart.
        /// </summary>        
        [Fact]
        public void FiveLevelDeepEmptyNestedFlowchart()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child1 = new TestBpmFlowchart();
            TestBpmFlowchart child2 = new TestBpmFlowchart();
            TestBpmFlowchart child3 = new TestBpmFlowchart();
            TestBpmFlowchart child4 = new TestBpmFlowchart();

            parent.AddStartLink(child1);
            child1.AddStartLink(child2);
            child2.AddStartLink(child3);
            child3.AddStartLink(child4);

            TestRuntime.RunAndValidateWorkflow(parent);
        }

        /// <summary>
        /// Five level deep nested flowchart with blocking activity
        /// </summary>   
        /// Disabled and failed in desktop     
        //[Fact]
        private void FiveLevelDeepNestedFlowchartWithBlockingActivity()
        {
            TestBpmFlowchart parent = new TestBpmFlowchart();
            TestBpmFlowchart child1 = new TestBpmFlowchart();
            TestBpmFlowchart child2 = new TestBpmFlowchart();
            TestBpmFlowchart child3 = new TestBpmFlowchart();
            TestBpmFlowchart child4 = new TestBpmFlowchart();
            TestBlockingActivity blocking = new TestBlockingActivity("Blocked");

            parent.AddStartLink(child1);
            child1.AddStartLink(child2);
            child2.AddStartLink(child3);
            child3.AddStartLink(child4);
            child4.AddStartLink(blocking);

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parent))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.ResumeBookMark("Blocked", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Execute flowchart with single activity without marking it as Start activity.
        /// </summary>        
        [Fact]
        public void ExecuteFlowchartWithSingleActivityNotMarkedStart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart { Elements = { new TestWriteLine("Only One", "OnlyOne") } };

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Define and run flowchart with only a flowconditional without associating start event to it.
        /// </summary>        
        [Fact]
        public void FlowchartWithOnlyFlowConditionalWithoutStartEvent()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<bool> flag = VariableHelper.CreateInitialized<bool>(true);
            flag.Name = "flag";
            flowchart.Variables.Add(flag);

            TestBpmFlowConditional decision = new TestBpmFlowConditional { ConditionExpression = env => flag.Get(env) };

            flowchart.AddConditionalLink(null, decision, new TestWriteLine("True", "True"), new TestWriteLine("False", "False"));

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Unload and load flowchart while executing condition of flow conditional.
        /// </summary>        
        [Fact()]
        public void UnloadFlowchartWhileExecutingFlowConditionalCondition()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestExpressionEvaluatorWithBody<bool> expression = new TestExpressionEvaluatorWithBody<bool>(true)
            {
                Body = new TestBlockingActivity("Block"),
                WillBodyExecute = true
            };

            TestBpmFlowConditional conditional = new TestBpmFlowConditional(HintTrueFalse.True)
            {
                ConditionValueExpression = expression
            };

            flowchart.AddConditionalLink(new TestWriteLine("Start", "Flowchart started"),
                                         conditional,
                                         new TestWriteLine("True", "True Action"),
                                         new TestWriteLine("False", "False Action"));


            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(System.Environment.CurrentDirectory);

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart, null, jsonStore, PersistableIdleAction.Unload))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(expression.DisplayName, TestActivityInstanceState.Executing);

                //testWorkflowRuntime.PersistWorkflow();
                testWorkflowRuntime.UnloadWorkflow();

                testWorkflowRuntime.LoadWorkflow();

                testWorkflowRuntime.ResumeBookMark("Block", null);

                testWorkflowRuntime.WaitForCompletion(true);
            }
        }

        /// <summary>
        /// Unload and load flowchart while executing flow switch's expression.
        /// </summary>        
        [Fact()]
        public void UnloadFlowchartWhileExecutingFlowSwitchExpression()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestWriteLine writeHello = new TestWriteLine("Hello", "Hello");

            TestWriteLine writeStart = new TestWriteLine("Start", "Start");

            TestExpressionEvaluatorWithBody<object> expressionActivity = new TestExpressionEvaluatorWithBody<object>
            {
                ExpressionResultExpression = context => "One",
                Body = new TestBlockingActivity("Block"),
                WillBodyExecute = true
            };

            Dictionary<object, TestActivity> cases = new Dictionary<object, TestActivity>();
            cases.Add("One", writeHello);
            cases.Add("Two", new TestWriteLine("Two", "Two will not execute"));

            List<int> hints = new List<int>() { 0 };

            flowchart.AddStartLink(writeStart);

            flowchart.AddSwitchLink(writeStart, cases, hints, expressionActivity, new TestWriteLine("Default", "Will not execute"));

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart, null, jsonStore, PersistableIdleAction.Unload))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(expressionActivity.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.UnloadWorkflow();

                testWorkflowRuntime.LoadWorkflow();

                testWorkflowRuntime.ResumeBookMark("Block", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Model listen in flowchart.
        /// </summary>        
        [Fact]
        public void Flowchart_Listen()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart("Flow1");

            Variable<int> counter = VariableHelper.CreateInitialized<int>(0);
            counter.Name = "counter";
            flowchart.Variables.Add(counter);

            TestBlockingActivity blocking1 = new TestBlockingActivity("Block1");
            TestBlockingActivity blocking2 = new TestBlockingActivity("Block2");
            TestBlockingActivity blocking3 = new TestBlockingActivity("Block3");
            TestBlockingActivity blocking4 = new TestBlockingActivity("Block4");

            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    blocking1,
                    new TestIncrement { CounterVariable = counter, IncrementCount = 1 }
                }
            };

            TestParallel parallel = new TestParallel { Branches = { seq, blocking2, blocking3, blocking4 }, CompletionConditionExpression = env => counter.Get(env) == 1, HintNumberOfBranchesExecution = 1 };

            flowchart.AddLink(parallel, new TestWriteLine("End", "The End"));

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking1.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.ResumeBookMark("Block1", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Use flowchart variable in flowcondition.
        /// </summary>        
        [Fact]
        public void UseVariableInFlowchart()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            Variable<int> flag = VariableHelper.CreateInitialized<int>(23);
            flag.Name = "flag";
            flowchart.Variables.Add(flag);

            flowchart.AddLink(new TestIncrement { CounterVariable = flag, IncrementCount = 13 }, new TestWriteLine { MessageExpression = e => flag.Get(e).ToString(), HintMessage = "36" });

            TestRuntime.RunAndValidateWorkflow(flowchart);
        }

        /// <summary>
        /// Unload and load flowchart while executing flow step
        /// </summary>        
        [Fact()]
        public void UnloadFlowchartWhileExecutingFlowStep()
        {
            TestBpmFlowchart flowchart = new TestBpmFlowchart();

            TestBlockingActivity blocking = new TestBlockingActivity("Block");

            flowchart.AddStartLink(blocking);

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(flowchart, null, jsonStore, PersistableIdleAction.Unload))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.UnloadWorkflow();

                testWorkflowRuntime.LoadWorkflow();


                testWorkflowRuntime.ResumeBookMark("Block", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }
    }
}

