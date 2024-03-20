using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkflowApplicationTestExtensions
{
    /// <summary>
    /// Activity that induces Idle for a few milliseconds but not PersistableIdle.
    /// This is similar to UiPath asynchronous in-process activities.
    /// </summary>
    public class NoPersistAsyncActivity : NativeActivity
    {
        private readonly Variable<NoPersistHandle> _noPersist = new();

        protected override bool CanInduceIdle => true;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddImplementationVariable(_noPersist);
            metadata.AddDefaultExtensionProvider(() => new BookmarkResumer());
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            _noPersist.Get(context).Enter(context);
            context.GetExtension<BookmarkResumer>().ResumeSoon(context.CreateBookmark());
        }
    }

    public class BookmarkResumer : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy _instance;
        public IEnumerable<object> GetAdditionalExtensions() => [];
        public void SetInstance(WorkflowInstanceProxy instance) => _instance = instance;
        public void ResumeSoon(Bookmark bookmark) => Task.Delay(10).ContinueWith(_ =>
        {
            _instance.BeginResumeBookmark(bookmark, null, null, null);
        });
    }
}
