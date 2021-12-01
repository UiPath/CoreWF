// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

[ContentProperty("Body")]
public sealed class ForEach<T> : NativeActivity
{
    private readonly Variable<IEnumerator<T>> _valueEnumerator;
    private CompletionCallback _onChildComplete;

    public ForEach()
        : base()
    {
        _valueEnumerator = new Variable<IEnumerator<T>>();
    }

    [DefaultValue(null)]
    public ActivityAction<T> Body { get; set; }

    [RequiredArgument]        
    [DefaultValue(null)]
    public InArgument<IEnumerable<T>> Values { get; set; }

    private CompletionCallback OnChildComplete
    {
        get
        {
            _onChildComplete ??= new CompletionCallback(GetStateAndExecute);
            return _onChildComplete;
        }
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument valuesArgument = new RuntimeArgument("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true);
        metadata.Bind(Values, valuesArgument);

        metadata.AddArgument(valuesArgument);
        metadata.AddDelegate(Body);
        metadata.AddImplementationVariable(_valueEnumerator);
    }

    protected override void Execute(NativeActivityContext context)
    {
        IEnumerable<T> values = Values.Get(context);
        if (values == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ForEachRequiresNonNullValues(DisplayName)));
        }

        IEnumerator<T> valueEnumerator = values.GetEnumerator();
        _valueEnumerator.Set(context, valueEnumerator);

        if (Body == null || Body.Handler == null)
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
        Fx.Assert(Body != null && Body.Handler != null, "Body and Body.Handler should not be null");

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

        context.ScheduleAction(Body, valueEnumerator.Current, OnChildComplete);
    }
}
