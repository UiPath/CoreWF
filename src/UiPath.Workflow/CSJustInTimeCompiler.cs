namespace Microsoft.CSharp.Activities
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.CodeAnalysis.Text;
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Internals;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Text;
    using UiPath.Workflow;

    public class CSJustInTimeCompiler : JustInTimeCompiler
    {
        protected MetadataReference[] MetadataReferences { get; set; }
        public CSJustInTimeCompiler(HashSet<Assembly> referencedAssemblies) => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
        public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
        {
            var options = ScriptOptions.Default
                .AddReferences(MetadataReferences)
                .AddImports(expressionToCompile.ImportedNamespaces);
            options = AddOptions(options);
            var untypedExpressionScript = CSharpScript.Create(expressionToCompile.Code, options);
            var compilation = untypedExpressionScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.First();
            var identifiers = IdentifiersWalker.GetIdentifiers(compilation, syntaxTree);
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
                .Select(GetFriendlyTypeName));
            var finalCompilation = compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(
              $"public static Expression<Func<{types}>> CreateExpression(){{ \nreturn ({names}) => {expressionToCompile.Code};\n}}")));
            var results = ScriptingAheadOfTimeCompiler.BuildAssembly(finalCompilation);
            if (results.HasErrors)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.Code, results), results.CompilerMessages));
            }
            return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var preFormat = "<";
            var postFormat = ">";
            if (type == null)
            {
                return null;
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            builder.AppendFormat("{0}.{1}", type.Namespace, name.Substring(0, index));
            builder.Append(preFormat);
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(',');
                }
                builder.Append(GetFriendlyTypeName(arg));
                first = false;
            }
            builder.Append(postFormat);
            return builder.ToString();
        }

        public virtual ScriptOptions AddOptions(ScriptOptions options) => options;
        class IdentifiersWalker : CSharpSyntaxWalker
        {
            private readonly HashSet<string> _identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public SemanticModel SemanticModel { get; }
            private IdentifiersWalker(SemanticModel semanticModel) => SemanticModel = semanticModel;
            public static string[] GetIdentifiers(Compilation compilation, SyntaxTree syntaxTree)
            {
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
    static class References
    {
        unsafe static MetadataReference GetReference(Assembly assembly)
        {
            if (!assembly.TryGetRawMetadata(out var blob, out var length))
            {
                throw new NotSupportedException();
            }
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }
        public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) =>
            assemblies.Select(GetReference);
    }
}