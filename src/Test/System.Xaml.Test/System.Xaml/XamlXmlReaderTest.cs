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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using System.Windows.Markup;
#if PCL

using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.ComponentModel;
using System.Xaml;
using System.Xaml.Schema;
#endif

using CategoryAttribute = NUnit.Framework.CategoryAttribute;
using XamlReader = System.Xaml.XamlReader;
using XamlParseException = System.Xaml.XamlParseException;

namespace MonoTests.System.Xaml
{
	[TestFixture]
	public class XamlXmlReaderTest : XamlReaderTestBase
	{
		// read test

		XamlReader GetReader(string filename, XamlXmlReaderSettings settings = null)
		{
			string xml = File.ReadAllText(Compat.GetTestFile(filename)).UpdateXml();
			return new XamlXmlReader(new StringReader(xml), new XamlSchemaContext(), settings);
		}

		XamlReader GetReaderText(string xml, XamlXmlReaderSettings settings = null)
		{
			xml = xml.UpdateXml();
			return new XamlXmlReader(new StringReader(xml), new XamlSchemaContext(), settings);
		}

		void ReadTest(string filename)
		{
			var r = GetReader(filename);
			while (!r.IsEof)
				r.Read();
		}

		[Test]
		public void SchemaContext ()
		{
			Assert.AreNotEqual (XamlLanguage.Type.SchemaContext, new XamlXmlReader (XmlReader.Create (new StringReader ("<root/>"))).SchemaContext, "#1");
		}

		[Test]
		public void Read_Int32 ()
		{
			ReadTest ("Int32.xml");
		}

		[Test]
		public void Read_DateTime ()
		{
			ReadTest ("DateTime.xml");
		}

		[Test]
		public void Read_TimeSpan ()
		{
			ReadTest ("TimeSpan.xml");
		}

		[Test]
		public void Read_ArrayInt32 ()
		{
			ReadTest ("Array_Int32.xml");
		}

		[Test]
		public void Read_DictionaryInt32String ()
		{
			ReadTest ("Dictionary_Int32_String.xml");
		}

		[Test]
		public void Read_DictionaryStringType ()
		{
			ReadTest ("Dictionary_String_Type.xml");
		}

		[Test]
		public void Read_SilverlightApp1 ()
		{
			ReadTest ("SilverlightApp1.xaml");
		}

		[Test]
		public void Read_Guid ()
		{
			ReadTest ("Guid.xml");
		}

		[Test]
		public void Read_GuidFactoryMethod ()
		{
			ReadTest ("GuidFactoryMethod.xml");
		}

