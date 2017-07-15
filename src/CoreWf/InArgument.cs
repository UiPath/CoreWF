// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Expressions;
using CoreWf.Runtime;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CoreWf
{
    public abstract class InArgument : Argument
    {
        internal InArgument()
            : base()
        {
            this.Direction = ArgumentDirection.In;
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
        public static InArgument CreateReference(InArgument argumentToReference, string referencedArgumentName)
        {
            if (argumentToReference == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("argumentToReference");
            }

            if (string.IsNullOrEmpty(referencedArgumentName))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("referencedArgumentName");
            }

            return (InArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.In, referencedArgumentName);
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Subclass needed to enforce rules about which directions can be referenced.")]
        public static InArgument CreateReference(InOutArgument argumentToReference, string referencedArgumentName)
        {
            if (argumentToReference == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("argumentToReference");
            }

            if (string.IsNullOrEmpty(referencedArgumentName))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("referencedArgumentName");
            }

            // Note that we explicitly pass In since we want an InArgument created
            return (InArgument)ActivityUtilities.CreateReferenceArgument(argumentToReference.ArgumentType, ArgumentDirection.In, referencedArgumentName);
        }
    }

    //[ContentProperty("Expression")]
    //[TypeConverter(typeof(InArgumentConverter))]
    //[ValueSerializer(typeof(ArgumentValueSerializer))]
    public sealed class InArgument<T> : InArgument
    {
        public InArgument(Variable variable)
            : this()
        {
            if (variable != null)
            {
                this.Expression = new VariableValue<T> { Variable = variable };
            }
        }

        public InArgument(DelegateArgument delegateArgument)
            : this()
        {
            if (delegateArgument != null)
            {
                this.Expression = new DelegateArgumentValue<T> { DelegateArgument = delegateArgument };
            }
        }

        public InArgument(T constValue)
            : this()
        {
            this.Expression = new Literal<T> { Value = constValue };
        }

        public InArgument(Expression<Func<ActivityContext, T>> expression)
            : this()
        {
            if (expression != null)
            {
                this.Expression = new LambdaValue<T>(expression);
            }
        }

        public InArgument(Activity<T> expression)
            : this()
        {
            this.Expression = expression;
        }


        public InArgument()
            : base()
        {
            this.ArgumentType = typeof(T);
        }

        [DefaultValue(null)]
        public new Activity<T> Expression
        {
            get;
            set;
        }

        internal override ActivityWithResult ExpressionCore
        {
            get
            {
                return this.Expression;
            }
            set
            {
                if (value == null)
                {
                    this.Expression = null;
                    return;
                }

                if (value is Activity<T>)
                {
                    this.Expression = (Activity<T>)value;
                }
                else
                {
                    // We do not verify compatibility here. We will do that
                    // during CacheMetadata in Argument.Validate.
                    this.Expression = new ActivityWithResultWrapper<T>(value);
                }
            }
        }

        public static implicit operator InArgument<T>(Variable variable)
        {
            return FromVariable(variable);
        }

        public static implicit operator InArgument<T>(DelegateArgument delegateArgument)
        {
            return FromDelegateArgument(delegateArgument);
        }

        public static implicit operator InArgument<T>(Activity<T> expression)
        {
            return FromExpression(expression);
        }

        public static implicit operator InArgument<T>(T constValue)
        {
            return FromValue(constValue);
        }

        public static InArgument<T> FromVariable(Variable variable)
        {
            if (variable == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("variable");
            }
            return new InArgument<T>(variable);
        }

        public static InArgument<T> FromDelegateArgument(DelegateArgument delegateArgument)
        {
            if (delegateArgument == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("delegateArgument");
            }
            return new InArgument<T>(delegateArgument);
        }

        public static InArgument<T> FromExpression(Activity<T> expression)
        {
            if (expression == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("expression");
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
        public new T Get(ActivityContext context)
        {
            return Get<T>(context);
        }

        public void Set(ActivityContext context, T value)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.SetValue(this, value);
        }

        internal override Location CreateDefaultLocation()
        {
            return Argument.CreateLocation<T>();
        }

        internal override bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance activityInstance, ActivityExecutor executor)
        {
            Fx.Assert(this.Expression != null, "This should only be called for non-empty bindings");

            Location<T> location = Argument.CreateLocation<T>();
            targetEnvironment.Declare(this.RuntimeArgument, location, activityInstance);

            if (this.Expression.UseOldFastPath)
            {
                location.Value = executor.ExecuteInResolutionContext<T>(activityInstance, this.Expression);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal override void Declare(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance)
        {
            targetEnvironment.Declare(this.RuntimeArgument, CreateDefaultLocation(), targetActivityInstance);
        }
    }
}
