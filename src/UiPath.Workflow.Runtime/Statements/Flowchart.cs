// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Nodes")]
public sealed partial class Flowchart : NativeActivity
{
    private Collection<Variable> _variables;
    private Collection<FlowNode> _nodes;
    private readonly Collection<FlowNode> _reachableNodes = new();
    private readonly Dictionary<FlowNode, StaticNodeStackInfo> _staticBranchesByNode = new();

    [DefaultValue(false)]
    public bool ValidateUnconnectedNodes { get; set; }

    public Collection<Variable> Variables => _variables ??= ValidatingCollection<Variable>.NullCheck();

    [DependsOn("Variables")]
    public FlowNode StartNode { get; set; }

    [DependsOn("StartNode")]
    public Collection<FlowNode> Nodes => _nodes ??= ValidatingCollection<FlowNode>.NullCheck();

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetVariablesCollection(Variables);
        metadata.AddImplementationVariable(_flowchartState);

        GatherReachableNodes();
        if (ValidateUnconnectedNodes && (_reachableNodes.Count < Nodes.Count))
        {
            metadata.AddValidationError(SR.FlowchartContainsUnconnectedNodes(DisplayName));
        }
        HashSet<Activity> uniqueChildren = new();
        IEnumerable<FlowNode> childrenNodes = ValidateUnconnectedNodes ? Nodes.Distinct() : _reachableNodes;
        foreach (FlowNode node in childrenNodes)
        {
            if (ValidateUnconnectedNodes && !WasVisited(node))
            {
                node.CacheMetadata(this, metadata);
            }
            var nodeActivities = node.GetChildActivities();
            if (nodeActivities != null)
            {
                uniqueChildren.AddRange(nodeActivities);
            }
            node.EndCacheMetadata(metadata);
        }

        metadata.SetChildrenCollection(new Collection<Activity>(uniqueChildren.ToList()));

        bool WasVisited(FlowNode node)
        {
            if (node is null)
            {
                return true;
            }

            if (node.Flowchart != null)
            {
                if (node.Flowchart != this)
                {
                    metadata.AddValidationError(SR.FlowNodeCannotBeShared(node.Flowchart.DisplayName, DisplayName));
                }
                return true;
            }
            return false;
        }

        void GatherReachableNodes()
        {
            // Clear out our cached list of all nodes
            _reachableNodes.Clear();

            if (StartNode == null && Nodes.Count > 0)
            {
                metadata.AddValidationError(SR.FlowchartMissingStartNode(DisplayName));
            }
            else if (StartNode is not null)
            {
                DepthFirstVisitNodes();
            }
        }

        void DepthFirstVisitNodes()
        {
            Stack<FlowNode> toVisit = new();
            toVisit.Push(StartNode);
            while (toVisit.TryPop(out var current))
            {
                if (WasVisited(current))
                    continue;

                var successors = VisitNode(current, toVisit);
                foreach (var successor in successors)
                {
                    toVisit.Push(successor);
                    PropagateSplitsStack(current, successor);
                }
            }
        }

        IReadOnlyList<FlowNode> VisitNode(FlowNode node, Stack<FlowNode> toVisit)
        {
            Fx.Assert(node.Index == -1 && !_reachableNodes.Contains(node), "Corrupt Flowchart.reachableNodes.");
            node.Index = _reachableNodes.Count;
            _reachableNodes.Add(node);
            node.CacheMetadata(this, metadata);

            return node.GetSuccessors();
        }

        void PropagateSplitsStack(FlowNode predecessor, FlowNode successor)
        {
            if (predecessor is null || successor is null)
                return;
            var predecessorStack = GetStaticSplitsStack(predecessor);
            var successorStack = GetStaticSplitsStack(successor);
            switch (predecessor)
            {
                case FlowSplit split:
                    successorStack.Push(split, predecessorStack);
                    break;
                case FlowMerge _:
                    successorStack.AddPop(predecessorStack);
                    break;
                default:
                    successorStack.PropagateStack(predecessorStack);
                    break;
            }
        }
    }

    internal StaticNodeStackInfo GetStaticSplitsStack(FlowNode node)
    {
        if (_staticBranchesByNode.ContainsKey(node))
            return _staticBranchesByNode[node];
        else
            return _staticBranchesByNode[node] = new();
    }

    internal List<FlowMerge> GetMerges(FlowNode flowNode)
    {
        var staticBranches = GetStaticSplitsStack(flowNode);

        var merges = (
        from nodeInfo in _staticBranchesByNode
        where nodeInfo.Key is FlowMerge
        where nodeInfo.Value.IsOnBranch(staticBranches)
        select nodeInfo
        ).ToList();

        return merges.Select(ni => ni.Key as FlowMerge).ToList();
    }
}
