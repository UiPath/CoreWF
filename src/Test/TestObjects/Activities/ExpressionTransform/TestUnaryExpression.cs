// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using SLE = System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.ExpressionTransform
{
    public enum UnaryOperator
    {
        Cast,
        CheckedCast,
        Not,
        TypeAs
    }

    public class TestUnaryExpression : TestExpression
    {
        public UnaryOperator Operator
        {
            get;
            set;
        }

        public TestExpression Operand
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
                case UnaryOperator.Cast:
                    et = SLE.ExpressionType.Convert;
                    break;
                case UnaryOperator.CheckedCast:
                    et = SLE.ExpressionType.ConvertChecked;
                    break;
                case UnaryOperator.Not:
                    et = SLE.ExpressionType.Not;
                    break;
                case UnaryOperator.TypeAs:
                    et = SLE.ExpressionType.TypeAs;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Operator: {0} is unsupported", Operator.ToString()));
            }

            UnaryExpression expression = null;
            expression = Expression.MakeUnary(et, this.Operand.CreateLinqExpression(), this.ResultType);

            return expression;
        }

        public override object CreateExpectedActivity()
        {
            Activity we = null;

            we = s_unaryExpressionHandler.MakeGenericMethod(Operand.ResultType, ResultType).Invoke
                (null, new object[] { this.Operator, this.Operand }) as Activity;

            return we;
        }

        private static MethodInfo s_unaryExpressionHandler = typeof(TestUnaryExpression).GetMethod("HandleUnaryExpression", BindingFlags.NonPublic | BindingFlags.Static);

        private static Activity<TResult> HandleUnaryExpression<TOperand, TResult>(UnaryOperator op, TestExpression operand)
        {
            Activity<TResult> we = null;
            switch (op)
            {
                case UnaryOperator.Cast:
                    we = new Cast<TOperand, TResult>()
                    {
                        Checked = false,
                        Operand = TestExpression.GetInArgumentFromExpectedNode<TOperand>(operand),
                    };
                    break;
                case UnaryOperator.CheckedCast:
                    we = new Cast<TOperand, TResult>()
                    {
                        Checked = true,
                        Operand = TestExpression.GetInArgumentFromExpectedNode<TOperand>(operand),
                    };
                    break;
                case UnaryOperator.Not:
                    we = new Not<TOperand, TResult>()
                    {
                        Operand = TestExpression.GetInArgumentFromExpectedNode<TOperand>(operand),
                    };
                    break;
                case UnaryOperator.TypeAs:
                    we = new As<TOperand, TResult>()
                    {
                        Operand = TestExpression.GetInArgumentFromExpectedNode<TOperand>(operand),
                    };
                    break;
                default:
                    throw new NotSupportedException(string.Format("Operator: {0} is unsupported", op.ToString()));
            }

            return we;
        }
    }
}
