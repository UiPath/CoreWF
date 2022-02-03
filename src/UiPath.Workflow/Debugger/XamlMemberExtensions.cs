// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Xaml;
using System.Xaml.Schema;

namespace System.Activities.Debugger;

internal static class XamlMemberExtensions
{
    internal static XamlMember ReplaceXamlMemberInvoker(this XamlMember originalXamlMember,
        XamlSchemaContext schemaContext, XamlMemberInvoker newInvoker)
    {
        if (originalXamlMember.IsEvent)
        {
            if (originalXamlMember.IsAttachable)
            {
                UnitTestUtility.Assert(originalXamlMember.UnderlyingMember is MethodInfo, "Guaranteed by XamlMember.");
                return new XamlMember(originalXamlMember.Name, originalXamlMember.UnderlyingMember as MethodInfo,
                    schemaContext, newInvoker);
            }

            UnitTestUtility.Assert(originalXamlMember.UnderlyingMember is EventInfo, "Guaranteed by XamlMember.");
            return new XamlMember(originalXamlMember.UnderlyingMember as EventInfo, schemaContext, newInvoker);
        }

        if (originalXamlMember.IsDirective)
        {
            return originalXamlMember;
        }

        if (originalXamlMember.IsUnknown)
        {
            return originalXamlMember;
        }

        if (originalXamlMember.IsAttachable)
        {
            var attachablePropertyMethod = originalXamlMember.UnderlyingMember as MethodInfo;
            if (attachablePropertyMethod.ReturnType == typeof(void))
            {
                return new XamlMember(originalXamlMember.Name, null, originalXamlMember.UnderlyingMember as MethodInfo,
                    schemaContext, newInvoker);
            }

            return new XamlMember(originalXamlMember.Name, originalXamlMember.UnderlyingMember as MethodInfo, null,
                schemaContext, newInvoker);
        }

        var propertyInfo = originalXamlMember.UnderlyingMember as PropertyInfo;
        if (propertyInfo != null)
        {
            return new XamlMember(propertyInfo, schemaContext, newInvoker);
        }

        return originalXamlMember;
    }
}