		[Test]
		public void ReadInt32Details ()
		{
			var r = GetReader ("Int32.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#3");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			Assert.AreEqual (XamlLanguage.Int32, r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sinit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sinit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sinit#3");

			Assert.IsTrue (r.Read (), "vinit#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vinit#2");
			Assert.AreEqual ("5", r.Value, "vinit#3"); // string

			Assert.IsTrue (r.Read (), "einit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "einit#2");

			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");

			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadDateTimeDetails ()
		{
			var r = GetReader ("DateTime.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual ("clr-namespace:System;assembly=System.Private.CoreLib", r.Namespace.Namespace, "ns#3");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (DateTime)), r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sinit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sinit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sinit#3");

			Assert.IsTrue (r.Read (), "vinit#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vinit#2");
			Assert.AreEqual ("2010-04-14", r.Value, "vinit#3"); // string

			Assert.IsTrue (r.Read (), "einit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "einit#2");

			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");
			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadGuidFactoryMethodDetails ()
		{
			var r = GetReader ("GuidFactoryMethod.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual ("clr-namespace:System;assembly=System.Private.CoreLib", r.Namespace.Namespace, "ns#3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#4");

			Assert.IsTrue (r.Read (), "ns2#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns2#2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns2#3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns2#4");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			var xt = r.SchemaContext.GetXamlType (typeof (Guid));
			Assert.AreEqual (xt, r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sfactory#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sfactory#2");
			Assert.AreEqual (XamlLanguage.FactoryMethod, r.Member, "sfactory#3");

			Assert.IsTrue (r.Read (), "vfactory#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vfactory#2");
			Assert.AreEqual ("Parse", r.Value, "vfactory#3"); // string

			Assert.IsTrue (r.Read (), "efactory#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "efactory#2");

			Assert.IsTrue (r.Read (), "sarg#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sarg#2");
			Assert.AreEqual (XamlLanguage.Arguments, r.Member, "sarg#3");

			Assert.IsTrue (r.Read (), "sarg1#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "sarg1#2");
			Assert.AreEqual (XamlLanguage.String, r.Type, "sarg1#3");

			Assert.IsTrue (r.Read (), "sInit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sInit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sInit#3");

			Assert.IsTrue (r.Read (), "varg1#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "varg1#2");
			Assert.AreEqual ("9c3345ec-8922-4662-8e8d-a4e41f47cf09", r.Value, "varg1#3");

			Assert.IsTrue (r.Read (), "eInit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eInit#2");

			Assert.IsTrue (r.Read (), "earg1#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "earg1#2");

			Assert.IsTrue (r.Read (), "earg#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "earg#2");


			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");

			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadEventStore ()
		{
			var r = GetReader ("EventStore2.xml");

			var xt = r.SchemaContext.GetXamlType (typeof (EventStore));
			var xm = xt.GetMember ("Event1");
			Assert.IsNotNull (xt, "premise#1");
			Assert.IsNotNull (xm, "premise#2");
			Assert.IsTrue (xm.IsEvent, "premise#3");
			while (true) {
				r.Read ();
				if (r.Member != null && r.Member.IsEvent)
					break;
				if (r.IsEof)
					Assert.Fail ("Items did not appear");
			}

			Assert.AreEqual (xm, r.Member, "#x1");
			Assert.AreEqual ("Event1", r.Member.Name, "#x2");

			Assert.IsTrue (r.Read (), "#x11");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x12");
			Assert.AreEqual ("Method1", r.Value, "#x13");

			Assert.IsTrue (r.Read (), "#x21");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x22");

			xm = xt.GetMember ("Event2");
			Assert.IsTrue (r.Read (), "#x31");
			Assert.AreEqual (xm, r.Member, "#x32");
			Assert.AreEqual ("Event2", r.Member.Name, "#x33");

			Assert.IsTrue (r.Read (), "#x41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x42");
			Assert.AreEqual ("Method2", r.Value, "#x43");

			Assert.IsTrue (r.Read (), "#x51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x52");

			Assert.IsTrue (r.Read (), "#x61");
			Assert.AreEqual ("Event1", r.Member.Name, "#x62");

			Assert.IsTrue (r.Read (), "#x71");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x72");
			Assert.AreEqual ("Method3", r.Value, "#x73"); // nonexistent, but no need to raise an error.

			Assert.IsTrue (r.Read (), "#x81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x82");

			while (!r.IsEof)
				r.Read ();

			r.Close ();
		}

		// common XamlReader tests.

		[Test]
		public void Read_String ()
		{
			var r = GetReader ("String.xml");
			Read_String (r);
		}

		[Test]
		public void WriteNullMemberAsObject ()
		{
			var r = GetReader ("TestClass4.xml");
			WriteNullMemberAsObject (r, null);
		}
		
		[Test]
		public void StaticMember ()
		{
			var r = GetReader ("TestClass5.xml");
			StaticMember (r);
		}

		[Test]
		public void Skip ()
		{
			var r = GetReader ("String.xml");
			Skip (r);
		}
		
		[Test]
		public void Skip2 ()
		{
			var r = GetReader ("String.xml");
			Skip2 (r);
		}

		[Test]
		[Category (Categories.NotWorking)] // Doesn't work on MS.NET due to inability to load System.Xml in PCL
		public void Read_XmlDocument ()
		{
			var doc = new XmlDocument ();
			doc.LoadXml ("<root xmlns='urn:foo'><elem attr='val' /></root>");
			// note that corresponding XamlXmlWriter is untested yet.
			var r = GetReader ("XmlDocument.xml");
			Read_XmlDocument (r);
		}

		[Test]
		public void Read_NonPrimitive ()
		{
			var r = GetReader ("NonPrimitive.xml");
			Read_NonPrimitive (r);
		}
		
		[Test]
		public void Read_TypeExtension ()
		{
			var r = GetReader ("Type.xml");
			Read_TypeOrTypeExtension (r, null, XamlLanguage.Type.GetMember ("Type"));
		}
		
		[Test]
		public void Read_Type2 ()
		{
			var r = GetReader ("Type2.xml");
			Read_TypeOrTypeExtension2 (r, null, XamlLanguage.Type.GetMember ("Type"));
		}
		
		[Test]
		public void Read_Reference ()
		{
			var r = GetReader ("Reference.xml");
			Read_Reference (r);
		}
		
		[Test]
		public void Read_Null ()
		{
			var r = GetReader ("NullExtension.xml");
			Read_NullOrNullExtension (r, null);
		}
		
		[Test]
		public void Read_StaticExtension ()
		{
			var r = GetReader ("StaticExtension.xml");
			Read_StaticExtension (r, XamlLanguage.Static.GetMember ("Member"));
		}
		
		[Test]
		public void Read_ListInt32 ()
		{
			var r = GetReader ("List_Int32.xml");
			Read_ListInt32 (r, null, new int [] {5, -3, int.MaxValue, 0}.ToList ());
		}
		
		[Test]
		public void Read_ListInt32_2 ()
		{
			var r = GetReader ("List_Int32_2.xml");
			Read_ListInt32 (r, null, new int [0].ToList ());
		}
		
		[Test]
		public void Read_ListType ()
		{
			var r = GetReader ("List_Type.xml");
			Read_ListType (r, false, false);
		}

		[Test]
		public void Read_ListArray ()
		{
			var r = GetReader ("List_Array.xml");
			Read_ListArray (r);
		}

		[Test]
		public void Read_ArrayList ()
		{
			var r = GetReader ("ArrayList.xml");
			Read_ArrayList (r);
		}
		
		[Test]
		public void Read_Array ()
		{
			var r = GetReader ("ArrayExtension.xml");
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, null, typeof (ArrayExtension));
		}
		
		[Test]
		public void Read_MyArrayExtension ()
		{
			var r = GetReader ("MyArrayExtension.xml");
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, null, typeof (MyArrayExtension));
		}

		[Test]
		public void Read_ArrayExtension2 ()
		{
			var r = GetReader ("ArrayExtension2.xml");
			Read_ArrayExtension2 (r);
		}

		[Test]
		public void Read_CustomMarkupExtension ()
		{
			var r = GetReader ("MyExtension.xml");
			Read_CustomMarkupExtension (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension2 ()
		{
			var r = GetReader ("MyExtension2.xml");
			Read_CustomMarkupExtension2 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension3 ()
		{
			var r = GetReader ("MyExtension3.xml");
			Read_CustomMarkupExtension3 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension4 ()
		{
			var r = GetReader ("MyExtension4.xml");
			Read_CustomMarkupExtension4 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension6 ()
		{
			var r = GetReader ("MyExtension6.xml");
			Read_CustomMarkupExtension6 (r);
		}

		[Test]
		public void Read_CustomExtensionWithPositionalChildExtension ()
		{
			var r = GetReader ("CustomExtensionWithPositionalChild.xml");
			Read_CustomExtensionWithPositionalChildExtension(r);
		}

		[Test]
		public void Read_CustomExtensionWithChildExtensionAndNamedProperty ()
		{
			var r = GetReader ("CustomExtensionWithChildExtensionAndNamedProperty.xml");
			Read_CustomExtensionWithChildExtensionAndNamedProperty(r);
		}

		[Test]
		public void Read_CustomExtensionWithChildExtension()
		{
			var r = GetReader("CustomExtensionWithChild.xml");
			Read_CustomExtensionWithChildExtension(r);
		}

		[Test]
		public void Read_CustomExtensionWithCommasInPositionalValue()
		{
			var r = GetReader("CustomExtensionWithCommasInPositionalValue.xml");
			Read_CustomExtensionWithCommasInPositionalValue(r);
		}

		[Test]
		public void Read_CustomExtensionWithStringFormat()
		{
			var r = GetReaderText(@"<ValueWrapper 
	StringValue='{MyExtension2 Bar=Hello {0}}' 
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
	xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'/>");

			r.Read(); // ns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);
			r.Read(); // ns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);
			r.Read();
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			var xt = r.Type;
			Assert.AreEqual(r.SchemaContext.GetXamlType(typeof(ValueWrapper)), xt);

			if (r is XamlXmlReader)
				ReadBase(r);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(xt.GetMember("StringValue"), r.Member);
			Assert.IsTrue(r.Read(), "#5");
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(r.SchemaContext.GetXamlType(typeof(MyExtension2)), xt = r.Type);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(xt.GetMember("Bar"), r.Member);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("Hello {0}", r.Value);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read());
			Assert.AreEqual(XamlNodeType.None, r.NodeType);
			Assert.IsTrue(r.IsEof);
		}

		[Test]
		public void Read_CustomExtensionWithStringFormatEscape()
		{
			var r = GetReaderText(@"<ValueWrapper 
	StringValue='{MyExtension2 Bar={}{0} Hello}' 
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
	xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'/>");

			r.Read(); // ns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);
			r.Read(); // ns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);
			r.Read();
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			var xt = r.Type;
			Assert.AreEqual(r.SchemaContext.GetXamlType(typeof(ValueWrapper)), xt);

			if (r is XamlXmlReader)
				ReadBase(r);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(xt.GetMember("StringValue"), r.Member);
			Assert.IsTrue(r.Read(), "#5");
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(r.SchemaContext.GetXamlType(typeof(MyExtension2)), xt = r.Type);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(xt.GetMember("Bar"), r.Member);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("{0} Hello", r.Value);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);
			Assert.IsTrue(r.Read());
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read());
			Assert.AreEqual(XamlNodeType.None, r.NodeType);
			Assert.IsTrue(r.IsEof);
		}

		[Test]
        public void Load_CustomExtensionWithEscapeChars()
        {
            var r = GetReader("CustomExtensionWithEscapeChars.xml");
            Load_CustomExtensionWithEscapeChars(r);
        }

        [Test]
		public void Read_CustomExtensionWithPositionalAndNamed()
		{
			var r = GetReader("CustomExtensionWithPositionalAndNamed.xml");
			Read_CustomExtensionWithPositionalAndNamed(r);
		}

		[Test]
		public void Read_CustomExtensionWithCommasInNamedValue()
		{
			var r = GetReader("CustomExtensionWithCommasInNamedValue.xml");
			Read_CustomExtensionWithCommasInNamedValue(r);
		}

		[Test]
		public void Read_CustomExtensionWithPositionalAndNamedWithChild()
		{
			var xml = @"
<ValueWrapper 
	StringValue='{MyExtension8 SomeValue, Bar={x:Type x:String}}' 
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
	xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases' />
".UpdateXml();
			var result = (ValueWrapper)XamlServices.Parse(xml);
		}

		[Test]
		public void Read_CustomExtensionWithPositonalAfterExplicitProperty()
		{
			// cannot have positional property after named property
			Assert.Throws<XamlParseException>(() =>
			{
				var r = GetReader("CustomExtensionWithPositonalAfterExplicitProperty.xml");
				Read_CustomExtensionWithPositonalAfterExplicitProperty(r);
			});
		}

		[Test]
		public void Read_CustomExtensionNotFound()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<TestClass4 xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'
									  Foo='{{NotFound}}'/>";
			var r = GetReaderText(xaml);

			r.Read(); // xmlns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);

			r.Read(); // <TestClass4>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);

			ReadBase(r);

			r.Read(); // StartMember (Foo)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(typeof(TestClass4), r.Member.DeclaringType.UnderlyingType);
			Assert.AreEqual(nameof(TestClass4.Foo), r.Member.Name);

			r.Read(); // StartObject (NotFound)
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.True(r.Type.IsUnknown);
			Assert.AreEqual("NotFound", r.Type.Name);
			Assert.AreEqual($"clr-namespace:MonoTests.System.Xaml;assembly={assembly}", r.Type.PreferredXamlNamespace);

			r.Read(); // EndObject (NotFound)
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // EndMember (foo)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // EndObject (TestClass4)
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.False(r.Read());
		}

		[Test]
		public void Looks_Up_Correct_Markup_Extension_Type_Names()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<TestClass4 xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'
								      Foo='{{Example}}'/>";
			var ctx = new TestSchemaContext("ExampleExtension");
			var reader = new XamlXmlReader(new StringReader(xaml), ctx);

			while (reader.Read()) ;

			Assert.AreEqual(new[] { "TestClass4", "ExampleExtension", "Example" }, ctx.RequestedTypeNames);
		}

		[Test]
		public void Looks_Up_Correct_Markup_Extension_Type_Names2()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<TestClass4 xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'
								      Foo='{{ExampleExtension}}'/>";
			var ctx = new TestSchemaContext("ExampleExtension");
			var reader = new XamlXmlReader(new StringReader(xaml), ctx);

			while (reader.Read()) ;

			Assert.AreEqual(new[] { "TestClass4", "ExampleExtensionExtension", "ExampleExtension" }, ctx.RequestedTypeNames);
		}

		[Test]
		public void Read_ArgumentAttributed ()
		{
			var obj = new ArgumentAttributed ("foo", "bar");
			var r = GetReader ("ArgumentAttributed.xml");
			Read_ArgumentAttributed (r, obj);
		}

		[Test]
		public void Read_Dictionary ()
		{
			var obj = new Dictionary<string,object> ();
			obj ["Foo"] = 5.0;
			obj ["Bar"] = -6.5;
			obj ["Woo"] = 123.45d;
			var r = GetReader ("Dictionary_String_Double.xml");
			Read_Dictionary (r, true);
		}
		
		[Test]
		public void Read_Dictionary2 ()
		{
			var obj = new Dictionary<string,Type> ();
			obj ["Foo"] = typeof (int);
			obj ["Bar"] = typeof (Dictionary<Type,XamlType>);
			var r = GetReader ("Dictionary_String_Type_2.xml");
			Read_Dictionary2 (r, XamlLanguage.Type.GetMember ("Type"), false);
		}
		
		[Test]
		public void PositionalParameters2 ()
		{
			var r = GetReader ("PositionalParametersWrapper.xml");
			PositionalParameters2 (r);
		}

		[Test]
		public void ComplexPositionalParameters ()
		{
			var r = GetReader ("ComplexPositionalParameterWrapper.xml");
			ComplexPositionalParameters (r);
		}
		
		[Test]
		public void Read_ListWrapper ()
		{
			var r = GetReader ("ListWrapper.xml");
			Read_ListWrapper (r);
		}
		
		[Test]
		public void Read_ListWrapper2 () // read-write list member.
		{
			var r = GetReader ("ListWrapper2.xml");
			Read_ListWrapper2 (r);
		}

		[Test]
		public void Read_ContentIncluded ()
		{
			var r = GetReader ("ContentIncluded.xml");
			Read_ContentIncluded (r);
		}

		[Test]
		public void Read_PropertyDefinition ()
		{
			var r = GetReader ("PropertyDefinition.xml");
			Read_PropertyDefinition (r);
		}

		[Test]
		public void Read_StaticExtensionWrapper ()
		{
			var r = GetReader ("StaticExtensionWrapper.xml");
			Read_StaticExtensionWrapper (r);
		}

		[Test]
		public void Read_TypeExtensionWrapper ()
		{
			var r = GetReader ("TypeExtensionWrapper.xml");
			Read_TypeExtensionWrapper (r);
		}

		[Test]
		public void Read_NamedItems ()
		{
			var r = GetReader ("NamedItems.xml");
			Read_NamedItems (r, false);
		}

		[Test]
		public void Read_NamedItems2 ()
		{
			var r = GetReader ("NamedItems2.xml");
			Read_NamedItems2 (r, false);
		}

		[Test]
		public void Read_XmlSerializableWrapper ()
		{
			var r = GetReader ("XmlSerializableWrapper.xml");
			Read_XmlSerializableWrapper (r, false);
		}

		[Test]
		public void Read_XmlSerializable ()
		{
			var r = GetReader ("XmlSerializable.xml");
			Read_XmlSerializable (r);
		}

		[Test]
		public void Read_ListXmlSerializable ()
		{
			var r = GetReader ("List_XmlSerializable.xml");
			Read_ListXmlSerializable (r);
		}

		[Test]
		public void Read_AttachedProperty ()
		{
			var r = GetReader ("AttachedProperty.xml");
			Read_AttachedProperty (r);
		}

		[Test]
		public void Read_AttachedPropertyWithNamespace()
		{
			var r = GetReader("AttachedPropertyWithNamespace.xml");
			var ns = "clr-namespace:MonoTests.System.Xaml;assembly=" + GetType().GetTypeInfo().Assembly.GetName().Name;
			Read_AttachedProperty(r, ns);
		}

		[Test]
		public void Read_AttachedPropertyOnClassWithDifferentNamespace()
		{
			var r = GetReader("AttachedPropertyOnClassWithDifferentNamespace.xml");
			var ns = "clr-namespace:MonoTests.System.Xaml.NamespaceTest2;assembly=" + GetType().GetTypeInfo().Assembly.GetName().Name;
			Read_AttachedProperty(r, ns, typeof(NamespaceTest2.AttachedWrapperWithDifferentBaseNamespace));
		}

		[Test]
		public void Read_AbstractWrapper ()
		{
			var r = GetReader ("AbstractContainer.xml");
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void Read_ReadOnlyPropertyContainer ()
		{
			var r = GetReader ("ReadOnlyPropertyContainer.xml");
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void Read_TypeConverterOnListMember ()
		{
			var r = GetReader ("TypeConverterOnListMember.xml");
			Read_TypeConverterOnListMember (r);
		}

		[Test]
		public void Read_EnumContainer ()
		{
			var r = GetReader ("EnumContainer.xml");
			Read_EnumContainer (r);
		}

		[Test]
		public void Read_CollectionContentProperty ()
		{
			var r = GetReader ("CollectionContentProperty.xml");
			Read_CollectionContentProperty (r, false);
		}

		[Test]
		public void Read_CollectionContentProperty2 ()
		{
			// bug #681835
			var r = GetReader ("CollectionContentProperty2.xml");
			Read_CollectionContentProperty (r, true);
		}

		[Test]
		public void Read_CollectionContentPropertyX ()
		{
			var r = GetReader ("CollectionContentPropertyX.xml");
			Read_CollectionContentPropertyX (r, false);
		}

		[Test]
		public void Read_CollectionContentPropertyX2 ()
		{
			var r = GetReader ("CollectionContentPropertyX2.xml");
			Read_CollectionContentPropertyX (r, true);
		}

		[Test]
		public void Read_AmbientPropertyContainer ()
		{
			var r = GetReader ("AmbientPropertyContainer.xml");
			Read_AmbientPropertyContainer (r, false);
		}

		[Test]
		public void Read_AmbientPropertyContainer2 ()
		{
			var r = GetReader ("AmbientPropertyContainer2.xml");
			Read_AmbientPropertyContainer (r, true);
		}

		[Test]
		public void Read_AmbientPropertyContainer3()
		{
			var r = GetReader("AmbientPropertyContainer3.xml");
			var writer = new XamlObjectWriter(new XamlSchemaContext());
			XamlServices.Transform(r, writer);
			//Read_AmbientPropertyContainer3(r, true);
		}

		/// <summary>
		/// Test ambient properties on a subclass of the container that defines the ambient property
		/// </summary>
		[Test]
		public void Read_AmbientPropertyContainer4()
		{
			var r = GetReader("AmbientPropertyContainer4.xml");
			var writer = new XamlObjectWriter(new XamlSchemaContext());
			XamlServices.Transform(r, writer);
		}

		[Test]
		public void Read_NullableContainer ()
		{
			var r = GetReader ("NullableContainer.xml");
			Read_NullableContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectListContainer ()
		{
			var r = GetReader ("DirectListContainer.xml");
			Read_DirectListContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectDictionaryContainer ()
		{
			var r = GetReader ("DirectDictionaryContainer.xml");
			Read_DirectDictionaryContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectDictionaryContainer2 ()
		{
			var r = GetReader ("DirectDictionaryContainer2.xml");
			Read_DirectDictionaryContainer2 (r);
		}
		
		[Test]
		public void Read_ContentPropertyContainer ()
		{
			var r = GetReader ("ContentPropertyContainer.xml");
			Read_ContentPropertyContainer (r);
		}

		/// <summary>
		/// Tests that when reading a content item element with the same name as a property of the parent
		/// </summary>
		[Test]
		public void Read_ContentObjectSameAsPropertyName ()
		{
			var xaml = @"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'><OtherItem/></CollectionParentItem>".UpdateXml ();
			var parent = (CollectionParentItem)XamlServices.Load (new StringReader (xaml));

			Assert.IsNotNull (parent, "#1");
			Assert.IsInstanceOf<CollectionParentItem> (parent, "#2");
			Assert.AreEqual (1, parent.Items.Count, "#3");
			var item = parent.Items.FirstOrDefault ();
			Assert.IsNotNull (item, "#4");
			Assert.AreEqual ("FromOther", item.Name, "#5");
		}

		/// <summary>
		/// Tests that when reading a content item element with the same name as a property of the parent
		/// </summary>
		[Test]
		public void Read_ContentObjectSameAsPropertyName2 ()
		{
			var xaml = @"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'><CollectionItem Name='Direct'/></CollectionParentItem>".UpdateXml ();
			var parent = (CollectionParentItem)XamlServices.Load (new StringReader (xaml));

			Assert.IsNotNull (parent, "#1");
			Assert.IsInstanceOf<CollectionParentItem> (parent, "#2");
			Assert.AreEqual (1, parent.Items.Count, "#3");
			var item = parent.Items.FirstOrDefault ();
			Assert.IsNotNull (item, "#4");
			Assert.AreEqual ("Direct", item.Name, "#5");
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)] // System.Xaml doesn't use typeconverters nor passes the value
		public void Read_CollectionWithContentWithConverter()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("System.Xaml doesn't use typeconverters nor passes the value");

			var xaml = @"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'><CollectionItem Name='Item1'/>SomeContent</CollectionParentItem>".UpdateXml();
			var parent = (CollectionParentItem)XamlServices.Load(new StringReader(xaml));

			Assert.IsNotNull(parent, "#1");
			Assert.IsInstanceOf<CollectionParentItem>(parent, "#2");
			Assert.AreEqual(2, parent.Items.Count, "#3");
			var item = parent.Items[0];
			Assert.IsNotNull(item, "#4");
			Assert.AreEqual("Item1", item.Name, "#5");
			item = parent.Items[1];
			Assert.IsNotNull(item, "#6");
			Assert.AreEqual("SomeContent", item.Name, "#7");
		}

		#region non-common tests
		[Test]
		public void Bug680385 ()
		{
			#if PCL136
			XamlServices.Load (new StreamReader(Compat.GetTestFile("CurrentVersion.xaml")));
			#else
			XamlServices.Load (Compat.GetTestFile("CurrentVersion.xaml"));
			#endif
		}
		#endregion

		[Test]
		public void LocalAssemblyShouldApplyToNamespace()
		{
			var settings = new XamlXmlReaderSettings();
			settings.LocalAssembly = typeof(TestClass1).GetTypeInfo().Assembly;
			string xml = File.ReadAllText(Compat.GetTestFile ("LocalAssembly.xml")).UpdateXml();
			var obj = XamlServices.Load(new XamlXmlReader(new StringReader(xml), settings));
			Assert.IsNotNull(obj, "#1");
			Assert.IsInstanceOf<TestClass1>(obj, "#2");
		}

		[Test]
		// not checking type of exception due to differences in implementation 
		public void LocalAssemblyShouldNotApplyToNamespace()
		{
			var settings = new XamlXmlReaderSettings();
			string xml = File.ReadAllText(Compat.GetTestFile ("LocalAssembly.xml")).UpdateXml();
#if PCL
			var exType = typeof(XamlParseException);
#else
			var exType = typeof(XamlObjectWriterException);
#endif
			Assert.Throws(exType, () => {
				var obj = XamlServices.Load (new XamlXmlReader (new StringReader (xml), settings));
				Assert.IsNotNull (obj, "#1");
				Assert.IsInstanceOf<TestClass1> (obj, "#2");
			});
		}

		[Test]
		public void Read_NumericValues()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(123.456, obj.DoubleValue, "#2");
			Assert.AreEqual(234.567M, obj.DecimalValue, "#3");
			Assert.AreEqual(345.678f, obj.FloatValue, "#4");
			Assert.AreEqual(123, obj.ByteValue, "#5");
			Assert.AreEqual(123456, obj.IntValue, "#6");
			Assert.AreEqual(234567, obj.LongValue, "#7");
		}

		[Test]
		public void Read_NumericValues_Max()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues_Max.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(double.MaxValue, obj.DoubleValue, "#2");
			Assert.AreEqual(decimal.MaxValue, obj.DecimalValue, "#3");
			Assert.AreEqual(float.MaxValue, obj.FloatValue, "#4");
			Assert.AreEqual(byte.MaxValue, obj.ByteValue, "#5");
			Assert.AreEqual(int.MaxValue, obj.IntValue, "#6");
			Assert.AreEqual(long.MaxValue, obj.LongValue, "#7");
		}

		[Test]
		public void Read_NumericValues_PositiveInfinity()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues_PositiveInfinity.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(double.PositiveInfinity, obj.DoubleValue, "#2");
			Assert.AreEqual(0, obj.DecimalValue, "#3");
			Assert.AreEqual(float.PositiveInfinity, obj.FloatValue, "#4");
			Assert.AreEqual(0, obj.ByteValue, "#5");
			Assert.AreEqual(0, obj.IntValue, "#6");
			Assert.AreEqual(0, obj.LongValue, "#7");
		}

		[Test]
		public void Read_NumericValues_NegativeInfinity()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues_NegativeInfinity.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(double.NegativeInfinity, obj.DoubleValue, "#2");
			Assert.AreEqual(0, obj.DecimalValue, "#3");
			Assert.AreEqual(float.NegativeInfinity, obj.FloatValue, "#4");
			Assert.AreEqual(0, obj.ByteValue, "#5");
			Assert.AreEqual(0, obj.IntValue, "#6");
			Assert.AreEqual(0, obj.LongValue, "#7");
		}

		[Test]
		public void Read_NumericValues_NaN()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues_NaN.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(double.NaN, obj.DoubleValue, "#2");
			Assert.AreEqual(0, obj.DecimalValue, "#3");
			Assert.AreEqual(float.NaN, obj.FloatValue, "#4");
			Assert.AreEqual(0, obj.ByteValue, "#5");
			Assert.AreEqual(0, obj.IntValue, "#6");
			Assert.AreEqual(0, obj.LongValue, "#7");
		}

		[Test]
		public void Read_DefaultNamespaces_ClrNamespace()
		{
#if PCL
			var settings = new XamlXmlReaderSettings();
			settings.AddNamespace(null, Compat.TestAssemblyNamespace);
			settings.AddNamespace("x", XamlLanguage.Xaml2006Namespace);
			var obj = (TestClass5)XamlServices.Load(GetReader("DefaultNamespaces.xml", settings));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(obj.Bar, "Hello");
			Assert.AreEqual(obj.Baz, null);
#else
			Assert.Ignore("Not supported in System.Xaml");
#endif
		}

		[Test]
		public void Read_DefaultNamespaces_WithDefinedNamespace()
		{
#if PCL
			var settings = new XamlXmlReaderSettings();
			settings.AddNamespace(null, "urn:mono-test");
			settings.AddNamespace("x", "urn:mono-test2");
			var obj = (NamespaceTest.NamespaceTestClass)XamlServices.Load(GetReader("DefaultNamespaces_WithDefinedNamespace.xml", settings));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(obj.Foo, "Hello");
			Assert.AreEqual(obj.Bar, null);
#else
			Assert.Ignore("Not supported in System.Xaml");
#endif
		}

		[Test]
		public void Read_NumericValues_StandardTypes()
		{
			var obj = (NumericValues)XamlServices.Load(GetReader("NumericValues_StandardTypes.xml"));
			Assert.IsNotNull(obj, "#1");
			Assert.AreEqual(123.456, obj.DoubleValue, "#2");
			Assert.AreEqual(234.567M, obj.DecimalValue, "#3");
			Assert.AreEqual(345.678f, obj.FloatValue, "#4");
			Assert.AreEqual(123, obj.ByteValue, "#5");
			Assert.AreEqual(123456, obj.IntValue, "#6");
			Assert.AreEqual(234567, obj.LongValue, "#7");
		}

		[Test]
		public void Read_BaseClassPropertiesInSeparateNamespace()
		{
			var obj = (NamespaceTest2.TestClassWithDifferentBaseNamespace)XamlServices.Load(GetReader("BaseClassPropertiesInSeparateNamespace.xml"));
			Assert.IsNotNull(obj);
			Assert.AreEqual("MyName", obj.TheName);
			Assert.AreEqual("OtherValue", obj.SomeOtherProperty);
			Assert.AreEqual("TheBar", obj.Bar);
			Assert.IsNull(obj.Baz);
		}

		[Test]
		public void Read_BaseClassPropertiesInSeparateNamespace_WithChildren()
		{
			var obj = (NamespaceTest2.TestClassWithDifferentBaseNamespace)XamlServices.Load(GetReader("BaseClassPropertiesInSeparateNamespace_WithChildren.xml"));
			Assert.IsNotNull(obj);
			Assert.AreEqual("MyName", obj.TheName);
			Assert.AreEqual("OtherValue", obj.SomeOtherProperty);
			Assert.AreEqual("TheBar", obj.Bar);
			Assert.IsNull(obj.Baz);
			Assert.IsNotNull(obj.Other);
			Assert.AreEqual("TheBar2", obj.Other.Bar);
		}

		[Test]
		public void Read_InvalidPropertiesShouldBeRead()
		{
			var xaml = @"<TestClassWithDifferentBaseNamespace UnknownProperty=""Woo"" xmlns=""urn:mono-test2""/>";
			var reader = GetReaderText(xaml);
			Assert.IsTrue(reader.Read());
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, reader.NodeType);
			Assert.AreEqual("urn:mono-test2", reader.Namespace.Namespace);

			XamlType xt;
			Assert.IsTrue(reader.Read());
			Assert.AreEqual(XamlNodeType.StartObject, reader.NodeType);
			Assert.AreEqual(xt = reader.SchemaContext.GetXamlType(typeof(MonoTests.System.Xaml.NamespaceTest2.TestClassWithDifferentBaseNamespace)), reader.Type);

			ReadBase(reader);

			Assert.IsTrue(reader.Read());
			Assert.AreEqual(XamlNodeType.StartMember, reader.NodeType);
			Assert.AreEqual("UnknownProperty", reader.Member.Name);
			Assert.IsTrue(reader.Member.IsUnknown);
		}

		[Test]
		public void Read_InvalidPropertiesShouldBeRead2()
		{
			var xaml = @"<TestClassWithDifferentBaseNamespace base:UnknownProperty=""Woo"" xmlns=""urn:mono-test2"" xmlns:base=""clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases""/>";
			var reader = GetReaderText(xaml);

			ReadNamespace(reader, string.Empty, "urn:mono-test2", "");

			var ns = "clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases".UpdateXml();
			ReadNamespace(reader, "base", ns, "");

			XamlType xt;
			Assert.IsTrue(reader.Read());
			Assert.AreEqual(XamlNodeType.StartObject, reader.NodeType);
			Assert.AreEqual(xt = reader.SchemaContext.GetXamlType(typeof(MonoTests.System.Xaml.NamespaceTest2.TestClassWithDifferentBaseNamespace)), reader.Type);

			ReadBase(reader);

			Assert.IsTrue(reader.Read());
			Assert.AreEqual(XamlNodeType.StartMember, reader.NodeType);
			Assert.AreEqual("UnknownProperty", reader.Member.Name);
			Assert.AreEqual(ns, reader.Member.PreferredXamlNamespace);
			Assert.IsTrue(reader.Member.IsUnknown);
		}

		[Test]
		public void Read_EscapedPropertyValue()
		{
			var r = GetReader("EscapedPropertyValue.xml");
			var ctx = r.SchemaContext;
			ReadNamespace(r, string.Empty, Compat.TestAssemblyNamespace, "#1");
			ReadObject(r, ctx.GetXamlType(typeof(TestClass5)), "#2", xt =>
			{
				ReadBase(r);
				ReadMember(r, xt.GetMember("Bar"), "#3", xm =>
				{
					ReadValue(r, "{ Some Value That Should Be Escaped", "#4");
				});
			});
		}

		/// <summary>
		/// Tests that unexpected object members are enclosed in the x:_UnknownContent intrinsic member (rather than just ignored).
		/// </summary>
		[Test]
		public void Read_UnknownContent()
		{
			var xaml = @"<TestClass1 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'><TestClass3/><TestClass4/></TestClass1>".UpdateXml ();
			var reader = GetReaderText(xaml);

			reader.Read(); // xmlns
			Assert.AreEqual(reader.NodeType, XamlNodeType.NamespaceDeclaration);

			reader.Read(); // <TestClass1>
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartObject);

			ReadBase(reader);

			reader.Read(); // StartMember (x:_UnknownContent)
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartMember);
			Assert.AreEqual(reader.Member, XamlLanguage.UnknownContent);

			reader.Read(); // <TestClass3>
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartObject);
			Assert.AreEqual(reader.Type, reader.SchemaContext.GetXamlType(typeof(TestClass3)));

			reader.Read(); // </TestClass3>
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndObject);	
			
			reader.Read(); // <TestClass4>
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartObject);
			Assert.AreEqual(reader.Type, reader.SchemaContext.GetXamlType(typeof(TestClass4)));

			reader.Read(); // </TestClass4>
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndObject);

			reader.Read(); // EndMember (x:_UnknownContent)
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndMember);

			reader.Read(); // </TestClass1>
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndObject);

			Assert.IsFalse(reader.Read()); // EOF
		}

		/// <summary>
		/// Tests that a property marked with [XamlDeferLoad] whose actual type is not compatible with the deferred content
		/// produces a valid XAML node list.
		/// </summary>
		[Test]
		public void Read_DeferLoadedProperty()
		{
			var xaml = File.ReadAllText(Compat.GetTestFile("DeferredLoadingContainerMember2.xml")).UpdateXml();
			var reader = GetReaderText(xaml);

			reader.Read(); // xmlns
			Assert.AreEqual(reader.NodeType, XamlNodeType.NamespaceDeclaration);

			reader.Read(); // <DeferredLoadingContainerMember2>
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartObject);

			ReadBase(reader);

			reader.Read(); // StartMember
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartMember);
						
			reader.Read(); // <DeferredLoadingChild>
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartObject);
			Assert.AreEqual(reader.Type, reader.SchemaContext.GetXamlType(typeof(TestClass4)));

