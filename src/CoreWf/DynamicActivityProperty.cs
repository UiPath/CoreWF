// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;

public class DynamicActivityProperty
{
    private Collection<Attribute> _attributes;

    public DynamicActivityProperty() { }

    public Collection<Attribute> Attributes
    {
        get
        {
            _attributes ??= new Collection<Attribute>();
            return _attributes;
        }
    }

    [DefaultValue(null)]
    public string Name { get; set; }

    [DefaultValue(null)]
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.PropertyNamesShouldNotMatchGetMethods, 
    //    Justification = "Workflow normalizes on Type for Type properties")]
    public Type Type { get; set; }

    [DefaultValue(null)]
    public object Value { get; set; }

    public override string ToString()
    {
        if (Type != null && Name != null)
        {
            return "Property: " + Type.ToString() + " " + Name;
        }
        return string.Empty;
    }
}
