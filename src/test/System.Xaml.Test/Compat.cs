using System;
using System.IO;
using System.Reflection;

namespace MonoTests.System.Xaml
{
	static class Compat
	{

#if PCL

#if CORE
		public const string Version = "core";
#elif WINDOWS_UWP
		public const string Version = "uwp";
#elif NETSTANDARD1_0
		public const string Version = "netstandard10";
#elif NETSTANDARD1_3
		public const string Version = "netstandard13";
#elif NETSTANDARD2_0
		public const string Version = "netstandard20";
#elif PCL136
		public const string Version = "pcl136";
#elif PCL259
		public const string Version = "pcl259";
#endif

		public const string Namespace = "System.Xaml";
		public const string Prefix = "sx";
		public static bool IsPortableXaml = true;
#else
		public const string Version = "net_4_5";
		public const string Namespace = "System.Xaml";
		public const string Prefix = "sx";
		public static bool IsPortableXaml = false;
#endif

		public static readonly string TestAssemblyName = typeof(Compat).Assembly.GetName().Name;

		public static readonly string TestAssemblyNamespace = "clr-namespace:MonoTests.System.Xaml;assembly=" + TestAssemblyName;

#if NETSTANDARD 		
#if HAS_ISUPPORT_INITIALIZE
		public const bool HasISupportInitializeInterface = true;
#else
		public const bool HasISupportInitializeInterface = false;
#endif
#else
		public const bool HasISupportInitializeInterface = true;
#endif	
		

		public static string Fixup(this string str)
		{
#if PCL
			return str;
#else
			return str
				.Replace ("System.Windows.Markup", "System.Windows.Markup")
				.Replace ("System.Xaml", Namespace);
#endif
		}

#if !NETSTANDARD
		public static Type GetTypeInfo(this Type type) => type;
#endif

#if PCL136
		public static Assembly GetAssembly(this Type type) => type.Assembly;
		public static bool GetIsGenericType(this Type type) => type.IsGenericType;
#else
		public static Assembly GetAssembly(this Type type) => type.GetTypeInfo().Assembly;
		public static bool GetIsGenericType(this Type type) => type.GetTypeInfo().IsGenericType;
#endif

		public static string UpdateXml(this string str)
		{
			return str
				.Replace("assembly=System.Xaml_test_net_4_5", $"assembly={TestAssemblyName}")
				.Replace("assembly=System.Xaml.Test", $"assembly={TestAssemblyName}")
				.Replace("net_4_0", Compat.Version)
				.Replace("net_4_5", Compat.Version)
				.Replace("clr-namespace:System.Xaml;assembly=System.Xaml", $"clr-namespace:{Compat.Namespace};assembly={Compat.Namespace}")
				.Replace($" px:", $" {Compat.Prefix}:")
				.Replace($"xmlns:px", $"xmlns:{Compat.Prefix}")
				.Replace("\r", "")
				.Replace("\n", Environment.NewLine);
		}

		public static string UpdateJson(this string str)
		{
			return str.Replace("net_4_0", Compat.Version)
				.Replace("net_4_5", Compat.Version)
				.Replace("clr-namespace:System.Xaml;assembly=System.Xaml", $"clr-namespace:{Compat.Namespace};assembly={Compat.Namespace}")
				.Replace($" px:", $" {Compat.Prefix}:")
				.Replace($"$ns:px", $"$ns:{Compat.Prefix}")
				.Replace("\r", "")
				.Replace("\n", Environment.NewLine);
		}

		public static bool IsMono
        {
            get { return Type.GetType("Mono.Runtime", false) != null; }
        }

		public static string GetTestFile (string fileName)
		{
			return Path.Combine (
#if !WINDOWS_UWP
				Path.GetDirectoryName (typeof(Compat).GetAssembly().Location),
#endif
				"XmlFiles",
				fileName);
		}
	}
}