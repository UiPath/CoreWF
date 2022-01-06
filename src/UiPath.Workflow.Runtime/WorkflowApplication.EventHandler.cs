// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;
using Tracking;

public partial class WorkflowApplication
{
    private class IdleEventHandler
    {
        private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;
        private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage2Callback;

        public IdleEventHandler() { }

        private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
        {
            get
            {
                _stage1Callback ??= new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                return _stage1Callback;
            }
        }

        private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage2Callback
        {
            get
            {
                _stage2Callback ??= new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage2Complete);
                return _stage2Callback;
            }
        }

        public bool Run(WorkflowApplication instance)
        {
            IAsyncResult result = null;

            if (instance.Controller.TrackingEnabled)
            {
                instance.Controller.Track(new WorkflowInstanceRecord(instance.Id, instance.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Idle, instance.DefinitionIdentity));

                instance.EventData.NextCallback = Stage1Callback;
                result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                if (!result.CompletedSynchronously)
                {
                    return false;
                }
            }

            return OnStage1Complete(result, instance, true);
        }

        private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication application, bool isStillSync)
        {
            if (lastResult != null)
            {
                application.Controller.EndFlushTrackingRecords(lastResult);
            }

            IAsyncResult result = null;

            if (application.RaiseIdleEvent())
            {
                if (application.Controller.IsPersistable && application._persistenceManager != null)
                {
                    Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> persistableIdleHandler = application.PersistableIdle;

                    if (persistableIdleHandler != null)
                    {
                        PersistableIdleAction action = PersistableIdleAction.None;

                        application._handlerThreadId = Environment.CurrentManagedThreadId;

                        try
                        {
                            application._isInHandler = true;
                            action = persistableIdleHandler(new WorkflowApplicationIdleEventArgs(application));
                        }
                        finally
                        {
                            application._isInHandler = false;
                        }

                        if (TD.WorkflowApplicationPersistableIdleIsEnabled())
                        {
                            TD.WorkflowApplicationPersistableIdle(application.Id.ToString(), action.ToString());
                        }

                        if (action != PersistableIdleAction.None)
                        {
                            PersistenceOperation operation = PersistenceOperation.Unload;

                            if (action == PersistableIdleAction.Persist)
                            {
                                operation = PersistenceOperation.Save;
                            }
                            else if (action != PersistableIdleAction.Unload)
                            {
                                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidIdleAction));
                            }

                            application.EventData.NextCallback = Stage2Callback;
                            result = application.BeginInternalPersist(operation, ActivityDefaults.InternalSaveTimeout, true, EventFrameCallback, application.EventData);

                            if (!result.CompletedSynchronously)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // Trace the default action
                        if (TD.WorkflowApplicationPersistableIdleIsEnabled())
                        {
                            TD.WorkflowApplicationPersistableIdle(application.Id.ToString(), PersistableIdleAction.None.ToString());
                        }
                    }
                }
            }

            return OnStage2Complete(result, application, isStillSync);
        }

        private bool OnStage2Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
        {
            if (lastResult != null)
            {
                instance.EndInternalPersist(lastResult);
            }

            return true;
        }
    }

    private class CompletedEventHandler
    {
        private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;
        private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage2Callback;

        public CompletedEventHandler() { }

        private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
        {
            get
            {
                _stage1Callback ??= new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                return _stage1Callback;
            }
        }

        private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage2Callback
        {
            get
            {
                _stage2Callback ??= new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage2Complete);
                return _stage2Callback;
            }
        }

        public bool Run(WorkflowApplication instance)
        {
            IAsyncResult result = null;
            if (instance.Controller.HasPendingTrackingRecords)
            {
                instance.EventData.NextCallback = Stage1Callback;
                result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                if (!result.CompletedSynchronously)
                {
                    return false;
                }
            }

            return OnStage1Complete(result, instance, true);
        }

        private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
        {
            if (lastResult != null)
            {
                instance.Controller.EndFlushTrackingRecords(lastResult);
            }
            ActivityInstanceState completionState = instance.Controller.GetCompletionState(out IDictionary<string, object> outputs, out Exception completionException);

            if (instance._invokeCompletedCallback == null)
            {
                Action<WorkflowApplicationCompletedEventArgs> handler = instance.Completed;

                if (handler != null)
                {
                    instance._handlerThreadId = Environment.CurrentManagedThreadId;

                    try
                    {
                        instance._isInHandler = true;
                        handler(new WorkflowApplicationCompletedEventArgs(instance, completionException, completionState, outputs));
                    }
                    finally
                    {
                        instance._isInHandler = false;
                    }
                }
            }

            switch (completionState)
            {
                case ActivityInstanceState.Closed:
                    if (TD.WorkflowApplicationCompletedIsEnabled())
                    {
                        TD.WorkflowApplicationCompleted(instance.Id.ToString());
                    }
                    break;
                case ActivityInstanceState.Canceled:
                    if (TD.WorkflowInstanceCanceledIsEnabled())
                    {
                        TD.WorkflowInstanceCanceled(instance.Id.ToString());
                    }
                    break;
                case ActivityInstanceState.Faulted:
                    if (TD.WorkflowApplicationTerminatedIsEnabled())
                    {
                        TD.WorkflowApplicationTerminated(instance.Id.ToString(), completionException);
                    }
                    break;
            }

            IAsyncResult result = null;
            Fx.Assert(instance.Controller.IsPersistable, "Should not be in a No Persist Zone once the instance is complete.");
            if (instance._persistenceManager != null || instance.HasPersistenceModule)
            {
                instance.EventData.NextCallback = Stage2Callback;
                result = instance.BeginInternalPersist(PersistenceOperation.Unload, ActivityDefaults.InternalSaveTimeout, true, EventFrameCallback, instance.EventData);

                if (!result.CompletedSynchronously)
                {
                    return false;
                }
            }
            else
            {
                instance.MarkUnloaded();
            }

            return OnStage2Complete(result, instance, isStillSync);
        }

        private bool OnStage2Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
        {
            if (lastResult != null)
            {
                instance.EndInternalPersist(lastResult);
            }

            if (instance._invokeCompletedCallback != null)
            {
                instance._invokeCompletedCallback();
            }

            return true;
        }
    }

    private class UnhandledExceptionEventHandler
    {
        private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;

        public UnhandledExceptionEventHandler() { }

        private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
        {
            get
            {
                _stage1Callback ??= new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                return _stage1Callback;
            }
        }

        public bool Run(WorkflowApplication instance, Exception exception, Activity exceptionSource, string exceptionSourceInstanceId)
        {
            IAsyncResult result = null;

            if (instance.Controller.HasPendingTrackingRecords)
            {
                instance.EventData.NextCallback = Stage1Callback;
                instance.EventData.UnhandledException = exception;
                instance.EventData.UnhandledExceptionSource = exceptionSource;
                instance.EventData.UnhandledExceptionSourceInstance = exceptionSourceInstanceId;
                result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                if (!result.CompletedSynchronously)
                {
                    return false;
                }
            }

            return OnStage1Complete(result, instance, exception, exceptionSource, exceptionSourceInstanceId);
        }

        private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
            => OnStage1Complete(lastResult, instance, instance.EventData.UnhandledException, instance.EventData.UnhandledExceptionSource, instance.EventData.UnhandledExceptionSourceInstance);

        private static bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, Exception exception, Activity source, string sourceInstanceId)
        {
            if (lastResult != null)
            {
                instance.Controller.EndFlushTrackingRecords(lastResult);
            }

            Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> handler = instance.OnUnhandledException;

            UnhandledExceptionAction action = UnhandledExceptionAction.Terminate;

            if (handler != null)
            {
                try
                {
                    instance._isInHandler = true;
                    instance._handlerThreadId = Environment.CurrentManagedThreadId;

                    action = handler(new WorkflowApplicationUnhandledExceptionEventArgs(instance, exception, source, sourceInstanceId));
                }
                finally
                {
                    instance._isInHandler = false;
                }
            }

            if (instance._invokeCompletedCallback != null)
            {
                action = UnhandledExceptionAction.Terminate;
            }

            if (TD.WorkflowApplicationUnhandledExceptionIsEnabled())
            {
                TD.WorkflowApplicationUnhandledException(instance.Id.ToString(), source.GetType().ToString(), source.DisplayName, action.ToString(), exception);
            }

            switch (action)
            {
                case UnhandledExceptionAction.Abort:
                    instance.AbortInstance(exception, true);
                    break;
                case UnhandledExceptionAction.Cancel:
                    instance.Controller.ScheduleCancel();
                    break;
                case UnhandledExceptionAction.Terminate:
                    instance.TerminateCore(exception);
                    break;
                default:
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidUnhandledExceptionAction));
            }

            return true;
        }
    }
}
