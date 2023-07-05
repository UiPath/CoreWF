using System.Activities.Expressions;
using System.Activities.Validation;
using System.Diagnostics;
using System.Linq.Expressions;

namespace System.Activities
{
    public abstract class TextExpressionBase<TResult> : CodeActivity<TResult>, ITextExpression
    {
        private static readonly Func<IValidationExtension> _validationFunc = () =>
        new ValidationExtension();

        public abstract string ExpressionText { get; set; }

        public abstract string Language { get; }

        public abstract Expression GetExpressionTree();

        protected bool QueueForValidation<T>(CodeActivityMetadata metadata, bool isLocation)
        {
            if (metadata.Environment.CompileExpressions)
            {
                return true;
            }

            if (metadata.Environment.IsValidating)
            {
                var extension = metadata.Environment.Extensions.GetOrAdd(_validationFunc) as ValidationExtension;
                extension.QueueExpressionForValidation<T>(new()
                {
                    Activity = this,
                    ExpressionText = ExpressionText,
                    IsLocation = isLocation,
                    ResultType = typeof(T),
                    Environment = metadata.Environment
                }, Language);

                return true;
            }
            return false;
        }
    }
}
