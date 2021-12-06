using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestCases.Workflows
{
    public static class PowerFxHelper
    {
        internal static readonly RecalcEngine Engine = new();
        public static ActivityWithResult CreateValue(Activity parent, string expressionText, Type targetType = null)
        {
            var locals = parent.GetLocals(local => local.Type.IsValueType ? Activator.CreateInstance(local.Type) : null);
            var checkResult = Engine.Check(expressionText, locals.Type);
            checkResult.ThrowOnErrors();
            var expressionType = targetType ?? checkResult.ReturnType.DotnetType();
            var activityType = typeof(PowerFxValue<>).MakeGenericType(expressionType);
            return (ActivityWithResult)Activator.CreateInstance(activityType, expressionText);
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
    public class PowerFxValue<T> : CodeActivity<T>
    {
        public PowerFxValue(string expression) => Expression = expression;
        public string Expression { get; }
        protected override T Execute(CodeActivityContext context)
        {
            var locals = this.GetLocals(local => context.UnsafeGetValue(local));
            var result = PowerFxHelper.Engine.Eval(Expression, locals).ToObject();
            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
    public class PowerFxTests
    {
        [Fact]
        public void CreateValueNoType()
        {
            var expression = "1+2/3*6";
            var value = (PowerFxValue<double>)PowerFxHelper.CreateValue(new Sequence(), expression);
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void CreateValueNoTypeString()
        {
            var expression = "\"d\"";
            var value = (PowerFxValue<string>)PowerFxHelper.CreateValue(new Sequence(), expression);
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void CreateValueNoTypeLenString()
        {
            var expression = "Len(\"d\")";
            var value = (PowerFxValue<double>)PowerFxHelper.CreateValue(new Sequence(), expression);
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void CreateValueWithType()
        {
            var expression = "1+2";
            var value = (PowerFxValue<int>)PowerFxHelper.CreateValue(new Sequence(), expression, typeof(int));
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void EvaluateExpression() => new WriteLine { Text = new PowerFxValue<string>("1+2*3") }.InvokeWorkflow().ShouldBe("7\r\n");
        [Fact]
        public void EvaluateVariables() => new Sequence
        {
            Variables = { new Variable<int>("one", 1) },
            Activities = { new Sequence { 
                Variables = { new Variable<float>("two", 2)  },
                Activities = { new Sequence
                {
                    Variables = { new Variable<double>("three", 3) },
                    Activities = { new WriteLine { Text = new PowerFxValue<string>("Len(20*(one+two*three))") } }
                }}}}
        }.InvokeWorkflow().ShouldBe("3\r\n");
        [Fact]
        public void EvaluateMembers() => new Sequence
        {
            Variables = { new Variable<Name>("assembly", _=>new Name("codeBase", "en-US")) },
            Activities = { new WriteLine { Text = new PowerFxValue<string>("Concatenate(assembly.CodeBase, assembly.CultureName)") } }
        }.InvokeWorkflow().ShouldBe("codeBaseen-US\r\n");
        public record Name(string CodeBase, string CultureName) { }
    }
}