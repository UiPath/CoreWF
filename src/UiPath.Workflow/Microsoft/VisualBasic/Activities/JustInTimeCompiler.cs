using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Activities
{
    public abstract class JustInTimeCompiler
    {
        public abstract LambdaExpression CompileExpression(ExpressionToCompile compilerRequest);
    }
    public class CompilerInput
    {
        public CompilerInput(string code, IReadOnlyCollection<Assembly> referencedAssemblies, IReadOnlyCollection<string> importedNamespaces)
        {
            Code = code;
            ReferencedAssemblies = referencedAssemblies;
            ImportedNamespaces = importedNamespaces;
        }
        public IReadOnlyCollection<Assembly> ReferencedAssemblies { get; set; }
        public IReadOnlyCollection<string> ImportedNamespaces { get; }
        public string Code { get; }
        public override string ToString() => Code;
    }
    public class ExpressionToCompile : CompilerInput
    {
        public ExpressionToCompile(string code, IReadOnlyCollection<Assembly> referencedAssemblies, IReadOnlyCollection<string> importedNamespaces) : 
            base(code, referencedAssemblies, importedNamespaces)
        {
        }
        public Func<string, Type> VariableTypeGetter { get; set; }
        public Type LambdaReturnType { get; set; }
    }
}