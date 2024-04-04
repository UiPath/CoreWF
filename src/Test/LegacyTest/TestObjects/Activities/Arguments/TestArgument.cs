// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    using System;
    using System.Linq.Expressions;
    using System.Activities;
    using System.Activities.Expressions;
    using Argument = System.Activities.Argument;

    public abstract class TestArgument
    {
        protected Argument productArgument;

        public string Name { get; set; }

        public Argument ProductArgument
        {
            get { return this.productArgument; }
        }
    }

    public class TestArgument<T> : TestArgument
    {
        public TestArgument(Direction direction, string name)
        {
            // This constructor is used for a special case in PowerShell activity which uses an InArgument without an expression
            this.productArgument = CreateProductArgument(direction, name, null);
        }

        public TestArgument(Direction direction, string name, T valueLiteral)
        {
            this.productArgument = CreateProductArgument(direction, name, new Literal<T>(valueLiteral));
        }

        public TestArgument(Direction direction, string name, Expression<Func<ActivityContext, T>> valueExpression)
        {
            this.productArgument = CreateProductArgument(direction, name, new LambdaValue<T>(valueExpression));
        }

        public TestArgument(Direction direction, string name, Variable<T> valueVariable)
        {
            if (direction == Direction.In)
            {
                this.productArgument = CreateProductArgument(direction, name, new VariableValue<T>(valueVariable));
            }
            else
            {
                this.productArgument = CreateProductArgument(direction, name, new VariableReference<T>(valueVariable));
            }
        }

        public TestArgument(Direction direction, string name, TestActivity valueActivity)
        {
            this.productArgument = CreateProductArgument(direction, name, (ActivityWithResult)valueActivity.ProductActivity);
        }

        private Argument CreateProductArgument(Direction direction, string name, ActivityWithResult expression)
        {
            Argument argument;

            switch (direction)
            {
                case Direction.In:
                    argument = new InArgument<T>();
                    break;
                case Direction.Out:
                    argument = new OutArgument<T>();
                    break;
                case Direction.InOut:
                    argument = new InOutArgument<T>();
                    break;
                default:
                    throw new ArgumentException("Unknown direction value", "direction");
            }

            this.Name = name;
            argument.Expression = expression;

            return argument;
        }
    }

    public enum Direction
    {
        In,
        Out,
        InOut
    }
}
