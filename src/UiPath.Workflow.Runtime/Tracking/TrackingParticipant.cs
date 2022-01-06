// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Tracking;

public abstract class TrackingParticipant
{
    protected TrackingParticipant() { }

    public virtual TrackingProfile TrackingProfile { get; set; }

    [Fx.Tag.InheritThrows(From = "Track", FromDeclaringType = typeof(TrackingParticipant))]
    protected internal virtual IAsyncResult BeginTrack(TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
        => new TrackAsyncResult(this, record, timeout, callback, state);

    [Fx.Tag.InheritThrows(From = "Track", FromDeclaringType = typeof(TrackingParticipant))]
    protected internal virtual void EndTrack(IAsyncResult result)
        => TrackAsyncResult.End(result);

    [Fx.Tag.Throws(typeof(Exception), "extensibility point")]
    [Fx.Tag.Throws.Timeout("Tracking data could not be saved before the timeout")]
    protected internal abstract void Track(TrackingRecord record, TimeSpan timeout);

    private class TrackAsyncResult : AsyncResult
    {
        private static readonly Action<object> asyncExecuteTrack = new(ExecuteTrack);
        private readonly TrackingParticipant _participant;
        private readonly TrackingRecord _record;
        private readonly TimeSpan _timeout;

        public TrackAsyncResult(TrackingParticipant participant, TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _participant = participant;
            _record = record;
            _timeout = timeout;
            ActionItem.Schedule(asyncExecuteTrack, this);
        }

        public static void End(IAsyncResult result) => End<TrackAsyncResult>(result);

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
                _participant.Track(_record, _timeout);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                participantException = exception;
            }
            Complete(false, participantException);
        }
    }
}
