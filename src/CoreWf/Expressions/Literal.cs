// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Markup;

namespace System.Activities.Expressions;

[DebuggerStepThrough]
[ContentProperty("Value")]
public sealed class Literal<T> : CodeActivity<T>, ILiteral, IExpressionContainer, IValueSerializableExpression
{
    private static readonly Regex ExpressionEscapeRegex = new(@"^(%*\[)");

    public Literal()
    {
        UseOldFastPath = true;
    }

    public Literal(T value)
        : this()
    {
        Value = value;
    }

    public T Value { get; set; }

    object ILiteral.Value => Value;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        Type literalType = typeof(T);

        if (!literalType.IsValueType && literalType != TypeHelper.StringType)
        {
            metadata.AddValidationError(SR.LiteralsMustBeValueTypesOrImmutableTypes(TypeHelper.StringType, literalType));
        }
    }

    protected override T Execute(CodeActivityContext context) => Value;

    public override string ToString() => Value == null ? "null" : Value.ToString();

    public bool CanConvertToString(IValueSerializerContext context)
    {
        Type typeArgument;
        Type valueType;
        TypeConverter converter;

        if (Value == null)
        {
            return true;
        }

        typeArgument = typeof(T);
        valueType = Value.GetType();

        if (valueType == TypeHelper.StringType)
        {
            string myValue = Value as string;
            if (string.IsNullOrEmpty(myValue))
            {
                return false;
            }
        }

        converter = TypeDescriptor.GetConverter(typeArgument);
        if (typeArgument == valueType &&
            converter != null &&
            converter.CanConvertTo(TypeHelper.StringType) &&
            converter.CanConvertFrom(TypeHelper.StringType))
        {
            if (valueType == typeof(DateTime))
            {
                DateTime literalValue = (DateTime)(object)Value;
                return IsShortTimeFormattingSafe(literalValue);
            }

            if (valueType == typeof(DateTimeOffset))
            {
                DateTimeOffset literalValue = (DateTimeOffset)(object)Value;
                return IsShortTimeFormattingSafe(literalValue);
            }

            return true;
        }

        return false;
    }

    private static bool IsShortTimeFormattingSafe(DateTime literalValue)
    {
        if (literalValue.Second == 0 && literalValue.Millisecond == 0 && literalValue.Kind == DateTimeKind.Unspecified)
        {
            DateTime noLeftOverTicksDateTime = new(
                literalValue.Year,
                literalValue.Month,
                literalValue.Day,
                literalValue.Hour,
                literalValue.Minute,
                literalValue.Second,
                literalValue.Millisecond,
                literalValue.Kind);

            if (literalValue.Ticks == noLeftOverTicksDateTime.Ticks)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsShortTimeFormattingSafe(DateTimeOffset literalValue) =>
        // DateTimeOffset is similar to DateTime in how its 4.0 string conversion did not preserve seconds, milliseconds, the remaining ticks and DateTimeKind data.
        IsShortTimeFormattingSafe(literalValue.DateTime);

    //[SuppressMessage(FxCop.Category.Globalization, FxCop.Rule.SpecifyIFormatProvider,
    //    Justification = "we really do want the string as-is")]
    public string ConvertToString(IValueSerializerContext context)
    {
        Type typeArgument;
        Type valueType;
        TypeConverter converter;

        if (Value == null)
        {
            return "[Nothing]";
        }

        typeArgument = typeof(T);
        valueType = Value.GetType();
        converter = TypeDescriptor.GetConverter(typeArgument);

        Fx.Assert(typeArgument == valueType &&
            converter != null &&
            converter.CanConvertTo(TypeHelper.StringType) &&
            converter.CanConvertFrom(TypeHelper.StringType),
            "Literal target type T and the return type mismatch or something wrong with its typeConverter!");

        // handle a Literal<string> of "[...]" by inserting escape chararcter '%' at the front
        if (typeArgument == TypeHelper.StringType)
        {
            string originalString = Convert.ToString(Value);
            if (originalString.EndsWith("]", StringComparison.Ordinal) && ExpressionEscapeRegex.IsMatch(originalString))
            {
                return "%" + originalString;
            }
        }
        return converter.ConvertToString(context, Value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeValue() => !Equals(Value, default(T));
}

public interface ILiteral
{
    public object Value { get; }
}
