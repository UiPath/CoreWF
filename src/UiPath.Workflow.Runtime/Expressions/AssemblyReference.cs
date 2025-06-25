// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace System.Activities.Expressions;

[TypeConverter(TypeConverters.AssemblyReferenceConverter)]
public class AssemblyReference
{
    private const int AssemblyToAssemblyNameCacheInitSize = 128;
    private const int AssemblyCacheInitialSize = 128;

    private static readonly ConcurrentDictionary<Assembly, AssemblyName> assemblyToAssemblyNameCache = new(Environment.ProcessorCount, AssemblyToAssemblyNameCacheInitSize);
    private static readonly ConcurrentDictionary<AssemblyName, Assembly> assemblyCache = new(Environment.ProcessorCount, AssemblyCacheInitialSize, new AssemblyNameEqualityComparer());
    private static readonly Lazy<ConcurrentDictionary<AssemblyName, bool>> notFoundAssemblyCache = new(() => new(Environment.ProcessorCount, AssemblyCacheInitialSize, new AssemblyNameEqualityComparer()));

    private Assembly _assembly;
    private AssemblyName _assemblyName;
    private readonly bool _isImmutable;

    static AssemblyReference()
    {
        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
    }

    public AssemblyReference() { }

    public AssemblyReference(string assemblyName)
    {
        _assemblyName = new AssemblyName(assemblyName);
    }

