using Microsoft.CodeAnalysis;
using System.Collections.Generic;

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

        public abstract string CreateExpressionCode(string types, string names, string code);

        public abstract StringComparer IdentifierNameComparer { get; }

        public abstract int IdentifierKind { get; }

        public abstract (string, string) DefineDelegate(string types);

        public abstract Compilation DefaultCompilationUnit { get; }

        public abstract ParseOptions ScriptParseOptions { get; }

        public abstract string GetTypeName(Type type);
    }
}
