// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.VisualBasic.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Validation;
    using System.Globalization;
    using System.Reflection;     

    sealed class VisualBasicNameShadowingConstraint : Constraint
    {
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
                        foundMultiple = FindLocationReferencesFromEnvironment(objectToValidateContext.Environment, locationReference.Name);
                        if (foundMultiple)
                        {
                            Constraint.AddValidationError(context, new ValidationError(SR.AmbiguousVBVariableReference(locationReference.Name)));
                        }
                    }
                }               
            }
        }

        static bool FindLocationReferencesFromEnvironment(LocationReferenceEnvironment environment, string targetName)
        {
            LocationReference foundLocationReference = null;            
            LocationReferenceEnvironment currentEnvironment;
            bool foundMultiple = false;

            currentEnvironment = environment;            
            while (currentEnvironment != null)
            {
                foreach (LocationReference reference in currentEnvironment.GetLocationReferences())
                {
                    if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
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
