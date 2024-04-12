using System.Activities.Runtime.Collections;
using System.Activities.Validation;
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

    protected override void OnEndCacheMetadata()
    {
        HashSet<FlowMerge> allMerges = new();
        foreach (var branch in Branches)
        {
            var merges = Flowchart.GetMerges(branch).Distinct().ToList();
            allMerges.AddRange(merges);
            if (merges.Count > 1)
                AddValidationError("Split branch should end in only one Merge node.", new[] { branch }.Concat(merges));
        }
        if (allMerges.Count == 0)
            AddValidationError("Split should end in one Merge node.");
        if (allMerges.Count > 1)
            AddValidationError("All split branches should end in only one Merge node.", allMerges);
        ValidateSingleSplitInAmonte();
    }

    internal override Flowchart.NodeInstance CreateInstance()
        => new SplitInstance();
}