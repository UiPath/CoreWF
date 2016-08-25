// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Microsoft.CoreWf.Statements
{
    public sealed class FlowDecision : FlowNode
    {
        private const string DefaultDisplayName = "Decision";
        private string _displayName;

        public FlowDecision()
        {
            _displayName = FlowDecision.DefaultDisplayName;
        }

        public FlowDecision(Expression<Func<ActivityContext, bool>> condition)
            : this()
        {
            if (condition == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("condition");
            }

            this.Condition = new LambdaValue<bool>(condition);
        }

        public FlowDecision(Activity<bool> condition)
            : this()
        {
            if (condition == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("condition");
            }

            this.Condition = condition;
        }

        [DefaultValue(null)]
        public Activity<bool> Condition
        {
            get;
            set;
        }

        [DefaultValue(null)]
        //[DependsOn("Condition")]
        public FlowNode True
        {
            get;
            set;
        }

        [DefaultValue(null)]
        //[DependsOn("True")]
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
                return _displayName;
            }
            set
            {
                _displayName = value;
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
