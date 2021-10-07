using AgileObjects.ReadableExpressions;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TestCases.Workflows
{
    using static ExpressionUtilities;
    using static Expression;
    static class Compiler
    {
        public static void Run(Activity root)
        {
            WorkflowInspectionServices.CacheMetadata(root);
            foreach (var activity in root.GetChildren())
            {
                foreach (var argument in activity.RuntimeArguments)
                {
                    Translate(argument.BoundArgument);
                }
            }
        }

        private static void Translate(Argument boundArgument)
        {
            var expression = boundArgument.Expression;
            if (expression == null)
            {
                return;
            }
            boundArgument.Expression = Translate(expression, boundArgument.Direction == ArgumentDirection.In);
        }

        private static ActivityWithResult Translate(object expression, bool isValue)
        {
            if (expression is not ITextExpression textExpression)
            {
                return (ActivityWithResult)expression;
            }
            ActivityWithResult newExpression;
            if (isValue)
            {
                var expressionTree = (LambdaExpression)textExpression.GetExpressionTree();
                var newExpressionTree = (LambdaExpression)new ValueVisitor().Visit(expressionTree);
                var code = newExpressionTree.ToReadableString();
                var funcType = typeof(FuncValue<>).MakeGenericType(textExpression.GetType().GenericTypeArguments[0]);
                newExpression = (ActivityWithResult)Activator.CreateInstance(funcType, new[] { newExpressionTree.Compile() });
            }
            else
            {
                newExpression = null;
            }
            WorkflowInspectionServices.CacheMetadata(newExpression);
            return newExpression;
        }

        class ValueVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var method = node.Method;
                if (method.IsGenericMethod && method.GetGenericMethodDefinition() == ActivityContextGetValueGenericMethod)
                {
                    var getValue = ActivityContextGetValue.MakeGenericMethod(method.GetGenericArguments()[0]);
                    var locationReference = (LocationReference)((ConstantExpression)node.Arguments[0]).Value;
                    return Call(node.Object, getValue, Constant(locationReference.Name));
                }
                return base.VisitMethodCall(node);
            }
        }

        static IEnumerable<Activity> GetChildren(this Activity root) => 
            new[] { root }.Concat(WorkflowInspectionServices.GetActivities(root).SelectMany(a => a.GetChildren()));

        private static readonly MethodInfo ActivityContextGetValue = typeof(ActivityContext).GetMethod(nameof(ActivityContext.GetValue), new Type[] { typeof(string) });
    }
}