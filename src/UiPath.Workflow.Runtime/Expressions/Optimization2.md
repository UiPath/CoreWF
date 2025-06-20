# Optimization 2: Implement Two-Level Caching with Read-Through Pattern

## Problem

The current `GetAssembly` method has several performance bottlenecks:

1. **Expensive array creation**: `AssemblyLoadContext.All.SelectMany(c=>c.Assemblies).ToArray()` creates a new array on every cache miss.
2. **Linear search**: Searching through all assemblies from end to start is O(n) operation.
3. **Repeated lookups**: When multiple threads look for the same assembly simultaneously, they all perform the expensive search operation.
4. **No negative caching**: Failed lookups are not cached, causing repeated expensive searches for non-existent assemblies.

## Solution

Implement a two-level caching strategy with a read-through pattern:

1. **Level 1 (L1) Cache**: Fast, lock-free cache for frequently accessed assemblies
2. **Level 2 (L2) Cache**: Comprehensive cache with negative caching support
3. **Read-through loader**: Single-execution guarantee for assembly loading

### Implementation Details

```csharp
public class AssemblyCache
{
    private readonly struct CacheEntry
    {
        public readonly Assembly Assembly;
        public readonly DateTime Timestamp;
        public readonly bool IsNegative; // true if assembly was not found
        
        public CacheEntry(Assembly assembly, bool isNegative = false)
        {
            Assembly = assembly;
            Timestamp = DateTime.UtcNow;
            IsNegative = isNegative;
        }
    }
    
    // L1: Most recently used assemblies (lock-free read)
    private readonly ConcurrentDictionary<AssemblyName, CacheEntry> _l1Cache;
    
    // L2: All assemblies with negative caching
    private readonly ConcurrentDictionary<AssemblyName, Lazy<CacheEntry>> _l2Cache;
    
    // Cached assembly list snapshot
    private volatile AssemblySnapshot _assemblySnapshot;
    private readonly Timer _snapshotRefreshTimer;
    
    private class AssemblySnapshot
    {
        public readonly Dictionary<string, Assembly> ByFullName;
        public readonly Dictionary<string, List<Assembly>> BySimpleName;
        public readonly DateTime Timestamp;
        
        public AssemblySnapshot(Assembly[] assemblies)
        {
            ByFullName = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            BySimpleName = new Dictionary<string, List<Assembly>>(StringComparer.OrdinalIgnoreCase);
            Timestamp = DateTime.UtcNow;
            
            // Build indexes for fast lookup
            foreach (var asm in assemblies.Where(a => !a.IsDynamic && !a.IsCollectible))
            {
                ByFullName[asm.FullName] = asm;
                
                var simpleName = asm.GetName().Name;
                if (!BySimpleName.TryGetValue(simpleName, out var list))
                {
                    list = new List<Assembly>();
                    BySimpleName[simpleName] = list;
                }
                list.Add(asm);
            }
        }
    }
    
    public Assembly GetAssembly(AssemblyName assemblyName)
    {
        // L1 cache check (fastest path)
        if (_l1Cache.TryGetValue(assemblyName, out var l1Entry) && !l1Entry.IsNegative)
        {
            return l1Entry.Assembly;
        }
        
        // L2 cache with single-execution guarantee
        var lazyEntry = _l2Cache.GetOrAdd(assemblyName, 
            key => new Lazy<CacheEntry>(() => LoadAssembly(key), 
                LazyThreadSafetyMode.ExecutionAndPublication));
        
        var entry = lazyEntry.Value;
        
        // Promote to L1 if successful
        if (!entry.IsNegative)
        {
            _l1Cache.TryAdd(assemblyName, entry);
            MaintainL1CacheSize();
        }
        
        return entry.Assembly;
    }
    
    private CacheEntry LoadAssembly(AssemblyName assemblyName)
    {
        // First check the snapshot
        var snapshot = _assemblySnapshot;
        var assembly = FindInSnapshot(snapshot, assemblyName);
        
        if (assembly != null)
        {
            return new CacheEntry(assembly);
        }
        
        // Try loading from disk
        assembly = LoadAssemblyFromDisk(assemblyName);
        
        if (assembly != null)
        {
            return new CacheEntry(assembly);
        }
        
        // Negative cache entry
        return new CacheEntry(null, isNegative: true);
    }
}
```

### Benefits

1. **Reduced contention**: L1 cache serves most requests without any locking
2. **Single execution**: `Lazy<T>` ensures assembly loading happens only once per assembly
3. **Faster lookups**: Indexed snapshot provides O(1) lookup instead of O(n)
4. **Negative caching**: Prevents repeated searches for non-existent assemblies
5. **Automatic refresh**: Snapshot updates periodically to catch newly loaded assemblies

### Estimated Performance Impact

- **Cache hit (L1)**: 10-20x faster than current implementation
- **Cache hit (L2)**: 5-10x faster than current implementation
- **Cache miss**: 3-5x faster due to indexed search and negative caching
- **Memory usage**: 20-30% increase, but with better cache utilization
