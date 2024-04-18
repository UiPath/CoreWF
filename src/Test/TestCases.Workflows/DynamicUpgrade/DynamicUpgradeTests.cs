using System;
using System.Activities;
using System.Activities.DynamicUpdate;
using System.Activities.Statements;
using System.IO;
using System.Threading;
using JsonFileInstanceStore;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using Xunit;

namespace TestCases.Workflows.DynamicUpgrade
{
    public class DynamicUpgradeTests
    {
        private readonly FileInstanceStore _store = new("dataFolder") { KeepInstanceDataAfterCompletion = true };
 
        [Fact]
        public void RunWithUpgrade()
        {
            var instanceId = RunInitialVersion();
            
            var workflow = GetWorkflow();

            DynamicUpdateServices.PrepareForUpdate(workflow);
            ModifyWorkflow(workflow);
            var map = DynamicUpdateServices.CreateUpdateMap(workflow);

            RunUpdatedWorkflow(instanceId, workflow,map);
        }
        private Guid RunInitialVersion()
        {
            var doneEvent = new AutoResetEvent(false);

            var sw = new StringWriter();
            var identity = new WorkflowIdentity() { Name = "mirciulica", Version = new Version(1, 0, 0, 0) };

            var workflow = GetWorkflow();
            var wfApp = new WorkflowApplication(workflow, identity)
            {
                InstanceStore = _store,
                PersistableIdle = e => PersistableIdleAction.Unload,
                Completed = e => doneEvent.Set(),
                Aborted = _ => doneEvent.Set(),
                Unloaded = _ => doneEvent.Set()
            };

            wfApp.Extensions.Add(sw);

            wfApp.Run();
            doneEvent.WaitOne();


            sw.ToString().ShouldContain("before");
            sw.ToString().ShouldNotContain("after");
            return wfApp.Id;
        }

        private void RunUpdatedWorkflow(Guid id, Activity workflow, DynamicUpdateMap map)
        {
            var doneEvent = new AutoResetEvent(false);
            var idleEvent = new AutoResetEvent(false);
            var identity = new WorkflowIdentity() { Name = "mirciulica", Version = new Version(2, 0, 0, 0) };
            var stringWriter = new StringWriter();

            var instance = WorkflowApplication.GetInstance(id, _store);

            var wfApp = new WorkflowApplication(workflow, identity)
            {
                InstanceStore = _store,
                PersistableIdle = e => PersistableIdleAction.Persist,
                Completed = e => doneEvent.Set(),
                Aborted = _ => doneEvent.Set(),
                Idle = _ => idleEvent.Set(),
            };
            wfApp.Extensions.Add(stringWriter);

            wfApp.Load(instance, map);
            wfApp.Run();

            while (WaitHandle.WaitAny([doneEvent, idleEvent]) != 0)
            {
                wfApp.ResumeBookmark("TheValue", 42);
            }

            stringWriter.ToString().ShouldNotContain("before");
            stringWriter.ToString().ShouldContain("after");
            stringWriter.ToString().ShouldContain("added activity 42");
        }

        [Fact]
        public void Test1()
        {
            RunInitialVersion();
        }

        private static void ModifyWorkflow(Activity workflow)
        {
            var sequence = workflow as Sequence;
            sequence!.Activities.Add(
                new WriteLine()
                {
                    Text = new VisualBasicValue<string>("\"added activity \" + result.ToString()")
                });
        }

        private Activity GetWorkflow()
        {
            return new Sequence()
            {
                Variables = { new Variable<int>("result") },
                Activities =
                {
                    new WriteLine()
                    {
                        Text = "before"
                    },
                     new TestWithBookmark(){Result = new VisualBasicReference<int>("result")},
                     new WriteLine()
                     {
                         Text = "after"
                     },

                }
            };
        }
    }

    public sealed class TestWithBookmark : NativeActivity<int>
    {

        protected override void Execute(NativeActivityContext context)
        {
            var bookMarkName = "TheValue";
            context.CreateBookmark(bookMarkName, new BookmarkCallback(OnReadComplete));
        }

        protected override bool CanInduceIdle => true;
        
        void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            this.Result.Set(context, Convert.ToInt32(state));
        }
    }
}