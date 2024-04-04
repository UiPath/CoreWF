// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Collections.ObjectModel;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    public class ExpressionEvaluatorWithBody<T> : NativeActivity<T>
    {
        private InArgument<T> _expressionResult;

        private Activity _body;

        public ExpressionEvaluatorWithBody(T result)
        {
            _expressionResult = new InArgument<T>(result);
        }

        public ExpressionEvaluatorWithBody()
        {
            _expressionResult = new InArgument<T>();
        }

        public InArgument<T> ExpressionResult
        {
            get
            {
                return _expressionResult;
            }
            set
            {
                _expressionResult = value;
            }
        }

        public Activity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("ExpressionResult", typeof(T), ArgumentDirection.In));
            metadata.Bind(this.ExpressionResult, runtimeArguments[0]);

            metadata.SetArgumentsCollection(runtimeArguments);

            metadata.AddImplementationChild(_body);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (_body != null)
            {
                context.ScheduleActivity(_body);
            }
            Result.Set(context, _expressionResult.Get(context));
        }
    }
}
