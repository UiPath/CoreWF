using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace System.Activities
{
    public abstract class CompilerHelper
    {
        internal static IReadOnlyCollection<string> DefaultNamespaces = new HashSet<string>
        {
            "System",
            "System.Linq.Expressions"
        };

        public const string Comma = ", ";

        public abstract string CreateExpressionCode(string[] types, string[] names, string code);

        public abstract StringComparer IdentifierNameComparer { get; }

        public abstract StringComparison IdentifierNameComparison { get; }

        public abstract int IdentifierKind { get; }

        [Obsolete("DefineDelegate(string types) is deprecated, please use DefineDelegate(IEnumerable<string> types) instead.")]
        public (string, string) DefineDelegate(string types)
            => DefineDelegateCommon(types.Split(",").Length - 1);

        public (string, string) DefineDelegate(IEnumerable<string> types)
            => DefineDelegateCommon(types.Count() - 1);

        protected abstract (string, string) DefineDelegateCommon(int argumentsCount);

        public abstract Compilation DefaultCompilationUnit { get; }

        public abstract ParseOptions ScriptParseOptions { get; }

        public abstract string GetTypeName(Type type);
    }
}
