namespace Microsoft.Common
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.CodeAnalysis.Text;
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Internals;
    using System.Linq;
    using System.Linq.Expressions;
    using UiPath.Workflow;

    public abstract class JustInTimeCompiler
    {
        private const string CommaSeparator = ", ";

        protected MetadataReference[] MetadataReferences { get; set; }

        protected abstract Script CreateScript(string expression, ScriptOptions options);

        protected abstract string[] GetIdentifiers(Compilation compilation, SyntaxTree syntaxTree);

        protected abstract string GetFriendlyTypeName(Type type);

        protected abstract string ExpressionTemplate { get; }

        public LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
        {
            var options = ScriptOptions.Default
                .AddReferences(MetadataReferences)
                .AddImports(expressionToCompile.ImportedNamespaces);
            options = AddOptions(options);
            var untypedExpressionScript = CreateScript(expressionToCompile.Code, options);
            var compilation = untypedExpressionScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.First();
            var identifiers = GetIdentifiers(compilation, syntaxTree);
            var resolvedIdentifiers =
                identifiers
                .Select(name => (Name: name, Type: expressionToCompile.VariableTypeGetter(name)))
                .Where(var => var.Type != null)
                .ToArray();
            var names = string.Join(CommaSeparator, resolvedIdentifiers.Select(var => var.Name));
            var types = string.Join(CommaSeparator,
                resolvedIdentifiers
                .Select(var => var.Type)
                .Concat(new[] { expressionToCompile.LambdaReturnType })
                .Select(GetFriendlyTypeName));
            var finalCompilation = compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(
                string.Format(ExpressionTemplate, types, names, expressionToCompile.Code))));
            var results = ScriptingAheadOfTimeCompiler.BuildAssembly(finalCompilation);
            if (results.HasErrors)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.Code, results), results.CompilerMessages));
            }
            return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
        }

        public virtual ScriptOptions AddOptions(ScriptOptions options) => options;
    }
}