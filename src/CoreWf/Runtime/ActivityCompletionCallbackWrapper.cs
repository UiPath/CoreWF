// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Runtime
{
    using System;
    using System.Runtime.Serialization;
    using System.Security;

    [DataContract]
    internal class ActivityCompletionCallbackWrapper : CompletionCallbackWrapper
    {
        private static readonly Type completionCallbackType = typeof(CompletionCallback);
        private static readonly Type[] completionCallbackParameters = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance) };

        public ActivityCompletionCallbackWrapper(CompletionCallback callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
        }

        [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
        {
            EnsureCallback(completionCallbackType, completionCallbackParameters);
            CompletionCallback completionCallback = (CompletionCallback)this.Callback;
            completionCallback(context, completedInstance);
        }
    }
}
