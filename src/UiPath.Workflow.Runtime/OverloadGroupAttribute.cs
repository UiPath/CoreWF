// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;

//[SuppressMessage(FxCop.Category.Design, FxCop.Rule.DefineAccessorsForAttributeArguments,
//Justification = "The setter is needed to enable XAML serialization of the attribute object.")]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class OverloadGroupAttribute : Attribute
{
    private string _groupName;

    public OverloadGroupAttribute() { }

    public OverloadGroupAttribute(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(groupName));
        }

        _groupName = groupName;
    }

    public string GroupName
    {
        get => _groupName;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(value));
            }
            _groupName = value;
        }
    }

    public override object TypeId => this;
}
