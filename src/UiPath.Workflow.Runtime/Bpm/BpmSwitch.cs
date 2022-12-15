using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Windows.Markup;
namespace System.Activities.Statements;
[ContentProperty("Cases")]
public sealed class BpmSwitch<T> : BpmNode
{
    private const string DefaultDisplayName = "Switch";
    internal IDictionary<T, BpmNode> _cases  = new NullableKeyDictionary<T, BpmNode>();
    private CompletionCallback<T> _onCompleted;
    [DefaultValue(null)]
    public Activity<T> Expression { get; set; }
    [DefaultValue(null)]
    public BpmNode Default { get; set; }
    [Fx.Tag.KnownXamlExternal]
    public IDictionary<T, BpmNode> Cases => _cases;
    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; } = DefaultDisplayName;
    internal override void OnOpen(BpmFlowchart owner, NativeActivityMetadata metadata)
    {
        if (Expression == null)
        {
            metadata.AddValidationError(SR.FlowSwitchRequiresExpression(owner.DisplayName));
        }
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        foreach (KeyValuePair<T, BpmNode> item in Cases)
        {
            connections.Add(item.Value);
        }
        if (Default != null)
        {
            connections.Add(Default);
        }
    }
    internal override Activity ChildActivity => Expression;
    internal override void Execute(NativeActivityContext context, BpmNode completed)
    {
        _onCompleted ??= new(OnCompleted);
        context.ScheduleActivity(Expression, _onCompleted);
    }
    BpmNode GetNextNode(T value)
    {
        if (Cases.TryGetValue(value, out BpmNode result))
        {
            if (TD.FlowchartSwitchCaseIsEnabled())
            {
                TD.FlowchartSwitchCase(Owner.DisplayName, value?.ToString());
            }
            return result;
        }
        else
        {
            if (Default != null)
            {
                if (TD.FlowchartSwitchDefaultIsEnabled())
                {
                    TD.FlowchartSwitchDefault(Owner.DisplayName);
                }
            }
            else
            {
                if (TD.FlowchartSwitchCaseNotFoundIsEnabled())
                {
                    TD.FlowchartSwitchCaseNotFound(Owner.DisplayName);
                }
            }
            return Default;
        }
    }
    internal void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        var next = GetNextNode(result);
        next.TryExecute(context, this, completedInstance);
    }
}