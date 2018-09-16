// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Hosting
{
    using System.Runtime.Serialization;
    using CoreWf.Runtime;

    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class LocationInfo
    {
        private string name;
        private string ownerDisplayName;
        private object value;

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
                return this.name;
            }
            private set
            {
                this.name = value;
            }
        }
        
        public string OwnerDisplayName
        {
            get
            {
                return this.ownerDisplayName;
            }
            private set
            {
                this.ownerDisplayName = value;
            }
        }
        
        public object Value
        {
            get
            {
                return this.value;
            }
            private set
            {
                this.value = value;
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
