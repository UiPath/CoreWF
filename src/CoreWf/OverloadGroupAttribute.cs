// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Internals;
    using System;

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefineAccessorsForAttributeArguments,
    //Justification = "The setter is needed to enable XAML serialization of the attribute object.")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class OverloadGroupAttribute : Attribute
    {
        private string groupName;

        public OverloadGroupAttribute()
        {
        }

        public OverloadGroupAttribute(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(groupName));
            }

            this.groupName = groupName;
        }

        public string GroupName
        {
            get 
            { 
                return this.groupName; 
            }

            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(value));
                }
                this.groupName = value;
            }
        }

        public override object TypeId
        {
            get
            {
                return this;
            }
        }
    }
}
