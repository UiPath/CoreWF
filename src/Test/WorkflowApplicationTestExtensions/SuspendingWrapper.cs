using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace WorkflowApplicationTestExtensions
{
    /// <summary>
    /// Wrapper over one/multiple sequential activities.
    /// Between scheduling the activity/activities, it induces PersistableIdle
    /// by creating bookmarks.
    /// The idea is to induce unload/load as much as possible to test persistence
    /// serialization/deserialization.
    /// When using <see cref="WorkflowApplicationTestExtensions"/>, the bookmarks
    /// can be automatically resumed and workflow continued transparently until
    /// completion.
    /// </summary>
    public class SuspendingWrapper : NativeActivity
    {
        private readonly Variable<int> _nextIndexToExecute = new();
        public List<Activity> Activities { get; }
        protected override bool CanInduceIdle => true;

        public SuspendingWrapper(IEnumerable<Activity> activities)
        {
            Activities = activities.ToList();
        }

        public SuspendingWrapper(Activity activity) : this([activity])
        {
        }

        public SuspendingWrapper() : this([])
        {
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddImplementationVariable(_nextIndexToExecute);
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context) => ExecuteNext(context);

        private void OnChildCompleted(NativeActivityContext context, ActivityInstance completedInstance) =>
            ExecuteNext(context);

        private void OnChildFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom) =>
            ExceptionDispatchInfo.Capture(propagatedException).Throw();

        private void ExecuteNext(NativeActivityContext context) =>
            context.CreateBookmark(
                $"{WorkflowApplicationTestExtensions.AutoResumedBookmarkNamePrefix}{Guid.NewGuid()}",
                AfterResume);

        private void AfterResume(NativeActivityContext context, Bookmark bookmark, object value)
        {
            var nextIndex = _nextIndexToExecute.Get(context);
            if (nextIndex == Activities.Count)
            {
                return;
            }
            _nextIndexToExecute.Set(context, nextIndex + 1);
            context.ScheduleActivity(Activities[nextIndex], OnChildCompleted, OnChildFaulted);
        }
    }
}
