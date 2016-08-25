// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Security;

namespace Microsoft.CoreWf.Runtime
{
    // can't add FuncCompletionCallbackWrapper<T> since we don't know what to close the generic with
    [KnownType(typeof(ActivityCompletionCallbackWrapper))]
    [KnownType(typeof(DelegateCompletionCallbackWrapper))]
    [DataContract]
    internal abstract class CompletionCallbackWrapper : CallbackWrapper
    {
        private static Type s_completionCallbackType = typeof(CompletionCallback);
        private static Type[] s_completionCallbackParameters = new Type[] { typeof(NativeActivityContext), typeof(ActivityInstance) };

        private bool _checkForCancelation;

        private bool _needsToGatherOutputs;

        protected CompletionCallbackWrapper(Delegate callback, ActivityInstance owningInstance)
            : base(callback, owningInstance)
        {
        }

        protected bool NeedsToGatherOutputs
        {
            get
            {
                return _needsToGatherOutputs;
            }

            set
            {
                _needsToGatherOutputs = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "checkForCancelation")]
        internal bool SerializedCheckForCancelation
        {
            get { return _checkForCancelation; }
            set { _checkForCancelation = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "needsToGatherOutputs")]
        internal bool SerializedNeedsToGatherOutputs
        {
            get { return _needsToGatherOutputs; }
            set { _needsToGatherOutputs = value; }
        }

        public void CheckForCancelation()
        {
            _checkForCancelation = true;
        }

        protected virtual void GatherOutputs(ActivityInstance completedInstance)
        {
            // No-op in the base class
        }

        internal WorkItem CreateWorkItem(ActivityInstance completedInstance, ActivityExecutor executor)
        {
            // We use the property to guard against the virtual method call
            // since we don't need it in the common case
            if (this.NeedsToGatherOutputs)
            {
                this.GatherOutputs(completedInstance);
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

            //if (completedInstance.InstanceMap != null)
            //{
            //    completedInstance.InstanceMap.AddEntry(workItem);
            //}

            return workItem;
        }

        [Fx.Tag.SecurityNote(Critical = "Because any implementation will be calling EnsureCallback",
            Safe = "Safe because the method needs to be part of an Activity and we are casting to the callback type and it has a very specific signature. The author of the callback is buying into being invoked from PT.")]
        [SecuritySafeCritical]
        protected internal abstract void Invoke(NativeActivityContext context, ActivityInstance completedInstance);

        [DataContract]
        public class CompletionWorkItem : ActivityExecutionWorkItem, ActivityInstanceMap.IActivityReference
        {
            private CompletionCallbackWrapper _callbackWrapper;
            private ActivityInstance _completedInstance;

            // Called by the Pool.
            public CompletionWorkItem()
            {
                this.IsPooled = true;
            }

            // Only used by non-pooled base classes.
            protected CompletionWorkItem(CompletionCallbackWrapper callbackWrapper, ActivityInstance completedInstance)
                : base(callbackWrapper.ActivityInstance)
            {
                _callbackWrapper = callbackWrapper;
                _completedInstance = completedInstance;
            }

            protected ActivityInstance CompletedInstance
            {
                get
                {
                    return _completedInstance;
                }
            }

            [DataMember(Name = "callbackWrapper")]
            internal CompletionCallbackWrapper SerializedCallbackWrapper
            {
                get { return _callbackWrapper; }
                set { _callbackWrapper = value; }
            }

            [DataMember(Name = "completedInstance")]
            internal ActivityInstance SerializedCompletedInstance
            {
                get { return _completedInstance; }
                set { _completedInstance = value; }
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
                if (TD.IsEnd2EndActivityTracingEnabled() && TD.CompleteCompletionWorkItemIsEnabled())
                {
                    TD.CompleteCompletionWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
                }
            }

            public override void TraceScheduled()
            {
                if (TD.IsEnd2EndActivityTracingEnabled() && TD.ScheduleCompletionWorkItemIsEnabled())
                {
                    TD.ScheduleCompletionWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
                }
            }

            public override void TraceStarting()
            {
                if (TD.IsEnd2EndActivityTracingEnabled() && TD.StartCompletionWorkItemIsEnabled())
                {
                    TD.StartCompletionWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, _completedInstance.Activity.GetType().ToString(), _completedInstance.Activity.DisplayName, _completedInstance.Id);
                }
            }

            public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
            {
                NativeActivityContext context = executor.NativeActivityContextPool.Acquire();

                Fx.Assert(_completedInstance.Activity != null, "Activity definition should always be associated with an activity instance.");

                try
                {
                    context.Initialize(this.ActivityInstance, executor, bookmarkManager);
                    _callbackWrapper.Invoke(context, _completedInstance);
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
                    context.Dispose();
                    executor.NativeActivityContextPool.Release(context);

                    if (this.ActivityInstance.InstanceMap != null)
                    {
                        this.ActivityInstance.InstanceMap.RemoveEntry(this);
                    }
                }

                return true;
            }

            Activity ActivityInstanceMap.IActivityReference.Activity
            {
                get
                {
                    return _completedInstance.Activity;
                }
            }

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
                : base(callbackWrapper, completedInstance)
            {
            }

            public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
            {
                if (this.CompletedInstance.State != ActivityInstanceState.Closed && this.ActivityInstance.IsPerformingDefaultCancelation)
                {
                    this.ActivityInstance.MarkCanceled();
                }

                return base.Execute(executor, bookmarkManager);
            }
        }
    }
}
