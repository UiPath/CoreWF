using System.Activities.Validation;

namespace System.Activities
{
    internal static class TextExpressionHelper
    {
        private static readonly Func<ValidationExtension> _validationFunc = () => new();

        public static bool QueueForValidation<T>(Activity activity, CodeActivityMetadata metadata, string expressionText, string language, bool isLocation)
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
                    Activity = activity,
                    ExpressionText = expressionText,
                    IsLocation = isLocation,
                    ResultType = typeof(T),
                    Environment = metadata.Environment
                }, language);

                return true;
            }
            return false;
        }
    }
}
