using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;
using ReflectionMagic;
using System;
using System.Activities;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using TestCases.Workflows;
using TestCases.Workflows.WF4Samples;

namespace TestConsole
{
    using StringToObject = Dictionary<string, object>;

    class Program
    {
        static void Main()
        {
            WorkflowInvoker.Invoke(new TestDelay());
            return;
            WorkflowInvoker.Invoke(new Sequence());
            while (true)
            {
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                for (int index = 0; index < 1000; index++)
                {
                    WorkflowInvoker.Invoke(new Sequence());
                }
                stopWatch.Stop();
                Console.WriteLine(stopWatch.Elapsed.TotalMilliseconds);
                Console.WriteLine(GC.CollectionCount(0));
                Console.ReadLine();
            }
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
    public class WriteLineEx : ActivityEx<KeyedValues>
    {
        public WriteLineEx(WriteLine activity) : base(activity) { }
        public string Text
        {
            get => Get<string>();
            set => Set(value);
        }
        public TextWriter TextWriter
        {
            get => Get<TextWriter>();
            set => Set(value);
        }
    }
    public class ActivityEx<TKeyedValues> : KeyedValues, IActivityEx where TKeyedValues : IKeyedValues, new()
    {
        readonly Activity _activity;
        public ActivityEx(Activity activity) => _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        Activity IActivityEx.Activity { get => _activity; }
        public async Task<TKeyedValues> ExecuteAsync()
        {
            var values = await ((HybridActivity)_activity.GetParent()).ExecuteAsync(this);
            return values == null ? default : new() { Values = values };
        }
    }
    public class AssignEx<T> : ActivityEx<AssignOutputs<T>>
    {
        public AssignEx(Assign<T> activity) : base(activity) { }
        public T Value
        {
            get => Get<T>();
            set => Set(value);
        }
    }
    public class AssignOutputs<T> : KeyedValues
    {
        public T To => Get<T>();
    }
    public class KeyedValues : IKeyedValues
    {
        StringToObject _values;
        public KeyedValues() { }
        public KeyedValues(StringToObject args) => _values = args;
        protected void Set(object value, [CallerMemberName] string name = null)
        {
            _values ??= new();
            _values[name] = value;
        }
        protected T Get<T>([CallerMemberName] string name = null) => (T)_values?.GetValueOrDefault(name);
        StringToObject IKeyedValues.Values { get => _values; set => _values = value; }
    }
    public interface IActivityEx : IKeyedValues
    {
        public Activity Activity { get; }
    }
    public interface IKeyedValues
    {
        public StringToObject Values { get; set; }
    }
    public abstract class HybridActivity : AsyncTaskNativeActivity
    {
        Activity _activity;
        TaskCompletionSource<StringToObject> _completionSource;
        protected IActivityEx[] _children;
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.SetImplementationChildrenCollection(new(Array.ConvertAll(_children, c => c.Activity)));
        }
        public async Task<StringToObject> ExecuteAsync(IActivityEx activityEx)
        {
            if (_completionSource != null)
            {
                throw new InvalidOperationException("There is already an async call in progress! Make sure you awaited the previous call.");
            }
            await Task.Yield();
            _activity = activityEx.Activity;
            _completionSource = new();
            try
            {
                _impl.Resume(true);
                return await _completionSource.Task;
            }
            finally
            {
                _completionSource = null;
            }
        }
        protected override void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            var activity = _activity;
            _activity = null;
            if (activity != null)
            {
                var activityInstance = context.ScheduleActivity(activity, (_, instance) => _completionSource.SetResult(instance.GetOutputs()), (_, ex, __) => _completionSource.SetException(ex));
            }
            else
            {
                _impl.BookmarkResumptionCallback(context, value);
            }
        }
    }
    public class TestDelay : HybridActivity
    {
        WriteLineEx _writeLine1 = new(new WriteLine() { Text = "AAAAAAAAAAAAAAAA" });
        WriteLineEx _writeLine2 = new(new WriteLine() { Text = "BBBBBBBBBBBBBBBB" });
        public TestDelay() => _children = new[] { _writeLine1, _writeLine2 };
        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            //context.AsDynamic().AllowChainedEnvironmentAccess = true;
            for (int index = 0; index < 3; index++)
            {
                //_writeLine1.Text.Set(context, index.ToString());
                await _writeLine1.ExecuteAsync();
            }
            await Task.Delay(100, cancellationToken);
            await ExecuteAsync(_writeLine1);
            await Task.Delay(1000, cancellationToken);
            await ExecuteAsync(_writeLine2);
            await Task.Delay(1000, cancellationToken);
            return _ => { };
        }
    }
    public abstract class AsyncTaskNativeActivity : NativeActivity
    {
        protected AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

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
        protected AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

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

    public struct AsyncTaskNativeImplementation
    {
        private Variable<NoPersistHandle> _noPersistHandle;

        // The token from this source should be passed around to any async tasks.
        private Variable<CancellationTokenSource> _cancellationTokenSource;

        private Variable<bool> _bookmarkResumed;
        BookmarkResumptionHelper _bookmarkHelper;
        Bookmark _bookmark;
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

            _bookmark = context.CreateBookmark(callback, BookmarkOptions.MultipleResume);
            _bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Set(context, cancellationTokenSource);

            _bookmarkResumed.Set(context, false);
            var _this = this;
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
                    _this.Resume(executionResult);
                }
            });
        }
        public void Resume(object executionResult) => _bookmarkHelper.ResumeBookmark(_bookmark, executionResult);
        public void BookmarkResumptionCallback(NativeActivityContext context, object value)
        {
            if (value is Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            _noPersistHandle.Get(context).Exit(context);
            context.RemoveBookmark(_bookmark);

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