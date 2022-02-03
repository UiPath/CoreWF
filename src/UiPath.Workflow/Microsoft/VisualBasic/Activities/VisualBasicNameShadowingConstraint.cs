// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;

namespace Microsoft.VisualBasic.Activities;

internal sealed class VisualBasicNameShadowingConstraint : Constraint
{
    protected override void OnExecute(NativeActivityContext context, object objectToValidate,
        ValidationContext objectToValidateContext)
    {
        var activity = (ActivityWithResult) objectToValidate;

        foreach (var runtimeArgument in activity.RuntimeArguments)
        {
            var boundExpression = runtimeArgument.BoundArgument.Expression;

            if (boundExpression is ILocationReferenceWrapper wrapper)
            {
                var locationReference = wrapper.LocationReference;

                if (locationReference != null)
                {
                    var foundMultiple = FindLocationReferencesFromEnvironment(objectToValidateContext.Environment,
                        locationReference.Name);
                    if (foundMultiple)
                    {
                        AddValidationError(context,
                            new ValidationError(SR.AmbiguousVBVariableReference(locationReference.Name)));
                    }
                }
            }
        }
    }

    private static bool FindLocationReferencesFromEnvironment(LocationReferenceEnvironment environment,
        string targetName)
    {
        LocationReference foundLocationReference = null;
        var currentEnvironment = environment;
        while (currentEnvironment != null)
        {
            foreach (var reference in currentEnvironment.GetLocationReferences())
            {
                if (string.Equals(reference.Name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    if (foundLocationReference != null)
                    {
                        return true;
                    }

                    foundLocationReference = reference;
                }
            }

            currentEnvironment = currentEnvironment.Parent;
        }

        return false;
    }
}
