using System.Linq;
namespace System.Activities.Statements;

public abstract partial class FlowNodeBase : FlowNode
{
    protected new Flowchart Owner { get; private set; }
    protected NativeActivityMetadata Metadata { get; private set; }

    internal FlowchartExtension Extension => Owner.Extension;

    internal abstract void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode);

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        Owner = owner;
        Metadata = metadata;
    }

    internal virtual void EndCacheMetadata()
    {

    }

    internal virtual void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        if (result is bool b)
            OnCompletionCallback(context, completedInstance, b);
    }

    protected virtual void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
    }

    protected void ScheduleWithCallback<T>(NativeActivityContext context, Activity<T> activity)
    {
        context.ScheduleActivity(activity, new CompletionCallback<T>(Owner.OnCompletionCallback));
    }
}