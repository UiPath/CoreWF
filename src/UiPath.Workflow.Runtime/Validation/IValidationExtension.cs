namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IEnumerable<ValidationError> Validate(Activity activity);
    }
}
