// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Branches")]
    public sealed class Parallel : NativeActivity
    {
        private CompletionCallback<bool> _onConditionComplete;
        private Collection<Activity> _branches;
        private Collection<Variable> _variables;

        private Variable<bool> _hasCompleted;

        public Parallel()
            : base()
        {
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (_variables == null)
                {
                    _variables = new ValidatingCollection<Variable>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _variables;
            }
        }

        [DefaultValue(null)]
        //[DependsOn("Variables")]
        public Activity<bool> CompletionCondition
        {
            get;
            set;
        }

        //[DependsOn("CompletionCondition")]
        public Collection<Activity> Branches
        {
            get
            {
                if (_branches == null)
                {
                    _branches = new ValidatingCollection<Activity>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _branches;
            }
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        //protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
        //{
        //    if (updateContext.IsCancellationRequested || this.branches == null)
        //    {
        //        return;
        //    }

        //    if (this.CompletionCondition != null && updateContext.GetValue(this.hasCompleted))
        //    {
        //        // when CompletionCondition exists, schedule newly added branches only if "hasCompleted" variable evaluates to false
        //        return;
        //    }           

        //    CompletionCallback onBranchComplete = new CompletionCallback(OnBranchComplete);

        //    foreach (Activity branch in this.branches)
        //    {
        //        if (updateContext.IsNewlyAdded(branch))
        //        {
        //            updateContext.ScheduleActivity(branch, onBranchComplete);
        //        }
        //    }
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            Collection<Activity> children = new Collection<Activity>();

            foreach (Activity branch in this.Branches)
            {
                children.Add(branch);
            }

            if (this.CompletionCondition != null)
            {
                children.Add(this.CompletionCondition);
            }

            metadata.SetChildrenCollection(children);

            metadata.SetVariablesCollection(this.Variables);

            if (this.CompletionCondition != null)
            {
                if (_hasCompleted == null)
                {
                    _hasCompleted = new Variable<bool>("hasCompletedVar");
                }

                metadata.AddImplementationVariable(_hasCompleted);
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (_branches != null && this.Branches.Count != 0)
            {
                CompletionCallback onBranchComplete = new CompletionCallback(OnBranchComplete);

                for (int i = this.Branches.Count - 1; i >= 0; i--)
                {
                    context.ScheduleActivity(this.Branches[i], onBranchComplete);
                }
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            // If we don't have a completion condition then we can just
            // use default logic.
            if (this.CompletionCondition == null)
            {
                base.Cancel(context);
            }
            else
            {
                context.CancelChildren();
            }
        }

        private void OnBranchComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (this.CompletionCondition != null && !_hasCompleted.Get(context))
            {
                // If we haven't completed, we've been requested to cancel, and we've had a child
                // end in a non-Closed state then we should cancel ourselves.
                if (completedInstance.State != ActivityInstanceState.Closed && context.IsCancellationRequested)
                {
                    context.MarkCanceled();
                    _hasCompleted.Set(context, true);
                    return;
                }

                if (_onConditionComplete == null)
                {
                    _onConditionComplete = new CompletionCallback<bool>(OnConditionComplete);
                }

                context.ScheduleActivity(this.CompletionCondition, _onConditionComplete);
            }
        }

        private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (result)
            {
                context.CancelChildren();
                _hasCompleted.Set(context, true);
            }
        }
    }
}
