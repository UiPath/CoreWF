// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Statements.Tracking;
using System.Collections.ObjectModel;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

/// <summary>
/// InternalState is internal representation of State.
/// </summary>
internal sealed class InternalState : NativeActivity<string>
{
    // State denotes corresponding State object.
    private readonly State _state;

    // internal representation of transitions.
    private readonly Collection<InternalTransition> _internalTransitions;

    // number of running triggers
    private readonly Variable<int> _currentRunningTriggers;
    private readonly Variable<bool> _isExiting;

    // This bookmark is used to evaluate condition of a transition of this state. 
    private readonly Variable<Bookmark> _evaluateConditionBookmark;

    // Callback which is called when Entry is completed.
    private readonly CompletionCallback _onEntryComplete;

    // Callback which is called when Trigger is completed.
    private readonly CompletionCallback _onTriggerComplete;

    // Callback which is called when Condition is completed.
    private readonly CompletionCallback<bool> _onConditionComplete;

    // Callback which is called when Exit is completed.
    private readonly CompletionCallback _onExitComplete;

    // Callback which is used to start to evaluate Condition of a transition of this state.
    private readonly BookmarkCallback _evaluateConditionCallback;
    private readonly Dictionary<Activity, InternalTransition> _triggerInternalTransitionMapping = new();

    public InternalState(State state)
    {
        _state = state;
        DisplayName = state.DisplayName;

        _onEntryComplete = new CompletionCallback(OnEntryComplete);
        _onTriggerComplete = new CompletionCallback(OnTriggerComplete);
        _onConditionComplete = new CompletionCallback<bool>(OnConditionComplete);
        _onExitComplete = new CompletionCallback(OnExitComplete);

        _evaluateConditionCallback = new BookmarkCallback(StartEvaluateCondition);

        _currentRunningTriggers = new Variable<int>();
        _isExiting = new Variable<bool>();
        _evaluateConditionBookmark = new Variable<Bookmark>();
        _internalTransitions = new Collection<InternalTransition>();
        _triggerInternalTransitionMapping = new Dictionary<Activity, InternalTransition>();
    }

    /// <summary>
    /// Gets or sets EventManager is used to globally manage event queue such that triggered events can be processed in order.
    /// </summary>
    [RequiredArgument]
    public InArgument<StateMachineEventManager> EventManager { get; set; }

    /// <summary>
    /// Gets Entry activity that will be executed when state is entering.
    /// </summary>
    public Activity Entry => _state.Entry;

    /// <summary>
    /// Gets Exit activity that will be executed when state is leaving.
    /// </summary>
    public Activity Exit => _state.Exit;

    /// <summary>
    /// Gets a value indicating whether this state is a final state or not.
    /// </summary>
    [DefaultValue(false)]
    public bool IsFinal => _state.IsFinal;

    /// <summary>
    /// Gets StateId, which is the identifier of a state. It's unique within a StateMachine.
    /// </summary>
    public string StateId => _state.StateId;

    /// <summary>
    /// Gets Transitions collection contains transitions on this state.
    /// </summary>
    public Collection<Transition> Transitions => _state.Transitions;

    /// <summary>
    /// Gets Variables collection contains Variables on this state.
    /// </summary>
    public Collection<Variable> Variables => _state.Variables;

    /// <summary>
    /// Gets the display name of the parent state machine of the state.
    /// Used for tracking purpose only.
    /// </summary>
    public string StateMachineName => _state.StateMachineName;

    protected override bool CanInduceIdle => true;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        _internalTransitions.Clear();

        if (Entry != null)
        {
            metadata.AddChild(Entry);
        }

        if (Exit != null)
        {
            metadata.AddChild(Exit);
        }

        ProcessTransitions(metadata);
        metadata.SetVariablesCollection(Variables);

