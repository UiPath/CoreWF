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
 
    internal void CacheMetadata(Flowchart flowchart, NativeActivityMetadata metadata)
    { 
        if (_owner != null && !ReferenceEquals(flowchart, _owner))
        {
            metadata.AddValidationError(SR.FlowNodeCannotBeShared(_owner.DisplayName, flowchart.DisplayName));
        }
        _owner = flowchart;
        Metadata = metadata;
        OnCacheMetadata();
    }
    protected virtual void OnCacheMetadata() { }

    internal virtual IEnumerable<Activity> GetChildActivities() => null;
    internal abstract IReadOnlyList<FlowNode> GetSuccessors();

    internal abstract Flowchart.NodeInstance CreateInstance();

    public override string ToString() => GetType().Name;

    public abstract class NodeInstance<TFlowNode, TCompletionResult> : NodeInstance<TFlowNode> where TFlowNode : FlowNode
    {
        protected override void OnCompletionCallback<T>(T result)
        {
            if (!typeof(T).Equals(typeof(TCompletionResult)))
            {
                throw new ArgumentException("Invalid argument type.");
            }

            OnCompletionCallback(result is TCompletionResult typedResult ? typedResult : default);
        }

        protected abstract void OnCompletionCallback(TCompletionResult result);
    }

    public abstract class NodeInstance<TFlowNode> : Flowchart.NodeInstance where TFlowNode : FlowNode
    {
        protected Flowchart Flowchart { get; private set; }
        protected TFlowNode Node { get; private set; }

        internal sealed override void Execute(Flowchart flowchart, FlowNode node)
        {
            Flowchart = flowchart;
            Node = node as TFlowNode;

            Execute();
        }
        internal sealed override void OnCompletionCallback<T>(Flowchart flowchart, FlowNode node, T result)
        {
            Flowchart = flowchart;
            Node = node as TFlowNode;

            OnCompletionCallback(result);
        }

        protected virtual void OnCompletionCallback<T>(T result)
        {
            OnCompletionCallback();
        }

        protected virtual void OnCompletionCallback() { }

        protected abstract void Execute();
    }

}
