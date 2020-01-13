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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using MonoTests.System.Xaml;
using System.Globalization;
using System.ComponentModel;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;
#if PCL
using System.Xaml.Markup;
using System.Xaml.ComponentModel;
using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
#endif


namespace MonoTests.System.Xaml
{
	[TestFixture]
	public class ValueSerializerContextTest
	{
		public static void RunCanConvertFromTest(ITypeDescriptorContext context, Type sourceType) => runCanConvertFrom?.Invoke(context, sourceType);
		public static void RunConvertFromTest(ITypeDescriptorContext context, CultureInfo culture, object value) => runConvertFrom?.Invoke(context, culture, value);
		public static void RunCanConvertToTest(ITypeDescriptorContext context, Type destinationType) => runCanConvertTo?.Invoke(context, destinationType);
		public static void RunConvertToTest(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => runConvertTo?.Invoke(context, culture, value, destinationType);

		static Action<ITypeDescriptorContext, Type> runCanConvertFrom;
		static Action<ITypeDescriptorContext, CultureInfo, object> runConvertFrom;
		static Action<ITypeDescriptorContext, Type> runCanConvertTo;
		static Action<ITypeDescriptorContext, CultureInfo, object, Type> runConvertTo;

		[SetUp]
		public void SetUp()
		{
			runCanConvertFrom = null;
			runConvertFrom = null;
			runCanConvertTo = null;
			runConvertTo = null;
		}

		void SetupReaderService()
		{
			var obj = new TestValueSerialized();
			var xr = new XamlObjectReader(obj);
			while (!xr.IsEof)
				xr.Read();
		}

		void SetupWriterService()
		{
			var obj = new TestValueSerialized();
			var ctx = new XamlSchemaContext();
			var xw = new XamlObjectWriter(ctx);
			var xt = ctx.GetXamlType(obj.GetType());
			xw.WriteStartObject(xt);
			xw.WriteStartMember(XamlLanguage.Initialization);
			xw.WriteValue("v");
			xw.WriteEndMember();
			xw.Close();
		}

		[Test]
		public void ReaderServiceTest()
		{
			bool ranConvertTo = false;
			bool ranCanConvertTo = false;
			runCanConvertTo = (context, destinationType) =>
			{
				Assert.IsNotNull(context, "#1");
				Assert.AreEqual(typeof(string), destinationType, "#2");
				//Assert.IsNull(Provider.GetService(typeof(IXamlNameResolver)), "#3");
				Assert.IsNotNull(context.GetService(typeof(IXamlNameProvider)), "#4");
				//Assert.IsNull(Provider.GetService(typeof(IXamlNamespaceResolver)), "#5");
				Assert.IsNotNull(context.GetService(typeof(INamespacePrefixLookup)), "#6");
				//Assert.IsNull(Provider.GetService(typeof(IXamlTypeResolver)), "#7");
				Assert.IsNotNull(context.GetService(typeof(IXamlSchemaContextProvider)), "#8");
				Assert.IsNull(context.GetService(typeof(IAmbientProvider)), "#9");
				Assert.IsNull(context.GetService(typeof(IAttachedPropertyStore)), "#10");
				Assert.IsNull(context.GetService(typeof(IDestinationTypeProvider)), "#11");
				Assert.IsNull(context.GetService(typeof(IXamlObjectWriterFactory)), "#12");
				ranCanConvertTo = true;
			};
			runConvertTo = (context, culture, value, destinationType) =>
			{
				Assert.IsNotNull(context, "#13");
				Assert.AreEqual(CultureInfo.InvariantCulture, culture, "#14");
				Assert.AreEqual(typeof(string), destinationType, "#15");
				//Assert.IsNull(Provider.GetService(typeof(IXamlNameResolver)), "#16");
				Assert.IsNotNull(context.GetService(typeof(IXamlNameProvider)), "#17");
				//Assert.IsNull(Provider.GetService(typeof(IXamlNamespaceResolver)), "#18");
				Assert.IsNotNull(context.GetService(typeof(INamespacePrefixLookup)), "#19");
				//Assert.IsNull(Provider.GetService(typeof(IXamlTypeResolver)), "#20");
				Assert.IsNotNull(context.GetService(typeof(IXamlSchemaContextProvider)), "#21");
				Assert.IsNull(context.GetService(typeof(IAmbientProvider)), "#22");
				Assert.IsNull(context.GetService(typeof(IAttachedPropertyStore)), "#23");
				Assert.IsNull(context.GetService(typeof(IDestinationTypeProvider)), "#24");
				Assert.IsNull(context.GetService(typeof(IXamlObjectWriterFactory)), "#25");
				ranConvertTo = true;
			};
			SetupReaderService();
			Assert.IsTrue(ranConvertTo, "#26");
			Assert.IsTrue(ranCanConvertTo, "#27");
		}

		[Test]
		public void WriterServiceTest()
		{
			bool ranConvertFrom = false;
			bool ranCanConvertFrom = false;
			// need to test within the call, not outside of it
			runCanConvertFrom = (context, sourceType) =>
			{
				Assert.AreEqual(sourceType, typeof(string), "#1");
				if (Compat.IsPortableXaml)
				{
					// only System.Xaml provides the context here (extended functionality)
					Assert.IsNotNull(context, "#2");
					Assert.IsNotNull(context.GetService(typeof(IXamlNameResolver)), "#3");
					//Assert.IsNull (Provider.GetService (typeof(IXamlNameProvider)), "#4");
					Assert.IsNotNull(context.GetService(typeof(IXamlNamespaceResolver)), "#5");
					//Assert.IsNull (Provider.GetService (typeof(INamespacePrefixLookup)), "#6");
					Assert.IsNotNull(context.GetService(typeof(IXamlTypeResolver)), "#7");
					Assert.IsNotNull(context.GetService(typeof(IXamlSchemaContextProvider)), "#8");
					Assert.IsNotNull(context.GetService(typeof(IAmbientProvider)), "#9");
					Assert.IsNull(context.GetService(typeof(IAttachedPropertyStore)), "#10");
					Assert.IsNotNull(context.GetService(typeof(IDestinationTypeProvider)), "#11");
					Assert.IsNotNull(context.GetService(typeof(IXamlObjectWriterFactory)), "#12");
				}
				ranCanConvertFrom = true;
			};
			runConvertFrom = (context, culture, value) =>
			{
				Assert.IsNotNull(context, "#13");
				Assert.AreEqual(CultureInfo.InvariantCulture, culture, "#14");
				Assert.AreEqual("v", value, "#15");
				Assert.IsNotNull(context.GetService(typeof(IXamlNameResolver)), "#16");
				//Assert.IsNull (Provider.GetService (typeof(IXamlNameProvider)), "#17");
				Assert.IsNotNull(context.GetService(typeof(IXamlNamespaceResolver)), "#18");
				//Assert.IsNull (Provider.GetService (typeof(INamespacePrefixLookup)), "#19");
				Assert.IsNotNull(context.GetService(typeof(IXamlTypeResolver)), "#20");
				Assert.IsNotNull(context.GetService(typeof(IXamlSchemaContextProvider)), "#21");
				Assert.IsNotNull(context.GetService(typeof(IAmbientProvider)), "#22");
				Assert.IsNull(context.GetService(typeof(IAttachedPropertyStore)), "#23");
				Assert.IsNotNull(context.GetService(typeof(IDestinationTypeProvider)), "#24");
				Assert.IsNotNull(context.GetService(typeof(IXamlObjectWriterFactory)), "#25");
				ranConvertFrom = true;
			};
			SetupWriterService();
			Assert.IsTrue(ranConvertFrom, "#26");
			Assert.IsTrue(ranCanConvertFrom, "#27");
		}

		[Test]
		public void NameResolver()
		{
			bool ranConvertFrom = false;
			runConvertFrom = (context, culture, sourceType) =>
			{
				var nr = (IXamlNameResolver)context.GetService(typeof(IXamlNameResolver));
				Assert.IsNull(nr.Resolve("random"), "nr#1");
				//var ft = nr.GetFixupToken (new string [] {"random"}); -> causes internal error.
				//var ft = nr.GetFixupToken (new string [] {"random"}, true); -> causes internal error
				//var ft = nr.GetFixupToken (new string [0], false);
				//Assert.IsNotNull (ft, "nr#2");
				ranConvertFrom = true;
			};

			SetupWriterService();

			Assert.IsTrue(ranConvertFrom, "#2");
		}
	}
}
