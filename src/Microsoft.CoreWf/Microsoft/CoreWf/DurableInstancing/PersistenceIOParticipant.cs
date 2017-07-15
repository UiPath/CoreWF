// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CoreWf.Persistence
{
    public abstract class PersistenceIOParticipant : PersistenceParticipant
    {
        protected PersistenceIOParticipant(bool isSaveTransactionRequired, bool isLoadTransactionRequired)
            : base(isSaveTransactionRequired, isLoadTransactionRequired)
        {
        }

        // Passed-in dictionaries are read-only.
        [Fx.Tag.Throws.Timeout("The operation could not be completed before the timeout.  The transaction should be rolled back and the pipeline aborted.")]
        [Fx.Tag.Throws(typeof(OperationCanceledException), "The operation has been aborted.  The transaction should be rolled back and the pipeline aborted.")]
        //[Fx.Tag.Throws(typeof(TransactionException), "The transaction associated with the operation has failed.  The pipeline should be aborted.")]
        protected virtual IAsyncResult BeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        [Fx.Tag.InheritThrows(From = "BeginOnSave")]
        protected virtual void EndOnSave(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        // Passed-in dictionary is read-only.
        [Fx.Tag.InheritThrows(From = "BeginOnSave")]
        protected virtual IAsyncResult BeginOnLoad(IDictionary<XName, object> readWriteValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new CompletedAsyncResult(callback, state);
        }

        [Fx.Tag.InheritThrows(From = "BeginOnLoad")]
        protected virtual void EndOnLoad(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected abstract void Abort();

        internal override IAsyncResult InternalBeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginOnSave(readWriteValues, writeOnlyValues, timeout, callback, state);
        }

        internal override void InternalEndOnSave(IAsyncResult result)
        {
            EndOnSave(result);
        }

        internal override IAsyncResult InternalBeginOnLoad(IDictionary<XName, object> readWriteValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginOnLoad(readWriteValues, timeout, callback, state);
        }

        internal override void InternalEndOnLoad(IAsyncResult result)
        {
            EndOnLoad(result);
        }

        internal override void InternalAbort()
        {
            Abort();
        }
    }
}
