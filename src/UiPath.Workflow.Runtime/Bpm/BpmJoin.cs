using System.Activities.Hosting;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
namespace System.Activities.Statements;
public class BpmJoin : BpmNode
{
    int _finishedCount;
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
    protected override void Execute(NativeActivityContext context)
    {
        _finishedCount++;
        if (_finishedCount == Branches.Count)
        {
            _finishedCount = 0;
            var bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();
            Task.Run(()=>bookmarkHelper.ResumeBookmark(new Bookmark(BookmarkName()), null));
            if (Next != null)
            {
                context.ScheduleActivity(Next);
            }
            return;
        }
        if (_finishedCount == 1)
        {
            context.CreateBookmark(BookmarkName(), delegate { GetHashCode(); });
        }
    }
    string BookmarkName() => $"{nameof(BpmJoin)}{GetHashCode()}";
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