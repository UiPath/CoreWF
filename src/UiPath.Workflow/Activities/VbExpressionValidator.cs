﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Activities;

/// <summary>
/// Validates VB.NET expressions for use in fast design-time expression validation.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_default = new();
    private static VbExpressionValidator s_instance;

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions = new(kind: SourceCodeKind.Script);

    /// <summary>
    /// Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Instance
    {
        get { return s_instance ?? s_default.Value; }
        set { s_instance = value; }
    }

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    protected override Compilation CreateCompilationUnit()
    {
        string assemblyName = Guid.NewGuid().ToString();
        VisualBasicCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
            rootNamespace: "",
            optionStrict: OptionStrict.On,
            optionInfer: true,
            optionExplicit: true,
            optionCompareText: false,
            embedVbCoreRuntime: false,
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: false,
            xmlReferenceResolver: null,
            sourceReferenceResolver: SourceFileResolver.Default,
            concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        return VisualBasicCompilation.Create(assemblyName, null, MetadataReferences, options);
    }

    protected override string CreateValidationCode(string parameters, string returnType, string code) =>
         $"Function ExpressionToValidate({parameters}) As {returnType}\nReturn ({code})\nEnd Function";

    protected override string FormatParameter(string name, Type type)
    {
        string result = $"{name} As ";
        if (type.IsGenericType)
        {
            result += GetGenericTypeName(type);
        }
        else
        {
            result += type.Namespace + "." + type.Name;
        }

        return result;
    }

    private string GetGenericTypeName(Type type)
    {
        string result = type.Namespace + "." + type.Name[..type.Name.IndexOf('`')] + "(Of ";
        var genericTypeNames = new string[type.GenericTypeArguments.Length];
        for (int i = 0; i < type.GenericTypeArguments.Length; i++)
        {
            Type genericTypeArgument = type.GenericTypeArguments[i];
            genericTypeNames[i] = genericTypeArgument.IsGenericType
                ? GetGenericTypeName(genericTypeArgument)
                : genericTypeArgument.Namespace + "." + genericTypeArgument.Name;
        }

        result += string.Join(", ", genericTypeNames);
        result += ")";
        return result;
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate) => 
        VisualBasicSyntaxTree.ParseText(expressionToValidate.Code, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}
