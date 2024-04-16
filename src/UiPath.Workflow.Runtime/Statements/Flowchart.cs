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


            ValidateSplitsAndMerges(metadata, node);
        }

        metadata.SetChildrenCollection(new Collection<Activity>(uniqueChildren.ToList()));
        bool WasVisited(FlowNode node) => node is null || _staticBranchesByNode.ContainsKey(node);

        void GatherReachableNodes()
        {
            // Clear out our cached list of all nodes
            _reachableNodes.Clear();
            _staticBranchesByNode.Clear();

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
            GetStaticSplitsStack(StartNode).PropagateStack(StaticNodeStackInfo.EmptyStack);
            while (toVisit.TryPop(out var current))
            {
                VisitNode(current, toVisit);
            }
        }
        void VisitNode(FlowNode current, Stack<FlowNode> toVisit)
        {
            Fx.Assert(current.Index == -1 && !_reachableNodes.Contains(current), "Corrupt Flowchart.reachableNodes.");
            current.Index = _reachableNodes.Count;
            _reachableNodes.Add(current);
            current.CacheMetadata(this, metadata);
            var successors = current.GetSuccessors();

            foreach (var successor in successors)
            {
                if (!WasVisited(successor))
                    toVisit.Push(successor);
                PropagateSplitsStack(current, successor);
            }
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
                    successorStack.AddPush(split, predecessorStack);
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

    private void ValidateSplitsAndMerges(NativeActivityMetadata metadata, FlowNode node)
    {
        ValidateSingleSplitInAmonte(node);
        switch (node)
        {
            case FlowMerge merge: ValidateMerge(merge); break;
            case FlowSplit split: ValidateSplit(split); break;
        }

        void ValidateSingleSplitInAmonte(FlowNode node)
        {
            var splits = GetStaticSplitsStack(node).GetTop();
            if (splits.Count > 1)
                AddValidationError($"Node has multiple splits incoming branches. Please precede with a Merge node.", node, splits);
        }
        void ValidateSplit(FlowSplit split)
        {
            var splitMerges = GetMerges(split);
            if (splitMerges.Count == 0)
                AddValidationError("Split should end in one Merge node.", split);
            if (splitMerges.Count > 1)
                AddValidationError("All split branches should end in only one Merge node.", split, splitMerges);
        }
        void ValidateMerge(FlowMerge node)
        {
            var splits = GetStaticSplitsStack(node).GetTop();
            if (splits.Count == 1 && splits.First() is null)
                AddValidationError("A merge must be preceded by a Split node.", node);
        }
        void AddValidationError(string message, FlowNode node, IEnumerable<FlowNode> otherNodes = null)
        {
            metadata.AddValidationError(new ValidationError(message)
            {
                SourceDetail = new[] { node }.Concat(otherNodes ?? Array.Empty<FlowNode>()).ToArray()
            });
        }
        List<FlowMerge> GetMerges(FlowSplit flowSplit)
        {
            return (from nodeInfo in _staticBranchesByNode 
                    where nodeInfo.Key is FlowMerge 
                    where nodeInfo.Value.GetTop().Contains(flowSplit) 
                    select nodeInfo.Key as FlowMerge)
                    .ToList();
        }
    }

    private StaticNodeStackInfo GetStaticSplitsStack(FlowNode node)
    {
        if (_staticBranchesByNode.ContainsKey(node))
            return _staticBranchesByNode[node];
        else
            return _staticBranchesByNode[node] = new();
    }
}
