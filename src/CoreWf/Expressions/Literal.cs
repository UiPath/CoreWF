// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions
{
    using System;
    using System.Activities.XamlIntegration;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Windows.Markup;
    using System.Activities.Runtime;

    [DebuggerStepThrough]
    [ContentProperty("Value")]
    public sealed class Literal<T> : CodeActivity<T>, ILiteral, IExpressionContainer, IValueSerializableExpression
    {
        private static Regex ExpressionEscapeRegex = new Regex(@"^(%*\[)");

        public Literal()
        {
            this.UseOldFastPath = true;
        }

        public Literal(T value)
            : this()
        {
            this.Value = value;
        }

        public T Value
        {
            get;
            set;
        }
        object ILiteral.Value => Value;

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            Type literalType = typeof(T);

            if (!literalType.IsValueType && literalType != TypeHelper.StringType)
            {
                metadata.AddValidationError(SR.LiteralsMustBeValueTypesOrImmutableTypes(TypeHelper.StringType, literalType));
            }
        }

        protected override T Execute(CodeActivityContext context)
        {
            return this.Value;
        }

        public override string ToString()
        {
            return this.Value == null ? "null" : this.Value.ToString();
        }

        public bool CanConvertToString(IValueSerializerContext context)
        {
            Type typeArgument;
            Type valueType;
            TypeConverter converter;

            if (this.Value == null)
            {
                return true;
            }
            
            typeArgument = typeof(T);
            valueType = this.Value.GetType();

            if (valueType == TypeHelper.StringType)
            {
                string myValue = this.Value as string;
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
                    DateTime literalValue = (DateTime)(object)this.Value;
                    return IsShortTimeFormattingSafe(literalValue);
                }

                if (valueType == typeof(DateTimeOffset))
                {
                    DateTimeOffset literalValue = (DateTimeOffset)(object)this.Value;
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
                DateTime noLeftOverTicksDateTime = new DateTime(
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

        private static bool IsShortTimeFormattingSafe(DateTimeOffset literalValue)
        {
            // DateTimeOffset is similar to DateTime in how its 4.0 string conversion did not preserve seconds, milliseconds, the remaining ticks and DateTimeKind data.
            return IsShortTimeFormattingSafe(literalValue.DateTime);
        }
        
        //[SuppressMessage(FxCop.Category.Globalization, FxCop.Rule.SpecifyIFormatProvider,
        //    Justification = "we really do want the string as-is")]
        public string ConvertToString(IValueSerializerContext context)
        {
            Type typeArgument;
            Type valueType;
            TypeConverter converter;

            if (this.Value == null)
            {
                return "[Nothing]";
            }

            typeArgument = typeof(T);
            valueType = this.Value.GetType();
            converter = TypeDescriptor.GetConverter(typeArgument);
            
            Fx.Assert(typeArgument == valueType &&
                converter != null &&
                converter.CanConvertTo(TypeHelper.StringType) &&
                converter.CanConvertFrom(TypeHelper.StringType),
                "Literal target type T and the return type mismatch or something wrong with its typeConverter!");

            // handle a Literal<string> of "[...]" by inserting escape chararcter '%' at the front
            if (typeArgument == TypeHelper.StringType)
            {
                string originalString = Convert.ToString(this.Value);
                if (originalString.EndsWith("]", StringComparison.Ordinal) && ExpressionEscapeRegex.IsMatch(originalString))
                {
                    return "%" + originalString;
                }
            }
            return converter.ConvertToString(context, this.Value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeValue()
        {
            return !object.Equals(this.Value, default(T));
        }
    }
    public interface ILiteral
    {
        public object Value { get; }
    }
}
