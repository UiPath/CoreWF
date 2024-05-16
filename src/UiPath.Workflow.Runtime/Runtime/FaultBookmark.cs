// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
internal class FaultBookmark
{
    private FaultCallbackWrapper _callbackWrapper;

    public FaultBookmark(FaultCallbackWrapper callbackWrapper)
    {
        _callbackWrapper = callbackWrapper;
    }

    [DataMember(Name = "callbackWrapper")]
    public FaultCallbackWrapper SerializedCallbackWrapper
    {
        get => _callbackWrapper;
        set => _callbackWrapper = value;
    }

    internal WorkItem GenerateWorkItem(Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
        => _callbackWrapper.CreateWorkItem(propagatedException, propagatedFrom, originalExceptionSource);
}
