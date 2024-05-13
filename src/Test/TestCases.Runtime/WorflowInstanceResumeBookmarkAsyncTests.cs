using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Runtime.Common.Activities;
using WorkflowApplicationTestExtensions.Persistence;
using Xunit;
namespace TestCases.Runtime.WorkflowInstanceTest;
public class WorflowInstanceResumeBookmarkAsyncTests
{
    [Theory]
    // Test resume bookmark with callback
    [InlineData(2)]
    public static void TestOperationsResumeBookmarkCallback(int operationId)
    {
        Variable<int> value = VariableHelper.Create<int>("value");
        const string WaitMessage = "WaitActivity will wait for this trace";
        TestSequence testSequence = new TestSequence()
        {
            Variables = { value },
            Activities =
            {
                new TestWriteLine()
                {
                    Message = "Workflow Started"
                },
                new TestWaitForTrace()
                {
                    DisplayName = "WaitActivity",
                    TraceToWait = WaitMessage
                },
                new TestWaitReadLine<int>("Read", "Read")
                {
                    BookmarkValue = value
                },
                new TestReadLine<int>("Read1", "Read1")
                {
                    BookmarkValue = value
                },
                new TestWriteLine()
                {
                    Message = "Workflow Completed"
                }
            }
        };

        WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");
        TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testSequence, null, jsonStore, PersistableIdleAction.Unload);
        workflowRuntime.ExecuteWorkflow();
        workflowRuntime.WaitForActivityStatusChange("WaitActivity", TestActivityInstanceState.Executing);

        TestWorkflowRuntimeAsyncResult asyncResult = workflowRuntime.BeginResumeBookMark("Read", 10, new AsyncCallback(ResumeBookmarkCallback), operationId);
        //Continue the WaitActivity
        TestTraceManager.Instance.AddTrace(workflowRuntime.CurrentWorkflowInstanceId, new SynchronizeTrace(WaitMessage));
        SynchronizeTrace.Trace(workflowRuntime.CurrentWorkflowInstanceId, WaitMessage);
        workflowRuntime.EndResumeBookMark(asyncResult);
        workflowRuntime.WaitForTrace(new UserTrace("After ResumeBookmarkCallback"));

        if (operationId == 2)
        {
            //Do nothing
        }
        else if (operationId == 3)
        {
            workflowRuntime.UnloadWorkflow();
            workflowRuntime.LoadWorkflow();
            workflowRuntime.ExecuteWorkflow();
            workflowRuntime.ResumeBookMark("Read1", 99);
        }
        else if (operationId == 4)
        {
            workflowRuntime.LoadWorkflow();
            workflowRuntime.ExecuteWorkflow();
            workflowRuntime.ResumeBookMark("Read1", 99);
        }

