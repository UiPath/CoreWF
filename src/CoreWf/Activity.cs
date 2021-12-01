// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities;
using Expressions;
using Hosting;
using Internals;
using Runtime;
using Validation;
using XamlIntegration;

[ContentProperty("Implementation")]
public abstract partial class Activity
{
    private const string GeneratedArgumentPrefix = "Argument";
    private static int _nextCacheId;
    private static readonly IList<Activity> _emptyChildren = new List<Activity>(0);
    private static readonly IList<Variable> _emptyVariables = new List<Variable>(0);
    private static readonly IList<RuntimeArgument> _emptyArguments = new List<RuntimeArgument>(0);
    private static readonly IList<ActivityDelegate> _emptyDelegates = new List<ActivityDelegate>(0);

    internal static readonly ReadOnlyCollection<Constraint> _emptyConstraints = new(Array.Empty<Constraint>());
    private string _displayName;
    private bool _isDisplayNameSet;
    private int _id;
    private RootProperties _rootProperties;
    private IList<RuntimeArgument> _arguments;
    private IList<Activity> _children;
    private IList<Activity> _implementationChildren;
    private IList<Activity> _importedChildren;
    private IList<ActivityDelegate> _delegates;
    private IList<ActivityDelegate> _implementationDelegates;
    private IList<ActivityDelegate> _importedDelegates;
    private IList<Variable> _variables;
    private IList<Variable> _implementationVariables;
    private IList<ValidationError> _tempValidationErrors;
    private IList<RuntimeArgument> _tempAutoGeneratedArguments;
    private Collection<Constraint> _constraints;
    private Activity _runtimeImplementation;
    private Activity _rootActivity;
    private readonly object _thisLock;
    private QualifiedId _qualifiedId;

    // For a given cacheId this tells us whether we've called InternalCacheMetadata yet or not
    private CacheStates _isMetadataCached;
    private int _cacheId;
    private RelationshipType _relationshipToParent;
    private bool? _isSubtreeEmpty;
    private int _symbolCount;

    // alternatives are extended through DynamicActivity, CodeActivity, and NativeActivity
    protected Activity()
    {
        _thisLock = new object();
    }

    [TypeConverter(TypeConverters.ImplementationVersionConverter)]
    [DefaultValue(null)]
    [IgnoreDataMember]
    protected virtual internal Version ImplementationVersion { get; set; }

    [XamlDeferLoad(OtherXaml.FuncDeferringLoader, OtherXaml.Activity)]
    [DefaultValue(null)]
    [Browsable(false)]
    [Ambient]
    [IgnoreDataMember]
    protected virtual Func<Activity> Implementation { get; set; }

    protected Collection<Constraint> Constraints
    {
        get
        {
            _constraints ??= new Collection<Constraint>();
            return _constraints;
        }
    }

    protected internal int CacheId => _cacheId;

    internal RelationshipType RelationshipToParent => _relationshipToParent;

    internal bool HasNonEmptySubtree
    {
        get
        {
            if (_isSubtreeEmpty.HasValue)
            {
                return !_isSubtreeEmpty.Value;
            }
            else
            {
                if (Children.Count > 0 || ImplementationChildren.Count > 0 || ImportedChildren.Count > 0 ||
                    Delegates.Count > 0 || ImplementationDelegates.Count > 0 || ImportedDelegates.Count > 0 ||
                    RuntimeVariables.Count > 0 || ImplementationVariables.Count > 0 ||
                    RuntimeArguments.Count > 0)
                {
                    _isSubtreeEmpty = false;
                }
                else
                {
                    _isSubtreeEmpty = true;
                }
                return !_isSubtreeEmpty.Value;
            }
        }
    }

    internal int SymbolCount => _symbolCount;

    internal IdSpace MemberOf { get; set; }

    internal IdSpace ParentOf { get; set; }

    internal QualifiedId QualifiedId
    {
        get
        {
            _qualifiedId ??= new QualifiedId(this);
            return _qualifiedId;
        }
    }

    // This flag governs special behavior that we need to keep for back-compat on activities
    // that implemented TryGetValue in 4.0.
    internal bool UseOldFastPath { get; set; }

    internal bool SkipArgumentResolution { get; set; }

