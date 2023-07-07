using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Activities.Validation
{
    internal sealed class ValidationScope
    {
        private readonly Dictionary<string, ExpressionToValidate> _expressionsToValidate = new();
        private string _language;

        internal void AddExpression<T>(ExpressionToValidate expressionToValidate, string language)
        {
            _language ??= language;
            if (_language != language)
            {
                expressionToValidate.Activity.AddTempValidationError(new ValidationError(SR.DynamicActivityMultipleExpressionLanguages(language), expressionToValidate.Activity));
                return;
            }
            _expressionsToValidate.Add(expressionToValidate.Activity.Id, expressionToValidate);
        }

        internal string Language => _language;

        internal ExpressionToValidate GetExpression(string activityId) => _expressionsToValidate[activityId];

        internal ImmutableArray<ExpressionToValidate> GetAllExpressions() => _expressionsToValidate.Values.ToImmutableArray();

        internal void Clear() => _expressionsToValidate.Clear();
    }
}