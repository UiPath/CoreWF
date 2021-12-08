// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class CancellationScope : NativeActivity
{
    private Collection<Variable> _variables;
    private readonly Variable<bool> _suppressCancel;

    public CancellationScope()
        : base()
    {
        _suppressCancel = new Variable<bool>();
    }

    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
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
            return _variables;
        }
    }

    [DefaultValue(null)]
    [DependsOn("Variables")]
    public Activity Body { get; set; }

    [DefaultValue(null)]
    [DependsOn("Body")]
    public Activity CancellationHandler { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.AddChild(Body);
        metadata.AddChild(CancellationHandler);
        metadata.SetVariablesCollection(Variables);
        metadata.AddImplementationVariable(_suppressCancel);
    }

    protected override void Execute(NativeActivityContext context)
    {
        if (Body != null)
        {
            context.ScheduleActivity(Body, new CompletionCallback(OnBodyComplete));
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

            if (CancellationHandler != null)
            {
                context.ScheduleActivity(CancellationHandler, onFaulted: new FaultCallback(OnExceptionFromCancelHandler));
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
        => _suppressCancel.Set(context, false);
}
