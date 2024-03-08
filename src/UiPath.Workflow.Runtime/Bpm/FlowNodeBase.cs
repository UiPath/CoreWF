namespace System.Activities.Statements;

public abstract partial class FlowNodeBase : FlowNode
{
    protected new Flowchart Owner { get; private set; }
    protected NativeActivityMetadata Metadata { get; private set; }

    internal FlowchartExtension Extension => Owner.Extension;

    internal abstract void Execute(FlowNode predecessorNode);

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        Owner = owner;
        Metadata = metadata;
    }

    internal virtual void EndCacheMetadata()
    {

    }

    protected virtual void OnCompletionCallback()
    {
    }

    internal virtual void OnCompletionCallback<T>(T result)
    {
        switch (result)
        {
            case null:
                OnCompletionCallback();
                break;
            case bool b:
                OnCompletionCallback(b);
                break;
        }
    }

    protected virtual void OnCompletionCallback(bool result)
    {
    }
}