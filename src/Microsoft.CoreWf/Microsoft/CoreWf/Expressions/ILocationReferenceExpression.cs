// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CoreWf.Expressions
{
    // this is an internal interface for EnvironmentLocationReference/EnvironmentLocationValue/LocationReferenceValue to implement
    // to avoid creating instances of those generic types via expensive Activator.CreateInstance.
    internal interface ILocationReferenceExpression
    {
        ActivityWithResult CreateNewInstance(LocationReference locationReference);
    }
}
