// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using CoreWf;
using CoreWf.Expressions;

namespace Test.Common.TestObjects.Activities.Variables
{
    public static class VariableHelper
    {
        public static Variable<T> Create<T>(string name)
        {
            Variable<T> variable = new Variable<T>();
            variable.Name = name;
            return variable;
        }

        public static Variable<T> Create<T>()
        {
            Variable<T> variable = new Variable<T>();
            variable.Name = "Variable" + Guid.NewGuid().ToString().Replace("-", "");

            return variable;
        }

        // use for primitive types
        public static Variable<T> CreateInitialized<T>(T initialValue)
        {
            Variable<T> variable = VariableHelper.Create<T>();

            Expression<Func<ActivityContext, T>> createLiteral =
                Expression.Lambda<Func<ActivityContext, T>>(Expression.Constant(initialValue, typeof(T)), Expression.Parameter(typeof(ActivityContext), "env"));

            variable.Default = new LambdaValue<T>(createLiteral);

            return variable;
        }

        public static Variable<T> CreateInitialized<T>(string name, T initialValue)
        {
            Variable<T> variable = VariableHelper.CreateInitialized(initialValue);
            variable.Name = name;
            return variable;
        }

        // use for non-primitive types
        public static Variable<T> CreateInitialized<T>(Expression<Func<ActivityContext, T>> initialValue)
        {
            Variable<T> variable = VariableHelper.Create<T>();
            variable.Default = new LambdaValue<T>(initialValue);

            return variable;
        }

        public static Variable<T> CreateInitialized<T>(string name, Expression<Func<ActivityContext, T>> initialValue)
        {
            Variable<T> variable = VariableHelper.CreateInitialized(initialValue);
            variable.Name = name;

            return variable;
        }
    }
}
