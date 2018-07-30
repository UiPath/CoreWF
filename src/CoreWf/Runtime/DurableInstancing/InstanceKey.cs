// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public class InstanceKey
    {
        private static IDictionary<XName, InstanceValue> s_emptyMetadata = new ReadOnlyDictionary<XName, InstanceValue>(new Dictionary<XName, InstanceValue>(0));
        private static InstanceKey s_invalidKey = new InstanceKey();

        private bool _invalid; // Comparisons to Guid.Empty are too slow.
        private IDictionary<XName, InstanceValue> _metadata;

        private InstanceKey()
        {
            this.Value = Guid.Empty;
            _invalid = true;
        }

        public InstanceKey(Guid value)
            : this(value, null)
        {
        }

        public InstanceKey(Guid value, IDictionary<XName, InstanceValue> metadata)
        {
            if (value == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(value), SR.InstanceKeyRequiresValidGuid);
            }

            this.Value = value;
            if (metadata != null)
            {
                if (metadata.IsReadOnly)
                {
                    this.Metadata = metadata;
                }
                else
                {
                    Dictionary<XName, InstanceValue> copy = new Dictionary<XName, InstanceValue>(metadata);
                    this.Metadata = new ReadOnlyDictionary<XName, InstanceValue>(copy);
                }
            }
            else
            {
                this.Metadata = s_emptyMetadata;
            }
        }

        public bool IsValid
        {
            get
            {
                return !_invalid;
            }
        }

        public Guid Value
        {
            get;
            private set;
        }

        public IDictionary<XName, InstanceValue> Metadata
        {
            get
            {
                // This can be true if the object was deserialized.
                if (_metadata == null)
                {
                    _metadata = s_emptyMetadata;
                }
                return _metadata;
            }
            private set
            {
                _metadata = value;
            }
        }

        public static InstanceKey InvalidKey
        {
            get
            {
                return InstanceKey.s_invalidKey;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Value.Equals(((InstanceKey)obj).Value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        [DataMember(Name = "Value")]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal Guid SerializedValue
        {
            get
            {
                return this.Value;
            }
            set
            {
                if (value.CompareTo(Guid.Empty) == 0)
                {
                    _invalid = true;
                }
                else
                {
                    this.Value = value;
                }
            }
        }

        [DataMember(Name = "Metadata", EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal IDictionary<XName, InstanceValue> SerializedMetadata
        {
            get
            {
                if (this.Metadata.Count == 0)
                {
                    return null;
                }
                else
                {
                    return this.Metadata;
                }
            }
            set
            {
                Fx.Assert(value != null, "A null value should not have been serialized because EmitDefaultValue is false");
                this.Metadata = value;
            }
        }
    }
}
