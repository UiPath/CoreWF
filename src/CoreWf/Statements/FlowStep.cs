// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System.Collections.Generic;
    using CoreWf;
    using System.ComponentModel;
    using Portable.Xaml.Markup;

    [ContentProperty("Action")]
    public sealed class FlowStep : FlowNode
    {
        public FlowStep()
        {
        }

        [DefaultValue(null)]
        public Activity Action
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Action")]
        public FlowNode Next
        {
            get;
            set;
        }

        internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
        {
        }

        internal override void GetConnectedNodes(IList<FlowNode> connections)
        {
            if (Next != null)
            {
                connections.Add(Next);
            }
        }

        internal override Activity ChildActivity
        {
            get { return Action; }
        }

        internal bool Execute(NativeActivityContext context, CompletionCallback onCompleted, out FlowNode nextNode)
        {
            if (Next == null)
            {
                if (TD.FlowchartNextNullIsEnabled())
                {
                    TD.FlowchartNextNull(this.Owner.DisplayName);
                }
            }    
            if (Action == null)
            {
                nextNode = Next;
                return true;
            }
            else
            {
                context.ScheduleActivity(Action, onCompleted);
                nextNode = null;
                return false;
            }
        }
    }
}
