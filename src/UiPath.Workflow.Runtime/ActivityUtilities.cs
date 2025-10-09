// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace System.Activities;
using Expressions;
using Internals;
using Runtime;
using Validation;

internal static class ActivityUtilities
{
    private static readonly Pop popActivity = new();
    private static readonly Type activityType = typeof(Activity);
    private static readonly Type activityGenericType = typeof(Activity<>);
    private static readonly Type activityDelegateType = typeof(ActivityDelegate);
    private static readonly Type constraintType = typeof(Constraint);
    private static readonly Type variableType = typeof(Variable);
    private static readonly Type variableGenericType = typeof(Variable<>);
    private static readonly Type delegateInArgumentType = typeof(DelegateInArgument);
    private static readonly Type delegateOutArgumentType = typeof(DelegateOutArgument);
    private static readonly Type delegateInArgumentGenericType = typeof(DelegateInArgument<>);
    private static readonly Type delegateOutArgumentGenericType = typeof(DelegateOutArgument<>);
    private static readonly Type inArgumentType = typeof(InArgument);
    private static readonly Type inArgumentGenericType = typeof(InArgument<>);
    private static readonly Type inOutArgumentType = typeof(InOutArgument);
    private static readonly Type inOutArgumentGenericType = typeof(InOutArgument<>);
    private static readonly Type outArgumentType = typeof(OutArgument);
    private static readonly Type outArgumentGenericType = typeof(OutArgument<>);
    private static readonly Type argumentType = typeof(Argument);
    private static readonly Type argumentReferenceGenericType = typeof(ArgumentReference<>);
    private static readonly Type argumentValueGenericType = typeof(ArgumentValue<>);
    private static readonly Type runtimeArgumentType = typeof(RuntimeArgument);
    private static readonly Type locationGenericType = typeof(Location<>);
    private static readonly Type variableReferenceGenericType = typeof(VariableReference<>);
    private static readonly Type handleType = typeof(Handle);
    private static readonly Type iDictionaryGenericType = typeof(IDictionary<,>);
    private static readonly Type locationReferenceValueType = typeof(LocationReferenceValue<>);
    private static readonly Type environmentLocationValueType = typeof(EnvironmentLocationValue<>);
    private static readonly Type environmentLocationReferenceType = typeof(EnvironmentLocationReference<>);
    private static IList<Type> collectionInterfaces;
    private static readonly Type inArgumentOfObjectType = typeof(InArgument<object>);
    private static readonly Type outArgumentOfObjectType = typeof(OutArgument<object>);
    private static readonly Type inOutArgumentOfObjectType = typeof(InOutArgument<object>);
    private static PropertyChangedEventArgs propertyChangedEventArgs;

    // Can't delay create this one because we use object.ReferenceEquals on it in WorkflowInstance
    private static readonly ReadOnlyDictionary<string, object> emptyParameters = new(new Dictionary<string, object>(0));

    public static ReadOnlyDictionary<string, object> EmptyParameters => emptyParameters;

    internal static PropertyChangedEventArgs ValuePropertyChangedEventArgs
    {
        get
        {
            propertyChangedEventArgs ??= new PropertyChangedEventArgs("Value");
            return propertyChangedEventArgs;
        }
    }

    private static IList<Type> CollectionInterfaces
    {
        get
        {
            collectionInterfaces ??= new List<Type>(2)
                {
                    typeof(IList<>),
                    typeof(ICollection<>)
                };
            return collectionInterfaces;
        }
    }

    public static bool IsInScope(ActivityInstance potentialChild, ActivityInstance scope)
    {
        if (scope == null)
        {
            // No scope means we're in scope
            return true;
        }

        ActivityInstance walker = potentialChild;

        while (walker != null && walker != scope)
        {
            walker = walker.Parent;
        }

        return walker != null;
    }

    public static bool IsHandle(Type type) => handleType.IsAssignableFrom(type);

    public static bool IsCompletedState(ActivityInstanceState state) => state != ActivityInstanceState.Executing;

    public static bool TryGetArgumentDirectionAndType(Type propertyType, out ArgumentDirection direction, out Type argumentType)
    {
        direction = ArgumentDirection.In; // default to In
        argumentType = TypeHelper.ObjectType;  // default to object

        if (propertyType.IsGenericType)
        {
            argumentType = propertyType.GetGenericArguments()[0];

            Type genericType = propertyType.GetGenericTypeDefinition();

            if (genericType == inArgumentGenericType)
            {
                return true;
            }

            if (genericType == outArgumentGenericType)
            {
                direction = ArgumentDirection.Out;
                return true;
            }

            if (genericType == inOutArgumentGenericType)
            {
                direction = ArgumentDirection.InOut;
                return true;
            }
        }
        else
        {
            if (propertyType == inArgumentType)
            {
                return true;
            }

            if (propertyType == outArgumentType)
            {
                direction = ArgumentDirection.Out;
                return true;
            }

            if (propertyType == inOutArgumentType)
            {
                direction = ArgumentDirection.InOut;
                return true;
            }
        }

        return false;
    }

    public static bool IsArgumentType(Type propertyType) => TypeHelper.AreTypesCompatible(propertyType, argumentType);

    public static bool IsRuntimeArgumentType(Type propertyType) => TypeHelper.AreTypesCompatible(propertyType, runtimeArgumentType);

