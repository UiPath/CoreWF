//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Xaml.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xaml.Markup;
using System.Xaml;
using System.Xaml.Schema;
using System.Xml;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

#if !HAS_TYPE_CONVERTER

#if STRONGNAME
[assembly: InternalsVisibleTo("System.Xaml.Compat, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d1c3c3fdff475bd48ad578039ab969e954c6378b6c7ab21ebcb1059d450b8c77e8260b1d6c227f6da946a45f1e67dea68e5e45daa21cd208bc9ea72c86568de861c64fd2c57d16a955ad24fb8b1cd78f4b7f9747014e69a1dfb3ea4dab6eb3a76a639dfb51eda575d5906831ca9cf251a200010d84faafb0ca64eae3504fecdc")]
#else
[assembly: InternalsVisibleTo("System.Xaml.Compat")]
#endif

#endif

namespace System.Xaml
{
	internal class ValueSerializerContext : IValueSerializerContext, IXamlSchemaContextProvider, ITypeDescriptorContext
	{
		XamlNameResolver name_resolver;
		XamlTypeResolver type_resolver;
		NamespaceResolver namespace_resolver;
		PrefixLookup prefix_lookup;
		XamlSchemaContext sctx;
		Func<IAmbientProvider> _ambientProviderProvider;
		IProvideValueTarget provideValue;
		IRootObjectProvider rootProvider;
		IDestinationTypeProvider destinationProvider;
		IXamlObjectWriterFactory objectWriterFactory;

#if !HAS_TYPE_CONVERTER

		static bool s_valueSerializerTypeInitialized;
		static Type s_valueSerializerType;

		static Type GetValueSerializerType()
		{
			if (s_valueSerializerTypeInitialized)
				return s_valueSerializerType;
			s_valueSerializerTypeInitialized = true;

			// use reflection.emit to create a subclass of ValueSerializerContext that implements 
			// System.ComponentModel.ITypeDescriptorContext since we can't access it here.
			var typeName = "SystemValueSerializerContext";

			var appDomainType = ReflectionHelpers.GetCorlibType("System.AppDomain");
			var assemblyBuilderAccess = ReflectionHelpers.GetCorlibType("System.Reflection.Emit.AssemblyBuilderAccess");
			var typeAttributesType = ReflectionHelpers.GetCorlibType("System.Reflection.TypeAttributes");
			var currentDomainProp = appDomainType?.GetRuntimeProperty("CurrentDomain");
			var typeDescriptorContextType = ReflectionHelpers.GetComponentModelType("System.ComponentModel.ITypeDescriptorContext");
			var containerType = ReflectionHelpers.GetComponentModelType("System.ComponentModel.IContainer");
			var propertyDescriptorType = ReflectionHelpers.GetComponentModelType("System.ComponentModel.PropertyDescriptor");
			var strongNameKeyPairType = ReflectionHelpers.GetCorlibType("System.Reflection.StrongNameKeyPair");
			if (appDomainType == null
				|| assemblyBuilderAccess == null
				|| typeAttributesType == null
				|| currentDomainProp == null
			    || typeDescriptorContextType == null
			    || containerType == null
			    || propertyDescriptorType == null
			    || strongNameKeyPairType == null
			   )
				return null;

			object currentDomain = currentDomainProp.GetValue(null);

			dynamic an = new AssemblyName("System.Xaml.Compat");
#if STRONGNAME
			using (var stream = typeof(ValueSerializerContext).GetTypeInfo().Assembly.GetManifestResourceStream("System.Xaml.Compat.snk"))
			{
				var data = new byte[stream.Length];
				stream.Read(data, 0, (int)stream.Length);
				dynamic keyPair = Activator.CreateInstance(strongNameKeyPairType, data);
				an.KeyPair = keyPair;

			}
#endif

			dynamic assemblyBuilder = appDomainType
				.GetRuntimeMethod("DefineDynamicAssembly", new Type[] { typeof(AssemblyName), assemblyBuilderAccess })
				.Invoke(currentDomain, new object[] { an, 1 });

			object moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

			dynamic typeBuilder = moduleBuilder
				.GetType()
				.GetRuntimeMethod("DefineType", new Type[] { typeof(string), typeAttributesType, typeof(Type) })
				.Invoke(moduleBuilder, new object[] { typeName, 0, typeof(ValueSerializerContext) }); // 0 = Class

			typeBuilder.AddInterfaceImplementation(typeDescriptorContextType);

			var getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;;
			//public IContainer Container => null;
			var propertyBuilder = typeBuilder.DefineProperty("Container", PropertyAttributes.None, containerType, null);
			var getter = typeBuilder.DefineMethod("get_Container", getSetAttr, containerType, null);
			var il = getter.GetILGenerator();
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);
			propertyBuilder.SetGetMethod(getter);

