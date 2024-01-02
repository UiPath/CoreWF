using BenchmarkDotNet.Running;
using CoreWf.Benchmarks;

#if RELEASE
BenchmarkRunner.Run<ExpressionTextboxPerformance>();
#else
new ExpressionTextboxPerformance().CS_CreatePrecompiledValue();
new ExpressionTextboxPerformance().VB_CreatePrecompiledValue();
await new ExpressionTextboxPerformance().CS_CreatePrecompiledValueAsync();
await new ExpressionTextboxPerformance().VB_CreatePrecompiledValueAsync();
#endif