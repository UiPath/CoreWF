// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Xaml;

namespace System.Activities.Debugger.Symbol;

[Fx.Tag.XamlVisibleAttribute(false)]
public static class DebugSymbol
{
    private static readonly Type s_attachingTypeName = typeof(DebugSymbol);

    //[SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotDeclareReadOnlyMutableReferenceTypes)]
    public static readonly AttachableMemberIdentifier SymbolName = new(s_attachingTypeName, "Symbol");
    
    [Fx.Tag.InheritThrowsAttribute(From = "SetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static void SetSymbol(object instance, object value) => 
        AttachablePropertyServices.SetProperty(instance, SymbolName, value);

    [Fx.Tag.InheritThrowsAttribute(From = "TryGetProperty", FromDeclaringType = typeof(AttachablePropertyServices))]
    public static object GetSymbol(object instance) => 
        AttachablePropertyServices.TryGetProperty(instance, SymbolName, out string value) ? value : string.Empty;
}
