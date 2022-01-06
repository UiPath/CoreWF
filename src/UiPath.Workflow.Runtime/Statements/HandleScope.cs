// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class HandleScope<THandle> : NativeActivity
    where THandle : Handle
{
    private Variable<THandle> declaredHandle;

    public HandleScope() { }

    public InArgument<THandle> Handle { get; set; }

    public Activity Body { get; set; }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument handleArgument = new("Handle", typeof(THandle), ArgumentDirection.In);
        metadata.Bind(Handle, handleArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { handleArgument });

        if (Body != null)
        {
            metadata.SetChildrenCollection(new Collection<Activity> { Body });
        }

        Collection<Variable> implementationVariables = null;

        if ((Handle == null) || Handle.IsEmpty)
        {
            declaredHandle ??= new Variable<THandle>();
        }
        else
        {
            declaredHandle = null;
        }

        if (declaredHandle != null)
        {
            ActivityUtilities.Add(ref implementationVariables, declaredHandle);
        }

        metadata.SetImplementationVariablesCollection(implementationVariables);
    }

    protected override void Execute(NativeActivityContext context)
    {
        // We should go through the motions even if there is no Body for debugging
        // purposes.  When testing handles people will probably use empty scopes
        // expecting everything except the Body execution to occur.

        Handle scopedHandle;
        if ((Handle == null) || Handle.IsEmpty)
        {
            Fx.Assert(declaredHandle != null, "We should have declared the variable if we didn't have the argument set.");
            scopedHandle = declaredHandle.Get(context);
        }
        else
        {
            scopedHandle = Handle.Get(context);
        }

        if (scopedHandle == null)
        {
            throw FxTrace.Exception.ArgumentNull("Handle");
        }

        context.Properties.Add(scopedHandle.ExecutionPropertyName, scopedHandle);

        if (Body != null)
        {
            context.ScheduleActivity(Body);
        }
    }
}
