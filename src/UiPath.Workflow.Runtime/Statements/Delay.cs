// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Duration")]
public sealed class Delay : NativeActivity
{
    private static readonly Func<TimerExtension> getDefaultTimerExtension = new Func<TimerExtension>(GetDefaultTimerExtension);
    private readonly Variable<Bookmark> _timerBookmark;
    private readonly Variable<NoPersistHandle> _noPersistHandle = new Variable<NoPersistHandle>();


    public Delay()
        : base()
    {
        _timerBookmark = new Variable<Bookmark>();
    }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TimeSpan> Duration { get; set; }


    protected override bool CanInduceIdle => true;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument durationArgument = new RuntimeArgument("Duration", typeof(TimeSpan), ArgumentDirection.In, true);
        metadata.Bind(Duration, durationArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { durationArgument });
        metadata.AddImplementationVariable(_timerBookmark);
        metadata.AddImplementationVariable(_noPersistHandle);
        metadata.AddDefaultExtensionProvider(getDefaultTimerExtension);
    }

    private static TimerExtension GetDefaultTimerExtension() => new DurableTimerExtension();

    protected override void Execute(NativeActivityContext context)
    {
        TimeSpan duration = Duration.Get(context);
        if (duration < TimeSpan.Zero)
        {
            throw FxTrace.Exception.ArgumentOutOfRange("Duration", duration, SR.DurationIsNegative(DisplayName));
        }

        if (duration == TimeSpan.Zero)
        {
            return;
        }

        TimerExtension timerExtension = GetTimerExtension(context);

        if (HasBlockingDelay(context))
            _noPersistHandle.Get(context).Enter(context);

        Bookmark bookmark = context.CreateBookmark();
        timerExtension.RegisterTimer(duration, bookmark);
        _timerBookmark.Set(context, bookmark);
    }

    protected override void Cancel(NativeActivityContext context)
    {
        Bookmark timerBookmark = _timerBookmark.Get(context);
        TimerExtension timerExtension = GetTimerExtension(context);
        timerExtension.CancelTimer(timerBookmark);
        context.RemoveBookmark(timerBookmark);
        context.MarkCanceled();
    }

    protected override void Abort(NativeActivityAbortContext context)
    {
        Bookmark timerBookmark = _timerBookmark.Get(context);
        // The bookmark could be null in abort when user passed in a negative delay as a duration
        if (timerBookmark != null)
        {
            TimerExtension timerExtension = GetTimerExtension(context);
            timerExtension.CancelTimer(timerBookmark);
        }
        base.Abort(context);
    }

    private TimerExtension GetTimerExtension(ActivityContext context)
    {
        TimerExtension timerExtension = context.GetExtension<TimerExtension>();
        Fx.Assert(timerExtension != null, "TimerExtension must exist.");
        return timerExtension;
    }

    private bool HasBlockingDelay(NativeActivityContext context)
    {
        return context.GetExtension<StatementsBehaviorExtension>()?.BlockingDelay == true;
    }
}
