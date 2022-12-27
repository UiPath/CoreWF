using System.Activities.Hosting;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
namespace System.Activities.Statements;
public class BpmJoin : BpmNode
{
    ValidatingCollection<BpmNode> _branches;
    [DefaultValue(null)]
    public Collection<BpmNode> Branches => _branches ??= ValidatingCollection<BpmNode>.NullCheck();
    [DefaultValue(null)]
    public BpmNode Next { get; set; }
    protected override bool CanInduceIdle => true;
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.RequireExtension<BookmarkResumptionHelper>();
        metadata.AddDefaultExtensionProvider(() => new BookmarkResumptionHelper());
    }
    record JoinState
    {
        public int Count;
    }
    protected override void Execute(NativeActivityContext context)
    {
        var key = $"{nameof(BpmJoin)}_{Id}";
        Dictionary<string, object> state;
        using (context.InheritVariables())
        {
            state = context.GetValue<Dictionary<string, object>>("flowchartState");
        }
        var joinState = (JoinState)(state.GetValueOrDefault(key) ?? new JoinState());
        joinState.Count++;
        if (joinState.Count == 1)
        {
            state[key] = joinState;
            context.CreateBookmark(key, OnBookmarkResumed);
        }
        if (joinState.Count == Branches.Count)
        {
            state.Remove(key);
            var bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();
            Task.Run(()=>bookmarkHelper.ResumeBookmark(new Bookmark(key), null));
            TryExecute(Next, context, context.CurrentInstance);
        }
    }
    static void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value) { }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }
}
internal sealed class BookmarkResumptionHelper : IWorkflowInstanceExtension
{
    private WorkflowInstanceProxy _workflowInstance;
    internal BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value) =>
        _workflowInstance.EndResumeBookmark(_workflowInstance.BeginResumeBookmark(bookmark, value, null, null));
    IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions() => null;
    void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance) => _workflowInstance = instance;
}