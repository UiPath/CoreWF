// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;

namespace System.Activities;
using Internals;
using Runtime;
using Tracking;

[Fx.Tag.XamlVisible(false)]
public class NativeActivityContext : ActivityContext
{
    private BookmarkManager _bookmarkManager;
    private ActivityExecutor _executor;

    // This is called by the Pool.
    internal NativeActivityContext() { }

    // This is only used by base classes which do not take
    // part in pooling.
    internal NativeActivityContext(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
        : base(instance, executor)
    {
        _executor = executor;
        _bookmarkManager = bookmarkManager;
    }

    public BookmarkScope DefaultBookmarkScope
    {
        get
        {
            ThrowIfDisposed();
            return _executor.BookmarkScopeManager.Default;
        }
    }

    public bool IsCancellationRequested
    {
        get
        {
            ThrowIfDisposed();
            return CurrentInstance.IsCancellationRequested;
        }
    }

    public ExecutionProperties Properties
    {
        get
        {
            ThrowIfDisposed();
            return new ExecutionProperties(this, CurrentInstance, CurrentInstance.PropertyManager);
        }
    }

    internal bool HasRuntimeTransaction => _executor.HasRuntimeTransaction;

    internal bool RequiresTransactionContextWaiterExists => _executor.RequiresTransactionContextWaiterExists;

    internal bool IsInNoPersistScope => (Properties.Find(NoPersistProperty.Name) != null) || _executor.HasRuntimeTransaction;

    internal void Initialize(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        Reinitialize(instance, executor);
        _executor = executor;
        _bookmarkManager = bookmarkManager;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public T GetValue<T>(Variable<T> variable)
    {
        ThrowIfDisposed();

        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        return GetValueCore<T>(variable);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "We explicitly provide a Variable overload to avoid requiring the object type parameter.")]
    public object GetValue(Variable variable)
    {
        ThrowIfDisposed();

        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        return GetValueCore<object>(variable);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public void SetValue<T>(Variable<T> variable, T value)
    {
        ThrowIfDisposed();

        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        SetValueCore(variable, value);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "We explicitly provide a Variable overload to avoid requiring the object type parameter.")]
    public void SetValue(Variable variable, object value)
    {
        ThrowIfDisposed();

        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        SetValueCore(variable, value);
    }

    public void CancelChildren()
    {
        ThrowIfDisposed();

        CurrentInstance.CancelChildren(this);
    }

    public ReadOnlyCollection<ActivityInstance> GetChildren()
    {
        ThrowIfDisposed();

        return CurrentInstance.GetChildren();
    }

    public void AbortChildInstance(ActivityInstance activity) => AbortChildInstance(activity, null);

    public void AbortChildInstance(ActivityInstance activity, Exception reason)
    {
        ThrowIfDisposed();

        if (activity == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activity));
        }

        if (activity.IsCompleted)
        {
            // We shortcut since we might not actually have
            // a reference to the parent for an already
            // completed child.
            return;
        }

