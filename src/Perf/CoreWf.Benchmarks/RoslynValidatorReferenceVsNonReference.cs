using BenchmarkDotNet.Attributes;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System.Activities.Validation;

namespace CoreWf.Benchmarks;

public class RoslynValidatorReferenceVsNonReference
{
    private readonly ValidationSettings _useValidator = new() { ForceExpressionCache = false };

    [Benchmark]
    public void TestVBValue()
    {
        for (var i = 0; i < 1000; i++)
        {
            var activity = new VisualBasicValue<bool>("Environment.Is64BitOperatingSystem");
            ActivityValidationServices.Validate(activity, _useValidator);
        }
    }

    [Benchmark]
    public void TestVBReference()
    {
        for (var i = 0; i < 1000; i++)
        {
            var activity = new VisualBasicReference<bool>("Environment.Is64BitOperatingSystem");
            ActivityValidationServices.Validate(activity, _useValidator);
        }
    }

    [Benchmark]
    public void TestCSReference()
    {
        for (var i = 0; i < 1000; i++)
        {
            var activity = new CSharpReference<bool>("Environment.Is64BitOperatingSystem");
            ActivityValidationServices.Validate(activity, _useValidator);
        }
    }

    [Benchmark]
    public void TestCSValue()
    {
        for (var i = 0; i < 1000; i++)
        {
            var activity = new CSharpValue<bool>("Environment.Is64BitOperatingSystem");
            ActivityValidationServices.Validate(activity, _useValidator);
        }
    }
}
