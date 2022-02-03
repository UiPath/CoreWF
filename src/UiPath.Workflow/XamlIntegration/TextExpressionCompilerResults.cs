// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Activities.XamlIntegration;

public class TextExpressionCompilerResults
{
    private readonly List<TextExpressionCompilerError> _messages = new();
    public Type ResultType { get; set; }
    public bool HasErrors => _messages.Any(m => !m.IsWarning);
    public IReadOnlyCollection<TextExpressionCompilerError> CompilerMessages => _messages;

    public void AddMessages(IEnumerable<TextExpressionCompilerError> messages)
    {
        _messages.AddRange(messages);
    }

    public override string ToString()
    {
        return string.Join("\n", _messages.OrderBy(m => m.IsWarning));
    }
}
