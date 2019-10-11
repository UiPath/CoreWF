// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Activities.Expressions;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Activities.Runtime.Collections;
    using System.Windows.Markup;
    using System;
    using System.Activities.Internals;
    using System.Activities.Runtime;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [While])")]
    [ContentProperty("Body")]
    public sealed class While : NativeActivity
    {
        private CompletionCallback onBodyComplete;
        private CompletionCallback<bool> onConditionComplete;
        private Collection<Variable> variables;

        public While()
            : base()
        {
        }

        public While(Expression<Func<ActivityContext, bool>> condition)
            : this()
        {
            if (condition == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(condition));
            }

            this.Condition = new LambdaValue<bool>(condition);
        }

        public While(Activity<bool> condition)
            : this()
        {
            this.Condition = condition ?? throw FxTrace.Exception.ArgumentNull(nameof(condition));
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (this.variables == null)
                {
                    this.variables = new ValidatingCollection<Variable>
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
                }
                return this.variables;
            }
        }

        [DefaultValue(null)]
        [DependsOn("Variables")]
        public Activity<bool> Condition
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Condition")]
        public Activity Body
        {
            get;
            set;
        }

#if NET45
        protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        {
            metadata.AllowUpdateInsideThisActivity();
        }
#endif

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.SetVariablesCollection(this.Variables);

            metadata.AddChild(this.Body);

            if (this.Condition == null)
            {
                metadata.AddValidationError(SR.WhileRequiresCondition(this.DisplayName));
            }
            else
            {
                metadata.AddChild(this.Condition);
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            ScheduleCondition(context);
        }

        private void ScheduleCondition(NativeActivityContext context)
        {
            Fx.Assert(this.Condition != null, "validated in OnOpen");
            if (this.onConditionComplete == null)
            {
                this.onConditionComplete = new CompletionCallback<bool>(OnConditionComplete);
            }

            context.ScheduleActivity(this.Condition, this.onConditionComplete);
        }

        private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (result)
            {
                if (this.Body != null)
                {
                    if (this.onBodyComplete == null)
                    {
                        this.onBodyComplete = new CompletionCallback(OnBodyComplete);
                    }

                    context.ScheduleActivity(this.Body, this.onBodyComplete);
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
