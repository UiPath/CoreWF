using System.Activities.Hosting;
using System.Activities.Runtime;
namespace System.Activities.Statements;
internal class ResumeBookmarkExtension : IWorkflowInstanceExtension
{
    private WorkflowInstanceProxy _instance;
    public static void Install(NativeActivityMetadata metadata) => metadata.AddDefaultExtensionProvider(static()=>new ResumeBookmarkExtension());
    public IEnumerable<object> GetAdditionalExtensions() => null;
    public void SetInstance(WorkflowInstanceProxy instance) => _instance = instance;
    public void ResumeBookmark(Bookmark bookmark)
    {
        var asyncResult = _instance.BeginResumeBookmark(bookmark, null, Fx.ThunkCallback(OnResumeBookmarkCompleted), _instance);
        if (asyncResult.CompletedSynchronously)
        {
            _instance.EndResumeBookmark(asyncResult);
        }
    }
    private static void OnResumeBookmarkCompleted(IAsyncResult result)
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