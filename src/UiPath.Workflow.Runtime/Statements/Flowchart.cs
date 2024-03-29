// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

[ContentProperty("Nodes")]
public sealed class Flowchart : NativeActivity
{
    private Collection<Variable> _variables;
    private Collection<FlowNode> _nodes;
    private readonly Collection<FlowNode> _reachableNodes;
    private CompletionCallback _onStepCompleted;
    private CompletionCallback<bool> _onDecisionCompleted;
    private readonly Variable<int> _currentNode;

    public Flowchart()
    {
        _currentNode = new Variable<int>();
        _reachableNodes = new Collection<FlowNode>();
    }

    [DefaultValue(false)]
    public bool ValidateUnconnectedNodes { get; set; }

    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }

    [DependsOn("Variables")]
    public FlowNode StartNode { get; set; }

    [DependsOn("StartNode")]
    public Collection<FlowNode> Nodes
    {
        get
        {
            _nodes ??= new ValidatingCollection<FlowNode>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _nodes;
        }
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        Flowchart originalFlowchart = (Flowchart)originalActivity;
        Dictionary<Activity, int> originalActivities = new Dictionary<Activity, int>();
        foreach (FlowNode node in originalFlowchart.reachableNodes)
        {
            if (node.ChildActivity == null)
            {
                continue;
            }
            if (metadata.IsReferenceToImportedChild(node.ChildActivity))
            {
                // We can't save original values for referenced children. Also, we can't reliably combine
                // implementation changes with changes to referenced children. For now, we just disable 
                // this scenario altogether; if we want to support it, we'll need deeper runtime support.
                metadata.DisallowUpdateInsideThisActivity(SR.FlowchartContainsReferences);
                return;
            }
            if (originalActivities.ContainsKey(node.ChildActivity))
            {
                metadata.DisallowUpdateInsideThisActivity(SR.MultipleFlowNodesSharingSameChildBlockDU);
                return;
            }

            originalActivities[node.ChildActivity] = node.Index;
        }

        HashSet<Activity> updatedActivities = new HashSet<Activity>();
        foreach (FlowNode node in this.reachableNodes)
        {
            if (node.ChildActivity != null)
            {
                if (metadata.IsReferenceToImportedChild(node.ChildActivity))
                {
                    metadata.DisallowUpdateInsideThisActivity(SR.FlowchartContainsReferences);
                    return;
                }

                if (updatedActivities.Contains(node.ChildActivity))
                {
                    metadata.DisallowUpdateInsideThisActivity(SR.MultipleFlowNodesSharingSameChildBlockDU);
                    return;
                }
                else
                {
                    updatedActivities.Add(node.ChildActivity);
                }

                Activity originalChild = metadata.GetMatch(node.ChildActivity);
                int originalIndex;
                if (originalChild != null && originalActivities.TryGetValue(originalChild, out originalIndex))
                {
                    if (originalFlowchart.reachableNodes[originalIndex].GetType() != node.GetType())
                    {
                        metadata.DisallowUpdateInsideThisActivity(SR.CannotMoveChildAcrossDifferentFlowNodeTypes);
                        return;
                    }

                    if (originalIndex != node.Index)
                    {
                        metadata.SaveOriginalValue(node.ChildActivity, originalIndex);
                    }
                }
            }
        }
    }

    protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
    {
        int oldNodeIndex = updateContext.GetValue(this.currentNode);

        foreach (FlowNode node in this.reachableNodes)
        {
            if (node.ChildActivity != null)
            {
                object originalValue = updateContext.GetSavedOriginalValue(node.ChildActivity);
                if (originalValue != null)
                {
                    int originalIndex = (int)originalValue;
                    if (originalIndex == oldNodeIndex)
                    {
                        updateContext.SetValue(this.currentNode, node.Index);
                        break;
                    }
                }
            }
        }
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetVariablesCollection(Variables);
        metadata.AddImplementationVariable(_currentNode);

        GatherReachableNodes(metadata);
        if (ValidateUnconnectedNodes && (_reachableNodes.Count < Nodes.Count))
        {
            metadata.AddValidationError(SR.FlowchartContainsUnconnectedNodes(DisplayName));
        }
        HashSet<Activity> uniqueChildren = new();
        IEnumerable<FlowNode> childrenNodes = ValidateUnconnectedNodes ? Nodes.Distinct() : _reachableNodes;
        foreach (FlowNode node in childrenNodes)
        {
            if (ValidateUnconnectedNodes)
            {
                node.OnOpen(this, metadata);
            }
            node.GetChildActivities(uniqueChildren);
        }

        List<Activity> children = new(uniqueChildren.Count);
        foreach (Activity child in uniqueChildren)
        {
            children.Add(child);
        }

        metadata.SetChildrenCollection(new Collection<Activity>(children));
    }

    private void GatherReachableNodes(NativeActivityMetadata metadata)
    {
        // Clear out our cached list of all nodes
        _reachableNodes.Clear();

        if (StartNode == null && Nodes.Count > 0)
        {
            metadata.AddValidationError(SR.FlowchartMissingStartNode(DisplayName));
        }
        else
        {
            DepthFirstVisitNodes((n) => VisitNode(n, metadata), StartNode);
        }
    }

    // Returns true if we should visit connected nodes
    private bool VisitNode(FlowNode node, NativeActivityMetadata metadata)
    {
        if (node.Open(this, metadata))
        {
            Fx.Assert(node.Index == -1 && !_reachableNodes.Contains(node), "Corrupt Flowchart.reachableNodes.");

            node.Index = _reachableNodes.Count;
            _reachableNodes.Add(node);

            return true;
        }

        return false;
    }

    private static void DepthFirstVisitNodes(Func<FlowNode, bool> visitNodeCallback, FlowNode start)
    {
        Fx.Assert(visitNodeCallback != null, "This must be supplied since it stops us from infinitely looping.");

        List<FlowNode> connected = new();
        Stack<FlowNode> stack = new();
        if (start == null)
        {
            return;
        }
        stack.Push(start);
        while (stack.Count > 0)
        {
            FlowNode current = stack.Pop();

            if (current == null)
            {
                continue;
            }

            if (visitNodeCallback(current))
            {
                connected.Clear();
                current.GetConnectedNodes(connected);

                for (int i = 0; i < connected.Count; i++)
                {
                    stack.Push(connected[i]);
                }
            }
        }
    }


    protected override void Execute(NativeActivityContext context)
    {
        if (StartNode != null)
        {
            if (TD.FlowchartStartIsEnabled())
            {
                TD.FlowchartStart(DisplayName);
            }
            ExecuteNodeChain(context, StartNode, null);
        }
        else
        {
            if (TD.FlowchartEmptyIsEnabled())
            {
                TD.FlowchartEmpty(DisplayName);
            }
        }
    }

    private void ExecuteNodeChain(NativeActivityContext context, FlowNode node, ActivityInstance completedInstance)
    {
        if (node == null)
        {
            if (context.IsCancellationRequested)
            {
                Fx.Assert(completedInstance != null, "cannot request cancel if we never scheduled any children");
                // we are done but the last child didn't complete successfully
                if (completedInstance.State != ActivityInstanceState.Closed)
                {
                    context.MarkCanceled();
                }
            }

            return;
        }

        if (context.IsCancellationRequested)
        {
            // we're not done and cancel has been requested
            context.MarkCanceled();
            return;
        }


        Fx.Assert(node != null, "caller should validate");
        FlowNode current = node;
        do
        {
            if (ExecuteSingleNode(context, current, out FlowNode next))
            {
                current = next;
            }
            else
            {
                _currentNode.Set(context, current.Index);
                current = null;
            }
        }
        while (current != null);
    }

    private bool ExecuteSingleNode(NativeActivityContext context, FlowNode node, out FlowNode nextNode)
    {
        Fx.Assert(node != null, "caller should validate");
        if (node is FlowStep step)
        {
            _onStepCompleted ??= new CompletionCallback(OnStepCompleted);
            return step.Execute(context, _onStepCompleted, out nextNode);
        }

        nextNode = null;
        if (node is FlowDecision decision)
        {
            _onDecisionCompleted ??= new CompletionCallback<bool>(OnDecisionCompleted);
            return decision.Execute(context, _onDecisionCompleted);
        }

        IFlowSwitch switchNode = node as IFlowSwitch;
        Fx.Assert(switchNode != null, "unrecognized FlowNode");

        return switchNode.Execute(context, this);
    }

    private FlowNode GetCurrentNode(NativeActivityContext context)
    {
        int index = _currentNode.Get(context);
        FlowNode result = _reachableNodes[index];
        Fx.Assert(result != null, "corrupt internal state");
        return result;
    }

    private void OnStepCompleted(NativeActivityContext context, ActivityInstance completedInstance)
    {
        FlowStep step = GetCurrentNode(context) as FlowStep;
        Fx.Assert(step != null, "corrupt internal state");
        FlowNode next = step.Next;
        ExecuteNodeChain(context, next, completedInstance);
    }

    private void OnDecisionCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        FlowDecision decision = GetCurrentNode(context) as FlowDecision;
        Fx.Assert(decision != null, "corrupt internal state");
        FlowNode next = result ? decision.True : decision.False;
        ExecuteNodeChain(context, next, completedInstance);
    }

    internal void OnSwitchCompleted<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        IFlowSwitch switchNode = GetCurrentNode(context) as IFlowSwitch;
        Fx.Assert(switchNode != null, "corrupt internal state");
        FlowNode next = switchNode.GetNextNode(result);
        ExecuteNodeChain(context, next, completedInstance);
    }
}
