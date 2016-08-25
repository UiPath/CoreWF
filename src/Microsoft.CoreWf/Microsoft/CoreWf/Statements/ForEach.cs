// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.CoreWf.Statements
{
    //[ContentProperty("Body")]
    public sealed class ForEach<T> : NativeActivity
    {
        private Variable<IEnumerator<T>> _valueEnumerator;
        private CompletionCallback _onChildComplete;

        public ForEach()
            : base()
        {
            _valueEnumerator = new Variable<IEnumerator<T>>();
        }

        [DefaultValue(null)]
        public ActivityAction<T> Body
        {
            get;
            set;
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<IEnumerable<T>> Values
        {
            get;
            set;
        }

        private CompletionCallback OnChildComplete
        {
            get
            {
                if (_onChildComplete == null)
                {
                    _onChildComplete = new CompletionCallback(GetStateAndExecute);
                }

                return _onChildComplete;
            }
        }

        //protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument valuesArgument = new RuntimeArgument("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true);
            metadata.Bind(this.Values, valuesArgument);

            metadata.AddArgument(valuesArgument);
            metadata.AddDelegate(this.Body);
            metadata.AddImplementationVariable(_valueEnumerator);
        }

        protected override void Execute(NativeActivityContext context)
        {
            IEnumerable<T> values = this.Values.Get(context);
            if (values == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ForEachRequiresNonNullValues(this.DisplayName)));
            }

            IEnumerator<T> valueEnumerator = values.GetEnumerator();
            _valueEnumerator.Set(context, valueEnumerator);

            if (this.Body == null || this.Body.Handler == null)
            {
                while (valueEnumerator.MoveNext())
                {
                    // do nothing                
                };
                valueEnumerator.Dispose();
                return;
            }
            InternalExecute(context, null, valueEnumerator);
        }

        private void GetStateAndExecute(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<T> valueEnumerator = _valueEnumerator.Get(context);
            Fx.Assert(valueEnumerator != null, "GetStateAndExecute");
            InternalExecute(context, completedInstance, valueEnumerator);
        }

        private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance, IEnumerator<T> valueEnumerator)
        {
            Fx.Assert(this.Body != null && this.Body.Handler != null, "Body and Body.Handler should not be null");

            if (!valueEnumerator.MoveNext())
            {
                if (completedInstance != null)
                {
                    if (completedInstance.State == ActivityInstanceState.Canceled ||
                        (context.IsCancellationRequested && completedInstance.State == ActivityInstanceState.Faulted))
                    {
                        context.MarkCanceled();
                    }
                }
                valueEnumerator.Dispose();
                return;
            }

            // After making sure there is another value, let's check for cancelation
            if (context.IsCancellationRequested)
            {
                context.MarkCanceled();
                valueEnumerator.Dispose();
                return;
            }

            context.ScheduleAction(this.Body, valueEnumerator.Current, this.OnChildComplete);
        }
    }
}
