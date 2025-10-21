using System;
using System.Activities;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace UiPath.Workflow.Runtime.Try;

internal sealed class CompiledWorkflow
{
    public Activity Activity { get; init; }
    public object? CompiledExpressionRoot { get; init; } // ICompiledExpressionRoot, keep as object to avoid ref
    public Assembly? ExpressionsAssembly { get; init; }
}

internal static class CompiledWorkflowCache
{
    private static readonly ConcurrentDictionary<string, CompiledWorkflow> s_cache = new();
    private const int MaxEntries = 256; // tune for your fleet; add a simple LRU if needed

    public static string MakeKey(Stream xaml, IEnumerable<Assembly> references)
    {
        // Hash XAML content + assembly MVIDs; callers should re-seek xaml to 0 afterwards.
        using var sha = SHA256.Create();
        var buf = new byte[8192];
        xaml.Position = 0;
        int r; while ((r = xaml.Read(buf, 0, buf.Length)) > 0) sha.TransformBlock(buf, 0, r, null, 0);
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        var mvidBytes = string.Join("|",
            references.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                      .Select(a => a.ManifestModule.ModuleVersionId.ToString("D")));

        return Convert.ToHexString(sha.Hash!) + "@" + mvidBytes;
    }

    public static bool TryGet(string key, out CompiledWorkflow compiled) => s_cache.TryGetValue(key, out compiled);

    public static void Put(string key, CompiledWorkflow compiled)
    {
        s_cache[key] = compiled;
        // if (s_cache.Count > MaxEntries) prune oldest (add timestamp & LRU queue if needed)
    }
}
