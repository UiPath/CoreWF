using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WorkflowApplicationTestExtensions.Persistence;

public class MemoryInstanceStore(IWorkflowSerializer workflowSerializer) : AbstractInstanceStore(workflowSerializer)
{
    private readonly ConcurrentDictionary<Guid, MemoryStream>  _cache = new();

    public MemoryInstanceStore() : this(new JsonWorkflowSerializer()) { }

    protected override void OnLoadDone(Guid instanceId, Stream stream)
    => _cache.Remove(instanceId, out _);

    protected override Task<Stream> GetLoadStream(Guid instanceId)
    {
        _cache[instanceId].TryGetBuffer(out var buffer);
        return Task.FromResult<Stream>(new MemoryStream(buffer.Array, buffer.Offset, buffer.Count));
    }

    protected override Task<Stream> GetSaveStream(Guid instanceId)
    => Task.FromResult<Stream>(_cache[instanceId] = new MemoryStream());
}

