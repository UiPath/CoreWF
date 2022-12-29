// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

/// <summary>
/// This class represents a StateMachine which contains States and Variables.
/// </summary>
[ContentProperty("States")]
public sealed class StateMachine : NativeActivity
{
    // internal Id of StateMachine. it's a constant value and states of state machine will generate their ids based on this root id.
    private const string RootId = "0";
    private const string ExitProperty = "Exit";

    // states in root level of StateMachine
    private Collection<State> _states;

    // variables used in StateMachine
    private Collection<Variable> _variables;

    // internal representations of states
    private readonly Collection<InternalState> _internalStates;

    // ActivityFuncs who call internal activities
    private readonly Collection<ActivityFunc<StateMachineEventManager, string>> _internalStateFuncs;

    // Callback when a state completes
    private readonly CompletionCallback<string> _onStateComplete;

    // eventManager is used to manage the events of trigger completion.
    // When a trigger on a transition is completed, the corresponding event will be sent to eventManager.
    // eventManager will decide whether immediate process it or just register it.
    private readonly Variable<StateMachineEventManager> _eventManager;

    /// <summary>
    /// It's constructor.
    /// </summary>
    public StateMachine()
    {
        _internalStates = new Collection<InternalState>();
        _internalStateFuncs = new Collection<ActivityFunc<StateMachineEventManager, string>>();
        _eventManager = new Variable<StateMachineEventManager> { Name = "EventManager", Default = new StateMachineEventManagerFactory() };
        _onStateComplete = new CompletionCallback<string>(OnStateComplete);
    }

    /// <summary>
    /// Gets or sets the start point of the StateMachine.
    /// </summary>
    [DefaultValue(null)]
    public State InitialState { get; set; }

