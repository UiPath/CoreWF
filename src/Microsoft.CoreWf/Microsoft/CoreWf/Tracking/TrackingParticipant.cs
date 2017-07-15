// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf.Tracking
{
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
            private static Action<object> s_asyncExecuteTrack = new Action<object>(ExecuteTrack);
            private TrackingParticipant _participant;
            private TrackingRecord _record;
            private TimeSpan _timeout;

            public TrackAsyncResult(TrackingParticipant participant, TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _participant = participant;
                _record = record;
                _timeout = timeout;
                ActionItem.Schedule(s_asyncExecuteTrack, this);
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
                base.Complete(false, participantException);
            }
        }
    }
}
