// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;

namespace System.Activities.Statements;

internal sealed class InternalConfirm : NativeActivity
{
    public InternalConfirm()
        : base() { }

    public InArgument<CompensationToken> Target { get; set; }

    protected override bool CanInduceIdle => true;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument targetArgument = new RuntimeArgument("Target", typeof(CompensationToken), ArgumentDirection.In);
        metadata.Bind(Target, targetArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { targetArgument });
    }

    protected override void Execute(NativeActivityContext context)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationToken compensationToken = Target.Get(context);
        Fx.Assert(compensationToken != null, "compensationToken must be valid");

        // The compensationToken should be a valid one at this point. Ensure its validated in Confirm activity.
        CompensationTokenData tokenData = compensationExtension.Get(compensationToken.CompensationId);
        Fx.Assert(tokenData != null, "The compensationToken should be a valid one at this point. Ensure its validated in Confirm activity.");

        Fx.Assert(tokenData.BookmarkTable[CompensationBookmarkName.Confirmed] == null, "Bookmark should not be already initialized in the bookmark table.");
        tokenData.BookmarkTable[CompensationBookmarkName.Confirmed] = context.CreateBookmark(new BookmarkCallback(OnConfirmed));

        tokenData.CompensationState = CompensationState.Confirming;
        compensationExtension.NotifyMessage(context, tokenData.CompensationId, CompensationBookmarkName.OnConfirmation);
    }

    // Successfully received Confirmed response.
    private void OnConfirmed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
        Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

        CompensationToken compensationToken = Target.Get(context);
        Fx.Assert(compensationToken != null, "compensationToken must be valid");

        // The compensationToken should be a valid one at this point. Ensure its validated in Confirm activity.
        CompensationTokenData tokenData = compensationExtension.Get(compensationToken.CompensationId);
        Fx.Assert(tokenData != null, "The compensationToken should be a valid one at this point. Ensure its validated in Confirm activity.");

        tokenData.CompensationState = CompensationState.Confirmed;
        if (TD.CompensationStateIsEnabled())
        {
            TD.CompensationState(tokenData.DisplayName, tokenData.CompensationState.ToString());
        }

        // Remove the token from the parent! 
        if (tokenData.ParentCompensationId != CompensationToken.RootCompensationId)
        {
            CompensationTokenData parentToken = compensationExtension.Get(tokenData.ParentCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Remove(tokenData);
        }
        else
        {
            // remove from workflow root...
            CompensationTokenData parentToken = compensationExtension.Get(CompensationToken.RootCompensationId);
            Fx.Assert(parentToken != null, "parentToken must be valid");

            parentToken.ExecutionTracker.Remove(tokenData);
        }

        tokenData.RemoveBookmark(context, CompensationBookmarkName.Confirmed);

        // Remove the token from the extension...
        compensationExtension.Remove(compensationToken.CompensationId);
    }

    protected override void Cancel(NativeActivityContext context)
    {
        // Suppress Cancel   
    }
}
