using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;
using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TestCases.Workflows;
using TestCases.Workflows.WF4Samples;
namespace TestConsole;
class Program
{
    static void Main()
    {
        WorkflowInvoker.Invoke(new TestDelay());
        return;
        WorkflowInvoker.Invoke(new Sequence());
        while (true)
        {
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            for (int index = 0; index < 1000; index++)
            {
                WorkflowInvoker.Invoke(new Sequence());
            }
            stopWatch.Stop();
            Console.WriteLine(stopWatch.Elapsed.TotalMilliseconds);
            Console.WriteLine(GC.CollectionCount(0));
            Console.ReadLine();
        }
        new PowerFxTests().EvaluateMembers();
        var engine = new RecalcEngine();
        var defaultValue = FormulaValue.New(null, typeof(string));
        var record = FormulaValue.RecordFromFields(new NamedValue("x", defaultValue));
        var text = "1+Len(Left(x, 2))/2";
        var checkResult = engine.Check(text, record.Type);
        checkResult.ThrowOnErrors();
        Console.WriteLine(checkResult.ReturnType);
        var formulaValue = engine.Eval(text, record);
        Console.WriteLine(formulaValue.ToObject());
        return;
        System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        new JustInTimeExpressions().SalaryCalculation();
    }
}
public class WriteLineEx : ActivityEx<KeyValues>
{
    public WriteLineEx(WriteLine activity) : base(activity) { }
    public string Text
    {
        get => Get<string>();
        set => Set(value);
    }
    public TextWriter TextWriter
    {
        get => Get<TextWriter>();
        set => Set(value);
    }
}
public sealed class Assign<T> : CodeActivity
{
    public OutArgument<T> To { get; set; }
    public InArgument<T> Value { get; set; }
    protected override void Execute(CodeActivityContext context) => context.SetValue(To, Value.Get(context));
}
public class AssignEx<T> : ActivityEx<AssignOutputs<T>>
{
    public AssignEx(Assign<T> activity) : base(activity) { }
    public T Value
    {
        get => Get<T>();
        set => Set(value);
    }
}
public class AssignOutputs<T> : KeyValues
{
    public T To => Get<T>();
}
public class TestDelay : AsyncCodeNativeActivity
{
    WriteLineEx _writeLine1 = new(new WriteLine() { Text = "AAAAAAAAAAAAAAAA" });
    WriteLineEx _writeLine2 = new(new WriteLine() { Text = "BBBBBBBBBBBBBBBB" });
    AssignEx<int> _assign1 = new(new Assign<int> { Value = 1 });
    public TestDelay() => _children = new ActivityEx[] { _writeLine1, _writeLine2, _assign1 };
    protected override async Task ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
    {
        await _writeLine1.ExecuteAsync();
        for (int index = 0; index < 3; index++)
        {
            _writeLine1.Text = index.ToString();
            await _writeLine1.ExecuteAsync();
        }
        await Task.Delay(1000, cancellationToken);
        await _writeLine2.ExecuteAsync();
        Console.WriteLine((await _assign1.ExecuteAsync()).To);
        _assign1.Value = 42;
        Console.WriteLine((await _assign1.ExecuteAsync()).To);
        await Task.Delay(1000, cancellationToken);
    }
}