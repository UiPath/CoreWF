using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Reflection;

namespace System.Activities
{
    public abstract class AheadOfTimeCompiler
    {
        public abstract TextExpressionCompilerResults Compile(ClassToCompile classToCompile);
    }
    public class ClassToCompile : CompilerInput
    {
        public ClassToCompile(string className, string code, IReadOnlyCollection<Assembly> referencedAssemblies, IReadOnlyCollection<string> importedNamespaces) :
            base(code, importedNamespaces)
        {
            ClassName = className;
            ReferencedAssemblies = referencedAssemblies;
        }
        public string ClassName { get; }
        public IReadOnlyCollection<Assembly> ReferencedAssemblies { get; set; }
    }
}