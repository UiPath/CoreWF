using ReflectionMagic;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities
{
    // DO NOT DELETE THIS for compatibility reasons.
    public abstract class TextExpressionBase<TResult> : CodeActivity<TResult>, ITextExpression
    {
        private static readonly Func<ValidationExtension> _validationFunc = () => new();

        public string ExpressionText
        {
            get => null;
            set
            {
                this.AsDynamic().ExpressionText = value;
            }
        }

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
                var extension = metadata.Environment.Extensions.GetOrAdd(_validationFunc);
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
