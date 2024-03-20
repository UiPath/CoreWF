// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.ParallelTracking;
using System.Collections.ObjectModel;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class ParallelForEach<T> : NativeActivity
{
    private Variable<bool> _hasCompleted;
    private CompletionCallback<bool> _onConditionComplete;

    public ParallelForEach()
        : base() { }

    [DefaultValue(null)]
    public ActivityAction<T> Body { get; set; }

    [DefaultValue(null)]
    public Activity<bool> CompletionCondition { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<IEnumerable<T>> Values { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument valuesArgument = new("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true);
        metadata.Bind(Values, valuesArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { valuesArgument });

        // declare the CompletionCondition as a child
        if (CompletionCondition != null)
        {
            metadata.SetChildrenCollection(new Collection<Activity> { CompletionCondition });
        }

        // declare the hasCompleted variable
        if (CompletionCondition != null)
        {
            _hasCompleted ??= new Variable<bool>("hasCompletedVar");
            metadata.AddImplementationVariable(_hasCompleted);
        }

        metadata.AddDelegate(Body);
    }

    protected override void Execute(NativeActivityContext context)
    {
        IEnumerable<T> values = Values.Get(context);
        if (values == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ParallelForEachRequiresNonNullValues(DisplayName)));
        }

        IEnumerator<T> valueEnumerator = values.GetEnumerator();

        CompletionCallback onBodyComplete = new(OnBodyComplete);
        while (valueEnumerator.MoveNext())
        {
            if (Body != null)
            {
                context.ScheduleAction(Body, valueEnumerator.Current, onBodyComplete).MarkNewParallelBranch();
            }
        }
        valueEnumerator.Dispose();
    }

    private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
    {
        // for the completion condition, we handle cancelation ourselves
        if (CompletionCondition != null && !_hasCompleted.Get(context))
        {
            if (completedInstance.State != ActivityInstanceState.Closed && context.IsCancellationRequested)
            {
                // If we hadn't completed before getting canceled
                // or one of our iteration of body cancels then we'll consider
                // ourself canceled.
                context.MarkCanceled();
                _hasCompleted.Set(context, true);
            }
            else
            {
                _onConditionComplete ??= new CompletionCallback<bool>(OnConditionComplete);
                context.ScheduleActivity(CompletionCondition, _onConditionComplete);
            }
        }
    }

    private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        if (result)
        {
            context.CancelChildren();
            _hasCompleted.Set(context, true);
        }
    }
}
