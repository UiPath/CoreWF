using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using Xunit;

namespace TestCases.Workflows;

public class ExpressionTests
{
    private ValidationSettings _skipCompilation = new ValidationSettings() { SkipExpressionCompilation = true };

    [Fact]
    public void ValidExpression_Basic()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void InvalidExpression_Basic()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public void InvalidExpression_SkipCompilation()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _skipCompilation);
        validationResults.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void ValidExpression_Variable()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", v, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("v", "I'm a variable"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(0);
    }
}
