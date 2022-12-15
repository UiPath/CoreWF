using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Windows.Markup;
namespace System.Activities.Statements;
internal interface IBpmSwitch
{
    bool Execute(NativeActivityContext context, BpmFlowchart parent);
    BpmNode GetNextNode(object value);
}
[ContentProperty("Cases")]
public sealed class BpmSwitch<T> : BpmNode, IBpmSwitch
{
    private const string DefaultDisplayName = "Switch";
    internal IDictionary<T, BpmNode> _cases;
    private CompletionCallback<T> _onSwitchCompleted;

    public BpmSwitch()
    {
        _cases = new NullableKeyDictionary<T, BpmNode>();
        DisplayName = DefaultDisplayName;
    }

    [DefaultValue(null)]
    public Activity<T> Expression { get; set; }

    [DefaultValue(null)]
    public BpmNode Default { get; set; }

    [Fx.Tag.KnownXamlExternal]
    public IDictionary<T, BpmNode> Cases => _cases;

    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; }

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

    bool IBpmSwitch.Execute(NativeActivityContext context, BpmFlowchart parent)
    {
        context.ScheduleActivity(Expression, GetSwitchCompletedCallback(parent));
        return false;
    }

    BpmNode IBpmSwitch.GetNextNode(object value)
    {
        T newValue = (T)value;
        if (Cases.TryGetValue(newValue, out BpmNode result))
        {
            if (TD.FlowchartSwitchCaseIsEnabled())
            {
                TD.FlowchartSwitchCase(Owner.DisplayName, newValue?.ToString());
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

    private CompletionCallback<T> GetSwitchCompletedCallback(BpmFlowchart parent)
    {
        _onSwitchCompleted ??= new CompletionCallback<T>(parent.OnSwitchCompleted);
        return _onSwitchCompleted;
    }
}
