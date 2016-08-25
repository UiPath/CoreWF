// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Security;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class FaultCallbackWrapper : CallbackWrapper
    {
        private static readonly Type s_faultCallbackType = typeof(FaultCallback);
        private static readonly Type[] s_faultCallbackParameters = new Type[] { typeof(NativeActivityFaultContext), typeof(Exception), typeof(ActivityInstance) };

        public FaultCallbackWrapper(FaultCallback callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
        }

        [Fx.Tag.SecurityNote(Critical = "Because we are calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        public void Invoke(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            EnsureCallback(s_faultCallbackType, s_faultCallbackParameters);
            FaultCallback faultCallback = (FaultCallback)this.Callback;
            faultCallback(faultContext, propagatedException, propagatedFrom);
        }

        public WorkItem CreateWorkItem(Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
        {
            return new FaultWorkItem(this, propagatedException, propagatedFrom, originalExceptionSource);
        }

        [DataContract]
        internal class FaultWorkItem : ActivityExecutionWorkItem
        {
            private FaultCallbackWrapper _callbackWrapper;
            private Exception _propagatedException;
            private ActivityInstance _propagatedFrom;
            private ActivityInstanceReference _originalExceptionSource;

            public FaultWorkItem(FaultCallbackWrapper callbackWrapper, Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
                : base(callbackWrapper.ActivityInstance)
            {
                _callbackWrapper = callbackWrapper;
                _propagatedException = propagatedException;
                _propagatedFrom = propagatedFrom;
                _originalExceptionSource = originalExceptionSource;
            }

            public override ActivityInstance OriginalExceptionSource
            {
                get
                {
                    return _originalExceptionSource.ActivityInstance;
                }
            }

            [DataMember(Name = "callbackWrapper")]
            internal FaultCallbackWrapper SerializedCallbackWrapper
            {
                get { return _callbackWrapper; }
                set { _callbackWrapper = value; }
            }

            [DataMember(Name = "propagatedException")]
            internal Exception SerializedPropagatedException
            {
                get { return _propagatedException; }
                set { _propagatedException = value; }
            }

            [DataMember(Name = "propagatedFrom")]
            internal ActivityInstance SerializedPropagatedFrom
            {
                get { return _propagatedFrom; }
                set { _propagatedFrom = value; }
            }

            [DataMember(Name = "originalExceptionSource")]
            internal ActivityInstanceReference SerializedOriginalExceptionSource
            {
                get { return _originalExceptionSource; }
                set { _originalExceptionSource = value; }
            }

            public override void TraceCompleted()
            {
                if (TD.CompleteFaultWorkItemIsEnabled())
                {
                    TD.CompleteFaultWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
                }
            }

            public override void TraceScheduled()
            {
                if (TD.ScheduleFaultWorkItemIsEnabled())
                {
                    TD.ScheduleFaultWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
                }
            }

            public override void TraceStarting()
            {
                if (TD.StartFaultWorkItemIsEnabled())
                {
                    TD.StartFaultWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
                }
            }

            public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
            {
                NativeActivityFaultContext faultContext = null;

                try
                {
                    faultContext = new NativeActivityFaultContext(this.ActivityInstance, executor, bookmarkManager, _propagatedException, _originalExceptionSource);
                    _callbackWrapper.Invoke(faultContext, _propagatedException, _propagatedFrom);

                    if (!faultContext.IsFaultHandled)
                    {
                        SetExceptionToPropagateWithoutAbort(_propagatedException);
                    }
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    this.ExceptionToPropagate = e;
                }
                finally
                {
                    if (faultContext != null)
                    {
                        faultContext.Dispose();
                    }

                    // Tell the executor to decrement its no persist count persistence of exceptions is disabled.
                    executor.ExitNoPersistForExceptionPropagation();
                }

                return true;
            }
        }
    }
}
