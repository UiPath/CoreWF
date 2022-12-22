using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestCases.Activities.Bpm;
using TestCases.Runtime.WorkflowInstanceTest;

namespace TestConsole;
class Program
{
    static void Main()
    {
        try
        {
            new BpmParallelTest().Should_join_branches();
            //var writeLine = new WriteLine { Text = "dd" };
            //var sequence = new Sequence { DisplayName = "Outer",  Activities = { new Sequence { DisplayName = "Inner" , Activities = { writeLine } }, writeLine } };
            //WorkflowInvoker.Invoke(sequence);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.ReadLine();
        }
    }
}
//new PowerFxTests().EvaluateMembers();
//var engine = new RecalcEngine();
//var defaultValue = FormulaValue.New(null, typeof(string));
//var record = FormulaValue.RecordFromFields(new NamedValue("x", defaultValue));
//var text = "1+Len(Left(x, 2))/2";
//var checkResult = engine.Check(text, record.Type);
//checkResult.ThrowOnErrors();
//Console.WriteLine(checkResult.ReturnType);
//var formulaValue = engine.Eval(text, record);
//Console.WriteLine(formulaValue.ToObject());
//return;
//System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
//new JustInTimeExpressions().SalaryCalculation();