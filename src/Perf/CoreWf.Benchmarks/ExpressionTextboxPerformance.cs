using BenchmarkDotNet.Attributes;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System.Activities;

namespace CoreWf.Benchmarks
{
    [MemoryDiagnoser(true)]
    public class ExpressionTextboxPerformance
    {
        static int index = 0;
        readonly Type environmentType = typeof(LocationReferenceEnvironment).Assembly.GetTypes().FirstOrDefault(t => t.Name == "ActivityLocationReferenceEnvironment")!;

        public LocationReferenceEnvironment GetFreshEnvironment => (Activator.CreateInstance(environmentType) as LocationReferenceEnvironment)!;
        [Benchmark]
        public void CS_CreatePrecompiledValue()
        {
            CSharpDesignerHelper.CreatePrecompiledValue(typeof(object), $"1 + {index++}", new[] { "System" }, new[] { "System" }, GetFreshEnvironment, out var returnType, out var errors, out var settings);
        }

        [Benchmark]
        public void VB_CreatePrecompiledValue()
        {
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(object), $"1 + {index++}", new[] { "System" }, new[] { "System" }, GetFreshEnvironment, out var returnType, out var errors, out var settings);
        }

        [Benchmark]
        public async Task CS_CreatePrecompiledValueAsync()
        {
            await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(object), $"1 + {index++}", new[] { "System" }, new[] { "System" }, GetFreshEnvironment);
        }

        [Benchmark]
        public async Task VB_CreatePrecompiledValueAsync()
        {
            await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(object), $"1 + {index++}", new[] { "System" }, new[] { "System" }, GetFreshEnvironment);
        }
    }
}
