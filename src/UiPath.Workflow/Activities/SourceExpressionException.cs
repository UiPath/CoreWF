// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace System.Activities.ExpressionParser;

[Serializable]
public class SourceExpressionException : Exception, ISerializable
{
    private TextExpressionCompilerError[] _errors;

    public SourceExpressionException()
        : base(SR.CompilerError) { }

    public SourceExpressionException(string message)
        : base(message) { }

    public SourceExpressionException(string message, Exception innerException)
        : base(message, innerException) { }

    public SourceExpressionException(string message, IReadOnlyCollection<TextExpressionCompilerError> errors)
        : base(message)
    {
        _errors = errors.ToArray();
    }

    protected SourceExpressionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        if (info == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(info));
        }

        var length = info.GetInt32("count");
        _errors = new TextExpressionCompilerError[length];
        for (var i = 0; i < length; ++i)
        {
            var error = new TextExpressionCompilerError();
            var index = i.ToString(CultureInfo.InvariantCulture);
            error.SourceLineNumber = info.GetInt32("line" + index);
            error.Number = info.GetString("number" + index);
            error.Message = info.GetString("text" + index);
            _errors[i] = error;
        }
    }

    public IEnumerable<TextExpressionCompilerError> Errors => _errors ??= Array.Empty<TextExpressionCompilerError>();

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(info));
        }

        if (_errors == null)
        {
            info.AddValue("count", 0);
        }
        else
        {
            info.AddValue("count", _errors.Length);
            for (var i = 0; i < _errors.Length; ++i)
            {
                var error = _errors[i];
                var index = i.ToString(CultureInfo.InvariantCulture);
                info.AddValue("line" + index, error.SourceLineNumber);
                info.AddValue("number" + index, error.Number);
                info.AddValue("text" + index, error.Message);
            }
        }

        base.GetObjectData(info, context);
    }
}
