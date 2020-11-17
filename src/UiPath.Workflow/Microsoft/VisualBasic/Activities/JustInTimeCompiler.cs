using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Activities
{
    public abstract class JustInTimeCompiler
    {
        public abstract LambdaExpression CompileExpression(ExpressionToCompile compilerRequest);
    }
    public class CompilerInput
    {
        public CompilerInput(string code, IReadOnlyCollection<string> importedNamespaces)
        {
            Code = code;
            ImportedNamespaces = importedNamespaces;
        }
        public IReadOnlyCollection<string> ImportedNamespaces { get; }
        public string Code { get; }
        public override string ToString() => Code;
    }
    public class ExpressionToCompile : CompilerInput
    {
        public ExpressionToCompile(string code, IReadOnlyCollection<string> importedNamespaces) : base(code, importedNamespaces) {}
        public Func<string, Type> VariableTypeGetter { get; set; }
        public Type LambdaReturnType { get; set; }
    }
}