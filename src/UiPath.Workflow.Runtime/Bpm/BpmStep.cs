// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Action")]
public sealed class BpmStep : BpmNode
{
    public BpmStep() { }

    [DefaultValue(null)]
    public Activity Action { get; set; }

    [DefaultValue(null)]
    [DependsOn("Action")]
    public BpmNode Next { get; set; }

    internal override void OnOpen(BpmFlowchart owner, NativeActivityMetadata metadata) { }

    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }

    internal override Activity ChildActivity => Action;

    internal bool Execute(NativeActivityContext context, CompletionCallback onCompleted, out BpmNode nextNode)
    {
        if (Next == null)
        {
            if (TD.FlowchartNextNullIsEnabled())
            {
                TD.FlowchartNextNull(Owner.DisplayName);
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
