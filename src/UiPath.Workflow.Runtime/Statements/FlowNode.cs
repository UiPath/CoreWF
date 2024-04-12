using System.Activities.Validation;
using System.Linq;
// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public abstract class FlowNode
{
    private Flowchart _owner;
                            
    internal FlowNode()
    {
        Index = -1;
    }

    internal int Index { get; set; }

    internal Flowchart Flowchart => _owner;
    protected NativeActivityMetadata Metadata { get; private set; }
 
    internal void CacheMetadata(Flowchart owner, NativeActivityMetadata metadata)
    { 
        _owner = owner;
        Metadata = metadata;
        OnCacheMetadata();
    }
    internal void EndCacheMetadata(NativeActivityMetadata metadata)
    {
        Metadata = metadata;
        OnEndCacheMetadata();
    }
    protected virtual void OnCacheMetadata() { }
    protected virtual void OnEndCacheMetadata()
    {
        ValidateSingleSplitInAmonte();
    }

    protected void ValidateSingleSplitInAmonte()
    {
        var splits = Flowchart.GetStaticSplitsStack(this).GetTop();
        if (splits.Count > 1)
            AddValidationError($"Node has multiple splits incoming branches. Please precede with a Merge node.", splits);
    }

    internal virtual IEnumerable<Activity> GetChildActivities() => null;
    internal abstract IReadOnlyList<FlowNode> GetSuccessors();


    internal abstract Flowchart.NodeInstance CreateInstance();

    protected void AddValidationError(string message, IEnumerable<FlowNode> nodes = null)
    {
        Metadata.AddValidationError(new ValidationError(message)
        {
            SourceDetail = new[] { this }.Concat(nodes ?? Array.Empty<FlowNode>()).ToArray()
        });
    }

    public override string ToString() => GetType().Name;
}
