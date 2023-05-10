using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Text;
using System.Threading;

namespace System.Activities
{
    public sealed class CSharpCompilerHelper : CompilerHelper
    {
        static int crt = 0;

        public override int IdentifierKind => (int)SyntaxKind.IdentifierName;

        public override StringComparer IdentifierNameComparer => StringComparer.Ordinal;

        public override string CreateExpressionCode(string types, string names, string code)
        {
            var arrayType = types.Split(",");
            if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
                return $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";


            var (myDelegate, name) = DefineDelegate(types);
            return $"{myDelegate} \n public static Expression<{name}<{types}>> CreateExpression() => ({names}) => {code};";
        }

        private static (string, string) DefineDelegate(string types)
        {
            var crtValue = Interlocked.Add(ref crt, 1);
            var arrayType = types.Split(",");
            var part1 = new StringBuilder();
            var part2 = new StringBuilder();

            for (var i = 0; i < arrayType.Length - 1; i++)
            {
                part1.Append($"in T{i}, ");
                part2.Append($" T{i} arg{i},");
            }
            part2.Remove(part2.Length - 1, 1);
            var name = $"Func{crtValue}";
            return ($"public delegate TResult {name}<{part1} out TResult>({part2});", name);
        }
    }
}
