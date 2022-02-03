// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Debugger;

public sealed class SourceLocationFoundEventArgs : EventArgs
{
    public SourceLocationFoundEventArgs(object target, SourceLocation sourceLocation)
    {
        UnitTestUtility.Assert(target != null, "Target cannot be null and is ensured by caller");
        UnitTestUtility.Assert(sourceLocation != null, "Target cannot be null and is ensured by caller");
        Target = target;
        SourceLocation = sourceLocation;
    }

    internal SourceLocationFoundEventArgs(object target, SourceLocation sourceLocation, bool isValueNode)
        : this(target, sourceLocation)
    {
        IsValueNode = isValueNode;
    }

    public object Target { get; }

    public SourceLocation SourceLocation { get; }

    internal bool IsValueNode { get; }
}
