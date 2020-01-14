using System;
using System.Globalization;
using System.Reflection;
#if NETSTANDARD
using System.ComponentModel;
#endif

namespace System.Xaml.ComponentModel
{

#if !HAS_TYPE_CONVERTER
	static class TypeDescriptor
	{
		static MethodInfo s_getConverterMethod;
		static object s_getConverterMethodLock = new object();

		public static TypeConverter GetConverter(Type type)
		{
			var converterAttribute = type.GetTypeInfo().GetCustomAttribute<TypeConverterAttribute>();
			if (converterAttribute != null)
			{
				var typeConverter = Activator.CreateInstance(Type.GetType(converterAttribute.ConverterTypeName)) as TypeConverter;
				return typeConverter;
			}

			if (s_getConverterMethod == null)
				lock (s_getConverterMethodLock)
				{
					if (s_getConverterMethod == null)
					{
						var typeDescriptorType = ReflectionHelpers.GetComponentModelType("System.ComponentModel.TypeDescriptor");
						s_getConverterMethod = typeDescriptorType?.GetRuntimeMethod("GetConverter", new[] { typeof(Type) });
					}
				}
			var converter = s_getConverterMethod?.Invoke(null, new object[] { type });
			if (converter == null)
				return null;
			return new SystemTypeConverter(converter);
		}
	}

	class SystemTypeConverter : TypeConverter
	{
		object _typeConverter;
		MethodInfo _canConvertFrom;
		MethodInfo _canConvertTo;
		MethodInfo _convertFrom;
		MethodInfo _convertTo;

		public static readonly Type s_typeDescriptorContentType = ReflectionHelpers.GetComponentModelType("System.ComponentModel.ITypeDescriptorContext");

		public object TypeConverter => _typeConverter;

		public SystemTypeConverter(object typeConverter)
		{
			if (s_typeDescriptorContentType == null)
				throw new XamlException("This platform does not provide System.Component.ITypeDescriptorContext");

			_typeConverter = typeConverter;
			var type = _typeConverter.GetType();
			_canConvertFrom = type.GetRuntimeMethod("CanConvertFrom", new[] { s_typeDescriptorContentType, typeof(Type) });
			_canConvertTo = type.GetRuntimeMethod("CanConvertTo", new[] { s_typeDescriptorContentType, typeof(Type) });
			_convertFrom = type.GetRuntimeMethod("ConvertFrom", new[] { s_typeDescriptorContentType, typeof(CultureInfo), typeof(object) });
			_convertTo = type.GetRuntimeMethod("ConvertTo", new[] { s_typeDescriptorContentType, typeof(CultureInfo), typeof(object), typeof(Type) });
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			var c = s_typeDescriptorContentType.GetTypeInfo().IsAssignableFrom(context?.GetType().GetTypeInfo()) ? context : null;
			return (bool)(_canConvertFrom?.Invoke(_typeConverter, new object[] { c, sourceType }) ?? false);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			var c = s_typeDescriptorContentType.GetTypeInfo().IsAssignableFrom(context?.GetType().GetTypeInfo()) ? context : null;
			return (bool)(_canConvertTo?.Invoke(_typeConverter, new object[] { c, destinationType }) ?? false);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var c = s_typeDescriptorContentType.GetTypeInfo().IsAssignableFrom(context?.GetType().GetTypeInfo()) ? context : null;
			return _convertFrom?.Invoke(_typeConverter, new object[] { c, culture, value });
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			var c = s_typeDescriptorContentType.GetTypeInfo().IsAssignableFrom(context?.GetType().GetTypeInfo()) ? context : null;
			return _convertTo?.Invoke(_typeConverter, new object[] { c, culture, value, destinationType });
		}
	}
#endif
}
