using BenchmarkDotNet.Running;
using CoreWf.Benchmarks;

#if RELEASE
BenchmarkRunner.Run<Expressions>();
#else
var e = new Expressions();
e.VB();
#endif
