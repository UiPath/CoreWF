// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Activities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Activities.Runtime.Collections;
    using System.Windows.Markup;
    using System.Activities.Runtime;

    [ContentProperty("Cases")]
    public sealed class FlowSwitch<T> : FlowNode, IFlowSwitch
    {
        private const string DefaultDisplayName = "Switch";
        internal IDictionary<T, FlowNode> cases;
        private CompletionCallback<T> onSwitchCompleted;
        private string displayName;

        public FlowSwitch()
        {
            this.cases = new NullableKeyDictionary<T, FlowNode>();
            this.displayName = FlowSwitch<T>.DefaultDisplayName;
        }

        [DefaultValue(null)]
        public Activity<T> Expression
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public FlowNode Default
        {
            get;
            set;
        }

        [Fx.Tag.KnownXamlExternal]
        public IDictionary<T, FlowNode> Cases
        {
            get
            {
                return this.cases;
            }
        }

        [DefaultValue(FlowSwitch<T>.DefaultDisplayName)]
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
            if (this.Expression == null)
            {
                metadata.AddValidationError(SR.FlowSwitchRequiresExpression(owner.DisplayName));
            }
        }

        internal override void GetConnectedNodes(IList<FlowNode> connections)
        {
            foreach (KeyValuePair<T, FlowNode> item in this.Cases)
            {
                connections.Add(item.Value);
            }
            if (this.Default != null)
            {
                connections.Add(this.Default);
            }
        }

        internal override Activity ChildActivity
        {
            get { return Expression; }
        }

        bool IFlowSwitch.Execute(NativeActivityContext context, Flowchart parent)
        {
            context.ScheduleActivity(Expression, this.GetSwitchCompletedCallback(parent));
            return false;
        }

        FlowNode IFlowSwitch.GetNextNode(object value)
        {
            T newValue = (T)value;
            if (Cases.TryGetValue(newValue, out FlowNode result))
            {
                if (TD.FlowchartSwitchCaseIsEnabled())
                {
                    TD.FlowchartSwitchCase(this.Owner.DisplayName, newValue.ToString());
                }
                return result;
            }
            else
            {
                if (this.Default != null)
                {
                    if (TD.FlowchartSwitchDefaultIsEnabled())
                    {
                        TD.FlowchartSwitchDefault(this.Owner.DisplayName);
                    }
                }
                else
                {
                    if (TD.FlowchartSwitchCaseNotFoundIsEnabled())
                    {
                        TD.FlowchartSwitchCaseNotFound(this.Owner.DisplayName);
                    }
                }
                return this.Default;
            }
        }

        private CompletionCallback<T> GetSwitchCompletedCallback(Flowchart parent)
        {
            if (onSwitchCompleted == null)
            {
                onSwitchCompleted = new CompletionCallback<T>(parent.OnSwitchCompleted<T>);
            }
            return onSwitchCompleted;
        }
    }
}
