using System.Activities.Hosting;
using System.Activities.Runtime;
namespace System.Activities.Statements;
public class ResumeBookmarkExtension : IWorkflowInstanceExtension
{
    private WorkflowInstanceProxy _instance;
    public static void Install(NativeActivityMetadata metadata) => metadata.AddDefaultExtensionProvider(static()=>new ResumeBookmarkExtension());
    public static void Resume(ActivityContext context, Bookmark bookmark)
    {
        var extension = context.GetExtension<ResumeBookmarkExtension>();
        Fx.Assert(extension != null, "Failed to obtain a ResumeBookmarkExtension.");
        extension.ResumeBookmark(bookmark);
    }
    public IEnumerable<object> GetAdditionalExtensions() => null;
    public void SetInstance(WorkflowInstanceProxy instance) => _instance = instance;
    void ResumeBookmark(Bookmark bookmark)
    {
        var asyncResult = _instance.BeginResumeBookmark(bookmark, null, Fx.ThunkCallback(OnResumeBookmarkCompleted), _instance);
        if (asyncResult.CompletedSynchronously)
        {
            _instance.EndResumeBookmark(asyncResult);
        }
    }
    static void OnResumeBookmarkCompleted(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }
        var instance = result.AsyncState as WorkflowInstanceProxy;
        Fx.Assert(instance != null, "BeginResumeBookmark should pass a WorkflowInstanceProxy object as the async state object.");
        instance.EndResumeBookmark(result);
    }
}