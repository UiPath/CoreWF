// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Xml.Linq;

namespace System.Activities.Persistence;

public abstract class PersistenceParticipant : IPersistencePipelineModule
{
    private readonly bool _isSaveTransactionRequired;
    private readonly bool _isLoadTransactionRequired;
    private readonly bool _isIOParticipant;

    protected PersistenceParticipant() { }

    internal PersistenceParticipant(bool isSaveTransactionRequired, bool isLoadTransactionRequired)
    {
        _isIOParticipant = true;
        _isSaveTransactionRequired = isSaveTransactionRequired;
        _isLoadTransactionRequired = isLoadTransactionRequired;
    }
        
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters, 
    //    Justification = "arch approved design. requires the two out dictionaries to avoid complex structures")]
    protected virtual void CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
    {
        readWriteValues = null;
        writeOnlyValues = null;
    }

    // Passed-in dictionaries are read-only.
    protected virtual IDictionary<XName, object> MapValues(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues) => null;

    // Passed-in dictionary is read-only.
    protected virtual void PublishValues(IDictionary<XName, object> readWriteValues) { }

    void IPersistencePipelineModule.CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
        => CollectValues(out readWriteValues, out writeOnlyValues);

    IDictionary<XName, object> IPersistencePipelineModule.MapValues(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues)
        => MapValues(readWriteValues, writeOnlyValues);

    void IPersistencePipelineModule.PublishValues(IDictionary<XName, object> readWriteValues) => PublishValues(readWriteValues);

    bool IPersistencePipelineModule.IsIOParticipant => _isIOParticipant;

    bool IPersistencePipelineModule.IsSaveTransactionRequired => _isSaveTransactionRequired;

    bool IPersistencePipelineModule.IsLoadTransactionRequired => _isLoadTransactionRequired;

    IAsyncResult IPersistencePipelineModule.BeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        => InternalBeginOnSave(readWriteValues, writeOnlyValues, timeout, callback, state);

    void IPersistencePipelineModule.EndOnSave(IAsyncResult result) => InternalEndOnSave(result);

    IAsyncResult IPersistencePipelineModule.BeginOnLoad(IDictionary<XName, object> readWriteValues, TimeSpan timeout, AsyncCallback callback, object state)
        => InternalBeginOnLoad(readWriteValues, timeout, callback, state);

    void IPersistencePipelineModule.EndOnLoad(IAsyncResult result) => InternalEndOnLoad(result);

    void IPersistencePipelineModule.Abort() => InternalAbort();

    internal virtual IAsyncResult InternalBeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        => throw Fx.AssertAndThrow("BeginOnSave should not be called on PersistenceParticipant.");

    internal virtual void InternalEndOnSave(IAsyncResult result) => Fx.Assert("EndOnSave should not be called on PersistenceParticipant.");

    internal virtual IAsyncResult InternalBeginOnLoad(IDictionary<XName, object> readWriteValues, TimeSpan timeout, AsyncCallback callback, object state)
        => throw Fx.AssertAndThrow("BeginOnLoad should not be called on PersistenceParticipant.");

    internal virtual void InternalEndOnLoad(IAsyncResult result) => Fx.Assert("EndOnLoad should not be called on PersistenceParticipant.");

    internal virtual void InternalAbort() { }
}
