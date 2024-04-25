// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
public class FaultCallbackWrapper : CallbackWrapper
{
    private static readonly Type faultCallbackType = typeof(FaultCallback);
    private static readonly Type[] faultCallbackParameters = new Type[] { typeof(NativeActivityFaultContext), typeof(Exception), typeof(ActivityInstance) };

    public FaultCallbackWrapper(FaultCallback callback, ActivityInstance owningInstance)
        : base(callback, owningInstance) { }

    internal void Invoke(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
    {
        EnsureCallback(faultCallbackType, faultCallbackParameters);
        FaultCallback faultCallback = (FaultCallback)Callback;
        faultCallback(faultContext, propagatedException, propagatedFrom);
    }

    internal WorkItem CreateWorkItem(Exception propagatedException, ActivityInstance propagatedFrom, ActivityInstanceReference originalExceptionSource)
        => new FaultWorkItem(this, propagatedException, propagatedFrom, originalExceptionSource);

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

        public override ActivityInstance OriginalExceptionSource => _originalExceptionSource.ActivityInstance;

        [DataMember(Name = "callbackWrapper")]
        internal FaultCallbackWrapper SerializedCallbackWrapper
        {
            get => _callbackWrapper;
            set => _callbackWrapper = value;
        }

        [DataMember(Name = "propagatedException")]
        internal Exception SerializedPropagatedException
        {
            get => _propagatedException;
            set => _propagatedException = value;
        }

        [DataMember(Name = "propagatedFrom")]
        internal ActivityInstance SerializedPropagatedFrom
        {
            get => _propagatedFrom;
            set => _propagatedFrom = value;
        }

        [DataMember(Name = "originalExceptionSource")]
        internal ActivityInstanceReference SerializedOriginalExceptionSource
        {
            get => _originalExceptionSource;
            set => _originalExceptionSource = value;
        }

        public override void TraceCompleted()
        {
            if (TD.CompleteFaultWorkItemIsEnabled())
            {
                TD.CompleteFaultWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
            }
        }

        public override void TraceScheduled()
        {
            if (TD.ScheduleFaultWorkItemIsEnabled())
            {
                TD.ScheduleFaultWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
            }
        }

        public override void TraceStarting()
        {
            if (TD.StartFaultWorkItemIsEnabled())
            {
                TD.StartFaultWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _originalExceptionSource.ActivityInstance.Activity.GetType().ToString(), _originalExceptionSource.ActivityInstance.Activity.DisplayName, _originalExceptionSource.ActivityInstance.Id, _propagatedException);
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            NativeActivityFaultContext faultContext = null;

            try
            {
                faultContext = new NativeActivityFaultContext(ActivityInstance, executor, bookmarkManager, _propagatedException, _originalExceptionSource);
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

                ExceptionToPropagate = e;
            }
            finally
            {
                faultContext?.Dispose();

                // Tell the executor to decrement its no persist count persistence of exceptions is disabled.
                executor.ExitNoPersistForExceptionPropagation();
            }

            return true;
        }
    }
}
