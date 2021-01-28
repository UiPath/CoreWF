namespace Microsoft.Common
{
    using System;
    using System.Collections.Generic;

    public class ExpressionToCompile : CompilerInput
    {
        public ExpressionToCompile(string code, IReadOnlyCollection<string> importedNamespaces) : base(code, importedNamespaces) { }
        public Func<string, Type> VariableTypeGetter { get; set; }
        public Type LambdaReturnType { get; set; }
    }
}