    internal bool IsFastPath => SkipArgumentResolution && IsActivityWithResult;

    internal virtual bool IsActivityWithResult => false;

    internal object Origin { get; set; }

    public string DisplayName
    {
        get
        {
            if (!_isDisplayNameSet && string.IsNullOrEmpty(_displayName))
            {
                _displayName = ActivityUtilities.GetDisplayName(this);
            }

            return _displayName;
        }
        set
        {
            _displayName = value ?? string.Empty;
            _isDisplayNameSet = true;
        }
    }

    public string Id => _id == 0 ? null : QualifiedId.ToString();

    internal bool IsExpressionRoot => _relationshipToParent == RelationshipType.ArgumentExpression;

    internal bool HasStartedCachingMetadata => _isMetadataCached != CacheStates.Uncached;

    internal bool IsMetadataCached => _isMetadataCached != CacheStates.Uncached;

    internal bool IsMetadataFullyCached => (_isMetadataCached & CacheStates.Full) == CacheStates.Full;

    internal bool IsRuntimeReady => (_isMetadataCached & CacheStates.RuntimeReady) == CacheStates.RuntimeReady;

    internal Activity RootActivity => _rootActivity;

    internal int InternalId
    {
        get
        {
            return _id;
        }
        set
        {
            Fx.Assert(value != 0, "0 is an invalid ID");
            ClearIdInfo();
            _id = value;
        }
    }

    internal ActivityDelegate HandlerOf { get; private set; }

    internal Activity Parent { get; private set; }

    internal LocationReferenceEnvironment HostEnvironment => RootActivity?._rootProperties?.HostEnvironment;

    internal IList<RuntimeArgument> RuntimeArguments => _arguments;

    internal IList<Activity> Children => _children;

    internal IList<Activity> ImplementationChildren => _implementationChildren;

    internal IList<Activity> ImportedChildren => _importedChildren;

    internal IList<ActivityDelegate> Delegates => _delegates;

    internal IList<ActivityDelegate> ImplementationDelegates => _implementationDelegates;

    internal IList<ActivityDelegate> ImportedDelegates => _importedDelegates;

    internal bool HasBeenAssociatedWithAnInstance
    {
        get
        {
            if (_rootProperties != null)
            {
                return _rootProperties.HasBeenAssociatedWithAnInstance;
            }
            else if (IsMetadataCached && RootActivity?._rootProperties != null)
            {
                return RootActivity._rootProperties.HasBeenAssociatedWithAnInstance;
            }
            else
            {
                return false;
            }
        }
        set
        {
            Fx.Assert(_rootProperties != null, "This should only be called on the root and we should already be cached.");
            Fx.Assert(value, "We really only let you set this to true.");

            _rootProperties.HasBeenAssociatedWithAnInstance = value;
        }
    }

    internal Dictionary<string, List<RuntimeArgument>> OverloadGroups
    {
        get
        {
            Fx.Assert(_rootProperties != null || Diagnostics.Debugger.IsAttached, "This should only be called on the root.");
            return _rootProperties.OverloadGroups;
        }
        set
        {
            Fx.Assert(_rootProperties != null, "This should only be called on the root.");
            _rootProperties.OverloadGroups = value;
        }
    }

    internal List<RuntimeArgument> RequiredArgumentsNotInOverloadGroups
    {
        get
        {
            Fx.Assert(_rootProperties != null || Diagnostics.Debugger.IsAttached, "This should only be called on the root.");
            return _rootProperties.RequiredArgumentsNotInOverloadGroups;
        }
        set
        {
            Fx.Assert(_rootProperties != null, "This should only be called on the root.");
            _rootProperties.RequiredArgumentsNotInOverloadGroups = value;
        }
    }

    internal ValidationHelper.OverloadGroupEquivalenceInfo EquivalenceInfo
    {
        get
        {
            Fx.Assert(_rootProperties != null || Diagnostics.Debugger.IsAttached, "This should only be called on the root.");
            return _rootProperties.EquivalenceInfo;
        }
        set
        {
            Fx.Assert(_rootProperties != null, "This should only be called on the root.");
            _rootProperties.EquivalenceInfo = value;
        }
    }

    internal IList<Variable> RuntimeVariables => _variables;

    internal IList<Variable> ImplementationVariables => _implementationVariables;

