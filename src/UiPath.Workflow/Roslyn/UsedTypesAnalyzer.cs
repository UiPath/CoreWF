using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CSharp.Activities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace System.Activities;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class UsedTypesAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _usedTypesRule = new("UT_001", "Used reference", "'{0}'", "Architecture", DiagnosticSeverity.Info, true);
#pragma warning restore RS2008

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(_usedTypesRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterCompilationAction(FindUsedTypes);
    }

    private void FindUsedTypes(CompilationAnalysisContext context)
    {
        HashSet<string> usedTypes = new();
        try
        {
            using var syntaxTreeEnumerator = context.Compilation.SyntaxTrees.GetEnumerator();
            while (syntaxTreeEnumerator.MoveNext()
                   && !context.CancellationToken.IsCancellationRequested)
            {
                var syntaxTree = syntaxTreeEnumerator.Current;
#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
#pragma warning restore RS1030
                var referenceCounter = GetTypeIdentifier(in context, syntaxTree, semanticModel, usedTypes);
                referenceCounter.FindUsedTypes(context.CancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }

        if (context.CancellationToken.IsCancellationRequested)
            return;

        foreach (var type in usedTypes)
        {
            context.ReportDiagnostic(Diagnostic.Create(_usedTypesRule, null, type));
        }
    }

    private static ITypeIdentifier GetTypeIdentifier(in CompilationAnalysisContext context, SyntaxTree syntaxTree, SemanticModel semanticModel, HashSet<string> usedTypes)
    {
        return context.Compilation.Language == CSharpHelper.Language
            ? new CSharpTypeIdentifier(syntaxTree, semanticModel, usedTypes)
            : new VisualBasicTypeIdentifier(syntaxTree, semanticModel, usedTypes);
    }

    private interface ITypeIdentifier
    {
        void FindUsedTypes(CancellationToken ct);
    }

    private sealed class CSharpTypeIdentifier : CSharpSyntaxWalker, ITypeIdentifier
    {
        private readonly HashSet<string> _usedTypes;
        private readonly SemanticModel _semanticModel;
        private readonly SyntaxTree _syntaxTree;

        public CSharpTypeIdentifier(SyntaxTree syntaxTree, SemanticModel semanticModel, HashSet<string> usedTypes)
        {
            _syntaxTree = syntaxTree;
            _semanticModel = semanticModel;
            _usedTypes = usedTypes;
        }

        public void FindUsedTypes(CancellationToken ct)
        {
            var root = _syntaxTree.GetRoot(ct);
            Visit(root);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            ISymbol symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            TryAdd(symbol);

            ITypeSymbol returnTypeSymbol = (symbol as IPropertySymbol)?.Type ?? (symbol as IMethodSymbol)?.ReturnType;
            TryAdd(returnTypeSymbol);

            foreach (ITypeSymbol genericArgumentType in (returnTypeSymbol as INamedTypeSymbol)?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty)
            {
                TryAdd(genericArgumentType);
            }

            base.DefaultVisit(node);
        }

        private void TryAdd(ISymbol symbol)
        {
            var symbolAssembly = symbol?.ContainingAssembly?.Identity;
            if (symbolAssembly != null)
            {
                var @namespace = symbol.ContainingNamespace;
                if (@namespace is not null)
                {
                    if (@namespace.ToString().Equals("<global namespace>"))
                        return;

                    _usedTypes.Add($"{@namespace} | {symbolAssembly}");
                    if (symbol is INamedTypeSymbol typeSymbol)
                    {
                        foreach (var @interface in typeSymbol.AllInterfaces)
                        {
                            TryAdd(@interface);
                        }
                    }
                }
            }
        }
    }

    private sealed class VisualBasicTypeIdentifier : VisualBasicSyntaxWalker, ITypeIdentifier
    {
        private readonly HashSet<string> _usedTypes;
        private readonly SemanticModel _semanticModel;
        private readonly SyntaxTree _syntaxTree;

        public VisualBasicTypeIdentifier(SyntaxTree syntaxTree, SemanticModel semanticModel, HashSet<string> usedTypes)
        {
            _syntaxTree = syntaxTree;
            _semanticModel = semanticModel;
            _usedTypes = usedTypes;
        }

        public void FindUsedTypes(CancellationToken ct)
        {
            var root = _syntaxTree.GetRoot(ct);
            Visit(root);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            ISymbol symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            TryAdd(symbol);

            ITypeSymbol returnTypeSymbol = (symbol as IPropertySymbol)?.Type ?? (symbol as IMethodSymbol)?.ReturnType;
            TryAdd(returnTypeSymbol);

            foreach (ITypeSymbol genericArgumentType in (returnTypeSymbol as INamedTypeSymbol)?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty)
            {
                TryAdd(genericArgumentType);
            }

            base.DefaultVisit(node);
        }

        private void TryAdd(ISymbol symbol)
        {
            var symbolAssembly = symbol?.ContainingAssembly?.Identity;
            if (symbolAssembly != null)
            {
                var @namespace = symbol.ContainingNamespace;
                if (@namespace is not null)
                {
                    if (@namespace.ToString().Equals("Global"))
                        return;

                    _usedTypes.Add($"{@namespace} | {symbolAssembly}");
                    if (symbol is INamedTypeSymbol typeSymbol)
                    {
                        foreach (var @interface in typeSymbol.AllInterfaces)
                        {
                            TryAdd(@interface);
                        }
                    }
                }
            }
        }
    }
}
