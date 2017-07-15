// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequiredArgumentAttribute : Attribute
    {
        public RequiredArgumentAttribute()
            : base()
        {
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
