// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions
{
    // this is an internal interface for EnvironmentLocationReference/EnvironmentLocationValue/LocationReferenceValue to implement
    // to avoid creating instances of those generic types via expensive Activator.CreateInstance.
    internal interface ILocationReferenceExpression
    {
        ActivityWithResult CreateNewInstance(LocationReference locationReference);
    }
}
