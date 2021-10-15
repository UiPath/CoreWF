// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace System.Activities.XamlIntegration
{
    using System;

    [Serializable]
    public class TextExpressionCompilerError
    {
        internal TextExpressionCompilerError()
        {
        }

        public TextExpressionCompilerError(Diagnostic diagnostic)
        {
            SourceLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line;
            Number = diagnostic.Id;
            Message = diagnostic.ToString();
            IsWarning = diagnostic.Severity < DiagnosticSeverity.Error;
        }

        public bool IsWarning
        {
            get;
            internal set;
        }

        public int SourceLineNumber
        {
            get;
            internal set;
        }
        
        public string Message
        {
            get;
            internal set;
        }
        
        public string Number
        {
            get;
            internal set;
        }
        public override string ToString() => Message;
    }
}
