// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities;
using Expressions;
using Internals;
using Runtime;

public abstract class InArgument : Argument
{
    internal InArgument()
        : base()
    {
        Direction = ArgumentDirection.In;
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
    public static InArgument CreateReference(InArgument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        return (InArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.In, referencedArgumentName);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
    public static InArgument CreateReference(InOutArgument argumentToReference, string referencedArgumentName)
    {
        if (argumentToReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argumentToReference));
        }

        if (string.IsNullOrEmpty(referencedArgumentName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(referencedArgumentName));
        }

        // Note that we explicitly pass In since we want an InArgument created
        return (InArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.In, referencedArgumentName);
    }
}

[ContentProperty("Expression")]
[TypeConverter(TypeConverters.InArgumentConverter)]
[ValueSerializer(OtherXaml.ArgumentValueSerializer)]
public sealed class InArgument<T> : InArgument
{
    public InArgument(Variable variable)
        : this()
    {
        if (variable != null)
        {
            Expression = new VariableValue<T> { Variable = variable };
        }
    }

    public InArgument(DelegateArgument delegateArgument)
        : this()
    {
        if (delegateArgument != null)
        {
            Expression = new DelegateArgumentValue<T> { DelegateArgument = delegateArgument };
        }
    }

    public InArgument(T constValue)
        : this()
    {
        Expression = new Literal<T> { Value = constValue }; 
    }

    public InArgument(Expression<Func<ActivityContext, T>> expression)
        : this()
    {
        if (expression != null)
        {                
            Expression = new LambdaValue<T>(expression);
        }
    }

    public InArgument(Activity<T> expression)
        : this()
    {
        Expression = expression;
    }


    public InArgument()
        : base()
    {
        ArgumentType = typeof(T);
    }

    [DefaultValue(null)]
    public new Activity<T> Expression { get; set; }

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

            if (value is Activity<T> activity)
            {
                Expression = activity;
            }
            else
            {
                // We do not verify compatibility here. We will do that
                // during CacheMetadata in Argument.Validate.
                Expression = new ActivityWithResultWrapper<T>(value);
            }
        }
    }

    public static implicit operator InArgument<T>(Variable variable) => FromVariable(variable);

    public static implicit operator InArgument<T>(DelegateArgument delegateArgument) => FromDelegateArgument(delegateArgument);

    public static implicit operator InArgument<T>(Activity<T> expression) => FromExpression(expression);

    public static implicit operator InArgument<T>(Func<ActivityContext, T> expression) => FromExpression(new FuncValue<T>(expression));

    public static implicit operator InArgument<T>(T constValue) => FromValue(constValue);

    public static InArgument<T> FromVariable(Variable variable)
    {
        if (variable == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(variable));
        }
        return new InArgument<T>(variable);
    }

    public static InArgument<T> FromDelegateArgument(DelegateArgument delegateArgument)
    {
        if (delegateArgument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(delegateArgument));
        }
        return new InArgument<T>(delegateArgument);
    }

    public static InArgument<T> FromExpression(Activity<T> expression)
    {
        if (expression == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        return new InArgument<T>(expression);
    }

    public static InArgument<T> FromValue(T constValue)
    {
        return new InArgument<T>
            {
                Expression = new Literal<T> { Value = constValue }
            };
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

        context.SetValue(this, value);
    }

    internal override Location CreateDefaultLocation() => CreateLocation<T>();

    internal override bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance activityInstance, ActivityExecutor executor)
    {
        Fx.Assert(Expression != null, "This should only be called for non-empty bindings");

        Location<T> location = CreateLocation<T>();
        targetEnvironment.Declare(RuntimeArgument, location, activityInstance);

        if (Expression.UseOldFastPath)
        {
            location.Value = executor.ExecuteInResolutionContext(activityInstance, Expression);
            return true;
        }
        else
        {
            return false;
        }
    }

    internal override void Declare(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance)
        => targetEnvironment.Declare(RuntimeArgument, CreateDefaultLocation(), targetActivityInstance);
}
