namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IList<ValidationError> Validate(Activity activity, IList<ValidationError> existingErrors);
    }
}
