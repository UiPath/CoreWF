namespace Microsoft.VisualBasic
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Scripting;
    using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;
    using Microsoft.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class VisualBasicJustInTimeCompiler : JustInTimeCompiler
    {
        public VisualBasicJustInTimeCompiler(HashSet<Assembly> referencedAssemblies)
            => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();

        protected override string ExpressionTemplate
            => "Public Shared Function CreateExpression() As Expression(Of Func(Of {0}))\nReturn Function({1}) ({2})\nEnd Function";

        protected override Script CreateScript(string expression, ScriptOptions options)
            => VisualBasicScript.Create($"? {expression}", options);

        protected override string[] GetIdentifiers(Compilation compilation, SyntaxTree syntaxTree)
            => IdentifiersWalker.GetIdentifiers(compilation, syntaxTree);

        protected override string GetFriendlyTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);

        class IdentifiersWalker : VisualBasicSyntaxWalker
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