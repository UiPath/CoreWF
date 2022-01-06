// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;

namespace System.Activities.Statements;

internal sealed class CompensationParticipant : NativeActivity
{
    private readonly InArgument<long> _compensationId;
    private readonly Variable<CompensationToken> _currentCompensationToken;

    public CompensationParticipant(Variable<long> compensationId)
        : base()
    {
        _compensationId = compensationId;

        _currentCompensationToken = new Variable<CompensationToken>();

        DefaultCompensation = new DefaultCompensation()
        {
            Target = new InArgument<CompensationToken>(_currentCompensationToken),
        };

        DefaultConfirmation = new DefaultConfirmation()
        {
            Target = new InArgument<CompensationToken>(_currentCompensationToken),
        };
    }

    public Activity CompensationHandler { get; set; }

    public Activity ConfirmationHandler { get; set; }

    public Activity CancellationHandler { get; set; }

    private Activity DefaultCompensation { get; set; }

    private Activity DefaultConfirmation { get; set; }

    protected override bool CanInduceIdle => true;

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetImplementationVariablesCollection(
            new Collection<Variable>
            {
                _currentCompensationToken,
            });

        Collection<Activity> children = new();

        if (CompensationHandler != null)
        {
            children.Add(CompensationHandler);
        }

        if (ConfirmationHandler != null)
        {
            children.Add(ConfirmationHandler);
        }

        if (CancellationHandler != null)
        {
            children.Add(CancellationHandler);
        }

        metadata.SetChildrenCollection(children);

        Collection<Activity> implementationChildren = new();
        Fx.Assert(DefaultCompensation != null, "DefaultCompensation must be valid");
        implementationChildren.Add(DefaultCompensation);

        Fx.Assert(DefaultConfirmation != null, "DefaultConfirmation must be valid");
        implementationChildren.Add(DefaultConfirmation);

        metadata.SetImplementationChildrenCollection(implementationChildren);