			//public PropertyDescriptor PropertyDescriptor => null;
			propertyBuilder = typeBuilder.DefineProperty("PropertyDescriptor", PropertyAttributes.None, propertyDescriptorType, null);
			getter = typeBuilder.DefineMethod("get_PropertyDescriptor", getSetAttr, propertyDescriptorType, null);
			il = getter.GetILGenerator();
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);
			propertyBuilder.SetGetMethod(getter);

			s_valueSerializerType = typeBuilder.CreateType();
			return s_valueSerializerType;
		}
#endif

		public static ValueSerializerContext Create(PrefixLookup prefixLookup, XamlSchemaContext schemaContext, Func<IAmbientProvider> ambientProvider, IProvideValueTarget provideValue, IRootObjectProvider rootProvider, IDestinationTypeProvider destinationProvider, IXamlObjectWriterFactory objectWriterFactory)
		{
#if !HAS_TYPE_CONVERTER
			ValueSerializerContext context;
			var type = GetValueSerializerType();
			if (type != null)
				context = Activator.CreateInstance(type) as ValueSerializerContext;
			else
				context = new ValueSerializerContext();
#else
			var context = new ValueSerializerContext();
#endif
			context.Initialize(prefixLookup, schemaContext, ambientProvider, provideValue, rootProvider, destinationProvider, objectWriterFactory);
			return context;
		}

		void Initialize(PrefixLookup prefixLookup, XamlSchemaContext schemaContext, Func<IAmbientProvider> ambientProvider, IProvideValueTarget provideValue, IRootObjectProvider rootProvider, IDestinationTypeProvider destinationProvider, IXamlObjectWriterFactory objectWriterFactory)
		{
			prefix_lookup = prefixLookup ?? throw new ArgumentNullException("prefixLookup");
			sctx = schemaContext ?? throw new ArgumentNullException("schemaContext");
			_ambientProviderProvider = ambientProvider;
			this.provideValue = provideValue;
			this.rootProvider = rootProvider;
			this.destinationProvider = destinationProvider;
			this.objectWriterFactory = objectWriterFactory;
		}

		NamespaceResolver NamespaceResolver => namespace_resolver ?? (namespace_resolver = new NamespaceResolver(prefix_lookup.Namespaces));

		XamlTypeResolver TypeResolver => type_resolver ?? (type_resolver = new XamlTypeResolver(NamespaceResolver, sctx));

		XamlNameResolver NameResolver => name_resolver ?? (name_resolver = new XamlNameResolver());

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(INamespacePrefixLookup))
				return prefix_lookup;
			if (serviceType == typeof(IXamlNamespaceResolver))
				return NamespaceResolver;
			if (serviceType == typeof(IXamlNameResolver) || serviceType == typeof(IXamlNameProvider))
				return NameResolver;
			if (serviceType == typeof(IXamlTypeResolver))
				return TypeResolver;
			if (serviceType == typeof(IAmbientProvider))
				return _ambientProviderProvider?.Invoke();
			if (serviceType == typeof(IXamlSchemaContextProvider))
				return this;
			if (serviceType == typeof(IProvideValueTarget))
				return provideValue;
			if (serviceType == typeof(IRootObjectProvider))
				return rootProvider;
			if (serviceType == typeof(IDestinationTypeProvider))
				return destinationProvider;
			if (serviceType == typeof(IXamlObjectWriterFactory))
				return objectWriterFactory;
			return null;
		}

		XamlSchemaContext IXamlSchemaContextProvider.SchemaContext => sctx;

		public virtual object Instance => null;

#if HAS_TYPE_CONVERTER
		public IContainer Container => null;

		public PropertyDescriptor PropertyDescriptor => null;
#endif

		public virtual void OnComponentChanged()
		{
			throw new NotImplementedException();
		}
		public virtual bool OnComponentChanging ()
		{
			throw new NotImplementedException ();
		}
		public ValueSerializer GetValueSerializerFor (PropertyInfo descriptor)
		{
			throw new NotImplementedException ();
		}
		public ValueSerializer GetValueSerializerFor (Type type)
		{
			throw new NotImplementedException ();
		}
	}

	internal class XamlTypeResolver : IXamlTypeResolver
	{
		NamespaceResolver ns_resolver;
		XamlSchemaContext schema_context;

		public XamlTypeResolver (NamespaceResolver namespaceResolver, XamlSchemaContext schemaContext)
		{
			ns_resolver = namespaceResolver;
			schema_context = schemaContext;
		}

		public Type Resolve (string typeName)
		{
			var tn = XamlTypeName.Parse (typeName, ns_resolver);
			var xt = schema_context.GetXamlType (tn);
			return xt != null ? xt.UnderlyingType : null;
		}
	}

	internal class NamespaceResolver : IXamlNamespaceResolver
	{
		public NamespaceResolver (IList<NamespaceDeclaration> source)
		{
			this.source = source;
		}
	
		IList<NamespaceDeclaration> source;
	
		public string GetNamespace (string prefix)
		{
			foreach (var nsd in source)
				if (nsd.Prefix == prefix)
					return nsd.Namespace;
			return null;
		}
	
		public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes ()
		{
			return source;
		}
	}
}
