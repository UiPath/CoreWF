// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequiredArgumentAttribute : Attribute
    {
        public RequiredArgumentAttribute()
            : base()
        {
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
