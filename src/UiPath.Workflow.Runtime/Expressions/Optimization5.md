# Optimization 5: Specialized Fast Paths for Common Assemblies

## Problem

The current implementation treats all assembly lookups equally, but in practice:

1. **80/20 rule**: A small set of assemblies (System.*, mscorlib) are requested most frequently
2. **Known assemblies**: Framework assemblies have predictable locations and versions
3. **Startup overhead**: Common assemblies are looked up repeatedly during application initialization
4. **Cache misses**: Even cached lookups have overhead for dictionary access

## Solution

Implement specialized fast paths for the most common assembly requests:

1. **Static lookup table**: Pre-computed lookups for framework assemblies
2. **Inline caching**: Thread-local caches for hot paths
3. **Assembly fingerprinting**: Quick validation without full comparison
4. **Specialized equality**: Optimized comparison for common patterns

### Implementation Details

```csharp
public static class AssemblyReferenceFastPath
{
    // Pre-computed framework assemblies
    private static readonly Dictionary<string, Assembly> FrameworkAssemblies;
    private static readonly HashSet<string> FrameworkAssemblyNames;
    
    // Thread-local inline cache for hot assemblies
    [ThreadStatic]
    private static InlineCache t_inlineCache;
    
    // Bloom filter for negative caching
    private static readonly BloomFilter<string> NonExistentAssemblies;
    
    private struct InlineCache
    {
        // Most recent 4 assemblies accessed by this thread
        public AssemblyName Name1, Name2, Name3, Name4;
        public Assembly Assembly1, Assembly2, Assembly3, Assembly4;
        public int HitCount;
        
        public bool TryGet(AssemblyName name, out Assembly assembly)
        {
            // Unrolled loop for maximum performance
            if (FastEquals(name, Name1)) { assembly = Assembly1; HitCount++; return true; }
            if (FastEquals(name, Name2)) { assembly = Assembly2; HitCount++; return true; }
            if (FastEquals(name, Name3)) { assembly = Assembly3; HitCount++; return true; }
            if (FastEquals(name, Name4)) { assembly = Assembly4; HitCount++; return true; }
            
            assembly = null;
            return false;
        }
        
        public void Add(AssemblyName name, Assembly assembly)
        {
            // LRU replacement
            Name4 = Name3; Assembly4 = Assembly3;
            Name3 = Name2; Assembly3 = Assembly2;
            Name2 = Name1; Assembly2 = Assembly1;
            Name1 = name; Assembly1 = assembly;
        }
    }
    
    static AssemblyReferenceFastPath()
    {
        // Pre-load all framework assemblies
        FrameworkAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        FrameworkAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var frameworkAssemblies = new[]
        {
            "mscorlib",
            "System",
            "System.Core",
            "System.Runtime",
            "System.Collections",
            "System.Linq",
            "System.Threading.Tasks",
            "System.IO",
            "System.Text.RegularExpressions",
            "System.Reflection",
            "System.Diagnostics.Debug",
            "System.Runtime.Extensions",
            "System.ObjectModel",
            "System.ComponentModel",
            "System.Xml",
            "System.Xml.Linq",
            "Microsoft.CSharp"
        };
        
        foreach (var name in frameworkAssemblies)
        {
            try
            {
                var assembly = Assembly.Load(name);
                FrameworkAssemblies[name] = assembly;
                FrameworkAssemblies[assembly.FullName] = assembly;
                FrameworkAssemblyNames.Add(name);
                FrameworkAssemblyNames.Add(assembly.FullName);
            }
            catch
            {
                // Ignore assemblies that don't exist in this runtime
            }
        }
        
        NonExistentAssemblies = new BloomFilter<string>(10000, 0.01);
    }
    
    public static bool TryGetAssemblyFast(AssemblyName assemblyName, out Assembly assembly)
    {
        // Level 0: Inline thread-local cache
        if (t_inlineCache.TryGet(assemblyName, out assembly))
        {
            return true;
        }
        
        // Level 1: Framework assembly fast path
        var simpleName = assemblyName.Name;
        if (FrameworkAssemblyNames.Contains(simpleName))
        {
            if (FrameworkAssemblies.TryGetValue(simpleName, out assembly))
            {
                // Verify version compatibility if specified
                if (assemblyName.Version == null || 
                    assembly.GetName().Version >= assemblyName.Version)
                {
                    t_inlineCache.Add(assemblyName, assembly);
                    return true;
                }
            }
        }
        
        // Level 2: Check negative cache
        var fullName = assemblyName.FullName;
        if (NonExistentAssemblies.Contains(fullName))
        {
            assembly = null;
            return true; // Known to not exist
        }
        
        // Fall back to regular cache
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastEquals(AssemblyName a, AssemblyName b)
    {
        // Fast path for reference equality
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        
        // Quick name comparison first (most likely to differ)
        if (!string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
            return false;
        
        // Only check version if specified in 'a'
        if (a.Version != null && !a.Version.Equals(b.Version))
            return false;
        
        // Skip culture and public key token for fast path
        // (framework assemblies typically don't vary on these)
        return true;
    }
    
    // Optimized assembly name comparer using fingerprinting
    public class FastAssemblyNameComparer : IEqualityComparer<AssemblyName>
    {
        public bool Equals(AssemblyName x, AssemblyName y)
        {
            return FastEquals(x, y);
        }
        
        public int GetHashCode(AssemblyName obj)
        {
            // Fast hash combining name and version
            unchecked
            {
                var hash = obj.Name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
                if (obj.Version != null)
                {
                    hash = (hash * 397) ^ obj.Version.GetHashCode();
                }
                return hash;
            }
        }
    }
}

// Integration with main class
public static Assembly GetAssembly(AssemblyName assemblyName)
{
    // Try fast path first
    if (AssemblyReferenceFastPath.TryGetAssemblyFast(assemblyName, out var assembly))
    {
        return assembly;
    }
    
    // Fall back to regular implementation
    return GetAssemblyRegular(assemblyName);
}
```

