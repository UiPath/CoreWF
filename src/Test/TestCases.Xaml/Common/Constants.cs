// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace TestCases.Xaml.Common
{
    public static class Constants
    {
        public const string Namespace2006 = "http://schemas.microsoft.com/winfx/2006/xaml";
        // Map 2008 namespace to 2006 namespace until 2009 namespace is available //
        // public const string NamespaceV2 = "http://schemas.microsoft.com/netfx/2008/xaml";
        public const string NamespaceV2 = Namespace2006;
        // Map 2008 namespace to 2006 namespace until 2009 namespace is available //
        //public const string NamespaceBuiltinTypes = "http://schemas.microsoft.com/netfx/2008/xaml/schema";
        public const string NamespaceBuiltinTypes = Namespace2006;
        public static readonly XName Directive2006Type = XName.Get("Directive", Namespace2006);
        public static readonly XName DirectiveV2Type = XName.Get("Directive", NamespaceV2);
        public static readonly XName Null = XName.Get("Null", Namespace2006);
        public static readonly XName Reference = XName.Get("Reference", Namespace2006);

        public static XName GetXNameFromType(Type type)
        {
            string namespaceFormat =
                "clr-namespace:{0};assembly={1}";

            string typeName = type.Name;
            //strip off the generic stuff (ie GenericTypeName`2)
            if (typeName.Contains("`"))
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }
            XName name = XName.Get(typeName,
                string.Format(namespaceFormat, type.Namespace, type.Assembly.GetName().Name));
            return name;

        }
    }
}
