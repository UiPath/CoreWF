using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System.Collections.Generic;
using System.Linq;

namespace System.Activities.Validation
{
    internal sealed class ValidationExtension : IValidationExtension
    {
        public IList<ValidationError> Validate(Activity activity, IList<ValidationError> existingErrors)
        {
            var validator = GetValidator(Scope.Language);
            var newErrrors = validator.Validate(activity, Scope);
            if(existingErrors != null)
              return newErrrors.Concat(existingErrors).ToList();

            return newErrrors;
        }

        private static RoslynExpressionValidator GetValidator(string language)
        {
            return language switch
            {
                CSharpHelper.Language => CSharpExpressionValidator.Instance,
                VisualBasicHelper.Language => VbExpressionValidator.Instance,
                _ => throw new ArgumentException(language, nameof(language))
            };
        }

        public void QueueExpressionForValidation<T>(ExpressionToValidate expressionToValidate, string language)
        {
            Scope.AddExpression<T>(expressionToValidate, language);
        }

        public ValidationScope Scope { get; } = new();
    }
}
