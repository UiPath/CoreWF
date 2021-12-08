// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public abstract partial class Activity
{
    internal enum RelationshipType : byte
    {
        Child = 0x00,
        ImportedChild = 0x01,
        ImplementationChild = 0x02,
        DelegateHandler = 0x03,
        ArgumentExpression = 0x04,
        VariableDefault = 0x05
    }
}
