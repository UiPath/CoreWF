// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using LegacyTest.Test.Common.TestObjects.CustomActivities;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestExpressionEvaluator<T> : TestActivity
    {
        public TestExpressionEvaluator()
        {
            this.ProductActivity = new ExpressionEvaluator<T>();
        }

        public TestExpressionEvaluator(T constValue)
        {
            this.ProductActivity = new ExpressionEvaluator<T>(constValue);
        }

        public T ExpressionResult
        {
            set
            {
                ((ExpressionEvaluator<T>)this.ProductActivity).ExpressionResult = new InArgument<T>(value);
            }
        }
    }
}
