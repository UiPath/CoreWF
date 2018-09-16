// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using SLE = System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.ExpressionTransform
{
    public enum BinaryOperator
    {
        Add,
        And,
        AndAlso,
        CheckedAdd,
        CheckedMultiply,
        CheckedSubtract,
        Divide,
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Multiply,
        NotEqual,
        Or,
        OrElse,
        Subtract
    }

    public class TestBinaryExpression : TestExpression
    {
        public BinaryOperator Operator
        {
            get;
            set;
        }

        public TestExpression Left
        {
            get;
            set;
        }

        public TestExpression Right
        {
            get;
            set;
        }

        //public override Expression CreateLinqExpression(Type[] parameterTypes)
        public override Expression CreateLinqExpression()
        {
            SLE.ExpressionType et;
            switch (Operator)
            {
                case BinaryOperator.Add:
                    et = SLE.ExpressionType.Add;
                    break;
                case BinaryOperator.And:
                    et = SLE.ExpressionType.And;
                    break;
                case BinaryOperator.AndAlso:
                    et = SLE.ExpressionType.AndAlso;
                    break;
                case BinaryOperator.CheckedAdd:
                    et = SLE.ExpressionType.AddChecked;
                    break;
                case BinaryOperator.CheckedMultiply:
                    et = SLE.ExpressionType.MultiplyChecked;
                    break;
                case BinaryOperator.CheckedSubtract:
                    et = SLE.ExpressionType.SubtractChecked;
                    break;
                case BinaryOperator.Divide:
                    et = SLE.ExpressionType.Divide;
                    break;
                case BinaryOperator.Equal:
                    et = SLE.ExpressionType.Equal;
                    break;
                case BinaryOperator.GreaterThan:
                    et = SLE.ExpressionType.GreaterThan;
                    break;
                case BinaryOperator.GreaterThanOrEqual:
                    et = SLE.ExpressionType.GreaterThanOrEqual;
                    break;
                case BinaryOperator.LessThan:
                    et = SLE.ExpressionType.LessThan;
                    break;
                case BinaryOperator.LessThanOrEqual:
                    et = SLE.ExpressionType.LessThanOrEqual;
                    break;
                case BinaryOperator.Multiply:
                    et = SLE.ExpressionType.Multiply;
                    break;
                case BinaryOperator.NotEqual:
                    et = SLE.ExpressionType.NotEqual;
                    break;
                case BinaryOperator.Or:
                    et = SLE.ExpressionType.Or;
                    break;
                case BinaryOperator.OrElse:
                    et = SLE.ExpressionType.OrElse;
                    break;
                case BinaryOperator.Subtract:
                    et = SLE.ExpressionType.Subtract;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Operator: {0} is unsupported", Operator.ToString()));
            }

            BinaryExpression expression = null;
            expression = Expression.MakeBinary(et, this.Left.CreateLinqExpression(), this.Right.CreateLinqExpression());

            return expression;
        }

        public override object CreateExpectedActivity()
        {
            Activity we = null;

            if (this.Left != null && this.Left.ExpectedConversionException != null)
            {
                this.ExpectedConversionException = this.Left.ExpectedConversionException;
                return null;
            }

            if (this.Right != null && this.Right.ExpectedConversionException != null)
            {
                this.ExpectedConversionException = this.Right.ExpectedConversionException;
                return null;
            }

            this.ExpectedConversionException = null;
            we = s_binaryExpressionHandler.MakeGenericMethod(Left.ResultType, Right.ResultType, ResultType).Invoke
                (null, new object[] { this.Operator, this.Left, this.Right }) as Activity;

            return we;
        }

        private static MethodInfo s_binaryExpressionHandler = typeof(TestBinaryExpression).GetMethod("HandleBinaryExpression", BindingFlags.NonPublic | BindingFlags.Static);

        private static Activity HandleBinaryExpression<TLeft, TRight, TResult>(BinaryOperator op, TestExpression left, TestExpression right)
        {
            Activity we = null;
            InArgument<TLeft> leftArgument = (InArgument<TLeft>)TestExpression.GetInArgumentFromExpectedNode<TLeft>(left);
            leftArgument.EvaluationOrder = 0;
            InArgument<TRight> rightArgument = (InArgument<TRight>)TestExpression.GetInArgumentFromExpectedNode<TRight>(right);
            rightArgument.EvaluationOrder = 1;

            switch (op)
            {
                case BinaryOperator.Add:
                    we = new Add<TLeft, TRight, TResult>()
                    {
                        Checked = false,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.And:
                    we = new And<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.AndAlso:
                    we = new AndAlso()
                    {
                        Left = TestExpression.GetWorkflowElementFromExpectedNode<bool>(left),
                        Right = TestExpression.GetWorkflowElementFromExpectedNode<bool>(right)
                    };
                    break;
                case BinaryOperator.CheckedAdd:
                    we = new Add<TLeft, TRight, TResult>()
                    {
                        Checked = true,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.CheckedMultiply:
                    we = new Multiply<TLeft, TRight, TResult>()
                    {
                        Checked = true,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.CheckedSubtract:
                    we = new Subtract<TLeft, TRight, TResult>()
                    {
                        Checked = true,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.Divide:
                    we = new Divide<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.Equal:
                    we = new Equal<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.GreaterThan:
                    we = new GreaterThan<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.GreaterThanOrEqual:
                    we = new GreaterThanOrEqual<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.LessThan:
                    we = new LessThan<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.LessThanOrEqual:
                    we = new LessThanOrEqual<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.Or:
                    we = new Or<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.Multiply:
                    we = new Multiply<TLeft, TRight, TResult>()
                    {
                        Checked = false,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.NotEqual:
                    we = new NotEqual<TLeft, TRight, TResult>()
                    {
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                case BinaryOperator.OrElse:
                    we = new OrElse()
                    {
                        Left = TestExpression.GetWorkflowElementFromExpectedNode<bool>(left),
                        Right = TestExpression.GetWorkflowElementFromExpectedNode<bool>(right)
                    };
                    break;
                case BinaryOperator.Subtract:
                    we = new Subtract<TLeft, TRight, TResult>()
                    {
                        Checked = false,
                        Left = leftArgument,
                        Right = rightArgument
                    };
                    break;
                default:
                    throw new NotSupportedException(string.Format("Operator: {0} is unsupported", op.ToString()));
            }

            return we;
        }
    }
}
