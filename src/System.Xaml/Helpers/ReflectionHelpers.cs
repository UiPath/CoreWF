using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
namespace System.Xaml
{
	static class ReflectionHelpers
	{
#if NETSTANDARD1_0
		public static readonly Type IXmlSerializableType = typeof(System.Xml.XmlReader).GetTypeInfo().Assembly.GetType("System.Xml.Serialization.IXmlSerializable");

		public static readonly MethodInfo IXmlSerializableWriteXmlMethod = IXmlSerializableType?.GetTypeInfo().GetDeclaredMethod("WriteXml");
		public static readonly MethodInfo IXmlSerializableReadXmlMethod = IXmlSerializableType?.GetTypeInfo().GetDeclaredMethod("ReadXml");
#elif NETSTANDARD1_3 || NETSTANDARD2_0
		public static readonly Type IXmlSerializableType = typeof(System.Xml.Serialization.IXmlSerializable);
#else

		static Assembly componentModelAssembly = typeof(System.ComponentModel.CancelEventArgs).GetTypeInfo().Assembly;
		static Assembly corlibAssembly = typeof(int).GetTypeInfo().Assembly;

		public static readonly Type TypeConverterType = GetComponentModelType("System.ComponentModel.TypeConverter");

		public static readonly Type IXmlSerializableType = Type.GetType("System.Xml.IXmlSerializable");

		public static readonly MethodInfo IXmlSerializableWriteXmlMethod = IXmlSerializableType?.GetTypeInfo().GetDeclaredMethod("WriteXml");
		public static readonly MethodInfo IXmlSerializableReadXmlMethod = IXmlSerializableType?.GetTypeInfo().GetDeclaredMethod("ReadXml");

		public static Type GetComponentModelType(string name) => componentModelAssembly.GetType(name);
		public static Type GetCorlibType(string name) => corlibAssembly.GetType(name);
#endif
	}
}
