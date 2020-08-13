using System.Collections.Generic;
using System.Reflection;

namespace System.Activities.XamlIntegration
{
    public abstract class AheadOfTimeCompiler
    {
        public abstract TextExpressionCompilerResults Compile(ClassToCompile classToCompile);
    }
    public class ClassToCompile : CompilerInput
    {
        public ClassToCompile(string className, string code, IReadOnlyCollection<Assembly> referencedAssemblies, IReadOnlyCollection<string> importedNamespaces) :
            base(code, referencedAssemblies, importedNamespaces) => ClassName = className;
        public string ClassName { get; }
    }
}