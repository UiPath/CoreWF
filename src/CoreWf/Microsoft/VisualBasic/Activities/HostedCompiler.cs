using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ReflectionMagic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.VisualBasic.Activities
{
    internal class HostedCompiler
    {
        public LambdaExpression CompileExpression(string expressionString, Func<string, Type> getVariableType, ScriptOptions options, Type lambdaReturnType = null)
        {
            var untypedExpressionScript = VisualBasicScript.Create($"? {expressionString}", options);
            var identifiers = IdentifiersWalker.GetIdentifiers(untypedExpressionScript);
            var resolvedIdentifiers =
                identifiers
                .Select(name => (Name: name, Type: getVariableType(name)))
                .Where(var => var.Type != null)
                .ToArray();
            const string Comma = ", ";
            var names = string.Join(Comma, resolvedIdentifiers.Select(var => var.Name));
            var types = string.Join(Comma,
                resolvedIdentifiers
                .Select(var => var.Type)
                .Concat(new[] { lambdaReturnType ?? typeof(object) })
                .Select(type => GetTypeName(type)));
            var typedExpressionScript = 
                VisualBasicScript
                .Create($"Dim resultExpression As Expression(Of Func(Of {types})) = Function({names}) ({expressionString})", options)
                .ContinueWith("? resultExpression", options);
            return (LambdaExpression)typedExpressionScript.RunAsync().GetAwaiter().GetResult().ReturnValue;
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

        private static string GetTypeName(Type type) => TypeNameFormatter.FormatTypeName(type, TypeOptions);

        private static readonly dynamic TypeOptions;

        private static readonly dynamic TypeNameFormatter =
            typeof(VisualBasicScript).Assembly.GetType("Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.VisualBasicObjectFormatter")
            .AsDynamicType()
            .s_impl
            .TypeNameFormatter;

        static HostedCompiler()
        {
            var type = typeof(ObjectFormatter).Assembly.GetType("Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");

            var args = new object[]
            {
                0, /* arrayBoundRadix */
                true /* showNamespaces */
            };

            TypeOptions = Activator.CreateInstance(type, args);
        }

        internal void Dispose()
        {
        }
    }
}
