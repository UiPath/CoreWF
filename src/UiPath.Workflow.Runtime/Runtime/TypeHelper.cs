// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

internal static class TypeHelper
{
    public static readonly Type ArrayType = typeof(Array);
    public static readonly Type BoolType = typeof(bool);
    public static readonly Type GenericCollectionType = typeof(ICollection<>);
    public static readonly Type ByteType = typeof(byte);
    public static readonly Type SByteType = typeof(sbyte);
    public static readonly Type CharType = typeof(char);
    public static readonly Type ShortType = typeof(short);
    public static readonly Type UShortType = typeof(ushort);
    public static readonly Type IntType = typeof(int);
    public static readonly Type UIntType = typeof(uint);
    public static readonly Type LongType = typeof(long);
    public static readonly Type ULongType = typeof(ulong);
    public static readonly Type FloatType = typeof(float);
    public static readonly Type DoubleType = typeof(double);
    public static readonly Type DecimalType = typeof(decimal);
    public static readonly Type ExceptionType = typeof(Exception);
    public static readonly Type NullableType = typeof(Nullable<>);
    public static readonly Type ObjectType = typeof(object);
    public static readonly Type StringType = typeof(string);
    public static readonly Type TypeType = typeof(Type);
    public static readonly Type VoidType = typeof(void);

    public static bool AreTypesCompatible(object source, Type destinationType)
    {
        if (source == null)
        {
            return !destinationType.IsValueType || IsNullableType(destinationType);
        }

        return AreTypesCompatible(source.GetType(), destinationType);
    }

    // return true if the sourceType is implicitly convertible to the destinationType
    public static bool AreTypesCompatible(Type sourceType, Type destinationType)
    {
        if (ReferenceEquals(sourceType, destinationType))
        {
            return true;
        }

        return IsImplicitNumericConversion(sourceType, destinationType) ||
            IsImplicitReferenceConversion(sourceType, destinationType) ||
            IsImplicitBoxingConversion(sourceType, destinationType) ||
            IsImplicitNullableConversion(sourceType, destinationType);
    }

    // simpler, more performant version of AreTypesCompatible when
    // we know both sides are reference types
    public static bool AreReferenceTypesCompatible(Type sourceType, Type destinationType)
    {
        Fx.Assert(!sourceType.IsValueType && !destinationType.IsValueType, "AreReferenceTypesCompatible can only be used for reference types");
        if (ReferenceEquals(sourceType, destinationType))
        {
            return true;
        }

        return IsImplicitReferenceConversion(sourceType, destinationType);
    }

    // variation to OfType<T> that uses AreTypesCompatible instead of Type equality
    public static IEnumerable<Type> GetCompatibleTypes(IEnumerable<Type> enumerable, Type targetType)
    {
        foreach (Type sourceType in enumerable)
        {
            if (AreTypesCompatible(sourceType, targetType))
            {
                yield return sourceType;
            }
        }
    }

    public static bool ContainsCompatibleType(IEnumerable<Type> enumerable, Type targetType)
    {
        foreach (Type sourceType in enumerable)
        {
            if (AreTypesCompatible(sourceType, targetType))
            {
                return true;
            }
        }

        return false;
    }

    // handles not only the simple cast, but also value type widening, etc.
    public static T Convert<T>(object source)
    {
        // first check the common cases
        if (source is T t)
        {
            return t;
        }

        if (source == null)
        {
            if (typeof(T).IsValueType && !IsNullableType(typeof(T)))
            {
                throw Fx.Exception.AsError(new InvalidCastException(SR.CannotConvertObject(source, typeof(T))));
            }

            return default;
        }

        if (TryNumericConversion(source, out T result))
        {
            return result;
        }

        throw Fx.Exception.AsError(new InvalidCastException(SR.CannotConvertObject(source, typeof(T))));
    }

    // get all of the types that this Type implements (based classes, interfaces, etc)
    public static IEnumerable<Type> GetImplementedTypes(Type type)
    {
        Dictionary<Type, object> typesEncountered = new();

        GetImplementedTypesHelper(type, typesEncountered);
        return typesEncountered.Keys;
    }

