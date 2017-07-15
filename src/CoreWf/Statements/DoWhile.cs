// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Expressions;
using CoreWf.Runtime;
using CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CoreWf.Statements
{
    //[ContentProperty("Body")]
    public sealed class DoWhile : NativeActivity
    {
        private CompletionCallback _onBodyComplete;
        private CompletionCallback<bool> _onConditionComplete;

        private Collection<Variable> _variables;

        public DoWhile()
            : base()
        {
        }

        public DoWhile(Expression<Func<ActivityContext, bool>> condition)
            : this()
        {
            if (condition == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("condition");
            }

            this.Condition = new LambdaValue<bool>(condition);
        }

        public DoWhile(Activity<bool> condition)
            : this()
        {
            if (condition == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("condition");
            }

            this.Condition = condition;
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
        public Activity<bool> Condition
        {
            get;
            set;
        }

        [DefaultValue(null)]
        //[DependsOn("Condition")]
        public Activity Body
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.SetVariablesCollection(this.Variables);

            if (this.Condition == null)
            {
                metadata.AddValidationError(SR.DoWhileRequiresCondition(this.DisplayName));
            }
            else
            {
                metadata.AddChild(this.Condition);
            }

            metadata.AddChild(this.Body);
        }

        protected override void Execute(NativeActivityContext context)
        {
            // initial logic is the same as when the condition completes with true
            OnConditionComplete(context, null, true);
        }

        private void ScheduleCondition(NativeActivityContext context)
        {
            Fx.Assert(this.Condition != null, "validated in OnOpen");
            if (_onConditionComplete == null)
            {
                _onConditionComplete = new CompletionCallback<bool>(OnConditionComplete);
            }

            context.ScheduleActivity(this.Condition, _onConditionComplete);
        }

        private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (result)
            {
                if (this.Body != null)
                {
                    if (_onBodyComplete == null)
                    {
                        _onBodyComplete = new CompletionCallback(OnBodyComplete);
                    }

                    context.ScheduleActivity(this.Body, _onBodyComplete);
                }
                else
                {
                    ScheduleCondition(context);
                }
            }
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            ScheduleCondition(context);
        }
    }
}
