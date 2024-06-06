// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

/// <summary>
/// This will be used to facilitate identifing objects that are more meaningfull in a designer as a breakpoint target
/// <br/><br/>
/// eg: in a Diagram/flow node an implementation/runtime activity that implements this interface will trigger the tracker/debuger
/// then the debugger will do the check:
/// <code>
/// if (targetObj is IRedirectDebugTarget redirection)
///   targetObj = redirection.Target
/// ...
/// return GetIdRef(targetObj)
/// </code>
/// </summary>
public interface IRedirectDebugTarget
{
    object Target { get; }
}
