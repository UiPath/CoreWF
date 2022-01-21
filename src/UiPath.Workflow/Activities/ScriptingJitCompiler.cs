// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using ReflectionMagic;
using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

namespace System.Activities;

public abstract class JustInTimeCompiler
{
    public abstract LambdaExpression CompileExpression(ExpressionToCompile compilerRequest);
}
public record CompilerInput(string Code, IReadOnlyCollection<string> ImportedNamespaces)
{
}
public record ExpressionToCompile(string Code, IReadOnlyCollection<string> ImportedNamespaces, Func<string, Type> VariableTypeGetter, Type LambdaReturnType)
    : CompilerInput(Code, ImportedNamespaces)
{
}
public abstract class ScriptingJitCompiler : JustInTimeCompiler
{
    protected MetadataReference[] MetadataReferences { get; set; }
    protected ScriptingJitCompiler(HashSet<Assembly> referencedAssemblies) => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
    protected abstract int IdentifierKind { get; }
    protected abstract string CreateExpressionCode(string types, string names, string code);
    protected abstract string GetTypeName(Type type);
    protected abstract Script<object> Create(string code, ScriptOptions options);
    public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
    {
        var options = ScriptOptions.Default
            .WithReferences(MetadataReferences)
            .WithImports(expressionToCompile.ImportedNamespaces)
            .WithOptimizationLevel(OptimizationLevel.Release);
        var untypedExpressionScript = Create(expressionToCompile.Code, options);
        var compilation = untypedExpressionScript.GetCompilation();
        var syntaxTree = compilation.SyntaxTrees.First();
        var identifiers = GetIdentifiers(syntaxTree);
        var resolvedIdentifiers =
            identifiers
            .Select(name => (Name: name, Type: expressionToCompile.VariableTypeGetter(name)))
            .Where(var => var.Type != null)
            .ToArray();
        const string Comma = ", ";
        var names = string.Join(Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = string.Join(Comma,
            resolvedIdentifiers
            .Select(var => var.Type)
            .Concat(new[] { expressionToCompile.LambdaReturnType })
            .Select(GetTypeName));
        var finalCompilation = compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(
            CreateExpressionCode(types, names, expressionToCompile.Code))));
        var results = ScriptingAotCompiler.BuildAssembly(finalCompilation);
        if (results.HasErrors)
        {
            throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.Code, results), results.CompilerMessages));
        }
        return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
    }
    public IEnumerable<string> GetIdentifiers(SyntaxTree syntaxTree) =>
    syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == IdentifierKind).Select(n => n.ToString()).Distinct().ToArray();
}
public static class References
{
    public unsafe static MetadataReference GetReference(Assembly assembly)
    {
        if (!assembly.TryGetRawMetadata(out var blob, out var length))
        {
            throw new NotSupportedException();
        }
        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
        return assemblyMetadata.GetReference();
    }
    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) => assemblies.Select(GetReference);
}
public class VbJitCompiler : ScriptingJitCompiler
{
    public VbJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }
    protected override Script<object> Create(string code, ScriptOptions options) => VisualBasicScript.Create("? " + code, options);
    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
    protected override string CreateExpressionCode(string types, string names, string code) =>
         $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";
    protected override int IdentifierKind => (int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierName;
}
public class CSharpJitCompiler : ScriptingJitCompiler
{
    private static readonly dynamic TypeOptions = GetTypeOptions();
    private static readonly dynamic TypeNameFormatter = GetTypeNameFormatter();
    public CSharpJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }
    protected override Script<object> Create(string code, ScriptOptions options) => CSharpScript.Create(code, options);
    protected override string GetTypeName(Type type) => (string)TypeNameFormatter.FormatTypeName(type, TypeOptions);
    protected override string CreateExpressionCode(string types, string names, string code) =>
         $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";
    protected override int IdentifierKind => (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName;
    static object GetTypeOptions()
    {
        var formatterOptionsType = typeof(ObjectFormatter).Assembly.GetType("Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
        const int ArrayBoundRadix = 0;
        const bool ShowNamespaces = true;
        return Activator.CreateInstance(formatterOptionsType, new object[] { ArrayBoundRadix, ShowNamespaces });
    }
    static object GetTypeNameFormatter() =>
        typeof(CSharpScript).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.CSharpObjectFormatter")
        .AsDynamicType()
        .s_impl
        .TypeNameFormatter;
}
