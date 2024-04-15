using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Linq;
namespace System.Activities.Statements;

public partial class FlowSplit : FlowNode
{
    private const string DefaultDisplayName = nameof(FlowSplit);
    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; } = DefaultDisplayName;

    [DefaultValue(null)]
    public Collection<FlowNode> Branches
      => _branches ??= ValidatingCollection<FlowNode>.NullCheck();

    private ValidatingCollection<FlowNode> _branches;

    internal override IReadOnlyList<FlowNode> GetSuccessors()
      => Branches.ToList();

    internal override Flowchart.NodeInstance CreateInstance()
      => new SplitInstance();
}