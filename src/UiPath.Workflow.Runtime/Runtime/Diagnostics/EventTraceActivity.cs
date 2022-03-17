// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities.Runtime.Diagnostics;

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
            s_empty ??= new EventTraceActivity(Guid.Empty);
            return s_empty;
        }
    }

    public static string Name
    {
        get { return "E2EActivity"; }
    }

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

    public static Guid GetActivityIdFromThread()
    {
        //return Trace.CorrelationManager.ActivityId;
        return ActivityIdThreadLocal.Value;
    }

    public void SetActivityId(Guid guid)
    {
        ActivityId = guid;
    }

    private void SetActivityIdOnThread()
    {
        //Trace.CorrelationManager.ActivityId = ActivityId;
        ActivityIdThreadLocal.Value = ActivityId;
    }
}
