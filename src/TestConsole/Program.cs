using System;
using System.Diagnostics;
using TestCases.Workflows.WF4Samples;

namespace TestConsole
{
    class Program
    {
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            new AheadOfTimeExpressions().CompileCSharpCalculation();
        }
    }
}
