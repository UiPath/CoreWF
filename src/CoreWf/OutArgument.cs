// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities;
using Expressions;
using Internals;
using Runtime;

public abstract class OutArgument : Argument
{
    internal OutArgument()
    {
        Direction = ArgumentDirection.Out;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
    public static OutArgument CreateReference(OutArgument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        return (OutArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.Out, referencedArgumentName);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
    public static OutArgument CreateReference(InOutArgument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        // Note that we explicitly pass Out since we want an OutArgument created
        return (OutArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.Out, referencedArgumentName);
    }
}

[ContentProperty("Expression")]
[TypeConverter(TypeConverters.OutArgumentConverter)]    
[ValueSerializer(OtherXaml.ArgumentValueSerializer)]
public sealed class OutArgument<T> : OutArgument
{
    public OutArgument(Variable variable)
        : this()
    {
        if (variable != null)
        {
            Expression = new VariableReference<T> { Variable = variable };
        }
    }

    public OutArgument(DelegateArgument delegateArgument)
        : this()
    {
        if (delegateArgument != null)
        {
            Expression = new DelegateArgumentReference<T> { DelegateArgument = delegateArgument };
        }
    }

    public OutArgument(Expression<Func<ActivityContext, T>> expression)
        : this()
    {
        if (expression != null)
        {
            Expression = new LambdaReference<T>(expression);
        }
    }

    public OutArgument(Activity<Location<T>> expression)
        : this()
    {
        Expression = expression;
    }

    public OutArgument()
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

            if (value is Activity<Location<T>> activity)
            {
                Expression = activity;
            }
            else
            {
                // We do not verify compatibility here. We will do that
                // during CacheMetadata in Argument.Validate.
                Expression = new ActivityWithResultWrapper<Location<T>>(value);
            }
        }
    }

    public static implicit operator OutArgument<T>(Variable variable) => FromVariable(variable);

    public static implicit operator OutArgument<T>(DelegateArgument delegateArgument) => FromDelegateArgument(delegateArgument);

    public static explicit operator OutArgument<T>(string locationReferenceName) => FromExpression(new Reference<T>(locationReferenceName));

    public static implicit operator OutArgument<T>(Activity<Location<T>> expression) => FromExpression(expression);

    public static OutArgument<T> FromVariable(Variable variable)
    {
        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }
        return new OutArgument<T>(variable);
    }

    public static OutArgument<T> FromDelegateArgument(DelegateArgument delegateArgument)
    {
        if (delegateArgument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(delegateArgument));
        }
        return new OutArgument<T>(delegateArgument);
    }

    public static OutArgument<T> FromExpression(Activity<Location<T>> expression)
    {
        if (expression == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        return new OutArgument<T>(expression);
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

    internal override Location CreateDefaultLocation()
    {
        return CreateLocation<T>();
    }

    internal override void Declare(LocationEnvironment targetEnvironment, ActivityInstance activityInstance)
    {
        targetEnvironment.DeclareTemporaryLocation<Location<T>>(RuntimeArgument, activityInstance, true);
    }

    internal override bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance, ActivityExecutor executor)
    {
        Fx.Assert(Expression != null, "This should only be called for non-empty bindings.");

        if (Expression.UseOldFastPath)
        {
            Location<T> argumentValue = executor.ExecuteInResolutionContext(targetActivityInstance, Expression);
            targetEnvironment.Declare(RuntimeArgument, argumentValue.CreateReference(true), targetActivityInstance);
            return true;
        }
        else
        {
            targetEnvironment.DeclareTemporaryLocation<Location<T>>(RuntimeArgument, targetActivityInstance, true);
            return false;
        }
    }
}
