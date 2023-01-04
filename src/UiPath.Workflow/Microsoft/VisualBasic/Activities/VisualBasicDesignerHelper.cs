// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.VisualBasic.Activities;

using CompilerFactory = Func<HashSet<Assembly>, JustInTimeCompiler>;

internal class VisualBasicHelper : JitCompilerHelper<VisualBasicHelper>
{
    public VisualBasicHelper(string expressionText, HashSet<AssemblyName> refAssemNames,
        HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }

    private VisualBasicHelper(string expressionText) : base(expressionText) { }

    internal static string Language => "VB";

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
        };
    }

    protected override void Initialize(HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames)
    {
        AddDefaultNamespaces(namespaceImportsNames);
        base.Initialize(refAssemNames, namespaceImportsNames);
    }

    private static void AddDefaultNamespaces(HashSet<string> namespaceImportsNames)
    {
        namespaceImportsNames.Add("System");
        namespaceImportsNames.Add("System.Linq.Expressions");
        namespaceImportsNames.Add("Microsoft.VisualBasic");
    }

    public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText,
        CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
    {
        GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity, false, out var localNamespaces,
            out var localAssemblies);
        var helper = new VisualBasicHelper(expressionText);
        var localReferenceAssemblies = new HashSet<AssemblyName>();
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
                localReferenceAssemblies.Add(assemblyReference.AssemblyName);
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

    public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyName> references,
        HashSet<string> namespaces)
    {
        return new VisualBasicHelper(expressionText, references, namespaces);
    }
}

public static class VisualBasicDesignerHelper
{
    private static readonly DesignerHelperImpl s_impl = new VisualBasicDesignerHelperImpl();

    // Returns the additional constraint for visual basic which enforces variable name shadowing for 
    // projects targeting 4.0 for backward compatibility. 
    public static Constraint NameShadowingConstraint { get; } = new VisualBasicNameShadowingConstraint();

    // Recompile the VBValue passed in, with its current LocationReferenceEnvironment context
    // in a weakly-typed manner (the argument VBValue's type argument is ignored)
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Design has been approved")]
    public static Activity RecompileVisualBasicValue(ActivityWithResult visualBasicValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileValue(visualBasicValue, out returnType, out compileError, out vbSettings);
    }

    // Recompile the VBReference passed in, with its current LocationReferenceEnvironment context
    // in a weakly-typed manner (the argument VBReference's type argument is ignored)
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
    //    Justification = "Design has been approved")]
    public static Activity RecompileVisualBasicReference(ActivityWithResult visualBasicReference, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileReference(visualBasicReference, out returnType, out compileError, out vbSettings);
    }

    // create a pre-compiled VBValueExpression, and also provides expressin type back to the caller.
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

    public static Activity CreatePrecompiledVisualBasicReference(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledValue(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError)
    {
        return s_impl.CreatePrecompiledValue(targetType, expressionText, parent, out returnType, out compileError);
    }

    public static Activity CreatePrecompiledReference(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, parent, out returnType,
            out compileError);
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
