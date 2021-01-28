namespace Microsoft.CSharp
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public class CSharpJustInTimeCompiler : JustInTimeCompiler
    {
        public CSharpJustInTimeCompiler(HashSet<Assembly> referencedAssemblies)
            => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();

        protected override string ExpressionTemplate
            => "public static Expression<Func<{0}>> CreateExpression(){{ \nreturn ({1}) => {2};\n}}";

        protected override Script CreateScript(string expression, ScriptOptions options)
            => CSharpScript.Create(expression, options);

        protected override string[] GetIdentifiers(Compilation compilation, SyntaxTree syntaxTree)
            => IdentifiersWalker.GetIdentifiers(compilation, syntaxTree);

        protected override string GetFriendlyTypeName(Type type)
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
}