// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

public static class WorkflowInstanceStates
{
    public const string Aborted = "Aborted";
    public const string Canceled = "Canceled";
    public const string Completed = "Completed";
    public const string Deleted = "Deleted";
    public const string Idle = "Idle";
    public const string Persisted = "Persisted";
    public const string Resumed = "Resumed";
    public const string Started = "Started";
    public const string Suspended = "Suspended";
    public const string Terminated = "Terminated";
    public const string UnhandledException = "UnhandledException";
    public const string Unloaded = "Unloaded";
    public const string Unsuspended = "Unsuspended";
    public const string Updated = "Updated";
    public const string UpdateFailed = "UpdateFailed";
}
