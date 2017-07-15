// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace CoreWf.Runtime
{
    [DataContract]
    internal class FaultBookmark
    {
        private FaultCallbackWrapper _callbackWrapper;

        public FaultBookmark(FaultCallbackWrapper callbackWrapper)
        {
            _callbackWrapper = callbackWrapper;
        }

        [DataMember(Name = "callbackWrapper")]
        internal FaultCallbackWrapper SerializedCallbackWrapper
        {
            get { return _callbackWrapper; }
            set { _callbackWrapper = value; }
        }

        public WorkItem GenerateWorkItem(Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
        {
            return _callbackWrapper.CreateWorkItem(propagatedException, propagatedFrom, originalExceptionSource);
        }
    }
}
