// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Collections.ObjectModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Body")]
    public sealed class HandleScope<THandle> : NativeActivity
        where THandle : Handle
    {
        private Variable<THandle> _declaredHandle;

        public HandleScope()
        {
        }

        public InArgument<THandle> Handle
        {
            get;
            set;
        }

        public Activity Body
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument handleArgument = new RuntimeArgument("Handle", typeof(THandle), ArgumentDirection.In);
            metadata.Bind(this.Handle, handleArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { handleArgument });

            if (this.Body != null)
            {
                metadata.SetChildrenCollection(new Collection<Activity> { this.Body });
            }

            Collection<Variable> implementationVariables = null;

            if ((this.Handle == null) || this.Handle.IsEmpty)
            {
                if (_declaredHandle == null)
                {
                    _declaredHandle = new Variable<THandle>();
                }
            }
            else
            {
                _declaredHandle = null;
            }

            if (_declaredHandle != null)
            {
                ActivityUtilities.Add(ref implementationVariables, _declaredHandle);
            }

            metadata.SetImplementationVariablesCollection(implementationVariables);
        }

        protected override void Execute(NativeActivityContext context)
        {
            // We should go through the motions even if there is no Body for debugging
            // purposes.  When testing handles people will probably use empty scopes
            // expecting everything except the Body execution to occur.

            Handle scopedHandle = null;

            if ((this.Handle == null) || this.Handle.IsEmpty)
            {
                Fx.Assert(_declaredHandle != null, "We should have declared the variable if we didn't have the argument set.");
                scopedHandle = _declaredHandle.Get(context);
            }
            else
            {
                scopedHandle = this.Handle.Get(context);
            }

            if (scopedHandle == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("Handle");
            }

            context.Properties.Add(scopedHandle.ExecutionPropertyName, scopedHandle);

            if (this.Body != null)
            {
                context.ScheduleActivity(this.Body);
            }
        }
    }
}
