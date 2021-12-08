// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;
using System.Activities.Persistence;
using System.Activities.Runtime;
using System.Xml.Linq;

namespace System.Activities.Statements;

public class CompensationExtension : PersistenceParticipant, IWorkflowInstanceExtension
{
    private static readonly XNamespace compensationNamespace = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/compensation");
    private static readonly XName compensationExtensionData = compensationNamespace.GetName("Data");

    [Fx.Tag.SynchronizationObject(Blocking = false)]
    private Dictionary<long, CompensationTokenData> _compensationTokenTable;

    public CompensationExtension()
    {
        _compensationTokenTable = new Dictionary<long, CompensationTokenData>();
    }

    internal Dictionary<long, CompensationTokenData> CompensationTokenTable
    {
        get => _compensationTokenTable;
        private set => _compensationTokenTable = value;
    }

    internal long Id { get; set; }

    internal Bookmark WorkflowCompensation { get; set; }

    internal Bookmark WorkflowConfirmation { get; set; }

    internal Bookmark WorkflowCompensationScheduled { get; private set; }

    internal bool IsWorkflowCompensationBehaviorScheduled { get; private set; }

    internal WorkflowInstanceProxy Instance { get; private set; }

    internal void Add(long compensationId, CompensationTokenData compensationToken)
    {
        Fx.Assert(compensationToken != null, "compensationToken must be valid");

        CompensationTokenTable[compensationId] = compensationToken;
    }

    internal void Remove(long compensationId) => CompensationTokenTable.Remove(compensationId);

    internal CompensationTokenData Get(long compensationId)
    {
        CompensationTokenTable.TryGetValue(compensationId, out CompensationTokenData compensationToken);
        return compensationToken;
    }

    internal Bookmark FindBookmark(long compensationId, CompensationBookmarkName bookmarkName)
    {
        Bookmark bookmark = null;

        if (CompensationTokenTable.TryGetValue(compensationId, out CompensationTokenData compensationToken))
        {
            bookmark = compensationToken.BookmarkTable[bookmarkName];
        }

        return bookmark;
    }

    internal void SetupWorkflowCompensationBehavior(NativeActivityContext context, BookmarkCallback callback, Activity workflowCompensationBehavior)
    {
        WorkflowCompensationScheduled = context.CreateBookmark(callback);

        Fx.Assert(workflowCompensationBehavior != null, "WorkflowCompensationBehavior must be valid");
        context.ScheduleSecondaryRoot(workflowCompensationBehavior, null);

        // Add the root compensationToken to track all root CA execution order.
        Add(CompensationToken.RootCompensationId, new CompensationTokenData(CompensationToken.RootCompensationId, CompensationToken.RootCompensationId));
        IsWorkflowCompensationBehaviorScheduled = true;
    }

    internal long GetNextId() => ++Id;

    internal void NotifyMessage(NativeActivityContext context, long compensationId, CompensationBookmarkName compensationBookmark)
    {
        Bookmark bookmark = FindBookmark(compensationId, compensationBookmark);

        if (bookmark != null)
        {
            context.ResumeBookmark(bookmark, compensationId);
        }
        else
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkNotRegistered(compensationBookmark)));
        }
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.InterfaceMethodsShouldBeCallableByChildTypes,
    //    Justification = "The inherit class don't need to call this method or access this method")]
    IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions() => null;

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.InterfaceMethodsShouldBeCallableByChildTypes,
    //    Justification = "The inherit class don't need to call this method or access this method")]
    void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance) => Instance = instance;

    // PersistenceParticipant
    protected override void CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
    {
        writeOnlyValues = null;
        readWriteValues = new Dictionary<XName, object>(1)
        {
            {
                compensationExtensionData,
                new List<object>(6)
                {
                    CompensationTokenTable,
                    WorkflowCompensation,
                    WorkflowConfirmation,
                    WorkflowCompensationScheduled,
                    IsWorkflowCompensationBehaviorScheduled,
                    Id
                }
            }
        };
    }

    protected override void PublishValues(IDictionary<XName, object> readWriteValues)
    {
        if (readWriteValues.TryGetValue(compensationExtensionData, out object data))
        {
            List<object> list = (List<object>)data;
            CompensationTokenTable = (Dictionary<long, CompensationTokenData>)list[0];
            WorkflowCompensation = (Bookmark)list[1];
            WorkflowConfirmation = (Bookmark)list[2];
            WorkflowCompensationScheduled = (Bookmark)list[3];
            IsWorkflowCompensationBehaviorScheduled = (bool)list[4];
            Id = (long)list[5];
        }
    }
}
