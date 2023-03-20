// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Runtime.Common;
using TestCases.Runtime.Common.Activities;
using Xunit;

namespace TestCases.Runtime.WorkflowInstanceTest;

public class WorkflowInstanceAbortTests
{
    private const string bookMarkName_InvalidOperationsOnAbortedWorkflow = "Read1";
    private const string AbortedHandlerCalled = "Abort handler is called";
    private const string TraceMessage_ThrowExceptionFromAborted = "Throwing an exception from Workflow Aborted callback";
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetBookMarksOnAbortedWorkflow(bool isDefaultTimeout)
    {
        const string traceToWaitMsg = "Continue WaitForTrace1";
        Sequence sequence = new Sequence()
        {
            Activities =
            {
                new WaitForTrace()
                {
                    DisplayName = "WaitForTrace1",
                    TraceToWait = new InArgument<string>(traceToWaitMsg)
                },
                new ReadLine<string>(bookMarkName_InvalidOperationsOnAbortedWorkflow)
                {
                }
            }
        };

        WorkflowApplication instance = new WorkflowApplication(sequence)
        {
            Aborted = OnWorkflowInstanceAborted
        };

        instance.Run();

        TestTraceManager.Instance.WaitForTrace(instance.Id, new SynchronizeTrace(WaitForTrace.EnterExecute), 1);
        instance.Abort();
        //Continue WaitForTrace. The instance should get aborted after WaitForTrace1 activity is done
        TestTraceManager.Instance.AddTrace(instance.Id, new SynchronizeTrace(traceToWaitMsg));
        SynchronizeTrace.Trace(instance.Id, traceToWaitMsg);

        TestTraceManager.Instance.WaitForTrace(instance.Id, new SynchronizeTrace(WorkflowInstanceAbortTests.AbortedHandlerCalled), 1);
        string message = String.Format(ExceptionStrings.WorkflowApplicationAborted, instance.Id);
        ExceptionHelpers.CheckForException(typeof(WorkflowApplicationAbortedException), message,
        delegate
        {
            if (isDefaultTimeout)
            {
                instance.GetBookmarks();
            }
            else
            {
                instance.GetBookmarks(TimeSpan.FromMilliseconds(1));
            }
        });
    }

    private void OnWorkflowInstanceAborted(WorkflowApplicationAbortedEventArgs e)
    {
        TestTraceManager.Instance.AddTrace(e.InstanceId, new SynchronizeTrace(WorkflowInstanceAbortTests.AbortedHandlerCalled));
        SynchronizeTrace.Trace(e.InstanceId, WorkflowInstanceAbortTests.AbortedHandlerCalled);
    }

    //Get a method that matches the name parameter
    private MethodInfo GetAMethod(Type type, string name)
    {
        MethodInfo[] methodInfos = type.GetMethods();
        foreach (MethodInfo methodInfo in methodInfos)
        {
            if (methodInfo.Name.Equals(name))
            {
                return methodInfo;
            }
        }

        return null;
    }

    private object[] GetParameterInstances(MethodInfo methodInfo)
    {
        object[] methodParameters = null;
        ParameterInfo[] paraInfos = methodInfo.GetParameters();
        if (paraInfos.Length != 0)
        {
            methodParameters = new object[paraInfos.Length];
            for (int i = 0; i < paraInfos.Length; i++)
            {
                if (paraInfos[i].ParameterType == typeof(string))
                {
                    methodParameters[i] = bookMarkName_InvalidOperationsOnAbortedWorkflow;
                }
                else
                {
                    methodParameters[i] = RuntimeHelper.GetAParameterInstance(paraInfos[i]);
                }
            }
        }
        return methodParameters;
    }
    [Theory]
    [InlineData(false)]

