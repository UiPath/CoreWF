using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
namespace System.Activities;

internal static class CSharpValidatorCommon
{
    private static int crt = 0;

    // This is used in case the expression does not properly close (e.g. missing quotes, or multiline comment not closed)
    private const string _expressionEnder = "// */ // \"";

    private const string _valueValidationTemplate = "public static System.Linq.Expressions.Expression<System.Func<{0}>> CreateExpression{1}()//activityId:{4}\n => ({2}) => {3}; {5}";
    private const string _delegateValueValidationTemplate = "{0}\npublic static System.Linq.Expressions.Expression<{1}<{2}>> CreateExpression{3}()//activityId:{6}\n => ({4}) => {5}; {7}";
    private const string _referenceValidationTemplate = "public static {0} IsLocation{1}()//activityId:{5}\n => ({2}) => {3} = default({4}); {6}";

    internal static string CreateReferenceCode(string types, string returnType, string names, string code, string activityId, int index)
    {
        var actionDefinition = !string.IsNullOrWhiteSpace(types)
            ? $"System.Action<{string.Join(CompilerHelper.Comma, types)}>"
            : "System.Action";
        return string.Format(_referenceValidationTemplate, actionDefinition, index, names, code, returnType, activityId, _expressionEnder);
    }

    internal static string CreateValueCode(IEnumerable<string> types, string names, string code, string activityId, int index)
    {
        var serializedArgumentTypes = string.Join(CompilerHelper.Comma, types);
        if (types.Count() <= 16) // .net defines Func<TResult>...Func<T1,...T16,TResult)
            return string.Format(_valueValidationTemplate, serializedArgumentTypes, index, names, code, activityId, _expressionEnder);

        var (myDelegate, name) = DefineDelegateCommon(types.Count() - 1);
        return string.Format(_delegateValueValidationTemplate, myDelegate, name, serializedArgumentTypes, index, names, code, activityId, _expressionEnder);
    }

    internal static (string, string) DefineDelegateCommon(int argumentsCount)
    {
        var crtValue = Interlocked.Add(ref crt, 1);

        var part1 = new StringBuilder();
        var part2 = new StringBuilder();
        for (var i = 0; i < argumentsCount; i++)
        {
            part1.Append($"in T{i}, ");
            part2.Append($" T{i} arg{i},");
        }
        part2.Remove(part2.Length - 1, 1);
        var name = $"Func{crtValue}";
        return ($"public delegate TResult {name}<{part1} out TResult>({part2});", name);
    }
}
