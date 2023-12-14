// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace System.Activities.XamlIntegration;

[Serializable]
public class TextExpressionCompilerError
{
    internal TextExpressionCompilerError() { }

    public bool IsWarning { get; internal set; }

    public int SourceLineNumber { get; internal set; }

    public string Message { get; internal set; }

    public string Number { get; internal set; }

    // To be used with reflection in Studio Web
    // marked as internal so it's not referenced in wrong places
    internal Diagnostic Diagnostic { get; set; }

    public override string ToString()
    {
        return Message;
    }
}