    private         // Abort with Cancel failing in desktop too. so disabling test
                    //[InlineData(true)]
            void AbortParallel(bool isTestCancel)
    {
        const int noBranches = 10;
        Variable<int> value = VariableHelper.Create<int>("value");

        TestParallel parallel = new TestParallel()
        {
            Variables = { value },
            ExpectedOutcome = Outcome.Faulted
        };

        for (int i = 0; i < noBranches; i++)
        {
            string branchName = "Branch" + i.ToString();

            TestSequence branchSequence = new TestSequence("Seq" + branchName)
            {
                Activities =
                {
                    new TestWriteLine()
                    {
                        Message = branchName + " Started"
                    },
                    new TestReadLine<int>(branchName, branchName)
                    {
                        BookmarkValue = value,
                        ExpectedOutcome = Outcome.Faulted
                    },
                    new TestWriteLine()
                    {
                        Message = branchName + " Completed",
                        ExpectedOutcome = Outcome.Faulted
                    },
                },
                ExpectedOutcome = Outcome.Faulted
            };

            if (isTestCancel)
            {
                if (i == 0)
                {
                    branchSequence.Activities[1].ExpectedOutcome = Outcome.Canceled;
                }
            }

            parallel.Branches.Add(branchSequence);
        }

        TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(parallel);
        runtime.ExecuteWorkflow();

        runtime.WaitForIdle();

        //Cancel Workflow
        if (isTestCancel)
        {
            //Log.Info("Cancelling Workflow");
            runtime.CancelWorkflow();
            runtime.WaitForCanceled();
        }

        //Abort Workflow
        runtime.AbortWorkflow("Aborting for Test");
        ExpectedTrace expectedTrace = parallel.GetExpectedTrace();
        //Only verify User trace since activity traces will not be available once abort is called
        expectedTrace.AddVerifyTypes(typeof(UserTrace));
        runtime.WaitForAborted(out Exception excepion, expectedTrace);
    }

    [Fact()]
    public void TestAbortLoad()
    {
        Variable<int> value = VariableHelper.Create<int>("value");
        TestSequence sequence = new TestSequence()
        {
            Variables = { value },
            Activities =
            {
                new TestAssign<int>()
                {
                    ToVariable = value,
                    Value = 100
                },
                new TestPersist(),
                new TestReadLine<int>("Read", "Read")
                {
                    BookmarkValue = value
                },
                new TestWriteLine("AfterAbort")
                {
                    MessageExpression= (env)=>value.Get(env).ToString(),
                    HintMessage = "9999"
                }
            }
        };

        JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(sequence, null, jsonStore, PersistableIdleAction.Persist);
        runtime.ExecuteWorkflow();
        runtime.WaitForActivityStatusChange("Read", TestActivityInstanceState.Executing);
        runtime.PersistWorkflow();
        runtime.AbortWorkflow("Abort Workflow");
        //Wait Sometime to Handle to free
        Thread.CurrentThread.Join((int)TimeSpan.FromSeconds(4).TotalMilliseconds);
        //Load Workflow from Last Persistence Point
        runtime.LoadWorkflow();
        runtime.ResumeBookMark("Read", 9999);
        runtime.WaitForActivityStatusChange("AfterAbort", TestActivityInstanceState.Closed);

        //Wait for the second completion trace
        TestTraceManager.Instance.WaitForTrace(runtime.CurrentWorkflowInstanceId, new SynchronizeTrace(RemoteWorkflowRuntime.CompletedOrAbortedHandlerCalled), 1);

        //Call Abort on Completed Workflow, this should not throw exception
        runtime.AbortWorkflow("Abort on Completed Workflow");
    }

    /// <summary>
    /// Abort the workflow while Branch0 is executing, Branch0 should complete while the rest of the branches will abort
    /// </summary>        
    [Fact]
    public void TestResumeBookmarkCallback()
    {
        const int noBranches = 10;
        Variable<int> value = VariableHelper.Create<int>("value");

        TestParallel parallel = new TestParallel()
        {
            Variables = { value },
            ExpectedOutcome = Outcome.Faulted
        };

        for (int i = 0; i < noBranches; i++)
        {
            string branchName = "Branch" + i.ToString();
            TestSequence sequence = new TestSequence()
            {
                Activities =
                {
                    new TestWaitReadLine<int>(branchName, branchName)
                    {
                        BookmarkValue = value,
                        WaitTime = TimeSpan.FromSeconds(10),
                    }
                },
                ExpectedOutcome = Outcome.Faulted,
            };

            if (i > 0)
            {
                (sequence.Activities[0] as TestWaitReadLine<int>).ExpectedOutcome = Outcome.Faulted;
            }

            parallel.Branches.Add(sequence);
        }

        JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(parallel, null, jsonStore, PersistableIdleAction.Persist);
        runtime.ExecuteWorkflow();

        runtime.WaitForIdle();

        for (int i = 0; i < noBranches; i++)
        {
            string branchName = "Branch" + i.ToString();
            runtime.BeginResumeBookMark(branchName, i, null, null);
        }

        runtime.WaitForTrace(new UserTrace(WaitReadLine<int>.BeforeWait));

        runtime.AbortWorkflow("Aborting Workflow");

        ExpectedTrace expectTrace = parallel.GetExpectedTrace();
        expectTrace.Trace.Steps.Clear();
        expectTrace.Trace.Steps.Add(new UserTrace(WaitReadLine<int>.BeforeWait));
        expectTrace.Trace.Steps.Add(new UserTrace(WaitReadLine<int>.AfterWait));
        expectTrace.AddVerifyTypes(typeof(UserTrace));

        runtime.WaitForAborted(out Exception exception, expectTrace);
    }