        RuntimeArgument compensationIdArgument = new("CompensationId", typeof(long), ArgumentDirection.In);
        metadata.Bind(_compensationId, compensationIdArgument);
        metadata.AddArgument(compensationIdArgument);
    }

    protected override void Execute(NativeActivityContext context)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = _compensationId.Get(context);
        Fx.Assert(compensationId != CompensationToken.RootCompensationId, "CompensationId passed to the SecondaryRoot must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        CompensationToken token = new(compensationToken);
        _currentCompensationToken.Set(context, token);

        compensationToken.IsTokenValidInSecondaryRoot = true;
        context.Properties.Add(CompensationToken.PropertyName, token);

        Fx.Assert(compensationToken.BookmarkTable[CompensationBookmarkName.OnConfirmation] == null, "Bookmark should not be already initialized in the bookmark table.");
        compensationToken.BookmarkTable[CompensationBookmarkName.OnConfirmation] = context.CreateBookmark(new BookmarkCallback(OnConfirmation));

        Fx.Assert(compensationToken.BookmarkTable[CompensationBookmarkName.OnCompensation] == null, "Bookmark should not be already initialized in the bookmark table.");
        compensationToken.BookmarkTable[CompensationBookmarkName.OnCompensation] = context.CreateBookmark(new BookmarkCallback(OnCompensation));

        Fx.Assert(compensationToken.BookmarkTable[CompensationBookmarkName.OnCancellation] == null, "Bookmark should not be already initialized in the bookmark table.");
        compensationToken.BookmarkTable[CompensationBookmarkName.OnCancellation] = context.CreateBookmark(new BookmarkCallback(OnCancellation));

        Bookmark onSecondaryRootScheduled = compensationToken.BookmarkTable[CompensationBookmarkName.OnSecondaryRootScheduled];
        Fx.Assert(onSecondaryRootScheduled != null, "onSecondaryRootScheduled bookmark must be already registered.");

        compensationToken.BookmarkTable[CompensationBookmarkName.OnSecondaryRootScheduled] = null;

        context.ResumeBookmark(onSecondaryRootScheduled, compensationId);
    }

    private void OnConfirmation(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = (long)value;
        Fx.Assert(compensationId != CompensationToken.RootCompensationId, "CompensationId must be passed when resuming the Completed bookmark");

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        Fx.Assert(compensationToken.CompensationState == CompensationState.Confirming, "CompensationState should be in Confirming state");

        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(compensationToken.DisplayName, compensationToken.CompensationState.ToString());
        }

        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnCancellation);
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnCompensation);

        if (ConfirmationHandler != null)
        {
            context.ScheduleActivity(ConfirmationHandler, new CompletionCallback(OnConfirmationHandlerComplete), new FaultCallback(OnExceptionFromHandler));
        }
        else
        {
            _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultConfirmation, new CompletionCallback(OnConfirmationComplete));
            }
            else
            {
                compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.Confirmed);
            }
        }
    }

    private void OnConfirmationHandlerComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        Fx.Assert(context != null, "context must be valid");
        Fx.Assert(completedInstance != null, "completedInstance must be valid");

        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Fx.Assert(compensationToken.CompensationState == CompensationState.Confirming, "CompensationParticipant should be in Confirming State");

            _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultConfirmation, new CompletionCallback(OnConfirmationComplete));
            }
            else
            {
                compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.Confirmed);
            }
        }
    }

    private void OnConfirmationComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        Fx.Assert(context != null, "context must be valid");
        Fx.Assert(completedInstance != null, "completedInstance must be valid");

        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.Confirmed);
        }
    }

    private void OnCompensation(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = (long)value;
        Fx.Assert(compensationId != CompensationToken.RootCompensationId, "CompensationId must be passed when resuming the Completed bookmark");

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        Fx.Assert(compensationToken.CompensationState == CompensationState.Compensating, "CompensationState should be in Compensating state");

        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(compensationToken.DisplayName, compensationToken.CompensationState.ToString());
        }

        // Cleanup Bookmarks..
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnCancellation);
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnConfirmation);

        if (CompensationHandler != null)
        {
            context.ScheduleActivity(CompensationHandler, new CompletionCallback(OnCompensationHandlerComplete), new FaultCallback(OnExceptionFromHandler));
        }
        else
        {
            _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultCompensation, new CompletionCallback(OnCompensationComplete));
            }
            else
            {
                InternalOnCompensationComplete(context, compensationExtension, compensationToken);
            }
        }
    }

    private void OnCompensationHandlerComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        Fx.Assert(context != null, "context must be valid");
        Fx.Assert(completedInstance != null, "completedInstance must be valid");

        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Fx.Assert(compensationToken.CompensationState == CompensationState.Compensating, "CompensationParticipant should be in Compensating State");

            _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultConfirmation, new CompletionCallback(OnCompensationComplete));
            }
            else
            {
                InternalOnCompensationComplete(context, compensationExtension, compensationToken);
            }
        }
    }

    private void OnCancellation(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        long compensationId = (long)value;
        Fx.Assert(compensationId != CompensationToken.RootCompensationId, "CompensationId must be passed when resuming the Completed bookmark");

        CompensationTokenData compensationToken = compensationExtension.Get(compensationId);
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        Fx.Assert(compensationToken.CompensationState == CompensationState.Canceling, "CompensationState should be in Canceling state");

        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(compensationToken.DisplayName, compensationToken.CompensationState.ToString());
        }

        // remove bookmarks.
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnCompensation);
        compensationToken.RemoveBookmark(context, CompensationBookmarkName.OnConfirmation);

        _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
        if (CancellationHandler != null)
        {
            context.ScheduleActivity(CancellationHandler, new CompletionCallback(OnCancellationHandlerComplete), new FaultCallback(OnExceptionFromHandler));
        }
        else
        {
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultCompensation, new CompletionCallback(OnCompensationComplete));
            }
            else
            {
                InternalOnCompensationComplete(context, compensationExtension, compensationToken);
            }
        }
    }

    private void OnCancellationHandlerComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Fx.Assert(compensationToken.CompensationState == CompensationState.Canceling, "CompensationParticipant should be in Canceling State");

            _currentCompensationToken.Set(context, new CompensationToken(compensationToken));
            if (compensationToken.ExecutionTracker.Count > 0)
            {
                context.ScheduleActivity(DefaultConfirmation, new CompletionCallback(OnCompensationComplete));
            }
            else
            {
                InternalOnCompensationComplete(context, compensationExtension, compensationToken);
            }
        }
    }

    private void OnCompensationComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        InternalOnCompensationComplete(context, compensationExtension, compensationToken);
    }

    private static void InternalOnCompensationComplete(NativeActivityContext context, CompensationExtension compensationExtension, CompensationTokenData compensationToken)
    {
        switch (compensationToken.CompensationState)
        {
            case CompensationState.Canceling:
                compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.Canceled);
                break;
            case CompensationState.Compensating:
                compensationExtension.NotifyMessage(context, compensationToken.CompensationId, CompensationBookmarkName.Compensated);
                break;
            default:
                Fx.Assert(false, "CompensationState is in unexpected state!");
                break;
        }
    }

    private void OnExceptionFromHandler(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData compensationToken = compensationExtension.Get(_compensationId.Get(context));
        Fx.Assert(compensationToken != null, "CompensationTokenData must be valid");

        InvalidOperationException exception = null;

        switch (compensationToken.CompensationState)
        {
            case CompensationState.Confirming:
                exception = new InvalidOperationException(SR.ConfirmationHandlerFatalException(compensationToken.DisplayName), propagatedException);
                break;
            case CompensationState.Compensating:
                exception = new InvalidOperationException(SR.CompensationHandlerFatalException(compensationToken.DisplayName), propagatedException);
                break;
            case CompensationState.Canceling:
                exception = new InvalidOperationException(SR.CancellationHandlerFatalException(compensationToken.DisplayName), propagatedException);
                break;
            default:
                Fx.Assert(false, "CompensationState is in unexpected state!");
                break;
        }

        context.Abort(exception);
    }
}
