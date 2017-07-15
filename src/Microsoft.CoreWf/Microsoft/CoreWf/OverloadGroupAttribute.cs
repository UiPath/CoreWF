// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefineAccessorsForAttributeArguments,
    //Justification = "The setter is needed to enable XAML serialization of the attribute object.")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class OverloadGroupAttribute : Attribute
    {
        private string _groupName;

        public OverloadGroupAttribute()
        {
        }

        public OverloadGroupAttribute(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("groupName");
            }

            _groupName = groupName;
        }

        public string GroupName
        {
            get
            {
                return _groupName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("value");
                }
                _groupName = value;
            }
        }

        //public override object TypeId
        //{
        //    get
        //    {
        //        return this;
        //    }
        //}
    }
}
