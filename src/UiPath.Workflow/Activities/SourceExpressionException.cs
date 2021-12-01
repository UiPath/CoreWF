// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using System.Activities.XamlIntegration;
using System.Linq;

namespace System.Activities.ExpressionParser;

[Serializable]
public class SourceExpressionException : Exception, ISerializable
{
    TextExpressionCompilerError[] errors;

    public SourceExpressionException()
        : base(SR.CompilerError)
    {
    }

    public SourceExpressionException(string message)
        : base(message)
    {
    }

    public SourceExpressionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SourceExpressionException(string message, IReadOnlyCollection<TextExpressionCompilerError> errors)
        : base(message)
    {
        this.errors = errors.ToArray();
    }

    protected SourceExpressionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        if (info == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(info));
        }
        int length = info.GetInt32("count");
        errors = new TextExpressionCompilerError[length];
        for (int i = 0; i < length; ++i)
        {
            var error = new TextExpressionCompilerError();
            string index = i.ToString(CultureInfo.InvariantCulture);
            error.SourceLineNumber = info.GetInt32("line" + index);
            error.Number = info.GetString("number" + index);
            error.Message = info.GetString("text" + index);
            errors[i] = error;
        }
    }

    public IEnumerable<TextExpressionCompilerError> Errors
    {
        get
        {
            return errors ?? (errors = new TextExpressionCompilerError[0]);
        }
    }

    [Fx.Tag.SecurityNote(Critical = "Critical because we are overriding a critical method in the base class.")]
    [SecurityCritical]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(info));
        }
        if (this.errors == null)
        {
            info.AddValue("count", 0);
        }
        else
        {
            info.AddValue("count", this.errors.Length);
            for (int i = 0; i < this.errors.Length; ++i)
            {
                var error = this.errors[i];
                string index = i.ToString(CultureInfo.InvariantCulture);
                info.AddValue("line" + index, error.SourceLineNumber);
                info.AddValue("number" + index, error.Number);
                info.AddValue("text" + index, error.Message);
            }
        }
        base.GetObjectData(info, context);
    }
}
