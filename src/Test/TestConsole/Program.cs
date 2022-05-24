using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;
using System;
using System.Activities;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using TestCases.Workflows;
using TestCases.Workflows.WF4Samples;

namespace TestConsole
{
    class Program
    {
        static void Main()
        {
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            WorkflowInvoker.Invoke(new TestDelay { Duration = TimeSpan.FromSeconds(1) });
            stopWatch.Stop();
            Console.WriteLine(stopWatch.Elapsed.TotalSeconds);
            return;
            new PowerFxTests().EvaluateMembers();
            var engine = new RecalcEngine();
            var defaultValue = FormulaValue.New(null, typeof(string));
            var record = FormulaValue.RecordFromFields(new NamedValue("x", defaultValue));
            var text = "1+Len(Left(x, 2))/2";
            var checkResult = engine.Check(text, record.Type);
            checkResult.ThrowOnErrors();
            Console.WriteLine(checkResult.ReturnType);
            var formulaValue = engine.Eval(text, record);
            Console.WriteLine(formulaValue.ToObject());
            return;
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            new JustInTimeExpressions().SalaryCalculation();
        }
    }
    [ContentProperty("Duration")]
    public sealed class TestDelay : NativeActivity
    {
        private static readonly Func<TimerExtension> getDefaultTimerExtension = new Func<TimerExtension>(GetDefaultTimerExtension);
        private readonly Variable<Bookmark> _timerBookmark;
        private readonly WriteLine _writeLine = new() { Text = "XXXXXXXXXXXXXXX" };

        public TestDelay()
            : base()
        {
            _timerBookmark = new Variable<Bookmark>();
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TimeSpan> Duration { get; set; }

        protected override bool CanInduceIdle => true;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument durationArgument = new RuntimeArgument("Duration", typeof(TimeSpan), ArgumentDirection.In, true);
            metadata.Bind(Duration, durationArgument);
            metadata.AddChild(_writeLine);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { durationArgument });
            metadata.AddImplementationVariable(_timerBookmark);
            metadata.AddDefaultExtensionProvider(getDefaultTimerExtension);
        }

        private static TimerExtension GetDefaultTimerExtension() => new DurableTimerExtension();

        protected override void Execute(NativeActivityContext context)
        {
            TimeSpan duration = Duration.Get(context);

            if (duration == TimeSpan.Zero)
            {
                return;
            }

            TimerExtension timerExtension = GetTimerExtension(context);
            Bookmark bookmark = context.CreateBookmark();
            timerExtension.RegisterTimer(duration, bookmark);
            _timerBookmark.Set(context, bookmark);
            context.ScheduleActivity(_writeLine);
        }

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
            // The bookmark could be null in abort when user passed in a negative delay as a duration
            if (timerBookmark != null)
            {
                TimerExtension timerExtension = GetTimerExtension(context);
                timerExtension.CancelTimer(timerBookmark);
            }
            base.Abort(context);
        }

        private TimerExtension GetTimerExtension(ActivityContext context)
        {
            TimerExtension timerExtension = context.GetExtension<TimerExtension>();
            return timerExtension;
        }
    }
    public abstract class AsyncTaskNativeActivity : NativeActivity
    {
        private AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

        // Always true because we create bookmarks.
        protected override bool CanInduceIdle => true;

        protected override void Cancel(NativeActivityContext context)
        {
            _impl.Cancel(context);

            // Called so that any outstanding bookmarks are removed.
            // Not the only side effect but it's what we're interested in here.
            base.Cancel(context);
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            _impl.CacheMetadata(metadata);
            base.CacheMetadata(metadata);
        }

        protected abstract Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken);

        protected override void Execute(NativeActivityContext context)
        {
            _impl.Execute(context, ExecuteAsync, BookmarkResumptionCallback);
        }

        protected virtual void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            _impl.BookmarkResumptionCallback(context, value);
        }
    }

    public abstract class AsyncTaskNativeActivity<T> : NativeActivity<T>
    {
        private AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

        protected override bool CanInduceIdle => true;

        protected override void Cancel(NativeActivityContext context)
        {
            _impl.Cancel(context);
            base.Cancel(context);
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            _impl.CacheMetadata(metadata);
            base.CacheMetadata(metadata);
        }

        protected abstract Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken);

        protected override void Execute(NativeActivityContext context)
        {
            _impl.Execute(context, ExecuteAsync, BookmarkResumptionCallback);
        }

        protected virtual void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            _impl.BookmarkResumptionCallback(context, value);
        }
    }

    internal struct AsyncTaskNativeImplementation
    {
        private Variable<NoPersistHandle> _noPersistHandle;

        // The token from this source should be passed around to any async tasks.
        private Variable<CancellationTokenSource> _cancellationTokenSource;

        private Variable<bool> _bookmarkResumed;

        public void Cancel(NativeActivityContext context)
        {
            bool bookmarkResumed = _bookmarkResumed.Get(context);

            if (!bookmarkResumed)
            {
                CancellationTokenSource cancellationTokenSource = _cancellationTokenSource.Get(context);

                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            context.MarkCanceled();
            // Overriding the Cancel method inhibits the propagation of cancellation requests to children.
            context.CancelChildren();

            if (!bookmarkResumed)
            {
                _noPersistHandle.Get(context).Exit(context);
            }
        }

        public void CacheMetadata(NativeActivityMetadata metadata)
        {
            _noPersistHandle = new Variable<NoPersistHandle>();
            _cancellationTokenSource = new Variable<CancellationTokenSource>();
            _bookmarkResumed = new Variable<bool>();

            metadata.AddImplementationVariable(_noPersistHandle);
            metadata.AddImplementationVariable(_cancellationTokenSource);
            metadata.AddImplementationVariable(_bookmarkResumed);
            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider(BookmarkResumptionHelper.Create);
        }

        public void Execute(NativeActivityContext context
            , Func<NativeActivityContext, CancellationToken, Task<Action<NativeActivityContext>>> onExecute
            , BookmarkCallback callback)
        {
            _noPersistHandle.Get(context).Enter(context);

            Bookmark bookmark = context.CreateBookmark(callback);
            BookmarkResumptionHelper bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Set(context, cancellationTokenSource);

            _bookmarkResumed.Set(context, false);

            onExecute(context, cancellationTokenSource.Token).ContinueWith(t =>
            {
                // We resume the bookmark only if the activity wasn't
                // cancelled since the cancellation removes any bookmarks.
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    object executionResult = null;

                    if (t.IsFaulted)
                    {
                        executionResult = t.Exception.InnerException;
                    }
                    else
                    {
                        executionResult = t.Result;
                    }

                    bookmarkHelper.ResumeBookmark(bookmark, executionResult);
                }
            });
        }

        public void BookmarkResumptionCallback(NativeActivityContext context, object value)
        {
            if (value is Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            _noPersistHandle.Get(context).Exit(context);

            Action<NativeActivityContext> executeCallback = value as Action<NativeActivityContext>;
            executeCallback?.Invoke(context);

            _cancellationTokenSource.Get(context)?.Dispose();

            _bookmarkResumed.Set(context, true);
        }
    }
    internal sealed class BookmarkResumptionHelper : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy _workflowInstance;

        public static BookmarkResumptionHelper Create()
        {
            return new BookmarkResumptionHelper();
        }

        internal BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value)
        {
            return _workflowInstance.EndResumeBookmark(_workflowInstance.BeginResumeBookmark(bookmark, value, null, null));
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            yield break;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            _workflowInstance = instance;
        }
    }
}