// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Hosting;

[DataContract]
[Fx.Tag.XamlVisible(false)]
public sealed class LocationInfo
{
    private string _name;
    private string _ownerDisplayName;
    private object _value;

    internal LocationInfo(string name, string ownerDisplayName, object value)
    {
        Name = name;
        OwnerDisplayName = ownerDisplayName;
        Value = value;
    }

    public string Name
    {
        get => _name;
        private set => _name = value;
    }

    public string OwnerDisplayName
    {
        get => _ownerDisplayName;
        private set => _ownerDisplayName = value;
    }

    public object Value
    {
        get => _value;
        private set => _value = value;
    }

    [DataMember(Name = "Name")]
    internal string SerializedName
    {
        get => Name;
        set => Name = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "OwnerDisplayName")]
    internal string SerializedOwnerDisplayName
    {
        get => OwnerDisplayName;
        set => OwnerDisplayName = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "Value")]
    internal object SerializedValue
    {
        get => Value;
        set => Value = value;
    }
}
