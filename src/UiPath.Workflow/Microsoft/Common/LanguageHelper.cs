namespace Microsoft.Common
{
    using Microsoft.VisualBasic.Activities;
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;
    using System.Reflection;

    abstract class LanguageHelper
    {
        protected HashSet<Assembly> referencedAssemblies;
        protected IReadOnlyCollection<string> namespaceImports;
        protected LocationReferenceEnvironment environment;
        protected CodeActivityPublicEnvironmentAccessor? publicAccessor;

        protected delegate bool FindMatch(LocationReference reference, string targetName, Type targetType, out bool terminateSearch);
        protected static readonly FindMatch delegateFindLocationReferenceMatchShortcut = new FindMatch(FindLocationReferenceMatchShortcut);
        protected static readonly FindMatch delegateFindFirstLocationReferenceMatch = new FindMatch(FindFirstLocationReferenceMatch);
        protected static readonly FindMatch delegateFindAllLocationReferenceMatch = new FindMatch(FindAllLocationReferenceMatch);

        // this is a flag to differentiate the cached short-cut Rewrite from the normal post-compilation Rewrite
        protected bool isShortCutRewrite = false;

        protected string TextToCompile { get; }

        protected LanguageHelper(string expressionText)
        {
            TextToCompile = expressionText;
        }

        public static AssemblyName GetFastAssemblyName(Assembly assembly)
        {
            return AssemblyReference.GetFastAssemblyName(assembly);
        }

        protected void Initialize(HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames)
        {
            namespaceImportsNames.Add("System");
            namespaceImportsNames.Add("System.Linq.Expressions");
            namespaceImportsNames.Remove("");
            namespaceImportsNames.Remove(null);
            namespaceImports = namespaceImportsNames;

            foreach (AssemblyName assemblyName in refAssemNames)
            {
                if (referencedAssemblies == null)
                {
                    referencedAssemblies = new HashSet<Assembly>();
                }
                try
                {
                    Assembly loaded = AssemblyReference.GetAssembly(assemblyName);
                    if (loaded != null)
                    {
                        referencedAssemblies.Add(loaded);
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

        public static void GetAllImportReferences(Activity activity, bool isDesignTime, out List<string> namespaces, out List<AssemblyReference> assemblies)
        {
            List<string> namespaceList = new List<string>();
            List<AssemblyReference> assemblyList = new List<AssemblyReference>();

            // Start with the defaults; any settings on the Activity will be added to these
            // The default settings are mutable, so we need to re-copy this list on every call
            ExtractNamespacesAndReferences(VisualBasicSettings.Default, namespaceList, assemblyList);

            LocationReferenceEnvironment environment = activity.GetParentEnvironment();
            if (environment == null || environment.Root == null)
            {
                namespaces = namespaceList;
                assemblies = assemblyList;
                return;
            }

            VisualBasicSettings rootVBSettings = VisualBasic.GetSettings(environment.Root);
            if (rootVBSettings != null)
            {
                // We have VBSettings
                ExtractNamespacesAndReferences(rootVBSettings, namespaceList, assemblyList);
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

        protected static bool FindLocationReferenceMatchShortcut(LocationReference reference, string targetName, Type targetType, out bool terminateSearch)
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

        protected static bool FindFirstLocationReferenceMatch(LocationReference reference, string targetName, Type targetType, out bool terminateSearch)
        {
            terminateSearch = false;
            if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                terminateSearch = true;
                return true;
            }
            return false;
        }

        protected static bool FindAllLocationReferenceMatch(LocationReference reference, string targetName, Type targetType, out bool terminateSearch)
        {
            terminateSearch = false;
            if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
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
                    BinaryExpression binaryExpression = (BinaryExpression)expression;
                    return FindParameter(binaryExpression.Left) ?? FindParameter(binaryExpression.Right);

                case ExpressionType.Conditional:
                    ConditionalExpression conditional = (ConditionalExpression)expression;
                    return FindParameter(conditional.Test) ?? FindParameter(conditional.IfTrue) ?? FindParameter(conditional.IfFalse);

                case ExpressionType.Constant:
                    return null;

                case ExpressionType.Invoke:
                    InvocationExpression invocation = (InvocationExpression)expression;
                    return FindParameter(invocation.Expression) ?? FindParameter(invocation.Arguments);

                case ExpressionType.Lambda:
                    LambdaExpression lambda = (LambdaExpression)expression;
                    return FindParameter(lambda.Body);

                case ExpressionType.ListInit:
                    ListInitExpression listInit = (ListInitExpression)expression;
                    return FindParameter(listInit.NewExpression) ?? FindParameter(listInit.Initializers);

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)expression;
                    return FindParameter(memberExpression.Expression);

                case ExpressionType.MemberInit:
                    MemberInitExpression memberInit = (MemberInitExpression)expression;
                    return FindParameter(memberInit.NewExpression) ?? FindParameter(memberInit.Bindings);

                case ExpressionType.ArrayIndex:
                    // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                    MethodCallExpression arrayIndex = expression as MethodCallExpression;
                    if (arrayIndex != null)
                    {
                        return FindParameter(arrayIndex.Object) ?? FindParameter(arrayIndex.Arguments);
                    }
                    BinaryExpression alternateIndex = (BinaryExpression)expression;
                    return FindParameter(alternateIndex.Left) ?? FindParameter(alternateIndex.Right);

                case ExpressionType.Call:
                    MethodCallExpression methodCall = (MethodCallExpression)expression;
                    return FindParameter(methodCall.Object) ?? FindParameter(methodCall.Arguments);

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    NewArrayExpression newArray = (NewArrayExpression)expression;
                    return FindParameter(newArray.Expressions);

                case ExpressionType.New:
                    NewExpression newExpression = (NewExpression)expression;
                    return FindParameter(newExpression.Arguments);

                case ExpressionType.Parameter:
                    ParameterExpression parameterExpression = (ParameterExpression)expression;
                    if (parameterExpression.Type == typeof(ActivityContext) && parameterExpression.Name == "context")
                    {
                        return parameterExpression;
                    }
                    return null;

                case ExpressionType.TypeIs:
                    TypeBinaryExpression typeBinary = (TypeBinaryExpression)expression;
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
                    UnaryExpression unary = (UnaryExpression)expression;
                    return FindParameter(unary.Operand);

                // Expression Tree V2.0 types

                case ExpressionType.Block:
                    BlockExpression block = (BlockExpression)expression;
                    ParameterExpression toReturn = FindParameter(block.Expressions);
                    if (toReturn != null)
                    {
                        return toReturn;
                    }
                    List<Expression> variableList = new List<Expression>();
                    foreach (ParameterExpression variable in block.Variables)
                    {
                        variableList.Add(variable);
                    }
                    return FindParameter(variableList);

                case ExpressionType.Assign:
                    BinaryExpression assign = (BinaryExpression)expression;
                    return FindParameter(assign.Left) ?? FindParameter(assign.Right);
            }

            Fx.Assert("Don't understand expression type " + expression.NodeType);
            return null;
        }

        protected static ParameterExpression FindParameter(ICollection<Expression> collection)
        {
            foreach (Expression expression in collection)
            {
                ParameterExpression result = FindParameter(expression);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        protected static ParameterExpression FindParameter(ICollection<ElementInit> collection)
        {
            foreach (ElementInit init in collection)
            {
                ParameterExpression result = FindParameter(init.Arguments);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        protected static ParameterExpression FindParameter(ICollection<MemberBinding> bindings)
        {
            foreach (MemberBinding binding in bindings)
            {
                ParameterExpression result;
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        MemberAssignment assignment = (MemberAssignment)binding;
                        result = FindParameter(assignment.Expression);
                        break;

                    case MemberBindingType.ListBinding:
                        MemberListBinding list = (MemberListBinding)binding;
                        result = FindParameter(list.Initializers);
                        break;

                    case MemberBindingType.MemberBinding:
                        MemberMemberBinding member = (MemberMemberBinding)binding;
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

        protected Expression Rewrite(Expression expression, ReadOnlyCollection<ParameterExpression> lambdaParameters, bool isLocationExpression, out bool abort)
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

                    BinaryExpression binaryExpression = (BinaryExpression)expression;
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

                    LambdaExpression conversion = (LambdaExpression)Rewrite(binaryExpression.Conversion, lambdaParameters, out abort);
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

                    ConditionalExpression conditional = (ConditionalExpression)expression;
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

                    InvocationExpression invocation = (InvocationExpression)expression;
                    expr1 = Rewrite(invocation.Expression, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    arguments = null;
                    tmpArguments = invocation.Arguments;
                    Fx.Assert(tmpArguments != null, "InvocationExpression.Arguments must not be null");
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

                    LambdaExpression lambda = (LambdaExpression)expression;
                    expr1 = Rewrite(lambda.Body, lambda.Parameters, isLocationExpression, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    return Expression.Lambda(lambda.Type, expr1, lambda.Parameters);

                case ExpressionType.ListInit:

                    ListInitExpression listInit = (ListInitExpression)expression;
                    newExpression = (NewExpression)Rewrite(listInit.NewExpression, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    ReadOnlyCollection<ElementInit> tmpInitializers = listInit.Initializers;
                    Fx.Assert(tmpInitializers != null, "ListInitExpression.Initializers must not be null");
                    List<ElementInit> initializers = new List<ElementInit>(tmpInitializers.Count);
                    for (i = 0; i < tmpInitializers.Count; i++)
                    {
                        tmpArguments = tmpInitializers[i].Arguments;
                        Fx.Assert(tmpArguments != null, "ElementInit.Arguments must not be null");
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
                    ParameterExpression variableExpression = (ParameterExpression)expression;
                    {
                        if (lambdaParameters != null && lambdaParameters.Contains(variableExpression))
                        {
                            return variableExpression;
                        }

                        FindMatch findMatch;
                        if (isShortCutRewrite)
                        {
                            // 
                            //  this is the opportunity to inspect whether the cached LambdaExpression(raw expression tree)
                            // does coincide with the current LocationReferenceEnvironment.
                            // If any mismatch discovered, it immediately returns NULL, indicating cache lookup failure.
                            //                         
                            findMatch = delegateFindLocationReferenceMatchShortcut;
                        }
                        else
                        {
                            // 
                            // variable(LocationReference) resolution process
                            // Note that the non-shortcut compilation pass always gaurantees successful variable resolution here.
                            //
                            findMatch = delegateFindFirstLocationReferenceMatch;
                        }

                        bool foundMultiple;
                        LocationReference finalReference = FindLocationReferencesFromEnvironment(
                            environment,
                            findMatch,
                            variableExpression.Name,
                            variableExpression.Type,
                            out foundMultiple);

                        if (finalReference != null && !foundMultiple)
                        {
                            if (publicAccessor != null)
                            {
                                CodeActivityPublicEnvironmentAccessor localPublicAccessor = publicAccessor.Value;

                                LocationReference inlinedReference;
                                if (ExpressionUtilities.TryGetInlinedReference(localPublicAccessor,
                                    finalReference, isLocationExpression, out inlinedReference))
                                {
                                    finalReference = inlinedReference;
                                }
                            }
                            return ExpressionUtilities.CreateIdentifierExpression(finalReference);
                        }

                        if (isShortCutRewrite)
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
                    }

                case ExpressionType.MemberAccess:

                    MemberExpression memberExpression = (MemberExpression)expression;

                    // When creating a location for a member on a struct, we also need a location
                    // for the struct (so we don't just set the member on a copy of the struct)
                    bool subTreeIsLocationExpression = isLocationExpression && memberExpression.Member.DeclaringType.IsValueType;

                    expr1 = Rewrite(memberExpression.Expression, lambdaParameters, subTreeIsLocationExpression, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    return Expression.MakeMemberAccess(expr1, memberExpression.Member);

                case ExpressionType.MemberInit:

                    MemberInitExpression memberInit = (MemberInitExpression)expression;
                    newExpression = (NewExpression)Rewrite(memberInit.NewExpression, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }

                    ReadOnlyCollection<MemberBinding> tmpMemberBindings = memberInit.Bindings;
                    Fx.Assert(tmpMemberBindings != null, "MemberInitExpression.Bindings must not be null");
                    List<MemberBinding> bindings = new List<MemberBinding>(tmpMemberBindings.Count);
                    for (i = 0; i < tmpMemberBindings.Count; i++)
                    {
                        MemberBinding binding = Rewrite(tmpMemberBindings[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }
                        bindings.Add(binding);
                    }
                    return Expression.MemberInit(newExpression, bindings);

                case ExpressionType.ArrayIndex:

                    // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                    MethodCallExpression arrayIndex = expression as MethodCallExpression;
                    if (arrayIndex != null)
                    {
                        expr1 = Rewrite(arrayIndex.Object, lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }
                        tmpArguments = arrayIndex.Arguments;
                        Fx.Assert(tmpArguments != null, "MethodCallExpression.Arguments must not be null");
                        List<Expression> indexes = new List<Expression>(tmpArguments.Count);
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
                    BinaryExpression alternateIndex = (BinaryExpression)expression;
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

                    MethodCallExpression methodCall = (MethodCallExpression)expression;
                    expr1 = Rewrite(methodCall.Object, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    arguments = null;
                    tmpArguments = methodCall.Arguments;
                    Fx.Assert(tmpArguments != null, "MethodCallExpression.Arguments must not be null");
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

                    NewArrayExpression newArray = (NewArrayExpression)expression;
                    ReadOnlyCollection<Expression> tmpExpressions = newArray.Expressions;
                    Fx.Assert(tmpExpressions != null, "NewArrayExpression.Expressions must not be null");
                    List<Expression> arrayInitializers = new List<Expression>(tmpExpressions.Count);
                    for (i = 0; i < tmpExpressions.Count; i++)
                    {
                        expr1 = Rewrite(tmpExpressions[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }
                        arrayInitializers.Add(expr1);
                    }
                    return Expression.NewArrayInit(newArray.Type.GetElementType(), arrayInitializers);

                case ExpressionType.NewArrayBounds:

                    NewArrayExpression newArrayBounds = (NewArrayExpression)expression;
                    tmpExpressions = newArrayBounds.Expressions;
                    Fx.Assert(tmpExpressions != null, "NewArrayExpression.Expressions must not be null");
                    List<Expression> bounds = new List<Expression>(tmpExpressions.Count);
                    for (i = 0; i < tmpExpressions.Count; i++)
                    {
                        expr1 = Rewrite(tmpExpressions[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }
                        bounds.Add(expr1);
                    }
                    return Expression.NewArrayBounds(newArrayBounds.Type.GetElementType(), bounds);

                case ExpressionType.New:

                    newExpression = (NewExpression)expression;
                    if (newExpression.Constructor == null)
                    {
                        // must be creating a valuetype
                        Fx.Assert(newExpression.Arguments.Count == 0, "NewExpression with null Constructor but some arguments");
                        return expression;
                    }
                    arguments = null;
                    tmpArguments = newExpression.Arguments;
                    Fx.Assert(tmpArguments != null, "NewExpression.Arguments must not be null");
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

                    TypeBinaryExpression typeBinary = (TypeBinaryExpression)expression;
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

                    UnaryExpression unary = (UnaryExpression)expression;
                    expr1 = Rewrite(unary.Operand, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    return Expression.MakeUnary(unary.NodeType, expr1, unary.Type, unary.Method);

                case ExpressionType.UnaryPlus:

                    UnaryExpression unaryPlus = (UnaryExpression)expression;
                    expr1 = Rewrite(unaryPlus.Operand, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    return Expression.UnaryPlus(expr1, unaryPlus.Method);

                // Expression Tree V2.0 types. This is due to the hosted VB compiler generating ET V2.0 nodes
                case ExpressionType.Block:

                    BlockExpression block = (BlockExpression)expression;
                    ReadOnlyCollection<ParameterExpression> tmpVariables = block.Variables;
                    Fx.Assert(tmpVariables != null, "BlockExpression.Variables must not be null");
                    List<ParameterExpression> parameterList = new List<ParameterExpression>(tmpVariables.Count);
                    for (i = 0; i < tmpVariables.Count; i++)
                    {
                        ParameterExpression param = (ParameterExpression)Rewrite(tmpVariables[i], lambdaParameters, out abort);
                        if (abort)
                        {
                            return null;
                        }
                        parameterList.Add(param);
                    }
                    tmpExpressions = block.Expressions;
                    Fx.Assert(tmpExpressions != null, "BlockExpression.Expressions must not be null");
                    List<Expression> expressionList = new List<Expression>(tmpExpressions.Count);
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

                    BinaryExpression assign = (BinaryExpression)expression;
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

        protected static LocationReference FindLocationReferencesFromEnvironment(LocationReferenceEnvironment environment, FindMatch findMatch, string targetName, Type targetType, out bool foundMultiple)
        {
            LocationReferenceEnvironment currentEnvironment = environment;
            foundMultiple = false;
            while (currentEnvironment != null)
            {
                LocationReference toReturn = null;
                foreach (LocationReference reference in currentEnvironment.GetLocationReferences())
                {
                    bool terminateSearch;
                    if (findMatch(reference, targetName, targetType, out terminateSearch))
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
        protected static void EnsureTypeReferencedRecurse(Type type, HashSet<Type> alreadyVisited)
        {
            if (alreadyVisited.Contains(type))
            {
                // this prevents circular reference
                // example), class Foo : IBar<Foo>
                return;
            }

            alreadyVisited.Add(type);

            // make sure any interfaces needed by this type are referenced
            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; ++i)
            {
                EnsureTypeReferencedRecurse(interfaces[i], alreadyVisited);
            }

            // same for base types
            Type baseType = type.BaseType;
            while ((baseType != null) && (baseType != TypeHelper.ObjectType))
            {
                EnsureTypeReferencedRecurse(baseType, alreadyVisited);
                baseType = baseType.BaseType;
            }

            // for generic types, all type arguments
            if (type.IsGenericType)
            {
                Type[] typeArgs = type.GetGenericArguments();
                for (int i = 1; i < typeArgs.Length; ++i)
                {
                    EnsureTypeReferencedRecurse(typeArgs[i], alreadyVisited);
                }
            }

            // array types
            if (type.HasElementType)
            {
                EnsureTypeReferencedRecurse(type.GetElementType(), alreadyVisited);
            }

            return;
        }

        private MemberBinding Rewrite(MemberBinding binding, ReadOnlyCollection<ParameterExpression> lambdaParameters, out bool abort)
        {
            int i;
            int j;
            Expression expr1;
            ReadOnlyCollection<Expression> tmpArguments;
            abort = false;
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:

                    MemberAssignment assignment = (MemberAssignment)binding;
                    expr1 = Rewrite(assignment.Expression, lambdaParameters, out abort);
                    if (abort)
                    {
                        return null;
                    }
                    return Expression.Bind(assignment.Member, expr1);

                case MemberBindingType.ListBinding:

                    MemberListBinding list = (MemberListBinding)binding;
                    List<ElementInit> initializers = null;
                    ReadOnlyCollection<ElementInit> tmpInitializers = list.Initializers;
                    Fx.Assert(tmpInitializers != null, "MemberListBinding.Initializers must not be null");
                    if (tmpInitializers.Count > 0)
                    {
                        initializers = new List<ElementInit>(tmpInitializers.Count);
                        for (i = 0; i < tmpInitializers.Count; i++)
                        {
                            List<Expression> arguments = null;
                            tmpArguments = tmpInitializers[i].Arguments;
                            Fx.Assert(tmpArguments != null, "ElementInit.Arguments must not be null");
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
                            initializers.Add(Expression.ElementInit(tmpInitializers[i].AddMethod, arguments));
                        }
                    }
                    return Expression.ListBind(list.Member, initializers);

                case MemberBindingType.MemberBinding:

                    MemberMemberBinding member = (MemberMemberBinding)binding;
                    ReadOnlyCollection<MemberBinding> tmpBindings = member.Bindings;
                    Fx.Assert(tmpBindings != null, "MemberMeberBinding.Bindings must not be null");
                    List<MemberBinding> bindings = new List<MemberBinding>(tmpBindings.Count);
                    for (i = 0; i < tmpBindings.Count; i++)
                    {
                        MemberBinding item = Rewrite(tmpBindings[i], lambdaParameters, out abort);
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

        private Expression Rewrite(Expression expression, ReadOnlyCollection<ParameterExpression> lambdaParameters, out bool abort)
        {
            return Rewrite(expression, lambdaParameters, false, out abort);
        }

        private static void ExtractNamespacesAndReferences(VisualBasicSettings vbSettings, IList<string> namespaces, IList<AssemblyReference> assemblies)
        {
            foreach (VisualBasicImportReference importReference in vbSettings.ImportReferences)
            {
                namespaces.Add(importReference.Import);
                assemblies.Add(new AssemblyReference
                {
                    Assembly = importReference.EarlyBoundAssembly,
                    AssemblyName = importReference.AssemblyName
                });
            }
        }

        protected ExpressionToCompile ExpressionToCompile(Func<string, Type> variableTypeGetter, Type lambdaReturnType) =>
            new ExpressionToCompile(TextToCompile, namespaceImports)
            {
                VariableTypeGetter = variableTypeGetter,
                LambdaReturnType = lambdaReturnType,
            };
    }
}
