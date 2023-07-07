
namespace System.Activities.Validation;

internal sealed class ExpressionToValidate
{
    public Activity Activity { get; init; }

    public string ExpressionText { get; init; }

    public LocationReferenceEnvironment Environment { get; init; }

    public Type ResultType { get; init; }

    public bool IsLocation { get; init; }
}
