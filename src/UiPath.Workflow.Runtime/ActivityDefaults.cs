// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

internal static class ActivityDefaults
{
    public static TimeSpan AcquireLockTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan AsyncOperationContextCompleteTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan CloseTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan DeleteTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan InvokeTimeout = TimeSpan.MaxValue;
    public static TimeSpan LoadTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan OpenTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan ResumeBookmarkTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan InternalSaveTimeout = TimeSpan.MaxValue;
    public static TimeSpan TrackingTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan TransactionCompletionTimeout = TimeSpan.FromSeconds(30);
}
