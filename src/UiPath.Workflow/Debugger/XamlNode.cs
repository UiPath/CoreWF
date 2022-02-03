// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xaml;

namespace System.Activities.Debugger;

internal class XamlNode
{
    public XamlMember Member { get; set; }

    public NamespaceDeclaration Namespace { get; set; }

    public XamlNodeType NodeType { get; set; }

    public XamlType Type { get; set; }

    public object Value { get; set; }

    public int LineNumber { get; set; }

    public int LinePosition { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"{LineNumber} {LinePosition} ");
        switch (NodeType)
        {
            case XamlNodeType.StartObject:
                sb.Append($"SO {Type}");
                break;
            case XamlNodeType.GetObject:
                sb.Append($"GO {Type}");
                break;
            case XamlNodeType.EndObject:
                sb.Append("EO ");
                break;
            case XamlNodeType.StartMember:
                sb.Append($"SM {Member}");
                break;
            case XamlNodeType.EndMember:
                sb.Append("EM ");
                break;
            case XamlNodeType.Value:
                sb.Append($"VA {Value}");
                break;
            case XamlNodeType.NamespaceDeclaration:
                sb.Append($"NS {Namespace}");
                break;
        }

        return sb.ToString();
    }
}
