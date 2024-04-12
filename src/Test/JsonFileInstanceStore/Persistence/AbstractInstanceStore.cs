using System;
using System.Activities.DurableInstancing;
using System.Diagnostics;
using System.IO;
using System.Activities.Runtime.DurableInstancing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Nito.AsyncEx.Interop;

namespace JsonFileInstanceStore.Persistence;


using InstanceDictionary = Dictionary<string, InstanceValue>;
using XInstanceDictionary = IDictionary<XName, InstanceValue>;
public interface IWorkflowSerializer
{
    /// <summary>
    /// Load workflow instance values from the sourceStream
    /// </summary>
    /// <param name="sourceStream">The source stream for the persisted data</param>
    /// <returns>Workflow instance state to be used</returns>
    public XInstanceDictionary LoadWorkflowInstance(Stream sourceStream);
    /// <summary>
    /// Persist the workflow instance values into the destination stream
    /// </summary>
    /// <param name="workflowInstanceState">The workflow instance state</param>
    /// <param name="destinationStream">The destination stream for the persisted data</param>
    public void SaveWorkflowInstance(XInstanceDictionary workflowInstanceState, Stream destinationStream);
}

static class WorkflowSerializerHelpers
{
    public static InstanceDictionary ToSave(this XInstanceDictionary source) => source
        .Where(property => !property.Value.Options.HasFlag(InstanceValueOptions.WriteOnly) && !property.Value.IsDeletedValue)
        .ToDictionary(property => property.Key.ToString(), property => property.Value);
    public static XInstanceDictionary ToNameDictionary(object source) => ((InstanceDictionary)source)
        .ToDictionary(sourceItem => (XName)sourceItem.Key, sourceItem => sourceItem.Value);
}

public abstract class AbstractInstanceStore(IWorkflowSerializer instanceSerializer) : InstanceStore
{
    private Guid _storageInstanceId = Guid.NewGuid();
    private Guid _lockId = Guid.NewGuid();
    private readonly IWorkflowSerializer _instanceSerializer = instanceSerializer;

    protected abstract Task<Stream> GetReadStream(Guid instanceId);
    protected abstract Task<Stream> GetWriteStream(Guid instanceId);

    protected sealed override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
    {
        return ApmAsyncFactory.ToBegin(TryCommandAndYield(), callback, state);
        async Task<bool> TryCommandAndYield()
        {
            try
            {
                var result = await TryCommandAsync(context, command);
                // When TryCommandAsync completes synchronously, prevent chaining the WF state machine steps
                // which can lead to large stack, e.g. BeginRun->ActivityComplete->Persist->Complete
                await Task.Yield();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }

    protected sealed override bool EndTryCommand(IAsyncResult result)
    {
        var task = (Task<bool>)result;
        return task.Result;
    }

    private async Task<bool> TryCommandAsync(InstancePersistenceContext context, InstancePersistenceCommand command)
    {
        if (command is CreateWorkflowOwnerCommand)
        {
            CreateWorkflowOwner(context);
        }
        else if (command is CreateWorkflowOwnerWithIdentityCommand)
        {
            CreateWorkflowOwnerWithIdentity(context);
        }
        else if (command is SaveWorkflowCommand)
        {
            await SaveWorkflow(context, (SaveWorkflowCommand)command);
        }
        else if (command is LoadWorkflowCommand)
        {
            await LoadWorkflow(context);
        }
        else if (command is DeleteWorkflowOwnerCommand)
        {
            //Nothing to 'delete', we don't implement locking
        }
        else
        {
            // This trace is for dev team, not actionable by end user.
            Trace.TraceInformation($"Persistence command {command.Name} is not supported.");
            return false;
        }

        Trace.TraceInformation($"Persistence command {command.Name} issued for job with Id {context.InstanceView.InstanceId}.");
        return true;
    }

    private async Task LoadWorkflow(InstancePersistenceContext context)
    {
        using var stream = await GetReadStream(context.InstanceView.InstanceId);
        context.LoadedInstance(InstanceState.Initialized, _instanceSerializer.LoadWorkflowInstance(stream), null, null, null);
    }

    private async Task SaveWorkflow(InstancePersistenceContext context, SaveWorkflowCommand command)
    {
        using var stream = await GetWriteStream(context.InstanceView.InstanceId);
        _instanceSerializer.SaveWorkflowInstance(command.InstanceData, stream);
    }

    private void CreateWorkflowOwner(InstancePersistenceContext context)
    {
        context.BindInstanceOwner(_storageInstanceId, _lockId);
    }

    private void CreateWorkflowOwnerWithIdentity(InstancePersistenceContext context)
    {
        context.BindInstanceOwner(_storageInstanceId, _lockId);
    }
}