    internal IList<Constraint> RuntimeConstraints => InternalGetConstraints();

    internal LocationReferenceEnvironment PublicEnvironment { get; set; }

    internal LocationReferenceEnvironment ImplementationEnvironment { get; set; }

    internal virtual bool InternalCanInduceIdle => false;

    internal bool HasTempViolations => _tempValidationErrors != null && _tempValidationErrors.Count > 0;

    internal object ThisLock => _thisLock;

    internal int RequiredExtensionTypesCount
    {
        get
        {
            Fx.Assert(_rootProperties != null || Diagnostics.Debugger.IsAttached, "only callable on the root");
            return _rootProperties.RequiredExtensionTypesCount;
        }
    }

    internal int DefaultExtensionsCount
    {
        get
        {
            Fx.Assert(_rootProperties != null || Diagnostics.Debugger.IsAttached, "only callable on the root");
            return _rootProperties.DefaultExtensionsCount;
        }
    }

    internal bool GetActivityExtensionInformation(out Dictionary<Type, WorkflowInstanceExtensionProvider> activityExtensionProviders, out HashSet<Type> requiredActivityExtensionTypes)
    {
        Fx.Assert(_rootProperties != null, "only callable on the root");
        return _rootProperties.GetActivityExtensionInformation(out activityExtensionProviders, out requiredActivityExtensionTypes);
    }

    internal virtual bool IsResultArgument(RuntimeArgument argument) => false;

    internal bool CanBeScheduledBy(Activity parent)
    {
        // fast path if we're the sole (or first) child
        if (ReferenceEquals(parent, Parent))
        {
            return _relationshipToParent == RelationshipType.ImplementationChild || _relationshipToParent == RelationshipType.Child;
        }
        else
        {
            return parent.Children.Contains(this) || parent.ImplementationChildren.Contains(this);
        }
    }

    internal void ClearIdInfo()
    {
        if (ParentOf != null)
        {
            ParentOf.Dispose();
            ParentOf = null;
        }

        _id = 0;
        _qualifiedId = null;
    }

    // We use these Set methods rather than a setter on the property since
    // we don't want to make it seem like setting these collections is the
    // "normal" thing to do.  Only OnInternalCacheMetadata implementations
    // should call these methods.
    internal void SetChildrenCollection(Collection<Activity> children) => _children = children;

    internal void AddChild(Activity child)
    {
        _children ??= new Collection<Activity>();
        _children.Add(child);
    }

    internal void SetImplementationChildrenCollection(Collection<Activity> implementationChildren) => _implementationChildren = implementationChildren;

    internal void AddImplementationChild(Activity implementationChild)
    {
        _implementationChildren ??= new Collection<Activity>();
        _implementationChildren.Add(implementationChild);
    }

    internal void SetImportedChildrenCollection(Collection<Activity> importedChildren) => _importedChildren = importedChildren;

    internal void AddImportedChild(Activity importedChild)
    {
        _importedChildren ??= new Collection<Activity>();
        _importedChildren.Add(importedChild);
    }

    internal void SetDelegatesCollection(Collection<ActivityDelegate> delegates) => _delegates = delegates;

    internal void AddDelegate(ActivityDelegate activityDelegate)
    {
        _delegates ??= new Collection<ActivityDelegate>();
        _delegates.Add(activityDelegate);
    }

    internal void SetImplementationDelegatesCollection(Collection<ActivityDelegate> implementationDelegates) => _implementationDelegates = implementationDelegates;

    internal void AddImplementationDelegate(ActivityDelegate implementationDelegate)
    {
        _implementationDelegates ??= new Collection<ActivityDelegate>();
        _implementationDelegates.Add(implementationDelegate);
    }

    internal void SetImportedDelegatesCollection(Collection<ActivityDelegate> importedDelegates) => _importedDelegates = importedDelegates;

    internal void AddImportedDelegate(ActivityDelegate importedDelegate)
    {
        _importedDelegates ??= new Collection<ActivityDelegate>();
        _importedDelegates.Add(importedDelegate);
    }

    internal void SetVariablesCollection(Collection<Variable> variables) => _variables = variables;

    internal void AddVariable(Variable variable)
    {
        _variables ??= new Collection<Variable>();
        _variables.Add(variable);
    }

