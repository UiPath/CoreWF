// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace System.Activities;
using Internals;
using Runtime;
using Validation;
using XamlIntegration;

public abstract class Argument
{
    public static readonly int UnspecifiedEvaluationOrder = -1;

    public const string ResultValue = "Result";
    private ArgumentDirection _direction;
    private RuntimeArgument _runtimeArgument;
    private int _evaluationOrder;

    internal Argument()
    {
        _evaluationOrder = UnspecifiedEvaluationOrder;
    }

    public Type ArgumentType { get; internal set; }

    public ArgumentDirection Direction
    {
        get => _direction;
        internal set
        {
            ArgumentDirectionHelper.Validate(value, "value");
            _direction = value;
        }
    }

    [DefaultValue(-1)]
    public int EvaluationOrder
    {
        get => _evaluationOrder;
        set
        {
            if (value < 0 && value != UnspecifiedEvaluationOrder)
            {
                throw FxTrace.Exception.ArgumentOutOfRange("EvaluationOrder", value, SR.InvalidEvaluationOrderValue);
            }
            _evaluationOrder = value;
        }
    }

    [IgnoreDataMember] // this member is repeated by all subclasses, which we control
    [DefaultValue(null)]
    public ActivityWithResult Expression
    {
        get => ExpressionCore;
        set => ExpressionCore = value;
    }

    internal abstract ActivityWithResult ExpressionCore { get; set; }

    internal RuntimeArgument RuntimeArgument
    {
        get => _runtimeArgument;
        set => _runtimeArgument = value;
    }

    internal bool IsInTree => _runtimeArgument != null && _runtimeArgument.IsInTree;

    internal bool WasDesignTimeNull { get; set; }

    internal int Id
    {
        get
        {
            Fx.Assert(_runtimeArgument != null, "We shouldn't call Id unless we have a runtime argument.");
            return _runtimeArgument.Id;
        }
    }

    internal bool IsEmpty => Expression == null;

    public static Argument CreateReference(Argument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        return ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, argumentToReference.Direction, referencedArgumentName);
    }

    // for ArgumentValueSerializer
    internal bool CanConvertToString(IValueSerializerContext context)
    {
        if (WasDesignTimeNull)
        {
            return true;
        }
        else
        {
            if (EvaluationOrder == UnspecifiedEvaluationOrder)
            {
                return ActivityWithResultValueSerializer.CanConvertToStringWrapper(Expression, context);
            }
            else
            {
                return false;
            }
        }
    }

    internal string ConvertToString(IValueSerializerContext context)
    {
        if (WasDesignTimeNull)
        {
            // this argument instance was artificially created by the runtime
            // to Xaml, this should appear as {x:Null}
            return null;
        }

        return ActivityWithResultValueSerializer.ConvertToStringWrapper(Expression, context);
    }

    public static implicit operator Argument(Func<ActivityContext, object> expression) => expression;

    internal static void Bind(Argument binding, RuntimeArgument argument)
    {
        if (binding != null)
        {
            Fx.Assert(binding.Direction == argument.Direction, "The directions must match.");
            Fx.Assert(binding.ArgumentType == argument.Type, "The types must match.");

            binding.RuntimeArgument = argument;
        }

        argument.BoundArgument = binding;
    }

    internal static void TryBind(Argument binding, RuntimeArgument argument, Activity violationOwner)
    {
        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        bool passedValidations = true;

        if (binding != null)
        {
            if (binding.Direction != argument.Direction)
            {
                violationOwner.AddTempValidationError(new ValidationError(SR.ArgumentDirectionMismatch(argument.Name, argument.Direction, binding.Direction)));
                passedValidations = false;
            }

            if (binding.ArgumentType != argument.Type)
            {
                violationOwner.AddTempValidationError(new ValidationError(SR.ArgumentTypeMismatch(argument.Name, argument.Type, binding.ArgumentType)));
                passedValidations = false;
            }
        }

        if (passedValidations)
        {
            Bind(binding, argument);
        }
    }

    public static Argument Create(Type type, ArgumentDirection direction)
    {
        return ActivityUtilities.CreateArgument(type, direction);
    }

    internal abstract Location CreateDefaultLocation();

    internal abstract void Declare(LocationEnvironment targetEnvironment, ActivityInstance activityInstance);

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public object Get(ActivityContext context) => Get<object>(context);

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public T Get<T>(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        return context.GetValue<T>(RuntimeArgument);
    }

    public void Set(ActivityContext context, object value)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        context.SetValue(RuntimeArgument, value);
    }

    internal void Validate(Activity owner, ref IList<ValidationError> validationErrors)
    {
        if (Expression != null)
        {
            if (Expression.Result != null && !Expression.Result.IsEmpty)
            {
                ValidationError validationError = new(SR.ResultCannotBeSetOnArgumentExpressions, false, RuntimeArgument.Name, owner);
                ActivityUtilities.Add(ref validationErrors, validationError);
            }

            ActivityWithResult actualExpression = Expression;

            if (actualExpression is IExpressionWrapper wrapper)
            {
                actualExpression = wrapper.InnerExpression;
            }

            switch (Direction)
            {
                case ArgumentDirection.In:
                    if (actualExpression.ResultType != ArgumentType)
                    {
                        ActivityUtilities.Add(
                            ref validationErrors,
                            new ValidationError(SR.ArgumentValueExpressionTypeMismatch(ArgumentType, actualExpression.ResultType), false, RuntimeArgument.Name, owner));
                    }
                    break;
                case ArgumentDirection.InOut:
                case ArgumentDirection.Out:
                    Type locationType;
                    if (!ActivityUtilities.IsLocationGenericType(actualExpression.ResultType, out locationType) ||
                        locationType != ArgumentType)
                    {
                        Type expectedType = ActivityUtilities.CreateActivityWithResult(ActivityUtilities.CreateLocation(ArgumentType));
                        ActivityUtilities.Add(
                            ref validationErrors,
                            new ValidationError(SR.ArgumentLocationExpressionTypeMismatch(expectedType.FullName, actualExpression.GetType().FullName), false, RuntimeArgument.Name, owner));
                    }
                    break;
            }
        }
    }

    // optional "fast-path" for arguments that can be resolved synchronously
    internal abstract bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance, ActivityExecutor executor);

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public Location GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        return _runtimeArgument.GetLocation(context);
    }

    internal void ThrowIfNotInTree()
    {
        if (!IsInTree)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentNotInTree(ArgumentType)));
        }
    }

    internal static Location<T> CreateLocation<T>()
    {
        return new Location<T>();
    }

    public interface IExpressionWrapper
    {
        ActivityWithResult InnerExpression { get; }
    }
}
