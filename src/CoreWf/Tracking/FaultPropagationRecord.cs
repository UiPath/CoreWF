// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class FaultPropagationRecord : TrackingRecord
{
    private ActivityInfo _faultSource;
    private ActivityInfo _faultHandler;
    private bool _isFaultSource;
    private Exception _fault;

    internal FaultPropagationRecord(Guid instanceId, ActivityInstance source, ActivityInstance faultHandler, bool isFaultSource, Exception fault)
        : base(instanceId)
    {
        Fx.Assert(source != null, "Fault source cannot be null");
        FaultSource = new ActivityInfo(source);

        if (faultHandler != null)
        {
            FaultHandler = new ActivityInfo(faultHandler);
        }
        IsFaultSource = isFaultSource;
        Fault = fault;
        Level = EventLevel.Warning;
    }

    //parameter faultHandler is null if there are no handlers
    public FaultPropagationRecord(
            Guid instanceId,
            long recordNumber,
            ActivityInfo faultSource,
            ActivityInfo faultHandler,
            bool isFaultSource,
            Exception fault)
        : base(instanceId, recordNumber)
    {
        FaultSource = faultSource ?? throw System.Activities.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(faultSource));
        FaultHandler = faultHandler;
        IsFaultSource = isFaultSource;
        Fault = fault;
        Level = EventLevel.Warning;
    }

    private FaultPropagationRecord(FaultPropagationRecord record)
        : base(record)
    {
        FaultSource = record.FaultSource;
        FaultHandler = record.FaultHandler;
        Fault = record.Fault;
        IsFaultSource = record.IsFaultSource;
    }

    public ActivityInfo FaultSource
    {
        get => _faultSource;
        private set => _faultSource = value;
    }

    public ActivityInfo FaultHandler
    {
        get => _faultHandler;
        private set => _faultHandler = value;
    }

    public bool IsFaultSource
    {
        get => _isFaultSource;
        private set => _isFaultSource = value;
    }

    public Exception Fault
    {
        get => _fault;
        private set => _fault = value;
    }

    [DataMember(Name = "FaultSource")]
    internal ActivityInfo SerializedFaultSource
    {
        get => FaultSource;
        set => FaultSource = value;
    }

    [DataMember(Name = "FaultHandler")]
    internal ActivityInfo SerializedFaultHandler
    {
        get => FaultHandler;
        set => FaultHandler = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "IsFaultSource")]
    internal bool SerializedIsFaultSource
    {
        get => IsFaultSource;
        set => IsFaultSource = value;
    }

    [DataMember(Name = "Fault")]
    internal Exception SerializedFault
    {
        get => Fault;
        set => Fault = value;
    }

    protected internal override TrackingRecord Clone() => new FaultPropagationRecord(this);

    public override string ToString()
        => string.Format(CultureInfo.CurrentCulture,
            "FaultPropagationRecord {{ {0}, FaultSource {{ {1} }}, FaultHandler {{ {2} }}, IsFaultSource = {3} }}",
            base.ToString(),
            FaultSource.ToString(),
            FaultHandler != null ? FaultHandler.ToString() : "<null>",
            IsFaultSource);
}
