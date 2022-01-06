// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Xml.Linq;

namespace System.Activities;
using Internals;
using Runtime;
using Runtime.DurableInstancing;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

// Wrapper over instance data retrieved from the Instance Store but not yet loaded into a WorkflowApplication.
// Once this instance is loaded into a WFApp using WFApp.Load(), this object is stale and trying to abort or reload it wil throw.
// Free-threaded: needs to be resillient to simultaneous loads/aborts on multiple threads
public class WorkflowApplicationInstance
{
    private int _state;

    internal WorkflowApplicationInstance(
        WorkflowApplication.PersistenceManagerBase persistenceManager,
        IDictionary<XName, InstanceValue> values,
        WorkflowIdentity definitionIdentity)
    {
        PersistenceManager = persistenceManager;
        Values = values;
        DefinitionIdentity = definitionIdentity;
        _state = (int)State.Initialized;
    }

    private enum State
    {
        Initialized,
        Loaded,
        Aborted
    }

    public WorkflowIdentity DefinitionIdentity { get; private set; }

    public InstanceStore InstanceStore => PersistenceManager.InstanceStore;

    public Guid InstanceId => PersistenceManager.InstanceId;

    internal WorkflowApplication.PersistenceManagerBase PersistenceManager { get; private set; }

    internal IDictionary<XName, InstanceValue> Values { get; private set; }

    public void Abandon() => Abandon(ActivityDefaults.DeleteTimeout);

    public void Abandon(TimeSpan timeout)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        MarkAsAbandoned();
        WorkflowApplication.DiscardInstance(PersistenceManager, timeout);
    }

    public IAsyncResult BeginAbandon(AsyncCallback callback, object state) => BeginAbandon(ActivityDefaults.DeleteTimeout, callback, state);

    public IAsyncResult BeginAbandon(TimeSpan timeout, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        MarkAsAbandoned();
        return WorkflowApplication.BeginDiscardInstance(PersistenceManager, timeout, callback, state);
    }

#pragma warning disable CA1822 // Mark members as static
    public void EndAbandon(IAsyncResult asyncResult) => WorkflowApplication.EndDiscardInstance(asyncResult);
#pragma warning restore CA1822 // Mark members as static

#if DYNAMICUPDATE
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters, Justification = "Approved Design. Returning a bool makes the intent much clearer than something that just returns a list.")]
    public bool CanApplyUpdate(DynamicUpdateMap updateMap, out IList<ActivityBlockingUpdate> activitiesBlockingUpdate)
    {
        if (updateMap == null)
        {
            throw FxTrace.Exception.ArgumentNull("updateMap");
        }

        activitiesBlockingUpdate = WorkflowApplication.GetActivitiesBlockingUpdate(this, updateMap);
        return activitiesBlockingUpdate == null || activitiesBlockingUpdate.Count == 0;
    } 
#endif

    internal void MarkAsLoaded()
    {
        int oldState = Interlocked.CompareExchange(ref _state, (int)State.Loaded, (int)State.Initialized);
        ThrowIfLoadedOrAbandoned((State)oldState);
    }

    private void MarkAsAbandoned()
    {
        int oldState = Interlocked.CompareExchange(ref _state, (int)State.Aborted, (int)State.Initialized);
        ThrowIfLoadedOrAbandoned((State)oldState);
    }

    private static void ThrowIfLoadedOrAbandoned(State oldState)
    {
        if (oldState == State.Loaded)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationInstanceLoaded));
        }

        if (oldState == State.Aborted)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationInstanceAbandoned));
        }
    }
}
