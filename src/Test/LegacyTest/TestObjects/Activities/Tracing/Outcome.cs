// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace LegacyTest.Test.Common.TestObjects.Activities.Tracing
{
    // The basic completion states
    public enum OutcomeState
    {
        Completed, Faulted, Canceled, None
    }

    // A way to create hierarchichal outcomes, which just do something else.
    public class Outcome
    {
        #region  The basic states

        public static readonly Outcome Completed = new Outcome(OutcomeState.Completed)
        {
            IsOverrideable = true
        };

        public static readonly Outcome Canceled = new Outcome(OutcomeState.Canceled);

        public static readonly Outcome Faulted = new Outcome(OutcomeState.Faulted);

        public static readonly Outcome None = new Outcome(OutcomeState.None);

        public static Outcome GetOutcome(OutcomeState state)
        {
            switch (state)
            {
                case OutcomeState.Canceled:
                    return Canceled;
                case OutcomeState.Completed:
                    return Completed;
                case OutcomeState.Faulted:
                    return Faulted;
                default:
                    return None;
            }
        }

        #endregion

        #region Globally usable states

        // All global outcomes that will be used regularly should be put here
        //   This gives us a 'directory' of the possible states, all on this class

        public static Outcome UncaughtException()
        {
            return new UncaughtExceptionOutcome();
        }

        public static Outcome UncaughtException(Type type)
        {
            return new UncaughtExceptionOutcome(type);
        }

        public static Outcome CaughtException()
        {
            return new CaughtExceptionOutcome();
        }

        public static Outcome CaughtException(Type type)
        {
            return new CaughtExceptionOutcome(type);
        }

        public static Outcome HandledException()
        {
            return new HandledExceptionOutcome();
        }

        public static Outcome TransactionFaultedOutcome(Type exceptionType, bool caughtException)
        {
            return new TransactionFaultedOutcome(caughtException, exceptionType);
        }

        public static Outcome TransactionFaultedOutcome(bool caughtException)
        {
            return new TransactionFaultedOutcome(caughtException);
        }

        #endregion

        // Flag to determine if this state should get overriden at runtime
        private bool _isOverridable = false;
        public bool IsOverrideable
        {
            get
            {
                return _isOverridable;
            }
            set
            {
                _isOverridable = value;
            }
        }

        // If any class gets this outcome and doesnt know what it is, use default state
        public OutcomeState DefaultPropogationState { get; protected set; }

        protected OutcomeState propogatedOutcome;

        public Outcome(OutcomeState ExpectedOutcome)
            : this(ExpectedOutcome, ExpectedOutcome)
        {
        }

        public Outcome(OutcomeState initialOutcome, OutcomeState propogatedOutcome)
        {
            this.DefaultPropogationState = initialOutcome;
            this.propogatedOutcome = propogatedOutcome;
        }

        public virtual Outcome Propogate()
        {
            Outcome newOut = (Outcome)this.MemberwiseClone();
            newOut.DefaultPropogationState = propogatedOutcome;
            return newOut;
        }
    }
}
