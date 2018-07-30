// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Tracking
{
    using CoreWf.Runtime;
    using System;

    public abstract class TrackingParticipant
    {
        protected TrackingParticipant()
        {
        }

        public virtual TrackingProfile TrackingProfile
        {
            get;
            set;
        }

        [Fx.Tag.InheritThrows(From = "Track", FromDeclaringType = typeof(TrackingParticipant))]
        protected internal virtual IAsyncResult BeginTrack(TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new TrackAsyncResult(this, record, timeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "Track", FromDeclaringType = typeof(TrackingParticipant))]
        protected internal virtual void EndTrack(IAsyncResult result)
        {
            TrackAsyncResult.End(result);
        }

        [Fx.Tag.Throws(typeof(Exception), "extensibility point")]
        [Fx.Tag.Throws.Timeout("Tracking data could not be saved before the timeout")]
        protected internal abstract void Track(TrackingRecord record, TimeSpan timeout);

        private class TrackAsyncResult : AsyncResult
        {
            private static Action<object> asyncExecuteTrack = new Action<object>(ExecuteTrack);
            private readonly TrackingParticipant participant;
            private readonly TrackingRecord record;
            private readonly TimeSpan timeout;

            public TrackAsyncResult(TrackingParticipant participant, TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.participant = participant;
                this.record = record;
                this.timeout = timeout;
                ActionItem.Schedule(asyncExecuteTrack, this);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<TrackAsyncResult>(result);
            }

            private static void ExecuteTrack(object state)
            {
                TrackAsyncResult thisPtr = (TrackAsyncResult)state;
                thisPtr.TrackCore();
            }

            private void TrackCore()
            {
                Exception participantException = null;
                try
                {
                    this.participant.Track(this.record, this.timeout);
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    participantException = exception;
                }
                base.Complete(false, participantException);
            }
        }

    }
}
