// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System;
    using CoreWf;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Portable.Xaml.Markup;
    using CoreWf.Internals;
    using CoreWf.Runtime;

    [ContentProperty("Duration")]
    public sealed class Delay : NativeActivity
    {
        private static readonly Func<TimerExtension> getDefaultTimerExtension = new Func<TimerExtension>(GetDefaultTimerExtension);
        private readonly Variable<Bookmark> timerBookmark;

        public Delay()
            : base()
        {
            this.timerBookmark = new Variable<Bookmark>();
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TimeSpan> Duration
        {
            get;
            set;
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument durationArgument = new RuntimeArgument("Duration", typeof(TimeSpan), ArgumentDirection.In, true);
            metadata.Bind(this.Duration, durationArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { durationArgument });
            metadata.AddImplementationVariable(this.timerBookmark);
            metadata.AddDefaultExtensionProvider(getDefaultTimerExtension);
        }

        private static TimerExtension GetDefaultTimerExtension()
        {
            return new DurableTimerExtension();
        }

        protected override void Execute(NativeActivityContext context)
        {
            TimeSpan duration = this.Duration.Get(context);
            if (duration < TimeSpan.Zero)
            {
                throw FxTrace.Exception.ArgumentOutOfRange("Duration", duration, SR.DurationIsNegative(this.DisplayName));
            }

            if (duration == TimeSpan.Zero)
            {
                return; 
            }
                        
            TimerExtension timerExtension = GetTimerExtension(context);
            Bookmark bookmark = context.CreateBookmark();
            timerExtension.RegisterTimer(duration, bookmark);
            this.timerBookmark.Set(context, bookmark);
        }

        protected override void Cancel(NativeActivityContext context)
        {
            Bookmark timerBookmark = this.timerBookmark.Get(context);
            TimerExtension timerExtension = GetTimerExtension(context);
            timerExtension.CancelTimer(timerBookmark);
            context.RemoveBookmark(timerBookmark);
            context.MarkCanceled();
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            Bookmark timerBookmark = this.timerBookmark.Get(context);
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
    }
}