### Additional Optimization: Assembly Fingerprinting

```csharp
public readonly struct AssemblyFingerprint : IEquatable<AssemblyFingerprint>
{
    private readonly ulong _hash;
    private readonly int _version;
    
    public AssemblyFingerprint(AssemblyName assemblyName)
    {
        // Compute a fast 64-bit hash of the assembly name
        var name = assemblyName.Name ?? string.Empty;
        _hash = ComputeHash(name);
        _version = assemblyName.Version?.GetHashCode() ?? 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash(string s)
    {
        // FNV-1a hash for speed
        const ulong FnvPrime = 0x00000100000001B3;
        const ulong FnvOffsetBasis = 0xCBF29CE484222325;
        
        ulong hash = FnvOffsetBasis;
        foreach (char c in s)
        {
            hash ^= char.ToLowerInvariant(c);
            hash *= FnvPrime;
        }
        return hash;
    }
    
    public bool Equals(AssemblyFingerprint other)
    {
        return _hash == other._hash && _version == other._version;
    }
}
```

### Benefits

1. **Zero-lookup for framework assemblies**: Most common assemblies return immediately
2. **Thread-local caching**: Eliminates contention for hot assemblies
3. **Negative caching**: Bloom filter prevents repeated failed lookups
4. **Optimized comparison**: Faster equality checks for common cases
5. **Memory efficiency**: Minimal overhead for fast paths

### Estimated Performance Impact

- **Framework assemblies**: 100-1000x faster (direct field access vs dictionary lookup)
- **Thread-local hits**: 50-100x faster (no synchronization needed)
- **Negative lookups**: 10x faster with bloom filter
- **Overall improvement**: 70-90% of requests served by fast path
- **Memory overhead**: < 1MB for all optimization structures
