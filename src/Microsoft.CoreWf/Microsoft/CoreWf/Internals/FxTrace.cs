// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Diagnostics.Tracing;

namespace Microsoft.CoreWf.Internals
{
    internal static partial class FxTrace
    {
        private const string baseEventSourceName = "TRACESOURCE_NAME";
        private const string EventSourceVersion = "4.0.0.0";
        private static string s_eventSourceName;
        private static ExceptionTrace s_exceptionTrace;

        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode,
        //    Justification = "This template is shared across all assemblies, some of which use this accessor.")]
        public static bool ShouldTraceInformation
        {
            get
            {
                return WfEventSource.Instance.IsEnabled(EventLevel.Informational, EventKeywords.All);
            }
        }

        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode,
        //    Justification = "This template is shared across all assemblies, some of which use this accessor.")]
        public static bool ShouldTraceVerboseToTraceSource
        {
            get
            {
                return WfEventSource.Instance.IsEnabled(EventLevel.Verbose, EventKeywords.All);
            }
        }

        public static ExceptionTrace Exception
        {
            get
            {
                if (s_exceptionTrace == null)
                {
                    // don't need a lock here since a true singleton is not required
                    s_exceptionTrace = new ExceptionTrace(EventSourceName);
                }

                return s_exceptionTrace;
            }
        }

        private static string EventSourceName
        {
            get
            {
                if (s_eventSourceName == null)
                {
                    s_eventSourceName = string.Concat(baseEventSourceName, " ", EventSourceVersion);
                }

                return s_eventSourceName;
            }
        }
    }
}
