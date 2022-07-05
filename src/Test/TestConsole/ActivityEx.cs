using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
namespace TestConsole;
using StringToObject = Dictionary<string, object>;
public class KeyValues
{
    StringToObject _values;
    public KeyValues() { }
    public KeyValues(StringToObject args) => _values = args;
    protected void Set(object value, [CallerMemberName] string name = null)
    {
        _values ??= new();
        _values[name] = value;
    }
    protected T Get<T>([CallerMemberName] string name = null) => (T)_values?.GetValueOrDefault(name);
    internal StringToObject Values { get => _values; init => _values = value; }
}
public abstract class ActivityEx : KeyValues
{
    protected ActivityEx(Activity activity) => Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    internal Activity Activity { get; }
}
public class ActivityEx<TKeyedValues> : ActivityEx where TKeyedValues : KeyValues, new()
{
    public ActivityEx(Activity activity) : base(activity) { }
    public async Task<TKeyedValues> ExecuteAsync()
    {
        var values = await ((AsyncCodeNativeActivity)Activity.GetParent()).ExecuteAsync(this);
        return values == null ? default : new() { Values = values };
    }
}
public abstract class AsyncCodeNativeActivity : AsyncTaskNativeActivity
{
    ActivityEx _activityEx;
    TaskCompletionSource<StringToObject> _completionSource;
    protected ActivityEx[] _children;
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);
        foreach (var child in _children)
        {
            metadata.AddImplementationChild(child.Activity);
        }
    }
    public async Task<StringToObject> ExecuteAsync(ActivityEx activityEx)
    {
        if (_completionSource != null)
        {
            throw new InvalidOperationException("There is already an async call in progress! Make sure you awaited the previous call.");
        }
        await Task.Yield();
        _activityEx = activityEx;
        _completionSource = new();
        try
        {
            Resume(null);
            return await _completionSource.Task;
        }
        finally
        {
            _completionSource = null;
        }
    }
    protected override void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
    {
        if (_activityEx == null)
        {
            base.BookmarkResumptionCallback(context, bookmark, value);
            return;
        }
        context.ScheduleActivity(_activityEx.Activity,
            (_, instance) => _completionSource.SetResult(instance.GetOutputs()), (_, ex, __) => _completionSource.SetException(ex), _activityEx.Values);
        _activityEx = null;
    }
}
public abstract class AsyncTaskNativeActivity : NativeActivity
{
    AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

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

    protected abstract Task ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken);

    protected override void Execute(NativeActivityContext context)
    {
        _impl.Execute(context, ExecuteAsync, BookmarkResumptionCallback);
    }

    protected virtual void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
    {
        _impl.BookmarkResumptionCallback(context, value);
    }
    protected void Resume(object result) => _impl.Resume(result);
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

    public void Execute(NativeActivityContext context, Func<NativeActivityContext, CancellationToken, Task> onExecute, BookmarkCallback callback)
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