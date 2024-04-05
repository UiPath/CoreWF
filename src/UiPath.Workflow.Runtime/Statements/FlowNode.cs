// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements.Interfaces;

namespace System.Activities.Statements;

public abstract class FlowNode: IFlowNode
{
    private Flowchart _owner;
    private int _cacheId;

    internal FlowNode()
    {
        Index = -1;
    }

    internal abstract Activity ChildActivity { get; }

    internal int Index { get; set; }

    internal bool IsOpen => _owner != null;

    internal Flowchart Owner => _owner;

    // Returns true if this is the first time we've visited this node during this pass
    internal bool Open(Flowchart owner, NativeActivityMetadata metadata)
    {
        if (_cacheId == owner.CacheId)
        {
            // We've already visited this node during this pass
            if (!ReferenceEquals(_owner, owner))
            {
                metadata.AddValidationError(SR.FlowNodeCannotBeShared(_owner.DisplayName, owner.DisplayName));
            }

            // Whether we found an issue or not we don't want to change
            // the metadata during this pass.
            return false;
        }

        // if owner.ValidateUnconnectedNodes - Flowchart will be responsible for calling OnOpen for all the Nodes (connected and unconnected)
        if (!owner.ValidateUnconnectedNodes)
        {
            OnOpen(owner, metadata);
        }
        _owner = owner;
        _cacheId = owner.CacheId;
        Index = -1;

        return true;
    }

    internal abstract void OnOpen(Flowchart owner, NativeActivityMetadata metadata);

    internal void GetChildActivities(ICollection<Activity> children)
    {
        if (ChildActivity != null)
        {
            children.Add(ChildActivity);
        }
    }

    internal abstract void GetConnectedNodes(IList<FlowNode> connections);

    IEnumerable<IFlowNode> IFlowNode.GetConnectedNodes()
    {
        var result = new List<FlowNode>();
        GetConnectedNodes(result);
        return result;
    }
}
