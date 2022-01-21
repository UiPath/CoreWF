using BenchmarkDotNet.Running;
using CoreWf.Benchmarks;

#if RELEASE
BenchmarkRunner.Run<Expressions>();
#else
var e = new Expressions();
try
{
    //e.CSharp();
    //e.VB();
    //e.VBSingleExpr100();
    e.VB400Stmts();
    //e.VBSingleExpr100Stmts();
    //e.VBSingleExpr200Stmts();
    //e.VBSingleExpr400Stmts();
    //e.VB400Stmts_AOT();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
#endif
