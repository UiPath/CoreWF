using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
namespace System.Activities.Statements;
public class BpmParallel : BpmNode
{
    private ValidatingCollection<BpmNode> _branches;
    [DefaultValue(null)]
    public Collection<BpmNode> Branches => _branches ??= ValidatingCollection<BpmNode>.NullCheck();
    protected override void Execute(NativeActivityContext context)
    {
        foreach (var branch in Branches)
        {
            context.ScheduleActivity(branch);
        }
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections) => connections.AddRange(Branches);
}