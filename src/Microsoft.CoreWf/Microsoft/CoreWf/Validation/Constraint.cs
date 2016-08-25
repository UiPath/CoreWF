// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Validation
{
    public abstract class Constraint : NativeActivity
    {
        public const string ValidationErrorListPropertyName = "Microsoft.CoreWf.Validation.Constraint.ValidationErrorList";

        internal const string ToValidateArgumentName = "ToValidate";
        internal const string ValidationErrorListArgumentName = "ViolationList";
        internal const string ToValidateContextArgumentName = "ToValidateContext";

        private RuntimeArgument _toValidate;
        private RuntimeArgument _violationList;
        private RuntimeArgument _toValidateContext;

        internal Constraint()
        {
            _toValidate = new RuntimeArgument(ToValidateArgumentName, typeof(object), ArgumentDirection.In);
            _toValidateContext = new RuntimeArgument(ToValidateContextArgumentName, typeof(ValidationContext), ArgumentDirection.In);
            _violationList = new RuntimeArgument(ValidationErrorListArgumentName, typeof(IList<ValidationError>), ArgumentDirection.Out);
        }

        public static void AddValidationError(NativeActivityContext context, ValidationError error)
        {
            List<ValidationError> validationErrorList = context.Properties.Find(ValidationErrorListPropertyName) as List<ValidationError>;

            if (validationErrorList == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.AddValidationErrorMustBeCalledFromConstraint(typeof(Constraint).Name)));
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotValidateNullObject(typeof(Constraint).Name, this.DisplayName)));
            }

            if (objectToValidateContext == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ValidationContextCannotBeNull(typeof(Constraint).Name, this.DisplayName)));
            }

            List<ValidationError> validationErrorList = new List<ValidationError>(1);
            context.Properties.Add(ValidationErrorListPropertyName, validationErrorList);

            _violationList.Set(context, validationErrorList);

            OnExecute(context, objectToValidate, objectToValidateContext);
        }

        //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotContainTypeNames,
        //Justification = "Can't replace object with Object because of casing rules")]
        protected abstract void OnExecute(NativeActivityContext context, object objectToValidate, ValidationContext objectToValidateContext);
    }

    //[ContentProperty("Body")]
    public sealed class Constraint<T> : Constraint
    {
        public Constraint()
        {
        }

        public ActivityAction<T, ValidationContext> Body
        {
            get;
            set;
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (this.Body != null)
            {
                metadata.SetDelegatesCollection(new Collection<ActivityDelegate> { this.Body });
            }
        }

        protected override void OnExecute(NativeActivityContext context, object objectToValidate, ValidationContext objectToValidateContext)
        {
            if (this.Body != null)
            {
                context.ScheduleAction(this.Body, (T)objectToValidate, objectToValidateContext);
            }
        }
    }
}
