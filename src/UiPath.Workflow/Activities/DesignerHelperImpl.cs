// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.VisualBasic.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Activities;

internal abstract class ExpressionFactory
{
    public abstract Activity CreateValue(string expressionText);
    public abstract Activity CreateReference(string expressionText);
}

internal abstract class DesignerHelperImpl
{
    private static readonly ImmutableArray<DiagnosticAnalyzer> _usedTypesAnalizerList = new List<DiagnosticAnalyzer> { new UsedTypesAnalyzer() }.ToImmutableArray();
    private const string _usedTypesDiagnosticId = "UT_001";

    public abstract Type ExpressionFactoryType { get; }
    public abstract string Language { get; }

    protected abstract ExpressionCompiler Compiler { get; }

    public abstract JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyReference> references, HashSet<string> namespaces);

    public Activity CreatePrecompiledValue(Type targetType,
        string expressionText,
        IEnumerable<string> namespaces,
        IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment,
        out Type returnType,
        out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    => CreatePrecompiledValue(targetType, expressionText, namespaces, CreateReferences(referencedAssemblies), environment, out returnType, out compileError, out vbSettings);

    public Activity CreatePrecompiledValue(Type targetType,
        string expressionText,
        IEnumerable<string> namespaces,
        IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment,
        out Type returnType,
        out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        LambdaExpression lambda = null;
        var namespacesSet = new HashSet<string>(namespaces ?? Enumerable.Empty<string>());
        var assembliesSet = new HashSet<AssemblyReference>(referencedAssemblies ?? Enumerable.Empty<AssemblyReference>());
        namespacesSet.Remove(null);
        assembliesSet.Remove(null);
        compileError = null;
        returnType = null;

        var compilerHelper = CreateJitCompilerHelper(expressionText, assembliesSet, namespacesSet);
        if (targetType == null)
        {
            try
            {
                lambda = compilerHelper.CompileNonGeneric(environment);
                if (lambda != null)
                {
                    returnType = lambda.ReturnType;
                }
            }
            catch (SourceExpressionException e)
            {
                compileError = e;
                returnType = typeof(object);
            }

            targetType = returnType;
        }
        else
        {
            var genericCompileMethod = compilerHelper.GetType()
                                                     .GetMethod("Compile",
                                                         new[] { typeof(LocationReferenceEnvironment) });
            genericCompileMethod = genericCompileMethod.MakeGenericMethod(targetType);
            try
            {
                lambda = (LambdaExpression)genericCompileMethod.Invoke(compilerHelper, new object[] { environment });
                returnType = targetType;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is SourceExpressionException se)
                {
                    compileError = se;
                    returnType = typeof(object);
                }
                else
                {
                    throw FxTrace.Exception.AsError(e.InnerException);
                }
            }
        }

        vbSettings = new VisualBasicSettings();
        if (lambda != null)
        {
            var typeReferences = new HashSet<Type>();
            FindTypeReferences(lambda.Body, typeReferences);
            foreach (var type in typeReferences)
            {
                var tassembly = type.Assembly;
                if (tassembly.IsDynamic)
                {
                    continue;
                }

                var assemblyName = AssemblyReference.GetFastAssemblyName(tassembly).Name;
                var importReference = new VisualBasicImportReference { Assembly = assemblyName, Import = type.Namespace };
                vbSettings.ImportReferences.Add(importReference);
            }
        }

        var concreteHelperType = ExpressionFactoryType.MakeGenericType(targetType);
        var expressionFactory = (ExpressionFactory)Activator.CreateInstance(concreteHelperType);

        return expressionFactory.CreateValue(expressionText);
    }

    public Task<CompiledExpressionResult> CreatePrecompiledValueAsync(Type targetType, string expressionText, IEnumerable<string> namespaces,
        IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment)
        => CreatePrecompiledExpressionAsync(targetType, expressionText, false, namespaces, referencedAssemblies, environment);

    public Activity RecompileValue(ActivityWithResult rValue, out Type returnType,
        out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        if (rValue is not ITextExpression textExpression || textExpression.Language != Language)
        {
            throw FxTrace.Exception.AsError(new ArgumentException());
        }

        var expressionText = textExpression.ExpressionText;
        var environment = rValue.GetParentEnvironment();

        JitCompilerHelper.GetAllImportReferences(rValue, true, out var namespaces, out var assemblies);

        return CreatePrecompiledValue(
            null,
            expressionText,
            namespaces,
            assemblies,
            environment,
            out returnType,
            out compileError,
            out vbSettings);
    }

    public Task<CompiledExpressionResult> RecompileValueAsync(ActivityWithResult rValue)
    {
        if (rValue is not ITextExpression textExpression || textExpression.Language != Language)
        {
            throw FxTrace.Exception.AsError(new ArgumentException());
        }

        var expressionText = textExpression.ExpressionText;
        var environment = rValue.GetParentEnvironment();

        JitCompilerHelper.GetAllImportReferences(rValue, true, out var namespaces, out var assemblies);

        return CreatePrecompiledValueAsync(
            null,
            expressionText,
            namespaces,
            assemblies,
            environment);
    }

    public Activity CreatePrecompiledReference(Type targetType,
        string expressionText,
        IEnumerable<string> namespaces,
        IEnumerable<string> referencedAssemblies,
        LocationReferenceEnvironment environment,
        out Type returnType,
        out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    => CreatePrecompiledReference(targetType, expressionText, namespaces, CreateReferences(referencedAssemblies), environment, out returnType, out compileError, out vbSettings);

    public Activity CreatePrecompiledReference(Type targetType,
        string expressionText,
        IEnumerable<string> namespaces,
        IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment,
        out Type returnType,
        out SourceExpressionException compileError,
        out VisualBasicSettings vbSettings)
    {
        LambdaExpression lambda = null;
        var namespacesSet = new HashSet<string>(namespaces ?? Enumerable.Empty<string>());
        var assembliesSet = new HashSet<AssemblyReference>(referencedAssemblies ?? Enumerable.Empty<AssemblyReference>());
        namespacesSet.Remove(null);
        assembliesSet.Remove(null);
        compileError = null;
        returnType = null;

        var compilerHelper = CreateJitCompilerHelper(expressionText, assembliesSet, namespacesSet);
        if (targetType == null)
        {
            try
            {
                lambda = compilerHelper.CompileNonGeneric(environment);
                if (lambda != null)
                {
                    // inspect the expressionTree to see if it is a valid location expression(L-value)
                    if (!ExpressionUtilities.IsLocation(lambda, targetType, out var extraErrorMessage))
                    {
                        var errorMessage = SR.InvalidLValueExpression;
                        if (extraErrorMessage != null)
                        {
                            errorMessage += ":" + extraErrorMessage;
                        }

                        throw FxTrace.Exception.AsError(
                            new SourceExpressionException(
                                SR.CompilerErrorSpecificExpression(expressionText, errorMessage)));
                    }

                    returnType = lambda.ReturnType;
                }
            }
            catch (SourceExpressionException e)
            {
                compileError = e;
                returnType = typeof(object);
            }

            targetType = returnType;
        }
        else
        {
            var genericCompileMethod = compilerHelper.GetType()
                                                     .GetMethod("Compile",
                                                          new[] { typeof(LocationReferenceEnvironment) });
            genericCompileMethod = genericCompileMethod.MakeGenericMethod(targetType);
            try
            {
                lambda = (LambdaExpression)genericCompileMethod.Invoke(compilerHelper, new object[] { environment });
                // inspect the expressionTree to see if it is a valid location expression(L-value)
                if (!ExpressionUtilities.IsLocation(lambda, targetType, out var extraErrorMessage))
                {
                    var errorMessage = SR.InvalidLValueExpression;
                    if (extraErrorMessage != null)
                    {
                        errorMessage += ":" + extraErrorMessage;
                    }

                    throw FxTrace.Exception.AsError(
                        new SourceExpressionException(
                            SR.CompilerErrorSpecificExpression(expressionText, errorMessage)));
                }

                returnType = targetType;
            }
            catch (SourceExpressionException e)
            {
                compileError = e;
                returnType = typeof(object);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is SourceExpressionException se)
                {
                    compileError = se;
                    returnType = typeof(object);
                }
                else
                {
                    throw FxTrace.Exception.AsError(e.InnerException);
                }
            }
        }

        vbSettings = new VisualBasicSettings();
        if (lambda != null)
        {
            var typeReferences = new HashSet<Type>();
            FindTypeReferences(lambda.Body, typeReferences);
            foreach (var type in typeReferences)
            {
                var tassembly = type.Assembly;
                if (tassembly.IsDynamic)
                {
                    continue;
                }

                var assemblyName = AssemblyReference.GetFastAssemblyName(tassembly).Name;
                var importReference = new VisualBasicImportReference { Assembly = assemblyName, Import = type.Namespace };
                vbSettings.ImportReferences.Add(importReference);
            }
        }

        var concreteHelperType = ExpressionFactoryType.MakeGenericType(targetType);
        var expressionFactory = (ExpressionFactory)Activator.CreateInstance(concreteHelperType);

        return expressionFactory.CreateReference(expressionText);
    }

    public Task<CompiledExpressionResult> CreatePrecompiledReferenceAsync(Type targetType, string expressionText, IEnumerable<string> namespaces,
        IEnumerable<AssemblyReference> referencedAssemblies, LocationReferenceEnvironment environment)
        => CreatePrecompiledExpressionAsync(targetType, expressionText, true, namespaces, referencedAssemblies, environment);

    public Activity RecompileReference(ActivityWithResult lValue, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        if (lValue is not ITextExpression textExpression || textExpression.Language != Language)
        {
            throw FxTrace.Exception.AsError(new ArgumentException());
        }

        var expressionText = textExpression.ExpressionText;
        var environment = lValue.GetParentEnvironment();

        JitCompilerHelper.GetAllImportReferences(lValue, true, out var namespaces, out var assemblies);

        return CreatePrecompiledReference(
            null,
            expressionText,
            namespaces,
            assemblies,
            environment,
            out returnType,
            out compileError,
            out vbSettings);
    }

    public Task<CompiledExpressionResult> RecompileReferenceAsync(ActivityWithResult lValue)
    {
        if (lValue is not ITextExpression textExpression || textExpression.Language != Language)
        {
            throw FxTrace.Exception.AsError(new ArgumentException());
        }

        var expressionText = textExpression.ExpressionText;
        var environment = lValue.GetParentEnvironment();

        JitCompilerHelper.GetAllImportReferences(lValue, true, out var namespaces, out var assemblies);

        return CreatePrecompiledReferenceAsync(
            null,
            expressionText,
            namespaces,
            assemblies,
            environment);
    }

    internal Activity CreatePrecompiledReference(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        JitCompilerHelper.GetAllImportReferences(parent, true, out var namespaces, out var assemblies);
        return CreatePrecompiledReference(targetType, expressionText, namespaces, assemblies, parent.PublicEnvironment,
            out returnType, out compileError, out vbSettings);
    }

    internal Activity CreatePrecompiledValue(Type targetType, string expressionText, Activity parent,
        out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings)
    {
        JitCompilerHelper.GetAllImportReferences(parent, true, out var namespaces, out var assemblies);
        return CreatePrecompiledValue(targetType, expressionText, namespaces, assemblies, parent.PublicEnvironment,
            out returnType, out compileError, out vbSettings);
    }

    private async Task<CompiledExpressionResult> CreatePrecompiledExpressionAsync(Type targetType, string expressionText, bool isLocation, IEnumerable<string> namespaces,
        IEnumerable<AssemblyReference> referencedAssemblies,
        LocationReferenceEnvironment environment)
    {
        SourceExpressionException compileError = null;
        var returnType = typeof(object);
        var vbSettings = new VisualBasicSettings();
        namespaces ??= Array.Empty<string>();
        referencedAssemblies ??= Array.Empty<AssemblyReference>();

        // execute compiler
        var compilation = Compiler.Compile(expressionText, isLocation, targetType ?? typeof(object), namespaces.ToList(), referencedAssemblies.ToList(), environment);
        var diagnostics = await compilation.WithAnalyzers(_usedTypesAnalizerList).GetAllDiagnosticsAsync();

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            compileError = GetErrorsFromDiagnostics(expressionText, diagnostics);
        }

        else
        {
            returnType = Compiler.GetReturnType(compilation);
            PopulateVbSettings(vbSettings, diagnostics);
        }

        targetType ??= returnType;

        var concreteHelperType = ExpressionFactoryType.MakeGenericType(targetType);
        var expressionFactory = (ExpressionFactory)Activator.CreateInstance(concreteHelperType);

        var activity = isLocation ? expressionFactory.CreateReference(expressionText) : expressionFactory.CreateValue(expressionText);
        return new CompiledExpressionResult(activity, returnType, compileError, vbSettings);
    }

    private static void EnsureTypeReferenced(Type type, bool isDirectReference, HashSet<Type> typeReferences)
    {
        if (type == null)
        {
            return;
        }

        if (type.HasElementType)
        {
            EnsureTypeReferenced(type.GetElementType(), isDirectReference, typeReferences);
        }
        else
        {
            EnsureTypeReferencedRecurse(type, isDirectReference, typeReferences);
            if (type.IsGenericType)
            {
                var typeArgs = type.GetGenericArguments();
                for (var i = 1; i < typeArgs.Length; ++i)
                {
                    EnsureTypeReferencedRecurse(typeArgs[i], isDirectReference, typeReferences);
                }
            }
        }
    }

    private static void EnsureTypeReferencedRecurse(Type type, bool isDirectReference, HashSet<Type> typeReferences)
    {
        if (typeReferences.Contains(type))
        {
            return;
        }

        // don't add base types/interfaces if they're in the default set (or we'll get superfluous xmlns references)
        if (isDirectReference || !JitCompilerHelper.DefaultReferencedAssemblies.Contains(type.Assembly))
        {
            typeReferences.Add(type);
        }

        // make sure any interfaces needed by this type are referenced
        var interfaces = type.GetInterfaces();
        foreach (var t in interfaces)
        {
            EnsureTypeReferencedRecurse(t, false, typeReferences);
        }

        // same for base types
        var baseType = type.BaseType;
        while (baseType != null && baseType != TypeHelper.ObjectType)
        {
            EnsureTypeReferencedRecurse(baseType, false, typeReferences);
            baseType = baseType.BaseType;
        }
    }

    private static void FindTypeReferences(Expression expression, HashSet<Type> typeReferences)
    {
        if (expression == null)
        {
            return;
        }

        switch (expression.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                var binaryExpression = (BinaryExpression)expression;
                FindTypeReferences(binaryExpression.Left, typeReferences);
                FindTypeReferences(binaryExpression.Right, typeReferences);
                return;

            case ExpressionType.Conditional:
                var conditional = (ConditionalExpression)expression;
                FindTypeReferences(conditional.Test, typeReferences);
                FindTypeReferences(conditional.IfTrue, typeReferences);
                FindTypeReferences(conditional.IfFalse, typeReferences);
                return;

            case ExpressionType.Constant:
                var constantExpr = (ConstantExpression)expression;
                if (constantExpr.Value is Type)
                {
                    EnsureTypeReferenced((Type)constantExpr.Value, true, typeReferences);
                }
                else if (constantExpr.Value != null)
                {
                    EnsureTypeReferenced(constantExpr.Value.GetType(), true, typeReferences);
                }

                return;

            case ExpressionType.Invoke:
                var invocation = (InvocationExpression)expression;
                FindTypeReferences(invocation.Expression, typeReferences);
                for (var i = 0; i < invocation.Arguments.Count; i++)
                {
                    FindTypeReferences(invocation.Arguments[i], typeReferences);
                }

                return;

            case ExpressionType.Lambda:
                var lambda = (LambdaExpression)expression;
                FindTypeReferences(lambda.Body, typeReferences);
                for (var i = 0; i < lambda.Parameters.Count; i++)
                {
                    FindTypeReferences(lambda.Parameters[i], typeReferences);
                }

                return;

            case ExpressionType.ListInit:
                var listInit = (ListInitExpression)expression;
                FindTypeReferences(listInit.NewExpression, typeReferences);
                for (var i = 0; i < listInit.Initializers.Count; i++)
                {
                    var arguments = listInit.Initializers[i].Arguments;
                    for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                    {
                        FindTypeReferences(arguments[argumentIndex], typeReferences);
                    }
                }

                return;

            case ExpressionType.Parameter:
                var paramExpr = (ParameterExpression)expression;
                EnsureTypeReferenced(paramExpr.Type, false, typeReferences);
                return;

            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)expression;
                if (memberExpression.Expression == null)
                {
                    EnsureTypeReferenced(memberExpression.Member.DeclaringType, true, typeReferences);
                }
                else
                {
                    FindTypeReferences(memberExpression.Expression, typeReferences);
                }

                EnsureTypeReferenced(memberExpression.Type, false, typeReferences);
                return;

            case ExpressionType.MemberInit:
                var memberInit = (MemberInitExpression)expression;
                FindTypeReferences(memberInit.NewExpression, typeReferences);
                var bindings = memberInit.Bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    FindTypeReferences(bindings[i], typeReferences);
                }

                return;

            case ExpressionType.ArrayIndex:
                // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                if (expression is MethodCallExpression arrayIndex)
                {
                    FindTypeReferences(arrayIndex.Object, typeReferences);
                    var arguments = arrayIndex.Arguments;
                    for (var i = 0; i < arguments.Count; i++)
                    {
                        FindTypeReferences(arguments[i], typeReferences);
                    }

                    return;
                }

                var alternateIndex = (BinaryExpression)expression;
                FindTypeReferences(alternateIndex.Left, typeReferences);
                FindTypeReferences(alternateIndex.Right, typeReferences);
                return;

            case ExpressionType.Call:
                var methodCall = (MethodCallExpression)expression;
                var method = methodCall.Method;
                EnsureTypeReferenced(methodCall.Type, false, typeReferences);
                if (methodCall.Object != null)
                {
                    FindTypeReferences(methodCall.Object, typeReferences);
                }
                else
                {
                    EnsureTypeReferenced(method.DeclaringType, true, typeReferences);
                }

                if (method.IsGenericMethod && !method.IsGenericMethodDefinition && !method.ContainsGenericParameters)
                {
                    // closed generic method
                    var typeArgs = method.GetGenericArguments();
                    for (var i = 1; i < typeArgs.Length; ++i)
                    {
                        EnsureTypeReferenced(typeArgs[i], true, typeReferences);
                    }
                }

                var parameters = method.GetParameters();
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        EnsureTypeReferenced(parameter.ParameterType, false, typeReferences);
                    }
                }

                var callArguments = methodCall.Arguments;
                for (var i = 0; i < callArguments.Count; i++)
                {
                    FindTypeReferences(callArguments[i], typeReferences);
                }

                return;

            case ExpressionType.NewArrayInit:
                var newArray = (NewArrayExpression)expression;
                EnsureTypeReferenced(newArray.Type.GetElementType(), true, typeReferences);
                var expressions = newArray.Expressions;
                for (var i = 0; i < expressions.Count; i++)
                {
                    FindTypeReferences(expressions[i], typeReferences);
                }

                return;

            case ExpressionType.NewArrayBounds:
                var newArrayBounds = (NewArrayExpression)expression;
                EnsureTypeReferenced(newArrayBounds.Type.GetElementType(), true, typeReferences);
                var boundExpressions = newArrayBounds.Expressions;
                for (var i = 0; i < boundExpressions.Count; i++)
                {
                    FindTypeReferences(boundExpressions[i], typeReferences);
                }

                return;

            case ExpressionType.New:
                var newExpression = (NewExpression)expression;
                if (newExpression.Constructor != null)
                {
                    EnsureTypeReferenced(newExpression.Constructor.DeclaringType, true, typeReferences);
                }
                else
                // if no constructors defined (e.g. structs), the simply use the type
                {
                    EnsureTypeReferenced(newExpression.Type, true, typeReferences);
                }

                var ctorArguments = newExpression.Arguments;
                for (var i = 0; i < ctorArguments.Count; i++)
                {
                    FindTypeReferences(ctorArguments[i], typeReferences);
                }

                return;

            case ExpressionType.TypeIs:
                var typeBinary = (TypeBinaryExpression)expression;
                FindTypeReferences(typeBinary.Expression, typeReferences);
                EnsureTypeReferenced(typeBinary.TypeOperand, true, typeReferences);
                return;

            case ExpressionType.TypeAs:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var unary = (UnaryExpression)expression;
                FindTypeReferences(unary.Operand, typeReferences);
                EnsureTypeReferenced(unary.Type, true, typeReferences);
                return;

            case ExpressionType.ArrayLength:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Quote:
            case ExpressionType.UnaryPlus:
                var unaryExpression = (UnaryExpression)expression;
                FindTypeReferences(unaryExpression.Operand, typeReferences);
                return;

            // Expression Tree V2.0 types.  This is due to the hosted VB compiler generating ET V2.0 nodes

            case ExpressionType.Block:
                var block = (BlockExpression)expression;
                var variables = block.Variables;
                for (var i = 0; i < variables.Count; i++)
                {
                    FindTypeReferences(variables[i], typeReferences);
                }

                var blockExpressions = block.Expressions;
                for (var i = 0; i < blockExpressions.Count; i++)
                {
                    FindTypeReferences(blockExpressions[i], typeReferences);
                }

                return;

            case ExpressionType.Assign:
                var assign = (BinaryExpression)expression;
                FindTypeReferences(assign.Left, typeReferences);
                FindTypeReferences(assign.Right, typeReferences);
                return;
        }

        Fx.Assert("Don't understand expression type " + expression.NodeType);
    }

    private static void FindTypeReferences(MemberBinding binding, HashSet<Type> typeReferences)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                var assignment = (MemberAssignment)binding;
                FindTypeReferences(assignment.Expression, typeReferences);
                return;

            case MemberBindingType.ListBinding:
                var list = (MemberListBinding)binding;
                var initializers = list.Initializers;
                for (var i = 0; i < initializers.Count; i++)
                {
                    var arguments = initializers[i].Arguments;
                    for (var j = 0; j < arguments.Count; j++)
                    {
                        FindTypeReferences(arguments[j], typeReferences);
                    }
                }

                return;

            case MemberBindingType.MemberBinding:
                var member = (MemberMemberBinding)binding;
                var bindings = member.Bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    FindTypeReferences(bindings[i], typeReferences);
                }

                return;

            default:
                Fx.Assert("MemberBinding type '" + binding.BindingType + "' is not supported.");
                return;
        }
    }


    private static SourceExpressionException GetErrorsFromDiagnostics(string expressionText, ImmutableArray<Diagnostic> diagnostics)
    {
        var errors = new TextExpressionCompilerResults();
        errors.AddMessages(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(diag =>
            new TextExpressionCompilerError
            {
                SourceLineNumber = diag.Location.GetMappedLineSpan().StartLinePosition.Line,
                Number = diag.Id,
                Message = diag.ToString(),
                IsWarning = diag.Severity < DiagnosticSeverity.Error
            }));
        return new SourceExpressionException(
            SR.CompilerErrorSpecificExpression(expressionText, errors), errors.CompilerMessages);
    }

    private static void PopulateVbSettings(VisualBasicSettings vbSettings, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics.Where(d => d.Id == _usedTypesDiagnosticId))
        {
            var diagMessages = diagnostic.GetMessage()[1..^1].Split('|');
            var import = new VisualBasicImportReference { Import = diagMessages[0].Trim(), Assembly = diagMessages[1].Trim().Split(',')[0] };
            vbSettings.ImportReferences.Add(import);
        }
    }

    private static IEnumerable<AssemblyReference> CreateReferences(IEnumerable<string> assemblyNames)
        => assemblyNames?.OfType<string>().Select(asmName => (AssemblyReference)new AssemblyName(asmName)) ?? Enumerable.Empty<AssemblyReference>();
}
