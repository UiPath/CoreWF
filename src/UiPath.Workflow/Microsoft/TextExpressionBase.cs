using System.Activities.Expressions;
using System.Linq.Expressions;

namespace System.Activities
{
    public abstract class TextExpressionBase<TResult> : CodeActivity<TResult>, ITextExpression
    {
        public abstract string ExpressionText { get; set; }

        public abstract string Language { get; }

        public abstract Expression GetExpressionTree();

        protected bool QueueForValidation<T>(CodeActivityMetadata metadata, bool isLocation)
        {
            if (metadata.Environment.IsDesignValidating)
            {
                metadata.Environment.ValidationScope.AddExpression(this, ExpressionText, metadata.Environment, Language, isLocation);
                return true;
            }
            return false;
        }
    }
}
