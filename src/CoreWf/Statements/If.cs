// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Activities;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using Portable.Xaml.Markup;
    using System;
    using System.Activities.Internals;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [If])")]
    public sealed class If : NativeActivity
    {
        public If()
            : base()
        {
        }

        public If(Expression<Func<ActivityContext, bool>> condition)
            : this()
        {
            if (condition == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(condition));
            }

            this.Condition = new InArgument<bool>(condition);
        }

        public If(Activity<bool> condition)
            : this()
        {
            if (condition == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(condition));
            }

            this.Condition = new InArgument<bool>(condition);
        }

        public If(InArgument<bool> condition)
            : this()
        {
            this.Condition = condition ?? throw FxTrace.Exception.ArgumentNull(nameof(condition));
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<bool> Condition
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Condition")]
        public Activity Then
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Then")]
        public Activity Else
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

        protected override void Execute(NativeActivityContext context)
        {
            if (Condition.Get(context))
            {
                if (Then != null)
                {
                    context.ScheduleActivity(Then);
                }
            }
            else if (Else != null)
            {
                context.ScheduleActivity(Else);
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument conditionArgument = new RuntimeArgument("Condition", typeof(bool), ArgumentDirection.In, true);
            metadata.Bind(this.Condition, conditionArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { conditionArgument });

            Collection<Activity> children = null;

            if (this.Then != null)
            {
                ActivityUtilities.Add(ref children, this.Then);
            }

            if (this.Else != null)
            {
                ActivityUtilities.Add(ref children, this.Else);
            }

            metadata.SetChildrenCollection(children);
        }
    }
}
