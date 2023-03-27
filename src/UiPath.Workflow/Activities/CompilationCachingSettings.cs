namespace System.Activities;

public class CompilationCachingSettings
{
    private CompilationCachingSettings()
    {
    }

    public static CompilationCachingSettings Default { get; } = new();

    internal event EventHandler OnCacheClearRequest;

    public void ClearCache()
    {
        OnCacheClearRequest?.Invoke(this, EventArgs.Empty);
    }
}
