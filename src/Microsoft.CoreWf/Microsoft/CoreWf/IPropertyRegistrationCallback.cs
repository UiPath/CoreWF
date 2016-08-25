// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;

namespace Microsoft.CoreWf
{
    public interface IPropertyRegistrationCallback
    {
        [Fx.Tag.Throws(typeof(Exception), "Extensibility point.")]
        void Register(RegistrationContext context);
        [Fx.Tag.Throws(typeof(Exception), "Extensibility point.")]
        void Unregister(RegistrationContext context);
    }
}


