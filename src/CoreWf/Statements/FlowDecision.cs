// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Windows.Markup;
    using System.Activities.Internals;

    public sealed class FlowDecision : FlowNode
    {
        private const string DefaultDisplayName = "Decision";
        private string displayName;

        public FlowDecision()
        {
            this.displayName = FlowDecision.DefaultDisplayName;
        }

        public FlowDecision(Expression<Func<ActivityContext, bool>> condition)
            : this()
        {
            if (condition == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(condition));
            }

            this.Condition = new LambdaValue<bool>(condition);
        }

        public FlowDecision(Activity<bool> condition)
            : this()
        {
            this.Condition = condition ?? throw FxTrace.Exception.ArgumentNull(nameof(condition));
        }

        [DefaultValue(null)]
        public Activity<bool> Condition
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Condition")]
        public FlowNode True
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("True")]
        public FlowNode False
        {
            get;
            set;
        }

        [DefaultValue(FlowDecision.DefaultDisplayName)]
        public string DisplayName
        {
            get
            {
                return this.displayName;
            }
            set
            {
                this.displayName = value;
            }
        }

        internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
        {
            if (this.Condition == null)
            {
                metadata.AddValidationError(SR.FlowDecisionRequiresCondition(owner.DisplayName));
            }
        }

        internal override void GetConnectedNodes(IList<FlowNode> connections)
        {
            if (True != null)
            {
                connections.Add(True);
            }

            if (False != null)
            {
                connections.Add(False);
            }
        }

        internal override Activity ChildActivity
        {
            get { return Condition; }
        }

        internal bool Execute(NativeActivityContext context, CompletionCallback<bool> onConditionCompleted)
        {
            context.ScheduleActivity(Condition, onConditionCompleted);
            return false;
        }
    }
}