    /// <summary>
    /// Throw an exception in OnAborted callback, Verify runtime has ignored the exception and continue to function
    /// Throw an exception in OnAborted callback, Verify runtime has ignored the exception and continue aborting
    /// </summary>        
    [Fact]
    public void OnAbortedThrowException()
    {
        TestSequence sequence = new TestSequence()
        {
            Activities =
            {
                new TestReadLine<string>("Read1", "Read1")
                {
                }
            }
        };

        TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(sequence);
        runtime.OnWorkflowAborted += new EventHandler<TestWorkflowAbortedEventArgs>(OnWorkflowInstanceAborted_ThrowException);
        runtime.ExecuteWorkflow();
        runtime.WaitForIdle();

        runtime.AbortWorkflow("Abort Workflow");
        TestTraceManager.Instance.WaitForTrace(runtime.CurrentWorkflowInstanceId, new SynchronizeTrace(TraceMessage_ThrowExceptionFromAborted), 1);

        //Verify that the product can continue to function
        runtime = TestRuntime.CreateTestWorkflowRuntime(sequence);
        runtime.ExecuteWorkflow();
        runtime.ResumeBookMark("Read1", "Continue workflow");
        runtime.WaitForCompletion();
    }

    /// <summary>
    /// All operations should be blocked by throwing an InvalidOperationException.
    /// </summary>
    ////[Fact]
    ////public void TestOperationsFromAbortedCallback()
    ////{
    ////    TestSequence sequence = new TestSequence()
    ////    {
    ////        Activities = 
    ////        {
    ////            new TestWriteLine()
    ////            {
    ////                Message = "Write a line"
    ////            },
    ////            new TestReadLine<string>("Read", "Read")
    ////            {
    ////            }
    ////        }
    ////    };
    ////
    ////    TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(sequence);
    ////    runtime.OnWorkflowAborted += new EventHandler<TestWorkflowAbortedEventArgs>(OnWorkflowInstanceAborted_TestOperations);
    ////    runtime.PersistenceProviderFactoryType = null;
    ////    runtime.ExecuteWorkflow();
    ////    runtime.WaitForIdle();
    ////    runtime.AbortWorkflow(AbortReasonMessage);
    ////
    ////    runtime.WaitForTrace(new SynchronizeTrace(WorkflowInstanceHelper.TraceMessage_TriedAllOperations));
    ////    //Wait for the completion trace
    ////
    ////    runtime.WaitForAborted();
    ////    TestTraceManager.Instance.WaitForTrace(runtime.CurrentWorkflowInstanceId, new SynchronizeTrace(RemoteWorkflowRuntime.CompletedOrAbortedHandlerCalled), 1);
    ////}

    private static void OnWorkflowInstanceAborted_ThrowException(object sender, TestWorkflowAbortedEventArgs e)
    {
        TestTraceManager.Instance.AddTrace(e.EventArgs.InstanceId, new SynchronizeTrace(TraceMessage_ThrowExceptionFromAborted));
        SynchronizeTrace.Trace(e.EventArgs.InstanceId, TraceMessage_ThrowExceptionFromAborted);
        throw new Exception("Throw an exception in OnAborted call back");
    }
    [Fact]
    public void Should_return_outargs_on_terminate()
    {
        ActivityWithResult<int> root = new();
        ManualResetEvent manualResetEvent = new(default);
        WorkflowApplicationCompletedEventArgs completedArgs = null;
        WorkflowApplication app = new(root)
        {
            Completed = args =>
            {
                completedArgs = args;
                manualResetEvent.Set();
            }
        };
        var exception = new Exception();
        IAsyncResult asyncResult = null;
        root.Action = ()=> asyncResult = app.BeginTerminate(exception, null, null);
        app.Run();
        manualResetEvent.WaitOne();
        app.EndTerminate(asyncResult);
        completedArgs.TerminationException.ShouldBe(exception);
        completedArgs.Outputs["Result"].ShouldBe(42);
    }
    public class ActivityWithResult<TResult> : NativeActivity<TResult>
    {
        public Action Action;
        protected override bool CanInduceIdle => true;
        protected override void Execute(NativeActivityContext context)
        {
            context.SetValue(Result, 42);
            Action();
            context.CreateBookmark();
        }
        protected override void Cancel(NativeActivityContext context)
        {
            Console.WriteLine("Cancel");
            base.Cancel(context);
        }
    }
}