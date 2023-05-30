using System;

namespace System.Activities
{
    public abstract class CompilerHelper
    {
        public abstract string CreateExpressionCode(string types, string names, string code);

        public abstract StringComparer IdentifierNameComparer { get; }

        public abstract int IdentifierKind { get; }

        public abstract (string, string) DefineDelegate(string types);
    }
}
