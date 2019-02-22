// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System;
    using System.Activities;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Activities.Runtime.Collections;
    using Portable.Xaml.Markup;
    using System.Activities.Internals;

#if NET45
    using System.Activities.DynamicUpdate; 
#endif

    [ContentProperty("Body")]
    public sealed class CancellationScope : NativeActivity
    {
        private Collection<Variable> variables;
        private readonly Variable<bool> suppressCancel;

        public CancellationScope()
            : base()
        {
            this.suppressCancel = new Variable<bool>();
        }

        public Collection<Variable> Variables
        {
            get
            {
                if (this.variables == null)
                {
                    this.variables = new ValidatingCollection<Variable>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw FxTrace.Exception.ArgumentNull(nameof(item));
                            }
                        }
                    };
                }
                return this.variables;
            }
        }

        [DefaultValue(null)]
        [DependsOn("Variables")]
        public Activity Body
        {
            get;
            set;
        }

        [DefaultValue(null)]
        [DependsOn("Body")]
        public Activity CancellationHandler
        {
            get;
            set;
        }

#if NET45
        protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        {
            metadata.AllowUpdateInsideThisActivity();
        } 
#endif

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(this.Body);
            metadata.AddChild(this.CancellationHandler);
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(this.suppressCancel);
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
                this.suppressCancel.Set(context, true);

                context.MarkCanceled();

                if (this.CancellationHandler != null)
                {
                    context.ScheduleActivity(this.CancellationHandler, onFaulted: new FaultCallback(OnExceptionFromCancelHandler));
                }
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            bool suppressCancel = this.suppressCancel.Get(context);
            if (!suppressCancel)
            {
                context.CancelChildren();
            }
        }

        private void OnExceptionFromCancelHandler(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            this.suppressCancel.Set(context, false);
        }
    }
}
