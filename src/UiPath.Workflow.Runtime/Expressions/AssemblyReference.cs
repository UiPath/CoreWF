// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Activities.Expressions;

[TypeConverter(TypeConverters.AssemblyReferenceConverter)]
public class AssemblyReference
{
    private const int AssemblyToAssemblyNameCacheInitSize = 100;
    private const int AssemblyCacheInitialSize = 100;

    // cache for Assembly ==> AssemblyName
    // Assembly.GetName() is very very expensive
    private static readonly object assemblyToAssemblyNameCacheLock = new();

    // Double-checked locking pattern requires volatile for read/write synchronization
    private static volatile Hashtable assemblyToAssemblyNameCache;

    // cache for AssemblyName ==> Assembly
    // For back-compat with VB (which in turn was roughly emulating XamlSchemaContext)
    // we want to cache a given AssemblyName once it's resolved, so it doesn't get re-resolved
    // even if a new matching assembly is loaded later.

    // Double-checked locking pattern requires volatile for read/write synchronization
    private static volatile Hashtable assemblyCache;
    private static readonly object assemblyCacheLock = new();

    private Assembly _assembly;
    private AssemblyName _assemblyName;
    private readonly bool _isImmutable;

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

    //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.OperatorOverloadsHaveNamedAlternates,
    //    Justification = "A named method provides no advantage over the property setter.")]
    public static implicit operator AssemblyReference(Assembly assembly) => new() { Assembly = assembly };

    //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.OperatorOverloadsHaveNamedAlternates,
    //    Justification = "A named method provides no advantage over the property setter.")]
    public static implicit operator AssemblyReference(AssemblyName assemblyName) => new() { AssemblyName = assemblyName };

    public void LoadAssembly()
    {
        if (AssemblyName != null && (_assembly == null || !_isImmutable))
        {
            _assembly = GetAssembly(AssemblyName);
        }
    }

    // this code is borrowed from XamlSchemaContext
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
        // the following assembly resolution logic
        // emulates the Xaml's assembly resolution logic as closely as possible.
        // Should Xaml's assembly resolution logic ever change, this code needs update as well.
        // please see XamlSchemaContext.ResolveAssembly() 
        if (assemblyCache == null)
        {
            lock (assemblyCacheLock)
            {
                if (assemblyCache == null)
                {
                    assemblyCache = new Hashtable(AssemblyCacheInitialSize, new AssemblyNameEqualityComparer());
                }
            }
        }

        Assembly assembly = assemblyCache[assemblyName] as Assembly;
        if (assembly != null)
        {
            return assembly;
        }

        // search current AppDomain first
        // this for-loop part is to ensure that 
        // loose AssemblyNames get resolved in the same way 
        // as Xaml would do.  that is to find the first match
        // found starting from the end of the array of Assemblies
        // returned by AppDomain.GetAssemblies()
        Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = currentAssemblies.Length - 1; i >= 0; i--)
        {
            Assembly curAsm = currentAssemblies[i];
            if (curAsm.IsDynamic)
            {
                // ignore dynamic assemblies
                continue;
            }

            AssemblyName curAsmName = GetFastAssemblyName(curAsm);
            Version curVersion = curAsmName.Version;
            CultureInfo curCulture = curAsmName.CultureInfo;
            byte[] curKeyToken = curAsmName.GetPublicKeyToken();

            Version reqVersion = assemblyName.Version;
            CultureInfo reqCulture = assemblyName.CultureInfo;
            byte[] reqKeyToken = assemblyName.GetPublicKeyToken();

            if ((string.Compare(curAsmName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase) == 0) &&
                        (reqVersion == null || reqVersion.Equals(curVersion)) &&
                        (reqCulture == null || reqCulture.Equals(curCulture)) &&
                        (reqKeyToken == null || AssemblyNameEqualityComparer.IsSameKeyToken(reqKeyToken, curKeyToken)))
            {
                lock (assemblyCacheLock)
                {
                    assemblyCache[assemblyName] = curAsm;
                    return curAsm;
                }
            }
        }

        assembly = LoadAssembly(assemblyName);
        if (assembly != null)
        {
            lock (assemblyCacheLock)
            {
                assemblyCache[assemblyName] = assembly;
            }
        }

        return assembly;
    }

    // this gets the cached AssemblyName
    // if not found, it caches the Assembly and creates its AssemblyName set it as the value
    // we don't cache DynamicAssemblies because they may be collectible and we don't want to root them
    internal static AssemblyName GetFastAssemblyName(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return new AssemblyName(assembly.FullName);
        }

        if (assemblyToAssemblyNameCache == null)
        {
            lock (assemblyToAssemblyNameCacheLock)
            {
                if (assemblyToAssemblyNameCache == null)
                {
                    assemblyToAssemblyNameCache = new Hashtable(AssemblyToAssemblyNameCacheInitSize);
                }
            }
        }

        if (assemblyToAssemblyNameCache[assembly] is AssemblyName assemblyName)
        {
            return assemblyName;
        }

        assemblyName = new AssemblyName(assembly.FullName);
        lock (assemblyToAssemblyNameCacheLock)
        {
            assemblyToAssemblyNameCache[assembly] = assemblyName;
        }

        return assemblyName;
    }

#pragma warning disable 618
    private static Assembly LoadAssembly(AssemblyName assemblyName)
    {
        Fx.Assert(assemblyName.Name != null, "AssemblyName.Name cannot be null");
        byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
        Assembly loaded;
        if (assemblyName.Version != null || assemblyName.CultureInfo != null || publicKeyToken != null)
        {
            // Assembly.Load(string)
            try
            {
                loaded = Assembly.Load(assemblyName.FullName);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException ||
                    ex is FileLoadException ||
                    (ex is TargetInvocationException exception &&
                    (exception.InnerException is FileNotFoundException ||
                    exception.InnerException is FileNotFoundException)))
                {
                    loaded = null;
                    ExceptionTrace.AsWarning(ex);
                }
                else
                {
                    throw;
                }
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
}