    public static bool IsArgumentDictionaryType(Type type, out Type innerType)
    {
        if (type.IsGenericType)
        {
            bool implementsIDictionary = false;
            Type dictionaryInterfaceType = null;

            if (type.GetGenericTypeDefinition() == iDictionaryGenericType)
            {
                implementsIDictionary = true;
                dictionaryInterfaceType = type;
            }
            else
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericType &&
                        interfaceType.GetGenericTypeDefinition() == iDictionaryGenericType)
                    {
                        implementsIDictionary = true;
                        dictionaryInterfaceType = interfaceType;
                        break;
                    }
                }
            }

            if (implementsIDictionary == true)
            {
                Type[] genericArguments = dictionaryInterfaceType.GetGenericArguments();
                if (genericArguments[0] == TypeHelper.StringType &&
                    IsArgumentType(genericArguments[1]))
                {
                    innerType = genericArguments[1];
                    return true;
                }
            }
        }

        innerType = null;
        return false;
    }

    public static bool IsKnownCollectionType(Type type, out Type innerType)
    {
        if (type.IsGenericType)
        {
            if (type.IsInterface)
            {
                Type localInterface = type.GetGenericTypeDefinition();
                foreach (Type knownInterface in CollectionInterfaces)
                {
                    if (localInterface == knownInterface)
                    {
                        Type[] genericArguments = type.GetGenericArguments();
                        if (genericArguments.Length == 1)
                        {
                            innerType = genericArguments[0];
                            return true;
                        }
                    }
                }
            }
            else
            {
                // Ask the type whether or not it implements any known collections.
                Type[] interfaceTypes = type.GetInterfaces();
                foreach (Type interfaceType in interfaceTypes)
                {
                    if (interfaceType.IsGenericType)
                    {
                        Type localInterface = interfaceType.GetGenericTypeDefinition();

                        foreach (Type knownInterface in CollectionInterfaces)
                        {
                            if (localInterface == knownInterface)
                            {
                                Type[] genericArguments = interfaceType.GetGenericArguments();
                                if (genericArguments.Length == 1)
                                {
                                    innerType = genericArguments[0];
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        innerType = null;
        return false;
    }

    public static bool IsActivityDelegateType(Type propertyType) => TypeHelper.AreTypesCompatible(propertyType, activityDelegateType);

    public static bool IsActivityType(Type propertyType) => IsActivityType(propertyType, true);

    public static bool IsActivityType(Type propertyType, bool includeConstraints)
    {
        if (!TypeHelper.AreTypesCompatible(propertyType, activityType))
        {
            return false;
        }

        // sometimes (for reflection analysis of Activity properties) we don't want constraints to count
        return includeConstraints || !TypeHelper.AreTypesCompatible(propertyType, constraintType);
    }

    public static bool TryGetDelegateArgumentDirectionAndType(Type propertyType, out ArgumentDirection direction, out Type argumentType)
    {
        direction = ArgumentDirection.In; // default to In
        argumentType = TypeHelper.ObjectType;  // default to object

        if (propertyType.IsGenericType)
        {
            argumentType = propertyType.GetGenericArguments()[0];

            Type genericType = propertyType.GetGenericTypeDefinition();

            if (genericType == delegateInArgumentGenericType)
            {
                return true;
            }

            if (genericType == delegateOutArgumentGenericType)
            {
                direction = ArgumentDirection.Out;
                return true;
            }
        }
        else
        {
            if (propertyType == delegateInArgumentType)
            {
                return true;
            }

            if (propertyType == delegateOutArgumentType)
            {
                direction = ArgumentDirection.Out;
                return true;
            }
        }

        return false;
    }

    public static bool IsVariableType(Type propertyType, out Type innerType)
    {
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == variableGenericType)
        {
            innerType = propertyType.GetGenericArguments()[0];
            return true;
        }

        innerType = null;
        return TypeHelper.AreTypesCompatible(propertyType, variableType);
    }

    public static bool IsVariableType(Type propertyType)
    {
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == variableGenericType)
        {
            return true;
        }

        return TypeHelper.AreTypesCompatible(propertyType, variableType);
    }

    public static bool IsLocationGenericType(Type type, out Type genericArgumentType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == locationGenericType)
        {
            genericArgumentType = type.GetGenericArguments()[0];
            return true;
        }

        genericArgumentType = null;
        return false;
    }

    public static object CreateVariableReference(Variable variable)
    {
        Type genericVariableReferenceType = variableReferenceGenericType.MakeGenericType(variable.Type);
        object variableReference = Activator.CreateInstance(genericVariableReferenceType);
        genericVariableReferenceType.GetProperty("Variable").SetValue(variableReference, variable, null);
        return variableReference;
    }

    public static ActivityWithResult CreateLocationAccessExpression(LocationReference locationReference, bool isReference, bool useLocationReferenceValue)
        => LocationAccessExpressionTypeDefinitionsCache.CreateNewLocationAccessExpression(locationReference.Type, isReference, useLocationReferenceValue, locationReference);

    public static Argument CreateArgument(Type type, ArgumentDirection direction)
    {
        Type argumentType = ArgumentTypeDefinitionsCache.GetArgumentType(type, direction);

        Argument argument = (Argument)Activator.CreateInstance(argumentType);

        return argument;
    }

    public static Argument CreateArgumentOfObject(ArgumentDirection direction)
    {
        Argument argument = direction switch
        {
            ArgumentDirection.In => (Argument)Activator.CreateInstance(inArgumentOfObjectType),
            ArgumentDirection.Out => (Argument)Activator.CreateInstance(outArgumentOfObjectType),
            _ => (Argument)Activator.CreateInstance(inOutArgumentOfObjectType),
        };
        return argument;
    }

    public static Type CreateLocation(Type locationType) => locationGenericType.MakeGenericType(locationType);

    public static Type CreateActivityWithResult(Type resultType) => activityGenericType.MakeGenericType(resultType);

    public static Argument CreateReferenceArgument(Type argumentType, ArgumentDirection direction, string referencedArgumentName)
    {
        Argument argument = Argument.Create(argumentType, direction);

        object argumentReference;

        if (direction == ArgumentDirection.In)
        {
            // If it is an In then we need an ArgumentValue<T>
            argumentReference = Activator.CreateInstance(argumentValueGenericType.MakeGenericType(argumentType), referencedArgumentName);
        }
        else
        {
            // If it is InOut or Out we need an ArgumentReference<T>
            argumentReference = Activator.CreateInstance(argumentReferenceGenericType.MakeGenericType(argumentType), referencedArgumentName);
        }

        argument.Expression = (ActivityWithResult)argumentReference;
        return argument;
    }

    public static Variable CreateVariable(string name, Type type, VariableModifiers modifiers)
    {
        Type variableType = variableGenericType.MakeGenericType(type);
        Variable variable = (Variable)Activator.CreateInstance(variableType);
        variable.Name = name;
        variable.Modifiers = modifiers;

        return variable;
    }

    // The argumentConsumer is the activity that is attempting to reference the argument
    // with argumentName.  That means that argumentConsumer must be in the Implementation
    // of an activity that defines an argument with argumentName.
    public static RuntimeArgument FindArgument(string argumentName, Activity argumentConsumer)
    {
        if (argumentConsumer.MemberOf != null && argumentConsumer.MemberOf.Owner != null)
        {
            Activity targetActivity = argumentConsumer.MemberOf.Owner;

            for (int i = 0; i < targetActivity.RuntimeArguments.Count; i++)
            {
                RuntimeArgument argument = targetActivity.RuntimeArguments[i];

                if (argument.Name == argumentName)
                {
                    return argument;
                }
            }
        }

        return null;
    }

    public static string GetDisplayName(object source)
    {
        Fx.Assert(source != null, "caller must verify");
        return GetDisplayName(source.GetType());
    }

    private static string GetDisplayName(Type sourceType)
    {
        if (sourceType.IsGenericType)
        {
            // start with the type name
            string displayName = sourceType.Name;
            int tickIndex = displayName.IndexOf('`');

            // remove the tick+number of parameters "generics format". Note that the
            // tick won't exist for nested implicitly generic classes, such as Foo`1+Bar
            if (tickIndex > 0)
            {
                displayName = displayName[..tickIndex];
            }

            // and provide a more readable version based on the closure type names
            Type[] genericArguments = sourceType.GetGenericArguments();
            StringBuilder stringBuilder = new(displayName);
            stringBuilder.Append('<');
            for (int i = 0; i < genericArguments.Length - 1; i++)
            {
                stringBuilder.AppendFormat("{0},", GetDisplayName(genericArguments[i]));
            }
            stringBuilder.AppendFormat("{0}>", GetDisplayName(genericArguments[^1]));
            return stringBuilder.ToString();
        }
        else
        {
            Fx.Assert(!sourceType.IsGenericTypeDefinition, "we have an actual object, so we should never have a generic type definition");
            return sourceType.Name;
        }
    }

    internal static void ValidateOrigin(object origin, Activity activity)
    {
        if (origin != null &&
            (origin is Activity || origin is Argument || origin is ActivityDelegate || origin is LocationReference))
        {
            activity.AddTempValidationError(new ValidationError(SR.OriginCannotBeRuntimeIntrinsic(origin)));
        }
    }

    // Returns true if there are any children
    private static void ProcessChildren(Activity parent, IList<Activity> children, ActivityCollectionType collectionType, bool addChildren, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining, ref IList<ValidationError> validationErrors)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Activity childActivity = children[i];
            if (childActivity.InitializeRelationship(parent, collectionType, ref validationErrors) && addChildren)
            {
                SetupForProcessing(childActivity, collectionType != ActivityCollectionType.Imports, ref nextActivity, ref activitiesRemaining);
            }
        }
    }

    // Note that we do not need an "isPublicCollection" parameter since all arguments are public
    // Returns true if there are any non-null expressions
    private static void ProcessArguments(Activity parent, IList<RuntimeArgument> arguments, bool addChildren, ref ActivityLocationReferenceEnvironment environment, ref int nextEnvironmentId, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining, ref IList<ValidationError> validationErrors)
    {
        if (arguments.Count <= 0)
        {
            return;
        }

        environment ??= new ActivityLocationReferenceEnvironment(parent.GetParentEnvironment());
        for (int i = 0; i < arguments.Count; i++)
        {
            RuntimeArgument argument = arguments[i];
            if (argument.InitializeRelationship(parent, ref validationErrors))
            {
                argument.Id = nextEnvironmentId;
                nextEnvironmentId++;

                // This must be called after InitializeRelationship since it makes
                // use of RuntimeArgument.Owner;
                environment.Declare(argument, argument.Owner, ref validationErrors);

                if (addChildren)
                {
                    SetupForProcessing(argument, ref nextActivity, ref activitiesRemaining);
                }
            }
        }
    }

    // Returns true if there are any non-null defaults
    private static void ProcessVariables(Activity parent, IList<Variable> variables, ActivityCollectionType collectionType, bool addChildren, ref ActivityLocationReferenceEnvironment environment, ref int nextEnvironmentId, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining, ref IList<ValidationError> validationErrors)
    {
        if (variables.Count <= 0)
        {
            return;
        }

        environment ??= new ActivityLocationReferenceEnvironment(parent.GetParentEnvironment());
        for (int i = 0; i < variables.Count; i++)
        {
            Variable variable = variables[i];
            if (variable.InitializeRelationship(parent, collectionType == ActivityCollectionType.Public, ref validationErrors))
            {
                variable.Id = nextEnvironmentId;
                nextEnvironmentId++;

                // This must be called after InitializeRelationship since it makes
                // use of Variable.Owner;
                environment.Declare(variable, variable.Owner, ref validationErrors);

                if (addChildren)
                {
                    SetupForProcessing(variable, ref nextActivity, ref activitiesRemaining);
                }
            }
        }
    }

    // Returns true if there are any non-null handlers
    private static void ProcessDelegates(Activity parent, IList<ActivityDelegate> delegates, ActivityCollectionType collectionType, bool addChildren, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining, ref IList<ValidationError> validationErrors)
    {
        for (int i = 0; i < delegates.Count; i++)
        {
            ActivityDelegate activityDelegate = delegates[i];
            if (activityDelegate.InitializeRelationship(parent, collectionType, ref validationErrors) && addChildren)
            {
                SetupForProcessing(activityDelegate, collectionType != ActivityCollectionType.Imports, ref nextActivity, ref activitiesRemaining);
            }
        }
    }

    private static void ProcessActivity(ChildActivity childActivity, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining, ActivityCallStack parentChain, ref IList<ValidationError> validationErrors, ProcessActivityTreeOptions options, ProcessActivityCallback callback)
    {
        Fx.Assert(options != null, "options should not be null.");

        if (options.CancellationToken.IsCancellationRequested)
        {
            throw FxTrace.Exception.AsError(new OperationCanceledException(options.CancellationToken));
        }

        Activity activity = childActivity.Activity;
        IList<Constraint> constraints = activity.RuntimeConstraints;
        IList<ValidationError> tempValidationErrors = null;

        Fx.Assert(validationErrors == null || !options.StoreTempViolations, "Incoming violations should be null if we are storing them in Activity.tempViolations.");

        if (!activity.HasStartedCachingMetadata)
        {
            // We need to add this activity to the IdSpace first so that we have a meaningful ID
            // for any errors that may occur.
            Fx.Assert(activity.MemberOf != null, "We always set this ahead of time - the root is set in InitializeAsRoot and all others are set in InitializeRelationship.");
            activity.MemberOf.AddMember(activity);

            if (TD.InternalCacheMetadataStartIsEnabled())
            {
                TD.InternalCacheMetadataStart(activity.Id);
            }
            activity.InternalCacheMetadata(options.CreateEmptyBindings, ref tempValidationErrors);
            if (TD.InternalCacheMetadataStopIsEnabled())
            {
                TD.InternalCacheMetadataStop(activity.Id);
            }

            ActivityValidationServices.ValidateArguments(activity, activity.Parent == null, ref tempValidationErrors);

            ActivityLocationReferenceEnvironment newPublicEnvironment = null;
            ActivityLocationReferenceEnvironment newImplementationEnvironment = new(activity.HostEnvironment)
            {
                InternalRoot = activity
            };

            int nextEnvironmentId = 0;
            bool shouldProcessChildren = !options.SkipPrivateChildren || (activity is DynamicActivity);

            ProcessChildren(activity, activity.Children, ActivityCollectionType.Public, true, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);
            ProcessChildren(activity, activity.ImportedChildren, ActivityCollectionType.Imports, true, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);
            ProcessChildren(activity, activity.ImplementationChildren, ActivityCollectionType.Implementation, shouldProcessChildren, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);

            ProcessArguments(activity, activity.RuntimeArguments, true, ref newImplementationEnvironment, ref nextEnvironmentId, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);

            ProcessVariables(activity, activity.RuntimeVariables, ActivityCollectionType.Public, true, ref newPublicEnvironment, ref nextEnvironmentId, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);
            ProcessVariables(activity, activity.ImplementationVariables, ActivityCollectionType.Implementation, true, ref newImplementationEnvironment, ref nextEnvironmentId, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);

            if (activity.HandlerOf != null)
            {
                // Since we are a delegate handler we have to do some processing
                // of the handlers parameters.  This is the one part of the tree
                // walk that actually reaches _up_ to process something we've
                // already passed.

                for (int i = 0; i < activity.HandlerOf.RuntimeDelegateArguments.Count; i++)
                {
                    RuntimeDelegateArgument delegateArgument = activity.HandlerOf.RuntimeDelegateArguments[i];
                    DelegateArgument boundArgument = delegateArgument.BoundArgument;
                    if (boundArgument != null)
                    {
                        // At runtime, delegate arguments end up owned by the Handler
                        // and are scoped like public variables of the handler.
                        //
                        // And since they don't own an expression, there's no equivalent
                        // SetupForProcessing method for DelegateArguments
                        if (boundArgument.InitializeRelationship(activity, ref tempValidationErrors))
                        {
                            boundArgument.Id = nextEnvironmentId;
                            nextEnvironmentId++;
                        }
                    }
                }
            }

            // NOTE: At this point the declared environment is complete (either we're using the parent or we've got a new one)
            if (newPublicEnvironment == null)
            {
                activity.PublicEnvironment = new ActivityLocationReferenceEnvironment(activity.GetParentEnvironment());
            }
            else
            {
                if (newPublicEnvironment.Parent == null)
                {
                    newPublicEnvironment.InternalRoot = activity;
                }

                activity.PublicEnvironment = newPublicEnvironment;
            }

            activity.ImplementationEnvironment = newImplementationEnvironment;

            // ProcessDelegates uses activity.Environment
            ProcessDelegates(activity, activity.Delegates, ActivityCollectionType.Public, true, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);
            ProcessDelegates(activity, activity.ImportedDelegates, ActivityCollectionType.Imports, true, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);
            ProcessDelegates(activity, activity.ImplementationDelegates, ActivityCollectionType.Implementation, true, ref nextActivity, ref activitiesRemaining, ref tempValidationErrors);

            callback?.Invoke(childActivity, parentChain);

            // copy validation errors in ValidationErrors list
            if (tempValidationErrors != null)
            {
                validationErrors ??= new List<ValidationError>();
                string prefix = ActivityValidationServices.GenerateValidationErrorPrefix(childActivity.Activity, parentChain, options, out Activity source);

                for (int i = 0; i < tempValidationErrors.Count; i++)
                {
                    ValidationError validationError = tempValidationErrors[i];

                    validationError.Source = source;
                    validationError.Id = source.Id;

                    if (!string.IsNullOrEmpty(prefix))
                    {
                        validationError.Message = prefix + validationError.Message;
                    }

                    validationErrors.Add(validationError);
                }

                tempValidationErrors = null;
            }

            if (options.StoreTempViolations)
            {
                if (validationErrors != null)
                {
                    childActivity.Activity.SetTempValidationErrorCollection(validationErrors);
                    validationErrors = null;
                }
            }
        }
        else
        {
            // We're processing a reference


            // Add all the children for processing even though they've already
            // been seen.
            SetupForProcessing(activity.Children, true, ref nextActivity, ref activitiesRemaining);
            SetupForProcessing(activity.ImportedChildren, false, ref nextActivity, ref activitiesRemaining);

            SetupForProcessing(activity.RuntimeArguments, ref nextActivity, ref activitiesRemaining);

            SetupForProcessing(activity.RuntimeVariables, ref nextActivity, ref activitiesRemaining);

            SetupForProcessing(activity.Delegates, true, ref nextActivity, ref activitiesRemaining);
            SetupForProcessing(activity.ImportedDelegates, false, ref nextActivity, ref activitiesRemaining);

            if (!options.SkipPrivateChildren)
            {
                SetupForProcessing(activity.ImplementationChildren, true, ref nextActivity, ref activitiesRemaining);
                SetupForProcessing(activity.ImplementationDelegates, true, ref nextActivity, ref activitiesRemaining);
                SetupForProcessing(activity.ImplementationVariables, ref nextActivity, ref activitiesRemaining);
            }

            if (callback != null && !options.OnlyCallCallbackForDeclarations)
            {
                callback(childActivity, parentChain);
            }

            if (childActivity.Activity.HasTempViolations && !options.StoreTempViolations)
            {
                childActivity.Activity.TransferTempValidationErrors(ref validationErrors);
            }
        }

        // We only run constraints if the activity could possibly
        // execute and we aren't explicitly skipping them.
        if (!options.SkipConstraints && parentChain.WillExecute && childActivity.CanBeExecuted && constraints.Count > 0)
        {
            ActivityValidationServices.RunConstraints(childActivity, parentChain, constraints, options, false, ref validationErrors);
        }
    }

    // We explicitly call this CacheRootMetadata since it treats the provided
    // activity as the root of the tree.
    public static void CacheRootMetadata(Activity activity, LocationReferenceEnvironment hostEnvironment, ProcessActivityTreeOptions options, ProcessActivityCallback callback, ref IList<ValidationError> validationErrors)
    {
        if (TD.CacheRootMetadataStartIsEnabled())
        {
            TD.CacheRootMetadataStart(activity.DisplayName);
        }
        if (!ShouldShortcut(activity, options))
        {
            lock (activity.ThisLock)
            {
                if (!ShouldShortcut(activity, options))
                {
                    if (activity.HasBeenAssociatedWithAnInstance)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RootActivityAlreadyAssociatedWithInstance(activity.DisplayName)));
                    }

                    activity.InitializeAsRoot(hostEnvironment);

                    ProcessActivityTreeCore(new ChildActivity(activity, true), null, options, callback, ref validationErrors);

                    // Regardless of where the violations came from we only want to
                    // set ourselves RuntimeReady if there are no errors and are
                    // fully cached.
                    if (!ActivityValidationServices.HasErrors(validationErrors) && options.IsRuntimeReadyOptions)
                    {
                        // We don't really support progressive caching at runtime so we only set ourselves
                        // as runtime ready if we cached the whole workflow and created empty bindings.
                        // In order to support progressive caching we need to deal with the following
                        // issues:
                        //   * We need a mechanism for supporting activities which supply extensions
                        //   * We need to understand when we haven't created empty bindings so that
                        //     we can progressively create them
                        //   * We need a mechanism for making sure that we've validated parent related
                        //     constraints at all possible callsites
                        activity.SetRuntimeReady();
                    }
                }
            }
        }
        if (TD.CacheRootMetadataStopIsEnabled())
        {
            TD.CacheRootMetadataStop(activity.DisplayName);
        }
    }

    // This API is only valid from ProcessActivityCallbacks.  It will cache the rest of the subtree rooted at the
    // provided activity allowing inspection of child metadata before the normal caching pass hits it.
    public static void FinishCachingSubtree(ChildActivity subtreeRoot, ActivityCallStack parentChain, ProcessActivityTreeOptions options)
    {
        IList<ValidationError> discardedValidationErrors = null;
        ProcessActivityTreeCore(subtreeRoot, parentChain, ProcessActivityTreeOptions.GetFinishCachingSubtreeOptions(options), new ProcessActivityCallback(NoOpCallback), ref discardedValidationErrors);
    }

    public static void FinishCachingSubtree(ChildActivity subtreeRoot, ActivityCallStack parentChain, ProcessActivityTreeOptions options, ProcessActivityCallback callback)
    {
        IList<ValidationError> discardedValidationErrors = null;
        ProcessActivityTreeCore(subtreeRoot, parentChain, ProcessActivityTreeOptions.GetFinishCachingSubtreeOptions(options), callback, ref discardedValidationErrors);
    }

    private static void NoOpCallback(ChildActivity element, ActivityCallStack parentChain) { }

    private static bool ShouldShortcut(Activity activity, ProcessActivityTreeOptions options) => options.SkipIfCached && options.IsRuntimeReadyOptions && activity.IsRuntimeReady;

    internal static void ProcessActivityTreeCore(ChildActivity currentActivity, ActivityCallStack parentChain, ProcessActivityTreeOptions options, ProcessActivityCallback callback, ref IList<ValidationError> validationErrors)
    {
        Fx.Assert(options != null, "We need you to explicitly specify options.");
        Fx.Assert(currentActivity.Activity.MemberOf != null, "We must have an activity with MemberOf setup or we need to skipIdGeneration.");

        ChildActivity nextActivity = ChildActivity.Empty;
        Stack<ChildActivity> activitiesRemaining = null;

        parentChain ??= new ActivityCallStack();

        if (options.OnlyVisitSingleLevel)
        {
            ProcessActivity(currentActivity, ref nextActivity, ref activitiesRemaining, parentChain, ref validationErrors, options, callback);
        }
        else
        {
            while (!currentActivity.Equals(ChildActivity.Empty))
            {
                if (ReferenceEquals(currentActivity.Activity, popActivity))
                {
                    ChildActivity completedParent = parentChain.Pop();
                    completedParent.Activity.SetCached(isSkippingPrivateChildren: options.SkipPrivateChildren);
                }
                else
                {
                    SetupForProcessing(popActivity, true, ref nextActivity, ref activitiesRemaining);
                    ProcessActivity(currentActivity, ref nextActivity, ref activitiesRemaining, parentChain, ref validationErrors, options, callback);
                    parentChain.Push(currentActivity);
                }

                // nextActivity is the top of the stack
                //    stackTop => nextActivity => currentActivity
                currentActivity = nextActivity;

                if (activitiesRemaining != null && activitiesRemaining.Count > 0)
                {
                    nextActivity = activitiesRemaining.Pop();
                }
                else
                {
                    nextActivity = ChildActivity.Empty;
                }
            }
        }
    }

    private static void SetupForProcessing(IList<Activity> children, bool canBeExecuted, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        for (int i = 0; i < children.Count; i++)
        {
            SetupForProcessing(children[i], canBeExecuted, ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(IList<ActivityDelegate> delegates, bool canBeExecuted, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        for (int i = 0; i < delegates.Count; i++)
        {
            SetupForProcessing(delegates[i], canBeExecuted, ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(IList<Variable> variables, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        for (int i = 0; i < variables.Count; i++)
        {
            SetupForProcessing(variables[i], ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(IList<RuntimeArgument> arguments, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            SetupForProcessing(arguments[i], ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(ActivityDelegate activityDelegate, bool canBeExecuted, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        if (activityDelegate.Handler != null)
        {
            SetupForProcessing(activityDelegate.Handler, canBeExecuted, ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(Variable variable, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        if (variable.Default != null)
        {
            SetupForProcessing(variable.Default, true, ref nextActivity, ref activitiesRemaining);
        }
    }

    private static void SetupForProcessing(RuntimeArgument argument, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        if (argument.BoundArgument != null && !argument.BoundArgument.IsEmpty)
        {
            SetupForProcessing(argument.BoundArgument.Expression, true, ref nextActivity, ref activitiesRemaining);
        }
    }

    // nextActivity is always the top of the stack
    private static void SetupForProcessing(Activity activity, bool canBeExecuted, ref ChildActivity nextActivity, ref Stack<ChildActivity> activitiesRemaining)
    {
        if (!nextActivity.Equals(ChildActivity.Empty))
        {
            activitiesRemaining ??= new Stack<ChildActivity>();
            activitiesRemaining.Push(nextActivity);
        }

        nextActivity = new ChildActivity(activity, canBeExecuted);
    }

    public static void ProcessActivityInstanceTree(ActivityInstance rootInstance, ActivityExecutor executor, Func<ActivityInstance, ActivityExecutor, bool> callback)
    {
        Queue<IList<ActivityInstance>> instancesRemaining = null;

        TreeProcessingList currentInstancesList = new();
        currentInstancesList.Add(rootInstance);

        TreeProcessingList nextInstanceList = null;
        if (rootInstance.HasChildren)
        {
            nextInstanceList = new TreeProcessingList();
        }

        while ((instancesRemaining != null && instancesRemaining.Count > 0)
            || currentInstancesList.Count != 0)
        {
            if (currentInstancesList.Count == 0)
            {
                Fx.Assert(instancesRemaining != null && instancesRemaining.Count > 0, "This must be the clause that caused us to enter");
                currentInstancesList.Set(instancesRemaining.Dequeue());
            }

            for (int i = 0; i < currentInstancesList.Count; i++)
            {
                ActivityInstance instance = currentInstancesList[i];

                if (callback(instance, executor) && instance.HasChildren)
                {
                    Fx.Assert(nextInstanceList != null, "We should have created this list if we are going to get here.");
                    instance.AppendChildren(nextInstanceList, ref instancesRemaining);
                }
            }

            if (nextInstanceList != null && nextInstanceList.Count > 0)
            {
                nextInstanceList.TransferTo(currentInstancesList);
            }
            else
            {
                // We'll just reuse this object on the next pass (Set will be called)
                currentInstancesList.Reset();
            }
        }
    }

    public delegate void ProcessActivityCallback(ChildActivity childActivity, ActivityCallStack parentChain);

    public static FaultBookmark CreateFaultBookmark(FaultCallback onFaulted, ActivityInstance owningInstance)
        => onFaulted == null ? null : new FaultBookmark(new FaultCallbackWrapper(onFaulted, owningInstance));

    public static CompletionBookmark CreateCompletionBookmark(CompletionCallback onCompleted, ActivityInstance owningInstance)
        => onCompleted == null ? null : new CompletionBookmark(new ActivityCompletionCallbackWrapper(onCompleted, owningInstance));

    public static CompletionBookmark CreateCompletionBookmark(DelegateCompletionCallback onCompleted, ActivityInstance owningInstance)
    {
        if (onCompleted != null)
        {
            return new CompletionBookmark(new DelegateCompletionCallbackWrapper(onCompleted, owningInstance));
        }
        return null;
    }

    public static CompletionBookmark CreateCompletionBookmark<TResult>(CompletionCallback<TResult> onCompleted, ActivityInstance owningInstance)
    {
        if (onCompleted != null)
        {
            return new CompletionBookmark(new FuncCompletionCallbackWrapper<TResult>(onCompleted, owningInstance));
        }

        return null;
    }

    public static string GetTraceString(Bookmark bookmark)
    {
        if (bookmark.IsNamed)
        {
            return "'" + bookmark.Name + "'";
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, "<Unnamed Id={0}>", bookmark.Id);
        }
    }

    public static string GetTraceString(BookmarkScope bookmarkScope)
    {
        if (bookmarkScope == null)
        {
            return "<None>";
        }
        else if (bookmarkScope.IsInitialized)
        {
            return "'" + bookmarkScope.Id.ToString() + "'";
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, "<Uninitialized TemporaryId={0}>", bookmarkScope.TemporaryId);
        }
    }

    public static void RemoveNulls(IList list)
    {
        if (list != null)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }

    public static void Add<T>(ref Collection<T> collection, T data)
    {
        if (data != null)
        {
            collection ??= new Collection<T>();
            collection.Add(data);
        }
    }

    public static void Add<T>(ref IList<T> list, T data)
    {
        if (data != null)
        {
            list ??= new List<T>();
            list.Add(data);
        }
    }

    public class TreeProcessingList
    {
        private ActivityInstance _singleItem;
        private IList<ActivityInstance> _multipleItems;
        private bool _addRequiresNewList;

        public TreeProcessingList() { }

        public int Count
        {
            get
            {
                if (_singleItem != null)
                {
                    return 1;
                }

                if (_multipleItems != null)
                {
                    return _multipleItems.Count;
                }

                return 0;
            }
        }

        public ActivityInstance this[int index]
        {
            get
            {
                if (_singleItem != null)
                {
                    Fx.Assert(index == 0, "We expect users of TreeProcessingList never to be out of range.");
                    return _singleItem;
                }
                else
                {
                    Fx.Assert(_multipleItems != null, "Users shouldn't call this if we have no items.");
                    Fx.Assert(_multipleItems.Count > index, "Users should never be out of range.");

                    return _multipleItems[index];
                }
            }
        }

        public void Set(IList<ActivityInstance> listToSet)
        {
            Fx.Assert(_singleItem == null && (_multipleItems == null || _multipleItems.Count == 0), "We should not have any items if calling set.");

            _multipleItems = listToSet;
            _addRequiresNewList = true;
        }

        public void Add(ActivityInstance item)
        {
            if (_multipleItems != null)
            {
                if (_addRequiresNewList)
                {
                    _multipleItems = new List<ActivityInstance>(_multipleItems);
                    _addRequiresNewList = false;
                }

                _multipleItems.Add(item);
            }
            else if (_singleItem != null)
            {
                _multipleItems = new List<ActivityInstance>(2)
                {
                    _singleItem,
                    item
                };
                _singleItem = null;
            }
            else
            {
                _singleItem = item;
            }
        }

        // Because of how we use this we don't need a Clear().
        // Basically we gain nothing by clearing the multipleItems
        // list and hanging onto it.
        public void Reset()
        {
            _addRequiresNewList = false;
            _multipleItems = null;
            _singleItem = null;
        }

        public void TransferTo(TreeProcessingList otherList)
        {
            otherList._singleItem = _singleItem;
            otherList._multipleItems = _multipleItems;
            otherList._addRequiresNewList = _addRequiresNewList;

            Reset();
        }
    }

    // We don't implement anything in this class.  We just use it as
    // a placeholder for when to pop off our parent stack.
    private class Pop : Activity
    {
        internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
            => throw Fx.AssertAndThrow("should never get here");

        internal override void OnInternalCacheMetadata(bool createEmptyBindings)
            => throw Fx.AssertAndThrow("should never get here");
    }

    public struct ChildActivity : IEquatable<ChildActivity>
    {
        public ChildActivity(Activity activity, bool canBeExecuted)
            : this()
        {
            Activity = activity;
            CanBeExecuted = canBeExecuted;
        }

        public static ChildActivity Empty => new();

        public Activity Activity { get; set; }

        public bool CanBeExecuted { get; set; }

        public bool Equals(ChildActivity other)
            => ReferenceEquals(Activity, other.Activity) && CanBeExecuted == other.CanBeExecuted;

        public override bool Equals(object obj) => obj is ChildActivity activity && Equals(activity);

        public override int GetHashCode() => Activity.GetHashCode();
    }

    public class ActivityCallStack
    {
        private int _nonExecutingParentCount;
        private readonly Quack<ChildActivity> _callStack;

        public ActivityCallStack()
        {
            _callStack = new Quack<ChildActivity>();
        }

        public bool WillExecute => _nonExecutingParentCount == 0;

        public ChildActivity this[int index] => _callStack[index];

        public int Count => _callStack.Count;

        public void Push(ChildActivity childActivity)
        {
            if (!childActivity.CanBeExecuted)
            {
                _nonExecutingParentCount++;
            }

            _callStack.PushFront(childActivity);
        }

        public ChildActivity Pop()
        {
            ChildActivity childActivity = _callStack.Dequeue();

            if (!childActivity.CanBeExecuted)
            {
                _nonExecutingParentCount--;
            }

            return childActivity;
        }
    }

    private static class ArgumentTypeDefinitionsCache
    {
        private static readonly Hashtable inArgumentTypeDefinitions = new();
        private static readonly Hashtable outArgumentTypeDefinitions = new();
        private static readonly Hashtable inOutArgumentTypeDefinitions = new();

        public static Type GetArgumentType(Type type, ArgumentDirection direction)
        {
            Hashtable lookupTable = direction switch
            {
                ArgumentDirection.In => inArgumentTypeDefinitions,
                ArgumentDirection.Out => outArgumentTypeDefinitions,
                _ => inOutArgumentTypeDefinitions,
            };
            Type argumentType = lookupTable[type] as Type;
            if (argumentType == null)
            {
                argumentType = CreateArgumentType(type, direction);
                lock (lookupTable)
                {
                    lookupTable[type] = argumentType;
                }
            }

            return argumentType;
        }

        private static Type CreateArgumentType(Type type, ArgumentDirection direction)
        {
            Type argumentType = direction switch
            {
                ArgumentDirection.In => inArgumentGenericType.MakeGenericType(type),
                ArgumentDirection.Out => outArgumentGenericType.MakeGenericType(type),
                _ => inOutArgumentGenericType.MakeGenericType(type),
            };
            return argumentType;
        }
    }

    private static class LocationAccessExpressionTypeDefinitionsCache
    {
        private static readonly object locationReferenceValueTypeDefinitionsLock = new();
        private static readonly Dictionary<Type, ILocationReferenceExpression> locationReferenceValueTypeDefinitions = new();
        private static readonly object environmentLocationReferenceTypeDefinitionsLock = new();
        private static readonly Dictionary<Type, ILocationReferenceExpression> environmentLocationReferenceTypeDefinitions = new();
        private static readonly object environmentLocationValueTypeDefinitionsLock = new();
        private static readonly Dictionary<Type, ILocationReferenceExpression> environmentLocationValueTypeDefinitions = new();

        public static ActivityWithResult CreateNewLocationAccessExpression(Type type, bool isReference, bool useLocationReferenceValue, LocationReference locationReference)
        {
            Dictionary<Type, ILocationReferenceExpression> lookupTable;
            object tableLock;
            if (useLocationReferenceValue)
            {
                lookupTable = locationReferenceValueTypeDefinitions;
                tableLock = locationReferenceValueTypeDefinitionsLock;
            }
            else
            {
                lookupTable = isReference ? environmentLocationReferenceTypeDefinitions : environmentLocationValueTypeDefinitions;
                tableLock = isReference ? environmentLocationReferenceTypeDefinitionsLock : environmentLocationValueTypeDefinitionsLock;
            }

            ILocationReferenceExpression existingInstance;
            lock (tableLock)
            {
                if (!lookupTable.TryGetValue(type, out existingInstance))
                {
                    Type locationAccessExpressionType = CreateLocationAccessExpressionType(type, isReference, useLocationReferenceValue);

                    // Create an "empty" (locationReference = null) instance to put in the cache. This empty instance will only be used to create other instances,
                    // including the instance returned from this method. The cached instance will never be included in an activity tree, so the cached instance's
                    // rootActivity field will not be filled in and thus will not pin all the objects in the activity tree. The cached empty instance has a null
                    // locationReference because locationReference also pins parts of activity tree.
                    existingInstance = (ILocationReferenceExpression)Activator.CreateInstance(
                        locationAccessExpressionType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);

                    lookupTable[type] = existingInstance;
                }
            }

            return existingInstance.CreateNewInstance(locationReference);
        }

        private static Type CreateLocationAccessExpressionType(Type type, bool isReference, bool useLocationReferenceValue)
        {
            Type openType;
            if (useLocationReferenceValue)
            {
                openType = locationReferenceValueType;
            }
            else
            {
                openType = isReference ? environmentLocationReferenceType : environmentLocationValueType;
            }

            return openType.MakeGenericType(type);
        }
    }
}
