// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Transactions;

namespace System.Activities;
using Runtime;

internal class WorkflowPersistenceContext
{
    private readonly CommittableTransaction _contextOwnedTransaction;
    private readonly Transaction _clonedTransaction;

    public WorkflowPersistenceContext(bool transactionRequired, TimeSpan transactionTimeout)
        : this(transactionRequired, CloneAmbientTransaction(), transactionTimeout) { }

    public WorkflowPersistenceContext(bool transactionRequired, Transaction transactionToUse, TimeSpan transactionTimeout)
    {
        if (transactionToUse != null)
        {
            _clonedTransaction = transactionToUse;
        }
        else if (transactionRequired)
        {
            _contextOwnedTransaction = new CommittableTransaction(transactionTimeout);
            // Clone it so that we don't pass a CommittableTransaction to the participants
            _clonedTransaction = _contextOwnedTransaction.Clone();
        }
    }

    public Transaction PublicTransaction => _clonedTransaction;

    public void Abort()
    {
        if (_contextOwnedTransaction != null)
        {
            try
            {
                _contextOwnedTransaction.Rollback();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                // Swallow these exceptions as we are already on the error path
            }
        }
    }

    public void Complete() => _contextOwnedTransaction?.Commit();

    // Returns true if end needs to be called
    // Note: this is side effecting even if it returns false
    public bool TryBeginComplete(AsyncCallback callback, object state, out IAsyncResult result)
    {
        // In the interest of allocating less objects we don't implement
        // the full async pattern here.  Instead, we've flattened it to
        // do the sync part and then optionally delegate down to the inner
        // BeginCommit.            

        if (_contextOwnedTransaction != null)
        {
            result = _contextOwnedTransaction.BeginCommit(callback, state);
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    public void EndComplete(IAsyncResult result)
    {
        Fx.Assert(_contextOwnedTransaction != null, "We must have a contextOwnedTransaction if we are calling End");

        _contextOwnedTransaction.EndCommit(result);
    }

    // We might as well clone the ambient transaction so that PersistenceParticipants
    // can't cast to a CommittableTransaction.
    private static Transaction CloneAmbientTransaction()
    {
        Transaction ambientTransaction = Transaction.Current;
        return ambientTransaction?.Clone();
    }
}
