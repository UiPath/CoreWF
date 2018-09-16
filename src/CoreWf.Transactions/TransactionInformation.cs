// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Transactions
{
    public class TransactionInformation
    {
        private InternalTransaction _internalTransaction;

        internal TransactionInformation(InternalTransaction internalTransaction)
        {
            _internalTransaction = internalTransaction;
        }

        public string LocalIdentifier
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }
 
                try
                {
                    return _internalTransaction.TransactionTraceId.TransactionIdentifier;
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


        public Guid DistributedIdentifier
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }

                try
                {
                    // syncronize to avoid potential race between accessing the DistributerIdentifier
                    // and getting the transaction information entry populated...

                    lock (_internalTransaction)
                    {
                        return _internalTransaction.State.get_Identifier(_internalTransaction);
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


        public DateTime CreationTime => new DateTime(_internalTransaction.CreationTime);

        public TransactionStatus Status
        {
            get
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                }

                try
                {
                    return _internalTransaction.State.get_Status(_internalTransaction);
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
}

