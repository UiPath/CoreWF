// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions;

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections;
using System.Activities.Internals;
using System.Activities.Runtime;

public static class ExpressionServices
{
    // Reflection is used to call generic function because type information are only known at runtime.
    private static readonly MethodInfo TryConvertBinaryExpressionHandle = typeof(ExpressionServices).GetMethod("TryConvertBinaryExpressionWorker", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo TryConvertUnaryExpressionHandle = typeof(ExpressionServices).GetMethod("TryConvertUnaryExpressionWorker", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo TryConvertMemberExpressionHandle = typeof(ExpressionServices).GetMethod("TryConvertMemberExpressionWorker", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo TryConvertArgumentExpressionHandle = typeof(ExpressionServices).GetMethod("TryConvertArgumentExpressionWorker", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo TryConvertReferenceMemberExpressionHandle = typeof(ExpressionServices).GetMethod("TryConvertReferenceMemberExpressionWorker", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo TryConvertIndexerReferenceHandle = typeof(ExpressionServices).GetMethod("TryConvertIndexerReferenceWorker", BindingFlags.NonPublic | BindingFlags.Static);

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "The parameter is restricted correctly.")]
    public static Activity<TResult> Convert<TResult>(Expression<Func<ActivityContext, TResult>> expression)
    {
        if (expression == null)
        {
            throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(expression), SR.ExpressionRequiredForConversion));
        }
        TryConvert(expression.Body, true, out Activity<TResult> result);
        return result;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "The parameter is restricted correctly.")]
    public static bool TryConvert<TResult>(Expression<Func<ActivityContext, TResult>> expression, out Activity<TResult> result)
    {
        if (expression == null)
        {
            result = null;
            return false;
        }
        return TryConvert(expression.Body, false, out result) == null;
    }

