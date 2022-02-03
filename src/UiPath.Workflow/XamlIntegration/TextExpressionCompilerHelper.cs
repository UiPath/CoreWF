// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;
using System.Xml;

namespace System.Activities.XamlIntegration;

public static class TextExpressionCompilerHelper
{
    public static IReadOnlyCollection<string> GetImports(this CodeCompileUnit compilationUnit)
    {
        return compilationUnit.Namespaces[0].Imports.Cast<CodeNamespaceImport>().Select(c => c.Namespace).Distinct()
                              .ToArray();
    }

    public static string GetCode(this CodeCompileUnit compilationUnit, string language)
    {
        var codeWriter = new StringWriter();
        var typeDeclaration = compilationUnit.Namespaces[0].Types[0];
        using (var codeDomProvider = CodeDomProvider.CreateProvider(language))
        {
            codeDomProvider.GenerateCodeFromType(typeDeclaration, codeWriter, new CodeGeneratorOptions());
        }

        return codeWriter.ToString();
    }

    internal static void GetNamespacesLineInfo(string sourceXamlFileName, Dictionary<string, int> lineNumbersForNSes,
        Dictionary<string, int> lineNumbersForNSesForImpl)
    {
        // read until StartMember: TextExpression.NamespacesForImplementation OR TextExpression.Namespaces
        // create a subtree reader,
        // in the subtree, 
        // look for StartObject nodes of String type.  their values are added to either LineNumbersForNSes or LineNumbersForNSesForImpl dictionaries.
        if (!File.Exists(sourceXamlFileName))
        {
            return;
        }

        using var xmlReader = XmlReader.Create(sourceXamlFileName);
        using var xreader = new XamlXmlReader(xmlReader, new XamlXmlReaderSettings {ProvideLineInfo = true});
        var hasHitFirstStartObj = false;
        while (!hasHitFirstStartObj && xreader.Read())
        {
            if (xreader.NodeType == XamlNodeType.StartObject)
            {
                hasHitFirstStartObj = true;
            }
        }

        if (hasHitFirstStartObj)
        {
            xreader.Read();
            do
            {
                if (IsStartMemberTextExprNs(xreader))
                {
                    var subTreeReader = xreader.ReadSubtree();
                    WalkSubTree(subTreeReader, lineNumbersForNSes);
                }
                else if (IsStartMemberTextExprNsForImpl(xreader))
                {
                    var subTreeReader = xreader.ReadSubtree();
                    WalkSubTree(subTreeReader, lineNumbersForNSesForImpl);
                }
                else
                {
                    xreader.Skip();
                }
            } while (!xreader.IsEof);
        }
    }

    private static bool IsStartMemberTextExprNs(XamlXmlReader xreader)
    {
        return xreader.NodeType == XamlNodeType.StartMember && xreader.Member.DeclaringType != null &&
            xreader.Member.DeclaringType.UnderlyingType == typeof(TextExpression) &&
            xreader.Member.Name == "Namespaces";
    }

    private static bool IsStartMemberTextExprNsForImpl(XamlXmlReader xreader)
    {
        return xreader.NodeType == XamlNodeType.StartMember && xreader.Member.DeclaringType != null &&
            xreader.Member.DeclaringType.UnderlyingType == typeof(TextExpression) &&
            xreader.Member.Name == "NamespacesForImplementation";
    }

    private static bool IsNamespaceString(XamlReader subTreeReader)
    {
        return subTreeReader.NodeType == XamlNodeType.StartObject &&
            subTreeReader.Type.UnderlyingType == typeof(string);
    }

    private static void WalkSubTree(XamlReader subTreeReader, Dictionary<string, int> lineNumbersDictionary)
    {
        while (subTreeReader.Read())
        {
            if (IsNamespaceString(subTreeReader))
            {
                while (subTreeReader.NodeType != XamlNodeType.Value)
                {
                    subTreeReader.Read();
                }

                var ixamlLineInfo = (IXamlLineInfo) subTreeReader;
                var namespaceName = subTreeReader.Value as string;
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    lineNumbersDictionary[namespaceName] = ixamlLineInfo.LineNumber;
                }
            }
        }
    }
}
