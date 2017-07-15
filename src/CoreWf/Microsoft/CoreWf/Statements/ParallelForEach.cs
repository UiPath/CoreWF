// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Body")]
    public sealed class ParallelForEach<T> : NativeActivity
    {
        private Variable<bool> _hasCompleted;
        private CompletionCallback<bool> _onConditionComplete;

        public ParallelForEach()
            : base()
        {
        }

        [DefaultValue(null)]
        public ActivityAction<T> Body
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public Activity<bool> CompletionCondition
        {
            get;
            set;
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<IEnumerable<T>> Values
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument valuesArgument = new RuntimeArgument("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true);
            metadata.Bind(this.Values, valuesArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { valuesArgument });

            // declare the CompletionCondition as a child
            if (this.CompletionCondition != null)
            {
                metadata.SetChildrenCollection(new Collection<Activity> { this.CompletionCondition });
            }

            // declare the hasCompleted variable
            if (this.CompletionCondition != null)
            {
                if (_hasCompleted == null)
                {
                    _hasCompleted = new Variable<bool>("hasCompletedVar");
                }

                metadata.AddImplementationVariable(_hasCompleted);
            }

            metadata.AddDelegate(this.Body);
        }

        protected override void Execute(NativeActivityContext context)
        {
            IEnumerable<T> values = this.Values.Get(context);
            if (values == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ParallelForEachRequiresNonNullValues(this.DisplayName)));
            }

            IEnumerator<T> valueEnumerator = values.GetEnumerator();

            CompletionCallback onBodyComplete = new CompletionCallback(OnBodyComplete);
            while (valueEnumerator.MoveNext())
            {
                if (this.Body != null)
                {
                    context.ScheduleAction(this.Body, valueEnumerator.Current, onBodyComplete);
                }
            }
            valueEnumerator.Dispose();
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            // for the completion condition, we handle cancelation ourselves
            if (this.CompletionCondition != null && !_hasCompleted.Get(context))
            {
                if (completedInstance.State != ActivityInstanceState.Closed && context.IsCancellationRequested)
                {
                    // If we hadn't completed before getting canceled
                    // or one of our iteration of body cancels then we'll consider
                    // ourself canceled.
                    context.MarkCanceled();
                    _hasCompleted.Set(context, true);
                }
                else
                {
                    if (_onConditionComplete == null)
                    {
                        _onConditionComplete = new CompletionCallback<bool>(OnConditionComplete);
                    }
                    context.ScheduleActivity(CompletionCondition, _onConditionComplete);
                }
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
