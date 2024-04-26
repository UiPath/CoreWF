using System.Activities.Runtime;

namespace System.Activities;

public static class ActivityUtilitiesWrapper
{
    public static FaultBookmark CreateFaultBookmark(FaultCallback onFaulted, ActivityInstance owningInstance) =>
        ActivityUtilities.CreateFaultBookmark(onFaulted, owningInstance);

    public static CompletionBookmark CreateCompletionBookmark(CompletionCallback onCompleted, ActivityInstance owningInstance) =>
        ActivityUtilities.CreateCompletionBookmark(onCompleted, owningInstance);
}
