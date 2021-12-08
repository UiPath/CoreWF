// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities;
using Expressions;
using Internals;
using Runtime;

public abstract class InOutArgument : Argument
{
    internal InOutArgument()
    {
        Direction = ArgumentDirection.InOut;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
    public static InOutArgument CreateReference(InOutArgument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        return (InOutArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.InOut, referencedArgumentName);
    }
}

[ContentProperty("Expression")]
[TypeConverter(TypeConverters.InOutArgumentConverter)]
[ValueSerializer(OtherXaml.ArgumentValueSerializer)]
public sealed class InOutArgument<T> : InOutArgument
{
    public InOutArgument(Variable variable)
        : this()
    {
        if (variable != null)
        {
            Expression = new VariableReference<T> { Variable = variable };
        }
    }

    public InOutArgument(Variable<T> variable)
        : this()
    {
        if (variable != null)
        {
            Expression = new VariableReference<T> { Variable = variable };
        }
    }

    public InOutArgument(Expression<Func<ActivityContext, T>> expression)
        : this()
    {
        if (expression != null)
        {
            Expression = new LambdaReference<T>(expression);
        }
    }

    public InOutArgument(Activity<Location<T>> expression)
        : this()
    {
        Expression = expression;
    }

    public InOutArgument()
        : base()
    {
        ArgumentType = typeof(T);
    }

    [DefaultValue(null)]
    public new Activity<Location<T>> Expression { get; set; }

    internal override ActivityWithResult ExpressionCore
    {
        get => Expression;
        set
        {
            if (value == null)
            {
                Expression = null;
                return;
            }


            if (value is Activity<Location<T>> typedActivity)
            {
                Expression = typedActivity;
            }
            else
            {
                // We do not verify compatibility here. We will do that
                // during CacheMetadata in Argument.Validate.
                Expression = new ActivityWithResultWrapper<Location<T>>(value);
            }
        }
    }

    public static implicit operator InOutArgument<T>(Variable<T> variable) => FromVariable(variable);

    public static explicit operator InOutArgument<T>(string locationReferenceName) => FromExpression(new Reference<T>(locationReferenceName));

    public static implicit operator InOutArgument<T>(Activity<Location<T>> expression) => FromExpression(expression);

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]    
    public static InOutArgument<T> FromVariable(Variable<T> variable) => new() { Expression = new VariableReference<T> { Variable = variable } };

    public static InOutArgument<T> FromExpression(Activity<Location<T>> expression)
    {
        if (expression == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        return new InOutArgument<T>
        {
            Expression = expression
        };
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public new Location<T> GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        return context.GetLocation<T>(RuntimeArgument);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public new T Get(ActivityContext context) => Get<T>(context);

    public void Set(ActivityContext context, T value)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        context.SetValue(this, value);
    }

    internal override Location CreateDefaultLocation() => CreateLocation<T>();

    internal override void Declare(LocationEnvironment targetEnvironment, ActivityInstance activityInstance)
        => targetEnvironment.DeclareTemporaryLocation<Location<T>>(RuntimeArgument, activityInstance, false);

    internal override bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance, ActivityExecutor executor)
    {
        Fx.Assert(Expression != null, "This should only be called for non-empty bindings.");

        if (Expression.UseOldFastPath)
        {
            Location<T> argumentValue = executor.ExecuteInResolutionContext(targetActivityInstance, Expression);
            targetEnvironment.Declare(RuntimeArgument, argumentValue.CreateReference(false), targetActivityInstance);
            return true;
        }
        else
        {
            targetEnvironment.DeclareTemporaryLocation<Location<T>>(RuntimeArgument, targetActivityInstance, false);
            return false;
        }
    }
}