			reader.Read(); // StartMember (Foo)
			Assert.AreEqual(reader.NodeType, XamlNodeType.StartMember);			
			
			reader.Read(); // "Blah"
			Assert.AreEqual(reader.NodeType, XamlNodeType.Value);

			reader.Read(); // EndMember
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndMember);

			reader.Read(); // </DeferredLoadingChild>
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndObject);

			reader.Read(); // EndMember
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndMember);

			reader.Read(); // </DeferredLoadingContainerMember2>
			Assert.AreEqual(reader.NodeType, XamlNodeType.EndObject);

			Assert.IsFalse(reader.Read()); // EOF
		}


		[Test]
		public void Read_ContentCollectionShouldParsePropertyAfterInnerItem()
		{
			var xaml = @"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
    <CollectionItem Name='World'/>
	<CollectionParentItem.OtherItem>True</CollectionParentItem.OtherItem>
</CollectionParentItem>";
			var r = GetReaderText(xaml);

			r.Read(); // xmlns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);

			r.Read(); // <CollectionParentItem>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);

			ReadBase(r);

			r.Read(); // StartMember (Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(typeof(CollectionParentItem), r.Member.DeclaringType.UnderlyingType);
			Assert.AreEqual(nameof(CollectionParentItem.Items), r.Member.Name);

			r.Read(); // GetObject
			Assert.AreEqual(XamlNodeType.GetObject, r.NodeType);

			r.Read(); // StartMember (_Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(XamlLanguage.Items, r.Member);

			r.Read(); // <CollectionItem>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);

			r.Read(); // StartMember (Name)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual("Name", r.Member.Name);

			r.Read(); // "World"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("World", r.Value);

			r.Read(); // EndMember (Name)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </CollectionItem>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // EndMember (Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </GetObject>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // EndMember (Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // StartMember (_Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(nameof(CollectionParentItem.OtherItem), r.Member.Name);

			r.Read(); // "True"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("True", r.Value);

			r.Read(); // EndMember (Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </CollectionParentItem>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read()); // EOF
		}

		[Test]
		public void Read_ContentCollectionWithTypeConverterShouldParseInnerTextAndItems()
		{
			var xaml = @"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
	Hello
    <CollectionItem Name='World'/>
	!
</CollectionParentItem>";
			var r = GetReaderText(xaml);

			r.Read(); // xmlns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);

			r.Read(); // <CollectionParentItem>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);

			ReadBase(r);

			r.Read(); // StartMember (Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(typeof(CollectionParentItem), r.Member.DeclaringType.UnderlyingType);
			Assert.AreEqual(nameof(CollectionParentItem.Items), r.Member.Name);

			r.Read(); // GetObject
			Assert.AreEqual(XamlNodeType.GetObject, r.NodeType);

			r.Read(); // StartMember (_Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(XamlLanguage.Items, r.Member);

			r.Read(); // "Hello"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("Hello ", r.Value);

			r.Read(); // <CollectionItem>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);

			r.Read(); // StartMember (Name)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual("Name", r.Member.Name);

			r.Read(); // "World"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("World", r.Value);

			r.Read(); // EndMember (Name)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </CollectionItem>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // "!"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual(" !", r.Value);

			r.Read(); // EndMember (_Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </CollectionItemCollectionAddOverride>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // EndMember (Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </CollectionParentItem>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read()); // EOF
		}

		[Test]
		public void Inner_Text_And_Items_Should_Be_Added_Via_TypeConverter()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<CollectionItemCollectionAddOverride xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'>
	Hello
    <CollectionItem Name='World'/>
	!
