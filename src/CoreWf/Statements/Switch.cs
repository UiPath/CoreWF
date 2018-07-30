// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System;
    using CoreWf;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using Portable.Xaml.Markup;
    using CoreWf.Runtime.Collections;
    using CoreWf.Internals;

    [ContentProperty("Cases")]
    public sealed class Switch<T> : NativeActivity  
    {
        private IDictionary<T, Activity> cases;

        public Switch()
        {
        }

        public Switch(Expression<Func<ActivityContext, T>> expression)
            : this()
        {
            if (expression == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(expression));
            }

            this.Expression = new InArgument<T>(expression);
        }

        public Switch(Activity<T> expression)
            : this()
        {
            if (expression == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(expression));
            }

            this.Expression = new InArgument<T>(expression);
        }

        public Switch(InArgument<T> expression)
            : this()
        {
            this.Expression = expression ?? throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<T> Expression
        {
            get; 
            set;
        }

        public IDictionary<T, Activity> Cases
        {
            get
            {
                if (this.cases == null)
                {
                    this.cases = new NullableKeyDictionary<T, Activity>();
                }
                return this.cases;
            }
        }

        [DefaultValue(null)]
        public Activity Default
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
            RuntimeArgument expressionArgument = new RuntimeArgument("Expression", typeof(T), ArgumentDirection.In, true);
            metadata.Bind(Expression, expressionArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { expressionArgument });

            Collection<Activity> children = new Collection<Activity>();

            foreach (Activity child in Cases.Values)
            {
                children.Add(child);
            }

            if (Default != null)
            {
                children.Add(Default);
            }

            metadata.SetChildrenCollection(children);
        }

        protected override void Execute(NativeActivityContext context)
        {
            T result = Expression.Get(context);

            if (!Cases.TryGetValue(result, out Activity selection))
            {
                if (this.Default != null)
                {
                    selection = this.Default;
                }
                else
                {
                    if (TD.SwitchCaseNotFoundIsEnabled())
                    {
                        TD.SwitchCaseNotFound(this.DisplayName);
                    }
                }
            }

            if (selection != null)
            {
                context.ScheduleActivity(selection);
            }
        }
    }
}
