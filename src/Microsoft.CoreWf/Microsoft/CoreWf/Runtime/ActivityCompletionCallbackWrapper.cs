// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Security;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class ActivityCompletionCallbackWrapper : CompletionCallbackWrapper
    {
        private static readonly Type s_completionCallbackType = typeof(CompletionCallback);
        private static readonly Type[] s_completionCallbackParameters = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance) };

        public ActivityCompletionCallbackWrapper(CompletionCallback callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
        }

        [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        protected internal override void Invoke(NativeActivityContext context, ActivityInstance completedInstance)
        {
            EnsureCallback(s_completionCallbackType, s_completionCallbackParameters);
            CompletionCallback completionCallback = (CompletionCallback)this.Callback;
            completionCallback(context, completedInstance);
        }
    }
}
