// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Validation;

public abstract class Constraint : NativeActivity
{
    public const string ValidationErrorListPropertyName = "System.Activities.Validation.Constraint.ValidationErrorList";

    internal const string ToValidateArgumentName = "ToValidate";
    internal const string ValidationErrorListArgumentName = "ViolationList";
    internal const string ToValidateContextArgumentName = "ToValidateContext";
    private readonly RuntimeArgument _toValidate;
    private readonly RuntimeArgument _violationList;
    private readonly RuntimeArgument _toValidateContext;

    internal Constraint()
    {
        _toValidate = new RuntimeArgument(ToValidateArgumentName, typeof(object), ArgumentDirection.In);
        _toValidateContext = new RuntimeArgument(ToValidateContextArgumentName, typeof(ValidationContext), ArgumentDirection.In);
        _violationList = new RuntimeArgument(ValidationErrorListArgumentName, typeof(IList<ValidationError>), ArgumentDirection.Out);
    }

    public static void AddValidationError(NativeActivityContext context, ValidationError error)
    {
        if (!(context.Properties.Find(ValidationErrorListPropertyName) is List<ValidationError> validationErrorList))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AddValidationErrorMustBeCalledFromConstraint(typeof(Constraint).Name)));
        }

        validationErrorList.Add(error);
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                _toValidate,
                _violationList,
                _toValidateContext
            });
    }

    protected override void Execute(NativeActivityContext context)
    {
        object objectToValidate = _toValidate.Get<object>(context);
        ValidationContext objectToValidateContext = _toValidateContext.Get<ValidationContext>(context);

        if (objectToValidate == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotValidateNullObject(typeof(Constraint).Name, DisplayName)));
        }

        if (objectToValidateContext == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ValidationContextCannotBeNull(typeof(Constraint).Name, DisplayName)));
        }

        List<ValidationError> validationErrorList = new List<ValidationError>(1);
        context.Properties.Add(ValidationErrorListPropertyName, validationErrorList);

        _violationList.Set(context, validationErrorList);

        OnExecute(context, objectToValidate, objectToValidateContext);
    }

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotContainTypeNames,
    //    Justification = "Can't replace object with Object because of casing rules")]
    protected abstract void OnExecute(NativeActivityContext context, object objectToValidate, ValidationContext objectToValidateContext);
}

[ContentProperty("Body")]
public sealed class Constraint<T> : Constraint
{
    public Constraint() { }

    public ActivityAction<T, ValidationContext> Body { get; set; }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);

        if (Body != null)
        {
            metadata.SetDelegatesCollection(new Collection<ActivityDelegate> { Body });
        }
    }

    protected override void OnExecute(NativeActivityContext context, object objectToValidate, ValidationContext objectToValidateContext)
    {
        if (Body != null)
        {
            context.ScheduleAction(Body, (T)objectToValidate, objectToValidateContext);
        }
    }
}
