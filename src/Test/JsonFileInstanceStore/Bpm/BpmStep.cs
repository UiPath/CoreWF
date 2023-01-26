using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
namespace UiPath.Bpm.Activities;
[ContentProperty("Action")]
public sealed class BpmStep : BpmNode
{
    private CompletionCallback _onCompleted;
    [DefaultValue(null)]
    public Activity Action { get; set; }
    [DefaultValue(null)]
    [DependsOn("Action")]
    public BpmNode Next { get; set; }
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.AddChild(Action);
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }
    protected override void Execute(NativeActivityContext context)
    {
        if (Action == null)
        {
            OnCompleted(context, null);
        }
        else
        {
            _onCompleted ??= new(OnCompleted);
            context.ScheduleActivity(Action, _onCompleted);
        }
    }
    private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance) => TryExecute(Next, context, completedInstance);
    public static BpmStep New(Activity activity, BpmNode next = null ) => new() { Action = activity, Next = next };
}