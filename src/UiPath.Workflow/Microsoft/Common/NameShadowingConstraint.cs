namespace Microsoft.Common
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Validation;

    internal abstract class NameShadowingConstraint : Constraint
    {
        public abstract StringComparison StringComparison { get; }
        public abstract void AddValidationErrorInternal(NativeActivityContext context, string referenceName);

        protected override void OnExecute(NativeActivityContext context, object objectToValidate, ValidationContext objectToValidateContext)
        {
            bool foundMultiple;
            ActivityWithResult boundExpression;
            LocationReference locationReference;
            ActivityWithResult activity = (ActivityWithResult)objectToValidate;

            foreach (RuntimeArgument runtimeArgument in activity.RuntimeArguments)
            {
                boundExpression = runtimeArgument.BoundArgument.Expression;

                if (boundExpression != null && boundExpression is ILocationReferenceWrapper)
                {
                    locationReference = ((ILocationReferenceWrapper)boundExpression).LocationReference;

                    if (locationReference != null)
                    {
                        foundMultiple = FindLocationReferencesFromEnvironment(objectToValidateContext.Environment, locationReference.Name, StringComparison);
                        if (foundMultiple)
                        {
                            AddValidationErrorInternal(context, locationReference.Name);
                        }
                    }
                }
            }
        }

        static bool FindLocationReferencesFromEnvironment(LocationReferenceEnvironment environment, string targetName, StringComparison stringComparison)
        {
            LocationReference foundLocationReference = null;
            LocationReferenceEnvironment currentEnvironment;
            bool foundMultiple = false;

            currentEnvironment = environment;
            while (currentEnvironment != null)
            {
                foreach (LocationReference reference in currentEnvironment.GetLocationReferences())
                {
                    if (string.Equals(reference.Name, targetName, stringComparison))
                    {
                        if (foundLocationReference != null)
                        {
                            foundMultiple = true;
                            return foundMultiple;
                        }

                        foundLocationReference = reference;
                    }
                }

                currentEnvironment = currentEnvironment.Parent;
            }

            return foundMultiple;
        }
    }
}