        if (!ReferenceEquals(activity.Parent, CurrentInstance))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CanOnlyAbortDirectChildren));
        }

        _executor.AbortActivityInstance(activity, reason);
    }

    public void Abort()
    {
        Abort(null);
    }

    public void Abort(Exception reason)
    {
        ThrowIfDisposed();
        _executor.AbortWorkflowInstance(reason);
    }

    internal void Terminate(Exception reason) => _executor.ScheduleTerminate(reason);

    public void Track(CustomTrackingRecord record)
    {
        ThrowIfDisposed();

        if (record == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(record));
        }

        TrackCore(record);
    }

    public void CancelChild(ActivityInstance activityInstance)
    {
        ThrowIfDisposed();
        if (activityInstance == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityInstance));
        }

        if (activityInstance.IsCompleted)
        {
            // We shortcut since we might not actually have
            // a reference to the parent for an already
            // completed child.
            return;
        }

        if (!ReferenceEquals(activityInstance.Parent, CurrentInstance))
        {
            throw FxTrace.Exception.AsError(
                new InvalidOperationException(SR.CanOnlyCancelDirectChildren));
        }

        _executor.CancelActivity(activityInstance);
    }

    internal void Cancel()
    {
        ThrowIfDisposed();
        CurrentInstance.BaseCancel(this);
    }

    public Bookmark CreateBookmark(string name)
    {
        // We don't allow BookmarkOptions to be specified for bookmarks without callbacks
        // because it must be Blocking and SingleFire to be of any value

        ThrowIfDisposed();
        ThrowIfCanInduceIdleNotSet();

        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        return _bookmarkManager.CreateBookmark(name, null, CurrentInstance, BookmarkOptions.None);
    }

    public Bookmark CreateBookmark(string name, BookmarkCallback callback) => CreateBookmark(name, callback, BookmarkOptions.None);

    public Bookmark CreateBookmark(string name, BookmarkCallback callback, BookmarkOptions options)
    {
        ThrowIfDisposed();
        ThrowIfCanInduceIdleNotSet();
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        if (callback == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(callback));
        }

        if (!CallbackWrapper.IsValidCallback(callback, CurrentInstance))
        {
            throw FxTrace.Exception.Argument(nameof(callback), SR.InvalidExecutionCallback(callback, Activity.ToString()));
        }

        BookmarkOptionsHelper.Validate(options, "options");

        return _bookmarkManager.CreateBookmark(name, callback, CurrentInstance, options);
    }

    public Bookmark CreateBookmark(string name, BookmarkCallback callback, BookmarkScope scope) => CreateBookmark(name, callback, scope, BookmarkOptions.None);

    public Bookmark CreateBookmark(string name, BookmarkCallback callback, BookmarkScope scope, BookmarkOptions options)
    {
        ThrowIfDisposed();
        ThrowIfCanInduceIdleNotSet();

        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        if (!CallbackWrapper.IsValidCallback(callback, CurrentInstance))
        {
            throw FxTrace.Exception.Argument(nameof(callback), SR.InvalidExecutionCallback(callback, Activity.ToString()));
        }

        if (scope == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(scope));
        }

        BookmarkOptionsHelper.Validate(options, "options");

        return _executor.BookmarkScopeManager.CreateBookmark(name, scope, callback, CurrentInstance, options);
    }

    // we don't just do CreateBookmark(BookmarkCallback callback = null, BookmarkOptions options = BookmarkOptions.None) below
    // since there would be overload resolution issues between it and CreateBookmark(string)
    public Bookmark CreateBookmark() => CreateBookmark((BookmarkCallback)null);

    public Bookmark CreateBookmark(BookmarkCallback callback) => CreateBookmark(callback, BookmarkOptions.None);

    public Bookmark CreateBookmark(BookmarkCallback callback, BookmarkOptions options)
    {
        ThrowIfDisposed();
        ThrowIfCanInduceIdleNotSet();

        if (callback != null && !CallbackWrapper.IsValidCallback(callback, CurrentInstance))
        {
            throw FxTrace.Exception.Argument(nameof(callback), SR.InvalidExecutionCallback(callback, Activity.ToString()));
        }

        BookmarkOptionsHelper.Validate(options, "options");

        return _bookmarkManager.CreateBookmark(callback, CurrentInstance, options);
    }

    internal BookmarkScope CreateBookmarkScope() => CreateBookmarkScope(Guid.Empty);

    internal BookmarkScope CreateBookmarkScope(Guid scopeId) => CreateBookmarkScope(scopeId, null);

    internal BookmarkScope CreateBookmarkScope(Guid scopeId, BookmarkScopeHandle scopeHandle)
    {
        Fx.Assert(!IsDisposed, "This should not be disposed.");

        if (scopeId != Guid.Empty && !_executor.KeysAllowed)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopesRequireKeys));
        }

        return _executor.BookmarkScopeManager.CreateAndRegisterScope(scopeId, scopeHandle);
    }

    internal void UnregisterBookmarkScope(BookmarkScope scope)
    {
        Fx.Assert(!IsDisposed, "This should not be disposed.");
        Fx.Assert(scope != null, "The scope should not equal null.");

        _executor.BookmarkScopeManager.UnregisterScope(scope);
    }

    internal void InitializeBookmarkScope(BookmarkScope scope, Guid id)
    {
        Fx.Assert(scope != null, "The scope should not be null.");
        Fx.Assert(id != Guid.Empty, "The caller should make sure this isn't empty.");

        ThrowIfDisposed();
        if (!_executor.KeysAllowed)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopesRequireKeys));
        }

        _executor.BookmarkScopeManager.InitializeScope(scope, id);
    }

    internal void RethrowException(FaultContext context)
    {
        Fx.Assert(!IsDisposed, "Must not be disposed.");

        _executor.RethrowException(CurrentInstance, context);
    }

    public void RemoveAllBookmarks()
    {
        ThrowIfDisposed();

        CurrentInstance.RemoveAllBookmarks(_executor.RawBookmarkScopeManager, _bookmarkManager);
    }

    public void MarkCanceled()
    {
        ThrowIfDisposed();

        if (!CurrentInstance.IsCancellationRequested)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MarkCanceledOnlyCallableIfCancelRequested));
        }

        CurrentInstance.MarkCanceled();
    }

    public bool RemoveBookmark(string name)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNull(nameof(name));
        }

        return RemoveBookmark(new Bookmark(name));
    }

    public bool RemoveBookmark(Bookmark bookmark)
    {
        ThrowIfDisposed();
        if (bookmark == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(bookmark));
        }
        return _bookmarkManager.Remove(bookmark, CurrentInstance);
    }

    public bool RemoveBookmark(string name, BookmarkScope scope)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        if (scope == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(scope));
        }

        return _executor.BookmarkScopeManager.RemoveBookmark(new Bookmark(name), scope, CurrentInstance);
    }

    public BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value)
    {
        ThrowIfDisposed();
        if (bookmark == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(bookmark));
        }
        return _executor.TryResumeUserBookmark(bookmark, value, false);
    }

    internal void RegisterMainRootCompleteCallback(Bookmark bookmark)
    {
        Fx.Assert(!IsDisposed, "Shouldn't call this on a disposed object.");
        Fx.Assert(bookmark != null, "Must have a bookmark.");

        _executor.RegisterMainRootCompleteCallback(bookmark);
    }

    internal ActivityInstance ScheduleSecondaryRoot(Activity activity, LocationEnvironment environment)
    {
        Fx.Assert(!IsDisposed, "Shouldn't call this on a disposed object.");
        Fx.Assert(activity != null, "Activity must not be null.");

        return _executor.ScheduleSecondaryRootActivity(activity, environment);
    }

    public ActivityInstance ScheduleActivity(Activity activity) => ScheduleActivity(activity, null, null);

    public ActivityInstance ScheduleActivity(Activity activity, CompletionCallback onCompleted) => ScheduleActivity(activity, onCompleted, null);

    public ActivityInstance ScheduleActivity(Activity activity, FaultCallback onFaulted) => ScheduleActivity(activity, null, onFaulted);

    public ActivityInstance ScheduleActivity(Activity activity, CompletionCallback onCompleted, FaultCallback onFaulted) =>
        ScheduleActivity(activity, onCompleted, onFaulted, null);

    public ActivityInstance ScheduleActivity(Activity activity, CompletionCallback onCompleted, FaultCallback onFaulted, 
        IDictionary<string, object> argumentValueOverrides)
    {
        ThrowIfDisposed();

        if (activity == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activity));
        }
        CompletionBookmark completionBookmark = null;
        FaultBookmark faultBookmark = null;

        if (onCompleted != null)
        {
            if (CallbackWrapper.IsValidCallback(onCompleted, CurrentInstance))
            {
                completionBookmark = ActivityUtilities.CreateCompletionBookmark(onCompleted, CurrentInstance);
            }
            else
            {
                throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, Activity.ToString()));
            }
        }

        if (onFaulted != null)
        {
            if (CallbackWrapper.IsValidCallback(onFaulted, CurrentInstance))
            {
                faultBookmark = ActivityUtilities.CreateFaultBookmark(onFaulted, CurrentInstance);
            }
            else
            {
                throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, Activity.ToString()));
            }
        }

        return InternalScheduleActivity(activity, completionBookmark, faultBookmark, argumentValueOverrides);
    }

    private ActivityInstance InternalScheduleActivity(Activity activity, CompletionBookmark onCompleted, FaultBookmark onFaulted,
        IDictionary<string, object> argumentValueOverrides = null)
    {
        ActivityInstance parent = CurrentInstance;

        if (!activity.IsMetadataCached || activity.CacheId != parent.Activity.CacheId)
        {
            throw FxTrace.Exception.Argument(nameof(activity), SR.ActivityNotPartOfThisTree(activity.DisplayName, parent.Activity.DisplayName));
        }

        if (!activity.CanBeScheduledBy(parent.Activity))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CanOnlyScheduleDirectChildren(parent.Activity.DisplayName, activity.DisplayName, activity.Parent.DisplayName)));
        }

        if (activity.HandlerOf != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateHandlersCannotBeScheduledDirectly(parent.Activity.DisplayName, activity.DisplayName)));
        }

        if (parent.WaitingForTransactionContext)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotScheduleChildrenWhileEnteringIsolation));
        }

        if (parent.IsPerformingDefaultCancelation)
        {
            parent.MarkCanceled();
            return ActivityInstance.CreateCanceledInstance(activity);
        }

        return _executor.ScheduleActivity(activity, parent, onCompleted, onFaulted, null, argumentValueOverrides);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction(ActivityAction activityAction, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        return InternalScheduleDelegate(activityAction, ActivityUtilities.EmptyParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T>(ActivityAction<T> activityAction, T argument, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(1)
        {
            { ActivityDelegate.ArgumentName, argument },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2>(ActivityAction<T1, T2> activityAction, T1 argument1, T2 argument2, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(2)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3>(ActivityAction<T1, T2, T3> activityAction, T1 argument1, T2 argument2, T3 argument3, CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(3)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4>(ActivityAction<T1, T2, T3, T4> activityAction, T1 argument1, T2 argument2, T3 argument3, T4 argument4,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(4)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5>(
        ActivityAction<T1, T2, T3, T4, T5> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(5)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6>(
        ActivityAction<T1, T2, T3, T4, T5, T6> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(6)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(7)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(8)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(9)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(10)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(11)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(12)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(13)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(14)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(15)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
            { ActivityDelegate.Argument15Name, argument15 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> activityAction,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, T16 argument16,
        CompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityAction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityAction));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(16)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
            { ActivityDelegate.Argument15Name, argument15 },
            { ActivityDelegate.Argument16Name, argument16 },
        };

        return InternalScheduleDelegate(activityAction, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleActivity<TResult>(Activity<TResult> activity, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activity == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activity));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        return InternalScheduleActivity(activity, ActivityUtilities.CreateCompletionBookmark(onCompleted, parent), ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<TResult>(ActivityFunc<TResult> activityFunc, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        return InternalScheduleDelegate(activityFunc, ActivityUtilities.EmptyParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T, TResult>(ActivityFunc<T, TResult> activityFunc, T argument, CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(1)
        {
            { ActivityDelegate.ArgumentName, argument }
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, TResult>(ActivityFunc<T1, T2, TResult> activityFunc, T1 argument1, T2 argument2,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(2)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, TResult>(ActivityFunc<T1, T2, T3, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(3)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, TResult>(ActivityFunc<T1, T2, T3, T4, TResult> activityFunc, T1 argument1, T2 argument2, T3 argument3, T4 argument4,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(4)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //   Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(5)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(6)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(7)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(8)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(9)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(10)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(11)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(12)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(13)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(14)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(15)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
            { ActivityDelegate.Argument15Name, argument15 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
        ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> activityFunc,
        T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, T10 argument10, T11 argument11, T12 argument12, T13 argument13, T14 argument14, T15 argument15, T16 argument16,
        CompletionCallback<TResult> onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityFunc == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityFunc));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        Dictionary<string, object> inputParameters = new(16)
        {
            { ActivityDelegate.Argument1Name, argument1 },
            { ActivityDelegate.Argument2Name, argument2 },
            { ActivityDelegate.Argument3Name, argument3 },
            { ActivityDelegate.Argument4Name, argument4 },
            { ActivityDelegate.Argument5Name, argument5 },
            { ActivityDelegate.Argument6Name, argument6 },
            { ActivityDelegate.Argument7Name, argument7 },
            { ActivityDelegate.Argument8Name, argument8 },
            { ActivityDelegate.Argument9Name, argument9 },
            { ActivityDelegate.Argument10Name, argument10 },
            { ActivityDelegate.Argument11Name, argument11 },
            { ActivityDelegate.Argument12Name, argument12 },
            { ActivityDelegate.Argument13Name, argument13 },
            { ActivityDelegate.Argument14Name, argument14 },
            { ActivityDelegate.Argument15Name, argument15 },
            { ActivityDelegate.Argument16Name, argument16 },
        };

        return InternalScheduleDelegate(activityFunc, inputParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefaultParametersShouldNotBeUsed, Justification = "Temporary suppression - to be addressed by DCR 127467")]
    public ActivityInstance ScheduleDelegate(ActivityDelegate activityDelegate, IDictionary<string, object> inputParameters,
        DelegateCompletionCallback onCompleted = null, FaultCallback onFaulted = null)
    {
        ThrowIfDisposed();

        ActivityInstance parent = CurrentInstance;

        if (activityDelegate == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityDelegate));
        }

        if (onCompleted != null && !CallbackWrapper.IsValidCallback(onCompleted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onCompleted), SR.InvalidExecutionCallback(onCompleted, parent.Activity.ToString()));
        }

        if (onFaulted != null && !CallbackWrapper.IsValidCallback(onFaulted, parent))
        {
            throw FxTrace.Exception.Argument(nameof(onFaulted), SR.InvalidExecutionCallback(onFaulted, parent.Activity.ToString()));
        }

        // Check if the inputParameters collection matches the expected inputs for activityDelegate
        IEnumerable<RuntimeDelegateArgument> expectedParameters = activityDelegate.RuntimeDelegateArguments.Where(p => ArgumentDirectionHelper.IsIn(p.Direction));
        int expectedParameterCount = expectedParameters.Count();
        if ((inputParameters == null && expectedParameterCount > 0) ||
            (inputParameters != null && inputParameters.Count != expectedParameterCount))
        {
            throw FxTrace.Exception.Argument(nameof(inputParameters), SR.InputParametersCountMismatch(inputParameters == null ? 0 : inputParameters.Count, expectedParameterCount));
        }
        else if (expectedParameterCount > 0)
        {
            foreach (RuntimeDelegateArgument expectedParameter in expectedParameters)
            {
                string parameterName = expectedParameter.Name;
                if (inputParameters.TryGetValue(parameterName, out object inputParameterValue))
                {
                    if (!TypeHelper.AreTypesCompatible(inputParameterValue, expectedParameter.Type))
                    {
                        throw FxTrace.Exception.Argument(nameof(inputParameters), SR.InputParametersTypeMismatch(expectedParameter.Type, parameterName));
                    }
                }
                else
                {
                    throw FxTrace.Exception.Argument(nameof(inputParameters), SR.InputParametersMissing(expectedParameter.Name));
                }
            }
        }

        return InternalScheduleDelegate(activityDelegate, inputParameters ?? ActivityUtilities.EmptyParameters,
            ActivityUtilities.CreateCompletionBookmark(onCompleted, parent),
            ActivityUtilities.CreateFaultBookmark(onFaulted, parent));
    }

    private ActivityInstance InternalScheduleDelegate(ActivityDelegate activityDelegate, IDictionary<string, object> inputParameters, CompletionBookmark completionBookmark, FaultBookmark faultBookmark)
    {
        ActivityInstance parent = CurrentInstance;

        if (activityDelegate.Handler != null)
        {
            Activity activity = activityDelegate.Handler;

            if (!activity.IsMetadataCached || activity.CacheId != parent.Activity.CacheId)
            {
                throw FxTrace.Exception.Argument("activity", SR.ActivityNotPartOfThisTree(activity.DisplayName, parent.Activity.DisplayName));
            }
        }

        if (activityDelegate.Owner == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityDelegateOwnerMissing(activityDelegate)));
        }

        if (!activityDelegate.CanBeScheduledBy(parent.Activity))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CanOnlyScheduleDirectChildren(parent.Activity.DisplayName, activityDelegate.DisplayName, activityDelegate.Owner.DisplayName)));
        }

        if (parent.WaitingForTransactionContext)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotScheduleChildrenWhileEnteringIsolation));
        }


        /* Unmerged change from project 'System.Activities (net6.0-windows)'
        Before:
                ActivityInstance declaringActivityInstance = FindDeclaringActivityInstance(CurrentInstance, activityDelegate.Owner);
        After:
                ActivityInstance declaringActivityInstance = NativeActivityContext.FindDeclaringActivityInstance(CurrentInstance, activityDelegate.Owner);
        */
        ActivityInstance declaringActivityInstance = FindDeclaringActivityInstance(CurrentInstance, activityDelegate.Owner);

        if (parent.IsPerformingDefaultCancelation)
        {
            parent.MarkCanceled();
            return ActivityInstance.CreateCanceledInstance(activityDelegate.Handler);
        }

        // Activity delegates execute in the environment of the declaring activity and not the invoking activity.
        return _executor.ScheduleDelegate(activityDelegate, inputParameters, parent, declaringActivityInstance.Environment, completionBookmark, faultBookmark);
    }

    internal void EnterNoPersist(NoPersistHandle handle)
    {
        ThrowIfDisposed();

        ExecutionProperties properties = GetExecutionProperties(handle);

        NoPersistProperty property = (NoPersistProperty)properties.FindAtCurrentScope(NoPersistProperty.Name);

        if (property == null)
        {
            property = _executor.CreateNoPersistProperty();
            properties.Add(NoPersistProperty.Name, property, true, false);
        }

        property.Enter();
    }

    private ExecutionProperties GetExecutionProperties(Handle handle)
    {
        Fx.Assert(handle != null, "caller must verify non-null handle");
        if (handle.Owner == CurrentInstance)
        {
            return Properties;
        }
        else
        {
            if (handle.Owner == null)
            {
                Fx.Assert(_executor.RootPropertyManager != null, "should only have a null owner for host-declared properties");
                // null owner means we have a root property. Use the propertyManager from the ActivityExecutor
                return new ExecutionProperties(this, null, _executor.RootPropertyManager);
            }
            else
            {
                return new ExecutionProperties(this, handle.Owner, handle.Owner.PropertyManager);
            }
        }
    }

    internal void ExitNoPersist(NoPersistHandle handle)
    {
        ThrowIfDisposed();

        ExecutionProperties properties = GetExecutionProperties(handle);

        NoPersistProperty property = (NoPersistProperty)properties.FindAtCurrentScope(NoPersistProperty.Name);

        if (property == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnmatchedNoPersistExit));
        }

        if (property.Exit())
        {
            properties.Remove(NoPersistProperty.Name, true);
        }
    }

    internal void RequestTransactionContext(bool isRequires, RuntimeTransactionHandle handle, Action<NativeActivityTransactionContext, object> callback, object state)
        => _executor.RequestTransactionContext(CurrentInstance, isRequires, handle, callback, state);

    internal void CompleteTransaction(RuntimeTransactionHandle handle, BookmarkCallback callback)
    {
        if (callback != null)
        {
            ThrowIfCanInduceIdleNotSet();
        }
        _executor.CompleteTransaction(handle, callback, CurrentInstance);
    }

    internal void RequestPersist(BookmarkCallback onPersistComplete)
    {
        Fx.Assert(!IsDisposed, "We shouldn't call this on a disposed object.");
        Fx.Assert(onPersistComplete != null, "We must have a persist complete callback.");

        Bookmark onPersistBookmark = CreateBookmark(onPersistComplete);
        _executor.RequestPersist(onPersistBookmark, CurrentInstance);
    }

    private static ActivityInstance FindDeclaringActivityInstance(ActivityInstance startingInstance, Activity activityToMatch)
    {
        Fx.Assert(startingInstance != null, "Starting instance should not be null.");

        ActivityInstance currentActivityInstance = startingInstance;
        while (currentActivityInstance != null)
        {
            if (ReferenceEquals(currentActivityInstance.Activity, activityToMatch))
            {
                return currentActivityInstance;
            }
            else
            {
                currentActivityInstance = currentActivityInstance.Parent;
            }
        }

        return null;
    }

    private void ThrowIfCanInduceIdleNotSet()
    {
        Activity associatedActivity = Activity;
        if (!associatedActivity.InternalCanInduceIdle)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CanInduceIdleNotSpecified(associatedActivity.GetType().FullName)));
        }
    }
}
