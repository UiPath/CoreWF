using Shouldly;
using System.Activities;
using System.Activities.Statements;
using Xunit;
namespace TestCases.Workflows
{
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
        public void CreateValueFromVariable()
        {
            var sequence = new Sequence { Variables = { new Variable<string>("str") } };
            WorkflowInspectionServices.CacheMetadata(sequence);
            var expression = "str";
            var value = (PowerFxValue<string>)PowerFxHelper.CreateValue(sequence, expression);
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void CreateValueFromExpression()
        {
            var sequence = new Sequence { Variables = { new Variable<string>("str"), new Variable<int>("int") } };
            WorkflowInspectionServices.CacheMetadata(sequence);
            var expression = "int+Len(str)";
            var value = (PowerFxValue<double>)PowerFxHelper.CreateValue(sequence, expression);
            value.Expression.ShouldBe(expression);
        }
        [Fact]
        public void EvaluateMembers() => new Sequence
        {
            Variables = { new Variable<Name>("assembly", _=>new Name("codeBase", "en-US")) },
            Activities = { new WriteLine { Text = new PowerFxValue<string>("Concatenate(assembly.CodeBase, assembly.CultureName)") } }
        }.InvokeWorkflow().ShouldBe("codeBaseen-US\r\n");
        public record Name(string CodeBase, string CultureName) { }
    }
}