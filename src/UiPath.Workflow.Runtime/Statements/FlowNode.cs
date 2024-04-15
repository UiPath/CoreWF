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
    protected virtual void OnCacheMetadata() { }

    internal virtual IEnumerable<Activity> GetChildActivities() => null;
    internal abstract IReadOnlyList<FlowNode> GetSuccessors();

    internal abstract Flowchart.NodeInstance CreateInstance();

    public override string ToString() => GetType().Name;
}