        ExpectedTrace expectedTrace = testSequence.GetExpectedTrace();
        expectedTrace.AddIgnoreTypes(typeof(UserTrace));
        workflowRuntime.WaitForCompletion(expectedTrace);
    }

    private static void ResumeBookmarkCallback(IAsyncResult result)
    {
        TestWorkflowRuntimeAsyncState asyncState = (TestWorkflowRuntimeAsyncState)result.AsyncState;
        int operationId = (int)asyncState.State;
        if (operationId == 2)
        {
            asyncState.Instance.ResumeBookmark("Read1", 99);
        }
        else if (operationId == 3)
        {
            asyncState.Instance.Persist();
        }
        else if (operationId == 4)
        {
            asyncState.Instance.Persist();
            asyncState.Instance.Unload();
        }

        TestTraceManager.Instance.AddTrace(asyncState.Instance.Id, new UserTrace("After ResumeBookmarkCallback"));
        //UserTrace.Trace(asyncState.Instance.Id, "After ResumeBookmarkCallback");
    }

    [Theory]
    // Cancel during resume bookmark
    [InlineData(2, true)]
    // Cancel during resume bookmark async
    [InlineData(2, false)]
    // Terminate during resume bookmark
    [InlineData(3, true)]
    // Terminate during resume bookmark async
    [InlineData(3, false)]
    // Unload during resume bookmark
    [InlineData(4, true)]
    // Unload during resume bookmark async
    [InlineData(4, false)]
    private static void TestInstanceOperationFromResumeBookmarkCallback(int operationsId, bool isSync)
    {
        string shouldNotExecuteMsg = "Should not see this message";
        Variable<int> value = VariableHelper.Create<int>("value");
        TestWriteLine writeLineNotRun = new TestWriteLine("NotExecuted", shouldNotExecuteMsg)
        {
        };
        TestSequence testSequence = new TestSequence()
        {
            Variables = { value },
            Activities =
            {
                new TestWriteLine()
                {
                    Message = "Workflow Started"
                },
                new TestWaitReadLine<int>("Read", "Read")
                {
                    BookmarkValue = value
                },
            }
        };

        //Get Expected Trace without TestWriteLine()
        if (operationsId == 2)
        {
            testSequence.ExpectedOutcome = Outcome.Canceled;
        }

        ExpectedTrace expectedTrace = testSequence.GetExpectedTrace();

        if (operationsId == 4)
        {
            expectedTrace.Trace.Steps.RemoveAt(expectedTrace.Trace.Steps.Count - 1);
        }
        else if (operationsId == 3)
        {
            expectedTrace.Trace.Steps.RemoveAt(expectedTrace.Trace.Steps.Count - 1);
            expectedTrace.Trace.Steps.Add(new ActivityTrace(testSequence.DisplayName, ActivityInstanceState.Faulted));
        }


        //Now Add TestWriteLine to workflow
        testSequence.Activities.Add(writeLineNotRun);

        TestWorkflowRuntimeAsyncResult asyncResultResume = null;
        TestWorkflowRuntimeAsyncResult asyncResultOperation = null;
        string message = "";

        //Execute Workflow
        WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");
        // using PersistableIdleAction.None here because the idle unload was racing with the resume bookmark after the wait for the BeforeWait trace.
        TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testSequence, null, jsonStore, PersistableIdleAction.None);
        workflowRuntime.ExecuteWorkflow();
        workflowRuntime.WaitForActivityStatusChange("Read", TestActivityInstanceState.Executing);
        if (isSync)
        {
            //Log.Info("Resuming Bookmark");
            if (isSync)
            {
                workflowRuntime.ResumeBookMark("Read", 9999);
            }
            else
            {
                asyncResultResume = workflowRuntime.BeginResumeBookMark("Read", 9999, null, null);
            }

            workflowRuntime.WaitForTrace(new UserTrace(WaitReadLine<int>.BeforeWait));
            switch (operationsId)
            {
                case 2:
                    //Cancel Workflow during OnResumeBookmark is executing
                    //Log.Info("CancelWorkflow during OnResumeBookmark executing");
                    if (isSync)
                    {
                        workflowRuntime.CancelWorkflow();
                    }
                    else
                    {
                        asyncResultOperation = workflowRuntime.BeginCancelWorkflow(null, null);

                        workflowRuntime.EndResumeBookMark(asyncResultResume);
                        workflowRuntime.EndCancelWorkflow(asyncResultOperation);
                    }
                    //Trace.WriteLine should not execute
                    break;
                case 3:
                    //Terminate Workflow during OnResumeBookmark is executing
                    //Log.Info("TerminateWorkflow during OnResumeBookmark executing");
                    if (isSync)
                    {
                        workflowRuntime.TerminateWorkflow("Terminate Exception");
                    }
                    else
                    {
                        asyncResultOperation = workflowRuntime.BeginTerminateWorkflow("Terminate Exception", null, null);

                        workflowRuntime.EndResumeBookMark(asyncResultResume);
                        workflowRuntime.EndTerminateWorkflow(asyncResultOperation);
                    }
                    //Trace.WriteLine should not execute.
                    break;
                case 4:
                    //Unload Workflow during OnResumeBookmark is executing
                    //This should wait till ResumeMark finishes the work
                    //Log.Info("UnloadWorkflow during OnResumeBookmark executing");
                    if (isSync)
                    {
                        workflowRuntime.UnloadWorkflow();
                    }
                    else
                    {
                        asyncResultOperation = workflowRuntime.BeginUnloadWorkflow(null, null);

                        workflowRuntime.EndResumeBookMark(asyncResultResume);
                        workflowRuntime.EndUnloadWorkflow(asyncResultOperation);
                    }

                    //message = String.Format(ExceptionStrings.WorkflowInstanceUnloaded, workflowRuntime.CurrentWorkflowInstanceId);
                    ExceptionHelpers.CheckForException(typeof(WorkflowApplicationUnloadedException), message, new ExceptionHelpers.MethodDelegate(
                    delegate
                    {
                        workflowRuntime.ResumeWorkflow();
                    }));

                    break;
            }
        }

        if (isSync)
        {
            switch (operationsId)
            {
                case 2:
                    {
                        workflowRuntime.WaitForCanceled(expectedTrace);
                        break;
                    }
                case 3:
                    {
                        workflowRuntime.WaitForTerminated(1, out Exception terminationException, expectedTrace);
                        break;
                    }
                case 4:
                    {
                        // We tried to do a ResumeWorkflow without loading it after an unload, so we expected
                        // to get a WorkflowApplicationUnloadedException. The workflow will never complete,
                        // so don't wait for it to complete.
                        break;
                    }
            }
        }
        else
        {
            //Give some time for Workflow to execute
            Thread.CurrentThread.Join((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        if (isSync)
        {
            expectedTrace.AddIgnoreTypes(typeof(WorkflowInstanceTrace));
            workflowRuntime.ActualTrace.Validate(expectedTrace);
        }
        else
        {
            //The traces are vary in the async situations, thus we can not do a full trace valdation
            //validate the writeline after read activity is not executed is sufficient
            if (workflowRuntime.ActualTrace.ToString().Contains(shouldNotExecuteMsg))
            {
                throw new Exception("The NotExecuted WriteLine activity has been executed, the expectation is it does not");
            }
        }
    }

    [Fact()]
    public static void TestPersistDuringResumeBookmark()
    {
        bool isSync = true;
        Variable<int> value = VariableHelper.Create<int>("value");
        Variable<string> persist = VariableHelper.Create<string>("persist");
        const string WaitMessage = "Continue the WaitActivity";

        TestSequence testSequence = new TestSequence()
        {
            Variables = { value, persist },
            Activities =
            {
                new TestWriteLine()
                {
                    Message = "Workflow Started"
                },
                new TestWaitForTrace()
                {
                    DisplayName = "WaitActivity",
                    TraceToWait = WaitMessage,
                    DelayDuration = TimeSpan.FromMilliseconds(10)
                },
                new TestWaitReadLine<int>("Read", "Read")
                {
                    BookmarkValue = value,
                    WaitTime = TimeSpan.FromSeconds(1)
                },
                new TestReadLine<string>("PersistBookmark", "PersistBookmark")
                {
                    BookmarkValue = persist
                },
                new TestWriteLine()
                {
                    MessageExpression = ((env)=>value.Get(env).ToString()),
                    HintMessage = "9999"
                }
            }
        };

        var jsonStore = new MemoryInstanceStore();
        var unloadEvent = new ManualResetEvent(false);
        TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testSequence, null, jsonStore, PersistableIdleAction.Unload);
        workflowRuntime.OnWorkflowUnloaded += (_, __) => unloadEvent.Set();

        workflowRuntime.ExecuteWorkflow();
        workflowRuntime.WaitForActivityStatusChange("WaitActivity", TestActivityInstanceState.Executing);
        TestTraceManager.Instance.AddTrace(workflowRuntime.CurrentWorkflowInstanceId, new SynchronizeTrace(WaitMessage));
        SynchronizeTrace.Trace(workflowRuntime.CurrentWorkflowInstanceId, WaitMessage);

        unloadEvent.Reset();
        if (isSync)
        {
            workflowRuntime.ResumeBookMark("Read", 9999);
            workflowRuntime.PersistWorkflow();
        }
        else
        {
            TestWorkflowRuntimeAsyncResult asyncResultResume = workflowRuntime.BeginResumeBookMark("Read", 9999, null, null);
            TestWorkflowRuntimeAsyncResult asyncResultPersist = workflowRuntime.BeginPersistWorkflow(null, null);

            workflowRuntime.EndResumeBookMark(asyncResultResume);
            workflowRuntime.EndPersistWorkflow(asyncResultPersist);
        }

        workflowRuntime.WaitForActivityStatusChange("PersistBookmark", TestActivityInstanceState.Executing);
        unloadEvent.WaitOne();
        workflowRuntime.LoadWorkflow();
        workflowRuntime.ResumeBookMark("PersistBookmark", "Yes");
        workflowRuntime.WaitForCompletion(false);
    }

    [Fact]
    public static void TestResumeWithDelay()
    {
        var testSequence = new TestSequence()
        {
            Activities =
            {
                new TestDelay()
                {
                    Duration = TimeSpan.FromMilliseconds(100)
                },
            }
        };
        WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");
        TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testSequence, null, jsonStore, PersistableIdleAction.Unload);
        workflowRuntime.ExecuteWorkflow();
        workflowRuntime.PersistWorkflow();
        workflowRuntime.WaitForUnloaded();
        workflowRuntime.LoadWorkflow();
        workflowRuntime.ResumeWorkflow();
        workflowRuntime.WaitForCompletion(false);
    }
    [Fact]
    public static void TestNoPersistSerialization()
    {
        TestSequence testSequence = new() { Activities = { new TestNoPersist() }};
        WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");
        TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testSequence, null, jsonStore, PersistableIdleAction.Unload);
        workflowRuntime.ExecuteWorkflow();
        workflowRuntime.PersistWorkflow();
        workflowRuntime.WaitForUnloaded();
        workflowRuntime.LoadWorkflow();
        workflowRuntime.ResumeWorkflow();
        workflowRuntime.WaitForCompletion(false);
    }
    public class TestNoPersist : TestActivity
    {
        public TestNoPersist() => ProductActivity = new NoPersistHandleActivity { Duration = TimeSpan.FromMilliseconds(10) };
        sealed class NoPersistHandleActivity : DelayBase
        {
            private readonly Variable<NoPersistHandle> _noPersistHandle = new();
            protected override void CacheMetadata(NativeActivityMetadata metadata)
            {
                base.CacheMetadata(metadata);
                metadata.AddImplementationVariable(_noPersistHandle);
            }
            protected override void Execute(NativeActivityContext context)
            {
                _noPersistHandle.Get(context).Enter(context);
                _noPersistHandle.Get(context).Exit(context);
                base.Execute(context);
            }
            protected override void OnResume(NativeActivityContext context, Bookmark bookmark, object value)
            {
                _noPersistHandle.Get(context).Enter(context);
                _noPersistHandle.Get(context).Exit(context);
            }
        }
    }
}
class DelayBase : NativeActivity
{
    private static readonly Func<TimerExtension> getDefaultTimerExtension = new(GetDefaultTimerExtension);
    private readonly Variable<Bookmark> _timerBookmark = new();
    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TimeSpan> Duration { get; set; }
    protected override bool CanInduceIdle => true;
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument durationArgument = new("Duration", typeof(TimeSpan), ArgumentDirection.In, true);
        metadata.Bind(Duration, durationArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { durationArgument });
        metadata.AddImplementationVariable(_timerBookmark);
        metadata.AddDefaultExtensionProvider(getDefaultTimerExtension);
    }
    private static TimerExtension GetDefaultTimerExtension() => new DurableTimerExtension();
    protected override void Execute(NativeActivityContext context)
    {
        TimeSpan duration = Duration.Get(context);
        if (duration <= TimeSpan.Zero)
        {
            return;
        }
        TimerExtension timerExtension = GetTimerExtension(context);
        Bookmark bookmark = context.CreateBookmark(OnResume);
        timerExtension.RegisterTimer(duration, bookmark);
        _timerBookmark.Set(context, bookmark);
    }
    protected virtual void OnResume(NativeActivityContext context, Bookmark bookmark, object value) { }
    protected override void Cancel(NativeActivityContext context)
    {
        Bookmark timerBookmark = _timerBookmark.Get(context);
        TimerExtension timerExtension = GetTimerExtension(context);
        timerExtension.CancelTimer(timerBookmark);
        context.RemoveBookmark(timerBookmark);
        context.MarkCanceled();
    }
    protected override void Abort(NativeActivityAbortContext context)
    {
        Bookmark timerBookmark = _timerBookmark.Get(context);
        if (timerBookmark != null)
        {
            TimerExtension timerExtension = GetTimerExtension(context);
            timerExtension.CancelTimer(timerBookmark);
        }
        base.Abort(context);
    }
    private static TimerExtension GetTimerExtension(ActivityContext context) => context.GetExtension<TimerExtension>();
}