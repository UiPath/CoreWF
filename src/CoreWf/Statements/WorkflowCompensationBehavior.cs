// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;

namespace System.Activities.Statements;

internal sealed class WorkflowCompensationBehavior : NativeActivity
{
    private readonly Variable<CompensationToken> currentCompensationToken;

    public WorkflowCompensationBehavior()
        : base()
    {
        currentCompensationToken = new Variable<CompensationToken>
            {
                Name = "currentCompensationToken",
            };

        DefaultCompensation = new DefaultCompensation()
            {
                Target = new InArgument<CompensationToken>(currentCompensationToken),
            };

        DefaultConfirmation = new DefaultConfirmation()
            {
                Target = new InArgument<CompensationToken>(currentCompensationToken),
            };
    }

    private Activity DefaultCompensation { get; set; }

    private Activity DefaultConfirmation { get; set; }

    protected override bool CanInduceIdle => true;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        Fx.Assert(DefaultCompensation != null, "DefaultCompensation must be valid");
        Fx.Assert(DefaultConfirmation != null, "DefaultConfirmation must be valid");
        metadata.SetImplementationChildrenCollection(
            new Collection<Activity>
            {
                DefaultCompensation, 
                DefaultConfirmation
            });

        metadata.SetImplementationVariablesCollection(new Collection<Variable> { currentCompensationToken });
    }

    protected override void Execute(NativeActivityContext context)
    {
        Bookmark mainRootCompleteBookmark = context.CreateBookmark(OnMainRootComplete, BookmarkOptions.NonBlocking);
        context.RegisterMainRootCompleteCallback(mainRootCompleteBookmark);

        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        compensationExtension.WorkflowCompensation = context.CreateBookmark(new BookmarkCallback(OnCompensate));
        compensationExtension.WorkflowConfirmation = context.CreateBookmark(new BookmarkCallback(OnConfirm));

        Fx.Assert(compensationExtension.WorkflowCompensationScheduled != null, "compensationExtension.WorkflowCompensationScheduled bookmark must be setup by now");
        context.ResumeBookmark(compensationExtension.WorkflowCompensationScheduled, null);
    }

    protected override void Cancel(NativeActivityContext context) => context.CancelChildren();

    private void OnMainRootComplete(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationTokenData rootHandle = compensationExtension.Get(CompensationToken.RootCompensationId);
        Fx.Assert(rootHandle != null, "rootToken must be valid");

        ActivityInstanceState completionState = (ActivityInstanceState)value;

        switch (completionState)
        {
            case ActivityInstanceState.Closed:
                context.ResumeBookmark(compensationExtension.WorkflowConfirmation, new CompensationToken(rootHandle));
                break;
            case ActivityInstanceState.Canceled:
                context.ResumeBookmark(compensationExtension.WorkflowCompensation, new CompensationToken(rootHandle));
                break;
            case ActivityInstanceState.Faulted:
                // Do nothing. Neither Compensate nor Confirm.
                // Remove the bookmark to complete the WorkflowCompensationBehavior execution. 
                context.RemoveBookmark(compensationExtension.WorkflowConfirmation);
                context.RemoveBookmark(compensationExtension.WorkflowCompensation);
                break;
        }
    }

    private void OnCompensate(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationToken rootToken = (CompensationToken)value;
        Fx.Assert(rootToken != null, "rootToken must be passed");

        currentCompensationToken.Set(context, rootToken);

        CompensationTokenData rootTokenData = compensationExtension.Get(rootToken.CompensationId);
        if (rootTokenData.ExecutionTracker.Count > 0)
        {
            context.ScheduleActivity(DefaultCompensation, new CompletionCallback(OnCompensationComplete));
        }
        else
        {
            OnCompensationComplete(context, null);
        }
    }

    private void OnCompensationComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        // Remove bookmark.... have a cleanup book mark method...
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        context.RemoveBookmark(compensationExtension.WorkflowConfirmation);
    }

    private void OnConfirm(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationToken rootToken = (CompensationToken)value;
        Fx.Assert(rootToken != null, "rootToken must be passed");

        currentCompensationToken.Set(context, rootToken);

        CompensationTokenData rootTokenData = compensationExtension.Get(rootToken.CompensationId);
        if (rootTokenData.ExecutionTracker.Count > 0)
        {
            context.ScheduleActivity(DefaultConfirmation, new CompletionCallback(OnConfirmationComplete));
        }
        else
        {
            OnConfirmationComplete(context, null);
        }
    }

    private void OnConfirmationComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        context.RemoveBookmark(compensationExtension.WorkflowCompensation);
    }
}
