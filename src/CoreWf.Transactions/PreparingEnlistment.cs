// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions
{
    public class PreparingEnlistment : Enlistment
    {
        internal PreparingEnlistment(
            InternalEnlistment enlistment
            ) : base(enlistment)
        {
        }

        public void Prepared()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentPrepared(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.Prepared(_internalEnlistment);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        public void ForceRollback()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentForceRollback(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, null);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        public void ForceRollback(Exception e)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentForceRollback(_internalEnlistment);
            }
 
            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.ForceRollback(_internalEnlistment, e);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        public byte[] RecoveryInformation()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            try
            {
                lock (_internalEnlistment.SyncRoot)
                {
                    return _internalEnlistment.State.RecoveryInformation(_internalEnlistment);
                }
            }
            finally
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
                }
            }
        }
    }
}
