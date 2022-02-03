// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Collections;
using System.Security;
using System.Threading;
using Microsoft.VisualBasic.Activities;
using Microsoft.VisualBasic.CompilerServices;

namespace System.Activities;

using CompilerCache = Dictionary<HashSet<Assembly>, JitCompilerHelper.HostedCompilerWrapper>;

internal abstract class JitCompilerHelper
{
    // cache for type's all base types, interfaces, generic arguments, element type
    // HopperCache is a pseudo-MRU cache
    private const int TypeReferenceCacheMaxSize = 100;

    // the following assemblies are provided to the compiler by default
    // items are public so the decompiler knows which assemblies it doesn't need to reference for interfaces
    public static readonly IReadOnlyCollection<Assembly> DefaultReferencedAssemblies = new HashSet<Assembly>
    {
        typeof(int).Assembly, // mscorlib
        typeof(CodeTypeDeclaration).Assembly, // System
        typeof(Expression).Assembly, // System.Core
        typeof(Conversions).Assembly, //Microsoft.VisualBasic.Core
        typeof(Activity).Assembly // System.Activities
    };

    private static readonly object s_typeReferenceCacheLock = new();
    private static readonly HopperCache s_typeReferenceCache = new(TypeReferenceCacheMaxSize, false);
    private static readonly FindMatch s_delegateFindLocationReferenceMatchShortcut = FindLocationReferenceMatchShortcut;
    private static readonly FindMatch s_delegateFindFirstLocationReferenceMatch = FindFirstLocationReferenceMatch;
    private static readonly FindMatch s_delegateFindAllLocationReferenceMatch = FindAllLocationReferenceMatch;

    protected LocationReferenceEnvironment Environment;

    // this is a flag to differentiate the cached short-cut Rewrite from the normal post-compilation Rewrite
    protected bool IsShortCutRewrite;
    protected IReadOnlyCollection<string> NamespaceImports;
    protected CodeActivityPublicEnvironmentAccessor? PublicAccessor;
    protected HashSet<Assembly> ReferencedAssemblies;
    public abstract LambdaExpression CompileNonGeneric(LocationReferenceEnvironment environment);
    protected abstract JustInTimeCompiler CreateCompiler(HashSet<Assembly> references);

    protected virtual void OnCompilerCacheCreated(CompilerCache compilerCache) { }

