using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Activities
{
    public abstract class Compiler
    {
        public abstract LambdaExpression CompileExpression(CompilerRequest compilerRequest);
    }
    public class CompilerRequest
    {
        public CompilerRequest(string expressionString, IReadOnlyCollection<Assembly> referencedAssemblies, IReadOnlyCollection<string> importedNamespaces)
        {
            ExpressionString = expressionString;
            ReferencedAssemblies = referencedAssemblies;
            ImportedNamespaces = importedNamespaces;
        }
        public IReadOnlyCollection<Assembly> ReferencedAssemblies { get; }
        public IReadOnlyCollection<string> ImportedNamespaces { get; }
        public string ExpressionString { get; }
        public Func<string, Type> VariableTypeGetter { get; set; }
        public Type LambdaReturnType { get; set; }
    }
}