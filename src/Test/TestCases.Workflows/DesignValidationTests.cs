using CustomTestObjects;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TestCases.Workflows;

public class DesignValidationTests
{
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

    static DesignValidationTests()
    {
        // There's no programmatic way (that I know of) to add assembly references when creating workflows like in these tests.
        // Adding the custom assembly directly to the expression validator to simulate XAML reference.
        // The null is for testing purposes.
        DesignVBExpressionValidator.Instance = new DesignVBExpressionValidator(new() { typeof(ClassWithCollectionProperties).Assembly, null });
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

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
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

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
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

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
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

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("The name 'b' does not exist in the current context");
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

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_Dictionary()
    {
        VisualBasicValue<string> vbv = new("something.FooDictionary(\"key\").ToString()");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<ClassWithCollectionProperties>("something"));

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }
    #region Check locations are not readonly
    [Fact]
    public void VB_Readonly_ThrowsError()
    {
        var activity = new Assign();
        activity.To = new OutArgument<bool>(new VisualBasicReference<bool>("Environment.HasShutdownStarted"));
        activity.Value = new InArgument<bool>(new Literal<bool>(true));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("BC30526: Property 'HasShutdownStarted' is 'ReadOnly'.");
    }

    [Fact]
    public void VB_NonReadonly_Works()
    {
        var activity = new Assign();
        activity.To = new OutArgument<string>(new VisualBasicReference<string>("Environment.CurrentDirectory"));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void VB_Variable_Works()
    {
        var seq = new Sequence();
        seq.Variables.Add(new Variable<string>("v1"));
        var activity = new Assign();
        activity.To = new OutArgument<string>(new VisualBasicReference<string>("v1"));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        seq.Activities.Add(activity);
        var result = DesignValidationServices.Validate(seq);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void VB_AssignmentInLocation_DoesNotWork()
    {
        var activity = new Assign();
        activity.To = new OutArgument<string>(new VisualBasicReference<string>("Environment.CurrentDirectory = \"abc\""));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'String'.");
    }

    [Fact]
    public void CS_Readonly_ThrowsError()
    {
        var activity = new Assign();
        activity.To = new OutArgument<bool>(new CSharpReference<bool>("Environment.HasShutdownStarted"));
        activity.Value = new InArgument<bool>(new Literal<bool>(true));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("CS0200: Property or indexer 'Environment.HasShutdownStarted' cannot be assigned to -- it is read only");
    }

    [Fact]
    public void CS_NonReadonly_Works()
    {
        var activity = new Assign();
        activity.To = new OutArgument<string>(new CSharpReference<string>("Environment.CurrentDirectory"));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CS_Variable_Works()
    {
        var seq = new Sequence();
        seq.Variables.Add(new Variable<string>("v1"));
        var activity = new Assign();
        activity.To = new OutArgument<string>(new CSharpReference<string>("v1"));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        seq.Activities.Add(activity);
        var result = DesignValidationServices.Validate(seq);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CS_AssignmentInLocation_DoesNotWork()
    {
        var activity = new Assign();
        activity.To = new OutArgument<string>(new CSharpReference<string>("Environment.CurrentDirectory = \"abc\""));
        activity.Value = new InArgument<string>(new Literal<string>("bla"));
        var result = DesignValidationServices.Validate(activity);
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("CS0131: The left-hand side of an assignment must be a variable, property or indexer");
    }
    #endregion

    [Fact]
    public void Vb_IntOverflow()
    {
        VisualBasicValue<int> vbv = new("2147483648");
        Sequence workflow = new();
        workflow.Variables.Add(new Variable<int>("someint"));
        Assign assign = new() { To = new OutArgument<int>(workflow.Variables[0]), Value = new InArgument<int>(vbv) };
        workflow.Activities.Add(assign);

        ValidationResults validationResults = DesignValidationServices.Validate(workflow);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("Constant expression not representable in type 'Integer'");
    }

    [Fact]
    public void VBValidator_StrictOn()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("(\"3\" + 3).ToString")) };
        var result = DesignValidationServices.Validate(activity);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("Option Strict On");
    }

    [Fact]
    public void VBValue_ShowsValidationError()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("var1")) };
        var result = DesignValidationServices.Validate(activity);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("'var1' is not declared");
    }

    [Fact]
    public void VBReference_ShowsValidationError()
    {
        var activity = new Assign
        {
            To = new OutArgument<string>(new VisualBasicReference<string>("var1")),
            Value = new InArgument<string>("\"abc\"")
        };
        var result = DesignValidationServices.Validate(activity);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("'var1' is not declared");
    }

    [Fact]
    public void VbValue_IdentifiersComparerOrdinalIgnoreCase()
    {
        var root = new Sequence();
        root.Variables.Add(new Variable<List<object>>("count"));
        root.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("count.Count.ToString")) });

        var result = DesignValidationServices.Validate(root);

        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CSValue_ShowsValidationError()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("var1")) };
        var result = DesignValidationServices.Validate(activity);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("The name 'var1' does not exist in the current context");
    }

    [Fact]
    public void CSReference_ShowsValidationError()
    {
        var activity = new Assign
        {
            To = new OutArgument<string>(new CSharpReference<string>("var1")),
            Value = new InArgument<string>("\"abc\"")
        };
        var result = DesignValidationServices.Validate(activity);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("The name 'var1' does not exist in the current context");
    }

    [Fact]
    public void VBValue_Validate_AddsRequiredAssembliesPerExpressionValidated()
    {
        var simpleActivity = new WriteLine() { Text = new InArgument<string>(new VisualBasicValue<string>("1.ToString")) };

        var requiresNewtonsoftActivity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("GetType(JsonConvert).ToString")) };
        var dy = new DynamicActivity() { Implementation = () => requiresNewtonsoftActivity };
        TextExpression.SetReferencesForImplementation(dy, new[] { new AssemblyReference("Newtonsoft.Json") });
        TextExpression.SetNamespacesForImplementation(dy, new[] { "Newtonsoft.Json" });

        // first validate an activity without needing Newtonsoft.Json assembly
        DesignValidationServices.Validate(simpleActivity);

        // then validate with a new assembly required (Newtonsoft.Json)
        var result = DesignValidationServices.Validate(dy);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CSValue_Validate_AddsRequiredAssembliesPerExpressionValidated()
    {

        var simpleActivity = new WriteLine() { Text = new InArgument<string>(new CSharpValue<string>("1.ToString()")) };

        var requiresNewtonsoftActivity = new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("typeof(JsonConvert).Name")) };
        var dy = new DynamicActivity() { Implementation = () => requiresNewtonsoftActivity };
        TextExpression.SetReferencesForImplementation(dy, new[] { new AssemblyReference("Newtonsoft.Json") });
        TextExpression.SetNamespacesForImplementation(dy, new[] { "Newtonsoft.Json" });

        // first validate an activity without needing Newtonsoft.Json assembly
        DesignValidationServices.Validate(simpleActivity);

        // then validate with a new assembly required (Newtonsoft.Json)
        var result = DesignValidationServices.Validate(dy);
        result.Errors.ShouldBeEmpty();
    }


    [Fact]
    public void VBRoslynValidator_ValidatesMoreThan16Arguments()
    {
        var sequence = new Sequence();
        for (int i = 0; i < 20; i++)
        {
            sequence.Variables.Add(new Variable<string>($"var{i}"));
        }
        var testActivity = new VisualBasicValue<string[]>(string.Format("{{{0}}}", string.Join(", ", Enumerable.Range(0, 20).Select(r => $"var{r}"))));
        sequence.Activities.Add(testActivity);
        var result = DesignValidationServices.Validate(sequence);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CSRoslynValidator_ValidatesMoreThan16Arguments()
    {
        var sequence = new Sequence();
        for (int i = 0; i < 20; i++)
        {
            sequence.Variables.Add(new Variable<string>($"var{i}"));
        }
        var testActivity = new CSharpValue<string[]>(string.Format("new [] {{{0}}}", string.Join(", ", Enumerable.Range(0, 20).Select(r => $"var{r}"))));
        sequence.Activities.Add(testActivity);
        var result = DesignValidationServices.Validate(sequence);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void VB_Multithreaded_NoError()
    {
        var activities = new List<Activity>();
        var tasks = new List<Task>();
        var results = new List<ValidationResults>();
        for (var i = 0; i < 20; i++)
        {
            var seq = new Sequence();
            seq.Variables.Add(new Variable<int>("sum"));
            for (var j = 0; j < 10000; j++)
            {
                seq.Activities.Add(new Assign
                {
                    To = new OutArgument<int>(new VisualBasicReference<int>("sum")),
                    Value = new InArgument<int>(new VisualBasicValue<int>($"sum + {j}"))
                });
            }
        }
        foreach (var activity in activities)
        {
            tasks.Add(Task.Run(() =>
            {
                results.Add(DesignValidationServices.Validate(activity));
            }));
        }
        Task.WaitAll(tasks.ToArray());

        results.All(r => !r.Errors.Any() && !r.Warnings.Any()).ShouldBeTrue();
    }

    [Fact]
    public void CS_Multithreaded_NoError()
    {
        var activities = new List<Activity>();
        var tasks = new List<Task>();
        var results = new List<ValidationResults>();
        for (var i = 0; i < 20; i++)
        {
            var seq = new Sequence();
            seq.Variables.Add(new Variable<int>("sum"));
            for (var j = 0; j < 10000; j++)
            {
                seq.Activities.Add(new Assign
                {
                    To = new OutArgument<int>(new CSharpReference<int>("sum")),
                    Value = new InArgument<int>(new CSharpValue<int>($"sum + {j}"))
                });
            }
        }
        foreach (var activity in activities)
        {
            tasks.Add(Task.Run(() =>
            {
                results.Add(DesignValidationServices.Validate(activity));
            }));
        }
        Task.WaitAll(tasks.ToArray());

        results.All(r => !r.Errors.Any() && !r.Warnings.Any()).ShouldBeTrue();
    }
}