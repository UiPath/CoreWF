using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace System.Activities.XamlIntegration
{
    public abstract class AheadOfTimeCompiler
    {
        public abstract TextExpressionCompilerResults Compile(ClassToCompile classToCompile);
    }
    public class ClassToCompile
    {
        public ClassToCompile(string className, CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            Code = compilationUnit.GetCSharpCode();
            References = options.GetReferences();
            Imports = compilationUnit.GetImports();
            ClassName = className;
        }
        public string Code { get; }
        public IReadOnlyCollection<string> References { get; }
        public IReadOnlyCollection<string> Imports { get; }
        public string ClassName { get; }
    }
}