    internal void SetImplementationVariablesCollection(Collection<Variable> implementationVariables) => _implementationVariables = implementationVariables;

    internal void AddImplementationVariable(Variable implementationVariable)
    {
        _implementationVariables ??= new Collection<Variable>();
        _implementationVariables.Add(implementationVariable);
    }

    internal void SetArgumentsCollection(Collection<RuntimeArgument> arguments, bool createEmptyBindings)
    {
        _arguments = arguments;

        // Arguments should always be "as bound as possible"
        if (_arguments != null && _arguments.Count > 0)
        {
            for (int i = 0; i < _arguments.Count; i++)
            {
                RuntimeArgument argument = _arguments[i];

                argument.SetupBinding(this, createEmptyBindings);
            }

            _arguments.QuickSort(RuntimeArgument.EvaluationOrderComparer);
        }
    }

    internal void AddArgument(RuntimeArgument argument, bool createEmptyBindings)
    {
        _arguments ??= new Collection<RuntimeArgument>();

        argument.SetupBinding(this, createEmptyBindings);

        int insertionIndex = _arguments.BinarySearch(argument, RuntimeArgument.EvaluationOrderComparer);
        if (insertionIndex < 0)
        {
            _arguments.Insert(~insertionIndex, argument);
        }
        else
        {
            _arguments.Insert(insertionIndex, argument);
        }
    }

    internal void SetTempValidationErrorCollection(IList<ValidationError> validationErrors) => _tempValidationErrors = validationErrors;

    internal void TransferTempValidationErrors(ref IList<ValidationError> newList)
    {
        if (_tempValidationErrors != null)
        {
            for (int i = 0; i < _tempValidationErrors.Count; i++)
            {
                ActivityUtilities.Add(ref newList, _tempValidationErrors[i]);
            }
        }
        _tempValidationErrors = null;

    }

    internal void AddTempValidationError(ValidationError validationError)
    {
        _tempValidationErrors ??= new Collection<ValidationError>();
        _tempValidationErrors.Add(validationError);
    }

    internal RuntimeArgument AddTempAutoGeneratedArgument(Type argumentType, ArgumentDirection direction)
    {
        _tempAutoGeneratedArguments ??= new Collection<RuntimeArgument>();

        string name = GeneratedArgumentPrefix + _tempAutoGeneratedArguments.Count.ToString(CultureInfo.InvariantCulture);
        RuntimeArgument argument = new(name, argumentType, direction);
        _tempAutoGeneratedArguments.Add(argument);
        return argument;
    }

    internal void ResetTempAutoGeneratedArguments() => _tempAutoGeneratedArguments = null;

    internal virtual IList<Constraint> InternalGetConstraints() => _constraints != null && _constraints.Count > 0 ? _constraints : Activity._emptyConstraints;

    public override string ToString() => string.Format(CultureInfo.CurrentCulture, "{0}: {1}", Id, DisplayName);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeDisplayName() => _isDisplayNameSet;

    // subclasses are responsible for creating/disposing the necessary contexts
    internal virtual void InternalAbort(ActivityInstance instance, ActivityExecutor executor, Exception terminationReason) { }

