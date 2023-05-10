using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Text;
using System.Threading;

namespace System.Activities
{
    public sealed class VBCompilerHelper : CompilerHelper
    {
        static int crt = 0;

        public override int IdentifierKind => (int)SyntaxKind.IdentifierName;

        public override StringComparer IdentifierNameComparer => StringComparer.OrdinalIgnoreCase;

        public override string CreateExpressionCode(string types, string names, string code)
        {
            var arrayType = types.Split(",");
            if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
                return $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";

            var (myDelegate, name) = DefineDelegate(types);
            return $"{myDelegate} \n Public Shared Function CreateExpression() As Expression(Of {name}(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";
        }

        private static (string, string) DefineDelegate(string types)
        {
            var crtValue = Interlocked.Add(ref crt, 1);

            var arrayType = types.Split(",");
            var part1 = new StringBuilder();
            var part2 = new StringBuilder();

            for (var i = 0; i < arrayType.Length - 1; i++)
            {
                part1.Append($" In T{i},");
                part2.Append($" ByVal arg as T{i},");
            }
            part2.Remove(part2.Length - 1, 1);

            var name = $"Func{crtValue}";
            return ($"Public Delegate Function {name}(Of {part1} Out TResult)({part2}) As TResult", name);
        }
    }
}
