using System;

namespace System.Xaml.Markup
{
	[AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
	//[System.Runtime.CompilerServices.TypeForwardedFrom (Consts.AssemblyWindowsBase)]
	public sealed class ValueSerializerAttribute : Attribute
	{
		private Type _valueSerializerType;
		private string _valueSerializerTypeName;

		public Type ValueSerializerType
		{
			get
			{
				if (_valueSerializerType == null && _valueSerializerTypeName != null)
					_valueSerializerType = Type.GetType(_valueSerializerTypeName);
				return _valueSerializerType;
			}
		}

		public string ValueSerializerTypeName
		{
			get
			{
				if (_valueSerializerType != null)
					return _valueSerializerType.AssemblyQualifiedName;
				else
					return _valueSerializerTypeName;
			}
		}

		public ValueSerializerAttribute(Type valueSerializerType)
		{
			_valueSerializerType = valueSerializerType;
		}

		public ValueSerializerAttribute(string valueSerializerTypeName)
		{
			_valueSerializerTypeName = valueSerializerTypeName;
		}
	}
}

