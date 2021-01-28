namespace Microsoft.Common
{
    using System.Collections.Generic;

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
}