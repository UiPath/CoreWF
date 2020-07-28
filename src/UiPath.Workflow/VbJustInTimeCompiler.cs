using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace UiPath.Workflow
{
    public class VbJustInTimeCompiler : JustInTimeCompiler
    {
        protected virtual ScriptOptions AddOptions(ScriptOptions options) => options;
        public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
        {
            var options = ScriptOptions.Default
                .AddReferences(expressionToCompile.ReferencedAssemblies)
                .AddImports(expressionToCompile.ImportedNamespaces);
            options = AddOptions(options);
            var untypedExpressionScript = VisualBasicScript.Create($"? {expressionToCompile.ExpressionString}", options);
            var identifiers = IdentifiersWalker.GetIdentifiers(untypedExpressionScript);
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
                .Select(VisualBasicObjectFormatter.FormatTypeName));
            var typedExpressionScript =
                VisualBasicScript
                .Create($"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({expressionToCompile.ExpressionString})\nEnd Function", options);
            var results = ScriptingAheadOfTimeCompiler.Compile(typedExpressionScript);
            if (results.HasErrors())
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.ExpressionString, results), results.CompilerMessages));
            }
            return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
        }
        class IdentifiersWalker : VisualBasicSyntaxWalker
        {
            private readonly HashSet<string> _identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public SemanticModel SemanticModel { get; }
            private IdentifiersWalker(SemanticModel semanticModel) => SemanticModel = semanticModel;
            public static string[] GetIdentifiers(Script script)
            {
                var compilation = script.GetCompilation();
                var syntaxTree = compilation.SyntaxTrees.First();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();
                var walker = new IdentifiersWalker(semanticModel);
                walker.Visit(root);
                return walker._identifiers.ToArray();
            }
            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                _identifiers.Add(node.Identifier.Text);
                base.VisitIdentifierName(node);
            }
        }
    }
    //static class References
    //{
    //    unsafe static MetadataReference GetReference(Assembly assembly)
    //    {
    //        if (!assembly.TryGetRawMetadata(out var blob, out var length))
    //        {
    //            throw new NotSupportedException();
    //        }
    //        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
    //        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
    //        return assemblyMetadata.GetReference();
    //    }
    //    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<string> assemblies) => 
    //        assemblies.Select(Assembly.Load).GetMetadataReferences();
    //    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) =>
    //        assemblies.Select(GetReference);
    //}
}