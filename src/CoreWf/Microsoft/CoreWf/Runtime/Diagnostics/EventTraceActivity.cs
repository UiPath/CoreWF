// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Threading;

namespace CoreWf.Runtime.Diagnostics
{
    internal class EventTraceActivity
    {
        // We're using thread local instead of Trace.CorrelationManager since it's not supported on core.
        public static readonly ThreadLocal<Guid> ActivityIdThreadLocal;
        // This field is public because it needs to be passed by reference for P/Invoke
        public Guid ActivityId;
        private static EventTraceActivity s_empty;

        static EventTraceActivity()
        {
            ActivityIdThreadLocal = new ThreadLocal<Guid>(() => Guid.NewGuid());
        }

        public EventTraceActivity(bool setOnThread = false)
            : this(Guid.NewGuid(), setOnThread)
        {
        }

        public EventTraceActivity(Guid guid, bool setOnThread = false)
        {
            this.ActivityId = guid;
            if (setOnThread)
            {
                SetActivityIdOnThread();
            }
        }


        public static EventTraceActivity Empty
        {
            get
            {
                if (s_empty == null)
                {
                    s_empty = new EventTraceActivity(Guid.Empty);
                }

                return s_empty;
            }
        }

        public static string Name
        {
            get { return "E2EActivity"; }
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because the CorrelationManager property has a link demand on UnmanagedCode.",
            Safe = "We do not leak security data.")]
        [SecuritySafeCritical]
        public static EventTraceActivity GetFromThreadOrCreate(bool clearIdOnThread = false)
        {
            //Guid guid = Trace.CorrelationManager.ActivityId;
            var guid = ActivityIdThreadLocal.Value;
            if (guid == Guid.Empty)
            {
                guid = Guid.NewGuid();
            }
            else if (clearIdOnThread)
            {
                // Reset the ActivityId on the thread to avoid using the same Id again
                //Trace.CorrelationManager.ActivityId = Guid.Empty;
                ActivityIdThreadLocal.Value = Guid.Empty;
            }

            return new EventTraceActivity(guid);
        }

        [Fx.Tag.SecurityNote(Critical = "Critical because the CorrelationManager property has a link demand on UnmanagedCode.",
            Safe = "We do not leak security data.")]
        [SecuritySafeCritical]
        public static Guid GetActivityIdFromThread()
        {
            //return Trace.CorrelationManager.ActivityId;
            return ActivityIdThreadLocal.Value;
        }

        public void SetActivityId(Guid guid)
        {
            this.ActivityId = guid;
        }
        [Fx.Tag.SecurityNote(Critical = "Critical because the CorrelationManager property has a link demand on UnmanagedCode.",
                    Safe = "We do not leak security data.")]
        [SecuritySafeCritical]

        private void SetActivityIdOnThread()
        {
            //Trace.CorrelationManager.ActivityId = this.ActivityId;
            ActivityIdThreadLocal.Value = this.ActivityId;
        }
    }
}
