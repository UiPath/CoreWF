// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf
{
    public class DynamicActivityProperty
    {
        private Collection<Attribute> _attributes;

        public DynamicActivityProperty()
        {
        }

        public Collection<Attribute> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new Collection<Attribute>();
                }
                return _attributes;
            }
        }

        [DefaultValue(null)]
        public string Name
        {
            get;
            set;
        }

        [DefaultValue(null)]
        //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.PropertyNamesShouldNotMatchGetMethods, 
        //Justification = "Workflow normalizes on Type for Type properties")]
        public Type Type
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public object Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            if (Type != null && Name != null)
            {
                return "Property: " + Type.ToString() + " " + Name;
            }
            return string.Empty;
        }
    }
}
