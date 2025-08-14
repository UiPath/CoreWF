using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Activities.JitCompilerHelper;

namespace Microsoft.CSharp.Activities;

internal sealed class CSharpExpressionCompiler : ExpressionCompiler
{
    private readonly CSharpCompilerHelper _compilerHelper = new();

    protected override Compilation GetCompilation(IReadOnlyCollection<AssemblyReference> assemblies, IReadOnlyCollection<string> namespaces)
    {
        var options = _compilerHelper.DefaultCompilationUnit.Options as CSharpCompilationOptions;

        return _compilerHelper.DefaultCompilationUnit
            .WithOptions(options.WithUsings(namespaces.Union(CompilerHelper.DefaultNamespaces)))
            .WithReferences(MetadataReferenceUtils.GetMetadataReferences(assemblies));
    }

    public override Type GetReturnType(Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var allNodes = syntaxTree.GetRoot().DescendantNodes().ToList();

        var para = allNodes.First(n => n.GetType() == typeof(ParenthesizedLambdaExpressionSyntax));
        var node = allNodes.Skip(allNodes.IndexOf(para) + 1).OfType<ExpressionSyntax>().First();
        var typeInfo = semanticModel.GetTypeInfo(node);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;

        return GetSystemType(typeSymbol, GetAssemblyForType(typeSymbol));
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expression, bool isLocation, Type returnType, LocationReferenceEnvironment environment)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(expression, _compilerHelper.ScriptParseOptions);
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == (int)SyntaxKind.IdentifierName)
                                    .Select(n => n.ToString()).Distinct(_compilerHelper.IdentifierNameComparer);
        var resolvedIdentifiers = identifiers
                .Select(name => (Name: name, Type: new ScriptAndTypeScope(environment).FindVariable(name, _compilerHelper.IdentifierNameComparison)))
                .Where(var => var.Type != null)
                .ToArray();

        var names = resolvedIdentifiers.Select(var => var.Name).ToArray();
        var types = resolvedIdentifiers.Select(var => var.Type).Concat(new[] { returnType }).Select(_compilerHelper.GetTypeName).ToArray();
        var lambdaFuncCode = _compilerHelper.CreateExpressionCode(types, names, expression);
        return CSharpSyntaxTree.ParseText(lambdaFuncCode, _compilerHelper.ScriptParseOptions);
    }
}
