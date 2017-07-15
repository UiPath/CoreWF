// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace CoreWf.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class InstanceValue
    {
        private readonly static InstanceValue s_deletedValue = new InstanceValue();

        public InstanceValue(object value)
            : this(value, InstanceValueOptions.None)
        {
        }

        public InstanceValue(object value, InstanceValueOptions options)
        {
            Value = value;
            Options = options;
        }

        private InstanceValue()
        {
            Value = this;
        }

        public object Value { get; private set; }

        public InstanceValueOptions Options { get; private set; }

        public bool IsDeletedValue
        {
            get
            {
                return object.ReferenceEquals(this, InstanceValue.DeletedValue);
            }
        }

        public static InstanceValue DeletedValue
        {
            get
            {
                return InstanceValue.s_deletedValue;
            }
        }

        [DataMember(Name = "Value", EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal object SerializedValue
        {
            get
            {
                return this.Value;
            }
            set
            {
                this.Value = value;
            }
        }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal InstanceValueOptions SerializedOptions
        {
            get
            {
                return this.Options;
            }
            set
            {
                this.Options = value;
            }
        }
    }
}
