// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Hosting
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class LocationInfo
    {
        private string _name;
        private string _ownerDisplayName;
        private object _value;

        internal LocationInfo(string name, string ownerDisplayName, object value)
        {
            this.Name = name;
            this.OwnerDisplayName = ownerDisplayName;
            this.Value = value;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
            }
        }

        public string OwnerDisplayName
        {
            get
            {
                return _ownerDisplayName;
            }
            private set
            {
                _ownerDisplayName = value;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;
            }
        }

        [DataMember(Name = "Name")]
        internal string SerializedName
        {
            get { return this.Name; }
            set { this.Name = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "OwnerDisplayName")]
        internal string SerializedOwnerDisplayName
        {
            get { return this.OwnerDisplayName; }
            set { this.OwnerDisplayName = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "Value")]
        internal object SerializedValue
        {
            get { return this.Value; }
            set { this.Value = value; }
        }
    }
}
