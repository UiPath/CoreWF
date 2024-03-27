// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
public class ActivityCompletionCallbackWrapper : CompletionCallbackWrapper
{
    private static readonly Type completionCallbackType = typeof(CompletionCallback);
    private static readonly Type[] completionCallbackParameters = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance) };

    public ActivityCompletionCallbackWrapper(CompletionCallback callback, ActivityInstance owningInstance)
        : base(callback, owningInstance) { }

    protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
    {
        EnsureCallback(completionCallbackType, completionCallbackParameters);
        CompletionCallback completionCallback = (CompletionCallback)Callback;
        completionCallback(context, completedInstance);
    }
}
