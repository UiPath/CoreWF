// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Diagnostics.Tracing;

namespace System.Activities.Internals;

internal static partial class FxTrace
{
    private const string BaseEventSourceName = "TRACESOURCE_NAME";
    private const string EventSourceVersion = "4.0.0.0";
    private static string s_eventSourceName;
    private static ExceptionTrace s_exceptionTrace;

    public static bool ShouldTraceInformation => WfEventSource.Instance.IsEnabled(EventLevel.Informational, EventKeywords.All);

    public static bool ShouldTraceVerboseToTraceSource => WfEventSource.Instance.IsEnabled(EventLevel.Verbose, EventKeywords.All);

    public static ExceptionTrace Exception
    {
        get
        {
            // don't need a lock here since a true singleton is not required
            s_exceptionTrace ??= new ExceptionTrace(EventSourceName);
            return s_exceptionTrace;
        }
    }

    private static string EventSourceName
    {
        get
        {
            s_eventSourceName ??= string.Concat(BaseEventSourceName, " ", EventSourceVersion);
            return s_eventSourceName;
        }
    }
}
