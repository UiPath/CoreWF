// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.CoreWf.Statements
{
    //[ContentProperty("Body")]
    public sealed class CancellationScope : NativeActivity
    {
        private Collection<Variable> _variables;
        private Variable<bool> _suppressCancel;

        public CancellationScope()
            : base()
        {
            _suppressCancel = new Variable<bool>();
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (_variables == null)
                {
                    _variables = new ValidatingCollection<Variable>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        }
                    };
                }
                return _variables;
            }
        }

        [DefaultValue(null)]
        //[DependsOn("Variables")]
        public Activity Body
        {
            get;
            set;
        }

        [DefaultValue(null)]
        //[DependsOn("Body")]
        public Activity CancellationHandler
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(this.Body);
            metadata.AddChild(this.CancellationHandler);
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(_suppressCancel);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (this.Body != null)
            {
                context.ScheduleActivity(this.Body, new CompletionCallback(OnBodyComplete));
            }
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            // Determine whether to run the Cancel based on whether the body
            // canceled rather than whether cancel had been requested.
            if (completedInstance.State == ActivityInstanceState.Canceled ||
                (context.IsCancellationRequested && completedInstance.State == ActivityInstanceState.Faulted))
            {
                // We don't cancel the cancel handler
                _suppressCancel.Set(context, true);

                context.MarkCanceled();

                if (this.CancellationHandler != null)
                {
                    context.ScheduleActivity(this.CancellationHandler, onFaulted: new FaultCallback(OnExceptionFromCancelHandler));
                }
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            bool suppressCancel = _suppressCancel.Get(context);
            if (!suppressCancel)
            {
                context.CancelChildren();
            }
        }

        private void OnExceptionFromCancelHandler(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            _suppressCancel.Set(context, false);
        }
    }
}
