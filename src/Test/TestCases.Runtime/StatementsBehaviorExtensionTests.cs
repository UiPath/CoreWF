using System;
using System.Activities;
using System.Activities.Statements;
using System.Threading;
using Shouldly;
using Test.Common.TestObjects.CustomActivities;
using WorkflowApplicationTestExtensions.Persistence;
using Xunit;

namespace TestCases.Runtime
{
    public class StatementsBehaviorExtensionTests
    {
        /// <summary>
        /// Tests that using StatementsBehaviorExtension extension with BlockingDelay = true, the Delay activity doesn't trigger PersistableIdle
        /// </summary>
        [Fact]
        public static void TestBlockingDelayShouldNotPersist()
        {
            var testSequence = new Sequence()
            {
                Activities =
                {
                    new Delay()
                    {
                        Duration = TimeSpan.FromMilliseconds(100)
                    },
                }
            };
            bool workflowIdleAndPersistable = false;
            var completed = new ManualResetEvent(false);
            WorkflowApplication workflowApplication = new WorkflowApplication(testSequence);
            workflowApplication.InstanceStore = new MemoryInstanceStore();
            workflowApplication.Extensions.Add(new StatementsBehaviorExtension { BlockingDelay = true });
            workflowApplication.PersistableIdle = (_) => { workflowIdleAndPersistable = true; return PersistableIdleAction.None; };
            workflowApplication.Completed = (_) => completed.Set();
            workflowApplication.Run();
            completed.WaitOne();
            workflowIdleAndPersistable.ShouldBeFalse(); //there is no persistable idle
        }

        /// <summary>
        /// Tests that without using StatementsBehaviorExtension extension, the Delay activity triggers PersistableIdle
        /// </summary>
        [Fact]
        public static void TestNonBlockingDelayShouldPersist()
        {
            var testSequence = new Sequence()
            {
                Activities =
                {
                    new Delay()
                    {
                        Duration = TimeSpan.FromMilliseconds(100)
                    },
                }
            };
            bool workflowIdleAndPersistable = false;
            var completed = new ManualResetEvent(false);
            WorkflowApplication workflowApplication = new WorkflowApplication(testSequence);
            workflowApplication.InstanceStore = new MemoryInstanceStore();
            workflowApplication.PersistableIdle = (_) => { workflowIdleAndPersistable = true; return PersistableIdleAction.None; };
            workflowApplication.Completed = (_) => completed.Set();
            workflowApplication.Run();
            completed.WaitOne();
            workflowIdleAndPersistable.ShouldBeTrue();
        }

        /// <summary>
        /// Tests that having StatementsBehaviorExtension extension with BlockingDelay=true, the Delay activity blocks the 
        /// PersistableIdle event from any other activity in the workflow, including parallel activities. 
        /// </summary>
        [Fact]
        public static void TestParallelBlockingActivityShouldTriggerPersistAfterDelayFinishes()
        {
            var delayDuration = TimeSpan.FromMilliseconds(100);

            var testSequence = new Sequence()
            {
                Activities =
                {
                    new Parallel()
                    {
                        Branches =
                        {
                            new Delay() { Duration = delayDuration  },
                            new BlockingActivity("B")
                        }
                    },
                }
            };
            bool workflowIdleAndPersistable = false;
            var completed = new ManualResetEvent(false);
            var persistableIdleTriggered = new ManualResetEvent(false);

            var sw = new System.Diagnostics.Stopwatch();

            WorkflowApplication workflowApplication = new WorkflowApplication(testSequence);
            workflowApplication.InstanceStore = new MemoryInstanceStore();
            workflowApplication.Extensions.Add(new StatementsBehaviorExtension { BlockingDelay = true });


            workflowApplication.PersistableIdle = (_) => {
                //check if more than 100ms passed
                sw.ElapsedMilliseconds.ShouldBeGreaterThan(delayDuration.Milliseconds);
                workflowIdleAndPersistable = true;
                persistableIdleTriggered.Set();
                return PersistableIdleAction.None;
            };
            workflowApplication.Completed = (_) => completed.Set();
            sw.Start();
            workflowApplication.Run();
            persistableIdleTriggered.WaitOne(); // Wait for PersistableIdle to trigger
            workflowApplication.ResumeBookmark("B", null); //Resume the bookmark from the blocking activity
            completed.WaitOne();
            workflowIdleAndPersistable.ShouldBeTrue();
        }
    }
}