    //[SuppressMessage(FxCop.Category.Usage, "CA2301:EmbeddableTypesInContainersRule", MessageId = "typesEncountered", //Justification = "No need to support type equivalence here.")]
    private static void GetImplementedTypesHelper(Type type, Dictionary<Type, object> typesEncountered)
    {
        if (typesEncountered.ContainsKey(type))
        {
            return;
        }

        typesEncountered.Add(type, type);

        Type[] interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; ++i)
        {
            GetImplementedTypesHelper(interfaces[i], typesEncountered);
        }

        Type baseType = type.BaseType;
        while ((baseType != null) && (baseType != ObjectType))
        {
            GetImplementedTypesHelper(baseType, typesEncountered);
            baseType = baseType.BaseType;
        }
    }

    //[SuppressMessage(FxCop.Category.Maintainability, FxCop.Rule.AvoidExcessiveComplexity,
    //Justification = "Need to check all possible numeric conversions")]
    private static bool IsImplicitNumericConversion(Type source, Type destination)
    {
        TypeCode sourceTypeCode = source.GetTypeCode();
        TypeCode destinationTypeCode = destination.GetTypeCode();

        return sourceTypeCode switch
        {
            TypeCode.SByte => destinationTypeCode is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Byte => destinationTypeCode is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int16 => destinationTypeCode is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt16 => destinationTypeCode is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int32 => destinationTypeCode is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt32 => destinationTypeCode is TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int64 or TypeCode.UInt64 => destinationTypeCode is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Char => destinationTypeCode is TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Single => destinationTypeCode is TypeCode.Double,
            _ => false,
        };
    }

    private static bool IsImplicitReferenceConversion(Type sourceType, Type destinationType) => destinationType.IsAssignableFrom(sourceType);

    private static bool IsImplicitBoxingConversion(Type sourceType, Type destinationType)
    {
        if (sourceType.IsValueType && (destinationType == ObjectType || destinationType == typeof(ValueType)))
        {
            return true;
        }
        if (sourceType.IsEnum && destinationType == typeof(Enum))
        {
            return true;
        }
        return false;
    }

    private static bool IsImplicitNullableConversion(Type sourceType, Type destinationType)
    {
        if (!IsNullableType(destinationType))
        {
            return false;
        }

        destinationType = destinationType.GetGenericArguments()[0];
        if (IsNullableType(sourceType))
        {
            sourceType = sourceType.GetGenericArguments()[0];
        }
        return AreTypesCompatible(sourceType, destinationType);
    }

    private static bool IsNullableType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == NullableType;

    private static bool TryNumericConversion<T>(object source, out T result)
    {
        Fx.Assert(source != null, "caller must verify");
        TypeCode sourceTypeCode = source.GetType().GetTypeCode();
        TypeCode destinationTypeCode = typeof(T).GetTypeCode();

        switch (sourceTypeCode)
        {
            case TypeCode.SByte:
                {
                    sbyte sbyteSource = (sbyte)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int16:
                            result = (T)(object)(short)sbyteSource;
                            return true;
                        case TypeCode.Int32:
                            result = (T)(object)(int)sbyteSource;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)sbyteSource;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)sbyteSource;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)sbyteSource;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)sbyteSource;
                            return true;
                    }
                    break;
                }
            case TypeCode.Byte:
                {
                    byte byteSource = (byte)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int16:
                            result = (T)(object)(short)byteSource;
                            return true;
                        case TypeCode.UInt16:
                            result = (T)(object)(ushort)byteSource;
                            return true;
                        case TypeCode.Int32:
                            result = (T)(object)(int)byteSource;
                            return true;
                        case TypeCode.UInt32:
                            result = (T)(object)(uint)byteSource;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)byteSource;
                            return true;
                        case TypeCode.UInt64:
                            result = (T)(object)(ulong)byteSource;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)byteSource;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)byteSource;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)byteSource;
                            return true;
                    }
                    break;
                }
            case TypeCode.Int16:
                {
                    short int16Source = (short)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int32:
                            result = (T)(object)(int)int16Source;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)int16Source;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)int16Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)int16Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)int16Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.UInt16:
                {
                    ushort uint16Source = (ushort)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int32:
                            result = (T)(object)(int)uint16Source;
                            return true;
                        case TypeCode.UInt32:
                            result = (T)(object)(uint)uint16Source;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)uint16Source;
                            return true;
                        case TypeCode.UInt64:
                            result = (T)(object)(ulong)uint16Source;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)uint16Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)uint16Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)uint16Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.Int32:
                {
                    int int32Source = (int)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int64:
                            result = (T)(object)(long)int32Source;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)int32Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)int32Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)int32Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.UInt32:
                {
                    uint uint32Source = (uint)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.UInt32:
                            result = (T)(object)(uint)uint32Source;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)uint32Source;
                            return true;
                        case TypeCode.UInt64:
                            result = (T)(object)(ulong)uint32Source;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)uint32Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)uint32Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)uint32Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.Int64:
                {
                    long int64Source = (long)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Single:
                            result = (T)(object)(float)int64Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)int64Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)int64Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.UInt64:
                {
                    ulong uint64Source = (ulong)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Single:
                            result = (T)(object)(float)uint64Source;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)uint64Source;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)uint64Source;
                            return true;
                    }
                    break;
                }
            case TypeCode.Char:
                {
                    char charSource = (char)source;
                    switch (destinationTypeCode)
                    {
                        case TypeCode.UInt16:
                            result = (T)(object)(ushort)charSource;
                            return true;
                        case TypeCode.Int32:
                            result = (T)(object)(int)charSource;
                            return true;
                        case TypeCode.UInt32:
                            result = (T)(object)(uint)charSource;
                            return true;
                        case TypeCode.Int64:
                            result = (T)(object)(long)charSource;
                            return true;
                        case TypeCode.UInt64:
                            result = (T)(object)(ulong)charSource;
                            return true;
                        case TypeCode.Single:
                            result = (T)(object)(float)charSource;
                            return true;
                        case TypeCode.Double:
                            result = (T)(object)(double)charSource;
                            return true;
                        case TypeCode.Decimal:
                            result = (T)(object)(decimal)charSource;
                            return true;
                    }
                    break;
                }
            case TypeCode.Single:
                {
                    if (destinationTypeCode == TypeCode.Double)
                    {
                        result = (T)(object)(double)(float)source;
                        return true;
                    }
                    break;
                }
        }

        result = default;
        return false;
    }

    public static object GetDefaultValueForType(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }

        if (type.IsEnum)
        {
            Array enumValues = Enum.GetValues(type);
            if (enumValues.Length > 0)
            {
                return enumValues.GetValue(0);
            }
        }

        return Activator.CreateInstance(type);
    }

    public static bool IsNullableValueType(Type type) => type.IsValueType && IsNullableType(type);

    public static bool IsNonNullableValueType(Type type)
    {
        if (!type.IsValueType)
        {
            return false;
        }

        if (type.IsGenericType)
        {
            return false;
        }

        return type != StringType;
    }

    public static TypeCode GetTypeCode(this Type type)
    {
        if (type == null)
            return TypeCode.Empty;

        if (type == typeof(bool))
            return TypeCode.Boolean;

        if (type == typeof(char))
            return TypeCode.Char;

        if (type == typeof(sbyte))
            return TypeCode.SByte;

        if (type == typeof(byte))
            return TypeCode.Byte;

        if (type == typeof(short))
            return TypeCode.Int16;

        if (type == typeof(ushort))
            return TypeCode.UInt16;

        if (type == typeof(int))
            return TypeCode.Int32;

        if (type == typeof(uint))
            return TypeCode.UInt32;

        if (type == typeof(long))
            return TypeCode.Int64;

        if (type == typeof(ulong))
            return TypeCode.UInt64;

        if (type == typeof(float))
            return TypeCode.Single;

        if (type == typeof(double))
            return TypeCode.Double;

        if (type == typeof(decimal))
            return TypeCode.Decimal;

        if (type == typeof(DateTime))
            return TypeCode.DateTime;

        if (type == typeof(string))
            return TypeCode.String;

        if (type.IsEnum)
            return GetTypeCode(Enum.GetUnderlyingType(type));

        return TypeCode.Object;
    }
}
