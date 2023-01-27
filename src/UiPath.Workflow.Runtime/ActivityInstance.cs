// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Activities;
using Internals;
using Runtime;
using Tracking;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

[DataContract(Name = XD.ActivityInstance.Name, Namespace = XD.Runtime.Namespace)]
[Fx.Tag.XamlVisible(false)]
public sealed class ActivityInstance
#if DYNAMICUPDATE
    : ActivityInstanceMap.IActivityReferenceWithEnvironment
#else
    : ActivityInstanceMap.IActivityReference
#endif
{
    private Activity _activity;
    private ChildList _childList;
    private ReadOnlyCollection<ActivityInstance> _childCache;
    private CompletionBookmark _completionBookmark;
    private ActivityInstanceMap _instanceMap;
    private ActivityInstance _parent;
    private string _ownerName;
    private int _busyCount;
    private ExtendedData _extendedData;

    // most activities will have a symbol (either variable or argument, so optimize for that case)
    private bool _noSymbols;
    private ActivityInstanceState _state;
    private bool _isCancellationRequested;
    private bool _performingDefaultCancelation;
    private Substate _substate;
    private long _id;
    private bool _initializationIncomplete;

    // This is serialized through the SerializedEnvironment property
    private LocationEnvironment _environment;
    private ExecutionPropertyManager _propertyManager;

    internal ActivityInstance() { }

    internal ActivityInstance(Activity activity)
    {
        _activity = activity;
        _state = ActivityInstanceState.Executing;
        _substate = Substate.Created;

        ImplementationVersion = activity.ImplementationVersion;
    }

    /// <summary>
    /// The values of the out arguments.
    /// </summary>
    /// <returns>null when there is nothing to return</returns>
    public Dictionary<string, object> GetOutputs()
    {
        Fx.Assert(ActivityUtilities.IsCompletedState(State), "We should only gather outputs when in a completed state.");
        Fx.Assert(_environment != null, "We should have set the root environment");
        // We only gather outputs for Closed - not for canceled or faulted
        if (State != ActivityInstanceState.Closed)
        {
            return null;
        }
        Dictionary<string, object> outputs = null;
        foreach (var argument in Activity.RuntimeArguments)
        {
            if (!ArgumentDirectionHelper.IsOut(argument.Direction))
            {
                continue;
            }
            outputs ??= new();
            outputs.Add(argument.Name, _environment.GetValue(argument));
        }
        return outputs;
    }

    public Activity Activity
    {
        get => _activity;

        internal set
        {
            Fx.Assert(value != null || _state == ActivityInstanceState.Closed, "");
            _activity = value;
        }
    }

    Activity ActivityInstanceMap.IActivityReference.Activity => Activity;

    internal Substate SubState => _substate;

    [DataMember(EmitDefaultValue = false)]
    internal LocationEnvironment SerializedEnvironment
    {
        get
        {
            if (IsCompleted)
            {
                return null;
            }
            else
            {
                return _environment;
            }
        }
        set
        {
            Fx.Assert(value != null, "We should never get null here.");

            _environment = value;
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "busyCount")]
    internal int SerializedBusyCount
    {
        get => _busyCount;
        set => _busyCount = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "extendedData")]
    internal ExtendedData SerializedExtendedData
    {
        get => _extendedData;
        set => _extendedData = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "noSymbols")]
    internal bool SerializedNoSymbols
    {
        get => _noSymbols;
        set => _noSymbols = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "state")]
    internal ActivityInstanceState SerializedState
    {
        get => _state;
        set => _state = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "isCancellationRequested")]
    internal bool SerializedIsCancellationRequested
    {
        get => _isCancellationRequested;
        set => _isCancellationRequested = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "performingDefaultCancelation")]
    internal bool SerializedPerformingDefaultCancelation
    {
        get => _performingDefaultCancelation;
        set => _performingDefaultCancelation = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "substate")]
    internal Substate SerializedSubstate
    {
        get => _substate;
        set => _substate = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "id")]
    internal long SerializedId
    {
        get => _id;
        set => _id = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "initializationIncomplete")]
    internal bool SerializedInitializationIncomplete
    {
        get => _initializationIncomplete;
        set => _initializationIncomplete = value;
    }

    internal LocationEnvironment Environment
    {
        get
        {
            Fx.Assert(_environment != null, "There should always be an environment");
            return _environment;
        }
    }

    internal ActivityInstanceMap InstanceMap => _instanceMap;

    public bool IsCompleted => ActivityUtilities.IsCompletedState(State);

    public ActivityInstanceState State => _state;

    internal bool IsCancellationRequested
    {
        get => _isCancellationRequested;
        set
        {
            // This is set at the time of scheduling the cancelation work item

            Fx.Assert(!_isCancellationRequested, "We should not set this if we have already requested cancel.");
            Fx.Assert(value != false, "We should only set this to true.");

            _isCancellationRequested = value;
        }
    }

    internal bool IsPerformingDefaultCancelation => _performingDefaultCancelation;

    public string Id => _id.ToString(CultureInfo.InvariantCulture);

    internal long InternalId => _id;

    internal bool IsEnvironmentOwner => !_noSymbols;

    internal bool IsResolvingArguments => _substate == Substate.ResolvingArguments;

    internal bool HasNotExecuted => (_substate & Substate.PreExecuting) != 0;

    internal bool HasPendingWork
    {
        get
        {
            if (HasChildren)
            {
                return true;
            }

            // check if we have pending bookmarks or outstanding OperationControlContexts/WorkItems
            if (_busyCount > 0)
            {
                return true;
            }

            return false;
        }
    }

    internal bool OnlyHasOutstandingBookmarks
    {
        get
        {
            // If our whole busy count is because of blocking bookmarks then
            // we should return true
            return !HasChildren && _extendedData != null && (_extendedData.BlockingBookmarkCount == _busyCount);
        }
    }

    internal ActivityInstance Parent => _parent;

    internal bool WaitingForTransactionContext
    {
        get
        {
            if (_extendedData == null)
            {
                return false;
            }
            else
            {
                return _extendedData.WaitingForTransactionContext;
            }
        }
        set
        {
            EnsureExtendedData();

            _extendedData.WaitingForTransactionContext = value;
        }
    }

    [DataMember(EmitDefaultValue = false)]
    internal CompletionBookmark CompletionBookmark
    {
        get => _completionBookmark;

        set => _completionBookmark = value;
    }

    internal FaultBookmark FaultBookmark
    {
        get
        {
            if (_extendedData == null)
            {
                return null;
            }

            return _extendedData.FaultBookmark;
        }

        set
        {
            Fx.Assert(value != null || (_extendedData == null || _extendedData.FaultBookmark == null), "cannot go from non-null to null");
            if (value != null)
            {
                EnsureExtendedData();
                _extendedData.FaultBookmark = value;
            }
        }
    }

    internal bool HasChildren => (_childList != null && _childList.Count > 0);

    internal ExecutionPropertyManager PropertyManager
    {
        get => _propertyManager;
        set => _propertyManager = value;
    }

    internal WorkflowDataContext DataContext
    {
        get
        {
            if (_extendedData != null)
            {
                return _extendedData.DataContext;
            }
            return null;
        }
        set
        {
            EnsureExtendedData();
            _extendedData.DataContext = value;
        }
    }

    internal object CompiledDataContexts { get; set; }

    internal object CompiledDataContextsForImplementation { get; set; }

    internal bool HasActivityReferences => _extendedData != null && _extendedData.HasActivityReferences;

    [DataMember(Name = XD.ActivityInstance.PropertyManager, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
    internal ExecutionPropertyManager SerializedPropertyManager
    {
        get
        {
            if (_propertyManager == null || !_propertyManager.ShouldSerialize(this))
            {
                return null;
            }
            else
            {
                return _propertyManager;
            }
        }
        set
        {
            Fx.Assert(value != null, "We don't emit the default value so this should never be null.");
            _propertyManager = value;
        }
    }

    [DataMember(Name = XD.ActivityInstance.Children, EmitDefaultValue = false)]
    internal ChildList SerializedChildren
    {
        get
        {
            if (HasChildren)
            {
                _childList.Compress();
                return _childList;
            }

            return null;
        }

        set
        {
            Fx.Assert(value != null, "value from Serialization should not be null");
            _childList = value;
        }
    }

    [DataMember(Name = XD.ActivityInstance.Owner, EmitDefaultValue = false)]
    internal string OwnerName
    {
        get
        {
            if (_ownerName == null)
            {
                _ownerName = Activity.GetType().Name;
            }
            return _ownerName;
        }
        set
        {
            Fx.Assert(value != null, "value from Serialization should not be null");
            _ownerName = value;
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public Version ImplementationVersion { get; internal set; }

    internal static ActivityInstance CreateCompletedInstance(Activity activity)
    {
        ActivityInstance instance = new(activity)
        {
            _state = ActivityInstanceState.Closed
        };

        return instance;
    }

    internal static ActivityInstance CreateCanceledInstance(Activity activity)
    {
        ActivityInstance instance = new(activity)
        {
            _state = ActivityInstanceState.Canceled
        };

        return instance;
    }

    internal ReadOnlyCollection<ActivityInstance> GetChildren()
    {
        if (!HasChildren)
        {
            return ChildList.Empty;
        }

        if (_childCache == null)
        {
            _childCache = _childList.AsReadOnly();
        }
        return _childCache;
    }

    internal HybridCollection<ActivityInstance> GetRawChildren() => _childList;

    private void EnsureExtendedData() => _extendedData ??= new ExtendedData();

    // Busy Count includes the following:
    //   1. Active OperationControlContexts.
    //   2. Active work items.
    //   3. Blocking bookmarks.
    internal void IncrementBusyCount() => _busyCount++;

    internal void DecrementBusyCount()
    {
        Fx.Assert(_busyCount > 0, "something went wrong with our bookkeeping");
        _busyCount--;
    }

    internal void DecrementBusyCount(int amount)
    {
        Fx.Assert(_busyCount >= amount, "something went wrong with our bookkeeping");
        _busyCount -= amount;
    }

    internal void AddActivityReference(ActivityInstanceReference reference)
    {
        EnsureExtendedData();
        _extendedData.AddActivityReference(reference);
    }

    internal void AddBookmark(Bookmark bookmark, BookmarkOptions options)
    {
        bool affectsBusyCount = false;

        if (!BookmarkOptionsHelper.IsNonBlocking(options))
        {
            IncrementBusyCount();
            affectsBusyCount = true;
        }

        EnsureExtendedData();
        _extendedData.AddBookmark(bookmark, affectsBusyCount);
    }

    internal void RemoveBookmark(Bookmark bookmark, BookmarkOptions options)
    {
        bool affectsBusyCount = false;

        if (!BookmarkOptionsHelper.IsNonBlocking(options))
        {
            DecrementBusyCount();
            affectsBusyCount = true;
        }

        Fx.Assert(_extendedData != null, "something went wrong with our bookkeeping");
        _extendedData.RemoveBookmark(bookmark, affectsBusyCount);
    }

    internal void RemoveAllBookmarks(BookmarkScopeManager bookmarkScopeManager, BookmarkManager bookmarkManager)
        => _extendedData?.PurgeBookmarks(bookmarkScopeManager, bookmarkManager, this);

    internal void SetInitializationIncomplete() => _initializationIncomplete = true;

    internal void MarkCanceled()
    {
        Fx.Assert(_substate == Substate.Executing || _substate == Substate.Canceling, "called from an unexpected state");
        _substate = Substate.Canceling;
    }

    private void MarkExecuted() => _substate = Substate.Executing;

    internal void MarkAsComplete(BookmarkScopeManager bookmarkScopeManager, BookmarkManager bookmarkManager)
    {
        if (_extendedData != null)
        {
            _extendedData.PurgeBookmarks(bookmarkScopeManager, bookmarkManager, this);

            if (_extendedData.DataContext != null)
            {
                _extendedData.DataContext.Dispose();
            }
        }

        if (_instanceMap != null)
        {
            _instanceMap.RemoveEntry(this);

            if (HasActivityReferences)
            {
                _extendedData.PurgeActivityReferences(_instanceMap);
            }
        }

        if (Parent != null)
        {
            Parent.RemoveChild(this);
        }
    }

    internal void Abort(ActivityExecutor executor, BookmarkManager bookmarkManager, Exception terminationReason, bool isTerminate)
    {
        // This is a gentle abort where we try to keep the runtime in a
        // usable state.
        AbortEnumerator abortEnumerator = new(this);

        while (abortEnumerator.MoveNext())
        {
            ActivityInstance currentInstance = abortEnumerator.Current;

            if (!currentInstance.HasNotExecuted)
            {
                currentInstance.Activity.InternalAbort(currentInstance, executor, terminationReason);
            }

            if (currentInstance.PropertyManager != null)
            {
                currentInstance.PropertyManager.UnregisterProperties(currentInstance, currentInstance.Activity.MemberOf, true);
            }

            executor.TerminateSpecialExecutionBlocks(currentInstance, terminationReason);

            executor.CancelPendingOperation(currentInstance);

            executor.HandleRootCompletion(currentInstance);

            currentInstance.MarkAsComplete(executor.RawBookmarkScopeManager, bookmarkManager);

            currentInstance._state = ActivityInstanceState.Faulted;

            currentInstance.FinalizeState(executor, false, !isTerminate);
        }
    }

    internal void BaseCancel(NativeActivityContext context)
    {
        // Default cancelation logic starts here, but is also performed in
        // UpdateState and through special completion work items

        Fx.Assert(IsCancellationRequested, "This should be marked to true at this point.");

        _performingDefaultCancelation = true;

        CancelChildren(context);
    }

    internal void CancelChildren(NativeActivityContext context)
    {
        if (HasChildren)
        {
            foreach (ActivityInstance child in GetChildren())
            {
                context.CancelChild(child);
            }
        }
    }

    internal void Cancel(ActivityExecutor executor, BookmarkManager bookmarkManager)
        => Activity.InternalCancel(this, executor, bookmarkManager);

    internal void Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (_initializationIncomplete)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InitializationIncomplete));
        }

        MarkExecuted();
        Activity.InternalExecute(this, executor, bookmarkManager);
    }

    internal void AddChild(ActivityInstance item)
    {
        _childList ??= new ChildList();
        _childList.Add(item);
        _childCache = null;
    }

    internal void RemoveChild(ActivityInstance item)
    {
        Fx.Assert(_childList != null, "");
        _childList.Remove(item, true);
        _childCache = null;
    }

    // called by ActivityUtilities tree-walk
    internal void AppendChildren(ActivityUtilities.TreeProcessingList nextInstanceList, ref Queue<IList<ActivityInstance>> instancesRemaining)
    {
        Fx.Assert(HasChildren, "AppendChildren is tuned to only be called when HasChildren is true");
        _childList.AppendChildren(nextInstanceList, ref instancesRemaining);
    }

    // called after deserialization of the workflow instance
    internal void FixupInstance(ActivityInstance parent, ActivityInstanceMap instanceMap, ActivityExecutor executor)
    {
        if (IsCompleted)
        {
            // We hang onto the root instance even after is it complete.  We skip the fixups
            // for a completed root.
            Fx.Assert(parent == null, "This should only happen to root instances.");

            return;
        }

        if (Activity == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityInstanceFixupFailed));
        }

        _parent = parent;
        _instanceMap = instanceMap;

        if (PropertyManager != null)
        {
            PropertyManager.OnDeserialized(this, parent, Activity.MemberOf, executor);
        }
        else if (_parent != null)
        {
            // The current property manager is null here
            PropertyManager = _parent.PropertyManager;
        }
        else
        {
            PropertyManager = executor.RootPropertyManager;
        }

        if (!_noSymbols)
        {
            _environment.OnDeserialized(executor, this);
        }
    }

    internal bool TryFixupChildren(ActivityInstanceMap instanceMap, ActivityExecutor executor)
    {
        if (!HasChildren)
        {
            return false;
        }

        _childList.FixupList(this, instanceMap, executor);
        return true;
    }

    internal void FillInstanceMap(ActivityInstanceMap instanceMap)
    {
        if (IsCompleted)
        {
            // We don't bother adding completed roots to the map
            return;
        }

        Fx.Assert(_instanceMap == null, "We should never call this unless the current map is null.");
        Fx.Assert(Parent == null, "Can only generate a map from a root instance.");

        _instanceMap = instanceMap;
        ActivityUtilities.ProcessActivityInstanceTree(this, null, new Func<ActivityInstance, ActivityExecutor, bool>(GenerateInstanceMapCallback));
    }

    private bool GenerateInstanceMapCallback(ActivityInstance instance, ActivityExecutor executor)
    {
        _instanceMap.AddEntry(instance);
        instance._instanceMap = _instanceMap;

        if (instance.HasActivityReferences)
        {
            instance._extendedData.FillInstanceMap(instance._instanceMap);
        }

        return true;
    }

    internal bool Initialize(ActivityInstance parent, ActivityInstanceMap instanceMap, LocationEnvironment parentEnvironment, long instanceId, ActivityExecutor executor, int delegateParameterCount = 0)
    {
        _parent = parent;
        _instanceMap = instanceMap;
        _id = instanceId;

        if (_instanceMap != null)
        {
            _instanceMap.AddEntry(this);
        }

        // propagate necessary information from our parent
        if (_parent != null)
        {
            if (_parent.PropertyManager != null)
            {
                PropertyManager = _parent.PropertyManager;
            }

            parentEnvironment ??= _parent.Environment;
        }

        int symbolCount = Activity.SymbolCount + delegateParameterCount;

        if (symbolCount == 0)
        {
            if (parentEnvironment == null)
            {
                // We create an environment for a root activity that otherwise would not have one
                // to simplify environment management.
                _environment = new LocationEnvironment(executor, Activity);
            }
            else
            {
                _noSymbols = true;
                _environment = parentEnvironment;
            }

            // We don't set Initialized here since the tracking/tracing would be too early
            return false;
        }
        else
        {
            _environment = new LocationEnvironment(executor, Activity, parentEnvironment, symbolCount);
            _substate = Substate.ResolvingArguments;
            return true;
        }
    }

    internal void ResolveNewArgumentsDuringDynamicUpdate(ActivityExecutor executor, IList<int> dynamicUpdateArgumentIndexes)
    {
        Fx.Assert(!_noSymbols, "Can only resolve arguments if we created an environment");
        Fx.Assert(_substate == Substate.Executing, "Dynamically added arguments are to be resolved only in Substate.Executing.");

        if (Activity.SkipArgumentResolution)
        {
            return;
        }

        IList<RuntimeArgument> runtimeArguments = Activity.RuntimeArguments;

        for (int i = 0; i < dynamicUpdateArgumentIndexes.Count; i++)
        {
            RuntimeArgument argument = runtimeArguments[dynamicUpdateArgumentIndexes[i]];
            Fx.Assert(Environment.GetSpecificLocation(argument.Id) == null, "This is a newly added argument so the location should be null");

            InternalTryPopulateArgumentValueOrScheduleExpression(argument, -1, executor, null, null, true);
        }
    }

    private bool InternalTryPopulateArgumentValueOrScheduleExpression(RuntimeArgument argument, int nextArgumentIndex, ActivityExecutor executor, IDictionary<string, object> argumentValueOverrides, Location resultLocation, bool isDynamicUpdate)
    {
        object overrideValue = null;
        argumentValueOverrides?.TryGetValue(argument.Name, out overrideValue);

        if (argument.TryPopulateValue(_environment, this, executor, overrideValue, resultLocation, isDynamicUpdate))
        {
            return true;
        }

        ResolveNextArgumentWorkItem workItem = null;
        Location location = _environment.GetSpecificLocation(argument.Id);

        if (isDynamicUpdate)
        {
            //1. Check if this argument has a temporary location that needs to be collapsed
            if (location.TemporaryResolutionEnvironment != null)
            {
                // 2. Add a workitem to collapse the temporary location
                executor.ScheduleItem(new CollapseTemporaryResolutionLocationWorkItem(location, this));
            }
        }
        else
        {
            //1. Check if there are more arguments to process
            nextArgumentIndex++;

            // 2. Add a workitem to resume argument resolution when
            // work related to 3 below either completes or it hits an async point.           
            int totalArgumentCount = Activity.RuntimeArguments.Count;

            if (nextArgumentIndex < totalArgumentCount)
            {
                workItem = executor.ResolveNextArgumentWorkItemPool.Acquire();
                workItem.Initialize(this, nextArgumentIndex, argumentValueOverrides, resultLocation);
            }
        }

        // 3. Schedule the argument expression.
        executor.ScheduleExpression(argument.BoundArgument.Expression, this, Environment, location, workItem);

        return false;
    }

    // return true if arguments were resolved synchronously
    internal bool ResolveArguments(ActivityExecutor executor, IDictionary<string, object> argumentValueOverrides, Location resultLocation, int startIndex = 0)
    {
        Fx.Assert(!_noSymbols, "Can only resolve arguments if we created an environment");
        Fx.Assert(_substate == Substate.ResolvingArguments, "Invalid sub-state machine");

        bool completedSynchronously = true;

        if (Activity.IsFastPath)
        {
            // We still need to resolve the result argument
            Fx.Assert(argumentValueOverrides == null, "We shouldn't have any overrides.");
            Fx.Assert(((ActivityWithResult)Activity).ResultRuntimeArgument != null, "We should have a result argument");

            RuntimeArgument argument = ((ActivityWithResult)Activity).ResultRuntimeArgument;

            if (!argument.TryPopulateValue(_environment, this, executor, null, resultLocation, false))
            {
                completedSynchronously = false;

                Location location = _environment.GetSpecificLocation(argument.Id);
                executor.ScheduleExpression(argument.BoundArgument.Expression, this, Environment, location, null);
            }
        }
        else if (!Activity.SkipArgumentResolution)
        {
            IList<RuntimeArgument> runtimeArguments = Activity.RuntimeArguments;

            int argumentCount = runtimeArguments.Count;

            if (argumentCount > 0)
            {
                for (int i = startIndex; i < argumentCount; i++)
                {
                    RuntimeArgument argument = runtimeArguments[i];

                    if (!InternalTryPopulateArgumentValueOrScheduleExpression(argument, i, executor, argumentValueOverrides, resultLocation, false))
                    {
                        completedSynchronously = false;
                        break;
                    }
                }
            }
        }

        if (completedSynchronously && startIndex == 0)
        {
            // We only move our state machine forward if this
            // is the first call to ResolveArguments (startIndex
            // == 0).  Otherwise, a call to UpdateState will
            // cause the substate switch (as well as a call to
            // CollapseTemporaryResolutionLocations).
            _substate = Substate.ResolvingVariables;
        }

        return completedSynchronously;
    }

    internal void ResolveNewVariableDefaultsDuringDynamicUpdate(ActivityExecutor executor, IList<int> dynamicUpdateVariableIndexes, bool forImplementation)
    {
        Fx.Assert(!_noSymbols, "Can only resolve variable default if we created an environment");
        Fx.Assert(_substate == Substate.Executing, "Dynamically added variable default expressions are to be resolved only in Substate.Executing.");

        IList<Variable> runtimeVariables;
        if (forImplementation)
        {
            runtimeVariables = Activity.ImplementationVariables;
        }
        else
        {
            runtimeVariables = Activity.RuntimeVariables;
        }

        for (int i = 0; i < dynamicUpdateVariableIndexes.Count; i++)
        {
            Variable newVariable = runtimeVariables[dynamicUpdateVariableIndexes[i]];
            if (newVariable.Default != null)
            {
                EnqueueVariableDefault(executor, newVariable, null);
            }
        }
    }

    internal bool ResolveVariables(ActivityExecutor executor)
    {
        Fx.Assert(!_noSymbols, "can only resolve variables if we created an environment");
        Fx.Assert(_substate == Substate.ResolvingVariables, "invalid sub-state machine");

        _substate = Substate.ResolvingVariables;
        bool completedSynchronously = true;

        IList<Variable> implementationVariables = Activity.ImplementationVariables;
        IList<Variable> runtimeVariables = Activity.RuntimeVariables;

        int implementationVariableCount = implementationVariables.Count;
        int runtimeVariableCount = runtimeVariables.Count;

        if (implementationVariableCount > 0 || runtimeVariableCount > 0)
        {
            for (int i = 0; i < implementationVariableCount; i++)
            {
                implementationVariables[i].DeclareLocation(executor, this);
            }

            for (int i = 0; i < runtimeVariableCount; i++)
            {
                runtimeVariables[i].DeclareLocation(executor, this);
            }

            for (int i = 0; i < implementationVariableCount; i++)
            {
                completedSynchronously &= ResolveVariable(implementationVariables[i], executor);
            }

            for (int i = 0; i < runtimeVariableCount; i++)
            {
                completedSynchronously &= ResolveVariable(runtimeVariables[i], executor);
            }
        }

        return completedSynchronously;
    }

    // returns true if completed synchronously
    private bool ResolveVariable(Variable variable, ActivityExecutor executor)
    {
        bool completedSynchronously = true;
        if (variable.Default != null)
        {
            Location variableLocation = Environment.GetSpecificLocation(variable.Id);

            if (variable.Default.UseOldFastPath)
            {
                variable.PopulateDefault(executor, this, variableLocation);
            }
            else
            {
                EnqueueVariableDefault(executor, variable, variableLocation);
                completedSynchronously = false;
            }
        }

        return completedSynchronously;
    }

    private void EnqueueVariableDefault(ActivityExecutor executor, Variable variable, Location variableLocation)
    {
        // Incomplete initialization detection logic relies on the fact that we
        // don't specify a completion callback.  If this changes we need to modify
        // callers of SetInitializationIncomplete().
        Fx.Assert(variable.Default != null, "If we've gone async we must have a default");
        if (variableLocation == null)
        {
            variableLocation = _environment.GetSpecificLocation(variable.Id);
        }
        variable.SetIsWaitingOnDefaultValue(variableLocation);
        executor.ScheduleExpression(variable.Default, this, _environment, variableLocation, null);
    }

    void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap)
    {
        if (activity.GetType().Name != OwnerName)
        {
            throw FxTrace.Exception.AsError(
                new ValidationException(SR.ActivityTypeMismatch(activity.DisplayName, OwnerName)));
        }

        if (activity.ImplementationVersion != ImplementationVersion)
        {
            throw FxTrace.Exception.AsError(new VersionMismatchException(SR.ImplementationVersionMismatch(ImplementationVersion, activity.ImplementationVersion, activity)));
        }

        Activity = activity;
    }

    // Returns true if the activity completed
    internal bool UpdateState(ActivityExecutor executor)
    {
        bool activityCompleted = false;

        if (HasNotExecuted)
        {
            if (IsCancellationRequested) // need to cancel any in-flight resolutions and bail
            {
                if (HasChildren)
                {
                    foreach (ActivityInstance child in GetChildren())
                    {
                        Fx.Assert(child.State == ActivityInstanceState.Executing, "should only have children if they're still executing");
                        executor.CancelActivity(child);
                    }
                }
                else
                {
                    SetCanceled();
                    activityCompleted = true;
                }
            }
            else if (!HasPendingWork)
            {
                bool scheduleBody = false;

                if (_substate == Substate.ResolvingArguments)
                {
                    // if we've had asynchronous resolution of Locations (Out/InOut Arguments), resolve them now
                    Environment.CollapseTemporaryResolutionLocations();

                    _substate = Substate.ResolvingVariables;
                    scheduleBody = ResolveVariables(executor);
                }
                else if (_substate == Substate.ResolvingVariables)
                {
                    scheduleBody = true;
                }

                if (scheduleBody)
                {
                    executor.ScheduleBody(this, false, null, null);
                }
            }

            Fx.Assert(HasPendingWork || activityCompleted, "should have scheduled work pending if we're not complete");
        }
        else if (!HasPendingWork)
        {
            if (!executor.IsCompletingTransaction(this))
            {
                activityCompleted = true;
                if (_substate == Substate.Canceling)
                {
                    SetCanceled();
                }
                else
                {
                    SetClosed();
                }
            }
        }
        else if (_performingDefaultCancelation)
        {
            if (OnlyHasOutstandingBookmarks)
            {
                RemoveAllBookmarks(executor.RawBookmarkScopeManager, executor.RawBookmarkManager);
                MarkCanceled();

                Fx.Assert(!HasPendingWork, "Shouldn't have pending work here.");

                SetCanceled();
                activityCompleted = true;
            }
        }

        return activityCompleted;
    }

    private void TryCancelParent()
    {
        if (_parent != null && _parent.IsPerformingDefaultCancelation)
        {
            _parent.MarkCanceled();
        }
    }

    internal void SetInitializedSubstate(ActivityExecutor executor)
    {
        Fx.Assert(_substate != Substate.Initialized, "SetInitializedSubstate called when substate is already Initialized.");
        _substate = Substate.Initialized;
        if (executor.ShouldTrackActivityStateRecordsExecutingState)
        {
            if (executor.ShouldTrackActivity(Activity.DisplayName))
            {
                executor.AddTrackingRecord(new ActivityStateRecord(executor.WorkflowInstanceId, this, _state));
            }
        }

        if (TD.InArgumentBoundIsEnabled())
        {
            int runtimeArgumentsCount = Activity.RuntimeArguments.Count;
            if (runtimeArgumentsCount > 0)
            {
                for (int i = 0; i < runtimeArgumentsCount; i++)
                {
                    RuntimeArgument argument = Activity.RuntimeArguments[i];

                    if (ArgumentDirectionHelper.IsIn(argument.Direction))
                    {
                        if (_environment.TryGetLocation(argument.Id, Activity, out Location location))
                        {
                            string argumentValue;
                            if (location.Value == null)
                            {
                                argumentValue = "<Null>";
                            }
                            else
                            {
                                argumentValue = "'" + location.Value.ToString() + "'";
                            }

                            TD.InArgumentBound(argument.Name, Activity.GetType().ToString(), Activity.DisplayName, Id, argumentValue);
                        }
                    }
                }
            }
        }
    }

    internal void FinalizeState(ActivityExecutor executor, bool faultActivity, bool skipTracking = false)
    {
        if (faultActivity)
        {
            TryCancelParent();

            // We can override previous completion states with this
            _state = ActivityInstanceState.Faulted;
        }

        Fx.Assert(_state != ActivityInstanceState.Executing, "We must be in a completed state at this point.");

        if (_state == ActivityInstanceState.Closed)
        {
            if (executor.ShouldTrackActivityStateRecordsClosedState && !skipTracking)
            {
                if (executor.ShouldTrackActivity(Activity.DisplayName))
                {
                    executor.AddTrackingRecord(new ActivityStateRecord(executor.WorkflowInstanceId, this, _state));
                }
            }
        }
        else
        {
            if (executor.ShouldTrackActivityStateRecords && !skipTracking)
            {
                executor.AddTrackingRecord(new ActivityStateRecord(executor.WorkflowInstanceId, this, _state));
            }
        }

        if (TD.ActivityCompletedIsEnabled())
        {
            TD.ActivityCompleted(Activity.GetType().ToString(), Activity.DisplayName, Id, State.GetStateName());
        }

    }

    private void SetCanceled()
    {
        Fx.Assert(!IsCompleted, "Should not be completed if we are changing the state.");

        TryCancelParent();

        _state = ActivityInstanceState.Canceled;
    }

    private void SetClosed()
    {
        Fx.Assert(!IsCompleted, "Should not be completed if we are changing the state.");

        _state = ActivityInstanceState.Closed;
    }

