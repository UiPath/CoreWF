using BenchmarkDotNet.Attributes;
using Microsoft.VisualBasic.Activities;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Text;

namespace CoreWf.Benchmarks;

public class Expressions
{
    private readonly Activity[] _vb100Stmts;
    private readonly Activity[] _vb400Stmts;
    private readonly Activity[] _vbSingleExpr100;
    private int _activityIndex;

    public Expressions()
    {
        _vb100Stmts = new Activity[]
        {
            GenerateManyExpressionsWorkflow(0, 100),
            GenerateManyExpressionsWorkflow(100, 100),
            GenerateManyExpressionsWorkflow(200, 100),
            GenerateManyExpressionsWorkflow(300, 100),
            GenerateManyExpressionsWorkflow(400, 100),
            GenerateManyExpressionsWorkflow(500, 100),
            GenerateManyExpressionsWorkflow(600, 100),
            GenerateManyExpressionsWorkflow(700, 100),
            GenerateManyExpressionsWorkflow(800, 100),
            GenerateManyExpressionsWorkflow(900, 100),
        };
        _vb400Stmts = new Activity[]
        {
            GenerateManyExpressionsWorkflow(0, 400),
            GenerateManyExpressionsWorkflow(400, 400),
            GenerateManyExpressionsWorkflow(800, 400),
        };
        _vbSingleExpr100 = GenerateManySingleExpressionWorkflows(500, 100);
        _activityIndex = 0;
    }

    [Benchmark]
    public void VB100Stmts()
    {
        var activity = _vb100Stmts[_activityIndex];
        _activityIndex = (_activityIndex + 1) % _vb100Stmts.Length;
        _ = ActivityValidationServices.Validate(activity);
    }

    [Benchmark]
    public void VB400Stmts()
    {
        var activity = _vb400Stmts[_activityIndex];
        _activityIndex = (_activityIndex + 1) % _vb400Stmts.Length;
        _ = ActivityValidationServices.Validate(activity);
    }

    [Benchmark]
    public void VBSingleExpr100()
    {
        var activity = _vbSingleExpr100[_activityIndex];
        _activityIndex = (_activityIndex + 1) % _vbSingleExpr100.Length;
        _ = ActivityValidationServices.Validate(activity);
    }

    private static Activity GenerateManyExpressionsWorkflow(int startExprNum, int numExpressions)
    {
        Sequence workflow = new();
        int endExprNum = startExprNum + numExpressions;
        for (int i = startExprNum; i < endExprNum; i++)
        {
            VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", ""beta "", {i})");
            WriteLine writeLine = new();
            writeLine.Text = new InArgument<string>(vbv);
            workflow.Activities.Add(writeLine);
        }

        return workflow;
    }

    private static Activity GenerateSingleExpressionWorkflow(int startExprNum, int numExpressions)
    {
        StringBuilder sb = new();
        sb.Append(@"String.Concat(""alpha "", ""beta """);
        int endExprNum = startExprNum + numExpressions;
        for (int i = startExprNum; i < endExprNum; i++)
        {
            sb.Append(", ");
            sb.Append(i);
        }
        sb.Append(')');

        VisualBasicValue<string> vbv = new(sb.ToString());
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        return workflow;
    }

    private static Activity[] GenerateManySingleExpressionWorkflows(int numWorkflows, int numExpressions)
    {
        Activity[] workflows = new Activity[numWorkflows];
        for (int i = 0; i < numWorkflows; i++)
        {
            workflows[i] = GenerateSingleExpressionWorkflow(i, numExpressions);
        }
        return workflows;
    }
}
