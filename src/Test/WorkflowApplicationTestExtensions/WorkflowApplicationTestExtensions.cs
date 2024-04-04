using JsonFileInstanceStore;
using System;
using System.Activities;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using StringToObject = System.Collections.Generic.IDictionary<string, object>;

namespace WorkflowApplicationTestExtensions
{
    public static class WorkflowApplicationTestExtensions
    {
        public const string AutoResumedBookmarkNamePrefix = "AutoResumedBookmark_";

        public record WorkflowApplicationResult(StringToObject Outputs, int PersistenceCount);

        /// <summary>
        /// Simple API to wait for the workflow to complete or propagate to the caller any error.
        /// Also, when PersistableIdle, will automatically Unload, Load, resume some bookmarks
        /// (those named "AutoResumedBookmark_...") and continue execution.
        /// </summary>
        public static WorkflowApplicationResult RunUntilCompletion(this WorkflowApplication application)
        {
            var persistenceCount = 0;
            var output = new TaskCompletionSource<WorkflowApplicationResult>();
            application.Completed += (WorkflowApplicationCompletedEventArgs args) =>
            {
                if (args.TerminationException is { } ex)
                {
                    output.TrySetException(ex);
                }
                if (args.CompletionState == ActivityInstanceState.Canceled)
                {
                    throw new OperationCanceledException("Workflow canceled.");
                }
                output.TrySetResult(new(args.Outputs, persistenceCount));
            };

            application.Aborted += args => output.TrySetException(args.Reason);

            application.InstanceStore = new FileInstanceStore(Environment.CurrentDirectory);
            application.PersistableIdle += (WorkflowApplicationIdleEventArgs args) =>
            {
                Debug.WriteLine("PersistableIdle");
                var bookmarks = args.Bookmarks;
                Task.Delay(100).ContinueWith(_ =>
                {
                    try
                    {
                        if (++persistenceCount > 100)
                        {
                            throw new Exception("Persisting too many times, aborting test.");
                        }
                        application = CloneWorkflowApplication(application);
                        application.Load(args.InstanceId);
                        foreach (var bookmark in bookmarks.Where(b => b.BookmarkName.StartsWith(AutoResumedBookmarkNamePrefix)))
                        {
                            application.ResumeBookmark(new Bookmark(bookmark.BookmarkName), null);
                        }
                    }
                    catch (Exception ex)
                    {
                        output.TrySetException(ex);
                    }
                });
                return PersistableIdleAction.Unload;
            };

            application.BeginRun(null, null);

            try
            {
                output.Task.Wait(TimeSpan.FromSeconds(15));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }
            return output.Task.GetAwaiter().GetResult();
        }

        private static WorkflowApplication CloneWorkflowApplication(WorkflowApplication application)
        {
            var clone = new WorkflowApplication(application.WorkflowDefinition, application.DefinitionIdentity)
            {
                Aborted = application.Aborted,
                Completed = application.Completed,
                PersistableIdle = application.PersistableIdle,
                InstanceStore = application.InstanceStore,
            };
            foreach (var extension in application.Extensions.GetAllSingletonExtensions())
            {
                clone.Extensions.Add(extension);
            }
            return clone;
        }
    }
}
