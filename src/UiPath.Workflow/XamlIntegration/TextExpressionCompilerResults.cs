// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TextExpressionCompilerResults
    {
        private readonly List<TextExpressionCompilerError> _messages = new List<TextExpressionCompilerError>();
        public Type ResultType { get; set; }
        public bool HasErrors => _messages.Any(m => !m.IsWarning);
        public IReadOnlyCollection<TextExpressionCompilerError> CompilerMessages => _messages;
        public void AddMessages(IEnumerable<TextExpressionCompilerError> messages) => _messages.AddRange(messages);
        public override string ToString() => string.Join("\n", _messages);
    }
}