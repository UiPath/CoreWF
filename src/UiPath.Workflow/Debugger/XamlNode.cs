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
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("{0} {1} ", this.LineNumber, this.LinePosition);
        switch (this.NodeType)
        {
            case XamlNodeType.StartObject:
                sb.AppendFormat("SO {0}", this.Type);
                break;
            case XamlNodeType.GetObject:
                sb.AppendFormat("GO {0}", this.Type);
                break;
            case XamlNodeType.EndObject:
                sb.Append("EO ");
                break;
            case XamlNodeType.StartMember:
                sb.AppendFormat("SM {0}", this.Member);
                break;
            case XamlNodeType.EndMember:
                sb.Append("EM ");
                break;
            case XamlNodeType.Value:
                sb.AppendFormat("VA {0}", this.Value);
                break;
            case XamlNodeType.NamespaceDeclaration:
                sb.AppendFormat("NS {0}", this.Namespace);
                break;
        }

        return sb.ToString();
    }
}
