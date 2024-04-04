// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Collections.ObjectModel;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    public class ExpressionEvaluator<T> : CodeActivity<T>
    {
        public ExpressionEvaluator(T result)
        {
            this.ExpressionResult = new InArgument<T>(result);
        }

        public ExpressionEvaluator()
        {
            this.ExpressionResult = new InArgument<T>();
        }

        public InArgument<T> ExpressionResult
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("ExpressionResult", typeof(T), ArgumentDirection.In, true));
            metadata.Bind(this.ExpressionResult, runtimeArguments[0]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override T Execute(CodeActivityContext context)
        {
            return this.ExpressionResult.Get(context);
        }
    }
}
