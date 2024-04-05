// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.Statements.Interfaces;
using System.Linq;
using System.Windows.Markup;

namespace System.Activities.Statements;
using IFlowSwitchInternal = IFlowSwitch;

[ContentProperty("Cases")]
public sealed class FlowSwitch<T> : FlowNode, IFlowSwitchInternal, IFlowSwitch<T>
{
    private const string DefaultDisplayName = "Switch";
    internal IDictionary<T, FlowNode> _cases;
    private CompletionCallback<T> _onSwitchCompleted;

    public FlowSwitch()
    {
        _cases = new NullableKeyDictionary<T, FlowNode>();
        DisplayName = DefaultDisplayName;
    }

    [DefaultValue(null)]
    public Activity<T> Expression { get; set; }

    Activity IHasExpressionNonGeneric.ExpressionNonGeneric
    {
        get => Expression;
    }

    [DefaultValue(null)]
    public FlowNode Default { get; set; }

    IFlowNode Interfaces.IFlowSwitch.Default { get => Default; set => Default = value as FlowNode; }

    [Fx.Tag.KnownXamlExternal]
    public IDictionary<T, FlowNode> Cases => _cases;

    IReadOnlyDictionary<T, IFlowNode> IFlowSwitch<T>.Cases => _cases?.ToDictionary(p => p.Key, p => p.Value as IFlowNode);

    IEnumerable<IFlowNode> Interfaces.IFlowSwitch.CaseNodes => _cases?.Values ?? Enumerable.Empty<IFlowNode>();

    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; }

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        if (Expression == null)
        {
            metadata.AddValidationError(SR.FlowSwitchRequiresExpression(owner.DisplayName));
        }
    }

    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        foreach (KeyValuePair<T, FlowNode> item in Cases)
        {
            connections.Add(item.Value);
        }
        if (Default != null)
        {
            connections.Add(Default);
        }
    }

    internal override Activity ChildActivity => Expression;

    bool IFlowSwitchInternal.Execute(NativeActivityContext context, Flowchart parent)
    {
        context.ScheduleActivity(Expression, GetSwitchCompletedCallback(parent));
        return false;
    }

    FlowNode IFlowSwitchInternal.GetNextNode(object value)
    {
        T newValue = (T)value;
        if (Cases.TryGetValue(newValue, out FlowNode result))
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

    private CompletionCallback<T> GetSwitchCompletedCallback(Flowchart parent)
    {
        _onSwitchCompleted ??= new CompletionCallback<T>(parent.OnSwitchCompleted);
        return _onSwitchCompleted;
    }
}
