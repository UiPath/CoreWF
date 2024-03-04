using System.Activities.Runtime.Collections;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.ObjectModel;
namespace System.Activities.Bpm;

public class FlowParallel : FlowNodeExtensible
{
    private ValidatingCollection<FlowNode> _branches;
    internal override Activity ChildActivity => null;
    [DefaultValue(null)]
    public Collection<FlowNode> Branches => _branches ??= ValidatingCollection<FlowNode>.NullCheck();

    internal override void GetConnectedNodes(IList<FlowNode> connections)
        => connections.AddRange(Branches);

    internal override void Execute(NativeActivityContext context)
    {
        foreach (FlowNode node in _branches)
        {
            Owner.ExecuteNextNode(context, node, context.CurrentInstance);
        }
    }
}