namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IList<ValidationError> Validate(Activity activity);

        void QueueExpressionForValidation<T>(ExpressionToValidate expressionToValidate, string language);
    }
}
