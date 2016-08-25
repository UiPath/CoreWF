// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Statements
{
    //[ContentProperty("Activities")]
    public sealed class Sequence : NativeActivity
    {
        private Collection<Activity> _activities;
        private Collection<Variable> _variables;
        private Variable<int> _lastIndexHint;
        private CompletionCallback _onChildComplete;

        public Sequence()
            : base()
        {
            _lastIndexHint = new Variable<int>();
            _onChildComplete = new CompletionCallback(InternalExecute);
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
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _variables;
            }
        }

        //[DependsOn("Variables")]
        public Collection<Activity> Activities
        {
            get
            {
                if (_activities == null)
                {
                    _activities = new ValidatingCollection<Activity>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _activities;
            }
        }

        //protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    // Our algorithm for recovering from update depends on iterating a unique Activities list.
        //    // So we can't support update if the same activity is referenced more than once.
        //    for (int i = 0; i < this.Activities.Count - 1; i++)
        //    {
        //        for (int j = i + 1; j < this.Activities.Count; j++)
        //        {
        //            if (this.Activities[i] == this.Activities[j])
        //            {
        //                metadata.DisallowUpdateInsideThisActivity(SR.SequenceDuplicateReferences);
        //                break;
        //            }
        //        }
        //    }
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.SetChildrenCollection(this.Activities);
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(_lastIndexHint);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (_activities != null && this.Activities.Count > 0)
            {
                Activity nextChild = this.Activities[0];

                context.ScheduleActivity(nextChild, _onChildComplete);
            }
        }

        private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance)
        {
            int completedInstanceIndex = _lastIndexHint.Get(context);

            if (completedInstanceIndex >= this.Activities.Count || this.Activities[completedInstanceIndex] != completedInstance.Activity)
            {
                completedInstanceIndex = this.Activities.IndexOf(completedInstance.Activity);
            }

            int nextChildIndex = completedInstanceIndex + 1;

            if (nextChildIndex == this.Activities.Count)
            {
                return;
            }

            Activity nextChild = this.Activities[nextChildIndex];

            context.ScheduleActivity(nextChild, _onChildComplete);

            _lastIndexHint.Set(context, nextChildIndex);
        }
    }
}
