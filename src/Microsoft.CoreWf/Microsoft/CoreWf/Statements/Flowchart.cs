// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.CoreWf.Statements
{
    //[ContentProperty("Nodes")]
    public sealed class Flowchart : NativeActivity
    {
        private Collection<Variable> _variables;
        private Collection<FlowNode> _nodes;
        private Collection<FlowNode> _reachableNodes;

        private CompletionCallback _onStepCompleted;
        private CompletionCallback<bool> _onDecisionCompleted;

        private Variable<int> _currentNode;
        public Flowchart()
        {
            _currentNode = new Variable<int>();
            _reachableNodes = new Collection<FlowNode>();
        }

        [DefaultValue(false)]
        public bool ValidateUnconnectedNodes
        {
            get;
            set;
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (_variables == null)
                {
                    _variables = new ValidatingCollection<Variable>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _variables;
            }
        }

        //[DependsOn("Variables")]
        public FlowNode StartNode
        {
            get;
            set;
        }

        //[DependsOn("StartNode")]
        public Collection<FlowNode> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new ValidatingCollection<FlowNode>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }

                return _nodes;
            }
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    Flowchart originalFlowchart = (Flowchart)originalActivity;
        //    Dictionary<Activity, int> originalActivities = new Dictionary<Activity, int>();
        //    foreach (FlowNode node in originalFlowchart.reachableNodes)
        //    {
        //        if (node.ChildActivity == null)
        //        {
        //            continue;
        //        }
        //        if (metadata.IsReferenceToImportedChild(node.ChildActivity))
        //        {
        //            // We can't save original values for referenced children. Also, we can't reliably combine
        //            // implementation changes with changes to referenced children. For now, we just disable 
        //            // this scenario altogether; if we want to support it, we'll need deeper runtime support.
        //            metadata.DisallowUpdateInsideThisActivity(SR.FlowchartContainsReferences);
        //            return;
        //        }
        //        if (originalActivities.ContainsKey(node.ChildActivity))
        //        {
        //            metadata.DisallowUpdateInsideThisActivity(SR.MultipleFlowNodesSharingSameChildBlockDU);
        //            return;
        //        }

        //        originalActivities[node.ChildActivity] = node.Index;
        //    }

        //    HashSet<Activity> updatedActivities = new HashSet<Activity>();
        //    foreach (FlowNode node in this.reachableNodes)
        //    {
        //        if (node.ChildActivity != null)
        //        {
        //            if (metadata.IsReferenceToImportedChild(node.ChildActivity))
        //            {
        //                metadata.DisallowUpdateInsideThisActivity(SR.FlowchartContainsReferences);
        //                return;
        //            }

        //            if (updatedActivities.Contains(node.ChildActivity))
        //            {
        //                metadata.DisallowUpdateInsideThisActivity(SR.MultipleFlowNodesSharingSameChildBlockDU);
        //                return;
        //            }
        //            else
        //            {
        //                updatedActivities.Add(node.ChildActivity);
        //            }

        //            Activity originalChild = metadata.GetMatch(node.ChildActivity);
        //            int originalIndex;
        //            if (originalChild != null && originalActivities.TryGetValue(originalChild, out originalIndex))
        //            {
        //                if (originalFlowchart.reachableNodes[originalIndex].GetType() != node.GetType())
        //                {
        //                    metadata.DisallowUpdateInsideThisActivity(SR.CannotMoveChildAcrossDifferentFlowNodeTypes);
        //                    return;
        //                }

        //                if (originalIndex != node.Index)
        //                {
        //                    metadata.SaveOriginalValue(node.ChildActivity, originalIndex);
        //                }
        //            }
        //        }
        //    }
        //}

        //protected override void UpdateInstance(NativeActivityUpdateContext updateContext)
        //{
        //    int oldNodeIndex = updateContext.GetValue(this.currentNode);

        //    foreach (FlowNode node in this.reachableNodes)
        //    {
        //        if (node.ChildActivity != null)
        //        {
        //            object originalValue = updateContext.GetSavedOriginalValue(node.ChildActivity);
        //            if (originalValue != null)
        //            {
        //                int originalIndex = (int)originalValue;
        //                if (originalIndex == oldNodeIndex)
        //                {
        //                    updateContext.SetValue(this.currentNode, node.Index);
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(_currentNode);

            this.GatherReachableNodes(metadata);
            if (this.ValidateUnconnectedNodes && (_reachableNodes.Count < this.Nodes.Count))
            {
                metadata.AddValidationError(SR.FlowchartContainsUnconnectedNodes(this.DisplayName));
            }
            HashSet<Activity> uniqueChildren = new HashSet<Activity>();
            IEnumerable<FlowNode> childrenNodes = this.ValidateUnconnectedNodes ? this.Nodes.Distinct() : _reachableNodes;
            foreach (FlowNode node in childrenNodes)
            {
                if (this.ValidateUnconnectedNodes)
                {
                    node.OnOpen(this, metadata);
                }
                node.GetChildActivities(uniqueChildren);
            }

            List<Activity> children = new List<Activity>(uniqueChildren.Count);
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

            if (this.StartNode == null && this.Nodes.Count > 0)
            {
                metadata.AddValidationError(SR.FlowchartMissingStartNode(this.DisplayName));
            }
            else
            {
                this.DepthFirstVisitNodes((n) => this.VisitNode(n, metadata), this.StartNode);
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

        private void DepthFirstVisitNodes(Func<FlowNode, bool> visitNodeCallback, FlowNode start)
        {
            Fx.Assert(visitNodeCallback != null, "This must be supplied since it stops us from infinitely looping.");

            List<FlowNode> connected = new List<FlowNode>();
            Stack<FlowNode> stack = new Stack<FlowNode>();
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
            if (this.StartNode != null)
            {
                if (TD.FlowchartStartIsEnabled())
                {
                    TD.FlowchartStart(this.DisplayName);
                }
                this.ExecuteNodeChain(context, this.StartNode, null);
            }
            else
            {
                if (TD.FlowchartEmptyIsEnabled())
                {
                    TD.FlowchartEmpty(this.DisplayName);
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
                FlowNode next;
                if (this.ExecuteSingleNode(context, current, out next))
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
            FlowStep step = node as FlowStep;
            if (step != null)
            {
                if (_onStepCompleted == null)
                {
                    _onStepCompleted = new CompletionCallback(this.OnStepCompleted);
                }

                return step.Execute(context, _onStepCompleted, out nextNode);
            }

            nextNode = null;
            FlowDecision decision = node as FlowDecision;
            if (decision != null)
            {
                if (_onDecisionCompleted == null)
                {
                    _onDecisionCompleted = new CompletionCallback<bool>(this.OnDecisionCompleted);
                }

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
            FlowStep step = this.GetCurrentNode(context) as FlowStep;
            Fx.Assert(step != null, "corrupt internal state");
            FlowNode next = step.Next;
            this.ExecuteNodeChain(context, next, completedInstance);
        }

        private void OnDecisionCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            FlowDecision decision = this.GetCurrentNode(context) as FlowDecision;
            Fx.Assert(decision != null, "corrupt internal state");
            FlowNode next = result ? decision.True : decision.False;
            this.ExecuteNodeChain(context, next, completedInstance);
        }

        internal void OnSwitchCompleted<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
        {
            IFlowSwitch switchNode = this.GetCurrentNode(context) as IFlowSwitch;
            Fx.Assert(switchNode != null, "corrupt internal state");
            FlowNode next = switchNode.GetNextNode(result);
            this.ExecuteNodeChain(context, next, completedInstance);
        }
    }
}
