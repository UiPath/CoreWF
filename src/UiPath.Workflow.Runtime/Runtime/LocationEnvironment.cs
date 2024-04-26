// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Runtime;

[DataContract]
internal sealed class LocationEnvironment
#if DYNAMICUPDATE
            : ActivityInstanceMap.IActivityReferenceWithEnvironment
#else
            : ActivityInstanceMap.IActivityReference
#endif
{
    private bool _isDisposed;
    private bool _hasHandles;
    private ActivityExecutor _executor;

    // These two fields should be null unless we're in between calls to Update() and OnDeserialized().
    // Therefore they should never need to serialize.
    private IList<Location> _locationsToUnregister;
    private IList<LocationReference> _locationsToRegister;
    private Location[] _locations;
    private bool _hasMappableLocations;
    private LocationEnvironment _parent;
    private Location _singleLocation;

    // This list keeps track of handles that are created and initialized.
    private List<Handle> _handles;

    // We store refCount - 1 because it is more likely to
    // be zero and skipped by serialization
    private int _referenceCountMinusOne;
    private bool _hasOwnerCompleted;

    internal LocationEnvironment() { }

    // this ctor overload is to be exclusively used by DU
    // for creating a LocationEnvironment for "noSymbols" ActivityInstance
    internal LocationEnvironment(LocationEnvironment parent, int capacity)
        : this(null, null, parent, capacity) { }

    internal LocationEnvironment(ActivityExecutor executor, Activity definition)
    {
        _executor = executor;
        Definition = definition;
    }

    internal LocationEnvironment(ActivityExecutor executor, Activity definition, LocationEnvironment parent, int capacity)
        : this(executor, definition)
    {
        _parent = parent;

        Fx.Assert(capacity > 0, "must have a positive capacity if using this overload");
        if (capacity > 1)
        {
            _locations = new Location[capacity];
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "locations")]
    internal Location[] SerializedLocations
    {
        get => _locations;
        set => _locations = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "hasMappableLocations")]
    internal bool SerializedHasMappableLocations
    {
        get => _hasMappableLocations;
        set => _hasMappableLocations = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "parent")]
    internal LocationEnvironment SerializedParent
    {
        get => _parent;
        set => _parent = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "singleLocation")]
    internal Location SerializedSingleLocation
    {
        get => _singleLocation;
        set => _singleLocation = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "handles")]
    internal List<Handle> SerializedHandles
    {
        get => _handles;
        set => _handles = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "referenceCountMinusOne")]
    internal int SerializedReferenceCountMinusOne
    {
        get => _referenceCountMinusOne;
        set => _referenceCountMinusOne = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "hasOwnerCompleted")]
    internal bool SerializedHasOwnerCompleted
    {
        get => _hasOwnerCompleted;
        set => _hasOwnerCompleted = value;
    }

    internal Activity Definition { get; private set; }

    internal LocationEnvironment Parent
    {
        get => _parent;
        set => _parent = value;
    }

    internal bool HasHandles => _hasHandles;

    private MappableObjectManager MappableObjectManager => _executor.MappableObjectManager;

    internal bool ShouldDispose => _referenceCountMinusOne == -1;

    internal bool HasOwnerCompleted => _hasOwnerCompleted;

    Activity ActivityInstanceMap.IActivityReference.Activity => Definition;

    internal List<Handle> Handles => _handles;

    void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap) => Definition = activity;

#if DYNAMICUPDATE
    void ActivityInstanceMap.IActivityReferenceWithEnvironment.UpdateEnvironment(EnvironmentUpdateMap map, Activity activity)
    {
        // LocationEnvironment.Update() is invoked through this path when this is a seondary root's environment(and in its parent chain) whose owner has already completed.
        this.Update(map, activity);
    }    
