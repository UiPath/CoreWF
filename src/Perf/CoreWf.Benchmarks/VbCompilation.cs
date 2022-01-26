using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace CoreWf.Benchmarks;

public class VbCompilation
{
    private static int GlobalCounter = 0;
    private VisualBasicCompilation _visualBasicCompilation;
    private VisualBasicParseOptions _parseOptions = new(kind: SourceCodeKind.Script);

    public VbCompilation()
    {
        string assemblyName = Guid.NewGuid().ToString();
        List<MetadataReference> references = new();
        MetadataReference mr = MetadataReference.CreateFromFile(typeof(string).Assembly.Location);
        references.Add(mr);
        VisualBasicCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary);
        _visualBasicCompilation = VisualBasicCompilation.Create(assemblyName, /*syntax trees*/ null, references, options);
        var syntaxTree = VisualBasicSyntaxTree.ParseText(GetExpressionText(), _parseOptions);
        _visualBasicCompilation = _visualBasicCompilation.AddSyntaxTrees(syntaxTree);
        var diagnostics = _visualBasicCompilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error))
        {
            Console.WriteLine("Error encountered");
        }
    }

    [Benchmark]
    public void MetadataReferenceCreate()
    {
        _ = MetadataReference.CreateFromFile(typeof(string).Assembly.Location);
    }

    [Benchmark]
    public void SingleExpression()
    {
        var oldSyntaxTree  = _visualBasicCompilation.SyntaxTrees[0];
        var syntaxTree = VisualBasicSyntaxTree.ParseText(GetExpressionText(), _parseOptions);
        _visualBasicCompilation = _visualBasicCompilation.ReplaceSyntaxTree(oldSyntaxTree, syntaxTree);
        _ = _visualBasicCompilation.GetDiagnostics();
    }

    private static string GetExpressionText() => @$"String.Concat(""alpha "", ""beta "", a, {GlobalCounter++})";
}

