// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Cases")]
public sealed class FlowSwitch<T> : FlowNode
{
    private const string DefaultDisplayName = "Switch";
    internal IDictionary<T, FlowNode> _cases;

    public FlowSwitch()
    {
        _cases = new NullableKeyDictionary<T, FlowNode>();
        DisplayName = DefaultDisplayName;
    }

    [DefaultValue(null)]
    public Activity<T> Expression { get; set; }

    [DefaultValue(null)]
    public FlowNode Default { get; set; }

    [Fx.Tag.KnownXamlExternal]
    public IDictionary<T, FlowNode> Cases => _cases;

    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; }

    internal override IReadOnlyList<FlowNode> GetSuccessors()
    {
        var connections = new List<FlowNode>(Cases.Count + 1);
        foreach (KeyValuePair<T, FlowNode> item in Cases)
        {
            connections.Add(item.Value);
        }
        if (Default != null)
        {
            connections.Add(Default);
        }
        return connections;
    }

    protected override void OnEndCacheMetadata()
    {
        if (Expression == null)
        {
            Metadata.AddValidationError(SR.FlowSwitchRequiresExpression(Flowchart.DisplayName));
        }
    }

    internal override IEnumerable<Activity> GetChildActivities()
        => new[] { Expression };

    internal override void Execute()
    {
        Flowchart.ScheduleWithCallback(Expression);
    }

    internal override void OnCompletionCallback<TResult>(TResult value)
    {
        T newValue;

        switch (value)
        {
            case T castedValue:
                newValue = castedValue;
                break;
            case null:
                newValue = default;
                break;
            default:
                throw new InvalidCastException();
        }

        if (Cases.TryGetValue(newValue, out FlowNode result))
        {
            if (TD.FlowchartSwitchCaseIsEnabled())
            {
                TD.FlowchartSwitchCase(Flowchart.DisplayName, newValue?.ToString());
            }
            Flowchart.EnqueueNodeExecution(result);
        }
        else
        {
            if (Default != null)
            {
                if (TD.FlowchartSwitchDefaultIsEnabled())
                {
                    TD.FlowchartSwitchDefault(Flowchart.DisplayName);
                }
            }
            else
            {
                if (TD.FlowchartSwitchCaseNotFoundIsEnabled())
                {
                    TD.FlowchartSwitchCaseNotFound(Flowchart.DisplayName);
                }
            }
            Flowchart.EnqueueNodeExecution(Default);
        }
    }
}
