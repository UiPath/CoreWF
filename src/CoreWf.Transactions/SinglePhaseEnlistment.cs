// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions
{
    public class SinglePhaseEnlistment : Enlistment
    {
        internal SinglePhaseEnlistment(InternalEnlistment enlistment) : base(enlistment)
        {
        }

        public void Aborted()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentAborted(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        public void Aborted(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentAborted(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Aborted(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }


        public void Committed()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentCommitted(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Committed(_internalEnlistment);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }


        public void InDoubt()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentInDoubt(_internalEnlistment);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }


        public void InDoubt(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentInDoubt(_internalEnlistment);
                }

                _internalEnlistment.State.InDoubt(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }
    }
}

