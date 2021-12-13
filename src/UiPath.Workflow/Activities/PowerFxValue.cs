using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;
using System.Collections.Generic;
using System.Linq;

namespace System.Activities;

public class PowerFxValue<T> : CodeActivity<T>
{
    static readonly RecalcEngine Engine = new();
    public PowerFxValue(string expression) => Expression = expression;
    public string Expression { get; }
    protected override T Execute(CodeActivityContext context)
    {
        RecordValue locals;
        using (context.InheritVariables())
        {
            locals = PowerFxHelper.GetLocals(Parent, local => local.GetLocation(context).Value);
        }
        var result = Engine.Eval(Expression, locals).ToObject();
        return (T)Convert.ChangeType(result, typeof(T));
    }
}
public static class PowerFxHelper
{
    public static RecordValue GetLocals(Activity parent, Func<LocationReference, object> getValue)
    {
        var localsValues = new Dictionary<string, FormulaValue>();
        foreach (var local in parent.GetLocals())
        {
            localsValues.TryAdd(local.Name, FormulaValue.New(getValue(local), local.Type));
        }
        return FormulaValue.RecordFromFields(localsValues.Select(l => new NamedValue(l)));
    }
}