#endif

    // Note that the owner should never call this as the first
    // AddReference is assumed
    internal void AddReference() => _referenceCountMinusOne++;

    internal void RemoveReference(bool isOwner)
    {
        if (isOwner)
        {
            _hasOwnerCompleted = true;
        }

        Fx.Assert(_referenceCountMinusOne >= 0, "We must at least have 1 reference (0 for refCountMinusOne)");
        _referenceCountMinusOne--;
    }

    internal void OnDeserialized(ActivityExecutor executor, ActivityInstance handleScope)
    {
        _executor = executor;

        // The instance map Load might have already set the definition to the correct one.
        // If not then we assume the definition is the same as the handle scope.
        if (Definition == null)
        {
            Definition = handleScope.Activity;
        }

        ReinitializeHandles(handleScope);
        RegisterUpdatedLocations(handleScope);
    }

    internal void ReinitializeHandles(ActivityInstance handleScope)
    {
        // Need to reinitialize the handles in the list.
        if (_handles != null)
        {
            int count = _handles.Count;
            for (int i = 0; i < count; i++)
            {
                _handles[i].Reinitialize(handleScope);
                _hasHandles = true;
            }
        }
    }

    internal void Dispose()
    {
        Fx.Assert(ShouldDispose, "We shouldn't be calling Dispose when we have existing references.");
        Fx.Assert(!_hasHandles, "We should have already uninitialized the handles and set our hasHandles variable to false.");
        Fx.Assert(!_isDisposed, "We should not already be disposed.");

        _isDisposed = true;

        CleanupMappedLocations();
    }

    internal void AddHandle(Handle handleToAdd)
    {
        _handles ??= new List<Handle>();
        _handles.Add(handleToAdd);
        _hasHandles = true;
    }

    private void CleanupMappedLocations()
    {
        if (_hasMappableLocations)
        {
            if (_singleLocation != null)
            {
                Fx.Assert(_singleLocation.CanBeMapped, "Can only have mappable locations for a singleton if its mappable.");
                UnregisterLocation(_singleLocation);
            }
            else if (_locations != null)
            {
                for (int i = 0; i < _locations.Length; i++)
                {
                    Location location = _locations[i];

                    if (location.CanBeMapped)
                    {
                        UnregisterLocation(location);
                    }
                }
            }
        }
    }

    internal void UninitializeHandles(ActivityInstance scope)
    {
        if (_hasHandles)
        {
            HandleInitializationContext context = null;

            try
            {
                UninitializeHandles(scope, Definition.RuntimeVariables, ref context);
                UninitializeHandles(scope, Definition.ImplementationVariables, ref context);

                _hasHandles = false;
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
        }
    }

    private void UninitializeHandles(ActivityInstance scope, IList<Variable> variables, ref HandleInitializationContext context)
    {
        for (int i = 0; i < variables.Count; i++)
        {
            Variable variable = variables[i];
            Fx.Assert(variable.Owner == Definition, "We should only be targeting the vairables at this scope.");

            if (variable.IsHandle)
            {
                Location location = GetSpecificLocation(variable.Id);

                if (location != null)
                {
                    Handle handle = (Handle)location.Value;
                    handle?.Uninitialize(context ?? new HandleInitializationContext(_executor, scope));
                    location.Value = null;
                }
            }
        }
    }

    internal void DeclareHandle(LocationReference locationReference, Location location, ActivityInstance activityInstance)
    {
        _hasHandles = true;

        Declare(locationReference, location, activityInstance);
    }

    internal void DeclareTemporaryLocation<T>(LocationReference locationReference, ActivityInstance activityInstance, bool bufferGetsOnCollapse)
        where T : Location
    {
        Location locationToDeclare = new Location<T>();
        locationToDeclare.SetTemporaryResolutionData(this, bufferGetsOnCollapse);

        Declare(locationReference, locationToDeclare, activityInstance);
    }

    internal void Declare(LocationReference locationReference, Location location, ActivityInstance activityInstance)
    {
        Fx.Assert((locationReference.Id == 0 && _locations == null) || (locationReference.Id >= 0 && _locations != null && locationReference.Id < _locations.Length), "The environment should have been created with the appropriate capacity.");
        Fx.Assert(location != null, "");

        RegisterLocation(location, locationReference, activityInstance);

        if (_locations == null)
        {
            Fx.Assert(_singleLocation == null, "We should not have had a single location if we are trying to declare one.");
            Fx.Assert(locationReference.Id == 0, "We should think the id is zero if we are setting the single location.");

            _singleLocation = location;
        }
        else
        {
            Fx.Assert(_locations[locationReference.Id] == null || _locations[locationReference.Id] is DummyLocation, "We should not have had a location at the spot we are replacing.");

            _locations[locationReference.Id] = location;
        }
    }

    internal object GetValue(RuntimeArgument argument)
    {
        var location = GetSpecificLocation(argument.BoundArgument.Id) ?? throw FxTrace.Exception.AsError(new InvalidOperationException(SR.NoOutputLocationWasFound(argument.Name)));
        return location.Value;
    }

    internal Location<T> GetSpecificLocation<T>(int id) => GetSpecificLocation(id) as Location<T>;

    internal Location GetSpecificLocation(int id)
    {
        Fx.Assert(id >= 0 && ((_locations == null && id == 0) || (_locations != null && id < _locations.Length)), "Id needs to be within bounds.");

        return _locations == null ? _singleLocation : _locations[id];
    }

    // called for asynchronous argument resolution to collapse Location<Location<T>> to Location<T> in the environment
    internal void CollapseTemporaryResolutionLocations()
    {
        if (_locations == null)
        {
            if (_singleLocation != null &&
                ReferenceEquals(_singleLocation.TemporaryResolutionEnvironment, this))
            {
                CollapseTemporaryResolutionLocation(ref _singleLocation);
            }
        }
        else
        {
            for (int i = 0; i < _locations.Length; i++)
            {
                Location referenceLocation = _locations[i];

                if (referenceLocation != null &&
                    ReferenceEquals(referenceLocation.TemporaryResolutionEnvironment, this))
                {
                    CollapseTemporaryResolutionLocation(ref _locations[i]);
                }
            }
        }
    }

    // Called after an argument is added in Dynamic Update, when we need to collapse
    // just one location rather than the whole environment
    internal void CollapseTemporaryResolutionLocation(Location location)
    {
        // This assert doesn't necessarily imply that the location is still part of this environment;
        // it might have been removed in a subsequent update. If so, this method is a no-op.
        Fx.Assert(location.TemporaryResolutionEnvironment == this, "Trying to collapse from the wrong environment");

        if (_singleLocation == location)
        {
            CollapseTemporaryResolutionLocation(ref _singleLocation);
        }
        else if (_locations != null)
        {
            for (int i = 0; i < _locations.Length; i++)
            {
                if (_locations[i] == location)
                {
                    CollapseTemporaryResolutionLocation(ref _locations[i]);
                }
            }
        }
    }

    private static void CollapseTemporaryResolutionLocation(ref Location location)
    {
        if (location.Value == null)
        {
            location = (Location)location.CreateDefaultValue();
        }
        else
        {
            location = ((Location)location.Value).CreateReference(location.BufferGetsOnCollapse);
        }
    }

    private void RegisterUpdatedLocations(ActivityInstance activityInstance)
    {
        if (_locationsToRegister != null)
        {
            foreach (LocationReference locationReference in _locationsToRegister)
            {
                RegisterLocation(GetSpecificLocation(locationReference.Id), locationReference, activityInstance);
            }
            _locationsToRegister = null;
        }

        if (_locationsToUnregister != null)
        {
            foreach (Location location in _locationsToUnregister)
            {
                UnregisterLocation(location);
            }
            _locationsToUnregister = null;
        }
    }

    // Gets the location at this scope.  The caller verifies that ref.owner == this.definition.
    internal bool TryGetLocation(int id, out Location value)
    {
        ThrowIfDisposed();

        value = null;

        if (_locations == null)
        {
            if (id == 0)
            {
                value = _singleLocation;
            }
        }
        else
        {
            if (_locations.Length > id)
            {
                value = _locations[id];
            }
        }

        return value != null;
    }

    internal bool TryGetLocation(int id, Activity environmentOwner, out Location value)
    {
        ThrowIfDisposed();

        LocationEnvironment targetEnvironment = this;

        while (targetEnvironment != null && targetEnvironment.Definition != environmentOwner)
        {
            targetEnvironment = targetEnvironment.Parent;
        }

        if (targetEnvironment == null)
        {
            value = null;
            return false;
        }

        value = null;

        if (id == 0 && targetEnvironment._locations == null)
        {
            value = targetEnvironment._singleLocation;
        }
        else if (targetEnvironment._locations != null && targetEnvironment._locations.Length > id)
        {
            value = targetEnvironment._locations[id];
        }

        return value != null;
    }

    private void RegisterLocation(Location location, LocationReference locationReference, ActivityInstance activityInstance)
    {
        if (location.CanBeMapped)
        {
            _hasMappableLocations = true;
            MappableObjectManager.Register(location, Definition, locationReference, activityInstance);
        }
    }

    private void UnregisterLocation(Location location) => MappableObjectManager.Unregister(location);

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw FxTrace.Exception.AsError(
                new ObjectDisposedException(GetType().FullName, SR.EnvironmentDisposed));
        }
    }

