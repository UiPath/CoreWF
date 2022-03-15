using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestCases.Workflows;

public class ExpressionTests
{
    private readonly ValidationSettings _skipCompilation = new() { SkipExpressionCompilation = true };
    private readonly ValidationSettings _forceCache = new() { ForceExpressionCache = true };
    private readonly ValidationSettings _useValidator = new() { ForceExpressionCache = false };

    public static IEnumerable<object[]> ValidVbExpressions
    {
        get
        {
            yield return new object[] { @$"String.Concat(""alpha "", ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", v, ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", l(0), ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", d(""gamma""), ""beta "", 1)" };
            yield return new object[] { @"[Enum].Parse(GetType(ArgumentDirection), ""In"").ToString()" };
            yield return new object[] { "GetType(Activities.VisualBasicSettings).Name" };
        }
    }

    public static IEnumerable<object[]> ValidCsExpressions
    {
        get
        {
            yield return new object[] { $@"string.Join(',', ""alpha"", 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", v, 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", l[0], 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", d[""gamma""], 1, ""beta"")" };
        }
    }

    [Theory]
    [MemberData(nameof(ValidVbExpressions))]
    public void Vb_Valid(string expr)
    {
        VisualBasicValue<string> vbv = new(expr);
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("v", "I'm a variable"));
        workflow.Variables.Add(new Variable<List<string>>("l"));
        workflow.Variables.Add(new Variable<Dictionary<string, List<string>>>("d"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Theory]
    [MemberData(nameof(ValidCsExpressions))]
    public void Cs_Valid(string expr)
    {
        CSharpValue<string> csv = new(expr);
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("v", "I'm a variable"));
        workflow.Variables.Add(new Variable<List<string>>("l"));
        workflow.Variables.Add(new Variable<Dictionary<string, List<string>>>("d"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_InvalidExpression_Basic()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Cs_InvalidExpression_Basic()
    {
        CSharpValue<string> csv = new(@$"string.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("The name 'b' does not exist in the current context");
    }

    [Fact]
    public void Vb_InvalidExpression_SkipCompilation()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _skipCompilation);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Cs_InvalidExpression_SkipCompilation()
    {
        CSharpValue<string> csv = new(@$"string.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _skipCompilation);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_CompareLambdas()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b?.Substring(0, 10), ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("b", "I'm a variable"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _forceCache);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("A null propagating operator cannot be converted into an expression tree.");

        validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("A null propagating operator cannot be converted into an expression tree.");
    }

    [Fact]
    public void Vb_LambdaExtension()
    {
        VisualBasicValue<string> vbv = new("list.First()");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<List<string>>("list"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_Dictionary()
    {
        VisualBasicValue<string> vbv = new("something.EquivalentGroupsDictionary(\"key\").ToString()");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<ValidationHelper.OverloadGroupEquivalenceInfo>("something"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_IntOverflow()
    {
        VisualBasicValue<int> vbv = new("2147483648");
        Sequence workflow = new();
        workflow.Variables.Add(new Variable<int>("someint"));
        Assign assign = new() { To = new OutArgument<int>(workflow.Variables[0]), Value = new InArgument<int>(vbv) };
        workflow.Activities.Add(assign);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("error BC30439: Constant expression not representable in type 'Integer'");
    }
}
