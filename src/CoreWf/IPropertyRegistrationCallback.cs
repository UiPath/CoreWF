// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Runtime;
    using System;

    public interface IPropertyRegistrationCallback
    {
        [Fx.Tag.Throws(typeof(Exception), "Extensibility point.")]
        void Register(RegistrationContext context);
        [Fx.Tag.Throws(typeof(Exception), "Extensibility point.")]
        void Unregister(RegistrationContext context);
    }
}


