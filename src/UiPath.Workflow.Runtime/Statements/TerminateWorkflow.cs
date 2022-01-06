// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Statements;

public sealed class TerminateWorkflow : NativeActivity
{
    public TerminateWorkflow() { }

    [DefaultValue(null)]
    public InArgument<string> Reason { get; set; }

    [DefaultValue(null)]
    public InArgument<Exception> Exception { get; set; }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();

        RuntimeArgument reasonArgument = new("Reason", typeof(string), ArgumentDirection.In, false);
        metadata.Bind(Reason, reasonArgument);

        RuntimeArgument exceptionArgument = new("Exception", typeof(Exception), ArgumentDirection.In, false);
        metadata.Bind(Exception, exceptionArgument);

        arguments.Add(reasonArgument);
        arguments.Add(exceptionArgument);

        metadata.SetArgumentsCollection(arguments);

        if ((Reason == null || Reason.IsEmpty) &&
            (Exception == null || Exception.IsEmpty))
        {
            metadata.AddValidationError(SR.OneOfTwoPropertiesMustBeSet("Reason", "Exception", "TerminateWorkflow", DisplayName));
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
