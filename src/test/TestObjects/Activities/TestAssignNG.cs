// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities
{
    public class TestAssignNG : TestActivity
    {
        private readonly Type _type;
        public TestAssignNG(Type assignmentType)
        {
            this.ProductActivity = new Assign();
            _type = assignmentType;
        }

        public TestAssignNG(string displayName, Type assignmentType)
            : this(assignmentType)
        {
            this.DisplayName = displayName;
            _type = assignmentType;
        }

        private Assign ProductAssign
        {
            get
            {
                return (Assign)this.ProductActivity;
            }
        }

        // Assign.To
        public Variable ToVariable
        {
            set { this.ProductAssign.To = Activator.CreateInstance(typeof(OutArgument<>).MakeGenericType(_type), value) as OutArgument; }
        }

        public Expression<Func<ActivityContext, object>> ToExpression
        {
            set { this.ProductAssign.To = Activator.CreateInstance(typeof(OutArgument<>).MakeGenericType(_type), value) as OutArgument; }
        }

        // Assign.Value
        public object Value
        {
            set { this.ProductAssign.Value = Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(_type), value) as InArgument; }
        }

        public Variable ValueVariable
        {
            set { this.ProductAssign.Value = Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(_type), value) as InArgument; }
        }

        public Expression<Func<ActivityContext, object>> ValueExpression
        {
            set { this.ProductAssign.Value = Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(_type), value) as InArgument; }
        }
    }
}
