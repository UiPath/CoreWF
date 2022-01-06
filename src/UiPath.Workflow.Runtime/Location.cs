// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Activities;
using Runtime;

[DataContract]
[DebuggerDisplay("{Value}")]
public abstract class Location
{
    private TemporaryResolutionData temporaryResolutionData;

    protected Location() { }

    public abstract Type LocationType { get; }

    public object Value
    {
        get => ValueCore;
        set => ValueCore = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "temporaryResolutionData")]
    internal TemporaryResolutionData SerializedTemporaryResolutionData
    {
        get => temporaryResolutionData;
        set => temporaryResolutionData = value;
    }

    internal virtual bool CanBeMapped => false;

    // When we are resolving an expression that resolves to a
    // reference to a location we need some way of notifying the
    // LocationEnvironment that it should extract the inner location
    // and throw away the outer one.  OutArgument and InOutArgument
    // create these TemporaryResolutionLocations if their expression
    // resolution goes async and LocationEnvironment gets rid of them
    // in CollapseTemporaryResolutionLocations().
    internal LocationEnvironment TemporaryResolutionEnvironment => temporaryResolutionData.TemporaryResolutionEnvironment;

    internal bool BufferGetsOnCollapse => temporaryResolutionData.BufferGetsOnCollapse;

    protected abstract object ValueCore { get; set; }

    internal void SetTemporaryResolutionData(LocationEnvironment resolutionEnvironment, bool bufferGetsOnCollapse)
    {
        temporaryResolutionData = new TemporaryResolutionData
        {
            TemporaryResolutionEnvironment = resolutionEnvironment,
            BufferGetsOnCollapse = bufferGetsOnCollapse
        };
    }

    internal virtual Location CreateReference(bool bufferGets)
    {
        if (CanBeMapped || bufferGets)
        {
            return new ReferenceLocation(this, bufferGets);
        }

        return this;
    }

    internal virtual object CreateDefaultValue()
    {
        Fx.Assert("We should only call this on Location<T>");
        return null;
    }

    [DataContract]
    internal struct TemporaryResolutionData
    {
        [DataMember(EmitDefaultValue = false)]
        public LocationEnvironment TemporaryResolutionEnvironment { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool BufferGetsOnCollapse { get; set; }
    }

    [DataContract]
    internal class ReferenceLocation : Location
    {
        private Location _innerLocation;
        private bool _bufferGets;
        private object _bufferedValue;

        public ReferenceLocation(Location innerLocation, bool bufferGets)
        {
            _innerLocation = innerLocation;
            _bufferGets = bufferGets;
        }

        public override Type LocationType => _innerLocation.LocationType;

        protected override object ValueCore
        {
            get => _bufferGets ? _bufferedValue : _innerLocation.Value;
            set
            {
                _innerLocation.Value = value;
                _bufferedValue = value;
            }
        }

        [DataMember(Name = "innerLocation")]
        internal Location SerializedInnerLocation
        {
            get => _innerLocation;
            set => _innerLocation = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "bufferGets")]
        internal bool SerializedBufferGets
        {
            get => _bufferGets;
            set => _bufferGets = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "bufferedValue")]
        internal object SerializedBufferedValue
        {
            get => _bufferedValue;
            set => _bufferedValue = value;
        }

        public override string ToString() => _bufferGets ? base.ToString() : _innerLocation.ToString();
    }
}

[DataContract]
public class Location<T> : Location
{
    private T _value;

    public Location()
        : base() { }

    public override Type LocationType => typeof(T);

    public virtual new T Value
    {
        get => _value;
        set => _value = value;
    }

    internal T TypedValue
    {
        get => Value;
        set => Value = value;
    }

    protected override sealed object ValueCore
    {
        get => Value;
        set => Value = TypeHelper.Convert<T>(value);
    }

    [DataMember(EmitDefaultValue = false, Name = "value")]
    internal T SerializedValue
    {
        get => _value;
        set => _value = value;
    }

    internal override Location CreateReference(bool bufferGets)
    {
        if (CanBeMapped || bufferGets)
        {
            return new ReferenceLocation(this, bufferGets);
        }

        return this;
    }

    internal override object CreateDefaultValue()
    {
        Fx.Assert(typeof(T).GetGenericTypeDefinition() == typeof(Location<>), "We should only be calling this with location subclasses.");

        return Activator.CreateInstance<T>();
    }

    public override string ToString() => _value != null ? _value.ToString() : "<null>";

    [DataContract]
    internal new class ReferenceLocation : Location<T>
    {
        private Location<T> _innerLocation;
        private bool _bufferGets;

        public ReferenceLocation(Location<T> innerLocation, bool bufferGets)
        {
            _innerLocation = innerLocation;
            _bufferGets = bufferGets;
        }

        public override T Value
        {
            get => _bufferGets ? _value : _innerLocation.Value;
            set
            {
                _innerLocation.Value = value;

                if (_bufferGets)
                {
                    _value = value;
                }
            }
        }

        [DataMember(Name = "innerLocation")]
        internal Location<T> SerializedInnerLocation
        {
            get => _innerLocation;
            set => _innerLocation = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "bufferGets")]
        internal bool SerializedBufferGets
        {
            get => _bufferGets;
            set => _bufferGets = value;
        }

        public override string ToString() => _bufferGets ? base.ToString() : _innerLocation.ToString();
    }
}
