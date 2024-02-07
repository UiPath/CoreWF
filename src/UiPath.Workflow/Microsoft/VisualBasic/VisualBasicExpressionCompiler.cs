﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Activities.JitCompilerHelper;

namespace Microsoft.VisualBasic.Activities;

internal sealed class VisualBasicExpressionCompiler : ExpressionCompiler
{
    private readonly VBCompilerHelper _compilerHelper = new();

    public override Type GetReturnType(Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var allNodes = syntaxTree.GetRoot().DescendantNodes().ToList();

        var para = allNodes.First(n => n.GetType() == typeof(SingleLineLambdaExpressionSyntax));
        var node = allNodes.Skip(allNodes.IndexOf(para) + 1).OfType<ExpressionSyntax>().First();

        var typeInfo = semanticModel.GetTypeInfo(node).Type;

        return GetSystemType(typeInfo, GetAssemblyForType(typeInfo));
    }

    protected override Compilation GetCompilation(IReadOnlyCollection<AssemblyReference> assemblies, IReadOnlyCollection<string> namespaces)
    {
        var options = _compilerHelper.DefaultCompilationUnit.Options as VisualBasicCompilationOptions;

        return _compilerHelper.DefaultCompilationUnit
            .WithOptions(options!.WithGlobalImports(GlobalImport.Parse(namespaces.Union(CompilerHelper.DefaultNamespaces))))
            .WithReferences(MetadataReferenceUtils.GetMetadataReferences(assemblies));
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expression, bool isLocation, Type returnType, LocationReferenceEnvironment environment)
    {
        var syntaxTree = VisualBasicSyntaxTree.ParseText(expression, _compilerHelper.ScriptParseOptions);
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == (int)SyntaxKind.IdentifierName)
                                    .Select(n => n.ToString()).Distinct(_compilerHelper.IdentifierNameComparer);
        var resolvedIdentifiers = identifiers
                .Select(name => (Name: name, Type: new ScriptAndTypeScope(environment).FindVariable(name)))
                .Where(var => var.Type != null)
                .ToArray();

        var names = string.Join(CompilerHelper.Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = string.Join(CompilerHelper.Comma, resolvedIdentifiers.Select(var => var.Type).Concat(new[] { returnType }).Select(_compilerHelper.GetTypeName));
        var lambdaFuncCode = _compilerHelper.CreateExpressionCode(types, names, expression);
        return VisualBasicSyntaxTree.ParseText(lambdaFuncCode, _compilerHelper.ScriptParseOptions);
    }
}
