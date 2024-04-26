// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

// can't add FuncCompletionCallbackWrapper<T> since we don't know what to close the generic with
[KnownType(typeof(ActivityCompletionCallbackWrapper))]
[KnownType(typeof(DelegateCompletionCallbackWrapper))]
[DataContract]
public abstract class CompletionCallbackWrapper : CallbackWrapper
{
    private bool _checkForCancelation;
    private bool _needsToGatherOutputs;

    public CompletionCallbackWrapper(Delegate callback, ActivityInstance owningInstance)
        : base(callback, owningInstance) { }

    protected bool NeedsToGatherOutputs
    {
        get => _needsToGatherOutputs;
        set => _needsToGatherOutputs = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "checkForCancelation")]
    internal bool SerializedCheckForCancelation
    {
        get => _checkForCancelation;
        set => _checkForCancelation = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "needsToGatherOutputs")]
    internal bool SerializedNeedsToGatherOutputs
    {
        get => _needsToGatherOutputs;
        set => _needsToGatherOutputs = value;
    }

    internal void CheckForCancelation() => _checkForCancelation = true;

    protected virtual void GatherOutputs(ActivityInstance completedInstance) { }

    internal WorkItem CreateWorkItem(ActivityInstance completedInstance, ActivityExecutor executor)
    {
        // We use the property to guard against the virtual method call
        // since we don't need it in the common case
        if (NeedsToGatherOutputs)
        {
            GatherOutputs(completedInstance);
        }

        CompletionWorkItem workItem;

        if (_checkForCancelation)
        {
            workItem = new CompletionWithCancelationCheckWorkItem(this, completedInstance);
        }
        else
        {
            workItem = executor.CompletionWorkItemPool.Acquire();
            workItem.Initialize(this, completedInstance);
        }

        completedInstance.InstanceMap?.AddEntry(workItem);

        return workItem;
    }

    protected internal abstract void Invoke(NativeActivityContext context, ActivityInstance completedInstance);

    [DataContract]
    internal class CompletionWorkItem : ActivityExecutionWorkItem, ActivityInstanceMap.IActivityReference
    {
        private CompletionCallbackWrapper _callbackWrapper;
        private ActivityInstance _completedInstance;

        // Called by the Pool.
        public CompletionWorkItem()
        {
            IsPooled = true;
        }

        // Only used by non-pooled base classes.
        protected CompletionWorkItem(CompletionCallbackWrapper callbackWrapper, ActivityInstance completedInstance)
            : base(callbackWrapper.ActivityInstance)
        {
            _callbackWrapper = callbackWrapper;
            _completedInstance = completedInstance;
        }

        protected ActivityInstance CompletedInstance => _completedInstance;

        [DataMember(Name = "callbackWrapper")]
        internal CompletionCallbackWrapper SerializedCallbackWrapper
        {
            get => _callbackWrapper;
            set => _callbackWrapper = value;
        }

        [DataMember(Name = "completedInstance")]
        internal ActivityInstance SerializedCompletedInstance
        {
            get => _completedInstance;
            set => _completedInstance = value;
        }

        public void Initialize(CompletionCallbackWrapper callbackWrapper, ActivityInstance completedInstance)
        {
            base.Reinitialize(callbackWrapper.ActivityInstance);
            _callbackWrapper = callbackWrapper;
            _completedInstance = completedInstance;
        }

        protected override void ReleaseToPool(ActivityExecutor executor)
        {
            base.ClearForReuse();
            _callbackWrapper = null;
            _completedInstance = null;

            executor.CompletionWorkItemPool.Release(this);
        }

        public override void TraceCompleted()
        {
            if (TD.CompleteCompletionWorkItemIsEnabled())
            {
                TD.CompleteCompletionWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
            }
        }

        public override void TraceScheduled()
        {
            if (TD.ScheduleCompletionWorkItemIsEnabled())
            {
                TD.ScheduleCompletionWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
            }
        }

        public override void TraceStarting()
        {
            if (TD.StartCompletionWorkItemIsEnabled())
            {
                TD.StartCompletionWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            NativeActivityContext context = executor.NativeActivityContextPool.Acquire();

            Fx.Assert(_completedInstance.Activity != null, "Activity definition should always be associated with an activity instance.");

            try
            {
                context.Initialize(ActivityInstance, executor, bookmarkManager);
                _callbackWrapper.Invoke(context, _completedInstance);
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
                context.Dispose();
                executor.NativeActivityContextPool.Release(context);

                ActivityInstance.InstanceMap?.RemoveEntry(this);
            }

            return true;
        }

        Activity ActivityInstanceMap.IActivityReference.Activity => _completedInstance.Activity;

        void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap)
        {
            if (_completedInstance.Activity == null)
            {
                ((ActivityInstanceMap.IActivityReference)_completedInstance).Load(activity, instanceMap);
            }
        }

    }

    [DataContract]
    private class CompletionWithCancelationCheckWorkItem : CompletionWorkItem
    {
        public CompletionWithCancelationCheckWorkItem(CompletionCallbackWrapper callbackWrapper, ActivityInstance completedInstance)
            : base(callbackWrapper, completedInstance) { }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            if (CompletedInstance.State != ActivityInstanceState.Closed && ActivityInstance.IsPerformingDefaultCancelation)
            {
                ActivityInstance.MarkCanceled();
            }

            return base.Execute(executor, bookmarkManager);
        }
    }
}