</CollectionItemCollectionAddOverride>";
			var result = (CollectionItemCollectionAddOverride)XamlServices.Parse(xaml);

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("Hello ", result[0].Name);
			Assert.AreEqual("World", result[1].Name);
			Assert.AreEqual(" !", result[2].Name);
		}

		[Test]
		public void Inner_Text_And_Items_Should_Be_Added_To_Content_Property_Via_TypeConverter()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'>
	Hello
    <CollectionItem Name='World'/>
	!
</CollectionParentItem>";
			var result = (CollectionParentItem)XamlServices.Parse(xaml);

			Assert.AreEqual(3, result.Items.Count);
			Assert.AreEqual("Hello ", result.Items[0].Name);
			Assert.AreEqual("World", result.Items[1].Name);
			Assert.AreEqual(" !", result.Items[2].Name);
		}

		public class TestSchemaContext : XamlSchemaContext
		{
			readonly string[] unknownTypeNames;

			public TestSchemaContext(params string[] unknownTypeNames)
			{
				this.unknownTypeNames = unknownTypeNames;
			}

			public List<string> RequestedTypeNames { get; } = new List<string>();

			protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
			{
				RequestedTypeNames.Add(name);
				return unknownTypeNames.Contains(name) ? null : base.GetXamlType(xamlNamespace, name, typeArguments);
			}
		}
		
		[Test]
		public void More_Readable_Error_Info()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"<CollectionParentItem xmlns='clr-namespace:MonoTests.System.Xaml;assembly={assembly}'>
	Hello
    <CollectionIte />
	!
</CollectionParentItem>";

			Assert.Catch<XamlObjectWriterException>(() =>
			{
				var res = ((CollectionParentItem) XamlServices.Parse(xaml));
			});


		}
	}
}
