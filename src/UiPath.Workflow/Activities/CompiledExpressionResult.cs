using Microsoft.VisualBasic.Activities;
using System.Activities.ExpressionParser;

namespace System.Activities
{
    public record CompiledExpressionResult(Activity Activity,
        Type ReturnType,
        SourceExpressionException SourceExpressionException,
        VisualBasicSettings VisualBasicSettings);
}
