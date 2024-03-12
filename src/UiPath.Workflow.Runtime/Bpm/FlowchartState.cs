using System.Linq;
namespace System.Activities.Statements;

internal abstract class FlowchartState
{
    private const string FlowChartStateVariableName = "flowchartState";

    private readonly string _key;
    private Flowchart _owner;
    private readonly Func<Flowchart> _getowner;
    private readonly Func<object> _addValue;

    public FlowchartState(string key, Func<Flowchart> getOwner, Func<object> addValue)
    {
        _key = key;
        _getowner = getOwner;
        _addValue = addValue;
    }
    public static bool IsInstalled(Flowchart owner)
        => owner.ImplementationVariables?.Any(v => v.Name == FlowChartStateVariableName) is true;

    public static void Install(NativeActivityMetadata metadata, Flowchart owner)
    {
        if (IsInstalled(owner))
            return;

        metadata.AddImplementationVariable(new Variable<Dictionary<string, object>>(FlowChartStateVariableName, c => new()));
    }
    public object GetOrAdd()
    {
        _owner ??= _getowner();
        return GetOrAdd(_owner.Extension.ActivityContext);
    }

    private object GetOrAdd(ActivityContext context)
    {
        _owner ??= _getowner();
        var variable = _owner.ImplementationVariables.Single(v => v.Name == FlowChartStateVariableName);
        var flowChartState = (Dictionary<string, object>)variable.Get(context);
        if (!flowChartState.TryGetValue(_key, out var value))
        {
            value = _addValue();
            flowChartState[_key] = value;
        }
        return value;
    }

    public class Of<T> : FlowchartState
    {
        public Of(string key, Flowchart owner, Func<T> addValue) : base(key, () => owner, () => addValue())
        {
        }
        public Of(string key, FlowNode node, Func<T> addValue) : base(key, () => node.Owner, () => addValue())
        {
        }

        public new T GetOrAdd() => (T)base.GetOrAdd();
    }
}
