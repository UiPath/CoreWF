using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Activities
{
    public class PowerFxValue<T> : CodeActivity<T>
    {
        public PowerFxValue(string expression) => Expression = expression;
        public string Expression { get; }
        protected override T Execute(CodeActivityContext context)
        {
            var locals = Parent.GetLocals(local => context.UnsafeGetValue(local));
            var result = PowerFxHelper.Engine.Eval(Expression, locals).ToObject();
            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
    public static class PowerFxHelper
    {
        internal static readonly RecalcEngine Engine = new();
        public static ActivityWithResult CreateValue(Activity parent, string expressionText, Type targetType = null)
        {
            var locals = parent.GetLocals(GetDefault);
            var checkResult = Engine.Check(expressionText, locals.Type);
            checkResult.ThrowOnErrors();
            var expressionType = targetType ?? checkResult.ReturnType.DotnetType();
            var activityType = typeof(PowerFxValue<>).MakeGenericType(expressionType);
            return (ActivityWithResult)Activator.CreateInstance(activityType, expressionText);
            static object GetDefault(LocationReference local)
            {
                var type = local.Type;
                if (type == typeof(string))
                {
                    return string.Empty;
                }
                if (type.IsAbstract || type.IsArray)
                {
                    return null;
                }
                return RuntimeHelpers.GetUninitializedObject(type);
            }
        }
        internal static RecordValue GetLocals(this Activity parent, Func<LocationReference, object> getValue)
        {
            var localsValues = new Dictionary<string, FormulaValue>();
            foreach (var local in parent.GetLocals())
            {
                localsValues.TryAdd(local.Name, FormulaValue.New(getValue(local), local.Type));
            }
            return FormulaValue.RecordFromFields(localsValues.Select(l => new NamedValue(l)));
        }
        internal static Type DotnetType(this FormulaType formulaType)
        {
            if (formulaType == FormulaType.Number)
            {
                return typeof(double);
            }
            if (formulaType == FormulaType.String || formulaType == FormulaType.OptionSetValue)
            {
                return typeof(string);
            }
            if (formulaType == FormulaType.Time || formulaType == FormulaType.Date || formulaType == FormulaType.DateTime || formulaType == FormulaType.DateTimeNoTimeZone)
            {
                return typeof(DateTime);
            }
            if (formulaType == FormulaType.Boolean)
            {
                return typeof(bool);
            }
            return typeof(object);
        }
    }
}