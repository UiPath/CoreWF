using AgileObjects.ReadableExpressions;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
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
            ActivityValidationServices.Validate(root, new() { SkipValidatingRootConfiguration = true });
            foreach (var activity in root.GetChildren().ToArray())
            {
                foreach (var argument in activity.RuntimeArguments)
                {
                    Translate(argument.BoundArgument);
                }
                foreach (var variable in activity.RuntimeVariables)
                {
                    Translate(variable);
                }
            }
            WorkflowInspectionServices.CacheMetadata(root);
        }
        private static void Translate(Variable variable)
        {
            var expression = variable.Default;
            if (expression == null)
            {
                return;
            }
            variable.Default = Translate(expression, true);
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
            var expressionTree = (LambdaExpression)textExpression.GetExpressionTree();
            return isValue ? CreateFuncValue(expressionTree) : CreateFuncReference(expressionTree);
        }
        private static ActivityWithResult CreateFuncReference(LambdaExpression expressionTree)
        {
            object[] arguments;
            Type activityType;
            Type[] genericArguments;
            var visitor = new ReferenceVisitor();
            var fullLocationExpression = visitor.Visit(expressionTree.Body);
            var locationName = visitor.LocationName;
            var locationParameter = visitor.Parameter;
            if (fullLocationExpression == locationParameter)
            {
                arguments = new[] { locationName };
                activityType = typeof(FuncReference<>);
                genericArguments = new[] { fullLocationExpression.Type };
                System.Diagnostics.Trace.WriteLine(locationName);
            }
            else
            {
                var get = Lambda(fullLocationExpression, locationParameter);
                get.Trace();
                var valueParameter = Parameter(fullLocationExpression.Type, "value");
                var set = Lambda(Block(Assign(fullLocationExpression, valueParameter), locationParameter), new[] { locationParameter, valueParameter });
                set.Trace();
                arguments = new object[] { locationName, get.Compile(), set.Compile() };
                activityType = typeof(FuncReference<,>);
                genericArguments = new[] { locationParameter.Type, fullLocationExpression.Type };
            }
            return CreateFunc(activityType, genericArguments, arguments);
        }
        private static ActivityWithResult CreateFuncValue(LambdaExpression expressionTree)
        {
            var newExpressionTree = (LambdaExpression)new ValueVisitor().Visit(expressionTree);
            newExpressionTree.Trace();
            return CreateFunc(typeof(FuncValue<>), new[] { expressionTree.ReturnType }, new[] { newExpressionTree.Compile() });
        }
        private static ActivityWithResult CreateFunc(Type activityType, Type[] genericArguments, object[] arguments)
        {
            var funcType = activityType.MakeGenericType(genericArguments);
            return (ActivityWithResult)Activator.CreateInstance(funcType, arguments);
        }
        static void Trace(this Expression expression) => System.Diagnostics.Trace.WriteLine(expression.ToReadableString());
        class ReferenceVisitor : ExpressionVisitor
        {
            public ParameterExpression Parameter { get; private set; }
            public string LocationName { get; private set; }
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.LocationType(out var locationType))
                {
                    Parameter = Parameter(locationType, "location");
                    LocationName = node.LocationName();
                    return Parameter;
                }
                return base.VisitMethodCall(node);
            }
        }
        class ValueVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.LocationType(out var locationType))
                {
                    var getValue = ActivityContextGetValue.MakeGenericMethod(locationType);
                    return Call(node.Object, getValue, Constant(node.LocationName()));
                }
                return base.VisitMethodCall(node);
            }
        }
        private static bool LocationType(this MethodCallExpression node, out Type locationType)
        {
            var method = node.Method;
            var isLocation = method.IsGenericMethod && method.GetGenericMethodDefinition() == ActivityContextGetValueGenericMethod;
            locationType = isLocation ? method.GetGenericArguments()[0] : null;
            return isLocation;
        }
        private static string LocationName(this MethodCallExpression node) => ((LocationReference)((ConstantExpression)node.Arguments[0]).Value).Name;
        static IEnumerable<Activity> GetChildren(this Activity root) => 
            new[] { root }.Concat(WorkflowInspectionServices.GetActivities(root).SelectMany(a => a.GetChildren()));
        static readonly MethodInfo ActivityContextGetValue = typeof(ActivityContext).GetMethod(nameof(ActivityContext.GetValue), new Type[] { typeof(string) });
    }
}