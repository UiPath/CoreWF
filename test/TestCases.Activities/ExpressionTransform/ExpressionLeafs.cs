// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using Test.Common.TestObjects.Utilities;

namespace TestCases.Activities.ExpressionTransform
{
    public struct NodeExpressionPair
    {
        //public object LeafNode;
        public Func<object> GetLeafNode;
        public Expression LeafExpression;
    }

    public class ExpressionLeafs
    {
        public static NodeExpressionPair ConstIntValue = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Literal<int>()
                {
                    Value = 123
                };
            },
            LeafExpression = Expression.Constant(123)
        };

        public static NodeExpressionPair ConstStringValue = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Literal<String>()
                {
                    Value = "teststringvalue"
                };
            },
            LeafExpression = Expression.Constant("teststringvalue")
        };

        public static NodeExpressionPair ConstBooleanTrue = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Literal<Boolean>()
                {
                    Value = true
                };
            },
            LeafExpression = Expression.Constant(true)
        };

        public static NodeExpressionPair ConstBooleanFalse = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Literal<Boolean>()
                {
                    Value = false
                };
            },
            LeafExpression = Expression.Constant(false)
        };

        // DummyHelper.StaticField
        public static NodeExpressionPair StaticIntField = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new FieldValue<DummyHelper, int>()
                {
                    FieldName = "StaticIntField"
                };
            },
            LeafExpression = Expression.Field(null, typeof(DummyHelper).GetField("StaticIntField", BindingFlags.Static | BindingFlags.Public))
        };

        // DummyHelper.StaticProperty
        public static NodeExpressionPair StaticIntProperty = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new PropertyValue<DummyHelper, int>()
                {
                    PropertyName = "StaticIntProperty"
                };
            },
            LeafExpression = Expression.Property(null, typeof(DummyHelper).GetMethod("get_StaticIntProperty", BindingFlags.Public | BindingFlags.Static /*| BindingFlags.GetProperty*/))
        };

        public static NodeExpressionPair StaticStringField = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new FieldValue<DummyHelper, string>()
                {
                    FieldName = "StaticStringField"
                };
            },
            LeafExpression = Expression.Field(null, typeof(DummyHelper).GetField("StaticStringField", BindingFlags.Static | BindingFlags.Public))
        };

        public static NodeExpressionPair StaticStringProperty = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new PropertyValue<DummyHelper, string>()
                {
                    PropertyName = "StaticStringProperty"
                };
            },
            LeafExpression = Expression.Property(null, typeof(DummyHelper).GetMethod("get_StaticStringProperty", BindingFlags.Static | BindingFlags.Public /*| BindingFlags.GetProperty*/))
        };

        public static NodeExpressionPair StaticBooleanField = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new FieldValue<DummyHelper, bool>()
                {
                    FieldName = "StaticBooleanField"
                };
            },
            LeafExpression = Expression.Field(null, typeof(DummyHelper).GetField("StaticBooleanField", BindingFlags.Static | BindingFlags.Public))
        };

        public static NodeExpressionPair StaticBooleanProperty = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new PropertyValue<DummyHelper, bool>()
                {
                    PropertyName = "StaticBooleanProperty"
                };
            },
            LeafExpression = Expression.Property(null, typeof(DummyHelper).GetMethod("get_StaticBooleanProperty", BindingFlags.Static | BindingFlags.Public /*| BindingFlags.GetProperty*/))
        };

        // DummyHelper.IntVar
        // Comments copied from ExpressionServices.cs:
        // This is to handle the leaf nodes as a variable                
        //
        // Linq actually generate a temp class wrapping all the local variables.
        //
        // The real expression object look like
        // new TempClass() { A = a }.A.Get(env)
        // 
        // A is a field 
        //
        // This is why the logic of the code below follows.
        // This is pretty dependent on Linq implementation.

        // The test is to simulate the scenario by creating the Linq expression as:
        // (env) => TempClass().A.Get(env)
        public static NodeExpressionPair VariableInWrapper = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Variable<int>()
                {
                    Name = typeof(int).Name + "Var"
                };
            },
            LeafExpression = LeafHelper.GetMemberAccessVariableExpression<int>()
        };

        public static NodeExpressionPair DummyVariable = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Variable<DummyHelper>()
                {
                    Name = typeof(DummyHelper).Name + "Var"
                };
            },
            LeafExpression = LeafHelper.GetMemberAccessVariableExpression<DummyHelper>()
        };

        public static NodeExpressionPair StringVariable = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Variable<String>()
                {
                    Name = typeof(String).Name + "Var"
                };
            },
            LeafExpression = LeafHelper.GetMemberAccessVariableExpression<String>()
        };

        public static NodeExpressionPair BooleanVariable = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new Variable<Boolean>()
                {
                    Name = typeof(Boolean).Name + "Var"
                };
            },
            LeafExpression = LeafHelper.GetMemberAccessVariableExpression<Boolean>()
        };

        public static NodeExpressionPair MethodCallNoParam = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                return new InvokeMethod<int>()
                {
                    MethodName = "MethodCallWithoutArgument",
                    TargetType = typeof(DummyHelper),
                };
            },
            LeafExpression = Expression.Call(
                    typeof(DummyHelper).GetMethod("MethodCallWithoutArgument", BindingFlags.Public | BindingFlags.Static))
        };

        public static NodeExpressionPair UnsupportedBinaryOperator = new NodeExpressionPair()
        {
            GetLeafNode = delegate ()
            {
                throw new NotSupportedException(string.Format(ErrorStrings.UnsupportedExpressionType, ExpressionType.Coalesce));
            },
            // DummyHelper.StaticNullableIntField ?? -1
            LeafExpression = Expression.Coalesce(
                Expression.Field(null, typeof(DummyHelper).GetField("StaticNullableIntField", BindingFlags.Static | BindingFlags.Public)),
                Expression.Constant(-1, typeof(int)))
        };
    }
}