#if DYNAMICUPDATE
    private static void UpdateLocationEnvironmentHierarchy(LocationEnvironment oldParentEnvironment, LocationEnvironment newEnvironment, ActivityInstance currentInstance)
    {
        Func<ActivityInstance, ActivityExecutor, bool> processInstanceCallback = delegate(ActivityInstance instance, ActivityExecutor executor)
        {
            if (instance == currentInstance)
            {
                return true;
            }

            if (instance.IsEnvironmentOwner)
            {
                if (instance._environment.Parent == oldParentEnvironment)
                {
                    // overwrite its parent with newEnvironment
                    instance._environment.Parent = newEnvironment;
                }

                // We do not need to process children instances beyond this point.
                return false;
            }

            if (instance._environment == oldParentEnvironment)
            {
                // this instance now points to newEnvironment
                instance._environment = newEnvironment;
            }

            return true;
        };

        ActivityUtilities.ProcessActivityInstanceTree(currentInstance, null, processInstanceCallback);
    }

    void ActivityInstanceMap.IActivityReferenceWithEnvironment.UpdateEnvironment(EnvironmentUpdateMap map, Activity activity)
    {            
        Fx.Assert(substate != Substate.ResolvingVariables, "We must have already performed the same validations in advance.");
        Fx.Assert(substate != Substate.ResolvingArguments, "We must have already performed the same validations in advance.");

        if (noSymbols)
        {
            // create a new LocationReference and this ActivityInstance becomes the owner of the created environment.
            LocationEnvironment oldParentEnvironment = environment;

            Fx.Assert(oldParentEnvironment != null, "environment must never be null.");

            environment = new LocationEnvironment(oldParentEnvironment, map.NewArgumentCount + map.NewVariableCount + map.NewPrivateVariableCount + map.RuntimeDelegateArgumentCount);
            noSymbols = false;

            // traverse the activity instance chain.
            // Update all its non-environment-owning decedent instances to point to the newly created enviroment,
            // and, update all its environment-owning decendent instances to have their environment's parent to point to the newly created environment.
            UpdateLocationEnvironmentHierarchy(oldParentEnvironment, environment, this);
        }

        Environment.Update(map, activity);
    }