    // This immutable ctor is for the default references, so they can be shared freely
    internal AssemblyReference(Assembly assembly, AssemblyName assemblyName)
    {
        _assembly = assembly;
        _assemblyName = assemblyName;
        _isImmutable = true;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Assembly Assembly
    {
        get => _assembly;
        set
        {
            ThrowIfImmutable();
            _assembly = value;
        }
    }

    public AssemblyName AssemblyName
    {
        get => _assemblyName;
        set
        {
            ThrowIfImmutable();
            _assemblyName = value;
        }
    }

    public static implicit operator AssemblyReference(Assembly assembly) => new() { Assembly = assembly };

    public static implicit operator AssemblyReference(AssemblyName assemblyName) => new() { AssemblyName = assemblyName };

    public void LoadAssembly()
    {
        if (AssemblyName != null && (_assembly == null || !_isImmutable))
        {
            _assembly = GetAssembly(AssemblyName);
        }
    }

    internal static bool AssemblySatisfiesReference(AssemblyName assemblyName, AssemblyName reference)
    {
        if (reference.Name != assemblyName.Name)
        {
            return false;
        }

        if (reference.Version != null && !reference.Version.Equals(assemblyName.Version))
        {
            return false;
        }

        if (reference.CultureInfo != null && !reference.CultureInfo.Equals(assemblyName.CultureInfo))
        {
            return false;
        }

        byte[] requiredToken = reference.GetPublicKeyToken();
        if (requiredToken != null)
        {
            byte[] actualToken = assemblyName.GetPublicKeyToken();
            if (!AssemblyNameEqualityComparer.IsSameKeyToken(requiredToken, actualToken))
            {
                return false;
            }
        }

        return true;
    }

    internal static Assembly GetAssembly(AssemblyName assemblyName)
    {
        // The following assembly resolution logic emulates the Xaml's assembly resolution logic
        // as closely as possible. Should Xaml's assembly resolution logic ever change, this code
        // needs update as well please see XamlSchemaContext.ResolveAssembly() 

        if (assemblyCache.TryGetValue(assemblyName, out Assembly assembly))
        {
            return assembly;
        }

        if (notFoundAssemblyCache.IsValueCreated && notFoundAssemblyCache.Value.TryGetValue(assemblyName, out var _))
        {
            return null;
        }

        // search current AppDomain first
        // this for-loop part is to ensure that 
        // loose AssemblyNames get resolved in the same way 
        // as Xaml would do.  that is to find the first match
        // found starting from the end of the array of Assemblies
        // returned by AppDomain.GetAssemblies()
        Assembly[] currentAssemblies = AssemblyLoadContext.All.SelectMany(c => c.Assemblies).ToArray();

        // For collectible assemblies, we need to ensure that they
        // are not cached, but are usable in expressions.
        var collectibleAssemblies = new Dictionary<string, Assembly>();
        Version reqVersion = assemblyName.Version;
        CultureInfo reqCulture = assemblyName.CultureInfo;
        byte[] reqKeyToken = assemblyName.GetPublicKeyToken();

        for (int i = currentAssemblies.Length - 1; i >= 0; i--)
        {
            Assembly curAsm = currentAssemblies[i];
            if (curAsm.IsCollectible)
            {
                // ignore collectible assemblies in the caching process
                collectibleAssemblies.TryAdd(curAsm.FullName, curAsm);
                collectibleAssemblies.TryAdd(curAsm.GetName().Name, curAsm);
                continue;
            }

            if (curAsm.IsDynamic)
            {
                // ignore dynamic assemblies
                continue;
            }

            AssemblyName curAsmName = GetFastAssemblyName(curAsm);
            Version curVersion = curAsmName.Version;
            CultureInfo curCulture = curAsmName.CultureInfo;
            byte[] curKeyToken = curAsmName.GetPublicKeyToken();

            if ((string.Compare(curAsmName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase) == 0) &&
                        (reqVersion == null || reqVersion.Equals(curVersion)) &&
                        (reqCulture == null || reqCulture.Equals(curCulture)) &&
                        (reqKeyToken == null || AssemblyNameEqualityComparer.IsSameKeyToken(reqKeyToken, curKeyToken)))
            {
                assemblyCache.TryAdd(assemblyName, curAsm);
                return curAsm;
            }
        }

        // Collectible assemblies should never be cached.
        // Load attempts should also be avoided for them.
        if (collectibleAssemblies.TryGetValue(assemblyName.FullName, out var collectibleAsembly))
        {
            return collectibleAsembly;
        }

        assembly = LoadAssembly(assemblyName);
        if (assembly != null)
        {
            assemblyCache.TryAdd(assemblyName, assembly);
        }
        else
        {
            notFoundAssemblyCache.Value.TryAdd(assemblyName, false);
        }

        return assembly;
    }

    // this gets the cached AssemblyName
    // if not found, it caches the Assembly and creates its AssemblyName set it as the value
    // we don't cache DynamicAssemblies because they may be collectible and we don't want to root them
    internal static AssemblyName GetFastAssemblyName(Assembly assembly)
    {
        if (assembly.IsDynamic || assembly.IsCollectible)
        {
            return new AssemblyName(assembly.FullName);
        }

        return assemblyToAssemblyNameCache.GetOrAdd(assembly, asm => new AssemblyName(asm.FullName));
    }

#pragma warning disable 618
    private static Assembly LoadAssembly(AssemblyName assemblyName)
    {
        Fx.Assert(assemblyName.Name != null, "AssemblyName.Name cannot be null");
        byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
        Assembly loaded;
        if (assemblyName.Version != null || assemblyName.CultureInfo != null || publicKeyToken != null)
        {
            try
            {
                loaded = Assembly.Load(assemblyName.FullName);
            }
            catch (Exception ex) 
                when (ex is FileNotFoundException or FileLoadException
                || ex is TargetInvocationException exception && exception.InnerException is FileNotFoundException or FileLoadException)
            {
                loaded = null;
                ExceptionTrace.AsWarning(ex);
            }
        }
        else
        {
            // partial assembly name
            loaded = Assembly.LoadWithPartialName(assemblyName.FullName);
        }

        return loaded;
    }
#pragma warning restore 618

    private void ThrowIfImmutable()
    {
        if (_isImmutable)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.AssemblyReferenceIsImmutable));
        }
    }

    private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        if (!notFoundAssemblyCache.IsValueCreated)
        {
            return;
        }

        notFoundAssemblyCache.Value.Clear();
    }
}
