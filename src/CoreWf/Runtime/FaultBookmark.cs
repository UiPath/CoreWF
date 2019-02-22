// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal class FaultBookmark
    {
        private FaultCallbackWrapper callbackWrapper;

        public FaultBookmark(FaultCallbackWrapper callbackWrapper)
        {
            this.callbackWrapper = callbackWrapper;
        }

        [DataMember(Name = "callbackWrapper")]
        internal FaultCallbackWrapper SerializedCallbackWrapper
        {
            get { return this.callbackWrapper; }
            set { this.callbackWrapper = value; }
        }

        public WorkItem GenerateWorkItem(Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
        {
            return this.callbackWrapper.CreateWorkItem(propagatedException, propagatedFrom, originalExceptionSource);
        }
    }
}