#if DYNAMICUPDATE
    internal void Update(EnvironmentUpdateMap map, Activity activity)
    {
        //                    arguments     public variables      private variables    RuntimeDelegateArguments
        //  Locations array:  AAAAAAAAAA   VVVVVVVVVVVVVVVVVVVVVV PPPPPPPPPPPPPPPPPPP  DDDDDDDDDDDDDDDDDDDDDDDDDDDDDD

        int actualRuntimeDelegateArgumentCount = activity.HandlerOf == null ? 0 : activity.HandlerOf.RuntimeDelegateArguments.Count;

        if (map.NewArgumentCount != activity.RuntimeArguments.Count ||
            map.NewVariableCount != activity.RuntimeVariables.Count ||
            map.NewPrivateVariableCount != activity.ImplementationVariables.Count ||
            map.RuntimeDelegateArgumentCount != actualRuntimeDelegateArgumentCount)
        {
            throw FxTrace.Exception.AsError(new InstanceUpdateException(SR.InvalidUpdateMap(
                SR.WrongEnvironmentCount(activity, map.NewArgumentCount, map.NewVariableCount, map.NewPrivateVariableCount, map.RuntimeDelegateArgumentCount,
                    activity.RuntimeArguments.Count, activity.RuntimeVariables.Count, activity.ImplementationVariables.Count, actualRuntimeDelegateArgumentCount))));
        }

        int expectedLocationCount = map.OldArgumentCount + map.OldVariableCount + map.OldPrivateVariableCount + map.RuntimeDelegateArgumentCount;

        int actualLocationCount;
        if (this.locations == null)
        {
            if (this.singleLocation == null)
            {
                // we can hit this condition when the root activity instance has zero symbol.
                actualLocationCount = 0;
            }
            else
            {
                actualLocationCount = 1;

                // temporarily normalize to locations array for the sake of environment update processing
                this.locations = new Location[] { this.singleLocation };
                this.singleLocation = null;
            }
        }
        else
        {
            Fx.Assert(this.singleLocation == null, "locations and singleLocations cannot be non-null at the same time.");
            actualLocationCount = this.locations.Length;
        }

        if (expectedLocationCount != actualLocationCount)
        {
            throw FxTrace.Exception.AsError(new InstanceUpdateException(SR.InvalidUpdateMap(
                SR.WrongOriginalEnvironmentCount(activity, map.OldArgumentCount, map.OldVariableCount, map.OldPrivateVariableCount, map.RuntimeDelegateArgumentCount,
                    expectedLocationCount, actualLocationCount))));
        }

        Location[] newLocations = null;

        // If newTotalLocations == 0, update will leave us with an empty LocationEnvironment,
        // which is something the runtime would normally never create. This is harmless, but it
        // is a loosening of normal invariants.
        int newTotalLocations = map.NewArgumentCount + map.NewVariableCount + map.NewPrivateVariableCount + map.RuntimeDelegateArgumentCount;
        if (newTotalLocations > 0)
        {
            newLocations = new Location[newTotalLocations];
        }

        UpdateArguments(map, newLocations);
        UnregisterRemovedVariables(map);
        UpdatePublicVariables(map, newLocations, activity);
        UpdatePrivateVariables(map, newLocations, activity);
        CopyRuntimeDelegateArguments(map, newLocations);

        Location newSingleLocation = null;
        if (newTotalLocations == 1)
        {
            newSingleLocation = newLocations[0];
            newLocations = null;
        }

        this.singleLocation = newSingleLocation;
        this.locations = newLocations;
    }

    void UpdateArguments(EnvironmentUpdateMap map, Location[] newLocations)
    {
        if (map.HasArgumentEntries)
        {
            for (int i = 0; i < map.ArgumentEntries.Count; i++)
            {
                EnvironmentUpdateMapEntry entry = map.ArgumentEntries[i];

                Fx.Assert(entry.NewOffset >= 0 && entry.NewOffset < map.NewArgumentCount, "Argument offset is out of range");

                if (entry.IsAddition)
                {
                    // Location allocation will be performed later during ResolveDynamicallyAddedArguments().
                    // for now, simply assign a dummy location so we know not to copy over the old value.
                    newLocations[entry.NewOffset] = dummyLocation;
                }
                else
                {
                    Fx.Assert(this.locations != null && this.singleLocation == null, "Caller should have copied singleLocation into locations array");

                    // rearrangement of existing arguments
                    // this entry here doesn't describe argument removal
                    newLocations[entry.NewOffset] = this.locations[entry.OldOffset];
                }
            }
        }

        // copy over unchanged Locations, and null out DummyLocations
        for (int i = 0; i < map.NewArgumentCount; i++)
        {
            if (newLocations[i] == null)
            {
                Fx.Assert(this.locations != null && this.locations.Length > i, "locations must be non-null and index i must be within the range of locations.");
                newLocations[i] = this.locations[i];
            }
            else if (newLocations[i] == dummyLocation)
            {
                newLocations[i] = null;
            }
        }
    }

    void UpdatePublicVariables(EnvironmentUpdateMap map, Location[] newLocations, Activity activity)
    {
        UpdateVariables(
            map.NewArgumentCount,
            map.OldArgumentCount,
            map.NewVariableCount,
            map.OldVariableCount,
            map.VariableEntries,
            activity.RuntimeVariables,
            newLocations);
    }

    void UpdatePrivateVariables(EnvironmentUpdateMap map, Location[] newLocations, Activity activity)
    {
        UpdateVariables(
            map.NewArgumentCount + map.NewVariableCount,
            map.OldArgumentCount + map.OldVariableCount,
            map.NewPrivateVariableCount,
            map.OldPrivateVariableCount,
            map.PrivateVariableEntries,
            activity.ImplementationVariables,
            newLocations);
    }

    void UpdateVariables(int newVariablesOffset, int oldVariablesOffset, int newVariableCount, int oldVariableCount, IList<EnvironmentUpdateMapEntry> variableEntries, IList<Variable> variables, Location[] newLocations)
    {
        if (variableEntries != null)
        {
            for (int i = 0; i < variableEntries.Count; i++)
            {
                EnvironmentUpdateMapEntry entry = variableEntries[i];

                Fx.Assert(entry.NewOffset >= 0 && entry.NewOffset < newVariableCount, "Variable offset is out of range");
                Fx.Assert(!entry.IsNewHandle, "This should have been caught in ActivityInstanceMap.UpdateRawInstance");

                if (entry.IsAddition)
                {
                    Variable newVariable = variables[entry.NewOffset];
                    Location location = newVariable.CreateLocation();
                    newLocations[newVariablesOffset + entry.NewOffset] = location;
                    if (location.CanBeMapped)
                    {
                        ActivityUtilities.Add(ref this.locationsToRegister, newVariable);
                    }
                }
                else
                {
                    Fx.Assert(this.locations != null && this.singleLocation == null, "Caller should have copied singleLocation into locations array");

                    // rearrangement of existing variable
                    // this entry here doesn't describe variable removal
                    newLocations[newVariablesOffset + entry.NewOffset] = this.locations[oldVariablesOffset + entry.OldOffset];
                }
            }
        }

        // copy over unchanged variable Locations
        for (int i = 0; i < newVariableCount; i++)
        {
            if (newLocations[newVariablesOffset + i] == null)
            {
                Fx.Assert(i < oldVariableCount, "New variable should have a location");
                Fx.Assert(this.locations != null && this.locations.Length > oldVariablesOffset + i, "locations must be non-null and index i + oldVariableOffset must be within the range of locations.");

                newLocations[newVariablesOffset + i] = this.locations[oldVariablesOffset + i];
            }
        }
    }

    void CopyRuntimeDelegateArguments(EnvironmentUpdateMap map, Location[] newLocations)
    {
        for (int i = 1; i <= map.RuntimeDelegateArgumentCount; i++)
        {
            newLocations[newLocations.Length - i] = this.locations[this.locations.Length - i];
        }
    }

    void UnregisterRemovedVariables(EnvironmentUpdateMap map)
    {
        bool hasMappableLocationsRemaining = false;
        int offset = map.OldArgumentCount;

        FindVariablesToUnregister(false, map, map.OldVariableCount, offset, ref hasMappableLocationsRemaining);

        offset = map.OldArgumentCount + map.OldVariableCount;

        FindVariablesToUnregister(true, map, map.OldPrivateVariableCount, offset, ref hasMappableLocationsRemaining);

        this.hasMappableLocations = hasMappableLocationsRemaining;
    }

    delegate int? GetNewVariableIndex(int oldIndex);
    private void FindVariablesToUnregister(bool forImplementation, EnvironmentUpdateMap map, int oldVariableCount, int offset, ref bool hasMappableLocationsRemaining)
    {
        for (int i = 0; i < oldVariableCount; i++)
        {
            Location location = this.locations[i + offset];
            if (location.CanBeMapped)
            {
                if ((forImplementation && map.GetNewPrivateVariableIndex(i).HasValue) || (!forImplementation && map.GetNewVariableIndex(i).HasValue))
                {
                    hasMappableLocationsRemaining = true;
                }
                else
                {
                    ActivityUtilities.Add(ref this.locationsToUnregister, location);
                }
            }
        }
    }

#endif
    private class DummyLocation : Location<object>
    {
        // this is a dummy location 
        // temporary place holder for a dynamically added LocationReference
        // Only used for dynamic update
    }
}