    /// <summary>
    /// Gets all root level States in the StateMachine.
    /// </summary>
    [DependsOn("InitialState")]
    public Collection<State> States
    {
        get
        {
            _states ??= new ValidatingCollection<State>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(item)));
                    }
                },
            };
            return _states;
        }
    }

    /// <summary>
    /// Gets Variables which can be used within StateMachine scope.
    /// </summary>
    [DependsOn("States")]
    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(item)));
                    }
                },
            };
            return _variables;
        }
    }

    private uint PassNumber { get; set; }

    /// <summary>
    /// Perform State Machine validation, in the following order:
    /// 1. Mark all states in States collection with an Id.
    /// 2. Traverse all states via declared transitions, and mark reachable states.
    /// 3. Validate transitions, states, and state machine
    /// Finally, declare arguments and variables of state machine, and declare states and transitions as activitydelegates.
    /// </summary>
    /// <param name="metadata">NativeActivityMetadata reference</param>
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // cleanup
        _internalStateFuncs.Clear();
        _internalStates.Clear();

        // clear Ids and Flags via transitions
        PassNumber++;
        TraverseViaTransitions(ClearState, ClearTransition);

        // clear Ids and Flags of all containing State references.
        PassNumber++;
        TraverseStates(
            (NativeActivityMetadata m, Collection<State> states) => { ClearStates(states); },
            (NativeActivityMetadata m, State state) => { ClearTransitions(state); },
            metadata,
            checkReached: false);

        // Mark via states and do some check
        PassNumber++;
        TraverseStates(
            MarkStatesViaChildren,
            (NativeActivityMetadata m, State state) => { MarkTransitionsInState(state); },
            metadata,
            checkReached: false);

        PassNumber++;

        // Mark via transition
        TraverseViaTransitions(delegate (State state) { MarkStateViaTransition(state); }, null);

        // Do validation via children
        // need not check the violation of state which is not reached
        PassNumber++;

        TraverseViaTransitions(
            (State state) =>
            {
                ValidateTransitions(metadata, state);
            },
            actionForTransition: null);

        PassNumber++;

        TraverseStates(
            ValidateStates,
            (NativeActivityMetadata m, State state) =>
            {
                if (!state.Reachable)
                {
                    // log validation for states that are not reachable in the previous pass.
                    ValidateTransitions(m, state);
                }
            },
            metadata: metadata,
            checkReached: true);

        // Validate the root state machine itself
        ValidateStateMachine(metadata);
        ProcessStates(metadata);

        metadata.AddImplementationVariable(_eventManager);
        foreach (Variable variable in Variables)
        {
            metadata.AddVariable(variable);
        }

        StateMachineExtension.Install(metadata);
    }

    /// <summary>
    /// Execution of StateMachine
    /// </summary>
    /// <param name="context">NativeActivityContext reference</param>
    protected override void Execute(NativeActivityContext context)
    {
        // We view the duration before moving to initial state is on transition.
        StateMachineEventManager localEventManager = _eventManager.Get(context);
        localEventManager.OnTransition = true;
        localEventManager.CurrentBeingProcessedEvent = null;
        int index = StateMachineIdHelper.GetChildStateIndex(RootId, InitialState.StateId);

        context.ScheduleFunc(
            _internalStateFuncs[index],
            localEventManager,
            _onStateComplete);
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        // enable dynamic update in state machine
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    private static void MarkTransitionsInState(State state)
    {
        if (state.Transitions.Count > 0)
        {
            for (int i = 0; i < state.Transitions.Count; i++)
            {
                Transition transition = state.Transitions[i];
                if (!string.IsNullOrEmpty(state.StateId))
                {
                    transition.Id = StateMachineIdHelper.GenerateTransitionId(state.StateId, i);
                }
            }
        }
    }

    private static void MarkStateViaTransition(State state) => state.Reachable = true;

    private static void ClearStates(Collection<State> states)
    {
        foreach (State state in states)
        {
            ClearState(state);
        }
    }

    private static void ClearState(State state)
    {
        state.StateId = null;
        state.Reachable = false;
        state.ClearInternalState();
    }

    private static void ClearTransitions(State state)
    {
        foreach (Transition transition in state.Transitions)
        {
            ClearTransition(transition);
        }
    }

    private static void ClearTransition(Transition transition) => transition.Source = null;

    private static void ValidateStates(NativeActivityMetadata metadata, Collection<State> states)
    {
        foreach (State state in states)
        {
            // only validate reached state.
            ValidateState(metadata, state);
        }
    }

    private static void ValidateState(NativeActivityMetadata metadata, State state)
    {
        Fx.Assert(!string.IsNullOrEmpty(state.StateId), "StateId should have been set.");
        if (state.Reachable)
        {
            if (state.IsFinal)
            {
                if (state.Exit != null)
                {
                    metadata.AddValidationError(new ValidationError(
                        SR.FinalStateCannotHaveProperty(state.DisplayName, ExitProperty),
                        isWarning: false,
                        propertyName: string.Empty,
                        sourceDetail: state));
                }

                if (state.Transitions.Count > 0)
                {
                    metadata.AddValidationError(new ValidationError(
                        SR.FinalStateCannotHaveTransition(state.DisplayName),
                        isWarning: false,
                        propertyName: string.Empty,
                        sourceDetail: state));
                }
            }
            else
            {
                if (state.Transitions.Count == 0)
                {
                    metadata.AddValidationError(new ValidationError(
                        SR.SimpleStateMustHaveOneTransition(state.DisplayName),
                        isWarning: false,
                        propertyName: string.Empty,
                        sourceDetail: state));
                }
            }
        }
    }

    private static void ValidateTransitions(NativeActivityMetadata metadata, State currentState)
    {
        Collection<Transition> transitions = currentState.Transitions;
        HashSet<Activity> conditionalTransitionTriggers = new();
        Dictionary<Activity, List<Transition>> unconditionalTransitionMapping = new();

        foreach (Transition transition in transitions)
        {
            if (transition.Source != null)
            {
                metadata.AddValidationError(new ValidationError(
                    SR.TransitionCannotBeAddedTwice(transition.DisplayName, currentState.DisplayName, transition.Source.DisplayName),
                    isWarning: false,
                    propertyName: string.Empty,
                    sourceDetail: transition));
                continue;
            }
            else
            {
                transition.Source = currentState;
            }

            if (transition.To == null)
            {
                metadata.AddValidationError(new ValidationError(
                    SR.TransitionTargetCannotBeNull(transition.DisplayName, currentState.DisplayName),
                    isWarning: false,
                    propertyName: string.Empty,
                    sourceDetail: transition));
            }
            else if (string.IsNullOrEmpty(transition.To.StateId))
            {
                metadata.AddValidationError(new ValidationError(
                    SR.StateNotBelongToAnyParent(
                        transition.DisplayName,
                        transition.To.DisplayName),
                    isWarning: false,
                    propertyName: string.Empty,
                    sourceDetail: transition));
            }

            Activity triggerActivity = transition.ActiveTrigger;

            if (transition.Condition == null)
            {
                if (!unconditionalTransitionMapping.ContainsKey(triggerActivity))
                {
                    unconditionalTransitionMapping.Add(triggerActivity, new List<Transition>());
                }

                unconditionalTransitionMapping[triggerActivity].Add(transition);
            }
            else
            {
                conditionalTransitionTriggers.Add(triggerActivity);
            }
        }

        foreach (KeyValuePair<Activity, List<Transition>> unconditionalTransitions in unconditionalTransitionMapping)
        {
            if (conditionalTransitionTriggers.Contains(unconditionalTransitions.Key) ||
                unconditionalTransitions.Value.Count > 1)
            {
                foreach (Transition transition in unconditionalTransitions.Value)
                {
                    if (transition.Trigger != null)
                    {
                        metadata.AddValidationError(new ValidationError(
                            SR.UnconditionalTransitionShouldNotShareTriggersWithOthers(
                                transition.DisplayName,
                                currentState.DisplayName,
                                transition.Trigger.DisplayName),
                            isWarning: false,
                            propertyName: string.Empty,
                            sourceDetail: currentState));
                    }
                    else
                    {
                        // Null Trigger
                        metadata.AddValidationError(new ValidationError(
                            SR.UnconditionalTransitionShouldNotShareNullTriggersWithOthers(
                                transition.DisplayName,
                                currentState.DisplayName),
                            isWarning: false,
                            propertyName: string.Empty,
                            sourceDetail: currentState));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Create internal states
    /// </summary>
    /// <param name="metadata">NativeActivityMetadata reference.</param>
    private void ProcessStates(NativeActivityMetadata metadata)
    {
        // remove duplicate state in the collection during evaluation
        IEnumerable<State> distinctStates = _states.Distinct();

        foreach (State state in distinctStates)
        {
            InternalState internalState = state.InternalState;
            _internalStates.Add(internalState);

            DelegateInArgument<StateMachineEventManager> eventManager = new();
            internalState.EventManager = eventManager;

            ActivityFunc<StateMachineEventManager, string> activityFunc = new()
            {
                Argument = eventManager,
                Handler = internalState,
            };

            if (state.Reachable)
            {
                // If this state is not reached, we should not add it as child because it's even not well validated.
                metadata.AddDelegate(activityFunc, /* origin = */ state);
            }

            _internalStateFuncs.Add(activityFunc);
        }
    }

    private void OnStateComplete(NativeActivityContext context, ActivityInstance completedInstance, string result)
    {
        if (StateMachineIdHelper.IsAncestor(RootId, result))
        {
            int index = StateMachineIdHelper.GetChildStateIndex(RootId, result);
            context.ScheduleFunc(
                _internalStateFuncs[index],
                _eventManager.Get(context),
                _onStateComplete);
        }
    }

    private void ValidateStateMachine(NativeActivityMetadata metadata)
    {
        if (InitialState == null)
        {
            metadata.AddValidationError(SR.StateMachineMustHaveInitialState(DisplayName));
        }
        else
        {
            if (InitialState.IsFinal)
            {
                Fx.Assert(!string.IsNullOrEmpty(InitialState.StateId), "StateId should have get set on the initialState.");
                metadata.AddValidationError(new ValidationError(
                    SR.InitialStateCannotBeFinalState(InitialState.DisplayName),
                    isWarning: false,
                    propertyName: string.Empty,
                    sourceDetail: InitialState));
            }

            if (!States.Contains(InitialState))
            {
                Fx.Assert(string.IsNullOrEmpty(InitialState.StateId), "Initial state would not have an id because it is not in the States collection.");
                metadata.AddValidationError(SR.InitialStateNotInStatesCollection(InitialState.DisplayName));
            }
        }
    }

    private void TraverseStates(
        Action<NativeActivityMetadata, Collection<State>> actionForStates,
        Action<NativeActivityMetadata, State> actionForTransitions,
        NativeActivityMetadata metadata,
        bool checkReached)
    {
        actionForStates?.Invoke(metadata, States);

        uint passNumber = PassNumber;

        IEnumerable<State> distinctStates = States.Distinct();
        foreach (State state in distinctStates)
        {
            if (!checkReached || state.Reachable)
            {
                state.PassNumber = passNumber;

                actionForTransitions?.Invoke(metadata, state);
            }
        }
    }

    private void MarkStatesViaChildren(NativeActivityMetadata metadata, Collection<State> states)
    {
        if (states.Count > 0)
        {
            for (int i = 0; i < states.Count; i++)
            {
                State state = states[i];

                if (string.IsNullOrEmpty(state.StateId))
                {
                    state.StateId = StateMachineIdHelper.GenerateStateId(RootId, i);
                    state.StateMachineName = DisplayName;
                }
                else
                {
                    // the state has been makred already: a duplicate state is found
                    metadata.AddValidationError(new ValidationError(
                    SR.StateCannotBeAddedTwice(state.DisplayName),
                        isWarning: false,
                        propertyName: string.Empty,
                        sourceDetail: state));
                }
            }
        }
    }

    private void TraverseViaTransitions(Action<State> actionForState, Action<Transition> actionForTransition)
    {
        Stack<State> stack = new();
        stack.Push(InitialState);
        uint passNumber = PassNumber;
        while (stack.Count > 0)
        {
            State currentState = stack.Pop();
            if (currentState == null || currentState.PassNumber == passNumber)
            {
                continue;
            }

            currentState.PassNumber = passNumber;

            actionForState?.Invoke(currentState);

            foreach (Transition transition in currentState.Transitions)
            {
                actionForTransition?.Invoke(transition);

                stack.Push(transition.To);
            }
        }
    }

    /// <summary>
    /// Originally, the Default value for StateMachineEventManager variable in StateMachine activity,
    /// is initialized via a LambdaValue activity. However, PartialTrust environment does not support 
    /// LambdaValue activity that references any local variables or non-public members.
    /// The recommended approach is to convert the LambdaValue to an equivalent internal CodeActivity.
    /// </summary>
    private sealed class StateMachineEventManagerFactory : CodeActivity<StateMachineEventManager>
    {
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            // metdata.Bind uses reflection if the argument property has a value of null. 
            // So by forcing the argument property to have a non-null value, it avoids reflection.
            // Otherwise it would use reflection to initializer Result and would fail Partial Trust.
            Result ??= new OutArgument<StateMachineEventManager>();
            RuntimeArgument eventManagerArgument = new("Result", ResultType, ArgumentDirection.Out);
            metadata.Bind(Result, eventManagerArgument);
            metadata.AddArgument(eventManagerArgument);
        }

        protected override StateMachineEventManager Execute(CodeActivityContext context) => new();
    }
}
