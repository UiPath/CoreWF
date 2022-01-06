// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Transactions;

namespace System.Activities;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class NativeActivityTransactionContext : NativeActivityContext
{
    private readonly ActivityExecutor _executor;
    private readonly RuntimeTransactionHandle _transactionHandle;

    internal NativeActivityTransactionContext(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarks, RuntimeTransactionHandle handle)
        : base(instance, executor, bookmarks)
    {
        _executor = executor;
        _transactionHandle = handle;
    }

    public void SetRuntimeTransaction(Transaction transaction)
    {
        ThrowIfDisposed();

        if (transaction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(transaction));
        }

        _executor.SetTransaction(_transactionHandle, transaction, _transactionHandle.Owner, CurrentInstance);
    }
}
