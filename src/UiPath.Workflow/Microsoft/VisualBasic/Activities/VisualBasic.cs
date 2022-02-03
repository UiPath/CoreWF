// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Xaml;

namespace Microsoft.VisualBasic.Activities;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.TypeNamesShouldNotMatchNamespaces,
//    Justification = "Approved name")]
public static class VisualBasic
{
    private static readonly AttachableMemberIdentifier s_settingsPropertyId = new(typeof(VisualBasic), "Settings");

    public static void SetSettings(object target, VisualBasicSettings value)
    {
        AttachablePropertyServices.SetProperty(target, s_settingsPropertyId, value);
    }

    public static VisualBasicSettings GetSettings(object target)
    {
        return AttachablePropertyServices.TryGetProperty(target, s_settingsPropertyId, out VisualBasicSettings value) ? value : null;
    }

    public static void SetSettingsForImplementation(object target, VisualBasicSettings value)
    {
        if (value != null)
        {
            value.SuppressXamlSerialization = true;
        }

        SetSettings(target, value);
    }

    public static bool ShouldSerializeSettings(object target)
    {
        var settings = GetSettings(target);

        if (settings != null && settings.SuppressXamlSerialization && target is Activity)
        {
            return false;
        }

        return true;
    }
}
