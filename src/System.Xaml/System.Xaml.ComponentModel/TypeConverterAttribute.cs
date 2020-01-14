#if !HAS_TYPE_CONVERTER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml.ComponentModel;
using System.Runtime.Serialization;
using System.Reflection;

namespace System.Xaml.ComponentModel
{

	/// <summary>
	/// Type converter attribute, for type converter compatibility in portable class libraries.
	/// </summary>
	class TypeConverterAttribute : Attribute
	{
		string _converterTypeName;
		/// <summary>
		/// Gets the name of the type for the type converter of the associated type.
		/// </summary>
		/// <value>The name of the type.</value>
		public string ConverterTypeName
		{
			get { return _converterTypeName; }
			set { _converterTypeName = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeConverterAttribute"/> class.
		/// </summary>
		/// <param name="type">Type of the type converter.</param>
		public TypeConverterAttribute(Type type)
		{
			ConverterTypeName = type.AssemblyQualifiedName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeConverterAttribute"/> class.
		/// </summary>
		/// <param name="typeName">Type name of the type converter.</param>
		public TypeConverterAttribute(string typeName)
		{
			ConverterTypeName = typeName; 
		}
	}
	
}
#endif