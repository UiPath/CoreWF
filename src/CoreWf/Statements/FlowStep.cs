// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Action")]
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
        //[DependsOn("Action")]
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
