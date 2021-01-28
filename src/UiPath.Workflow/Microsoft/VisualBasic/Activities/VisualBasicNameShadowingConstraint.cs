// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.VisualBasic.Activities
{
    using Microsoft.Common;
    using System;
    using System.Activities;
    using System.Activities.Validation;

    sealed class VisualBasicNameShadowingConstraint : NameShadowingConstraint
    {
        public override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

        public override void AddValidationErrorInternal(NativeActivityContext context, string referenceName)
        {
            AddValidationError(context, new ValidationError(SR.AmbiguousVBVariableReference(referenceName)));
        }
    }
}