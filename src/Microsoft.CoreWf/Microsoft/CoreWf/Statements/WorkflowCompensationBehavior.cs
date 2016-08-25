// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Statements
{
    internal sealed class WorkflowCompensationBehavior : NativeActivity
    {
        private Variable<CompensationToken> _currentCompensationToken;

        public WorkflowCompensationBehavior()
            : base()
        {
            _currentCompensationToken = new Variable<CompensationToken>
            {
                Name = "currentCompensationToken",
            };

            DefaultCompensation = new DefaultCompensation()
            {
                Target = new InArgument<CompensationToken>(_currentCompensationToken),
            };

            DefaultConfirmation = new DefaultConfirmation()
            {
                Target = new InArgument<CompensationToken>(_currentCompensationToken),
            };
        }

        private Activity DefaultCompensation
        {
            get;
            set;
        }

        private Activity DefaultConfirmation
        {
            get;
            set;
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            Fx.Assert(this.DefaultCompensation != null, "DefaultCompensation must be valid");
            Fx.Assert(this.DefaultConfirmation != null, "DefaultConfirmation must be valid");
            metadata.SetImplementationChildrenCollection(
                new Collection<Activity>
                {
                    this.DefaultCompensation,
                    this.DefaultConfirmation
                });

            metadata.SetImplementationVariablesCollection(new Collection<Variable> { _currentCompensationToken });
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

        protected override void Cancel(NativeActivityContext context)
        {
            context.CancelChildren();
        }

        private void OnMainRootComplete(NativeActivityContext context, Bookmark bookmark, object value)
        {
            CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
            Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

            CompensationTokenData rootHandle = compensationExtension.Get(CompensationToken.RootCompensationId);
            Fx.Assert(rootHandle != null, "rootToken must be valid");

            ActivityInstanceState completionState = (ActivityInstanceState)value;

            if (completionState == ActivityInstanceState.Closed)
            {
                context.ResumeBookmark(compensationExtension.WorkflowConfirmation, new CompensationToken(rootHandle));
            }
            else if (completionState == ActivityInstanceState.Canceled)
            {
                context.ResumeBookmark(compensationExtension.WorkflowCompensation, new CompensationToken(rootHandle));
            }
            else if (completionState == ActivityInstanceState.Faulted)
            {
                // Do nothing. Neither Compensate nor Confirm.
                // Remove the bookmark to complete the WorkflowCompensationBehavior execution. 
                context.RemoveBookmark(compensationExtension.WorkflowConfirmation);
                context.RemoveBookmark(compensationExtension.WorkflowCompensation);
            }
        }

        private void OnCompensate(NativeActivityContext context, Bookmark bookmark, object value)
        {
            CompensationExtension compensationExtension = context.GetExtension<CompensationExtension>();
            Fx.Assert(compensationExtension != null, "CompensationExtension must be valid");

            CompensationToken rootToken = (CompensationToken)value;
            Fx.Assert(rootToken != null, "rootToken must be passed");

            _currentCompensationToken.Set(context, rootToken);

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

            _currentCompensationToken.Set(context, rootToken);

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
}