    protected void Initialize(HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames)
    {
        namespaceImportsNames.Add("System");
        namespaceImportsNames.Add("System.Linq.Expressions");
        namespaceImportsNames.Remove("");
        namespaceImportsNames.Remove(null);
        NamespaceImports = namespaceImportsNames;

        foreach (var assemblyName in refAssemNames)
        {
            ReferencedAssemblies ??= new HashSet<Assembly>();

            try
            {
                var loaded = AssemblyReference.GetAssembly(assemblyName);
                if (loaded != null)
                {
                    ReferencedAssemblies.Add(loaded);
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                FxTrace.Exception.TraceUnhandledException(e);
            }
        }
    }

    public static void GetAllImportReferences(Activity activity, bool isDesignTime, out List<string> namespaces,
        out List<AssemblyReference> assemblies)
    {
        var namespaceList = new List<string>();
        var assemblyList = new List<AssemblyReference>();

        // Start with the defaults; any settings on the Activity will be added to these
        // The default settings are mutable, so we need to re-copy this list on every call
        ExtractNamespacesAndReferences(VisualBasicSettings.Default, namespaceList, assemblyList);

        var environment = activity.GetParentEnvironment();
        if (environment?.Root == null)
        {
            namespaces = namespaceList;
            assemblies = assemblyList;
            return;
        }

        var rootVbSettings = VisualBasic.GetSettings(environment.Root);
        if (rootVbSettings != null)
        {
            // We have VBSettings
            ExtractNamespacesAndReferences(rootVbSettings, namespaceList, assemblyList);
        }
        else
        {
            // Use TextExpression settings
            IList<string> rootNamespaces;
            IList<AssemblyReference> rootAssemblies;
            if (isDesignTime)
            {
                // When called via VisualBasicDesignerHelper, we don't know whether or not 
                // we're in an implementation, so check both.
                rootNamespaces = TextExpression.GetNamespacesForImplementation(environment.Root);
                rootAssemblies = TextExpression.GetReferencesForImplementation(environment.Root);
                if (rootNamespaces.Count == 0 && rootAssemblies.Count == 0)
                {
                    rootNamespaces = TextExpression.GetNamespaces(environment.Root);
                    rootAssemblies = TextExpression.GetReferences(environment.Root);
                }
            }
            else
            {
                rootNamespaces = TextExpression.GetNamespacesInScope(activity);
                rootAssemblies = TextExpression.GetReferencesInScope(activity);
            }

            namespaceList.AddRange(rootNamespaces);
            assemblyList.AddRange(rootAssemblies);
        }

        namespaces = namespaceList;
        assemblies = assemblyList;
    }

    private static void ExtractNamespacesAndReferences(VisualBasicSettings vbSettings,
        ICollection<string> namespaces, ICollection<AssemblyReference> assemblies)
    {
        foreach (var importReference in vbSettings.ImportReferences)
        {
            namespaces.Add(importReference.Import);
            assemblies.Add(new AssemblyReference
            {
                Assembly = importReference.EarlyBoundAssembly,
                AssemblyName = importReference.AssemblyName
            });
        }
    }

    private static bool FindLocationReferenceMatchShortcut(LocationReference reference, string targetName,
        Type targetType, out bool terminateSearch)
    {
        terminateSearch = false;
        if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
        {
            if (targetType != reference.Type)
            {
                terminateSearch = true;
                return false;
            }

            return true;
        }

        return false;
    }

    private static bool FindFirstLocationReferenceMatch(LocationReference reference, string targetName, Type targetType,
        out bool terminateSearch)
    {
        terminateSearch = false;
        if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
        {
            terminateSearch = true;
            return true;
        }

        return false;
    }

    private static bool FindAllLocationReferenceMatch(LocationReference reference, string targetName, Type targetType,
        out bool terminateSearch)
    {
        terminateSearch = false;
        if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }


    // Returning null indicates the cached LambdaExpression used here doesn't coincide with current LocationReferenceEnvironment.
    // Null return value causes the process to rewind and start from HostedCompiler.CompileExpression().
    protected Expression Rewrite(Expression expression, ReadOnlyCollection<ParameterExpression> lambdaParameters,
        out bool abort)
    {
        return Rewrite(expression, lambdaParameters, false, out abort);
    }

    protected Expression Rewrite(Expression expression, ReadOnlyCollection<ParameterExpression> lambdaParameters,
        bool isLocationExpression, out bool abort)
    {
        int i;
        int j;
        Expression expr1;
        Expression expr2;
        Expression expr3;
        List<Expression> arguments;
        NewExpression newExpression;
        ReadOnlyCollection<Expression> tmpArguments;

        abort = false;
        if (expression == null)
        {
            return null;
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

                var binaryExpression = (BinaryExpression) expression;
                expr1 = Rewrite(binaryExpression.Left, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                expr2 = Rewrite(binaryExpression.Right, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                var conversion = (LambdaExpression) Rewrite(binaryExpression.Conversion, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.MakeBinary(
                    binaryExpression.NodeType,
                    expr1,
                    expr2,
                    binaryExpression.IsLiftedToNull,
                    binaryExpression.Method,
                    conversion);

            case ExpressionType.Conditional:

                var conditional = (ConditionalExpression) expression;
                expr1 = Rewrite(conditional.Test, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                expr2 = Rewrite(conditional.IfTrue, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                expr3 = Rewrite(conditional.IfFalse, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.Condition(expr1, expr2, expr3);

            case ExpressionType.Constant:
                return expression;

            case ExpressionType.Invoke:

                var invocation = (InvocationExpression) expression;
                expr1 = Rewrite(invocation.Expression, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                arguments = null;
                tmpArguments = invocation.Arguments;
                if (tmpArguments.Count > 0)
                {
                    arguments = new List<Expression>(tmpArguments.Count);
                    for (i = 0; i < tmpArguments.Count; i++)
                    {
                        expr2 = Rewrite(tmpArguments[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }

                        arguments.Add(expr2);
                    }
                }

                return Expression.Invoke(expr1, arguments);

            case ExpressionType.Lambda:

                var lambda = (LambdaExpression) expression;
                expr1 = Rewrite(lambda.Body, lambda.Parameters, isLocationExpression, out abort);
                return abort ? null : Expression.Lambda(lambda.Type, expr1, lambda.Parameters);

            case ExpressionType.ListInit:

                var listInit = (ListInitExpression) expression;
                newExpression = (NewExpression) Rewrite(listInit.NewExpression, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                var tmpInitializers = listInit.Initializers;
                var initializers = new List<ElementInit>(tmpInitializers.Count);
                for (i = 0; i < tmpInitializers.Count; i++)
                {
                    tmpArguments = tmpInitializers[i].Arguments;
                    arguments = new List<Expression>(tmpArguments.Count);
                    for (j = 0; j < tmpArguments.Count; j++)
                    {
                        expr1 = Rewrite(tmpArguments[j], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }

                        arguments.Add(expr1);
                    }

                    initializers.Add(Expression.ElementInit(tmpInitializers[i].AddMethod, arguments));
                }

                return Expression.ListInit(newExpression, initializers);

            case ExpressionType.Parameter:
                var variableExpression = (ParameterExpression) expression;
                if (lambdaParameters != null && lambdaParameters.Contains(variableExpression))
                {
                    return variableExpression;
                }

                FindMatch findMatch;
                if (IsShortCutRewrite)
                    // 
                    //  this is the opportunity to inspect whether the cached LambdaExpression(raw expression tree)
                    // does coincide with the current LocationReferenceEnvironment.
                    // If any mismatch discovered, it immediately returns NULL, indicating cache lookup failure.
                    //                         
                {
                    findMatch = s_delegateFindLocationReferenceMatchShortcut;
                }
                else
                    // 
                    // variable(LocationReference) resolution process
                    // Note that the non-shortcut compilation pass always gaurantees successful variable resolution here.
                    //
                {
                    findMatch = s_delegateFindFirstLocationReferenceMatch;
                }

                var finalReference = FindLocationReferencesFromEnvironment(
                    Environment,
                    findMatch,
                    variableExpression.Name,
                    variableExpression.Type,
                    out var foundMultiple);

                if (finalReference != null && !foundMultiple)
                {
                    if (PublicAccessor != null)
                    {
                        var localPublicAccessor = PublicAccessor.Value;

                        if (ExpressionUtilities.TryGetInlinedReference(localPublicAccessor,
                                finalReference, isLocationExpression, out var inlinedReference))
                        {
                            finalReference = inlinedReference;
                        }
                    }

                    return ExpressionUtilities.CreateIdentifierExpression(finalReference);
                }

                if (IsShortCutRewrite)
                {
                    // cached LambdaExpression doesn't match this LocationReferenceEnvironment!!
                    // no matching LocationReference found.
                    // fail immeditely.
                    abort = true;
                    return null;
                }

                // if we are here, this variableExpression is a temp variable 
                // generated by the compiler.
                return variableExpression;

            case ExpressionType.MemberAccess:

                var memberExpression = (MemberExpression) expression;

                // When creating a location for a member on a struct, we also need a location
                // for the struct (so we don't just set the member on a copy of the struct)
                var subTreeIsLocationExpression =
                    isLocationExpression && memberExpression.Member.DeclaringType!.IsValueType;

                expr1 = Rewrite(memberExpression.Expression, lambdaParameters, subTreeIsLocationExpression, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.MakeMemberAccess(expr1, memberExpression.Member);

            case ExpressionType.MemberInit:

                var memberInit = (MemberInitExpression) expression;
                newExpression = (NewExpression) Rewrite(memberInit.NewExpression, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                var tmpMemberBindings = memberInit.Bindings;
                var bindings = new List<MemberBinding>(tmpMemberBindings.Count);
                for (i = 0; i < tmpMemberBindings.Count; i++)
                {
                    var binding = Rewrite(tmpMemberBindings[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    bindings.Add(binding);
                }

                return Expression.MemberInit(newExpression, bindings);

            case ExpressionType.ArrayIndex:

                // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                if (expression is MethodCallExpression arrayIndex)
                {
                    expr1 = Rewrite(arrayIndex.Object, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    tmpArguments = arrayIndex.Arguments;
                    var indexes = new List<Expression>(tmpArguments.Count);
                    for (i = 0; i < tmpArguments.Count; i++)
                    {
                        expr2 = Rewrite(tmpArguments[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }

                        indexes.Add(expr2);
                    }

                    return Expression.ArrayIndex(expr1, indexes);
                }

                var alternateIndex = (BinaryExpression) expression;
                expr1 = Rewrite(alternateIndex.Left, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                expr2 = Rewrite(alternateIndex.Right, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.ArrayIndex(expr1, expr2);

            case ExpressionType.Call:

                var methodCall = (MethodCallExpression) expression;
                expr1 = Rewrite(methodCall.Object, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                arguments = null;
                tmpArguments = methodCall.Arguments;
                if (tmpArguments.Count > 0)
                {
                    arguments = new List<Expression>(tmpArguments.Count);
                    for (i = 0; i < tmpArguments.Count; i++)
                    {
                        expr2 = Rewrite(tmpArguments[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }

                        arguments.Add(expr2);
                    }
                }

                return Expression.Call(expr1, methodCall.Method, arguments);

            case ExpressionType.NewArrayInit:

                var newArray = (NewArrayExpression) expression;
                var tmpExpressions = newArray.Expressions;
                var arrayInitializers = new List<Expression>(tmpExpressions.Count);
                for (i = 0; i < tmpExpressions.Count; i++)
                {
                    expr1 = Rewrite(tmpExpressions[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    arrayInitializers.Add(expr1);
                }

                return Expression.NewArrayInit(newArray.Type.GetElementType()!, arrayInitializers);

            case ExpressionType.NewArrayBounds:

                var newArrayBounds = (NewArrayExpression) expression;
                tmpExpressions = newArrayBounds.Expressions;
                var bounds = new List<Expression>(tmpExpressions.Count);
                for (i = 0; i < tmpExpressions.Count; i++)
                {
                    expr1 = Rewrite(tmpExpressions[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    bounds.Add(expr1);
                }

                return Expression.NewArrayBounds(newArrayBounds.Type.GetElementType()!, bounds);

            case ExpressionType.New:

                newExpression = (NewExpression) expression;
                if (newExpression.Constructor == null)
                {
                    // must be creating a valuetype
                    Fx.Assert(newExpression.Arguments.Count == 0,
                        "NewExpression with null Constructor but some arguments");
                    return expression;
                }

                arguments = null;
                tmpArguments = newExpression.Arguments;
                if (tmpArguments.Count > 0)
                {
                    arguments = new List<Expression>(tmpArguments.Count);
                    for (i = 0; i < tmpArguments.Count; i++)
                    {
                        expr1 = Rewrite(tmpArguments[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }

                        arguments.Add(expr1);
                    }
                }

                return newExpression.Update(arguments);

            case ExpressionType.TypeIs:

                var typeBinary = (TypeBinaryExpression) expression;
                expr1 = Rewrite(typeBinary.Expression, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.TypeIs(expr1, typeBinary.TypeOperand);

            case ExpressionType.ArrayLength:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Quote:
            case ExpressionType.TypeAs:

                var unary = (UnaryExpression) expression;
                expr1 = Rewrite(unary.Operand, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.MakeUnary(unary.NodeType, expr1, unary.Type, unary.Method);

            case ExpressionType.UnaryPlus:

                var unaryPlus = (UnaryExpression) expression;
                expr1 = Rewrite(unaryPlus.Operand, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.UnaryPlus(expr1, unaryPlus.Method);

            // Expression Tree V2.0 types. This is due to the hosted VB compiler generating ET V2.0 nodes
            case ExpressionType.Block:

                var block = (BlockExpression) expression;
                var tmpVariables = block.Variables;
                var parameterList = new List<ParameterExpression>(tmpVariables.Count);
                for (i = 0; i < tmpVariables.Count; i++)
                {
                    var param = (ParameterExpression) Rewrite(tmpVariables[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    parameterList.Add(param);
                }

                tmpExpressions = block.Expressions;
                var expressionList = new List<Expression>(tmpExpressions.Count);
                for (i = 0; i < tmpExpressions.Count; i++)
                {
                    expr1 = Rewrite(tmpExpressions[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    expressionList.Add(expr1);
                }

                return Expression.Block(parameterList, expressionList);

            case ExpressionType.Assign:

                var assign = (BinaryExpression) expression;
                expr1 = Rewrite(assign.Left, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                expr2 = Rewrite(assign.Right, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.Assign(expr1, expr2);
        }

        Fx.Assert("Don't understand expression type " + expression.NodeType);
        return expression;
    }

    private MemberBinding Rewrite(MemberBinding binding, ReadOnlyCollection<ParameterExpression> lambdaParameters,
        out bool abort)
    {
        int i;
        int j;
        Expression expr1;
        ReadOnlyCollection<Expression> tmpArguments;
        abort = false;
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:

                var assignment = (MemberAssignment) binding;
                expr1 = Rewrite(assignment.Expression, lambdaParameters, out abort);
                if (abort)
                {
                    return null;
                }

                return Expression.Bind(assignment.Member, expr1);

            case MemberBindingType.ListBinding:

                var list = (MemberListBinding) binding;
                List<ElementInit> initializers = null;
                var tmpInitializers = list.Initializers;
                if (tmpInitializers.Count <= 0)
                {
                    return Expression.ListBind(list.Member, Enumerable.Empty<ElementInit>());
                }

                initializers = new List<ElementInit>(tmpInitializers.Count);
                for (i = 0; i < tmpInitializers.Count; i++)
                {
                    List<Expression> arguments = null;
                    tmpArguments = tmpInitializers[i].Arguments;
                    if (tmpArguments.Count > 0)
                    {
                        arguments = new List<Expression>(tmpArguments.Count);
                        for (j = 0; j < tmpArguments.Count; j++)
                        {
                            expr1 = Rewrite(tmpArguments[j], lambdaParameters, out abort);
                            if (abort)
                            {
                                return null;
                            }

                            arguments.Add(expr1);
                        }
                    }

                    initializers.Add(Expression.ElementInit(tmpInitializers[i].AddMethod,
                        arguments ?? Enumerable.Empty<Expression>()));
                }

                return Expression.ListBind(list.Member, initializers);

            case MemberBindingType.MemberBinding:

                var member = (MemberMemberBinding) binding;
                var tmpBindings = member.Bindings;
                var bindings = new List<MemberBinding>(tmpBindings.Count);
                for (i = 0; i < tmpBindings.Count; i++)
                {
                    var item = Rewrite(tmpBindings[i], lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    bindings.Add(item);
                }

                return Expression.MemberBind(member.Member, bindings);

            default:
                Fx.Assert("MemberBinding type '" + binding.BindingType + "' is not supported.");
                return binding;
        }
    }

    protected static ParameterExpression FindParameter(Expression expression)
    {
        if (expression == null)
        {
            return null;
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
                var binaryExpression = (BinaryExpression) expression;
                return FindParameter(binaryExpression.Left) ?? FindParameter(binaryExpression.Right);

            case ExpressionType.Conditional:
                var conditional = (ConditionalExpression) expression;
                return FindParameter(conditional.Test) ??
                    FindParameter(conditional.IfTrue) ?? FindParameter(conditional.IfFalse);

            case ExpressionType.Constant:
                return null;

            case ExpressionType.Invoke:
                var invocation = (InvocationExpression) expression;
                return FindParameter(invocation.Expression) ?? FindParameter(invocation.Arguments);

            case ExpressionType.Lambda:
                var lambda = (LambdaExpression) expression;
                return FindParameter(lambda.Body);

            case ExpressionType.ListInit:
                var listInit = (ListInitExpression) expression;
                return FindParameter(listInit.NewExpression) ?? FindParameter(listInit.Initializers);

            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression) expression;
                return FindParameter(memberExpression.Expression);

            case ExpressionType.MemberInit:
                var memberInit = (MemberInitExpression) expression;
                return FindParameter(memberInit.NewExpression) ?? FindParameter(memberInit.Bindings);

            case ExpressionType.ArrayIndex:
                // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                if (expression is MethodCallExpression arrayIndex)
                {
                    return FindParameter(arrayIndex.Object) ?? FindParameter(arrayIndex.Arguments);
                }

                var alternateIndex = (BinaryExpression) expression;
                return FindParameter(alternateIndex.Left) ?? FindParameter(alternateIndex.Right);

            case ExpressionType.Call:
                var methodCall = (MethodCallExpression) expression;
                return FindParameter(methodCall.Object) ?? FindParameter(methodCall.Arguments);

            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                var newArray = (NewArrayExpression) expression;
                return FindParameter(newArray.Expressions);

            case ExpressionType.New:
                var newExpression = (NewExpression) expression;
                return FindParameter(newExpression.Arguments);

            case ExpressionType.Parameter:
                var parameterExpression = (ParameterExpression) expression;
                if (parameterExpression.Type == typeof(ActivityContext) && parameterExpression.Name == "context")
                {
                    return parameterExpression;
                }

                return null;

            case ExpressionType.TypeIs:
                var typeBinary = (TypeBinaryExpression) expression;
                return FindParameter(typeBinary.Expression);

            case ExpressionType.ArrayLength:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Quote:
            case ExpressionType.TypeAs:
            case ExpressionType.UnaryPlus:
                var unary = (UnaryExpression) expression;
                return FindParameter(unary.Operand);

            // Expression Tree V2.0 types

            case ExpressionType.Block:
                var block = (BlockExpression) expression;
                var toReturn = FindParameter(block.Expressions);
                if (toReturn != null)
                {
                    return toReturn;
                }

                var variableList = block.Variables.Cast<Expression>().ToList();
                return FindParameter(variableList);

            case ExpressionType.Assign:
                var assign = (BinaryExpression) expression;
                return FindParameter(assign.Left) ?? FindParameter(assign.Right);
        }

        Fx.Assert("Don't understand expression type " + expression.NodeType);
        return null;
    }

    private static ParameterExpression FindParameter(IEnumerable<Expression> collection) =>
        collection.Select(expression => FindParameter(expression)).FirstOrDefault(result => result != null);

    private static ParameterExpression FindParameter(IEnumerable<ElementInit> collection) =>
        collection.Select(init => FindParameter(init.Arguments)).FirstOrDefault(result => result != null);

    private static ParameterExpression FindParameter(IEnumerable<MemberBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            ParameterExpression result;
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    var assignment = (MemberAssignment) binding;
                    result = FindParameter(assignment.Expression);
                    break;

                case MemberBindingType.ListBinding:
                    var list = (MemberListBinding) binding;
                    result = FindParameter(list.Initializers);
                    break;

                case MemberBindingType.MemberBinding:
                    var member = (MemberMemberBinding) binding;
                    result = FindParameter(member.Bindings);
                    break;

                default:
                    Fx.Assert("MemberBinding type '" + binding.BindingType + "' is not supported.");
                    result = null;
                    break;
            }

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    protected internal static void EnsureTypeReferenced(Type type, ref HashSet<Type> typeReferences)
    {
        // lookup cache 
        // underlying assumption is that type's inheritance(or interface) hierarchy 
        // stays static throughout the lifetime of AppDomain
        var alreadyVisited = (HashSet<Type>) s_typeReferenceCache.GetValue(s_typeReferenceCacheLock, type);
        if (alreadyVisited != null)
        {
            if (typeReferences == null)
                // used in VBHelper.Compile<>
                // must not alter this set being returned for integrity of cache
            {
                typeReferences = alreadyVisited;
            }
            else
                // used in VBDesignerHelper.FindTypeReferences
            {
                typeReferences.UnionWith(alreadyVisited);
            }

            return;
        }

        alreadyVisited = new HashSet<Type>();
        EnsureTypeReferencedRecurse(type, alreadyVisited);

        // cache resulting alreadyVisited set for fast future lookup
        lock (s_typeReferenceCacheLock)
        {
            s_typeReferenceCache.Add(type, alreadyVisited);
        }

        if (typeReferences == null)
            // used in VBHelper.Compile<>
            // must not alter this set being returned for integrity of cache
        {
            typeReferences = alreadyVisited;
        }
        else
            // used in VBDesignerHelper.FindTypeReferences
        {
            typeReferences.UnionWith(alreadyVisited);
        }
    }

    private static void EnsureTypeReferencedRecurse(Type type, HashSet<Type> alreadyVisited)
    {
        if (alreadyVisited.Contains(type))
            // this prevents circular reference
            // example), class Foo : IBar<Foo>
        {
            return;
        }

        alreadyVisited.Add(type);

        // make sure any interfaces needed by this type are referenced
        var interfaces = type.GetInterfaces();
        foreach (var t in interfaces)
        {
            EnsureTypeReferencedRecurse(t, alreadyVisited);
        }

        // same for base types
        var baseType = type.BaseType;
        while (baseType != null && baseType != TypeHelper.ObjectType)
        {
            EnsureTypeReferencedRecurse(baseType, alreadyVisited);
            baseType = baseType.BaseType;
        }

        // for generic types, all type arguments
        if (type.IsGenericType)
        {
            var typeArgs = type.GetGenericArguments();
            for (var i = 1; i < typeArgs.Length; ++i)
            {
                EnsureTypeReferencedRecurse(typeArgs[i], alreadyVisited);
            }
        }

        // array types
        if (type.HasElementType)
        {
            EnsureTypeReferencedRecurse(type.GetElementType(), alreadyVisited);
        }
    }

    private static LocationReference FindLocationReferencesFromEnvironment(LocationReferenceEnvironment environment,
        FindMatch findMatch, string targetName, Type targetType, out bool foundMultiple)
    {
        var currentEnvironment = environment;
        foundMultiple = false;
        while (currentEnvironment != null)
        {
            LocationReference toReturn = null;
            foreach (var reference in currentEnvironment.GetLocationReferences())
            {
                if (findMatch(reference, targetName, targetType, out var terminateSearch))
                {
                    if (toReturn != null)
                    {
                        foundMultiple = true;
                        return toReturn;
                    }

                    toReturn = reference;
                }

                if (terminateSearch)
                {
                    return toReturn;
                }
            }

            if (toReturn != null)
            {
                return toReturn;
            }

            currentEnvironment = currentEnvironment.Parent;
        }

        return null;
    }

    private delegate bool FindMatch(LocationReference reference, string targetName, Type targetType,
        out bool terminateSearch);

    // this is a place holder for LambdaExpression(raw Expression Tree) that is to be stored in the cache
    // this wrapper is necessary because HopperCache requires that once you already have a key along with its associated value in the cache
    // you cannot add the same key with a different value.
    protected class RawTreeCacheValueWrapper
    {
        public LambdaExpression Value { get; set; }
    }

    protected class RawTreeCacheKey
    {
        private static readonly IEqualityComparer<HashSet<Assembly>> s_assemblySetEqualityComparer =
            HashSet<Assembly>.CreateSetComparer();

        private static readonly IEqualityComparer<HashSet<string>> s_namespaceSetEqualityComparer =
            HashSet<string>.CreateSetComparer();

        private readonly HashSet<Assembly> _assemblies;

        private readonly string _expressionText;

        private readonly int _hashCode;
        private readonly HashSet<string> _namespaces;
        private readonly Type _returnType;

        public RawTreeCacheKey(string expressionText, Type returnType, HashSet<Assembly> assemblies,
            IReadOnlyCollection<string> namespaces)
        {
            _expressionText = expressionText;
            _returnType = returnType;
            _assemblies = new HashSet<Assembly>(assemblies);
            _namespaces = new HashSet<string>(namespaces);

            _hashCode = expressionText?.GetHashCode() ?? 0;
            _hashCode = CombineHashCodes(_hashCode, s_assemblySetEqualityComparer.GetHashCode(assemblies));
            _hashCode = CombineHashCodes(_hashCode, s_namespaceSetEqualityComparer.GetHashCode(_namespaces));
            if (returnType != null)
            {
                _hashCode = CombineHashCodes(_hashCode, returnType.GetHashCode());
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not RawTreeCacheKey rtcKey || _hashCode != rtcKey._hashCode)
            {
                return false;
            }

            return _expressionText == rtcKey._expressionText &&
                _returnType == rtcKey._returnType &&
                s_assemblySetEqualityComparer.Equals(_assemblies, rtcKey._assemblies) &&
                s_namespaceSetEqualityComparer.Equals(_namespaces, rtcKey._namespaces);
        }

        public override int GetHashCode() => _hashCode;

        private static int CombineHashCodes(int h1, int h2) => ((h1 << 5) + h1) ^ h2;
    }

    protected internal class ScriptAndTypeScope
    {
        private readonly LocationReferenceEnvironment _environmentProvider;
        private List<Assembly> _assemblies;

        public ScriptAndTypeScope(LocationReferenceEnvironment environmentProvider, List<Assembly> assemblies)
        {
            _environmentProvider = environmentProvider;
            _assemblies = assemblies;
        }

        public string ErrorMessage { get; private set; }

        public Type FindVariable(string name)
        {
            LocationReference referenceToReturn = null;
            var findMatch = s_delegateFindAllLocationReferenceMatch;
            referenceToReturn =
                FindLocationReferencesFromEnvironment(_environmentProvider, findMatch, name, null, out var foundMultiple);
            if (referenceToReturn != null)
            {
                if (foundMultiple)
                {
                    // we have duplicate variable names in the same visible environment!!!!
                    // compile error here!!!!
                    ErrorMessage = SR.AmbiguousVBVariableReference(name);
                    return null;
                }

                return referenceToReturn.Type;
            }

            return null;
        }

        public Type[] FindTypes(string typeName, string nsPrefix) => null;

        public bool NamespaceExists(string ns) => false;
    }

    [Fx.Tag.SecurityNoteAttribute(Critical =
        "Critical because it holds a HostedCompiler instance, which requires FullTrust.")]
    [SecurityCritical]
    internal class HostedCompilerWrapper
    {
        private readonly object _wrapperLock;
        private bool _isCached;
        private int _refCount;

        public HostedCompilerWrapper(JustInTimeCompiler compiler)
        {
            Fx.Assert(compiler != null, "HostedCompilerWrapper must be assigned a non-null compiler");
            _wrapperLock = new object();
            Compiler = compiler;
            _isCached = true;
            _refCount = 0;
        }

        public JustInTimeCompiler Compiler { get; private set; }

        // Storing ticks of the time it last used.
        public ulong Timestamp { get; private set; }

        // this is called only when this Wrapper is being kicked out the Cache
        public void MarkAsKickedOut()
        {
            IDisposable compilerToDispose = null;
            lock (_wrapperLock)
            {
                _isCached = false;
                if (_refCount == 0)
                {
                    // if conditions are met,
                    // Dispose the HostedCompiler
                    compilerToDispose = Compiler as IDisposable;
                    Compiler = null;
                }
            }

            compilerToDispose?.Dispose();
        }

        // this always precedes Compiler.CompileExpression() operation in a thread of execution
        // this must never be called after Compiler.Dispose() either in MarkAsKickedOut() or Release()
        public void Reserve(ulong timestamp)
        {
            Fx.Assert(_isCached, "Can only reserve cached HostedCompiler");
            lock (_wrapperLock)
            {
                _refCount++;
            }

            Timestamp = timestamp;
        }

        // Compiler.CompileExpression() is always followed by this in a thread of execution
        public void Release()
        {
            IDisposable compilerToDispose = null;
            lock (_wrapperLock)
            {
                _refCount--;
                if (!_isCached && _refCount == 0)
                {
                    // if conditions are met,
                    // Dispose the HostedCompiler
                    compilerToDispose = Compiler as IDisposable;
                    Compiler = null;
                }
            }

            compilerToDispose?.Dispose();
        }
    }
}

internal abstract class JitCompilerHelper<TLanguage> : JitCompilerHelper
{
    // Cache<(expressionText+ReturnType+Assemblies+Imports), LambdaExpression>
    // LambdaExpression represents raw ExpressionTrees right out of the vb hosted compiler
    // these raw trees are yet to be rewritten with appropriate Variables
    private const int RawTreeCacheMaxSize = 128;

    private const int HostedCompilerCacheSize = 10;
    private static ulong s_lastTimestamp;
    private static readonly object s_rawTreeCacheLock = new();

    [Fx.Tag.SecurityNoteAttribute(Critical =
        "Critical because it caches objects created under a demand for FullTrust.")]
    [SecurityCritical]
    private static HopperCache s_rawTreeCache;

    [Fx.Tag.SecurityNoteAttribute(Critical =
        "Critical because it holds HostedCompilerWrappers which hold HostedCompiler instances, which require FullTrust.")]
    [SecurityCritical]
    private static CompilerCache s_hostedCompilerCache;

    public JitCompilerHelper(string expressionText, HashSet<AssemblyName> refAssemNames,
        HashSet<string> namespaceImportsNames)
        : this(expressionText)
    {
        Initialize(refAssemNames, namespaceImportsNames);
    }

    protected JitCompilerHelper(string expressionText)
    {
        TextToCompile = expressionText;
    }

    private static HopperCache RawTreeCache
    {
        [Fx.Tag.SecurityNoteAttribute(Critical = "Critical because it access critical member rawTreeCache.")]
        [SecurityCritical]
        get => s_rawTreeCache ??= new HopperCache(RawTreeCacheMaxSize, false);
    }

    public string TextToCompile { get; }

    [Fx.Tag.SecurityNoteAttribute(
        Critical =
            "Critical because it creates Microsoft.Compiler.VisualBasic.HostedCompiler, which is in a non-APTCA assembly, and thus has a LinkDemand.",
        Safe =
            "Safe because it puts the HostedCompiler instance into the HostedCompilerCache member, which is SecurityCritical and we are demanding FullTrust.")]
    [SecuritySafeCritical]
    //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    private HostedCompilerWrapper GetCachedHostedCompiler(HashSet<Assembly> assemblySet)
    {
        if (s_hostedCompilerCache == null)
        {
            // we don't want to newup a Dictionary everytime GetCachedHostedCompiler is called only to find out the cache is already initialized.
            var oldCompilerCache = Interlocked.CompareExchange(ref s_hostedCompilerCache,
                new CompilerCache(HostedCompilerCacheSize, HashSet<Assembly>.CreateSetComparer()),
                null);
            if (oldCompilerCache == null)
            {
                OnCompilerCacheCreated(s_hostedCompilerCache);
            }
        }

        lock (s_hostedCompilerCache)
        {
            if (s_hostedCompilerCache.TryGetValue(assemblySet, out var hostedCompilerWrapper))
            {
                hostedCompilerWrapper.Reserve(unchecked(++s_lastTimestamp));
                return hostedCompilerWrapper;
            }

            if (s_hostedCompilerCache.Count >= HostedCompilerCacheSize)
            {
                // Find oldest used compiler to kick out
                var oldestTimestamp = ulong.MaxValue;
                HashSet<Assembly> oldestCompiler = null;
                foreach (var (key, value) in s_hostedCompilerCache)
                {
                    if (oldestTimestamp > value.Timestamp)
                    {
                        oldestCompiler = key;
                        oldestTimestamp = value.Timestamp;
                    }
                }

                if (oldestCompiler != null)
                {
                    hostedCompilerWrapper = s_hostedCompilerCache[oldestCompiler];
                    s_hostedCompilerCache.Remove(oldestCompiler);
                    hostedCompilerWrapper.MarkAsKickedOut();
                }
            }

            hostedCompilerWrapper = new HostedCompilerWrapper(CreateCompiler(assemblySet));
            s_hostedCompilerCache[assemblySet] = hostedCompilerWrapper;
            hostedCompilerWrapper.Reserve(unchecked(++s_lastTimestamp));

            return hostedCompilerWrapper;
        }
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical =
            "Critical because it invokes a HostedCompiler, which requires FullTrust and also accesses RawTreeCache, which is SecurityCritical.",
        Safe = "Safe because we are demanding FullTrust.")]
    [SecuritySafeCritical]
    //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public override LambdaExpression CompileNonGeneric(LocationReferenceEnvironment environment)
    {
        bool abort;
        Expression finalBody;
        Environment = environment;
        ReferencedAssemblies ??= new HashSet<Assembly>();

        ReferencedAssemblies.UnionWith(DefaultReferencedAssemblies);

        var rawTreeKey = new RawTreeCacheKey(
            TextToCompile,
            null,
            ReferencedAssemblies,
            NamespaceImports);

        var rawTreeHolder = RawTreeCache.GetValue(s_rawTreeCacheLock, rawTreeKey) as RawTreeCacheValueWrapper;
        if (rawTreeHolder != null)
        {
            // try short-cut
            // if variable resolution fails at Rewrite, rewind and perform normal compile steps
            var rawTree = rawTreeHolder.Value;
            IsShortCutRewrite = true;
            finalBody = Rewrite(rawTree.Body, null, false, out abort);
            IsShortCutRewrite = false;

            if (!abort)
            {
                return Expression.Lambda(rawTree.Type, finalBody, rawTree.Parameters);
            }

            // if we are here, then that means the shortcut Rewrite failed.
            // we don't want to see too many of vb expressions in this pass since we just wasted Rewrite time for no good.
        }

        var scriptAndTypeScope = new ScriptAndTypeScope(environment, ReferencedAssemblies.ToList());
        var compilerWrapper = GetCachedHostedCompiler(ReferencedAssemblies);
        var compiler = compilerWrapper.Compiler;
        LambdaExpression lambda = null;
        try
        {
            lock (compiler)
            {
                try
                {
                    lambda = compiler.CompileExpression(ExpressionToCompile(scriptAndTypeScope.FindVariable,
                        typeof(object)));
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    FxTrace.Exception.TraceUnhandledException(e);
                    throw;
                }
            }
        }
        finally
        {
            compilerWrapper.Release();
        }

        if (scriptAndTypeScope.ErrorMessage != null)
        {
            throw FxTrace.Exception.AsError(
                new SourceExpressionException(
                    SR.CompilerErrorSpecificExpression(TextToCompile, scriptAndTypeScope.ErrorMessage)));
        }

        // replace the field references with variable references to our dummy variables
        // and rewrite lambda.body.Type to equal the lambda return type T            
        if (lambda == null)
            // ExpressionText was either an empty string or Null
            // we return null which eventually evaluates to default(TResult) at execution time.
        {
            return null;
        }

        // add the pre-rewrite lambda to RawTreeCache
        var typedLambda = Expression.Lambda(lambda.Body is UnaryExpression cast ? cast.Operand : lambda.Body,
            lambda.Parameters);
        AddToRawTreeCache(rawTreeKey, rawTreeHolder, typedLambda);

        finalBody = Rewrite(typedLambda.Body, null, false, out abort);
        Fx.Assert(abort == false, "this non-shortcut Rewrite must always return abort == false");

        return Expression.Lambda(finalBody, lambda.Parameters);
    }

    private ExpressionToCompile ExpressionToCompile(Func<string, Type> variableTypeGetter, Type lambdaReturnType)
    {
        return new ExpressionToCompile(TextToCompile, NamespaceImports, variableTypeGetter, lambdaReturnType);
    }

    public Expression<Func<ActivityContext, T>> Compile<T>(CodeActivityPublicEnvironmentAccessor publicAccessor,
        bool isLocationReference = false)
    {
        PublicAccessor = publicAccessor;

        return Compile<T>(publicAccessor.ActivityMetadata.Environment, isLocationReference);
    }

    // Soft-Link: This method is called through reflection by VisualBasicDesignerHelper.
    public Expression<Func<ActivityContext, T>> Compile<T>(LocationReferenceEnvironment environment)
    {
        Fx.Assert(PublicAccessor == null, "No public accessor so the value for isLocationReference doesn't matter");
        return Compile<T>(environment, false);
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical =
            "Critical because it invokes a HostedCompiler, which requires FullTrust and also accesses RawTreeCache, which is SecurityCritical.",
        Safe = "Safe because we are demanding FullTrust.")]
    [SecuritySafeCritical]
    //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public Expression<Func<ActivityContext, T>> Compile<T>(LocationReferenceEnvironment environment,
        bool isLocationReference)
    {
        bool abort;
        Expression finalBody;
        var lambdaReturnType = typeof(T);

        Environment = environment;
        ReferencedAssemblies ??= new HashSet<Assembly>();
        ReferencedAssemblies.UnionWith(DefaultReferencedAssemblies);

        var rawTreeKey = new RawTreeCacheKey(
            TextToCompile,
            lambdaReturnType,
            ReferencedAssemblies,
            NamespaceImports);

        var rawTreeHolder = RawTreeCache.GetValue(s_rawTreeCacheLock, rawTreeKey) as RawTreeCacheValueWrapper;
        if (rawTreeHolder != null)
        {
            // try short-cut
            // if variable resolution fails at Rewrite, rewind and perform normal compile steps
            var rawTree = rawTreeHolder.Value;
            IsShortCutRewrite = true;
            finalBody = Rewrite(rawTree.Body, null, isLocationReference, out abort);
            IsShortCutRewrite = false;

            if (!abort)
            {
                Fx.Assert(finalBody.Type == lambdaReturnType,
                    "Compiler generated ExpressionTree return type doesn't match the target return type");
                // convert it into the our expected lambda format (context => ...)
                return Expression.Lambda<Func<ActivityContext, T>>(finalBody,
                    FindParameter(finalBody) ?? ExpressionUtilities.RuntimeContextParameter);
            }

            // if we are here, then that means the shortcut Rewrite failed.
            // we don't want to see too many of vb expressions in this pass since we just wasted Rewrite time for no good.

            PublicAccessor?.ActivityMetadata.CurrentActivity.ResetTempAutoGeneratedArguments();
        }

        // ensure the return type's assembly is added to ref assembly list
        HashSet<Type> allBaseTypes = null;
        EnsureTypeReferenced(lambdaReturnType, ref allBaseTypes);
        foreach (var baseType in allBaseTypes)
            // allBaseTypes list always contains lambdaReturnType
        {
            ReferencedAssemblies.Add(baseType.Assembly);
        }

        var scriptAndTypeScope = new ScriptAndTypeScope(environment, ReferencedAssemblies.ToList());
        var compilerWrapper = GetCachedHostedCompiler(ReferencedAssemblies);
        var compiler = compilerWrapper.Compiler;

        if (TD.CompileVbExpressionStartIsEnabled())
        {
            TD.CompileVbExpressionStart(TextToCompile);
        }

        LambdaExpression lambda = null;
        try
        {
            lock (compiler)
            {
                try
                {
                    lambda = compiler.CompileExpression(ExpressionToCompile(scriptAndTypeScope.FindVariable,
                        lambdaReturnType));
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    // We never want to end up here, Compiler bugs needs to be fixed.
                    FxTrace.Exception.TraceUnhandledException(e);
                    throw;
                }
            }
        }
        finally
        {
            compilerWrapper.Release();
        }

        if (TD.CompileVbExpressionStopIsEnabled())
        {
            TD.CompileVbExpressionStop();
        }

        if (scriptAndTypeScope.ErrorMessage != null)
        {
            throw FxTrace.Exception.AsError(
                new SourceExpressionException(
                    SR.CompilerErrorSpecificExpression(TextToCompile, scriptAndTypeScope.ErrorMessage)));
        }

        // replace the field references with variable references to our dummy variables
        // and rewrite lambda.body.Type to equal the lambda return type T            
        if (lambda == null)
            // ExpressionText was either an empty string or Null
            // we return null which eventually evaluates to default(TResult) at execution time.
        {
            return null;
        }

        // add the pre-rewrite lambda to RawTreeCache
        AddToRawTreeCache(rawTreeKey, rawTreeHolder, lambda);

        finalBody = Rewrite(lambda.Body, null, isLocationReference, out abort);
        Fx.Assert(abort == false, "this non-shortcut Rewrite must always return abort == false");
        Fx.Assert(finalBody.Type == lambdaReturnType,
            "Compiler generated ExpressionTree return type doesn't match the target return type");

        // convert it into the our expected lambda format (context => ...)
        return Expression.Lambda<Func<ActivityContext, T>>(finalBody,
            FindParameter(finalBody) ?? ExpressionUtilities.RuntimeContextParameter);
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical = "Critical because it access SecurityCritical member RawTreeCache, thus requiring FullTrust.",
        Safe = "Safe because we are demanding FullTrust.")]
    [SecuritySafeCritical]
    //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    private static void AddToRawTreeCache(RawTreeCacheKey rawTreeKey, RawTreeCacheValueWrapper rawTreeHolder,
        LambdaExpression lambda)
    {
        if (rawTreeHolder != null)
            // this indicates that the key had been found in RawTreeCache,
            // but the value Expression Tree failed the short-cut Rewrite.
            // ---- is really not an issue here, because
            // any one of possibly many raw Expression Trees that are all 
            // represented by the same key can be written here.
        {
            rawTreeHolder.Value = lambda;
        }
        else
            // we never hit RawTreeCache with the given key
        {
            lock (s_rawTreeCacheLock)
            {
                // ensure we don't add the same key with two different RawTreeValueWrappers
                if (RawTreeCache.GetValue(s_rawTreeCacheLock, rawTreeKey) == null)
                    // do we need defense against alternating miss of the shortcut Rewrite?
                {
                    RawTreeCache.Add(rawTreeKey, new RawTreeCacheValueWrapper {Value = lambda});
                }
            }
        }
    }
}
