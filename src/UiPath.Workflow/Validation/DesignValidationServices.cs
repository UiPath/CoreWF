using System.Collections.Generic;
using System.Linq;

namespace System.Activities.Validation;

public static class DesignValidationServices
{
    public static ValidationResults Validate(Activity activity) => Validate(activity, new());

    public static ValidationResults Validate(Activity activity, ValidationSettings settings)
    {
        settings.Environment ??= new ActivityLocationReferenceEnvironment();
        settings.Environment.ValidationScope.Reset();
        settings.IsDesignValidating = true;
        // make sure we don't double validate
        settings.ForceExpressionCache = true;
        var results = ActivityValidationServices.Validate(activity, settings);
        var expressionResults = GetValidator(settings.Environment).Validate(activity, settings.Environment.ValidationScope);
        if (expressionResults.Any())
        {
            var errors = new List<ValidationError>();
            errors.AddRange(results.Errors);
            errors.AddRange(results.Warnings);
            errors.AddRange(expressionResults);
            results = new(errors);
        }
        return results;
    }

    public static Activity Resolve(Activity root, string id) => WorkflowInspectionServices.Resolve(root, id);

    private static DesignRoslynExpressionValidator GetValidator(LocationReferenceEnvironment environment)
    {
        return environment.ValidationScope.Language == "C#"
            ? DesignCSharpExpressionValidator.Instance
            : DesignVBExpressionValidator.Instance;
    }
}
