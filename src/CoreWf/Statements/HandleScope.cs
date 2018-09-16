// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using CoreWf;
    using System.Collections.ObjectModel;
    using Portable.Xaml.Markup;
    using CoreWf.Runtime;
    using CoreWf.Internals;

    [ContentProperty("Body")]
    public sealed class HandleScope<THandle> : NativeActivity 
        where THandle : Handle
    {
        private Variable<THandle> declaredHandle;

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
                if (this.declaredHandle == null)
                {
                    this.declaredHandle = new Variable<THandle>();
                }
            }
            else
            {
                this.declaredHandle = null;
            }

            if (this.declaredHandle != null)
            {
                ActivityUtilities.Add(ref implementationVariables, this.declaredHandle);
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
                Fx.Assert(this.declaredHandle != null, "We should have declared the variable if we didn't have the argument set.");
                scopedHandle = this.declaredHandle.Get(context);
            }
            else
            {
                scopedHandle = this.Handle.Get(context);
            }

            if (scopedHandle == null)
            {
                throw FxTrace.Exception.ArgumentNull("Handle");
            }

            context.Properties.Add(scopedHandle.ExecutionPropertyName, scopedHandle);

            if (this.Body != null)
            {
                context.ScheduleActivity(this.Body);
            }
        }
    }
}