#endif

    internal enum Substate : byte
    {
        Executing = 0, // choose the most common persist-time state for the default
        PreExecuting = 0x80, // used for all states prior to "core execution"
        Created = 1 | PreExecuting,
        ResolvingArguments = 2 | PreExecuting,
        // ResolvedArguments = 2,
        ResolvingVariables = 3 | PreExecuting,
        // ResolvedVariables = 3,
        Initialized = 4 | PreExecuting,
        Canceling = 5,
    }

    // data necessary to support non-mainline usage of instances (i.e. creating bookmarks, using transactions)
    [DataContract]
    internal class ExtendedData
    {
        private BookmarkList _bookmarks;
        private ActivityReferenceList _activityReferences;
        private int _blockingBookmarkCount;

        public ExtendedData()
        {
        }

        public int BlockingBookmarkCount
        {
            get => _blockingBookmarkCount;
            private set => _blockingBookmarkCount = value;
        }

        [DataMember(Name = XD.ActivityInstance.WaitingForTransactionContext, EmitDefaultValue = false)]
        public bool WaitingForTransactionContext { get; set; }

        [DataMember(Name = XD.ActivityInstance.FaultBookmark, EmitDefaultValue = false)]
        public FaultBookmark FaultBookmark { get; set; }

        public WorkflowDataContext DataContext { get; set; }

        [DataMember(Name = XD.ActivityInstance.BlockingBookmarkCount, EmitDefaultValue = false)]
        internal int SerializedBlockingBookmarkCount
        {
            get => BlockingBookmarkCount;
            set => BlockingBookmarkCount = value;
        }

        [DataMember(Name = XD.ActivityInstance.Bookmarks, EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
        internal BookmarkList Bookmarks
        {
            get
            {
                if (_bookmarks == null || _bookmarks.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _bookmarks;
                }
            }
            set
            {
                Fx.Assert(value != null, "We don't emit the default value so this should never be null.");
                _bookmarks = value;
            }
        }

        [DataMember(Name = XD.ActivityInstance.ActivityReferences, EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
        internal ActivityReferenceList ActivityReferences
        {
            get
            {
                if (_activityReferences == null || _activityReferences.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _activityReferences;
                }
            }
            set
            {
                Fx.Assert(value != null && value.Count > 0, "We shouldn't emit the default value or empty lists");
                _activityReferences = value;
            }
        }

        public bool HasActivityReferences => _activityReferences != null && _activityReferences.Count > 0;

        public void AddBookmark(Bookmark bookmark, bool affectsBusyCount)
        {
            if (_bookmarks == null)
            {
                _bookmarks = new BookmarkList();
            }

            if (affectsBusyCount)
            {
                BlockingBookmarkCount++;
            }

            _bookmarks.Add(bookmark);
        }

        public void RemoveBookmark(Bookmark bookmark, bool affectsBusyCount)
        {
            Fx.Assert(_bookmarks != null, "The bookmark list should have been initialized if we are trying to remove one.");

            if (affectsBusyCount)
            {
                Fx.Assert(BlockingBookmarkCount > 0, "We should never decrement below zero.");

                BlockingBookmarkCount--;
            }

            _bookmarks.Remove(bookmark);
        }

        public void PurgeBookmarks(BookmarkScopeManager bookmarkScopeManager, BookmarkManager bookmarkManager, ActivityInstance owningInstance)
        {
            if (_bookmarks != null)
            {
                if (_bookmarks.Count > 0)
                {
                    _bookmarks.TransferBookmarks(out Bookmark singleBookmark, out IList<Bookmark> multipleBookmarks);
                    _bookmarks = null;

                    if (bookmarkScopeManager != null)
                    {
                        bookmarkScopeManager.PurgeBookmarks(bookmarkManager, singleBookmark, multipleBookmarks);
                    }
                    else
                    {
                        bookmarkManager.PurgeBookmarks(singleBookmark, multipleBookmarks);
                    }

                    // Clean up the busy count
                    owningInstance.DecrementBusyCount(BlockingBookmarkCount);
                    BlockingBookmarkCount = 0;
                }
            }
        }

        public void AddActivityReference(ActivityInstanceReference reference)
        {
            _activityReferences ??= new ActivityReferenceList();
            _activityReferences.Add(reference);
        }

        public void FillInstanceMap(ActivityInstanceMap instanceMap)
        {
            Fx.Assert(HasActivityReferences, "Must have references to have called ");

            _activityReferences.FillInstanceMap(instanceMap);
        }

        public void PurgeActivityReferences(ActivityInstanceMap instanceMap)
        {
            Fx.Assert(HasActivityReferences, "Must have references to have called ");

            _activityReferences.PurgeActivityReferences(instanceMap);
        }

        [DataContract]
        internal class ActivityReferenceList : HybridCollection<ActivityInstanceReference>
        {
            public ActivityReferenceList() : base() { }

            public void FillInstanceMap(ActivityInstanceMap instanceMap)
            {
                Fx.Assert(Count > 0, "Should only call this when we have items");

                if (SingleItem != null)
                {
                    instanceMap.AddEntry(SingleItem);
                }
                else
                {
                    for (int i = 0; i < MultipleItems.Count; i++)
                    {
                        ActivityInstanceReference reference = MultipleItems[i];

                        instanceMap.AddEntry(reference);
                    }
                }
            }

            public void PurgeActivityReferences(ActivityInstanceMap instanceMap)
            {
                Fx.Assert(Count > 0, "Should only call this when we have items");

                if (SingleItem != null)
                {
                    instanceMap.RemoveEntry(SingleItem);
                }
                else
                {
                    for (int i = 0; i < MultipleItems.Count; i++)
                    {
                        instanceMap.RemoveEntry(MultipleItems[i]);
                    }
                }
            }
        }
    }

    [DataContract]
    internal class ChildList : HybridCollection<ActivityInstance>
    {
        private static ReadOnlyCollection<ActivityInstance> EmptyChildren;

        public ChildList() : base() { }

        public static ReadOnlyCollection<ActivityInstance> Empty
        {
            get
            {
                EmptyChildren ??= new ReadOnlyCollection<ActivityInstance>(Array.Empty<ActivityInstance>());
                return EmptyChildren;
            }
        }

        public void AppendChildren(ActivityUtilities.TreeProcessingList nextInstanceList, ref Queue<IList<ActivityInstance>> instancesRemaining)
        {
            // This is only called if there is at least one item in the list.

            if (SingleItem != null)
            {
                nextInstanceList.Add(SingleItem);
            }
            else if (nextInstanceList.Count == 0)
            {
                nextInstanceList.Set(MultipleItems);
            }
            else
            {
                // Next instance list already has some stuff and we have multiple
                // items.  Let's enqueue them for later processing.

                if (instancesRemaining == null)
                {
                    instancesRemaining = new Queue<IList<ActivityInstance>>();
                }

                instancesRemaining.Enqueue(MultipleItems);
            }
        }

        public void FixupList(ActivityInstance parent, ActivityInstanceMap instanceMap, ActivityExecutor executor)
        {
            if (SingleItem != null)
            {
                SingleItem.FixupInstance(parent, instanceMap, executor);
            }
            else
            {
                for (int i = 0; i < MultipleItems.Count; i++)
                {
                    MultipleItems[i].FixupInstance(parent, instanceMap, executor);
                }
            }
        }
    }

    // Does a depth first walk and uses some knowledge of
    // the abort process to determine which child to visit next
    private class AbortEnumerator : IEnumerator<ActivityInstance>
    {
        private readonly ActivityInstance _root;
        private ActivityInstance _current;
        private bool _initialized;

        public AbortEnumerator(ActivityInstance root) => _root = root;

        public ActivityInstance Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_initialized)
            {
                _current = _root;

                // We start by diving down the tree along the
                // "first child" path
                while (_current.HasChildren)
                {
                    _current = _current.GetChildren()[0];
                }

                _initialized = true;

                return true;
            }
            else
            {
                if (_current == _root)
                {
                    // We're done if we returned all the way to the root last time
                    return false;
                }
                else
                {
                    Fx.Assert(!_current.Parent.GetChildren().Contains(_current), "We should always have removed the current one from the parent's list by now.");

                    _current = _current.Parent;

                    // Dive down the tree of remaining first children
                    while (_current.HasChildren)
                    {
                        _current = _current.GetChildren()[0];
                    }

                    return true;
                }
            }
        }

        public void Reset()
        {
            _current = null;
            _initialized = false;
        }

        public void Dispose()
        {
            // no op
        }
    }
}