    private static string TryConvert<TResult>(Expression body, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        switch (body)
        {
            case UnaryExpression unaryExpressionBody:
                {
                    Type operandType = unaryExpressionBody.Operand.Type;
                    return TryConvertUnaryExpression(unaryExpressionBody, operandType, throwOnError, out result);
                }

            case BinaryExpression binaryExpressionBody:
                {
                    Type leftType = binaryExpressionBody.Left.Type;
                    Type rightType = binaryExpressionBody.Right.Type;
                    if (binaryExpressionBody.NodeType == ExpressionType.ArrayIndex)
                    {
                        return TryConvertArrayItemValue(binaryExpressionBody, leftType, rightType, throwOnError, out result);
                    }
                    return TryConvertBinaryExpression(binaryExpressionBody, leftType, rightType, throwOnError, out result);
                }

            case MemberExpression memberExpressionBody:
                {
                    Type memberType = memberExpressionBody.Expression == null ? memberExpressionBody.Member.DeclaringType : memberExpressionBody.Expression.Type;
                    return TryConvertMemberExpression(memberExpressionBody, memberType, throwOnError, out result);
                }

            case MethodCallExpression methodCallExpressionBody:
                {
                    MethodInfo calledMethod = methodCallExpressionBody.Method;
                    Type declaringType = calledMethod.DeclaringType;
                    ParameterInfo[] parameters = calledMethod.GetParameters();
                    if (TypeHelper.AreTypesCompatible(declaringType, typeof(Variable)) && calledMethod.Name == "Get" && parameters.Length == 1
                        && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertVariableValue(methodCallExpressionBody, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(Argument))
                        && calledMethod.Name == "Get" && parameters.Length == 1 && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertArgumentValue(methodCallExpressionBody.Object as MemberExpression, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(DelegateArgument))
                        && calledMethod.Name == "Get" && parameters.Length == 1 && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertDelegateArgumentValue(methodCallExpressionBody, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(ActivityContext)) && calledMethod.Name == "GetValue" && parameters.Length == 1
                        && (TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(Argument)) || TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(RuntimeArgument))))
                    {
                        MemberExpression memberExpression = methodCallExpressionBody.Arguments[0] as MemberExpression;
                        return TryConvertArgumentValue(memberExpression, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(ActivityContext)) && calledMethod.Name == "GetValue" && parameters.Length == 1
                        && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(LocationReference)))
                    {
                        return TryConvertLocationReference(methodCallExpressionBody, throwOnError, out result);
                    }
                    else
                    {
                        return TryConvertMethodCallExpression(methodCallExpressionBody, throwOnError, out result);
                    }
                }

            case InvocationExpression invocationExpression:
                return TryConvertInvocationExpression(invocationExpression, throwOnError, out result);
            case NewExpression newExpression:
                return TryConvertNewExpression(newExpression, throwOnError, out result);
            case NewArrayExpression newArrayExpression when newArrayExpression.NodeType != ExpressionType.NewArrayInit:
                return TryConvertNewArrayExpression(newArrayExpression, throwOnError, out result);
            case ConstantExpression constantExpressionBody:
                // This is to handle the leaf node as a literal value
                result = new Literal<TResult> { Value = (TResult)constantExpressionBody.Value };
                return null;
        }
        if (throwOnError)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedExpressionType(body.NodeType)));
        }
        else
        {
            return SR.UnsupportedExpressionType(body.NodeType);
        }
    }        

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "The parameter is restricted correctly.")]
    public static Activity<Location<TResult>> ConvertReference<TResult>(Expression<Func<ActivityContext, TResult>> expression)
    {
        if (expression == null)
        {
            throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(expression), SR.ExpressionRequiredForConversion));
        }

        TryConvertReference(expression.Body, true, out Activity<Location<TResult>> result);
        return result;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "The parameter is restricted correctly.")]
    public static bool TryConvertReference<TResult>(Expression<Func<ActivityContext, TResult>> expression, out Activity<Location<TResult>> result)
    {
        if (expression == null)
        {
            result = null;
            return false;
        }
        return TryConvertReference(expression.Body, false, out result) == null;
    }

    private static string TryConvertReference<TResult>(Expression body, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        switch (body)
        {
            case MemberExpression memberExpressionBody:
                {
                    Type memberType = memberExpressionBody.Expression == null ? memberExpressionBody.Member.DeclaringType : memberExpressionBody.Expression.Type;
                    return TryConvertReferenceMemberExpression(memberExpressionBody, memberType, throwOnError, out result);
                }

            case BinaryExpression binaryExpressionBody:
                {
                    Type leftType = binaryExpressionBody.Left.Type;
                    Type rightType = binaryExpressionBody.Right.Type;
                    if (binaryExpressionBody.NodeType == ExpressionType.ArrayIndex)
                    {
                        return TryConvertArrayItemReference(binaryExpressionBody, leftType, rightType, throwOnError, out result);
                    }

                    break;
                }

            case MethodCallExpression methodCallExpressionBody:
                {
                    Type declaringType = methodCallExpressionBody.Method.DeclaringType;
                    MethodInfo calledMethod = methodCallExpressionBody.Method;
                    if (declaringType.IsArray && calledMethod.Name == "Get")
                    {
                        return TryConvertMultiDimensionalArrayItemReference(methodCallExpressionBody, throwOnError, out result);
                    }

                    if (calledMethod.IsSpecialName && calledMethod.Name == "get_Item")
                    {
                        return TryConvertIndexerReference(methodCallExpressionBody, throwOnError, out result);
                    }

                    ParameterInfo[] parameters = calledMethod.GetParameters();
                    if (TypeHelper.AreTypesCompatible(declaringType, typeof(Variable)) && calledMethod.Name == "Get" && parameters.Length == 1 
                        && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertVariableReference(methodCallExpressionBody, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(Argument)) && calledMethod.Name == "Get" && parameters.Length == 1 
                        && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertArgumentReference(methodCallExpressionBody, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(DelegateArgument)) && calledMethod.Name == "Get" && parameters.Length == 1 
                        && TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(ActivityContext)))
                    {
                        return TryConvertDelegateArgumentReference(methodCallExpressionBody, throwOnError, out result);
                    }
                    else if (TypeHelper.AreTypesCompatible(declaringType, typeof(ActivityContext)) && calledMethod.Name == "GetValue" && parameters.Length == 1 
                        && (TypeHelper.AreTypesCompatible(parameters[0].ParameterType, typeof(LocationReference))))
                    {
                        return TryConvertReferenceLocationReference(methodCallExpressionBody, throwOnError, out result);
                    }

                    break;
                }
        }
        if (throwOnError)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedReferenceExpressionType(body.NodeType)));
        }
        else
        {
            return SR.UnsupportedReferenceExpressionType(body.NodeType);
        }
    }

    private static string TryConvertIndexerReference<TResult>(MethodCallExpression methodCallExpressionBody, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        try
        {
            if (methodCallExpressionBody.Object == null)
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ValidationException(SR.InstanceMethodCallRequiresTargetObject));
                }
                else
                {
                    return SR.InstanceMethodCallRequiresTargetObject;
                }
            }
            MethodInfo specializedHandle = TryConvertIndexerReferenceHandle.MakeGenericMethod(methodCallExpressionBody.Object.Type, typeof(TResult));
            object[] parameters = new object[] { methodCallExpressionBody, throwOnError, null };
            string errorString = specializedHandle.Invoke(null, parameters) as string;
            result = parameters[2] as Activity<Location<TResult>>;
            return errorString;
        }
        catch (TargetInvocationException e)
        {
            throw FxTrace.Exception.AsError(e.InnerException);
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertIndexerReferenceWorker<TOperand, TResult>(MethodCallExpression methodCallExpressionBody, bool throwOnError, out Activity<Location<TResult>> result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;
        Fx.Assert(methodCallExpressionBody.Object != null, "Indexer must have a target object");
        if (!typeof(TOperand).IsValueType)
        {
            string operandError = TryConvert(methodCallExpressionBody.Object, throwOnError, out Activity<TOperand> operand);
            if (operandError != null)
            {
                return operandError;
            }
            IndexerReference<TOperand, TResult> indexerReference = new()
            {
                Operand = new InArgument<TOperand>(operand) { EvaluationOrder = 0 },
            };
            string argumentError = TryConvertArguments(methodCallExpressionBody.Arguments, indexerReference.Indices, methodCallExpressionBody.GetType(), 1, null, throwOnError);
            if (argumentError != null)
            {
                return argumentError;
            }
            result = indexerReference;

        }
        else
        {
            string operandError = TryConvertReference(methodCallExpressionBody.Object, throwOnError, out Activity<Location<TOperand>> operandReference);
            if (operandError != null)
            {
                return operandError;
            }
            ValueTypeIndexerReference<TOperand, TResult> indexerReference = new()
            {
                OperandLocation = new InOutArgument<TOperand>(operandReference) { EvaluationOrder = 0 },
            };
            string argumentError = TryConvertArguments(methodCallExpressionBody.Arguments, indexerReference.Indices, methodCallExpressionBody.GetType(), 1, null, throwOnError);
            if (argumentError != null)
            {
                return argumentError;
            }
            result = indexerReference;
        }
        return null;
    }

    private static string TryConvertMultiDimensionalArrayItemReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        if (methodCallExpression.Object == null)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.InstanceMethodCallRequiresTargetObject));
            }
            else
            {
                return SR.InstanceMethodCallRequiresTargetObject;
            }
        }
        string errorString = TryConvert(methodCallExpression.Object, throwOnError, out Activity<Array> operand);
        if (errorString != null)
        {
            return errorString;
        }

        MultidimensionalArrayItemReference<TResult> reference = new()
        {
            Array = new InArgument<Array>(operand) { EvaluationOrder = 0 },
        };
        _ = reference.Indices;
        string argumentError = TryConvertArguments(methodCallExpression.Arguments, reference.Indices, methodCallExpression.GetType(), 1, null, throwOnError);
        if (argumentError != null)
        {
            return argumentError;
        }
        result = reference;
        return null;
    }

    private static string TryConvertVariableReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        Variable variableObject = null;

        //
        // This is a fast path to handle a simple variable object.               
        //
        // Linq actually generate a temp class wrapping all the local variables.
        //
        // The real expression object look like
        // new TempClass() { A = a }.A.Get(env)
        // 
        // A is a field 

        if (methodCallExpression.Object is MemberExpression)
        {
            MemberExpression member = methodCallExpression.Object as MemberExpression;
            if (member.Expression is ConstantExpression)
            {
                ConstantExpression memberExpression = member.Expression as ConstantExpression;
                if (member.Member is FieldInfo)
                {
                    FieldInfo field = member.Member as FieldInfo;
                    variableObject = field.GetValue(memberExpression.Value) as Variable;
                    Fx.Assert(variableObject != null, "Linq generated expression tree should be correct");
                    result = new VariableReference<TResult> { Variable = variableObject };
                    return null;
                }
            }
        }

        //This is to handle the expression whose evaluation result is a variable object.
        //Limitation: The expression of variable object has to be evaludated in conversion time. It means after conversion, the variable object should not be changed any more.
        //For example, the following case is not legal:
        //
        //Program.static_X = new Variable<string> { Default = "Hello" };
        //Activity<Location<string>> weRef = ExpressionServices.ConvertReference<string>((env) => Program.static_X.Get(env));
        //Program.static_X = new Variable<string> { Default = "World" };
        //Sequence sequence = new Sequence
        //{
        //    Variables = { Program.static_X },
        //    Activities = 
        //      {
        //         new Assign<string>
        //         {
        //             To = new OutArgument<string>{Expression = weRef},
        //             Value = "haha",
        //         },
        //         new WriteLine
        //         {
        //             Text = Program.static_X,
        //         }
        //      }
        //};
        //WorkflowInvoker.Invoke(sequence);
        //
        // The reason is that "Program.static_X = new Variable<string> { Default = "World" }" happens after conversion.
        try
        {
            Expression<Func<Variable>> funcExpression = Expression.Lambda<Func<Variable>>(methodCallExpression.Object);
            Func<Variable> func = funcExpression.Compile();
            variableObject = func();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(e);
            }
            else
            {
                return e.Message;
            }
        }
        Fx.Assert(variableObject is Variable<TResult>, "Linq generated expression tree should be correct");
        result = new VariableReference<TResult> { Variable = variableObject };
        return null;
    }

    private static string TryConvertArrayItemReference<TResult>(BinaryExpression binaryExpression, Type leftType, Type rightType, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;

        //for ArrayIndex expression, Left type is always TResult[] and Right type is always int
        if (!leftType.IsArray)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerOnNonArrayType(leftType)));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerOnNonArrayType(leftType);
            }
        }
        //Because co-variance for LValue requires that TResult is compatible with actual type. However, we cannot write such a lambda expression. E,g:
        //Expression<Func<ActivityContext, DerivedClass> expr = env => a.Get(env). Here a.Get(env) returns BaseClass.  So we needn't co-viariance here.
        if (leftType.GetElementType() != typeof(TResult))
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerReferenceWithDifferentArrayTypeAndResultType(leftType, typeof(TResult))));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerReferenceWithDifferentArrayTypeAndResultType(leftType, typeof(TResult));
            }
        }
        if (rightType != typeof(int))
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerWithNonIntIndex(rightType)));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerWithNonIntIndex(rightType);
            }
        }

        string arrayError = TryConvert(binaryExpression.Left, throwOnError, out Activity<TResult[]> array);
        if (arrayError != null)
        {
            return arrayError;
        }

        string indexError = TryConvert(binaryExpression.Right, throwOnError, out Activity<int> index);
        if (indexError != null)
        {
            return indexError;
        }

        result = new ArrayItemReference<TResult>
        {
            Array = new InArgument<TResult[]>(array) { EvaluationOrder = 0 },
            Index = new InArgument<int>(index) { EvaluationOrder = 1 },
        };
        return null;
    }

    private static string TryConvertVariableValue<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        Variable variableObject = null;
            
        //
        // This is a fast path to handle a simple variable object                
        //
        // Linq actually generate a temp class wrapping all the local variables.
        //
        // The real expression object look like
        // new TempClass() { A = a }.A.Get(env)
        // 
        // A is a field 

        if (methodCallExpression.Object is MemberExpression)
        {
            MemberExpression member = methodCallExpression.Object as MemberExpression;
            if (member.Expression is ConstantExpression)
            {
                ConstantExpression memberExpression = member.Expression as ConstantExpression;
                if (member.Member is FieldInfo)
                {
                    FieldInfo field = member.Member as FieldInfo;
                    variableObject = field.GetValue(memberExpression.Value) as Variable;
                    result = new VariableValue<TResult> { Variable = variableObject };
                    return null;
                }
            }
        }

        //This is to handle the expression whose evaluation result is a variable object.
        //Limitation: The expression of variable object has to be evaludated in conversion time. It means after conversion, the variable object should not be changed any more.
        //For example, the following case is not legal:
        //
        //  Program.static_X = new Variable<string> { Default = "Hello" };
        //  Activity<string> we = ExpressionServices.Convert((env) => Program.static_X.Get(env));
        //  Program.static_X = new Variable<string> { Default = "World" };
        //  Sequence sequence = new Sequence
        //  {
        //      Variables = { Program.static_X },
        //      Activities = 
        //      {
        //             new WriteLine
        //          {
        //                 Text = new InArgument<string>{Expression = we},
        //          }
        //      }
        //  };
        //  WorkflowInvoker.Invoke(sequence);
        //
        // The reason is that "Program.static_X = new Variable<string> { Default = "World" }" happens after conversion.

        try
        {
            Expression<Func<Variable>> funcExpression = Expression.Lambda<Func<Variable>>(methodCallExpression.Object);
            Func<Variable> func = funcExpression.Compile();
            variableObject = func();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(e);
            }
            else
            {
                return e.Message;
            }
        }
        result = new VariableValue<TResult> { Variable = variableObject };
        return null;
    }

    private static string TryConvertDelegateArgumentValue<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        DelegateArgument delegateArgument = null;

        //This is to handle the expression whose evaluation result is a DelegateArgument.
        //Limitation: The expression of variable object has to be evaluated in conversion time. It means after conversion, the DelegateArgument object should not be changed any more.
        //For example, the following case is not legal:
        //
        //  Program.static_X = new DelegateInArgument<string>();
        //  Activity<string> we = ExpressionServices.Convert((env) => Program.static_X.Get(env));
        //  Program.static_X = new DelegateInArgument<string>();
        //  ActivityAction<string> activityAction = new ActivityAction<string>
        //  {
        //      Argument = Program.static_X,
        //      Handler = new WriteLine
        //          {
        //                 Text = we,
        //          }
        //      }
        //  };
        //  WorkflowInvoker.Invoke( new InvokeAction<string>
        //                          {
        //                              Argument = "Hello",
        //                              Action = activityAction,
        //                          }
        //);
        //
        // The reason is that "Program.static_X" is changed after conversion.

        try
        {
            Expression<Func<DelegateArgument>> funcExpression = Expression.Lambda<Func<DelegateArgument>>(methodCallExpression.Object);
            Func<DelegateArgument> func = funcExpression.Compile();
            delegateArgument = func();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(e);
            }
            else
            {
                return e.Message;
            }
        }
        result = new DelegateArgumentValue<TResult>(delegateArgument);
        return null;
    }

    private static string TryConvertDelegateArgumentReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        DelegateArgument delegateArgument = null;

        //This is to handle the expression whose evaluation result is a DelegateArgument.
        //Limitation: The expression of variable object has to be evaluated in conversion time. It means after conversion, the DelegateArgument object should not be changed any more.
        //For example, the following case is not legal:
        //
        //  Program.static_X = new DelegateInArgument<string>();
        //  Activity<string> we = ExpressionServices.Convert((env) => Program.static_X.Get(env));
        //  Program.static_X = new DelegateInArgument<string>();
        //  ActivityAction<string> activityAction = new ActivityAction<string>
        //  {
        //      Argument = Program.static_X,
        //      Handler = new WriteLine
        //          {
        //                 Text = we,
        //          }
        //      }
        //  };
        //  WorkflowInvoker.Invoke( new InvokeAction<string>
        //                          {
        //                              Argument = "Hello",
        //                              Action = activityAction,
        //                          }
        //);
        //
        // The reason is that "Program.static_X" is changed after conversion.

        try
        {
            Expression<Func<DelegateArgument>> funcExpression = Expression.Lambda<Func<DelegateArgument>>(methodCallExpression.Object);
            Func<DelegateArgument> func = funcExpression.Compile();
            delegateArgument = func();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(e);
            }
            else
            {
                return e.Message;
            }
        }
        result = new DelegateArgumentReference<TResult>(delegateArgument);
        return null;
    }

    private static string TryConvertArgumentValue<TResult>(MemberExpression memberExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;

        if (memberExpression != null && TypeHelper.AreTypesCompatible(memberExpression.Type, typeof(RuntimeArgument)))
        {
            RuntimeArgument ra = null;
            try
            {
                Expression<Func<RuntimeArgument>> expr = Expression.Lambda<Func<RuntimeArgument>>(memberExpression, null);
                Func<RuntimeArgument> func = expr.Compile();
                ra = func();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                return e.Message;
            }

            if (ra != null)
            {
                result = new ArgumentValue<TResult>
                {
                    ArgumentName = ra.Name,
                };
                return null;
            }
            else
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ValidationException(SR.RuntimeArgumentNotCreated));
                }
                else
                {
                    return SR.RuntimeArgumentNotCreated;
                }
            }

        }
        else
        {
            //Assumption: Arguments must be properties of Activity object. Otherwise, it cannot be found by runtime via ArgumentValue.
            if (memberExpression != null && memberExpression.Member is PropertyInfo)
            {
                PropertyInfo property = memberExpression.Member as PropertyInfo;
                result = new ArgumentValue<TResult> { ArgumentName = property.Name };
                return null;
            }
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.ArgumentMustbePropertyofWorkflowElement));
            }
            else
            {
                return SR.ArgumentMustbePropertyofWorkflowElement;
            }
        }
    }

    private static string TryConvertArgumentReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;
        //Assumption: Arguments must be properties of Activity object. Otherwise, it cannot be found by runtime via ArgumentReference.
        if (methodCallExpression.Object is MemberExpression)
        {
            MemberExpression member = methodCallExpression.Object as MemberExpression;
            if (member.Member is PropertyInfo)
            {
                PropertyInfo property = member.Member as PropertyInfo;
                result = new ArgumentReference<TResult> { ArgumentName = property.Name };
                return null;
            }
        }
        if (throwOnError)
        {
            throw FxTrace.Exception.AsError(new ValidationException(SR.ArgumentMustbePropertyofWorkflowElement));
        }
        else
        {
            return SR.ArgumentMustbePropertyofWorkflowElement;
        }
    }

    private static string TryConvertBinaryExpression<TResult>(BinaryExpression binaryExpressionBody, Type leftType, Type rightType, bool throwOnError, out Activity<TResult> result)
    {
        try
        {
            MethodInfo specializedHandle = TryConvertBinaryExpressionHandle.MakeGenericMethod(leftType, rightType, typeof(TResult));
            object[] parameters = new object[] { binaryExpressionBody, throwOnError, null };
            string errorString = specializedHandle.Invoke(null, parameters) as string;
            result = parameters[2] as Activity<TResult>;
            return errorString;
        }
        catch (TargetInvocationException e)
        {
            throw FxTrace.Exception.AsError(e.InnerException);
        }
    }

    //this method handles single dimentional array. Multiple dimentional array accessor is method call expression
    private static string TryConvertArrayItemValue<TResult>(BinaryExpression binaryExpression, Type leftType, Type rightType, bool throwOnError, out Activity<TResult> result)
    {
        result = null;

        //for ArrayIndex expression, Left type is always TResult[] and Right type is always int
        if (!leftType.IsArray)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerOnNonArrayType(leftType)));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerOnNonArrayType(leftType);
            }
        }
        if (!TypeHelper.AreTypesCompatible(leftType.GetElementType(), typeof(TResult)))
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerValueWithIncompatibleArrayTypeAndResultType(leftType, typeof(TResult))));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerValueWithIncompatibleArrayTypeAndResultType(leftType, typeof(TResult));
            }
        }
        if (rightType != typeof(int))
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.DoNotSupportArrayIndexerWithNonIntIndex(rightType)));
            }
            else
            {
                return SR.DoNotSupportArrayIndexerWithNonIntIndex(rightType);
            }
        }

        string arrayError = TryConvert(binaryExpression.Left, throwOnError, out Activity<TResult[]> array);
        if (arrayError != null)
        {
            return arrayError;
        }

        string indexError = TryConvert(binaryExpression.Right, throwOnError, out Activity<int> index);
        if (indexError != null)
        {
            return indexError;
        }

        result = new ArrayItemValue<TResult>
        {
            Array = new InArgument<TResult[]>(array) { EvaluationOrder = 0 },
            Index = new InArgument<int>(index) { EvaluationOrder = 1 },
        };
        return null;
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertBinaryExpressionWorker<TLeft, TRight, TResult>(BinaryExpression binaryExpressionBody, bool throwOnError, out Activity<TResult> result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;

        string leftError = TryConvert(binaryExpressionBody.Left, throwOnError, out Activity<TLeft> left);
        if (leftError != null)
        {
            return leftError;
        }
        string rightError = TryConvert(binaryExpressionBody.Right, throwOnError, out Activity<TRight> right);
        if (rightError != null)
        {
            return rightError;
        }

        if (binaryExpressionBody.Method != null)
        {
            return TryConvertOverloadingBinaryOperator(binaryExpressionBody, left, right, throwOnError, out result);
        }

        InArgument<TLeft> leftArgument = new(left) { EvaluationOrder = 0 };
        InArgument<TRight> rightArgument = new(right) { EvaluationOrder = 1 };

        switch (binaryExpressionBody.NodeType)
        {
            case ExpressionType.Add:
                result = new Add<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = false };
                break;
            case ExpressionType.AddChecked:
                result = new Add<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = true };
                break;
            case ExpressionType.Subtract:
                result = new Subtract<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = false };
                break;
            case ExpressionType.SubtractChecked:
                result = new Subtract<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = true };
                break;
            case ExpressionType.Multiply:
                result = new Multiply<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = false };
                break;
            case ExpressionType.MultiplyChecked:
                result = new Multiply<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument, Checked = true };
                break;
            case ExpressionType.Divide:
                result = new Divide<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.AndAlso:
                Fx.Assert(typeof(TLeft) == typeof(bool), "AndAlso only accept bool.");
                Fx.Assert(typeof(TRight) == typeof(bool), "AndAlso only accept bool.");
                Fx.Assert(typeof(TResult) == typeof(bool), "AndAlso only accept bool.");
                // Work around generic constraints
                object leftObject1 = left;
                object rightObject1 = right;
                object resultObject1 = new AndAlso() { Left = (Activity<bool>)leftObject1, Right = (Activity<bool>)rightObject1 };
                result = (Activity<TResult>)resultObject1;
                break;
            case ExpressionType.OrElse:
                Fx.Assert(typeof(TLeft) == typeof(bool), "OrElse only accept bool.");
                Fx.Assert(typeof(TRight) == typeof(bool), "OrElse only accept bool.");
                Fx.Assert(typeof(TResult) == typeof(bool), "OrElse only accept bool.");
                // Work around generic constraints
                object leftObject2 = left;
                object rightObject2 = right;
                object resultObject2 = new OrElse() { Left = (Activity<bool>)leftObject2, Right = (Activity<bool>)rightObject2 };
                result = (Activity<TResult>)resultObject2;
                break;
            case ExpressionType.Or:
                result = new Or<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.And:
                result = new And<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.LessThan:
                result = new LessThan<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.LessThanOrEqual:
                result = new LessThanOrEqual<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.GreaterThan:
                result = new GreaterThan<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.GreaterThanOrEqual:
                result = new GreaterThanOrEqual<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.Equal:
                result = new Equal<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            case ExpressionType.NotEqual:
                result = new NotEqual<TLeft, TRight, TResult>() { Left = leftArgument, Right = rightArgument };
                break;
            default:
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedExpressionType(binaryExpressionBody.NodeType)));
                }
                else
                {
                    return SR.UnsupportedExpressionType(binaryExpressionBody.NodeType);
                }
        }

        return null;
    }

    private static string TryConvertUnaryExpression<TResult>(UnaryExpression unaryExpressionBody, Type operandType, bool throwOnError, out Activity<TResult> result)
    {
        try
        {
            MethodInfo specializedHandle = TryConvertUnaryExpressionHandle.MakeGenericMethod(operandType, typeof(TResult));
            object[] parameters = new object[] { unaryExpressionBody, throwOnError, null };
            string errorString = specializedHandle.Invoke(null, parameters) as string;
            result = parameters[2] as Activity<TResult>;
            return errorString;
        }
        catch (TargetInvocationException e)
        {
            throw FxTrace.Exception.AsError(e.InnerException);
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertUnaryExpressionWorker<TOperand, TResult>(UnaryExpression unaryExpressionBody, bool throwOnError, out Activity<TResult> result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;

        string operandError = TryConvert(unaryExpressionBody.Operand, throwOnError, out Activity<TOperand> operand);
        if (operandError != null)
        {
            return operandError;
        }

        if (unaryExpressionBody.Method != null)
        {
            return TryConvertOverloadingUnaryOperator(unaryExpressionBody, operand, throwOnError, out result);
        }

        switch (unaryExpressionBody.NodeType)
        {
            case ExpressionType.Not:
                result = new Not<TOperand, TResult> { Operand = operand };
                break;
            case ExpressionType.Convert:
                result = new Cast<TOperand, TResult> { Operand = operand, Checked = false };
                break;
            case ExpressionType.ConvertChecked:
                result = new Cast<TOperand, TResult> { Operand = operand, Checked = true };
                break;
            case ExpressionType.TypeAs:
                result = new As<TOperand, TResult> { Operand = operand };
                break;
            default:
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedExpressionType(unaryExpressionBody.NodeType)));
                }
                else
                {
                    return SR.UnsupportedExpressionType(unaryExpressionBody.NodeType);
                }
        }

        return null;
    }

    private static string TryConvertMemberExpression<TResult>(MemberExpression memberExpressionBody, Type operandType, bool throwOnError, out Activity<TResult> result)
    {
        try
        {
            MethodInfo specializedHandle = TryConvertMemberExpressionHandle.MakeGenericMethod(operandType, typeof(TResult));
            object[] parameters = new object[] { memberExpressionBody, throwOnError, null };
            string errorString = specializedHandle.Invoke(null, parameters) as string;
            result = parameters[2] as Activity<TResult>;
            return errorString;
        }
        catch (TargetInvocationException e)
        {
            throw FxTrace.Exception.AsError(e.InnerException);
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertMemberExpressionWorker<TOperand, TResult>(MemberExpression memberExpressionBody, bool throwOnError, out Activity<TResult> result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;
        Activity<TOperand> operand = null;
        if (memberExpressionBody.Expression != null)
        {
            // Static property might not have any expressions.
            string operandError = TryConvert(memberExpressionBody.Expression, throwOnError, out operand);
            if (operandError != null)
            {
                return operandError;
            }
        }
        if (memberExpressionBody.Member is PropertyInfo)
        {
            if (operand == null)
            {
                result = new PropertyValue<TOperand, TResult> { PropertyName = memberExpressionBody.Member.Name };
            }
            else
            {
                result = new PropertyValue<TOperand, TResult> { Operand = operand, PropertyName = memberExpressionBody.Member.Name };
            }
            return null;
        }
        else if (memberExpressionBody.Member is FieldInfo)
        {
            if (operand == null)
            {
                result = new FieldValue<TOperand, TResult> { FieldName = memberExpressionBody.Member.Name };
            }
            else
            {
                result = new FieldValue<TOperand, TResult> { Operand = operand, FieldName = memberExpressionBody.Member.Name };
            }
            return null;
        }
        if (throwOnError)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedMemberExpressionWithType(memberExpressionBody.Member.GetType().Name)));
        }
        else
        {
            return SR.UnsupportedMemberExpressionWithType(memberExpressionBody.Member.GetType().Name);
        }
    }

    private static string TryConvertReferenceMemberExpression<TResult>(MemberExpression memberExpressionBody, Type operandType, bool throwOnError, out Activity<Location<TResult>> result)
    {
        try
        {
            MethodInfo specializedHandle = TryConvertReferenceMemberExpressionHandle.MakeGenericMethod(operandType, typeof(TResult));
            object[] parameters = new object[] { memberExpressionBody, throwOnError, null };
            string errorString = specializedHandle.Invoke(null, parameters) as string;
            result = parameters[2] as Activity<Location<TResult>>;
            return errorString;
        }
        catch (TargetInvocationException e)
        {
            throw FxTrace.Exception.AsError(e.InnerException);
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertReferenceMemberExpressionWorker<TOperand, TResult>(MemberExpression memberExpressionBody, bool throwOnError, out Activity<Location<TResult>> result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;
        Activity<TOperand> operand = null;
        Activity<Location<TOperand>> operandReference = null;
        bool isValueType = typeof(TOperand).IsValueType;
        if (memberExpressionBody.Expression != null)
        {
            // Static property might not have any expressions.
            if (!isValueType)
            {
                string operandError = TryConvert(memberExpressionBody.Expression, throwOnError, out operand);
                if (operandError != null)
                {
                    return operandError;
                }
            }
            else
            {
                string operandError = TryConvertReference(memberExpressionBody.Expression, throwOnError, out operandReference);
                if (operandError != null)
                {
                    return operandError;
                }
            }
        }
        if (memberExpressionBody.Member is PropertyInfo)
        {
            if (!isValueType)
            {
                if (operand == null)
                {
                    result = new PropertyReference<TOperand, TResult> { PropertyName = memberExpressionBody.Member.Name };
                }
                else
                {
                    result = new PropertyReference<TOperand, TResult> { Operand = operand, PropertyName = memberExpressionBody.Member.Name };
                }
            }
            else
            {
                if (operandReference == null)
                {
                    result = new ValueTypePropertyReference<TOperand, TResult> { PropertyName = memberExpressionBody.Member.Name };
                }
                else
                {
                    result = new ValueTypePropertyReference<TOperand, TResult> { OperandLocation = operandReference, PropertyName = memberExpressionBody.Member.Name };
                }

            }
            return null;
        }
        if (memberExpressionBody.Member is FieldInfo)
        {
            if (!isValueType)
            {
                if (operand == null)
                {
                    result = new FieldReference<TOperand, TResult> { FieldName = memberExpressionBody.Member.Name };
                }
                else
                {
                    result = new FieldReference<TOperand, TResult> { Operand = operand, FieldName = memberExpressionBody.Member.Name };
                }
            }
            else
            {
                if (operandReference == null)
                {
                    result = new ValueTypeFieldReference<TOperand, TResult> { FieldName = memberExpressionBody.Member.Name };
                }
                else
                {
                    result = new ValueTypeFieldReference<TOperand, TResult> { OperandLocation = operandReference, FieldName = memberExpressionBody.Member.Name };
                }

            }
            return null;
        }
        if (throwOnError)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedMemberExpressionWithType(memberExpressionBody.Member.GetType().Name)));
        }
        else
        {
            return SR.UnsupportedMemberExpressionWithType(memberExpressionBody.Member.GetType().Name);
        }
    }

    private static string TryConvertOverloadingUnaryOperator<TOperand, TResult>(UnaryExpression unaryExpression, Activity<TOperand> operand, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        if (!unaryExpression.Method.IsStatic)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.OverloadingMethodMustBeStatic));
            }
            else
            {
                return SR.OverloadingMethodMustBeStatic;
            }
        }

        result = new InvokeMethod<TResult>
        {
            MethodName = unaryExpression.Method.Name,
            TargetType = unaryExpression.Method.DeclaringType,
            Parameters = { new InArgument<TOperand> { Expression = operand } },
        };
        return null;
    }

    private static string TryConvertOverloadingBinaryOperator<TLeft, TRight, TResult>(BinaryExpression binaryExpression, Activity<TLeft> left, Activity<TRight> right, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        if (!binaryExpression.Method.IsStatic)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.OverloadingMethodMustBeStatic));
            }
            else
            {
                return SR.OverloadingMethodMustBeStatic;
            }
        }

        result = new InvokeMethod<TResult>
        {
            MethodName = binaryExpression.Method.Name,
            TargetType = binaryExpression.Method.DeclaringType,
            Parameters = { new InArgument<TLeft> { Expression = left, EvaluationOrder = 0 }, new InArgument<TRight> { Expression = right, EvaluationOrder = 1 } },
        };
        return null;
    }

    private static string TryConvertMethodCallExpression<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        MethodInfo methodInfo = methodCallExpression.Method;

        if (methodInfo == null)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.MethodInfoRequired(methodCallExpression.GetType().Name)));
            }
            else
            {
                return SR.MethodInfoRequired(methodCallExpression.GetType().Name);
            }
        };
        if (string.IsNullOrEmpty(methodInfo.Name) || methodInfo.DeclaringType == null)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.MethodNameRequired(methodInfo.GetType().Name)));
            }
            else
            {
                return SR.MethodNameRequired(methodInfo.GetType().Name);
            }
        }
        InvokeMethod<TResult> invokeMethod = new()
        {
            MethodName = methodInfo.Name,
        };

        ParameterInfo[] parameterInfoArray = methodInfo.GetParameters();
        if (methodCallExpression.Arguments.Count != parameterInfoArray.Length)//no optional argument call for LINQ expression
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.ArgumentNumberRequiresTheSameAsParameterNumber(methodCallExpression.GetType().Name)));
            }
            else
            {
                return SR.ArgumentNumberRequiresTheSameAsParameterNumber(methodCallExpression.GetType().Name);
            }
        }

        string error = TryConvertArguments(methodCallExpression.Arguments, invokeMethod.Parameters, methodCallExpression.GetType(), 1, parameterInfoArray, throwOnError);
        if (error != null)
        {
            return error;
        }

        foreach (Type type in methodInfo.GetGenericArguments())
        {
            if (type == null)
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ValidationException(SR.InvalidGenericTypeInfo(methodCallExpression.GetType().Name)));
                }
                else
                {
                    return SR.InvalidGenericTypeInfo(methodCallExpression.GetType().Name);
                }
            }
            invokeMethod.GenericTypeArguments.Add(type);
        }
        if (methodInfo.IsStatic)
        {
            invokeMethod.TargetType = methodInfo.DeclaringType;
        }
        else
        {
            if (methodCallExpression.Object == null)
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ValidationException(SR.InstanceMethodCallRequiresTargetObject));
                }
                else
                {
                    return SR.InstanceMethodCallRequiresTargetObject;
                }
            }
            object[] parameters = new object[] { methodCallExpression.Object, false, throwOnError, null };
            error = TryConvertArgumentExpressionHandle.MakeGenericMethod(methodCallExpression.Object.Type).Invoke(null, parameters) as string;
            if (error != null)
            {
                return error;
            }
            InArgument argument = (InArgument)parameters[3];
            argument.EvaluationOrder = 0;
            invokeMethod.TargetObject = argument;
        }
        result = invokeMethod;
        return null;
    }

    private static string TryConvertInvocationExpression<TResult>(InvocationExpression invocationExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        if (invocationExpression.Expression == null || invocationExpression.Expression.Type == null)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.InvalidExpressionProperty(invocationExpression.GetType().Name)));
            }
            else
            {
                return SR.InvalidExpressionProperty(invocationExpression.GetType().Name);
            }
        }
        InvokeMethod<TResult> invokeMethod = new()
        {
            MethodName = "Invoke",
        };
        object[] parameters = new object[] { invocationExpression.Expression, false, throwOnError, null };
        if (TryConvertArgumentExpressionHandle.MakeGenericMethod(invocationExpression.Expression.Type).Invoke(null, parameters) is string error)
        {
            return error;
        }
        InArgument argument = (InArgument)parameters[3];
        argument.EvaluationOrder = 0;
        invokeMethod.TargetObject = argument;

        //InvocationExpression can not have a by-ref parameter.
        error = TryConvertArguments(invocationExpression.Arguments, invokeMethod.Parameters, invocationExpression.GetType(), 1, null, throwOnError);

        if (error != null)
        {
            return error;
        }

        result = invokeMethod;
        return null;
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static string TryConvertArgumentExpressionWorker<TArgument>(Expression expression, bool isByRef, bool throwOnError, out Argument result)
#pragma warning restore IDE0051 // Remove unused private members
    {
        result = null;

        string error;
        if (isByRef)
        {
            error = TryConvertReference(expression, throwOnError, out Activity<Location<TArgument>> argument);
            if (error == null)
            {
                result = new InOutArgument<TArgument>
                {
                    Expression = argument,
                };
            }
        }
        else
        {
            error = TryConvert(expression, throwOnError, out Activity<TArgument> argument);
            if (error == null)
            {
                result = new InArgument<TArgument>
                {
                    Expression = argument,
                };
            }
        }
        return error;
    }

    private static string TryConvertNewExpression<TResult>(NewExpression newExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        New<TResult> newActivity = new();
        ParameterInfo[] parameterInfoArray = null;
        if (newExpression.Constructor != null)
        {
            parameterInfoArray = newExpression.Constructor.GetParameters();
            if (newExpression.Arguments.Count != parameterInfoArray.Length)//no optional argument call for LINQ expression
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ValidationException(SR.ArgumentNumberRequiresTheSameAsParameterNumber(newExpression.GetType().Name)));
                }
                else
                {
                    return SR.ArgumentNumberRequiresTheSameAsParameterNumber(newExpression.GetType().Name);
                }
            }
        }

        string error = TryConvertArguments(newExpression.Arguments, newActivity.Arguments, newExpression.GetType(), 0, parameterInfoArray, throwOnError);
        if (error != null)
        {
            return error;
        }
        result = newActivity;
        return null;
    }

    private static string TryConvertNewArrayExpression<TResult>(NewArrayExpression newArrayExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;
        NewArray<TResult> newArrayActivity = new();
        string error = TryConvertArguments(newArrayExpression.Expressions, newArrayActivity.Bounds, newArrayExpression.GetType(), 0, null, throwOnError);
        if (error != null)
        {
            return error;
        }
        result = newArrayActivity;
        return null;

    }

    private static string TryConvertArguments(ReadOnlyCollection<Expression> source, IList target, Type expressionType, int baseEvaluationOrder, ParameterInfo[] parameterInfoArray, bool throwOnError)
    {
        object[] parameters;
        for (int i = 0; i < source.Count; i++)
        {
            bool isByRef = false;
            Expression expression = source[i];
            if (parameterInfoArray != null)
            {
                ParameterInfo parameterInfo = parameterInfoArray[i];

                if (parameterInfo == null || parameterInfo.ParameterType == null)
                {
                    if (throwOnError)
                    {
                        throw FxTrace.Exception.AsError(new ValidationException(SR.InvalidParameterInfo(i, expressionType.Name)));
                    }
                    else
                    {
                        return SR.InvalidParameterInfo(i, expressionType.Name);
                    }
                }
                isByRef = parameterInfo.ParameterType.IsByRef;
            }
            parameters = new object[] { expression, isByRef, throwOnError, null };
            if (TryConvertArgumentExpressionHandle.MakeGenericMethod(expression.Type).Invoke(null, parameters) is string error)
            {
                return error;
            }
            Argument argument = (Argument)parameters[3];
            argument.EvaluationOrder = i + baseEvaluationOrder;
            target.Add(argument);
        }
        return null;
    }

    private static string TryConvertLocationReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<TResult> result)
    {
        result = null;

        Expression expression = methodCallExpression.Arguments[0];
        if (expression.NodeType != ExpressionType.Constant)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(
                    SR.UnexpectedExpressionNodeType(ExpressionType.Constant.ToString(), expression.NodeType.ToString())));
            }
            else
            {
                return SR.UnexpectedExpressionNodeType(ExpressionType.Constant.ToString(), expression.NodeType.ToString());
            }
        }

        object value = ((ConstantExpression)expression).Value;
        Type valueType = value.GetType();

        if (typeof(RuntimeArgument).IsAssignableFrom(valueType))
        {
            RuntimeArgument runtimeArgument = (RuntimeArgument)value;
            result = new ArgumentValue<TResult>
            {
                ArgumentName = runtimeArgument.Name,
            };
        }
        else if (typeof(Variable).IsAssignableFrom(valueType))
        {
            Variable variable = (Variable)value;
            result = new VariableValue<TResult> { Variable = variable };
        }
        else if (typeof(DelegateArgument).IsAssignableFrom(valueType))
        {
            DelegateArgument delegateArgument = (DelegateArgument)value;
            result = new DelegateArgumentValue<TResult>
            {
                DelegateArgument = delegateArgument
            };
        }

        if (result == null)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedLocationReferenceValue));
            }
            else
            {
                return SR.UnsupportedLocationReferenceValue;
            }
        }

        return null;
    }

    private static string TryConvertReferenceLocationReference<TResult>(MethodCallExpression methodCallExpression, bool throwOnError, out Activity<Location<TResult>> result)
    {
        result = null;

        Expression expression = methodCallExpression.Arguments[0];
        if (expression.NodeType != ExpressionType.Constant)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new ValidationException(
                    SR.UnexpectedExpressionNodeType(ExpressionType.Constant.ToString(), expression.NodeType.ToString())));
            }
            else
            {
                return SR.UnexpectedExpressionNodeType(ExpressionType.Constant.ToString(), expression.NodeType.ToString());
            }
        }

        object value = ((ConstantExpression)expression).Value;
        Type valueType = value.GetType();

        if (typeof(RuntimeArgument).IsAssignableFrom(valueType))
        {
            RuntimeArgument runtimeArgument = (RuntimeArgument)value;
            result = new ArgumentReference<TResult>
            {
                ArgumentName = runtimeArgument.Name,
            };
        }
        else if (typeof(Variable).IsAssignableFrom(valueType))
        {
            Variable variable = (Variable)value;
            result = new VariableReference<TResult> { Variable = variable };
        }
        else if (typeof(DelegateArgument).IsAssignableFrom(valueType))
        {
            DelegateArgument delegateArgument = (DelegateArgument)value;
            result = new DelegateArgumentReference<TResult>
            {
                DelegateArgument = delegateArgument
            };
        }
            
        if (result == null && throwOnError)
        {
            if (throwOnError)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.UnsupportedLocationReferenceValue));
            }
            else
            {
                return SR.UnsupportedLocationReferenceValue;
            }
        }

        return null;
    }
}
