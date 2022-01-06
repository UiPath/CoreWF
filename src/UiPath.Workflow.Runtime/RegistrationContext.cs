// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public sealed class RegistrationContext
{
    private readonly ExecutionPropertyManager _properties;
    private readonly IdSpace _currentIdSpace;

    internal RegistrationContext(ExecutionPropertyManager properties, IdSpace currentIdSpace)
    {
        _properties = properties;
        _currentIdSpace = currentIdSpace;
    }

    public object FindProperty(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        return _properties?.GetProperty(name, _currentIdSpace);
    }
}
