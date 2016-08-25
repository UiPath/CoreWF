// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class RegistrationContext
    {
        private ExecutionPropertyManager _properties;
        private IdSpace _currentIdSpace;

        internal RegistrationContext(ExecutionPropertyManager properties, IdSpace currentIdSpace)
        {
            _properties = properties;
            _currentIdSpace = currentIdSpace;
        }

        public object FindProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
            }

            if (_properties == null)
            {
                return null;
            }
            else
            {
                return _properties.GetProperty(name, _currentIdSpace);
            }
        }
    }
}


