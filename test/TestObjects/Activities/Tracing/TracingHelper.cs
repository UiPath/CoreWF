// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Test.Common.TestObjects.Activities.Tracing
{
    internal static class TracingHelper
    {
        // Helper which flips an uncaughtexception to caught
        public static bool CatchAnException(Type type, TestActivity act)
        {
            UncaughtExceptionOutcome ueo = act.ExpectedOutcome as UncaughtExceptionOutcome;

            //if (ueo != null && type.IsAssignableFrom(ueo.ExceptionType))
            //{
            //    act.ExpectedOutcome = new CaughtExceptionOutcome(ueo.ExceptionType);
            //    return true;
            //}
            //else
            //{
            //    foreach (TestActivity child in act.GetChildren())
            //    {
            //        if (CatchAnException(type, child))
            //            return true;
            //    }
            //}
            return false;
        }
    }


    public class HandledExceptionOutcome : Outcome
    {
        public HandledExceptionOutcome()
            : base(OutcomeState.Faulted, OutcomeState.Completed)
        {
        }
    }

    public class UncaughtExceptionOutcome : Outcome
    {
        public Type ExceptionType;

        public UncaughtExceptionOutcome() :
            base(OutcomeState.Faulted, OutcomeState.None)
        {
        }

        public UncaughtExceptionOutcome(Type exceptionType) :
            this()
        {
            this.ExceptionType = exceptionType;
        }
    }

    public class CaughtExceptionOutcome : Outcome
    {
        public Type ExceptionType;

        public CaughtExceptionOutcome() :
            base(OutcomeState.Faulted, OutcomeState.Canceled)
        {
        }

        public CaughtExceptionOutcome(Type exceptionType) :
            this()
        {
            this.ExceptionType = exceptionType;
        }
    }

    // When a transaction faults, it always behaves as the exception is uncaught (even if it is actually caught) inside it's body and on itself. 
    // Once the fault leaves the TransactionScope boundary, however, behavior will revert to normal
    public class TransactionFaultedOutcome : UncaughtExceptionOutcome
    {
        // The exception to propogate after the TSA boundary (could be caught or uncaught)
        // Only TestTransactionScopeActivity needs to see this, which lives in TestObjects so we are making this internal
        internal Outcome OutsideTsaOutcome
        {
            get;
            private set;
        }

        public TransactionFaultedOutcome(bool isCaughtException)
            : base()
        {
            if (isCaughtException)
            {
                this.OutsideTsaOutcome = new CaughtExceptionOutcome();
            }
            else
            {
                this.OutsideTsaOutcome = new UncaughtExceptionOutcome();
            }

            base.DefaultPropogationState = OutcomeState.Faulted;
            base.propogatedOutcome = OutcomeState.Faulted;
        }

        public TransactionFaultedOutcome(bool isCaughtException, Type exceptionType)
            : base(exceptionType)
        {
            if (isCaughtException)
            {
                this.OutsideTsaOutcome = new CaughtExceptionOutcome(exceptionType);
            }
            else
            {
                this.OutsideTsaOutcome = new UncaughtExceptionOutcome(exceptionType);
            }

            base.DefaultPropogationState = OutcomeState.Faulted;
            base.propogatedOutcome = OutcomeState.Faulted;
        }
    }
}
