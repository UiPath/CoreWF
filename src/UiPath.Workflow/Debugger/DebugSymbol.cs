// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Xaml;

namespace System.Activities.Debugger.Symbol;

[Fx.Tag.XamlVisible(false)]
public static class DebugSymbol
{
    static Type attachingTypeName = typeof(DebugSymbol);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier SymbolName = new AttachableMemberIdentifier(attachingTypeName, "Symbol");


    [Fx.Tag.InheritThrows(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetSymbol(object instance, object value)
    {
        AttachablePropertyServices.SetProperty(instance, SymbolName, value);
    }

    [Fx.Tag.InheritThrows(From = "TryGetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static object GetSymbol(object instance)
    {
        string value;
        if (AttachablePropertyServices.TryGetProperty(instance, SymbolName, out value))
        {
            return value;
        }
        else
        {
            return string.Empty;
        }
    }
}
