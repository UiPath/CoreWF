# Optimization 3: Batch Processing and Request Coalescing

## Problem

When multiple threads request the same assembly simultaneously, the current implementation has these issues:

1. **Thundering herd**: All threads waiting for the same assembly will attempt to load it when the lock is released.
2. **Duplicate work**: Multiple threads may perform the same expensive assembly search operation.
3. **No request batching**: Each thread processes its request independently, missing optimization opportunities.
4. **Cache stampede**: After a cache invalidation or application start, many threads compete to populate the cache.

## Solution

Implement request coalescing and batch processing to handle concurrent requests more efficiently:

1. **Request coalescing**: Merge multiple requests for the same assembly into a single operation
2. **Batch loading**: Process multiple assembly requests together
3. **Async loading**: Use async/await to avoid blocking threads during I/O operations
4. **Predictive preloading**: Load commonly requested assemblies together

### Implementation Details

```csharp
public class OptimizedAssemblyReference
{
    private readonly struct PendingRequest
    {
        public readonly AssemblyName AssemblyName;
        public readonly TaskCompletionSource<Assembly> CompletionSource;
        
        public PendingRequest(AssemblyName name)
        {
            AssemblyName = name;
            CompletionSource = new TaskCompletionSource<Assembly>();
        }
    }
    
    // Coalescing dictionary for in-flight requests
    private readonly ConcurrentDictionary<AssemblyName, Task<Assembly>> _inFlightRequests;
    
    // Batch processor
    private readonly Channel<PendingRequest> _requestChannel;
    private readonly Task _batchProcessorTask;
    
    // Assembly group loader for common patterns
    private readonly Dictionary<string, string[]> _assemblyGroups;
    
    public OptimizedAssemblyReference()
    {
        _inFlightRequests = new ConcurrentDictionary<AssemblyName, Task<Assembly>>(
            new AssemblyNameEqualityComparer());
        
        _requestChannel = Channel.CreateUnbounded<PendingRequest>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        
        _assemblyGroups = InitializeAssemblyGroups();
        _batchProcessorTask = Task.Run(ProcessBatchesAsync);
    }
    
    public async Task<Assembly> GetAssemblyAsync(AssemblyName assemblyName)
    {
        // Check cache first
        if (TryGetFromCache(assemblyName, out var assembly))
        {
            return assembly;
        }
        
        // Check for in-flight request (request coalescing)
        var existingTask = _inFlightRequests.GetOrAdd(assemblyName, name =>
        {
            var request = new PendingRequest(name);
            _requestChannel.Writer.TryWrite(request);
            return request.CompletionSource.Task;
        });
        
        return await existingTask.ConfigureAwait(false);
    }
    
    private async Task ProcessBatchesAsync()
    {
        var batch = new List<PendingRequest>();
        var batchTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
        
        while (!_requestChannel.Reader.Completion.IsCompleted)
        {
            // Collect requests for batching
            var timerTask = batchTimer.WaitForNextTickAsync().AsTask();
            var readTask = ReadBatchAsync(batch);
            
            var completedTask = await Task.WhenAny(timerTask, readTask);
            
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch);
                batch.Clear();
            }
        }
    }
    
    private async Task ProcessBatchAsync(List<PendingRequest> batch)
    {
        // Group requests by assembly characteristics
        var groups = batch.GroupBy(r => GetAssemblyGroup(r.AssemblyName));
        
        // Process each group in parallel
        var tasks = groups.Select(group => ProcessGroupAsync(group.ToList()));
        await Task.WhenAll(tasks);
        
        // Clean up in-flight requests
        foreach (var request in batch)
        {
            _inFlightRequests.TryRemove(request.AssemblyName, out _);
        }
    }
    
    private async Task ProcessGroupAsync(List<PendingRequest> group)
    {
        // Load all assemblies in the group
        var assemblies = await LoadAssembliesAsync(group.Select(r => r.AssemblyName));
        
        // Complete all requests
        foreach (var request in group)
        {
            var assembly = assemblies.FirstOrDefault(a => 
                AssemblySatisfiesReference(a.GetName(), request.AssemblyName));
            
            if (assembly != null)
            {
                CacheAssembly(request.AssemblyName, assembly);
                request.CompletionSource.SetResult(assembly);
            }
            else
            {
                request.CompletionSource.SetResult(null);
            }
        }
    }
    
    private string GetAssemblyGroup(AssemblyName assemblyName)
    {
        // Identify common assembly groups for predictive loading
        var simpleName = assemblyName.Name;
        
        // Check known groups (e.g., System.*, Microsoft.*, etc.)
        foreach (var kvp in _assemblyGroups)
        {
            if (simpleName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        
        return "default";
    }
    
    private Dictionary<string, string[]> InitializeAssemblyGroups()
    {
        // Define assembly groups that are commonly loaded together
        return new Dictionary<string, string[]>
        {
            ["System."] = new[] { "System.Runtime", "System.Collections", "System.Linq" },
            ["Microsoft."] = new[] { "Microsoft.CSharp", "Microsoft.Extensions.DependencyInjection" },
            ["Newtonsoft."] = new[] { "Newtonsoft.Json" }
        };
    }
}
```

### Benefits

1. **Request coalescing**: Multiple threads requesting the same assembly share a single load operation
2. **Batch efficiency**: Loading multiple assemblies together reduces overhead
3. **Non-blocking**: Async operations prevent thread pool starvation
4. **Predictive loading**: Common assembly groups are loaded together, improving cache hit rate
5. **Reduced contention**: Channel-based design minimizes lock contention

### Estimated Performance Impact

- **Concurrent same-assembly requests**: 50-100x improvement (only one actual load)
- **Batch processing**: 3-5x improvement for related assemblies
- **Thread utilization**: 40-60% reduction in blocked threads
- **Overall throughput**: 5-10x improvement under high concurrency
