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
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace UiPath.Workflow
{
    class VbJustInTimeCompiler : JustInTimeCompiler
    {
        public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
        {
            var options = ScriptOptions.Default
                .AddReferences(expressionToCompile.ReferencedAssemblies)
                .AddImports(expressionToCompile.ImportedNamespaces);
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
                .Concat(new[] { expressionToCompile.LambdaReturnType ?? typeof(object) })
                .Select(VisualBasicObjectFormatter.FormatTypeName));
            var typedExpressionScript = 
                VisualBasicScript
                .Create($"Dim resultExpression As Expression(Of Func(Of {types})) = Function({names}) ({expressionToCompile.ExpressionString})", options)
                .ContinueWith("? resultExpression", options);
            try
            {
                return (LambdaExpression)typedExpressionScript.RunAsync().GetResult().ReturnValue;
            }
            catch (CompilationErrorException ex)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.ExpressionString, ex.ToString())));
            }
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