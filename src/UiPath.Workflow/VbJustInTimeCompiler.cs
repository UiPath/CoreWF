﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

namespace UiPath.Workflow
{
    public class VbJustInTimeCompiler : ScriptingJustInTimeCompiler
    {
        public VbJustInTimeCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }
        protected override Script<object> Create(string code, ScriptOptions options) => VisualBasicScript.Create("? "+code, options);
        protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
        protected override string CreateExpressionCode(string types, string names, string code) =>
             $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";
    }
    public abstract class ScriptingJustInTimeCompiler : JustInTimeCompiler
    {
        protected MetadataReference[] MetadataReferences { get; set; }
        protected ScriptingJustInTimeCompiler(HashSet<Assembly> referencedAssemblies) => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
        public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
        {
            var options = ScriptOptions.Default
                .AddReferences(MetadataReferences)
                .AddImports(expressionToCompile.ImportedNamespaces);
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
            var finalCompilation = compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(CreateExpressionCode(types, names, expressionToCompile.Code))));
            var results = ScriptingAheadOfTimeCompiler.BuildAssembly(finalCompilation);
            if (results.HasErrors)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.Code, results), results.CompilerMessages));
            }
            return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
        }
        protected abstract string CreateExpressionCode(string types, string names, string code);
        protected abstract string GetTypeName(Type type);
        protected abstract Script<object> Create(string code, ScriptOptions options);
        public static IEnumerable<string> GetIdentifiers(SyntaxTree syntaxTree) =>
            syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == (int)SyntaxKind.IdentifierName).Select(n => n.ToString()).Distinct().ToArray();
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
        public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) => assemblies.Select(GetReference);
    }
}