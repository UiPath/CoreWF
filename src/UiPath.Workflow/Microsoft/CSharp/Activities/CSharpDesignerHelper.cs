namespace Microsoft.CSharp.Activities
{
    using Microsoft.VisualBasic.Activities;
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Expressions;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Activities.Validation;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class CSharpDesignerHelper
    {
        static readonly Type CSharpExpressionFactoryType = typeof(CSharpExpressionFactory<>);
        static readonly CSharpNameShadowingConstraint nameShadowingConstraint = new CSharpNameShadowingConstraint();

        // Returns the additional constraint for C# which enforces variable name shadowing for 
        // projects targeting 4.0 for backward compatibility. 
        public static Constraint NameShadowingConstraint
        {
            get
            {
                return nameShadowingConstraint;
            }
        }

        // Recompile the CSharpValue passed in, with its current LocationReferenceEnvironment context
        // in a weakly-typed manner (the argument CSharpValue's type argument is ignored)
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity RecompileCSharpValue(ActivityWithResult cSharpValue,
            out Type returnType,
            out SourceExpressionException compileError,
            out VisualBasicSettings visualBasicSettings)
        {
            ITextExpression textExpression = cSharpValue as ITextExpression;
            if (textExpression == null || textExpression.Language != CSharpHelper.Language)
            {
                // the argument must be of type CSharpValue<>
                throw FxTrace.Exception.AsError(new ArgumentException());
            }
            string expressionText = textExpression.ExpressionText;
            LocationReferenceEnvironment environment = cSharpValue.GetParentEnvironment();

            List<string> namespaces;
            List<string> referencedAssemblies;
            GetAllImportReferences(cSharpValue, out namespaces, out referencedAssemblies);

            return CreatePrecompiledCSharpValue(
                null,
                expressionText,
                namespaces,
                referencedAssemblies,
                environment,
                out returnType,
                out compileError,
                out visualBasicSettings);
        }

        // Recompile the CSharpReference passed in, with its current LocationReferenceEnvironment context
        // in a weakly-typed manner (the argument CSharpReference's type argument is ignored)
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity RecompileCSharpReference(ActivityWithResult cSharpReference,
            out Type returnType,
            out SourceExpressionException compileError,
            out VisualBasicSettings visualBasicSettings)
        {
            ITextExpression textExpression = cSharpReference as ITextExpression;
            if (textExpression == null || textExpression.Language != CSharpHelper.Language)
            {
                // the argument must be of type CSharpReference<>
                throw FxTrace.Exception.AsError(new ArgumentException());
            }
            string expressionText = textExpression.ExpressionText;
            LocationReferenceEnvironment environment = cSharpReference.GetParentEnvironment();

            List<string> namespaces;
            List<string> referencedAssemblies;
            GetAllImportReferences(cSharpReference, out namespaces, out referencedAssemblies);

            return CreatePrecompiledCSharpReference(
                null,
                expressionText,
                namespaces,
                referencedAssemblies,
                environment,
                out returnType,
                out compileError,
                out visualBasicSettings);
        }

        // create a pre-compiled CSharpValueExpression, and also provides expressin type back to the caller.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity CreatePrecompiledCSharpValue(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment,
            out Type returnType,
            out SourceExpressionException compileError,
            out VisualBasicSettings visualBasicSettings)
        {
            LambdaExpression lambda = null;
            HashSet<string> namespacesSet = new HashSet<string>();
            HashSet<AssemblyName> assembliesSet = new HashSet<AssemblyName>();
            compileError = null;
            returnType = null;

            if (namespaces != null)
            {
                foreach (string ns in namespaces)
                {
                    if (ns != null)
                    {
                        namespacesSet.Add(ns);
                    }
                }
            }

            if (referencedAssemblies != null)
            {
                foreach (string assm in referencedAssemblies)
                {
                    if (assm != null)
                    {
                        assembliesSet.Add(new AssemblyName(assm));
                    }
                }
            }

            CSharpHelper cSharpHelper = new CSharpHelper(expressionText, assembliesSet, namespacesSet);
            if (targetType == null)
            {
                try
                {
                    lambda = cSharpHelper.CompileNonGeneric(environment);
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
                MethodInfo genericCompileMethod = typeof(CSharpHelper).GetMethod("Compile", new Type[] { typeof(LocationReferenceEnvironment) });
                genericCompileMethod = genericCompileMethod.MakeGenericMethod(new Type[] { targetType });
                try
                {
                    lambda = (LambdaExpression)genericCompileMethod.Invoke(cSharpHelper, new object[] { environment });
                    returnType = targetType;
                }
                catch (TargetInvocationException e)
                {
                    SourceExpressionException se = e.InnerException as SourceExpressionException;
                    if (se != null)
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

            visualBasicSettings = new VisualBasicSettings();
            if (lambda != null)
            {
                HashSet<Type> typeReferences = new HashSet<Type>();
                FindTypeReferences(lambda.Body, typeReferences);
                foreach (Type type in typeReferences)
                {
                    Assembly tassembly = type.Assembly;
                    if (tassembly.IsDynamic)
                    {
                        continue;
                    }
                    string assemblyName = CSharpHelper.GetFastAssemblyName(tassembly).Name;
                    VisualBasicImportReference importReference = new VisualBasicImportReference { Assembly = assemblyName, Import = type.Namespace };
                    visualBasicSettings.ImportReferences.Add(importReference);
                }
            }

            Type concreteHelperType = CSharpExpressionFactoryType.MakeGenericType(targetType);
            CSharpExpressionFactory expressionFactory = (CSharpExpressionFactory)Activator.CreateInstance(concreteHelperType);

            return expressionFactory.CreateCSharpValue(expressionText);
        }

        // create a pre-compiled CSharpValueExpression, and also provides expressin type back to the caller.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity CreatePrecompiledCSharpReference(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment,
            out Type returnType,
            out SourceExpressionException compileError,
            out VisualBasicSettings visualBasicSettings)
        {
            LambdaExpression lambda = null;
            HashSet<string> namespacesSet = new HashSet<string>();
            HashSet<AssemblyName> assembliesSet = new HashSet<AssemblyName>();
            compileError = null;
            returnType = null;

            if (namespaces != null)
            {
                foreach (string ns in namespaces)
                {
                    if (ns != null)
                    {
                        namespacesSet.Add(ns);
                    }
                }
            }

            if (referencedAssemblies != null)
            {
                foreach (string assm in referencedAssemblies)
                {
                    if (assm != null)
                    {
                        assembliesSet.Add(new AssemblyName(assm));
                    }
                }
            }

            CSharpHelper cSharpHelper = new CSharpHelper(expressionText, assembliesSet, namespacesSet);
            if (targetType == null)
            {
                try
                {
                    lambda = cSharpHelper.CompileNonGeneric(environment);
                    if (lambda != null)
                    {
                        // inspect the expressionTree to see if it is a valid location expression(L-value)
                        string extraErrorMessage;
                        if (!ExpressionUtilities.IsLocation(lambda, targetType, out extraErrorMessage))
                        {
                            string errorMessage = SR.InvalidLValueExpression;
                            if (extraErrorMessage != null)
                            {
                                errorMessage += ":" + extraErrorMessage;
                            }
                            throw FxTrace.Exception.AsError(
                                new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionText, errorMessage)));
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
                MethodInfo genericCompileMethod = typeof(CSharpHelper).GetMethod("Compile", new Type[] { typeof(LocationReferenceEnvironment) });
                genericCompileMethod = genericCompileMethod.MakeGenericMethod(new Type[] { targetType });
                try
                {
                    lambda = (LambdaExpression)genericCompileMethod.Invoke(cSharpHelper, new object[] { environment });
                    // inspect the expressionTree to see if it is a valid location expression(L-value)
                    string extraErrorMessage = null;
                    if (!ExpressionUtilities.IsLocation(lambda, targetType, out extraErrorMessage))
                    {
                        string errorMessage = SR.InvalidLValueExpression;
                        if (extraErrorMessage != null)
                        {
                            errorMessage += ":" + extraErrorMessage;
                        }
                        throw FxTrace.Exception.AsError(
                            new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionText, errorMessage)));
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
                    SourceExpressionException se = e.InnerException as SourceExpressionException;
                    if (se != null)
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

            visualBasicSettings = new VisualBasicSettings();
            if (lambda != null)
            {
                HashSet<Type> typeReferences = new HashSet<Type>();
                FindTypeReferences(lambda.Body, typeReferences);
                foreach (Type type in typeReferences)
                {
                    Assembly tassembly = type.Assembly;
                    if (tassembly.IsDynamic)
                    {
                        continue;
                    }
                    string assemblyName = CSharpHelper.GetFastAssemblyName(tassembly).Name;
                    VisualBasicImportReference importReference = new VisualBasicImportReference { Assembly = assemblyName, Import = type.Namespace };
                    visualBasicSettings.ImportReferences.Add(importReference);
                }
            }

            Type concreteHelperType = CSharpExpressionFactoryType.MakeGenericType(targetType);
            CSharpExpressionFactory expressionFactory = (CSharpExpressionFactory)Activator.CreateInstance(concreteHelperType);

            return expressionFactory.CreateCSharpReference(expressionText);
        }


        static void EnsureTypeReferenced(Type type, bool isDirectReference, HashSet<Type> typeReferences)
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
                    Type[] typeArgs = type.GetGenericArguments();
                    for (int i = 1; i < typeArgs.Length; ++i)
                    {
                        EnsureTypeReferencedRecurse(typeArgs[i], isDirectReference, typeReferences);
                    }
                }
            }
        }

        static void EnsureTypeReferencedRecurse(Type type, bool isDirectReference, HashSet<Type> typeReferences)
        {
            if (typeReferences.Contains(type))
            {
                return;
            }

            // don't add base types/interfaces if they're in the default set (or we'll get superfluous xmlns references)
            if (isDirectReference || !CSharpHelper.DefaultReferencedAssemblies.Contains(type.Assembly))
            {
                typeReferences.Add(type);
            }

            // make sure any interfaces needed by this type are referenced
            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; ++i)
            {
                EnsureTypeReferencedRecurse(interfaces[i], false, typeReferences);
            }

            // same for base types
            Type baseType = type.BaseType;
            while ((baseType != null) && (baseType != TypeHelper.ObjectType))
            {
                EnsureTypeReferencedRecurse(baseType, false, typeReferences);
                baseType = baseType.BaseType;
            }
        }

        static void FindTypeReferences(Expression expression, HashSet<Type> typeReferences)
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
                    BinaryExpression binaryExpression = (BinaryExpression)expression;
                    FindTypeReferences(binaryExpression.Left, typeReferences);
                    FindTypeReferences(binaryExpression.Right, typeReferences);
                    return;

                case ExpressionType.Conditional:
                    ConditionalExpression conditional = (ConditionalExpression)expression;
                    FindTypeReferences(conditional.Test, typeReferences);
                    FindTypeReferences(conditional.IfTrue, typeReferences);
                    FindTypeReferences(conditional.IfFalse, typeReferences);
                    return;

                case ExpressionType.Constant:
                    ConstantExpression constantExpr = (ConstantExpression)expression;
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
                    InvocationExpression invocation = (InvocationExpression)expression;
                    FindTypeReferences(invocation.Expression, typeReferences);
                    for (int i = 0; i < invocation.Arguments.Count; i++)
                    {
                        FindTypeReferences(invocation.Arguments[i], typeReferences);
                    }
                    return;

                case ExpressionType.Lambda:
                    LambdaExpression lambda = (LambdaExpression)expression;
                    FindTypeReferences(lambda.Body, typeReferences);
                    for (int i = 0; i < lambda.Parameters.Count; i++)
                    {
                        FindTypeReferences(lambda.Parameters[i], typeReferences);
                    }
                    return;

                case ExpressionType.ListInit:
                    ListInitExpression listInit = (ListInitExpression)expression;
                    FindTypeReferences(listInit.NewExpression, typeReferences);
                    for (int i = 0; i < listInit.Initializers.Count; i++)
                    {
                        ReadOnlyCollection<Expression> arguments = listInit.Initializers[i].Arguments;
                        for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                        {
                            FindTypeReferences(arguments[argumentIndex], typeReferences);
                        }
                    }
                    return;

                case ExpressionType.Parameter:
                    ParameterExpression paramExpr = (ParameterExpression)expression;
                    EnsureTypeReferenced(paramExpr.Type, false, typeReferences);
                    return;

                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)expression;
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
                    MemberInitExpression memberInit = (MemberInitExpression)expression;
                    FindTypeReferences(memberInit.NewExpression, typeReferences);
                    ReadOnlyCollection<MemberBinding> bindings = memberInit.Bindings;
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        FindTypeReferences(bindings[i], typeReferences);
                    }
                    return;

                case ExpressionType.ArrayIndex:
                    // ArrayIndex can be a MethodCallExpression or a BinaryExpression
                    MethodCallExpression arrayIndex = expression as MethodCallExpression;
                    if (arrayIndex != null)
                    {
                        FindTypeReferences(arrayIndex.Object, typeReferences);
                        ReadOnlyCollection<Expression> arguments = arrayIndex.Arguments;
                        for (int i = 0; i < arguments.Count; i++)
                        {
                            FindTypeReferences(arguments[i], typeReferences);
                        }
                        return;
                    }
                    BinaryExpression alternateIndex = (BinaryExpression)expression;
                    FindTypeReferences(alternateIndex.Left, typeReferences);
                    FindTypeReferences(alternateIndex.Right, typeReferences);
                    return;

                case ExpressionType.Call:
                    MethodCallExpression methodCall = (MethodCallExpression)expression;
                    MethodInfo method = methodCall.Method;
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
                        Type[] typeArgs = method.GetGenericArguments();
                        for (int i = 1; i < typeArgs.Length; ++i)
                        {
                            EnsureTypeReferenced(typeArgs[i], true, typeReferences);
                        }
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters != null)
                    {
                        foreach (ParameterInfo parameter in parameters)
                        {
                            EnsureTypeReferenced(parameter.ParameterType, false, typeReferences);
                        }
                    }

                    ReadOnlyCollection<Expression> callArguments = methodCall.Arguments;
                    for (int i = 0; i < callArguments.Count; i++)
                    {
                        FindTypeReferences(callArguments[i], typeReferences);
                    }
                    return;

                case ExpressionType.NewArrayInit:
                    NewArrayExpression newArray = (NewArrayExpression)expression;
                    EnsureTypeReferenced(newArray.Type.GetElementType(), true, typeReferences);
                    ReadOnlyCollection<Expression> expressions = newArray.Expressions;
                    for (int i = 0; i < expressions.Count; i++)
                    {
                        FindTypeReferences(expressions[i], typeReferences);
                    }
                    return;

                case ExpressionType.NewArrayBounds:
                    NewArrayExpression newArrayBounds = (NewArrayExpression)expression;
                    EnsureTypeReferenced(newArrayBounds.Type.GetElementType(), true, typeReferences);
                    ReadOnlyCollection<Expression> boundExpressions = newArrayBounds.Expressions;
                    for (int i = 0; i < boundExpressions.Count; i++)
                    {
                        FindTypeReferences(boundExpressions[i], typeReferences);
                    }
                    return;

                case ExpressionType.New:
                    NewExpression newExpression = (NewExpression)expression;
                    if (newExpression.Constructor != null)
                    {
                        EnsureTypeReferenced(newExpression.Constructor.DeclaringType, true, typeReferences);
                    }
                    else
                    {
                        // if no constructors defined (e.g. structs), the simply use the type
                        EnsureTypeReferenced(newExpression.Type, true, typeReferences);
                    }
                    ReadOnlyCollection<Expression> ctorArguments = newExpression.Arguments;
                    for (int i = 0; i < ctorArguments.Count; i++)
                    {
                        FindTypeReferences(ctorArguments[i], typeReferences);
                    }
                    return;

                case ExpressionType.TypeIs:
                    TypeBinaryExpression typeBinary = (TypeBinaryExpression)expression;
                    FindTypeReferences(typeBinary.Expression, typeReferences);
                    EnsureTypeReferenced(typeBinary.TypeOperand, true, typeReferences);
                    return;

                case ExpressionType.TypeAs:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    UnaryExpression unary = (UnaryExpression)expression;
                    FindTypeReferences(unary.Operand, typeReferences);
                    EnsureTypeReferenced(unary.Type, true, typeReferences);
                    return;

                case ExpressionType.ArrayLength:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.UnaryPlus:
                    UnaryExpression unaryExpression = (UnaryExpression)expression;
                    FindTypeReferences(unaryExpression.Operand, typeReferences);
                    return;

                // Expression Tree V2.0 types.  This is due to the hosted C# compiler generating ET V2.0 nodes

                case ExpressionType.Block:
                    BlockExpression block = (BlockExpression)expression;
                    ReadOnlyCollection<ParameterExpression> variables = block.Variables;
                    for (int i = 0; i < variables.Count; i++)
                    {
                        FindTypeReferences(variables[i], typeReferences);
                    }
                    ReadOnlyCollection<Expression> blockExpressions = block.Expressions;
                    for (int i = 0; i < blockExpressions.Count; i++)
                    {
                        FindTypeReferences(blockExpressions[i], typeReferences);
                    }
                    return;

                case ExpressionType.Assign:
                    BinaryExpression assign = (BinaryExpression)expression;
                    FindTypeReferences(assign.Left, typeReferences);
                    FindTypeReferences(assign.Right, typeReferences);
                    return;
            }

            Fx.Assert("Don't understand expression type " + expression.NodeType);
            return;
        }

        static void FindTypeReferences(MemberBinding binding, HashSet<Type> typeReferences)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    MemberAssignment assignment = (MemberAssignment)binding;
                    FindTypeReferences(assignment.Expression, typeReferences);
                    return;

                case MemberBindingType.ListBinding:
                    MemberListBinding list = (MemberListBinding)binding;
                    ReadOnlyCollection<ElementInit> initializers = list.Initializers;
                    for (int i = 0; i < initializers.Count; i++)
                    {
                        ReadOnlyCollection<Expression> arguments = initializers[i].Arguments;
                        for (int j = 0; j < arguments.Count; j++)
                        {
                            FindTypeReferences(arguments[j], typeReferences);
                        }
                    }
                    return;

                case MemberBindingType.MemberBinding:
                    MemberMemberBinding member = (MemberMemberBinding)binding;
                    ReadOnlyCollection<MemberBinding> bindings = member.Bindings;
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        FindTypeReferences(bindings[i], typeReferences);
                    }
                    return;

                default:
                    Fx.Assert("MemberBinding type '" + binding.BindingType + "' is not supported.");
                    return;
            }
        }

        static void GetAllImportReferences(Activity activity, out List<string> namespaces, out List<string> assemblies)
        {
            List<AssemblyReference> referencedAssemblies;
            CSharpHelper.GetAllImportReferences(activity, true, out namespaces, out referencedAssemblies);

            assemblies = new List<string>();
            foreach (AssemblyReference reference in referencedAssemblies)
            {
                if (reference.AssemblyName != null)
                {
                    assemblies.Add(reference.AssemblyName.FullName);
                }
                else if (reference.Assembly != null)
                {
                    assemblies.Add(reference.Assembly.FullName);
                }
            }
        }

        // to perform the generics dance around CSharpValue/Reference we need these helpers
        abstract class CSharpExpressionFactory
        {
            public abstract Activity CreateCSharpValue(string expressionText);
            public abstract Activity CreateCSharpReference(string expressionText);
        }

        class CSharpExpressionFactory<T> : CSharpExpressionFactory
        {
            public override Activity CreateCSharpReference(string expressionText)
            {
                return new CSharpReference<T>()
                {
                    ExpressionText = expressionText
                };
            }

            public override Activity CreateCSharpValue(string expressionText)
            {
                return new CSharpValue<T>()
                {
                    ExpressionText = expressionText
                };
            }
        }
    }
}