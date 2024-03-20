// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.ParallelTracking;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

[ContentProperty("Branches")]
public sealed class Pick : NativeActivity
{
    private const string pickStateProperty = "System.Activities.Statements.Pick.PickState";
    private Collection<PickBranch> _branches;
    private readonly Variable<PickState> _pickStateVariable;
    private Collection<Activity> _branchBodies;

    public Pick()
    {
        _pickStateVariable = new Variable<PickState>();
    }

    protected override bool CanInduceIdle => true;

    public Collection<PickBranch> Branches
    {
        get
        {
            _branches ??= new ValidatingCollection<PickBranch>
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
            return _branches;
        }
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    }

    protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
    {
        PickState pickState = updateContext.GetValue(this.pickStateVariable);
        Fx.Assert(pickState != null, "Pick's Execute must have run by now.");

        if (updateContext.IsCancellationRequested || pickState.TriggerCompletionBookmark == null)
        {
            // do not schedule newly added Branches once a Trigger has successfully completed.
            return;
        }

        CompletionCallback onBranchCompleteCallback = new CompletionCallback(OnBranchComplete);
        foreach (PickBranchBody body in this.branchBodies)
        {
            if (updateContext.IsNewlyAdded(body))
            {
                updateContext.ScheduleActivity(body, onBranchCompleteCallback, null);
            }
        }
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        if (_branchBodies == null)
        {
            _branchBodies = new Collection<Activity>();
        }
        else
        {
            _branchBodies.Clear();
        }

        foreach (PickBranch branch in Branches)
        {
            if (branch.Trigger == null)
            {
                metadata.AddValidationError(new ValidationError(SR.PickBranchRequiresTrigger(branch.DisplayName), false, null, branch));
            }

            PickBranchBody pickBranchBody = new()
            {
                Action = branch.Action,
                DisplayName = branch.DisplayName,
                Trigger = branch.Trigger,
                Variables = branch.Variables,
            };

            _branchBodies.Add(pickBranchBody);

            metadata.AddChild(pickBranchBody, origin: branch);
        }

        metadata.AddImplementationVariable(_pickStateVariable);
    }

    protected override void Execute(NativeActivityContext context)
    {
        if (_branchBodies.Count == 0)
        {
            return;
        }

        PickState pickState = new();
        _pickStateVariable.Set(context, pickState);

        pickState.TriggerCompletionBookmark = context.CreateBookmark(new BookmarkCallback(OnTriggerComplete));

        context.Properties.Add(pickStateProperty, pickState);

        CompletionCallback onBranchCompleteCallback = new(OnBranchComplete);

        //schedule every branch to only run trigger
        for (int i = _branchBodies.Count - 1; i >= 0; i--)
        {
            context.ScheduleActivity(_branchBodies[i], onBranchCompleteCallback);
        }
    }

    protected override void Cancel(NativeActivityContext context) => context.CancelChildren();

    private void OnBranchComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        PickState pickState = _pickStateVariable.Get(context);
        ReadOnlyCollection<ActivityInstance> executingChildren = context.GetChildren();

        switch (completedInstance.State)
        {
            case ActivityInstanceState.Closed:
                pickState.HasBranchCompletedSuccessfully = true;
                break;
            case ActivityInstanceState.Canceled:
            case ActivityInstanceState.Faulted:
                if (context.IsCancellationRequested)
                {
                    if (executingChildren.Count == 0 && !pickState.HasBranchCompletedSuccessfully)
                    {
                        // All of the branches are complete and we haven't had a single
                        // one complete successfully and we've been asked to cancel.
                        context.MarkCanceled();
                        context.RemoveAllBookmarks();
                    }
                }
                break;
        }

        //the last branch should always resume action bookmark if it's still there
        if (executingChildren.Count == 1 && pickState.ExecuteActionBookmark != null)
        {
            ResumeExecutionActionBookmark(pickState, context);
        }
    }

    private void OnTriggerComplete(NativeActivityContext context, Bookmark bookmark, object state)
    {
        PickState pickState = _pickStateVariable.Get(context);

        string winningBranch = (string)state;

        ReadOnlyCollection<ActivityInstance> children = context.GetChildren();

        bool resumeAction = true;

        for (int i = 0; i < children.Count; i++)
        {
            ActivityInstance child = children[i];

            if (child.Id != winningBranch)
            {
                context.CancelChild(child);
                resumeAction = false;
            }
        }

        if (resumeAction)
        {
            ResumeExecutionActionBookmark(pickState, context);
        }
    }

    private static void ResumeExecutionActionBookmark(PickState pickState, NativeActivityContext context)
    {
        Fx.Assert(pickState.ExecuteActionBookmark != null, "This should have been set by the branch.");

        context.ResumeBookmark(pickState.ExecuteActionBookmark, null);
        pickState.ExecuteActionBookmark = null;
    }

    [DataContract]
    internal class PickState
    {
        [DataMember(EmitDefaultValue = false)]
        public bool HasBranchCompletedSuccessfully { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Bookmark TriggerCompletionBookmark { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Bookmark ExecuteActionBookmark { get; set; }
    }

    private class PickBranchBody : NativeActivity
    {
        public PickBranchBody() { }

        protected override bool CanInduceIdle => true;

        public Collection<Variable> Variables { get; set; }

        public Activity Trigger { get; set; }

        public Activity Action { get; set; }

#if DYNAMICUPDATE
        protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        {
            PickBranchBody originalBranchBody = (PickBranchBody)originalActivity;
            if ((originalBranchBody.Action != null && metadata.GetMatch(this.Trigger) == originalBranchBody.Action) || (this.Action != null && metadata.GetMatch(this.Action) == originalBranchBody.Trigger))
            {
                metadata.DisallowUpdateInsideThisActivity(SR.PickBranchTriggerActionSwapped);
                return;
            }

            metadata.AllowUpdateInsideThisActivity();
        } 
#endif

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            Collection<Activity> children = null;

            if (Trigger != null)
            {
                ActivityUtilities.Add(ref children, Trigger);
            }
            if (Action != null)
            {
                ActivityUtilities.Add(ref children, Action);
            }

            metadata.SetChildrenCollection(children);

            metadata.SetVariablesCollection(Variables);
        }

        protected override void Execute(NativeActivityContext context)
        {
            Fx.Assert(Trigger != null, "We validate that the trigger is not null in Pick.CacheMetadata");

            context.ScheduleActivity(Trigger, new CompletionCallback(OnTriggerCompleted)).MarkNewParallelBranch();
        }

        private void OnTriggerCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            PickState pickState = (PickState)context.Properties.Find(pickStateProperty);

            if (completedInstance.State == ActivityInstanceState.Closed && pickState.TriggerCompletionBookmark != null)
            {
                // We're the first trigger!  We win!
                context.ResumeBookmark(pickState.TriggerCompletionBookmark, context.ActivityInstanceId);
                pickState.TriggerCompletionBookmark = null;
                pickState.ExecuteActionBookmark = context.CreateBookmark(new BookmarkCallback(OnExecuteAction));
            }
            else if (!context.IsCancellationRequested)
            {
                // We didn't win, but we haven't been requested to cancel yet.
                // We'll just create a bookmark to keep ourselves from completing.
                context.CreateBookmark();
            }
            // else
            // {
            //     No need for an else since default cancelation will cover it!
            // }
        }

        private void OnExecuteAction(NativeActivityContext context, Bookmark bookmark, object state)
        {
            if (Action != null)
            {
                context.ScheduleActivity(Action);
            }
        }
    }
}
