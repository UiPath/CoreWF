// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf.Statements
{
    public sealed class TerminateWorkflow : NativeActivity
    {
        public TerminateWorkflow() { }

        [DefaultValue(null)]
        public InArgument<string> Reason { get; set; }

        [DefaultValue(null)]
        public InArgument<Exception> Exception { get; set; }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>();

            RuntimeArgument reasonArgument = new RuntimeArgument("Reason", typeof(string), ArgumentDirection.In, false);
            metadata.Bind(this.Reason, reasonArgument);

            RuntimeArgument exceptionArgument = new RuntimeArgument("Exception", typeof(Exception), ArgumentDirection.In, false);
            metadata.Bind(this.Exception, exceptionArgument);

            arguments.Add(reasonArgument);
            arguments.Add(exceptionArgument);

            metadata.SetArgumentsCollection(arguments);

            if ((this.Reason == null || this.Reason.IsEmpty) &&
                (this.Exception == null || this.Exception.IsEmpty))
            {
                metadata.AddValidationError(SR.OneOfTwoPropertiesMustBeSet("Reason", "Exception", "TerminateWorkflow", this.DisplayName));
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            // If Reason is provided, we'll create a WorkflowApplicationTerminatedException from
            // it, wrapping Exception if it is also provided. Otherwise just use Exception.
            // If neither is provided just throw a new WorkflowTerminatedException.
            string reason = Reason.Get(context);
            Exception exception = Exception.Get(context);
            if (!string.IsNullOrEmpty(reason))
            {
                context.Terminate(new WorkflowTerminatedException(reason, exception));
            }
            else if (exception != null)
            {
                context.Terminate(exception);
            }
            else
            {
                context.Terminate(new WorkflowTerminatedException());
            }
        }
    }
}