    // subclasses are responsible for creating/disposing the necessary contexts
    internal virtual void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (_runtimeImplementation != null)
        {
            executor.ScheduleActivity(_runtimeImplementation, instance, null, null, null);
        }
    }

    // subclasses are responsible for creating/disposing the necessary contexts. This implementation
    // covers Activity, Activity<T>, DynamicActivity, DynamicActivity<T>
    internal virtual void InternalCancel(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        NativeActivityContext context = executor.NativeActivityContextPool.Acquire();
        try
        {
            context.Initialize(instance, executor, bookmarkManager);
            context.Cancel();
        }
        finally
        {
            context.Dispose();
            executor.NativeActivityContextPool.Release(context);
        }
    }

    internal bool IsSingletonActivityDeclared(string name)
    {
        if (_rootActivity == null || _rootActivity._rootProperties == null)
        {
            return false;
        }
        else
        {
            return _rootActivity._rootProperties.IsSingletonActivityDeclared(name);
        }
    }

    internal void DeclareSingletonActivity(string name, Activity activity)
    {
        if (_rootActivity != null && _rootActivity._rootProperties != null)
        {
            _rootActivity._rootProperties.DeclareSingletonActivity(name, activity);
        }
    }

    internal Activity GetSingletonActivity(string name)
    {
        if (_rootActivity != null && _rootActivity._rootProperties != null)
        {
            return _rootActivity._rootProperties.GetSingletonActivity(name);
        }

        return null;
    }

    internal void ClearCachedInformation()
    {
        ClearCachedMetadata();
        _isMetadataCached = CacheStates.Uncached;
    }

    internal void InitializeAsRoot(LocationReferenceEnvironment hostEnvironment)
    {
        // We're being treated as the root of the workflow
        Parent = null;
        ParentOf = null;

        Interlocked.CompareExchange(ref _nextCacheId, 1, int.MaxValue);
        _cacheId = Interlocked.Increment(ref _nextCacheId);

        ClearCachedInformation();

        MemberOf = new IdSpace();
        _rootProperties = new RootProperties
        {
            HostEnvironment = hostEnvironment
        };
        _rootActivity = this;
    }

    internal LocationReferenceEnvironment GetParentEnvironment()
    {
        LocationReferenceEnvironment parentEnvironment = null;

        if (Parent == null)
        {
            Fx.Assert(_rootProperties != null, "Root properties must be available now.");

            parentEnvironment = new ActivityLocationReferenceEnvironment(_rootProperties.HostEnvironment) { InternalRoot = this };
        }
        else
        {
            switch (_relationshipToParent)
            {
                case RelationshipType.ArgumentExpression:
                    parentEnvironment = Parent.PublicEnvironment.Parent;

                    if (parentEnvironment == null)
                    {
                        parentEnvironment = RootActivity._rootProperties.HostEnvironment;
                    }
                    break;
                case RelationshipType.DelegateHandler:
                    Fx.Assert(HandlerOf != null, "Must have the parent delegate set");

                    parentEnvironment = HandlerOf.Environment;
                    break;
                case RelationshipType.Child:
                case RelationshipType.ImportedChild:
                case RelationshipType.VariableDefault:
                    parentEnvironment = Parent.PublicEnvironment;
                    break;
                case RelationshipType.ImplementationChild:
                    parentEnvironment = Parent.ImplementationEnvironment;
                    break;
            }
        }

        return parentEnvironment;
    }

    internal bool InitializeRelationship(ActivityDelegate activityDelegate, ActivityCollectionType collectionType, ref IList<ValidationError> validationErrors)
    {
        if (_cacheId == activityDelegate.Owner.CacheId)
        {
            // This means that we already have a parent and a delegate is trying to initialize
            // a relationship.  Delegate handlers MUST be declared.

            ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityDelegateHandlersMustBeDeclarations(DisplayName, activityDelegate.Owner.DisplayName, Parent.DisplayName), false, activityDelegate.Owner));

            return false;
        }

        if (InitializeRelationship(activityDelegate.Owner, collectionType != ActivityCollectionType.Implementation, RelationshipType.DelegateHandler, ref validationErrors))
        {
            HandlerOf = activityDelegate;

            return true;
        }

        return false;
    }

    internal bool InitializeRelationship(RuntimeArgument argument, ref IList<ValidationError> validationErrors)
        => InitializeRelationship(argument.Owner, true, RelationshipType.ArgumentExpression, ref validationErrors);

    internal bool InitializeRelationship(Variable variable, bool isPublic, ref IList<ValidationError> validationErrors)
        => InitializeRelationship(variable.Owner, isPublic, RelationshipType.VariableDefault, ref validationErrors);

    internal bool InitializeRelationship(Activity parent, ActivityCollectionType collectionType, ref IList<ValidationError> validationErrors)
    {
        RelationshipType relationshipType = RelationshipType.Child;
        if (collectionType == ActivityCollectionType.Imports)
        {
            relationshipType = RelationshipType.ImportedChild;
        }
        else if (collectionType == ActivityCollectionType.Implementation)
        {
            relationshipType = RelationshipType.ImplementationChild;
        }

        return InitializeRelationship(parent, collectionType != ActivityCollectionType.Implementation, relationshipType, ref validationErrors);
    }

    private bool InitializeRelationship(Activity parent, bool isPublic, RelationshipType relationship, ref IList<ValidationError> validationErrors)
    {
        if (_cacheId == parent._cacheId)
        {
            // This means that we've already encountered a parent in the tree

            // Validate that it is visible.

            // In order to see the activity the new parent must be
            // in the implementation IdSpace of an activity which has
            // a public reference to it.
            Activity referenceTarget = parent.MemberOf.Owner;

            if (ReferenceEquals(this, parent))
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityCannotReferenceItself(DisplayName), parent));

                return false;
            }
            else if (Parent == null)
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.RootActivityCannotBeReferenced(DisplayName, parent.DisplayName), parent));

                return false;
            }
            else if (referenceTarget == null)
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityCannotBeReferencedWithoutTarget(DisplayName, parent.DisplayName, Parent.DisplayName), parent));

                return false;
            }
            else if (!referenceTarget.Children.Contains(this) && !referenceTarget.ImportedChildren.Contains(this))
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.ActivityCannotBeReferenced(DisplayName, parent.DisplayName, referenceTarget.DisplayName, Parent.DisplayName), false, parent));

                return false;
            }

            // This is a valid reference so we want to allow
            // normal processing to proceed.
            return true;
        }

        Parent = parent;
        HandlerOf = null;
        _rootActivity = parent.RootActivity;
        _cacheId = parent._cacheId;
        _isMetadataCached = CacheStates.Uncached;
        ClearCachedMetadata();
        _relationshipToParent = relationship;

        if (isPublic)
        {
            MemberOf = parent.MemberOf;
        }
        else
        {
            parent.ParentOf ??= new IdSpace(parent.MemberOf, parent.InternalId);
            MemberOf = parent.ParentOf;
        }

        return true;
    }

    private void ClearCachedMetadata()
    {
        _symbolCount = 0;

        _arguments = null;

        _children = null;
        _implementationChildren = null;
        _importedChildren = null;

        _delegates = null;
        _implementationDelegates = null;
        _importedDelegates = null;

        _variables = null;
        _implementationVariables = null;
    }

    internal void InternalCacheMetadata(bool createEmptyBindings, ref IList<ValidationError> validationErrors)
    {
        OnInternalCacheMetadata(createEmptyBindings);

        if (_tempAutoGeneratedArguments != null)
        {
            Fx.Assert(_tempAutoGeneratedArguments.Count > 0, "We should only have a non-null value here if we generated an argument");
            if (!SkipArgumentResolution)
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(
                    SR.PublicReferencesOnActivityRequiringArgumentResolution(DisplayName), false, this));
            }

            if (_arguments == null)
            {
                _arguments = _tempAutoGeneratedArguments;
            }
            else
            {
                for (int i = 0; i < _tempAutoGeneratedArguments.Count; i++)
                {
                    _arguments.Add(_tempAutoGeneratedArguments[i]);
                }
            }

            _tempAutoGeneratedArguments = null;
        }

        if (_arguments != null && _arguments.Count > 1)
        {
            ActivityValidationServices.ValidateEvaluationOrder(_arguments, this, ref _tempValidationErrors);
        }

        if (_tempValidationErrors != null)
        {
            if (validationErrors == null)
            {
                validationErrors = new List<ValidationError>();
            }

            for (int i = 0; i < _tempValidationErrors.Count; i++)
            {
                ValidationError validationError = _tempValidationErrors[i];

                validationError.Source = this;
                validationError.Id = Id;

                validationErrors.Add(validationError);
            }

            _tempValidationErrors = null;
        }

        if (_arguments == null)
        {
            _arguments = _emptyArguments;
        }
        else
        {
            _symbolCount += _arguments.Count;
        }

        if (_variables == null)
        {
            _variables = _emptyVariables;
        }
        else
        {
            _symbolCount += _variables.Count;
        }

        if (_implementationVariables == null)
        {
            _implementationVariables = _emptyVariables;
        }
        else
        {
            _symbolCount += _implementationVariables.Count;
        }

        _children ??= _emptyChildren;
        _importedChildren ??= _emptyChildren;
        _implementationChildren ??= _emptyChildren;
        _delegates ??= _emptyDelegates;
        _importedDelegates ??= _emptyDelegates;
        _implementationDelegates ??= _emptyDelegates;
        _isMetadataCached = CacheStates.Partial;
    }

    // Note that this is relative to the type of walk we've done.  If we
    // skipped implementation then we can still be "Cached" even though
    // we never ignored the implementation.
    internal void SetCached(bool isSkippingPrivateChildren) => _isMetadataCached = isSkippingPrivateChildren ? CacheStates.Partial : CacheStates.Full;

    internal void SetRuntimeReady() => _isMetadataCached |= CacheStates.RuntimeReady;

    internal virtual void OnInternalCacheMetadata(bool createEmptyBindings)
    {
        // By running CacheMetadata first we allow the user
        // to set their Implementation during CacheMetadata.
        ActivityMetadata metadata = new(this, GetParentEnvironment(), createEmptyBindings);
        CacheMetadata(metadata);
        metadata.Dispose();

        _runtimeImplementation = Implementation != null ? Implementation() : null;

        if (_runtimeImplementation != null)
        {
            SetImplementationChildrenCollection(new Collection<Activity>
            {
                _runtimeImplementation
            });
        }
    }

    protected virtual void CacheMetadata(ActivityMetadata metadata)
    {
        ReflectedInformation information = new(this);

        SetImportedChildrenCollection(information.GetChildren());
        SetVariablesCollection(information.GetVariables());
        SetImportedDelegatesCollection(information.GetDelegates());
        SetArgumentsCollection(information.GetArguments(), metadata.CreateEmptyBindings);
    }

