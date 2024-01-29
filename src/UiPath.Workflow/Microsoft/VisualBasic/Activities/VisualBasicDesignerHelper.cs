// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VisualBasic.Activities;

internal class VisualBasicHelper : JitCompilerHelper<VisualBasicHelper>
{
    public VisualBasicHelper(string expressionText, HashSet<AssemblyReference> assemblyReferences,
        HashSet<string> namespaceImportsNames) : base(expressionText, assemblyReferences, namespaceImportsNames) { }

    private VisualBasicHelper(string expressionText) : base(expressionText) { }

    internal const string Language = "VB";

    protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references)
    {
        return VisualBasicSettings.CreateCompiler(references);
    }

    protected override void OnCompilerCacheCreated(Dictionary<HashSet<Assembly>, HostedCompilerWrapper> compilerCache)
    {
        VisualBasicSettings.Default.CompilerChanged += delegate
        {
            lock (compilerCache)
            {
                compilerCache.Clear();
            }

            ClearRawTreeCache();
        };

        base.OnCompilerCacheCreated(compilerCache);
    }

    protected override void Initialize(HashSet<AssemblyReference> assemblyReferences, HashSet<string> namespaceImportsNames)
    {
        namespaceImportsNames.Add("Microsoft.VisualBasic");
        base.Initialize(assemblyReferences, namespaceImportsNames);
    }

    public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText,
        CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
    {
        GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity, false, out var localNamespaces,
            out var localAssemblies);
        var helper = new VisualBasicHelper(expressionText);
        var localReferenceAssemblies = new HashSet<AssemblyReference>();
        var localImports = new HashSet<string>(localNamespaces);
        foreach (var assemblyReference in localAssemblies)
        {
            if (assemblyReference.Assembly != null)
            {
                // directly add the Assembly to the list
                // so that we don't have to go through 
                // the assembly resolution process
                helper.ReferencedAssemblies ??= new HashSet<Assembly>();
                helper.ReferencedAssemblies.Add(assemblyReference.Assembly);
            }
            else if (assemblyReference.AssemblyName != null)
            {
                localReferenceAssemblies.Add(assemblyReference);
            }
        }

        helper.Initialize(localReferenceAssemblies, localImports);
        return helper.Compile<T>(publicAccessor, isLocationExpression);
    }
}

internal class VisualBasicDesignerHelperImpl : DesignerHelperImpl
{
    public override Type ExpressionFactoryType => typeof(VisualBasicExpressionFactory<>);
    public override string Language => VisualBasicHelper.Language;

    protected override ExpressionCompiler Compiler { get; } = new VisualBasicExpressionCompiler();

    public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyReference> references, HashSet<string> namespaces)
        => new VisualBasicHelper(expressionText, references, namespaces);
}

public static class VisualBasicDesignerHelper
{
    private static readonly DesignerHelperImpl s_impl = new VisualBasicDesignerHelperImpl();

    // Recompile the VBValue passed in, with its current LocationReferenceEnvironment context
    // in a weakly-typed manner (the argument VBValue's type argument is ignored)
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Design has been approved")]
    public static Activity RecompileVisualBasicValue(ActivityWithResult visualBasicValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileValue(visualBasicValue, out returnType, out compileError, out vbSettings);
    }

    public static Task<CompiledExpressionResult> RecompileValueAsync(ActivityWithResult rValue)
        => s_impl.RecompileValueAsync(rValue);

    // Recompile the VBReference passed in, with its current LocationReferenceEnvironment context
    // in a weakly-typed manner (the argument VBReference's type argument is ignored)
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Design has been approved")]
    public static Activity RecompileVisualBasicReference(ActivityWithResult visualBasicReference, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileReference(visualBasicReference, out returnType, out compileError, out vbSettings);
    }

    public static Task<CompiledExpressionResult> RecompileReferenceAsync(ActivityWithResult rValue)
        => s_impl.RecompileReferenceAsync(rValue);

    // create a pre-compiled VBValueExpression, and also provides expression type back to the caller.
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Design has been approved")]
    public static Activity CreatePrecompiledVisualBasicValue(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledValue(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledVisualBasicValue(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledValue(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Task<CompiledExpressionResult> CreatePrecompiledValueAsync(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<AssemblyReference> assemblies, LocationReferenceEnvironment environment)
    => s_impl.CreatePrecompiledValueAsync(targetType, expressionText, namespaces, assemblies, environment);

    public static Activity CreatePrecompiledVisualBasicReference(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledVisualBasicReference(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Task<CompiledExpressionResult> CreatePrecompiledReferenceAsync(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<AssemblyReference> assemblies, LocationReferenceEnvironment environment)
        => s_impl.CreatePrecompiledReferenceAsync(targetType, expressionText, namespaces, assemblies, environment);

    public static Activity CreatePrecompiledValue(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledValue(targetType, expressionText, parent, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledValue(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError)
    {
        return CreatePrecompiledValue(targetType, expressionText, parent, out returnType, out compileError, out _);
    }

    public static Activity CreatePrecompiledReference(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, parent, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledReference(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError)
    {
        return CreatePrecompiledReference(targetType, expressionText, parent, out returnType, out compileError, out _);
    }
}

internal class VisualBasicExpressionFactory<T> : ExpressionFactory
{
    public override Activity CreateReference(string expressionText)
    {
        return new VisualBasicReference<T>(expressionText);
    }

    public override Activity CreateValue(string expressionText)
    {
        return new VisualBasicValue<T>(expressionText);
    }
}
