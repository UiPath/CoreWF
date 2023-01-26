using System.Activities.Expressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Markup;
namespace UiPath.Bpm.Activities;
public sealed class BpmDecision : BpmNode
{
    private CompletionCallback<bool> _onCompleted;
    public BpmDecision() { }
    public BpmDecision(Expression<Func<ActivityContext, bool>> condition) => Condition = new LambdaValue<bool>(condition ?? throw new ArgumentNullException(nameof(condition)));
    public BpmDecision(Activity<bool> condition) => Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    [DefaultValue(null)]
    public Activity<bool> Condition { get; set; }
    [DefaultValue(null)]
    [DependsOn("Condition")]
    public BpmNode True { get; set; }
    [DefaultValue(null)]
    [DependsOn("True")]
    public BpmNode False { get; set; }
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        if (Condition == null)
        {
            metadata.AddValidationError(SR.FlowDecisionRequiresCondition(Owner.DisplayName));
        }
        else
        {
            metadata.AddChild(Condition);
        }
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (True != null)
        {
            connections.Add(True);
        }
        if (False != null)
        {
            connections.Add(False);
        }
    }
    protected override void Execute(NativeActivityContext context)
    {
        _onCompleted ??= new(OnCompleted);
        context.ScheduleActivity(Condition, _onCompleted);
    }
    void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result) => TryExecute(result ? True : False, context, completedInstance);
}