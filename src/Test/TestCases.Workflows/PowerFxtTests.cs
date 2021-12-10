using Shouldly;
using System.Activities;
using System.Activities.Statements;
using Xunit;

namespace TestCases.Workflows;

public class PowerFxTests
{
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