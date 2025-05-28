namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IEnumerable<ValidationError> PostValidate(Activity activity, ValidationSettings validationSettings);
    }
}