#if DYNAMICUPDATE
    internal virtual void OnInternalCreateDynamicUpdateMap(DynamicUpdateMapBuilder.Finalizer finalizer,
        DynamicUpdateMapBuilder.IDefinitionMatcher matcher, Activity originalActivity)
    {
        UpdateMapMetadata metadata = new UpdateMapMetadata(finalizer, matcher, this);
        try
        {
            OnCreateDynamicUpdateMap(metadata, originalActivity);
        }
        finally
        {
            metadata.Dispose();
        }
    }
    protected virtual void OnCreateDynamicUpdateMap(UpdateMapMetadata metadata, Activity originalActivity)
    {
    }
#endif

    internal void AddDefaultExtensionProvider<T>(Func<T> extensionProvider)
        where T : class
    {
        Fx.Assert(extensionProvider != null, "caller must verify");
        Fx.Assert(_rootActivity != null && _rootActivity._rootProperties != null, "need a valid root");
        _rootActivity._rootProperties.AddDefaultExtensionProvider(extensionProvider);
    }

    internal void RequireExtension(Type extensionType)
    {
        Fx.Assert(extensionType != null && !extensionType.IsValueType, "caller should verify we have a valid reference type");
        Fx.Assert(_rootActivity != null && _rootActivity._rootProperties != null, "need a valid root");
        _rootActivity._rootProperties.RequireExtension(extensionType);
    }
}

