#if PCL136
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace System.Xaml
{
	struct InterfaceMapping
	{
		public Type TargetType;

		public Type InterfaceType;

		public MethodInfo[] InterfaceMethods;

		public MethodInfo[] TargetMethods;

		public InterfaceMapping(Type targetType, Type interfaceType)
		{
			TargetType = targetType;
			InterfaceType = interfaceType;
			var getMethod = targetType.GetType ().GetMethod ("GetInterfaceMap");
			var mapping = getMethod.Invoke (targetType, new object[] { interfaceType });
			InterfaceMethods = mapping.GetType ().GetField ("InterfaceMethods").GetValue(mapping) as MethodInfo[];
			TargetMethods = mapping.GetType ().GetField ("TargetMethods").GetValue(mapping) as MethodInfo[];
		}

	}

	static class Compat136
	{
		public static Type GetTypeInfo(this Type type)
		{
			return type;
		}

		public static PropertyInfo GetRuntimeProperty(this Type type, string name)
		{
			return type.GetProperty(name);
		}

		public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type)
		{
			return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public static Delegate CreateDelegate(this MethodInfo method, Type eventHandlerType, object target)
		{
			return Delegate.CreateDelegate (eventHandlerType, target, method);
		}

		public static Delegate CreateDelegate(this MethodInfo method, Type eventHandlerType)
		{
			return Delegate.CreateDelegate (eventHandlerType, method);
		}

		public static IEnumerable<Attribute> GetCustomAttributes(this Assembly assembly, Type attributeType)
		{
			return assembly.GetCustomAttributes (attributeType, false).OfType<Attribute>();
		}

		public static IEnumerable<Attribute> GetCustomAttributes(this Assembly assembly)
		{
			return assembly.GetCustomAttributes(false).OfType<Attribute>();
		}

		public static System.Reflection.AssemblyName GetName(this Assembly assembly)
		{
			return new System.Reflection.AssemblyName (assembly.FullName);
		}

		public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
		{
			return type.GetProperties ();
		}

		public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type)
		{
			return type.GetEvents ();
		}

		public static T GetCustomAttribute<T>(this Type type, bool inherit = true)
		{
			return type.GetCustomAttributes(typeof(T), inherit).OfType<T>().FirstOrDefault();
		}

		public static T GetCustomAttribute<T>(this PropertyInfo property, bool inherit = true)
		{
			return property.GetCustomAttributes(typeof(T), inherit).OfType<T>().FirstOrDefault();
		}

		public static IEnumerable<T> GetCustomAttributes<T>(this Assembly assembly, bool inherit = true)
		{
			return assembly.GetCustomAttributes(typeof(T), inherit).Cast<T>();
		}

		public static FieldInfo GetRuntimeField(this Type type, string name)
		{
			return type.GetField(name);
		}

		public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] types)
		{
			return type.GetMethod (name, types);
		}

		public static InterfaceMapping GetRuntimeInterfaceMap(this Type type, Type interfaceType)
		{
			return new InterfaceMapping (type, interfaceType);
		}

		public static MethodInfo GetPrivateSetMethod(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetSetMethod(true);
		}

		public static MethodInfo GetPrivateGetMethod(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetGetMethod(true);
		}

		public static bool HasDefaultValue(this ParameterInfo parameterInfo)
		{
			var val = parameterInfo.DefaultValue;
			return val == null || val.GetType().FullName != "System.DBNull";
		}

		public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method)
		{
			return method.GetBaseDefinition();
		}

		public static MethodInfo GetMethodInfo(this Delegate del)
		{
			return del.Method;
		}

		public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type, string name)
		{
			return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				       .Where(r => r.Name == name);
		}
	}
}
#endif