        RuntimeArgument eventManagerArgument = new RuntimeArgument("EventManager", EventManager.ArgumentType, ArgumentDirection.In);
        metadata.Bind(EventManager, eventManagerArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                eventManagerArgument
            });

        metadata.AddImplementationVariable(_currentRunningTriggers);
        metadata.AddImplementationVariable(_isExiting);
        metadata.AddImplementationVariable(_evaluateConditionBookmark);
    }

    protected override void Execute(NativeActivityContext context)
    {
        StateMachineEventManager eventManager = EventManager.Get(context);
        eventManager.CurrentBeingProcessedEvent = null;
        _isExiting.Set(context, false);
        ScheduleEntry(context);
    }

    protected override void Abort(NativeActivityAbortContext context)
    {
        RemoveActiveBookmark(context);
        base.Abort(context);
    }

    protected override void Cancel(NativeActivityContext context)
    {
        RemoveActiveBookmark(context);
        base.Cancel(context);
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        InternalState originalInternalState = (InternalState)originalActivity;

        // NOTE: State.Entry/Exit are allowed to be removed, because it doesn't change the execution semantics of SM
        // if this removed activity was executing, WF runtime would disallow the update.
        Activity entryActivityMatch = metadata.GetMatch(this.Entry);
        Activity exitActivityMatch = metadata.GetMatch(this.Exit);

        if ((null != entryActivityMatch && !object.ReferenceEquals(entryActivityMatch, originalInternalState.Entry)) ||
            (null != exitActivityMatch && !object.ReferenceEquals(exitActivityMatch, originalInternalState.Exit)))
        {
            // original State.Entry/Exit is replaced with another child activities with InternalState
            // new State.Entry/Exit is moved from another child activities within InternalState.
            metadata.DisallowUpdateInsideThisActivity(SR.MovingActivitiesInStateBlockDU);
            return;
        }

        int originalTriggerInUpdatedDefinition = 0;

        foreach (InternalTransition originalTransition in originalInternalState.internalTransitions)
        {
            if (metadata.IsReferenceToImportedChild(originalTransition.Trigger))
            {
                metadata.DisallowUpdateInsideThisActivity(SR.TriggerOrConditionIsReferenced);
                return;
            }

            if (!originalTransition.IsUnconditional)
            {
                // new Trigger activity
                foreach (TransitionData transitionData in originalTransition.TransitionDataList)
                {
                    if (metadata.IsReferenceToImportedChild(transitionData.Condition))
                    {
                        metadata.DisallowUpdateInsideThisActivity(SR.TriggerOrConditionIsReferenced);
                        return;
                    }
                }
            }
        }

        foreach (InternalTransition updatedTransition in this.internalTransitions)
        {
            if (metadata.IsReferenceToImportedChild(updatedTransition.Trigger))
            {
                // if the trigger is referenced, it might have another save values already.
                metadata.DisallowUpdateInsideThisActivity(SR.TriggerOrConditionIsReferenced);
                return;
            }

            Activity triggerMatch = metadata.GetMatch(updatedTransition.Trigger);

            if (null != triggerMatch)
            {
                InternalTransition originalTransition;

                if (originalInternalState.triggerInternalTransitionMapping.TryGetValue(triggerMatch, out originalTransition))
                {
                    originalTriggerInUpdatedDefinition++;

                    if (originalTransition.IsUnconditional)
                    {
                        string errorMessage;
                        bool canTransitionBeUpdated = ValidateDUInUnconditionalTransition(metadata, updatedTransition, originalTransition, out errorMessage);

                        if (!canTransitionBeUpdated)
                        {
                            metadata.DisallowUpdateInsideThisActivity(errorMessage);
                            return;
                        }
                    }
                    else
                    {
                        if (updatedTransition.IsUnconditional)
                        {
                            // cannot change the transition from condition to unconditional.
                            metadata.DisallowUpdateInsideThisActivity(SR.ChangeConditionalTransitionToUnconditionalBlockDU);
                            return;
                        }
                        else
                        {
                            string errorMessage;
                            bool canTransitionBeUpdated = ValidateDUInConditionTransition(metadata, updatedTransition, originalTransition, out errorMessage);

                            if (!canTransitionBeUpdated)
                            {
                                metadata.DisallowUpdateInsideThisActivity(errorMessage);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // the trigger is an child activity moved from elsewhere within the state
                    metadata.DisallowUpdateInsideThisActivity(SR.MovingActivitiesInStateBlockDU);
                    return;
                }
            }
            else
            {
                // new Trigger activity
                foreach (TransitionData transitionData in updatedTransition.TransitionDataList)
                {
                    if ((null != transitionData.Condition && null != metadata.GetMatch(transitionData.Condition)) ||
                        (null != transitionData.Action && null != metadata.GetMatch(transitionData.Action)))
                    {
                        // if a new transition is added, it is expected that the Condition/Action 
                        // are newly created.
                        metadata.DisallowUpdateInsideThisActivity(SR.ChangingTriggerOrUseOriginalConditionActionBlockDU);
                        return;
                    }
                }
            }
        }

        if (originalTriggerInUpdatedDefinition != originalInternalState.internalTransitions.Count)
        {
            // NOTE: in general, if the transition is removed when there are pending triggers,
            // runtime would be able to detect the missing child activities.  However, in cases,
            // where the transition is happening already (in between completion of Transition.Action
            // callback but before InternalState is completed), the workflow definition can be unloaded
            // and updated.  The InternalState is unable to trace the original transition that set the 
            // destination state index.  In that case, the update would fail at UpdateInstance.
            // To simplify the model, it is more convenient to disallow removing existing transitions
            // from an executing InternalState.  The only extra restriction it brings, is that it disables
            // update even if the InternalState is uploaded at State.Entry.  This scenario, however, is uncommon.
            metadata.DisallowUpdateInsideThisActivity(SR.RemovingTransitionsBlockDU);
        }
    }

    protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
    {
        StateMachineEventManager eventManager = updateContext.GetValue(this.EventManager) as StateMachineEventManager;
        Fx.Assert(eventManager != null, "eventManager is available in every internalActivity.");

        if (eventManager.CurrentBeingProcessedEvent != null || eventManager.Queue.Any())
        {
            // Updated state is evaluating conditions or transitioning to another state,
            // Then we need to update the index of the current evaluated trigger (in case the trigger is moved)
            // and the condition index.
            // if the state is transitioning already, then we should update destination state id.
            bool isUpdateSuccessful = this.UpdateEventManager(updateContext, eventManager);

            if (!isUpdateSuccessful)
            {
                updateContext.DisallowUpdate(SR.DUTriggerOrConditionChangedDuringTransitioning);
                return;
            }

            if (updateContext.GetValue(this.isExiting) != true)
            {
                this.RescheduleNewlyAddedTriggers(updateContext);
            }
        }
        else if (updateContext.GetValue(this.currentRunningTriggers) > 0)
        {
            Fx.Assert(updateContext.GetValue(this.isExiting) != true, "No triggers have completed, state should not be transitioning.");

            // the state is not transitioning yet and is persisted at trigger.
            this.RescheduleNewlyAddedTriggers(updateContext);
        }
    } 
#endif

    private static void AddTransitionData(NativeActivityMetadata metadata, InternalTransition internalTransition, Transition transition)
    {
        TransitionData transitionData = new();
        Activity<bool> condition = transition.Condition;
        transitionData.Condition = condition;

        if (condition != null)
        {
            metadata.AddChild(condition);
        }

        Activity action = transition.Action;
        transitionData.Action = action;

        if (action != null)
        {
            metadata.AddChild(action);
        }

        if (transition.To != null)
        {
            transitionData.To = transition.To.InternalState;
        }

        internalTransition.TransitionDataList.Add(transitionData);
    }

    private static void ProcessNextTriggerCompletedEvent(NativeActivityContext context, StateMachineEventManager eventManager)
    {
        eventManager.CurrentBeingProcessedEvent = null;
        eventManager.OnTransition = false;

        TriggerCompletedEvent completedEvent = eventManager.GetNextCompletedEvent();

        if (completedEvent != null)
        {
            ResumeBookmarkExtension.Resume(context, completedEvent.Bookmark);
        }
    }

#if DYNAMICUPDATE
    private static bool ValidateDUInConditionTransition(NativeActivityUpdateMapMetadata metadata, InternalTransition updatedTransition, InternalTransition originalTransition, out string errorMessage)
    {
        Fx.Assert(!originalTransition.IsUnconditional, "Transition should be conditional in the original definition.");
        errorMessage = string.Empty;

        foreach (TransitionData updatedTData in updatedTransition.TransitionDataList)
        {
            if (metadata.IsReferenceToImportedChild(updatedTData.Condition))
            {
                // if the trigger is referenced, it might have another save values already.
                errorMessage = SR.TriggerOrConditionIsReferenced;
                return false;
            }

            Fx.Assert(null != updatedTData.Condition, "Must be a condition transition.");
            Activity conditionMatch = metadata.GetMatch(updatedTData.Condition);

            if (null == conditionMatch && null != metadata.GetMatch(updatedTData.Action))
            {
                // new Transition.Condition with an Transition.Action moved from within the InternalState.
                errorMessage = SR.MovingActivitiesInStateBlockDU;
                return false;
            }
            else if (null != conditionMatch)
            {
                bool foundMatchingOriginalCondition = false;

                for (int transitionIndex = 0; transitionIndex < originalTransition.TransitionDataList.Count; transitionIndex++)
                {
                    if (object.ReferenceEquals(originalTransition.TransitionDataList[transitionIndex].Condition, conditionMatch))
                    {
                        foundMatchingOriginalCondition = true;

                        // found the original matching condition in updated transition definition.
                        TransitionData originalTData = originalTransition.TransitionDataList[transitionIndex];

                        Activity originalAction = originalTData.Action;

                        // NOTE: Transition.Action is allowed to be removed, because it doesn't change the execution semantics of SM
                        // if this removed activity was executing, WF runtime would disallow the update.
                        Activity actionMatch = metadata.GetMatch(updatedTData.Action);

                        if (null != actionMatch && !object.ReferenceEquals(originalAction, actionMatch))
                        {
                            // Transition.Action is an activity moved from elsewhere within the InternalState
                            errorMessage = SR.MovingActivitiesInStateBlockDU;
                            return false;
                        }

                        metadata.SaveOriginalValue(updatedTransition.Trigger, originalTransition.InternalTransitionIndex);
                        metadata.SaveOriginalValue(updatedTData.Condition, transitionIndex);
                    }
                }

                if (!foundMatchingOriginalCondition)
                {
                    // another child activity is move to the Transition.Condition.
                    errorMessage = SR.DUDisallowIfCannotFindingMatchingCondition;
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidateDUInUnconditionalTransition(NativeActivityUpdateMapMetadata metadata, InternalTransition updatedTransition, InternalTransition originalTransition, out string errorMessage)
    {
        Fx.Assert(originalTransition.IsUnconditional, "Transition should be unconditional in the original definition.");
        Activity originalAction = originalTransition.TransitionDataList[0].Action;

        foreach (TransitionData transitionData in updatedTransition.TransitionDataList)
        {
            Activity updatedAction = transitionData.Action;
            Activity actionMatch = metadata.GetMatch(updatedAction);
            Activity conditionMatch = metadata.GetMatch(transitionData.Condition);

            if ((null == originalAction && null != actionMatch) ||
                (null != originalAction && null != actionMatch && !object.ReferenceEquals(originalAction, actionMatch)))
            {
                // Transition.Action is an activity moved from elsewhere within the InternalState
                errorMessage = SR.MovingActivitiesInStateBlockDU;
                return false;
            }
        }

        errorMessage = string.Empty;
        metadata.SaveOriginalValue(updatedTransition.Trigger, originalTransition.InternalTransitionIndex);
        return true;
    }

    private void RescheduleNewlyAddedTriggers(NativeActivityUpdateContext updateContext)
    {
        // NOTE: triggers are scheduled already, so the state has completed executing State.Entry
        Fx.Assert(this.internalTransitions.Count == this.triggerInternalTransitionMapping.Count, "Triggers mappings are correct.");
        List<Activity> newTriggers = new List<Activity>();

        foreach (InternalTransition transition in this.internalTransitions)
        {
            if (updateContext.IsNewlyAdded(transition.Trigger))
            {
                newTriggers.Add(transition.Trigger);
            }

            // NOTE: all Triggers in triggerInternalTransitionMapping are either new or was previously scheduled
        }

        foreach (Activity newTrigger in newTriggers)
        {
            updateContext.ScheduleActivity(newTrigger, this.onTriggerComplete);
        }

        updateContext.SetValue<int>(this.currentRunningTriggers, updateContext.GetValue(this.currentRunningTriggers) + newTriggers.Count);
    }

    /// <summary>
    /// Used for Dynamic Update: after the instance is updated, if the statemachine is already transitioning, the index of the to-be-scheduled state 
    /// would need to be updated.
    /// </summary>
    /// <param name="updateContext">Dynamic Update context</param>
    /// <param name="eventManager">Internal StateMachineEventManager</param>
    /// <returns>True, 1. if update is successful and the instanced is updated with the new indexes, and 2 all the trigger ID in the queue are updated;
    /// false otherwise and the update should fail.</returns>
    private bool UpdateEventManager(
        NativeActivityUpdateContext updateContext,
        StateMachineEventManager eventManager)
    {
        Fx.Assert(null != eventManager.CurrentBeingProcessedEvent, "The eventManager must have some info that needs to be updated during transition.");

        int updatedEventsInQueue = 0;
        int originalTriggerId = int.MinValue;
        int originalConditionIndex = int.MinValue;
        bool updateCurrentEventSucceed = null == eventManager.CurrentBeingProcessedEvent ? true : false;

        foreach (InternalTransition transition in this.internalTransitions)
        {
            object savedTriggerIndex = updateContext.GetSavedOriginalValue(transition.Trigger);
            if (savedTriggerIndex != null)
            {
                Fx.Assert(!updateContext.IsNewlyAdded(transition.Trigger), "the trigger in transition already exist.");

                if (null != eventManager.CurrentBeingProcessedEvent &&
                    eventManager.CurrentBeingProcessedEvent.TriggedId == (int)savedTriggerIndex)
                {
                    // found a match of the running trigger update the current processed event
                    // Don't match the trigger ID, match only when the Condition is also matched.
                    if (eventManager.CurrentConditionIndex == -1)
                    {
                        if (transition.IsUnconditional)
                        {
                            // executing transition before persist is unconditional
                            originalTriggerId = eventManager.CurrentBeingProcessedEvent.TriggedId;
                            originalConditionIndex = 0;
                            eventManager.CurrentBeingProcessedEvent.TriggedId = transition.InternalTransitionIndex;

                            if (updateContext.GetValue(this.isExiting))
                            {
                                Fx.Assert(eventManager.OnTransition, "The state is transitioning.");
                                updateContext.SetValue(this.Result, GetTo(transition.InternalTransitionIndex));
                            }

                            updateCurrentEventSucceed = true;
                        }
                        else
                        {
                            updateContext.DisallowUpdate(SR.ChangeTransitionTypeDuringTransitioningBlockDU);
                            return false;
                        }
                    }
                    else if (eventManager.CurrentConditionIndex >= 0)
                    {
                        Fx.Assert(!transition.IsUnconditional, "Cannot update a running conditional transition with a unconditional one.");

                        if (!transition.IsUnconditional)
                        {
                            // executing transition before and after are conditional
                            for (int updatedIndex = 0; updatedIndex < transition.TransitionDataList.Count; updatedIndex++)
                            {
                                Activity condition = transition.TransitionDataList[updatedIndex].Condition;
                                Fx.Assert(null != condition, "Conditional transition must have Condition activity.");
                                int? savedCondIndex = updateContext.GetSavedOriginalValue(condition) as int?;

                                if (eventManager.CurrentConditionIndex == savedCondIndex)
                                {
                                    originalTriggerId = eventManager.CurrentBeingProcessedEvent.TriggedId;
                                    originalConditionIndex = eventManager.CurrentConditionIndex;
                                    eventManager.CurrentBeingProcessedEvent.TriggedId = transition.InternalTransitionIndex;
                                    eventManager.CurrentConditionIndex = updatedIndex;

                                    if (updateContext.GetValue(this.isExiting))
                                    {
                                        Fx.Assert(eventManager.OnTransition, "The state is transitioning.");
                                        updateContext.SetValue(this.Result, this.GetTo(transition.InternalTransitionIndex, (int)updatedIndex));
                                    }

                                    updateCurrentEventSucceed = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                foreach (TriggerCompletedEvent completedEvent in eventManager.Queue)
                {
                    if ((int)savedTriggerIndex == completedEvent.TriggedId)
                    {
                        completedEvent.TriggedId = transition.InternalTransitionIndex;
                        updatedEventsInQueue++;
                    }
                }
            }
        }

        return eventManager.Queue.Count() == updatedEventsInQueue ? updateCurrentEventSucceed : false;
    } 
#endif

    private void ScheduleEntry(NativeActivityContext context)
    {
        context.Track(new StateMachineStateRecord
        {
            StateMachineName = StateMachineName,
            StateName = DisplayName,
        });

        if (Entry != null)
        {
            context.ScheduleActivity(Entry, _onEntryComplete);
        }
        else
        {
            _onEntryComplete(context, null);
        }
    }

    private void OnEntryComplete(NativeActivityContext context, ActivityInstance instance)
    {
        ProcessNextTriggerCompletedEvent(context, EventManager.Get(context));
        ScheduleTriggers(context);
    }

    private void ScheduleTriggers(NativeActivityContext context)
    {
        if (!IsFinal)
        {
            // Final state need not condition evaluation bookmark.
            AddEvaluateConditionBookmark(context);
        }

        if (_internalTransitions.Count > 0)
        {
            foreach (InternalTransition transition in _internalTransitions)
            {
                context.ScheduleActivity(transition.Trigger, _onTriggerComplete);
            }

            _currentRunningTriggers.Set(context, _currentRunningTriggers.Get(context) + _internalTransitions.Count);
        }
    }

    private void OnTriggerComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        int runningTriggers = _currentRunningTriggers.Get(context);
        _currentRunningTriggers.Set(context, --runningTriggers);
        bool isOnExit = _isExiting.Get(context);

        if (!context.IsCancellationRequested && runningTriggers == 0 && isOnExit)
        {
            ScheduleExit(context);
        }
        else if (completedInstance.State == ActivityInstanceState.Closed)
        {
            _triggerInternalTransitionMapping.TryGetValue(completedInstance.Activity, out InternalTransition internalTransition);
            Fx.Assert(internalTransition != null, "internalTransition should be added into triggerInternalTransitionMapping in CacheMetadata.");

            StateMachineEventManager eventManager = EventManager.Get(context);
            eventManager.RegisterCompletedEvent(
                new TriggerCompletedEvent { Bookmark = _evaluateConditionBookmark.Get(context), TriggerId = internalTransition.InternalTransitionIndex },
                out bool canBeProcessedImmediately);

            if (canBeProcessedImmediately)
            {
                ProcessNextTriggerCompletedEvent(context, eventManager);
            }
        }
    }

    private void StartEvaluateCondition(NativeActivityContext context, Bookmark bookmark, object value)
    {
        // Start to evaluate conditions of the trigger which represented by currentTriggerIndex
        StateMachineEventManager eventManager = EventManager.Get(context);
        int triggerId = eventManager.CurrentBeingProcessedEvent.TriggerId;
        InternalTransition transition = GetInternalTransition(triggerId);

        if (transition.IsUnconditional)
        {
            eventManager.CurrentConditionIndex = -1;
            TakeTransition(context, eventManager, triggerId);
        }
        else
        {
            eventManager.CurrentConditionIndex = 0;
            context.ScheduleActivity(
                GetCondition(
                    triggerId,
                    eventManager.CurrentConditionIndex),
                _onConditionComplete,
                null);
        }
    }

    private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        StateMachineEventManager eventManager = EventManager.Get(context);
        int triggerId = eventManager.CurrentBeingProcessedEvent.TriggerId;

        if (result)
        {
            TakeTransition(context, eventManager, triggerId);
        }
        else
        {
            // condition failed: reschedule trigger
            int currentConditionIndex = eventManager.CurrentConditionIndex;
            Fx.Assert(eventManager.CurrentConditionIndex >= 0, "Conditional Transition must have non-negative index.");
            InternalTransition transition = GetInternalTransition(triggerId);
            currentConditionIndex++;

            if (currentConditionIndex < transition.TransitionDataList.Count)
            {
                eventManager.CurrentConditionIndex = currentConditionIndex;
                context.ScheduleActivity(transition.TransitionDataList[currentConditionIndex].Condition, _onConditionComplete, null);
            }
            else
            {
                // Schedule current trigger again firstly.
                context.ScheduleActivity(transition.Trigger, _onTriggerComplete);
                _currentRunningTriggers.Set(context, _currentRunningTriggers.Get(context) + 1);

                // check whether there is any other trigger completed.
                ProcessNextTriggerCompletedEvent(context, eventManager);
            }
        }
    }

    private void ScheduleExit(NativeActivityContext context)
    {
        if (Exit != null)
        {
            context.ScheduleActivity(Exit, _onExitComplete);
        }
        else
        {
            _onExitComplete(context, null);
        }
    }

    private void OnExitComplete(NativeActivityContext context, ActivityInstance instance) => ScheduleAction(context);

    private void ScheduleAction(NativeActivityContext context)
    {
        StateMachineEventManager eventManager = EventManager.Get(context);
        if (eventManager.IsReferredByBeingProcessedEvent(_evaluateConditionBookmark.Get(context)))
        {
            InternalTransition transition = GetInternalTransition(eventManager.CurrentBeingProcessedEvent.TriggerId);
            Activity action = transition.TransitionDataList[-1 == eventManager.CurrentConditionIndex ? 0 : eventManager.CurrentConditionIndex].Action;

            if (action != null)
            {
                context.ScheduleActivity(action);
            }
        }

        RemoveBookmarks(context);
    }

    private void ProcessTransitions(NativeActivityMetadata metadata)
    {
        for (int i = 0; i < Transitions.Count; i++)
        {
            Transition transition = Transitions[i];
            Activity triggerActivity = transition.ActiveTrigger;

            if (!_triggerInternalTransitionMapping.TryGetValue(triggerActivity, out InternalTransition internalTransition))
            {
                metadata.AddChild(triggerActivity);

                internalTransition = new InternalTransition
                {
                    Trigger = triggerActivity,
                    InternalTransitionIndex = _internalTransitions.Count,
                };

                _triggerInternalTransitionMapping.Add(triggerActivity, internalTransition);
                _internalTransitions.Add(internalTransition);
            }

            AddTransitionData(metadata, internalTransition, transition);
        }
    }

    private InternalTransition GetInternalTransition(int triggerIndex) => _internalTransitions[triggerIndex];

    private Activity<bool> GetCondition(int triggerIndex, int conditionIndex) => _internalTransitions[triggerIndex].TransitionDataList[conditionIndex].Condition;

    private string GetTo(int triggerIndex, int conditionIndex = 0) => _internalTransitions[triggerIndex].TransitionDataList[conditionIndex].To.StateId;

    private void AddEvaluateConditionBookmark(NativeActivityContext context)
    {
        Bookmark bookmark = context.CreateBookmark(_evaluateConditionCallback, BookmarkOptions.MultipleResume);
        _evaluateConditionBookmark.Set(context, bookmark);
        EventManager.Get(context).AddActiveBookmark(bookmark);
    }

    private void RemoveBookmarks(NativeActivityContext context)
    {
        context.RemoveAllBookmarks();
        RemoveActiveBookmark(context);
    }

    private void RemoveActiveBookmark(ActivityContext context)
    {
        StateMachineEventManager eventManager = EventManager.Get(context);
        Bookmark bookmark = _evaluateConditionBookmark.Get(context);
        if (bookmark != null)
        {
            eventManager.RemoveActiveBookmark(bookmark);
        }
    }

    private void TakeTransition(NativeActivityContext context, StateMachineEventManager eventManager, int triggerId)
    {
        EventManager.Get(context).OnTransition = true;
        InternalTransition transition = GetInternalTransition(triggerId);

        if (transition.IsUnconditional)
        {
            Fx.Assert(-1 == eventManager.CurrentConditionIndex, "CurrentConditionIndex should be -1, if the transition is unconditional.");
            PrepareForExit(context, GetTo(triggerId));
        }
        else
        {
            Fx.Assert(-1 != eventManager.CurrentConditionIndex, "CurrentConditionIndex should not be -1, if the transition is conditional.");
            PrepareForExit(context, GetTo(triggerId, eventManager.CurrentConditionIndex));
        }
    }

    private void PrepareForExit(NativeActivityContext context, string targetStateId)
    {
        ReadOnlyCollection<ActivityInstance> children = context.GetChildren();
        Result.Set(context, targetStateId);
        _isExiting.Set(context, true);

        if (children.Count > 0)
        {
            // Cancel all other pending triggers.
            context.CancelChildren();
        }
        else
        {
            ScheduleExit(context);
        }
    }
}
