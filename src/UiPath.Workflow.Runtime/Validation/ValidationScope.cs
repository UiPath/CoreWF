using System.Collections.Immutable;

namespace System.Activities.Validation
{
    public class ValidationScope
    {
        private readonly Dictionary<string, ExpressionToValidate> _expressionsToValidate = new();
        private string _language;
        internal void AddExpression<T>(CodeActivity<T> activity, string expressionText, LocationReferenceEnvironment environment, string language, bool isLocation)
        {
            _language ??= language;
            if (_language != language)
            {
                activity.AddTempValidationError(new ValidationError("Expression language mismatch", activity));
                return;
            }
            _expressionsToValidate.Add(activity.Id,
                new ExpressionToValidate
                {
                    Activity = activity,
                    ExpressionText = expressionText,
                    IsLocation = isLocation,
                    ResultType = typeof(T),
                    Environment = environment
                });
        }

        internal string Language => _language;

        internal ExpressionToValidate GetExpression(string activityId) => _expressionsToValidate[activityId];

        internal ImmutableArray<ExpressionToValidate> GetAllExpressions() => _expressionsToValidate.Values.ToImmutableArray();

        internal void Reset() => _expressionsToValidate.Clear();
    }
}