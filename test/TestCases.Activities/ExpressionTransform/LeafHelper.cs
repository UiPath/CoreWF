// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Linq.Expressions;
using System.Reflection;
using Test.Common.TestObjects.Activities.ExpressionTransform;

namespace TestCases.Activities.ExpressionTransform
{
    public class LeafHelper
    {
        public static Variable<T> GetVariable<T>()
        {
            return new Variable<T>()
            {
                Name = typeof(T).Name + "Var"
            };
        }

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
        public static Expression GetMemberAccessVariableExpression<T>()
        {
            MethodInfo getMethodInfo = typeof(Variable<T>).GetMethod("Get", new Type[] { typeof(ActivityContext) });
            DummyHelper dummy = new DummyHelper();

            // DummyHelper.<T>Var = LeafHelper.GetVariable<T>();
            FieldInfo fieldInfo;
            fieldInfo = typeof(DummyHelper).GetField(typeof(T).Name + "Var");
            fieldInfo.SetValue(dummy, LeafHelper.GetVariable<T>());

            return Expression.Call(
                Expression.Field(Expression.Constant(dummy), typeof(DummyHelper).GetField(typeof(T).Name + "Var")),
                getMethodInfo,
                TestExpression.EnvParameter);
        }

        public static NodeExpressionPair GetConstNode(Type t)
        {
            if (t == typeof(int))
                return ExpressionLeafs.ConstIntValue;
            else if (t == typeof(string))
                return ExpressionLeafs.ConstStringValue;
            else if (t == typeof(bool))
                return ExpressionLeafs.ConstBooleanTrue;
            else
                throw new NotSupportedException("Unsupported const leaf node");
        }
    }
}
