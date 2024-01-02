// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Activities;

namespace Microsoft.CSharp.Activities;

internal class CSharpHelper : JitCompilerHelper<CSharpHelper>
{
    public CSharpHelper(string expressionText, HashSet<AssemblyName> refAssemNames,
        HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }

    protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => 
        new CSharpJitCompiler(references);

    internal const string Language = "C#";
}

internal class CSharpExpressionFactory<T> : ExpressionFactory
{
    public override Activity CreateReference(string expressionText) => new CSharpReference<T>(expressionText);

    public override Activity CreateValue(string expressionText) => new CSharpValue<T>(expressionText);
}

internal class CSharpDesignerHelperImpl : DesignerHelperImpl
{
    public override Type ExpressionFactoryType => typeof(CSharpExpressionFactory<>);
    public override string Language => CSharpHelper.Language;

    public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyName> references,
        HashSet<string> namespaces)
    {
        return new CSharpHelper(expressionText, references, namespaces);
    }

    protected override ExpressionCompiler Compiler => new CSharpExpressionCompiler();
}

public static class CSharpDesignerHelper
{
    private static readonly DesignerHelperImpl s_impl = new CSharpDesignerHelperImpl();

    public static Activity RecompileValue(ActivityWithResult rValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
        => s_impl.RecompileValue(rValue, out returnType, out compileError, out vbSettings);

    public static Task<CompiledExpressionResult> RecompileValueAsync(ActivityWithResult rValue)
        => s_impl.RecompileValueAsync(rValue);

    public static Activity RecompileReference(ActivityWithResult lValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileReference(lValue, out returnType, out compileError, out vbSettings);
    }

    public static Task<CompiledExpressionResult> RecompileReferenceAsync(ActivityWithResult rValue)
        => s_impl.RecompileReferenceAsync(rValue);

    public static Activity CreatePrecompiledValue(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledValue(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

    public static Activity CreatePrecompiledReference(Type targetType, string expressionText,
        IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        return s_impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies,
            environment, out returnType, out compileError, out vbSettings);
    }

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

    public static Task<CompiledExpressionResult> CreatePrecompiledValueAsync(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> assemblies, LocationReferenceEnvironment environment)
        => s_impl.CreatePrecompiledValueAsync(targetType, expressionText, namespaces, assemblies, environment);

    public static Task<CompiledExpressionResult> CreatePrecompiledReferenceAsync(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> assemblies, LocationReferenceEnvironment environment)
        => s_impl.CreatePrecompiledReferenceAsync(targetType, expressionText, namespaces, assemblies, environment);
}