[TypeConverter(TypeConverters.ActivityWithResultConverter)]
[ValueSerializer(typeof(ActivityWithResultValueSerializer))]
public abstract class Activity<TResult> : ActivityWithResult
{
    // alternatives are extended through DynamicActivity<TResult>, CodeActivity<TResult>, and NativeActivity<TResult>
    protected Activity() : base() { }

    [DefaultValue(null)]
    public new OutArgument<TResult> Result { get; set; }

    internal override Type InternalResultType => typeof(TResult);

    internal override OutArgument ResultCore
    {
        get => Result;
        set
        {
            Result = value as OutArgument<TResult>;

            if (Result == null && value != null)
            {
                throw FxTrace.Exception.Argument(nameof(value), SR.ResultArgumentMustBeSpecificType(typeof(TResult)));
            }
        }
    }

    public static implicit operator Activity<TResult>(TResult constValue) => FromValue(constValue);

    public static implicit operator Activity<TResult>(Func<ActivityContext, TResult> func) => new FuncValue<TResult>(func);

    public static implicit operator Activity<TResult>(Variable variable) => FromVariable(variable);

    public static implicit operator Activity<TResult>(Variable<TResult> variable) => FromVariable(variable);

    public static Activity<TResult> FromValue(TResult constValue) => new Literal<TResult> { Value = constValue };

