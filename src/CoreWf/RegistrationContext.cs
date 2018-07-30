// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Runtime;
    using CoreWf.Internals;

    [Fx.Tag.XamlVisible(false)]
    public sealed class RegistrationContext
    {
        private readonly ExecutionPropertyManager properties;
        private readonly IdSpace currentIdSpace;

        internal RegistrationContext(ExecutionPropertyManager properties, IdSpace currentIdSpace)
        {
            this.properties = properties;
            this.currentIdSpace = currentIdSpace;
        }

        public object FindProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
            }

            if (this.properties == null)
            {
                return null;
            }
            else
            {
                return this.properties.GetProperty(name, this.currentIdSpace);
            }
        }
    }
}


