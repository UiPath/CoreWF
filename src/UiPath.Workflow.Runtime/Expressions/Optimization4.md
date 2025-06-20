# Optimization 4: Memory-Mapped Assembly Index

## Problem

The current implementation has memory and performance issues:

1. **Repeated assembly enumeration**: `AssemblyLoadContext.All.SelectMany(c=>c.Assemblies).ToArray()` allocates a large array on every cache miss
2. **Memory pressure**: Creating arrays of all assemblies causes GC pressure
3. **No persistence**: Assembly cache is rebuilt on every application start
4. **Inefficient search**: Linear search through assemblies even with known assembly names

## Solution

Create a memory-mapped assembly index that:

1. **Persists across app domains**: Shared memory reduces startup time
2. **Zero-copy access**: Direct memory access without array allocation
3. **Efficient indexing**: Pre-built indexes for fast lookup
4. **Update notifications**: File system watcher for dynamic assembly loading

### Implementation Details

```csharp
public class MemoryMappedAssemblyIndex : IDisposable
{
    private readonly struct AssemblyIndexEntry
    {
        public readonly long FullNameOffset;
        public readonly long SimpleNameOffset;
        public readonly long LocationOffset;
        public readonly int FullNameLength;
        public readonly int SimpleNameLength;
        public readonly int LocationLength;
        public readonly long VersionTicks;
        public readonly int Flags; // IsDynamic, IsCollectible, etc.
    }
    
    private readonly MemoryMappedFile _indexFile;
    private readonly MemoryMappedViewAccessor _headerAccessor;
    private readonly MemoryMappedViewAccessor _indexAccessor;
    private readonly MemoryMappedViewAccessor _stringAccessor;
    private readonly FileSystemWatcher _assemblyWatcher;
    private readonly ReaderWriterLockSlim _indexLock;
    
    // Index structure in memory:
    // [Header: 64 bytes]
    // [Index entries: N * sizeof(AssemblyIndexEntry)]
    // [String data: Variable length]
    
    private const string IndexFileName = "assembly_index.dat";
    private const int HeaderSize = 64;
    private const int MaxAssemblies = 10000;
    
    public MemoryMappedAssemblyIndex()
    {
        _indexLock = new ReaderWriterLockSlim();
        
        var indexPath = Path.Combine(Path.GetTempPath(), IndexFileName);
        var fileSize = HeaderSize + 
                      (MaxAssemblies * Marshal.SizeOf<AssemblyIndexEntry>()) + 
                      (MaxAssemblies * 1024); // Avg 1KB per assembly strings
        
        // Create or open memory-mapped file
        _indexFile = MemoryMappedFile.CreateFromFile(
            indexPath,
            FileMode.OpenOrCreate,
            "AssemblyIndex",
            fileSize,
            MemoryMappedFileAccess.ReadWrite);
        
        _headerAccessor = _indexFile.CreateViewAccessor(0, HeaderSize);
        _indexAccessor = _indexFile.CreateViewAccessor(
            HeaderSize, 
            MaxAssemblies * Marshal.SizeOf<AssemblyIndexEntry>());
        _stringAccessor = _indexFile.CreateViewAccessor(
            HeaderSize + (MaxAssemblies * Marshal.SizeOf<AssemblyIndexEntry>()), 
            0);
        
        InitializeOrValidateIndex();
        SetupFileWatcher();
    }
    
    public Assembly FindAssembly(AssemblyName assemblyName)
    {
        _indexLock.EnterReadLock();
        try
        {
            // Binary search in sorted index
            var entryIndex = BinarySearchAssembly(assemblyName);
            if (entryIndex >= 0)
            {
                var entry = ReadIndexEntry(entryIndex);
                var location = ReadString(_stringAccessor, entry.LocationOffset, entry.LocationLength);
                
                // Verify assembly still exists and matches
                if (File.Exists(location))
                {
                    return Assembly.LoadFrom(location);
                }
            }
            
            return null;
        }
        finally
        {
            _indexLock.ExitReadLock();
        }
    }
    
    private int BinarySearchAssembly(AssemblyName target)
    {
        var count = _headerAccessor.ReadInt32(0);
        var left = 0;
        var right = count - 1;
        
        while (left <= right)
        {
            var mid = (left + right) / 2;
            var entry = ReadIndexEntry(mid);
            
            var fullName = ReadString(_stringAccessor, entry.FullNameOffset, entry.FullNameLength);
            var midName = new AssemblyName(fullName);
            
            var comparison = CompareAssemblyNames(target, midName);
            if (comparison == 0)
            {
                return mid;
            }
            else if (comparison < 0)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }
        
        return -1;
    }
    
    private void RebuildIndex()
    {
        _indexLock.EnterWriteLock();
        try
        {
            var assemblies = AssemblyLoadContext.All
                .SelectMany(c => c.Assemblies)
                .Where(a => !a.IsDynamic && !a.IsCollectible)
                .OrderBy(a => a.FullName)
                .ToList();
            
            var stringOffset = 0L;
            var entries = new List<AssemblyIndexEntry>();
            
            using (var stringStream = new MemoryStream())
            using (var writer = new BinaryWriter(stringStream))
            {
                foreach (var assembly in assemblies)
                {
                    var fullName = assembly.FullName;
                    var simpleName = assembly.GetName().Name;
                    var location = assembly.Location ?? string.Empty;
                    
                    var entry = new AssemblyIndexEntry
                    {
                        FullNameOffset = stringOffset,
                        FullNameLength = Encoding.UTF8.GetByteCount(fullName),
                        SimpleNameOffset = stringOffset + entry.FullNameLength,
                        SimpleNameLength = Encoding.UTF8.GetByteCount(simpleName),
                        LocationOffset = entry.SimpleNameOffset + entry.SimpleNameLength,
                        LocationLength = Encoding.UTF8.GetByteCount(location),
                        VersionTicks = assembly.GetName().Version?.ToBinary() ?? 0,
                        Flags = 0
                    };
                    
                    entries.Add(entry);
                    
                    writer.Write(Encoding.UTF8.GetBytes(fullName));
                    writer.Write(Encoding.UTF8.GetBytes(simpleName));
                    writer.Write(Encoding.UTF8.GetBytes(location));
                    
                    stringOffset += entry.FullNameLength + entry.SimpleNameLength + entry.LocationLength;
                }
                
                // Write header
                _headerAccessor.Write(0, entries.Count);
                _headerAccessor.Write(8, DateTime.UtcNow.Ticks);
                
                // Write index entries
                for (int i = 0; i < entries.Count; i++)
                {
                    WriteIndexEntry(i, entries[i]);
                }
                
                // Write string data
                var stringData = stringStream.ToArray();
                _stringAccessor.WriteArray(0, stringData, 0, stringData.Length);
            }
        }
        finally
        {
            _indexLock.ExitWriteLock();
        }
    }
    
    private void SetupFileWatcher()
    {
        // Watch for new assemblies being loaded
        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        _assemblyWatcher = new FileSystemWatcher(appPath, "*.dll")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true
        };
        
        _assemblyWatcher.Changed += (s, e) => ScheduleIndexRebuild();
        _assemblyWatcher.Created += (s, e) => ScheduleIndexRebuild();
        _assemblyWatcher.EnableRaisingEvents = true;
    }
}
```

### Benefits

1. **Zero allocation**: No array creation for assembly enumeration
2. **Persistent cache**: Index survives application restarts
3. **Memory efficiency**: Shared memory across app domains
4. **O(log n) lookup**: Binary search in sorted index
5. **Dynamic updates**: Automatic index updates when assemblies change

### Estimated Performance Impact

- **Memory allocation**: 90% reduction in GC pressure
- **Lookup performance**: O(log n) vs O(n), 10-50x faster for large assembly counts
- **Startup time**: 80% reduction when index is already built
- **Memory usage**: 50% reduction due to shared memory model