    public static Activity<TResult> FromVariable(Variable variable)
    {
        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        if (TypeHelper.AreTypesCompatible(variable.Type, typeof(TResult)))
        {
            return new VariableValue<TResult> { Variable = variable };
        }
        else
        {
            if (ActivityUtilities.IsLocationGenericType(typeof(TResult), out Type locationGenericType))
            {
                if (locationGenericType == variable.Type)
                {
                    return (Activity<TResult>)ActivityUtilities.CreateVariableReference(variable);
                }
            }
        }

        throw FxTrace.Exception.Argument(nameof(variable), SR.ConvertVariableToValueExpressionFailed(variable.GetType().FullName, typeof(Activity<TResult>).FullName));
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public static Activity<TResult> FromVariable(Variable<TResult> variable)
    {
        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }

        return new VariableValue<TResult>(variable);
    }

    internal override bool IsResultArgument(RuntimeArgument argument) => ReferenceEquals(argument, ResultRuntimeArgument);

    internal sealed override void OnInternalCacheMetadata(bool createEmptyBindings)
    {
        OnInternalCacheMetadataExceptResult(createEmptyBindings);

        bool foundResult = false;

        // This could be null at this point
        IList<RuntimeArgument> runtimeArguments = RuntimeArguments;
        if (runtimeArguments != null)
        {
            int runtimeArgumentCount = runtimeArguments.Count;
            for (int i = 0; i < runtimeArgumentCount; i++)
            {
                RuntimeArgument argument = runtimeArguments[i];

                if (argument.Name == "Result")
                {
                    foundResult = true;

                    if (argument.Type != typeof(TResult) || argument.Direction != ArgumentDirection.Out)
                    {
                        // The user supplied "Result" is incorrect so we
                        // log a violation.
                        AddTempValidationError(new ValidationError(SR.ResultArgumentHasRequiredTypeAndDirection(typeof(TResult), argument.Direction, argument.Type)));
                    }
                    else if (!IsBoundArgumentCorrect(argument, createEmptyBindings))
                    {
                        // The user supplied "Result" is not bound to the correct
                        // argument object.
                        AddTempValidationError(new ValidationError(SR.ResultArgumentMustBeBoundToResultProperty));
                    }
                    else
                    {
                        // The user supplied "Result" is correct so we
                        // cache it.
                        ResultRuntimeArgument = argument;
                    }

                    break;
                }
            }
        }

        if (!foundResult)
        {
            ResultRuntimeArgument = new RuntimeArgument("Result", typeof(TResult), ArgumentDirection.Out);

            if (Result == null)
            {
                if (createEmptyBindings)
                {
                    Result = new OutArgument<TResult>();
                    Argument.Bind(Result, ResultRuntimeArgument);
                }
                else
                {
                    OutArgument<TResult> tempArgument = new();
                    Argument.Bind(tempArgument, ResultRuntimeArgument);
                }
            }
            else
            {
                Argument.Bind(Result, ResultRuntimeArgument);
            }


            AddArgument(ResultRuntimeArgument, createEmptyBindings);
        }
    }

    private bool IsBoundArgumentCorrect(RuntimeArgument argument, bool createEmptyBindings)
    {
        if (createEmptyBindings)
        {
            // We must match if we've gone through
            // RuntimeArgument.SetupBinding with
            // createEmptyBindings == true.
            return ReferenceEquals(argument.BoundArgument, Result);
        }
        else
        {
            // Otherwise, if the Result is null then
            // SetupBinding has created a default
            // BoundArgument which is fine.  If it
            // is non-null then it had better match.
            return Result == null || ReferenceEquals(argument.BoundArgument, Result);
        }
    }

    // default to Activity's behavior
    internal virtual void OnInternalCacheMetadataExceptResult(bool createEmptyBindings)
        => base.OnInternalCacheMetadata(createEmptyBindings);

    internal override object InternalExecuteInResolutionContextUntyped(CodeActivityContext resolutionContext)
        => InternalExecuteInResolutionContext(resolutionContext);

    internal virtual TResult InternalExecuteInResolutionContext(CodeActivityContext resolutionContext)
        => throw Fx.AssertAndThrow("This should only be called on CodeActivity<T>");
}


#if DYNAMICUPDATE
namespace DynamicUpdate { }
#endif
