﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualBasic.Activities;

namespace Microsoft.CSharp.Activities;

internal class CSharpHelper : JitCompilerHelper<CSharpHelper>
{
    public CSharpHelper(string expressionText, HashSet<AssemblyName> refAssemNames,
        HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }

    private CSharpHelper(string expressionText) : base(expressionText) { }

    protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => 
        new CSharpJitCompiler(references);

    public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText,
        CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
    {
        GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity, false, out var localNamespaces,
            out var localAssemblies);
        var helper = new CSharpHelper(expressionText);
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

internal class CSharpExpressionFactory<T> : ExpressionFactory
{
    public override Activity CreateReference(string expressionText) => new CSharpReference<T>(expressionText);

    public override Activity CreateValue(string expressionText) => new CSharpValue<T>(expressionText);
}

internal class CSharpDesignerHelperImpl : DesignerHelperImpl
{
    public override Type ExpressionFactoryType => typeof(CSharpExpressionFactory<>);
    public override string Language => "C#";

    public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyName> references,
        HashSet<string> namespaces)
    {
        return new CSharpHelper(expressionText, references, namespaces);
    }
}

public static class CSharpDesignerHelper
{
    private static readonly DesignerHelperImpl s_impl = new CSharpDesignerHelperImpl();

    public static Activity RecompileValue(ActivityWithResult rValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileValue(rValue, out returnType, out compileError, out vbSettings);
    }

    public static Activity RecompileReference(ActivityWithResult lValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        return s_impl.RecompileReference(lValue, out returnType, out compileError, out vbSettings);
    }

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
