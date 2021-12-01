using BenchmarkDotNet.Attributes;
using System.Activities;
using System.Activities.Validation;
using System.Activities.XamlIntegration;

namespace CoreWf.Benchmarks;

public class Expressions
{
    private readonly Activity _vbXaml;
    private readonly Activity _csXaml;
    private readonly ValidationSettings _skipExprComp;

    public Expressions()
    {
        var asm = typeof(Expressions).Assembly;
        var ns = typeof(Expressions).Namespace;
        using (var stream = asm.GetManifestResourceStream(ns + ".ExpressionsVB.xaml"))
        {
            _vbXaml = ActivityXamlServices.Load(stream);
        }
        using (var stream = asm.GetManifestResourceStream(ns + ".ExpressionsCSharp.xaml"))
        {
            _csXaml = ActivityXamlServices.Load(stream);
        }
        _skipExprComp = new ValidationSettings() { SkipExpressionCompilation = true };
    }

    [Benchmark(Baseline = true)]
    public void SkipExpressionCompilation()
    {
        _ = ActivityValidationServices.Validate(_vbXaml, _skipExprComp);
    }

    [Benchmark]
    public void VB()
    {
        _ = ActivityValidationServices.Validate(_vbXaml);
    }

    [Benchmark]
    public void CSharp()
    {
        _ = ActivityValidationServices.Validate(_csXaml);
    }
}
