// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

public sealed class TryCatch : NativeActivity
{
    private CatchList _catches;
    private Collection<Variable> _variables;
    private readonly Variable<TryCatchState> _state;
    private FaultCallback _exceptionFromCatchOrFinallyHandler;

    internal const string FaultContextId = "{35ABC8C3-9AF1-4426-8293-A6DDBB6ED91D}";

    public TryCatch()
        : base()
    {
        _state = new Variable<TryCatchState>();
    }

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
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }

    [DefaultValue(null)]
    [DependsOn("Variables")]
    public Activity Try { get; set; }

    [DependsOn("Try")]
    public Collection<Catch> Catches
    {
        get
        {
            _catches ??= new CatchList();
            return _catches;
        }
    }

    [DefaultValue(null)]
    [DependsOn("Catches")]
    public Activity Finally { get; set; }

    private FaultCallback ExceptionFromCatchOrFinallyHandler
    {
        get
        {
            _exceptionFromCatchOrFinallyHandler ??= new FaultCallback(OnExceptionFromCatchOrFinally);
            return _exceptionFromCatchOrFinallyHandler;
        }
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    }

    protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
    {
        TryCatchState state = updateContext.GetValue(this.state);
        if (state != null && !state.SuppressCancel && state.CaughtException != null && this.FindCatch(state.CaughtException.Exception) == null)
        {
            // This is a very small window of time in which we want to block update inside TryCatch.  
            // This is in between OnExceptionFromTry faultHandler and OnTryComplete completionHandler.  
            // A Catch handler could be found at OnExceptionFromTry before update, yet that appropriate Catch handler could have been removed during update and not be found at OnTryComplete.
            // In such case, the exception can be unintentionally swallowed without ever propagating it upward.  
            // Such TryCatch state is detected by inspecting the TryCatchState private variable for SuppressCancel == false && CaughtException != Null && this.FindCatch(state.CaughtException.Exception) == null.
            updateContext.DisallowUpdate(SR.TryCatchInvalidStateForUpdate(state.CaughtException.Exception));
        }
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        if (Try != null)
        {
            metadata.AddChild(Try);
        }

        if (Finally != null)
        {
            metadata.AddChild(Finally);
        }

        Collection<ActivityDelegate> delegates = new Collection<ActivityDelegate>();

        if (_catches != null)
        {
            foreach (Catch item in _catches)
            {
                ActivityDelegate catchDelegate = item.GetAction();
                if (catchDelegate != null)
                {
                    delegates.Add(catchDelegate);
                }
            }
        }

        metadata.AddImplementationVariable(_state);

        metadata.SetDelegatesCollection(delegates);

        metadata.SetVariablesCollection(Variables);

        if (Finally == null && Catches.Count == 0)
        {
            metadata.AddValidationError(SR.CatchOrFinallyExpected(DisplayName));
        }
    }

    internal static Catch FindCatchActivity(Type typeToMatch, IList<Catch> catches)
    {
        foreach (Catch item in catches)
        {
            if (item.ExceptionType == typeToMatch)
            {
                return item;
            }
        }

        return null;
    }

    protected override void Execute(NativeActivityContext context)
    {
        ExceptionPersistenceExtension extension = context.GetExtension<ExceptionPersistenceExtension>();
        if ((extension != null) && !extension.PersistExceptions)
        {
            // We will need a NoPersistProperty if we catch an exception.
            if (!(context.Properties.FindAtCurrentScope(NoPersistProperty.Name) is NoPersistProperty noPersistProperty))
            {
                noPersistProperty = new NoPersistProperty(context.CurrentExecutor);
                context.Properties.Add(NoPersistProperty.Name, noPersistProperty);
            }
        }

        _state.Set(context, new TryCatchState());
        if (Try != null)
        {
            context.ScheduleActivity(Try, new CompletionCallback(OnTryComplete), new FaultCallback(OnExceptionFromTry));
        }
        else
        {
            OnTryComplete(context, null);
        }
    }

    protected override void Cancel(NativeActivityContext context)
    {
        TryCatchState state = _state.Get(context);
        if (!state.SuppressCancel)
        {
            context.CancelChildren();
        }
    }

    private void OnTryComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        TryCatchState state = _state.Get(context);

        // We only allow the Try to be canceled.
        state.SuppressCancel = true;

        if (state.CaughtException != null)
        {
            Catch toSchedule = FindCatch(state.CaughtException.Exception);

            if (toSchedule != null)
            {
                state.ExceptionHandled = true;
                if (toSchedule.GetAction() != null)
                {
                    context.Properties.Add(FaultContextId, state.CaughtException, true);
                    toSchedule.ScheduleAction(context, state.CaughtException.Exception, new CompletionCallback(OnCatchComplete), ExceptionFromCatchOrFinallyHandler);
                    return;
                }
            }
        }

        OnCatchComplete(context, null);
    }

    private void OnExceptionFromTry(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
    {
        if (propagatedFrom.IsCancellationRequested)
        {
            if (TD.TryCatchExceptionDuringCancelationIsEnabled())
            {
                TD.TryCatchExceptionDuringCancelation(DisplayName);
            }

            // The Try activity threw an exception during Cancel; abort the workflow
            context.Abort(propagatedException);
            context.HandleFault();
        }
        else
        {
            Catch catchHandler = FindCatch(propagatedException);
            if (catchHandler != null)
            {
                if (TD.TryCatchExceptionFromTryIsEnabled())
                {
                    TD.TryCatchExceptionFromTry(DisplayName, propagatedException.GetType().ToString());
                }

                context.CancelChild(propagatedFrom);
                TryCatchState state = _state.Get(context);

                // If we are not supposed to persist exceptions, enter our noPersistScope
                ExceptionPersistenceExtension extension = context.GetExtension<ExceptionPersistenceExtension>();
                if ((extension != null) && !extension.PersistExceptions)
                {
                    NoPersistProperty noPersistProperty = (NoPersistProperty)context.Properties.FindAtCurrentScope(NoPersistProperty.Name);
                    if (noPersistProperty != null)
                    {
                        // The property will be exited when the activity completes or aborts.
                        noPersistProperty.Enter();
                    }
                }

                state.CaughtException = context.CreateFaultContext();
                context.HandleFault();
            }
        }
    }

    private void OnCatchComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        // Start suppressing cancel for the finally activity
        TryCatchState state = _state.Get(context);
        state.SuppressCancel = true;

        if (completedInstance != null && completedInstance.State != ActivityInstanceState.Closed)
        {
            state.ExceptionHandled = false;
        }

        context.Properties.Remove(FaultContextId);

        if (Finally != null)
        {
            context.ScheduleActivity(Finally, new CompletionCallback(OnFinallyComplete), ExceptionFromCatchOrFinallyHandler);
        }
        else
        {
            OnFinallyComplete(context, null);
        }
    }

    private void OnFinallyComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        TryCatchState state = _state.Get(context);
        if (context.IsCancellationRequested && !state.ExceptionHandled)
        {
            context.MarkCanceled();
        }
    }

    private void OnExceptionFromCatchOrFinally(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
    {
        if (TD.TryCatchExceptionFromCatchOrFinallyIsEnabled())
        {
            TD.TryCatchExceptionFromCatchOrFinally(DisplayName);
        }

        // We allow cancel through if there is an exception from the catch or finally
        TryCatchState state = _state.Get(context);
        state.SuppressCancel = false;
    }

    private Catch FindCatch(Exception exception)
    {
        Type exceptionType = exception.GetType();
        Catch potentialCatch = null;

        foreach (Catch catchHandler in Catches)
        {
            if (catchHandler.ExceptionType == exceptionType)
            {
                // An exact match
                return catchHandler;
            }
            else if (catchHandler.ExceptionType.IsAssignableFrom(exceptionType))
            {
                if (potentialCatch != null)
                {
                    if (catchHandler.ExceptionType.IsSubclassOf(potentialCatch.ExceptionType))
                    {
                        // The new handler is more specific
                        potentialCatch = catchHandler;
                    }
                }
                else
                {
                    potentialCatch = catchHandler;
                }
            }
        }

        return potentialCatch;
    }

    [DataContract]
    internal class TryCatchState
    {
        [DataMember(EmitDefaultValue = false)]
        public bool SuppressCancel { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public FaultContext CaughtException { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool ExceptionHandled { get; set; }
    }

    private class CatchList : ValidatingCollection<Catch>
    {
        public CatchList()
            : base()
        {
            OnAddValidationCallback = item =>
            {
                if (item == null)
                {
                    throw FxTrace.Exception.ArgumentNull(nameof(item));
                }
            };
        }

        protected override void InsertItem(int index, Catch item)
        {
            if (item == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(item));
            }

            Catch existingCatch = FindCatchActivity(item.ExceptionType, Items);

            if (existingCatch != null)
            {
                throw FxTrace.Exception.Argument(nameof(item), SR.DuplicateCatchClause(item.ExceptionType.FullName));
            }

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, Catch item)
        {
            if (item == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(item));
            }

            Catch existingCatch = FindCatchActivity(item.ExceptionType, Items);

            if (existingCatch != null && !ReferenceEquals(this[index], existingCatch))
            {
                throw FxTrace.Exception.Argument(nameof(item), SR.DuplicateCatchClause(item.ExceptionType.FullName));
            }

            base.SetItem(index, item);
        }
    }
}
