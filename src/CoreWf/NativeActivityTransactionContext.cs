// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Runtime;
    using CoreWf.Transactions;
    using CoreWf.Internals;

    [Fx.Tag.XamlVisible(false)]
    public sealed class NativeActivityTransactionContext : NativeActivityContext
    {
        private readonly ActivityExecutor executor;
        private RuntimeTransactionHandle transactionHandle;

        internal NativeActivityTransactionContext(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarks, RuntimeTransactionHandle handle)
            : base(instance, executor, bookmarks)
        {
            this.executor = executor;
            this.transactionHandle = handle;
        }

        public void SetRuntimeTransaction(Transaction transaction)
        {
            ThrowIfDisposed();

            if (transaction == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(transaction));
            }

            this.executor.SetTransaction(this.transactionHandle, transaction, transactionHandle.Owner, this.CurrentInstance);
        }
    }
}
