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
using System.ComponentModel;
using System.Xaml;
using System.Xaml.Schema;
using XamlParseException = System.Xaml.XamlParseException;
#endif

using CategoryAttribute = NUnit.Framework.CategoryAttribute;
using XamlReader = System.Xaml.XamlReader;

namespace MonoTests.System.Xaml
{
	[TestFixture]
	public class XamlObjectWriterTest
	{
		PropertyInfo str_len = typeof(string).GetProperty("Length");
		XamlSchemaContext sctx = new XamlSchemaContext(null, null);
		XamlType xt, xt3, xt4, xt5;
		XamlMember xm2, xm3;

		public XamlObjectWriterTest()
		{
			xt = new XamlType(typeof(string), sctx);
			xt3 = new XamlType(typeof(TestClass1), sctx);
			xt4 = new XamlType(typeof(Foo), sctx);
			xt5 = new XamlType(typeof(List<TestClass1>), sctx);
			xm2 = new XamlMember(typeof(TestClass1).GetProperty("TestProp1"), sctx);
			xm3 = new XamlMember(typeof(TestClass1).GetProperty("TestProp2"), sctx);
		}

		public class TestClass1
		{
			public TestClass1()
			{
				TestProp3 = "foobar";
			}

			public string TestProp1 { get; set; }
			// nested.
			public TestClass1 TestProp2 { get; set; }

			public string TestProp3 { get; set; }

			public int TestProp4 { get; set; }
		}

		public class Foo : List<int>
		{
			public Foo()
			{
				Bar = new List<string>();
			}

			public List<string> Bar { get; private set; }

			public List<string> Baz { get; set; }

			public string Ext { get; set; }
		}

		[Test]
		public void SchemaContextNull()
		{
			Assert.Throws<ArgumentNullException>(() => new XamlObjectWriter(null));
		}

		[Test]
		public void SettingsNull()
		{
			// allowed.
			var w = new XamlObjectWriter(sctx, null);
			Assert.AreEqual(sctx, w.SchemaContext, "#1");
		}

		[Test]
		public void InitWriteEndMember()
		{
			Assert.Throws<XamlObjectWriterException>(() => new XamlObjectWriter(sctx, null).WriteEndMember());
		}

		[Test]
		public void InitWriteEndObject()
		{
			Assert.Throws<XamlObjectWriterException>(() => new XamlObjectWriter(sctx, null).WriteEndObject());
		}

		[Test]
		public void InitWriteGetObject()
		{
			Assert.Throws<XamlObjectWriterException>(() => new XamlObjectWriter(sctx, null).WriteGetObject());
		}

		[Test]
		public void InitWriteValue()
		{
			Assert.Throws<XamlObjectWriterException>(() => new XamlObjectWriter(sctx, null).WriteValue("foo"));
		}

		[Test]
		public void InitWriteStartMember()
		{
			Assert.Throws<XamlObjectWriterException>(() => new XamlObjectWriter(sctx, null).WriteStartMember(new XamlMember(str_len, sctx)));
		}

		[Test]
		public void InitWriteNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "x")); // ignored.
			xw.Close();
			Assert.IsNull(xw.Result, "#1");
		}

		[Test]
		public void WriteNamespaceNull()
		{
			Assert.Throws<ArgumentNullException>(() => new XamlObjectWriter(sctx, null).WriteNamespace(null));
		}

		[Test]
		public void InitWriteStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(new XamlType(typeof(int), sctx));
			xw.Close();
			Assert.AreEqual(0, xw.Result, "#1");
		}

		[Test]
		public void GetObjectAfterStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteGetObject());
		}

		[Test]
		//[ExpectedException (typeof (XamlObjectWriterException))]
		public void WriteStartObjectAfterTopLevel()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			// writing another root is <del>not</del> allowed.
			xw.WriteStartObject(xt3);
		}

		[Test]
		public void WriteEndObjectExcess()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteEndObject());
		}

		[Test]
		public void StartObjectWriteEndMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteEndMember());
		}

		[Test]
		public void WriteObjectAndMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteValue("foo");
			xw.WriteEndMember();
			xw.Close();
		}

		[Test]
		public void StartMemberWriteEndMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteEndMember(); // unlike XamlXmlWriter, it is not treated as an error...
			xw.Close();
		}

		[Test]
		public void StartMemberWriteStartMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartMember(xm3));
		}

		[Test]
		public void WriteObjectInsideMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			xw.WriteEndMember();
			xw.Close();
		}

		[Test]
		public void ValueAfterObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			// passes here, but ...
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml throws here
									  // rejected here, unlike XamlXmlWriter.
				xw.WriteEndMember(); // .NET 4.5 throws here
			});
		}

		[Test]
		public void ValueAfterObject2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			// passes here, but should be rejected later.
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml throws here

				xw.WriteEndMember(); // .NET 4.5 throws here. Though this raises an error.
			});
		}

		[Test]
		public void DuplicateAssignment()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml - causes duplicate assignment.
				xw.WriteEndMember(); // .NET 4.5
			});
		}

		[Test]
		public void DuplicateAssignment2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			xw.WriteEndMember();
			Assert.Throws<XamlDuplicateMemberException>(() => xw.WriteStartMember(xm3));
		}

		[Test]
		//[ExpectedException (typeof (ArgumentException))] // oh? XamlXmlWriter raises this.
		public void WriteValueTypeMismatch()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			xw.WriteStartMember(XamlLanguage.Initialization);
			xw.WriteValue(new TestClass1());
			xw.WriteEndMember();
			xw.Close();
			Assert.IsNotNull(xw.Result, "#1");
			Assert.AreEqual(typeof(TestClass1), xw.Result.GetType(), "#2");
		}

		[Test]
		// it fails to convert type and set property value.
		public void WriteValueTypeMismatch2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			Assert.Throws<XamlObjectWriterException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml throws here
				xw.WriteEndMember(); // .NET 4.5 throws here
			});
		}

		[Test]
		public void WriteValueTypeOK()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			xw.WriteStartMember(XamlLanguage.Initialization);
			xw.WriteValue("foo");
			xw.WriteEndMember();
			xw.Close();
			Assert.AreEqual("foo", xw.Result, "#1");
		}

		[Test]
		// This behavior is different from XamlXmlWriter. Compare to XamlXmlWriterTest.WriteValueList().
		public void WriteValueList()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(new XamlType(typeof(List<string>), sctx));
			xw.WriteStartMember(XamlLanguage.Items);
			xw.WriteValue("foo");
			xw.WriteValue("bar");
			xw.WriteEndMember();
			xw.Close();
			var l = xw.Result as List<string>;
			Assert.IsNotNull(l, "#1");
			Assert.AreEqual("foo", l[0], "#2");
			Assert.AreEqual("bar", l[1], "#3");
		}

		// I believe .NET XamlObjectWriter.Dispose() is hack and should
		// be fixed to exactly determine which of End (member or object)
		// to call that results in this ExpectedException.
		// Surprisingly, PositionalParameters is allowed to be closed
		// without EndMember. So it smells that .NET is hacky.
		// We should disable this test and introduce better code (which
		// is already in XamlWriterInternalBase).
		[Test]
		public void CloseWithoutEndMember()
		{
			if (Compat.IsPortableXaml)
				Assert.Ignore("Don't necessarily need this as it may be a .NET hack. See the comment in XamlObjectWriterTest.cs");
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			xw.WriteStartMember(XamlLanguage.Initialization);
			xw.WriteValue("foo");
			Assert.Throws<XamlObjectWriterException>(() => xw.Close());
		}

		[Test]
		public void WriteValueAfterValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue("foo"));
			//xw.WriteValue("bar");
		}

		[Test]
		public void WriteValueAfterNullValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue(null));
			//xw.WriteValue("bar");
		}

		public void StartMemberWriteEndObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteEndObject());
		}

		[Test]
		public void WriteNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, "x"));
			xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y"));
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			xw.Close();
			var ret = xw.Result;
			Assert.IsTrue(ret is TestClass1, "#1");
		}

		[Test]
		public void StartObjectStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartObject(xt3));
		}

		[Test]
		public void StartObjectValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue("foo"));
		}

		[Test]
		public void ObjectContainsObjectAndObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteStartObject(xt3); // System.Xaml throws here
				xw.WriteEndObject(); // .NET 4.5 the exception happens *here*
									 // FIXME: so, WriteEndMember() should not be required, but we fail here. Practically this difference should not matter.
				xw.WriteEndMember(); // of xm3
			});
		}

		[Test]
		public void ObjectContainsObjectAndValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml throws here, but this is allowed ...

				xw.WriteEndMember(); // .NET 4.5 throws here, Though this raises an error.
			});
		}

		[Test]
		public void ObjectContainsObjectAndValue2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm3);
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			Assert.Throws<XamlDuplicateMemberException>(() =>
			{
				xw.WriteValue("foo"); // System.Xaml throws here
				xw.WriteEndMember(); // .NET 4.5 throws here ... until here.
			});
		}

		[Test]
		public void EndObjectAfterNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() =>
			{
				xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y")); // System.Xaml throws here
				xw.WriteEndObject(); // .NET 4.5 throws here
			});
		}

		[Test]
		public void WriteValueAfterNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt);
			xw.WriteStartMember(XamlLanguage.Initialization);
			xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y"));
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue("foo"));
		}

		[Test]
		public void ValueThenStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteValue("foo");
			Assert.Throws<XamlObjectWriterException>(() =>
			{
				xw.WriteStartObject(xt3); // System.Xaml throws here
				xw.Close(); // .NET 4.5 throws here
			});
		}

		[Test]
		public void CollectionValueThenStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt5);
			xw.WriteStartMember(XamlLanguage.Items);
			xw.WriteValue(new TestClass1());
			xw.WriteStartObject(xt3);
			xw.Close();
		}

		[Test]
		public void CollectionMixedObjectsAndValues()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt5);
			xw.WriteStartMember(XamlLanguage.Items);
			xw.WriteValue(new TestClass1());
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			xw.WriteValue(new TestClass1());
			xw.WriteStartObject(xt3);
			xw.WriteEndObject();
			xw.Close();
		}

		[Test]
		// ... unlike XamlXmlWriter (allowed, as it allows StartObject after Value)
		public void ValueThenNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteValue("foo");
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteNamespace(new NamespaceDeclaration("y", "urn:foo"))); // this does not raise an error (since it might start another object)
		}

		[Test]
		// strange, this does *not* result in IOE...
		public void ValueThenNamespaceThenEndMember()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteValue("foo");
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteNamespace(new NamespaceDeclaration("y", "urn:foo")));
			xw.WriteEndMember();
		}

		[Test]
		// This is also very different, requires exactly opposite namespace output manner to XamlXmlWriter (namespace first, object follows).
		public void StartMemberAfterNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y")));
		}

		[Test]
		public void StartMemberBeforeNamespace()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2); // note that it should be done *after* WriteNamespace in XamlXmlWriter. SO inconsistent.
			xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y"));
			xw.WriteEndMember();
			xw.Close();
		}

		[Test]
		public void StartMemberBeforeNamespace2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteNamespace(new NamespaceDeclaration("urn:foo", "y"));
			// and here, NamespaceDeclaration is written as if it 
			// were another value object( unlike XamlXmlWriter)
			// and rejects further value.
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue("foo"));
		}

		[Test]
		public void EndMemberThenStartObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			xw.WriteValue("foo");
			xw.WriteEndMember();
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartObject(xt3));
		}

		// The semantics on WriteGetObject() is VERY different from XamlXmlWriter.

		[Test]
		public void GetObjectOnNullValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xm2);
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteGetObject());
		}

		[Test]
		public void GetObjectOnNullValue2()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Baz"), sctx)); // unlike Bar, Baz is not initialized.
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteGetObject()); // fails, because it is null.
		}

		[Test]
		public void GetObjectOnIntValue()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xt3.GetMember("TestProp4")); // int
			xw.WriteGetObject(); // passes!!! WTF
			xw.WriteEndObject();
		}

		[Test]
		// String is not treated as a collection on XamlXmlWriter, while this XamlObjectWriter does.
		public void GetObjectOnNonNullString()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt3);
			Assert.IsNull(xw.Result, "#1");
			xw.WriteStartMember(xt3.GetMember("TestProp3"));
			xw.WriteGetObject();
			Assert.IsNull(xw.Result, "#2");
		}

		[Test]
		public void GetObjectOnCollection()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Bar"), sctx));
			xw.WriteGetObject();
			xw.Close();
		}

		[Test]
		public void ValueAfterGetObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Bar"), sctx));
			xw.WriteGetObject();
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteValue("foo"));
		}

		[Test]
		public void StartObjectAfterGetObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Bar"), sctx));
			xw.WriteGetObject();
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartObject(xt));
		}

		[Test]
		public void EndMemberAfterGetObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Bar"), sctx));
			xw.WriteGetObject();
			Assert.Throws<XamlObjectWriterException>(() => xw.WriteEndMember()); // ...!?
		}

		[Test]
		public void StartMemberAfterGetObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			var xmm = xt4.GetMember("Bar");
			xw.WriteStartMember(xmm); // <List.Bar>
			xw.WriteGetObject(); // shifts current member to List<T>.
			xw.WriteStartMember(xmm.Type.GetMember("Capacity"));
			xw.WriteValue(5);
			xw.WriteEndMember();
			/*
			xw.WriteEndObject (); // got object
			xw.WriteEndMember (); // Bar
			xw.WriteEndObject (); // started object
			*/
			xw.Close();
		}

		[Test]
		public void EndObjectAfterGetObject()
		{
			var xw = new XamlObjectWriter(sctx, null);
			xw.WriteStartObject(xt4);
			xw.WriteStartMember(new XamlMember(typeof(Foo).GetProperty("Bar"), sctx));
			xw.WriteGetObject();
			xw.WriteEndObject();
		}

		[Test]
		public void WriteAttachableProperty()
		{
			Attached2 result = null;

			var rsettings = new XamlXmlReaderSettings();
			using (var reader = new XamlXmlReader(new StringReader(String.Format(@"<Attached2 AttachedWrapper3.Property=""Test"" xmlns=""clr-namespace:MonoTests.System.Xaml;assembly={0}""></Attached2>", typeof(AttachedWrapper3).GetTypeInfo().Assembly.GetName().Name)), rsettings))
			{
				var wsettings = new XamlObjectWriterSettings();
				using (var writer = new XamlObjectWriter(reader.SchemaContext, wsettings))
				{
					XamlServices.Transform(reader, writer, false);
					result = (Attached2)writer.Result;
				}
			}

			Assert.AreEqual("Test", result.Property, "#1");
		}

		[Test]
		public void OnSetValueAndHandledFalse() // part of bug #3003
		{
			/*
			var obj = new TestClass3 ();
			obj.Nested = new TestClass3 ();
			var sw = new StringWriter ();
			var xxw = new XamlXmlWriter (XmlWriter.Create (sw), new XamlSchemaContext ());
			XamlServices.Transform (new XamlObjectReader (obj), xxw);
			Console.Error.WriteLine (sw);
			*/
			var xml = "<TestClass3 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><TestClass3.Nested><TestClass3 Nested='{x:Null}' /></TestClass3.Nested></TestClass3>".UpdateXml();
			var settings = new XamlObjectWriterSettings();
			bool invoked = false;
			settings.XamlSetValueHandler = (sender, e) =>
			{
				invoked = true;
				Assert.IsNotNull(sender, "#1");
				Assert.AreEqual(typeof(TestClass3), sender.GetType(), "#2");
				Assert.AreEqual("Nested", e.Member.Name, "#3");
				Assert.IsTrue(sender != e.Member.Invoker.GetValue(sender), "#4");
				Assert.IsFalse(e.Handled, "#5");
				// ... and leave Handled as false, to invoke the actual setter
			};
			var xow = new XamlObjectWriter(new XamlSchemaContext(), settings);
			var xxr = new XamlXmlReader(XmlReader.Create(new StringReader(xml)));
			XamlServices.Transform(xxr, xow);
			Assert.IsTrue(invoked, "#6");
			Assert.IsNotNull(xow.Result, "#7");
			var ret = xow.Result as TestClass3;
			Assert.IsNotNull(ret.Nested, "#8");
		}

		[Test] // bug #3003 repro
		public void gsAndProcessingOrder()
		{
			if (Compat.IsPortableXaml && !Compat.HasISupportInitializeInterface)
				Assert.Ignore("The ISupportInitialize starts support from netstandard20");
			
			var asm = GetType().GetTypeInfo().Assembly;
			var context = new XamlSchemaContext(new Assembly[] { asm });
			var output = XamarinBug3003.TestContext.Writer;
			output.WriteLine();

			var reader = new XamlXmlReader(XmlReader.Create(new StringReader(XamarinBug3003.TestContext.XmlInput)), context);

			var writerSettings = new XamlObjectWriterSettings();
			writerSettings.AfterBeginInitHandler = (sender, e) =>
			{
				output.WriteLine("XamlObjectWriterSettings.AfterBeginInit: {0}", e.Instance);
			};
			writerSettings.AfterEndInitHandler = (sender, e) =>
			{
				output.WriteLine("XamlObjectWriterSettings.AfterEndInit: {0}", e.Instance);
			};

			writerSettings.BeforePropertiesHandler = (sender, e) =>
			{
				output.WriteLine("XamlObjectWriterSettings.BeforeProperties: {0}", e.Instance);
			};
			writerSettings.AfterPropertiesHandler = (sender, e) =>
			{
				output.WriteLine("XamlObjectWriterSettings.AfterProperties: {0}", e.Instance);
			};
			writerSettings.XamlSetValueHandler = (sender, e) =>
			{
				output.WriteLine("XamlObjectWriterSettings.XamlSetValue: {0}, Member: {1}", e.Value, e.Member.Name);
			};

			var writer = new XamlObjectWriter(context, writerSettings);
			XamlServices.Transform(reader, writer);
			var obj = writer.Result as XamarinBug3003.Parent;

			output.WriteLine("Loaded {0}", obj);

			Assert.AreEqual(XamarinBug3003.TestContext.ExpectedResult.Replace("\r\n", "\n"), output.ToString().Replace("\r\n", "\n"), "#1");

			Assert.AreEqual(2, obj.Children.Count, "#2");
		}

		// extra use case based tests.

		[Test]
		public void WriteEx_Type_WriteString()
		{
			var ow = new XamlObjectWriter(sctx);
			ow.WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, "x"
			));
			ow.WriteStartObject(XamlLanguage.Type);
			ow.WriteStartMember(XamlLanguage.PositionalParameters);
			ow.WriteValue("x:Int32");
			ow.Close();
			Assert.AreEqual(typeof(int), ow.Result, "#1");
		}

		[Test]
		public void WriteEx_Type_WriteType()
		{
			var ow = new XamlObjectWriter(sctx);
			ow.WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, "x"
			));
			ow.WriteStartObject(XamlLanguage.Type);
			ow.WriteStartMember(XamlLanguage.PositionalParameters);
			ow.WriteValue(typeof(int));
			ow.Close();
			Assert.AreEqual(typeof(int), ow.Result, "#1");
		}

		[Test]
		public void LookupCorrectEventBoundMethod()
		{
			var o = (XamarinBug2927.MyRootClass)XamlServices.Load(GetReader("LookupCorrectEvent.xml"));
			o.Child.Descendant.Work();
			Assert.IsTrue(o.Invoked, "#1");
			Assert.IsFalse(o.Child.Invoked, "#2");
			Assert.IsFalse(o.Child.Descendant.Invoked, "#3");
		}

		[Test]
		public void LookupCorrectEventBoundMethod2()
		{
			Assert.Throws<XamlObjectWriterException>(() => XamlServices.Load(GetReader("LookupCorrectEvent2.xml")));
		}

		[Test]
		public void LookupCorrectEventBoundMethod3()
		{
			XamlServices.Load(GetReader("LookupCorrectEvent3.xml"));
		}

		// common use case based tests (to other readers/writers).

		XamlReader GetReader(string filename)
		{
			string xml = File.ReadAllText(Compat.GetTestFile(filename)).UpdateXml();
			return new XamlXmlReader(XmlReader.Create(new StringReader(xml)));
		}

		[Test]
		public void Write_String()
		{
			using (var xr = GetReader("String.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual("foo", des, "#1");
			}
		}

		[Test]
		public void Write_Int32()
		{
			using (var xr = GetReader("Int32.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(5, des, "#1");
			}
		}

		[Test]
		public void Write_DateTime()
		{
			using (var xr = GetReader("DateTime.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(new DateTime(2010, 4, 14), des, "#1");
			}
		}

		[Test]
		public void Write_TimeSpan()
		{
			using (var xr = GetReader("TimeSpan.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(TimeSpan.FromMinutes(7), des, "#1");
			}
		}

		[Test]
		public void Write_Uri()
		{
			using (var xr = GetReader("Uri.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(new Uri("urn:foo"), des, "#1");
			}
		}

		[Test]
		public void Write_Null()
		{
			using (var xr = GetReader("NullExtension.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.IsNull(des, "#1");
			}
		}

		[Test]
		public void Write_Type()
		{
			using (var xr = GetReader("Type.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(typeof(int), des, "#1");
			}
		}

		[Test]
		public void Write_Type2()
		{
			var obj = typeof(MonoTests.System.Xaml.TestClass1);
			using (var xr = GetReader("Type2.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_Guid()
		{
			var obj = Guid.Parse("9c3345ec-8922-4662-8e8d-a4e41f47cf09");
			using (var xr = GetReader("Guid.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_GuidFactoryMethod()
		{
			var obj = Guid.Parse("9c3345ec-8922-4662-8e8d-a4e41f47cf09");
			using (var xr = GetReader("GuidFactoryMethod.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_StaticExtension()
		{
			var obj = new StaticExtension("FooBar");
			using (var xr = GetReader("StaticExtension.xml"))
			{
				Assert.Throws<XamlObjectWriterException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		public void Write_Reference()
		{
			using (var xr = GetReader("Reference.xml"))
			{
				var des = XamlServices.Load(xr);
				// .NET does not return Reference.
				// Its ProvideValue() returns MS.Internal.Xaml.Context.NameFixupToken,
				// which is assumed (by name) to resolve to the referenced object.
				Assert.IsNotNull(des, "#1");
				//Assert.AreEqual (new Reference ("FooBar"), des, "#1");
			}
		}

		[Test]
		public void Write_ArrayInt32()
		{
			var obj = new int[] { 4, -5, 0, 255, int.MaxValue };
			using (var xr = GetReader("Array_Int32.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_ListInt32()
		{
			var obj = new int[] { 5, -3, int.MaxValue, 0 }.ToList();
			using (var xr = GetReader("List_Int32.xml"))
			{
				var des = (List<int>)XamlServices.Load(xr);
				Assert.AreEqual(obj.ToArray(), des.ToArray(), "#1");
			}
		}

		[Test]
		public void Write_ListInt32_2()
		{
			var obj = new List<int>(new int[0]) { Capacity = 0 }; // set explicit capacity for trivial implementation difference
			using (var xr = GetReader("List_Int32_2.xml"))
			{
				var des = (List<int>)XamlServices.Load(xr);
				Assert.AreEqual(obj.ToArray(), des.ToArray(), "#1");
			}
		}

		[Test]
		public void Write_ListType()
		{
			var obj = new List<Type>(new Type[] { typeof(int), typeof(Dictionary<Type, XamlType>) }) { Capacity = 2 };
			using (var xr = GetReader("List_Type.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_ListArray()
		{
			var obj = new List<Array>(new Array[] { new int[] { 1, 2, 3 }, new string[] { "foo", "bar", "baz" } }) { Capacity = 2 };
			using (var xr = GetReader("List_Array.xml"))
			{
				var des = (List<Array>)XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void Write_DictionaryInt32String()
		{
			var dic = new Dictionary<int, string>();
			dic.Add(0, "foo");
			dic.Add(5, "bar");
			dic.Add(-2, "baz");
			using (var xr = GetReader("Dictionary_Int32_String.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(dic, des, "#1");
			}
		}

		[Test]
		public void Write_DictionaryStringType()
		{
			var dic = new Dictionary<string, Type>();
			dic.Add("t1", typeof(int));
			dic.Add("t2", typeof(int[]));
			dic.Add("t3", typeof(int?));
			dic.Add("t4", typeof(List<int>));
			dic.Add("t5", typeof(Dictionary<int, DateTime>));
			dic.Add("t6", typeof(List<KeyValuePair<int, DateTime>>));
			using (var xr = GetReader("Dictionary_String_Type.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(dic, des, "#1");
			}
		}

		[Test]
		public void Write_PositionalParameters1Wrapper()
		{
			// Unlike the above case, this has the wrapper object and hence PositionalParametersClass1 can be written as an attribute (markup extension)
			var obj = new PositionalParametersWrapper("foo", 5);
			using (var xr = GetReader("PositionalParametersWrapper.xml"))
			{
				var des = XamlServices.Load(xr) as PositionalParametersWrapper;
				Assert.IsNotNull(des, "#1");
				Assert.IsNotNull(des.Body, "#2");
				Assert.AreEqual(obj.Body.Foo, des.Body.Foo, "#3");
				Assert.AreEqual(obj.Body.Bar, des.Body.Bar, "#4");
			}
		}

		[Test]
		public void Write_ArgumentAttributed()
		{
			//var obj = new ArgumentAttributed ("foo", "bar");
			using (var xr = GetReader("ArgumentAttributed.xml"))
			{
				var des = (ArgumentAttributed)XamlServices.Load(xr);
				Assert.AreEqual("foo", des.Arg1, "#1");
				Assert.AreEqual("bar", des.Arg2, "#2");
			}
		}

		[Test]
		public void Write_ArgumentNonAttributed()
		{
			//var obj = new ArgumentNonAttributed ("foo", "bar");
			using (var xr = GetReader("ArgumentNonAttributed.xml"))
			{
				var des = (ArgumentNonAttributed)XamlServices.Load(xr);
				Assert.AreEqual("foo", des.Arg1, "#1");
				Assert.AreEqual("bar", des.Arg2, "#2");
			}
		}

		[Test]
		public void Write_ArgumentMultipleTypesFromString()
		{
			using (var xr = GetReader("ArgumentMultipleTypesFromString.xml"))
			{
				var des = (ArgumentMultipleTypes)XamlServices.Load(xr);
				Assert.AreEqual("foo", des.StringArg, "#1");
				Assert.AreEqual(0, des.IntArg, "#2");
			}
		}
		[Test]
		public void Write_ArgumentMultipleTypesFromInt()
		{
			using (var xr = GetReader("ArgumentMultipleTypesFromInt.xml"))
			{
				var des = (ArgumentMultipleTypes)XamlServices.Load(xr);
				Assert.AreEqual(null, des.StringArg, "#1");
				Assert.AreEqual(10, des.IntArg, "#2");
			}
		}

		[Test]
		public void Write_ArgumentMultipleTypesFromAttribute()
		{
			using (var xr = GetReader("ArgumentMultipleTypesFromAttribute.xml"))
			{
				var des = (ArgumentMultipleTypes)XamlServices.Load(xr);
				Assert.AreEqual("foo", des.StringArg, "#1");
				Assert.AreEqual(0, des.IntArg, "#2");
			}
		}

		[Test]
		public void Write_ArgumentWithIntConstructorFromAttribute()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("System.Xaml will convert the types if needed");
			using (var xr = GetReader("ArgumentWithIntConstructorFromAttribute.xml"))
			{
				var des = (ArgumentWithIntConstructor)XamlServices.Load(xr);
				Assert.AreEqual(10, des.IntArg, "#2");
			}
		}

		[Test]
		public void Write_ArgumentWithIntConstructorFromInt()
		{
			using (var xr = GetReader("ArgumentWithIntConstructorFromInt.xml"))
			{
				var des = (ArgumentWithIntConstructor)XamlServices.Load(xr);
				Assert.AreEqual(11, des.IntArg, "#2");
			}
		}

		[Test]
		public void Write_ArgumentWithIntConstructorFromString()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("System.Xaml will convert the types if needed");
			using (var xr = GetReader("ArgumentWithIntConstructorFromString.xml"))
			{
				var des = (ArgumentWithIntConstructor)XamlServices.Load(xr);
				Assert.AreEqual(12, des.IntArg, "#2");
			}
		}

		[Test]
		public void Write_ArrayExtension2()
		{
			//var obj = new ArrayExtension (typeof (int));
			using (var xr = GetReader("ArrayExtension2.xml"))
			{
				var des = XamlServices.Load(xr);
				// The resulting object is not ArrayExtension.
				Assert.AreEqual(new int[0], des, "#1");
			}
		}

		[Test]
		public void Write_ArrayList()
		{
			var obj = new ArrayList(new int[] { 5, -3, 0 });
			using (var xr = GetReader("ArrayList.xml"))
			{
				var des = XamlServices.Load(xr);
				Assert.AreEqual(obj, des, "#1");
			}
		}

		[Test]
		public void ComplexPositionalParameterWrapper()
		{
			var ex = Assert.Throws<XamlObjectWriterException>(() =>
			{
				using (var xr = GetReader("ComplexPositionalParameterWrapper.xml"))
				{
					var des = (ComplexPositionalParameterWrapper)XamlServices.Load(xr);
					Assert.IsNotNull(des.Param, "#1");
					Assert.AreEqual("foo", des.Param.Value.Foo, "#2");
				}
			});
			Assert.IsInstanceOf<ArgumentException>(ex.InnerException, "#3");
		}

		[Test]
		public void ComplexPositionalParameterWrapper2()
		{
			using (var xr = GetReader("ComplexPositionalParameterWrapper2.xml"))
			{
				var des = (ComplexPositionalParameterWrapper2)XamlServices.Load(xr);
				Assert.IsNotNull(des.Param, "#1");
				Assert.AreEqual("foo", des.Param, "#2");
			}
		}

		[Test]
		public void Write_ListWrapper()
		{
			var obj = new ListWrapper(new List<int>(new int[] { 5, -3, 0 }) { Capacity = 3 }); // set explicit capacity for trivial implementation difference
			using (var xr = GetReader("ListWrapper.xml"))
			{
				var des = (ListWrapper)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.IsNotNull(des.Items, "#2");
				Assert.AreEqual(obj.Items.ToArray(), des.Items.ToArray(), "#3");
			}
		}

		[Test]
		public void Write_ListWrapper2()
		{
			var obj = new ListWrapper2(new List<int>(new int[] { 5, -3, 0 }) { Capacity = 3 }); // set explicit capacity for trivial implementation difference
			using (var xr = GetReader("ListWrapper2.xml"))
			{
				var des = (ListWrapper2)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.IsNotNull(des.Items, "#2");
				Assert.AreEqual(obj.Items.ToArray(), des.Items.ToArray(), "#3");
			}
		}

		[Test]
		public void Write_MyArrayExtension()
		{
			//var obj = new MyArrayExtension (new int [] {5, -3, 0});
			using (var xr = GetReader("MyArrayExtension.xml"))
			{
				var des = XamlServices.Load(xr);
				// ProvideValue() returns an array
				Assert.AreEqual(new int[] { 5, -3, 0 }, des, "#1");
			}
		}

		[Test]
		public void Write_MyArrayExtensionA()
		{
			//var obj = new MyArrayExtensionA (new int [] {5, -3, 0});
			using (var xr = GetReader("MyArrayExtensionA.xml"))
			{
				var des = XamlServices.Load(xr);
				// ProvideValue() returns an array
				Assert.AreEqual(new int[] { 5, -3, 0 }, des, "#1");
			}
		}

		[Test]
		public void Write_MyExtension()
		{
			//var obj = new MyExtension () { Foo = typeof (int), Bar = "v2", Baz = "v7"};
			using (var xr = GetReader("MyExtension.xml"))
			{
				var des = XamlServices.Load(xr);
				// ProvideValue() returns this.
				Assert.AreEqual("provided_value", des, "#1");
			}
		}

		[Test]
		// unable to cast string to MarkupExtension
		public void Write_MyExtension2()
		{
			//var obj = new MyExtension2 () { Foo = typeof (int), Bar = "v2"};
			using (var xr = GetReader("MyExtension2.xml"))
			{
				Assert.Throws<InvalidCastException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		public void Write_MyExtension3()
		{
			//var obj = new MyExtension3 () { Foo = typeof (int), Bar = "v2"};
			using (var xr = GetReader("MyExtension3.xml"))
			{
				var des = XamlServices.Load(xr);
				// StringConverter is used and the resulting value comes from ToString().
				Assert.AreEqual("MonoTests.System.Xaml.MyExtension3", des, "#1");
			}
		}

		[Test]
		// wrong TypeConverter input (input string for DateTimeConverter invalid)
		public void Write_MyExtension4()
		{
			var obj = new MyExtension4() { Foo = typeof(int), Bar = "v2" };
			using (var xr = GetReader("MyExtension4.xml"))
			{
				Assert.Throws<XamlObjectWriterException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		public void Write_MyExtension6()
		{
			//var obj = new MyExtension6 ("foo");
			using (var xr = GetReader("MyExtension6.xml"))
			{
				var des = XamlServices.Load(xr);
				// ProvideValue() returns this.
				Assert.AreEqual("foo", des, "#1");
			}
		}

		[Test]
		public void Write_PropertyDefinition()
		{
			//var obj = new PropertyDefinition () { Modifier = "protected", Name = "foo", Type = XamlLanguage.String };
			using (var xr = GetReader("PropertyDefinition.xml"))
			{
				var des = (PropertyDefinition)XamlServices.Load(xr);
				Assert.AreEqual("protected", des.Modifier, "#1");
				Assert.AreEqual("foo", des.Name, "#2");
				Assert.AreEqual(XamlLanguage.String, des.Type, "#3");
			}
		}

		[Test]
		public void Write_AmbientResourceProvider()
		{
			// tests whether nesting order is correct when providing ambient values
			const string resourceValue = "resource content";
			using (var xr = GetReader("AmbientResourceProvider.xml"))
			{
				var outer = (AmbientResourceProvider)XamlServices.Load(xr);
				var inner = (AmbientResourceProvider)outer.Content;
				var wrapper = (AmbientResourceWrapper)inner.Content;
				Assert.AreEqual(resourceValue, wrapper.Foo);
			}
		}

#if PCL
		// this test won't compile with System.Xaml because it uses new 3-arg constructor
		[Test]
		public void Write_AmbientResourceWrapper()
		{
			// tests whether parent ambient provider is used correctly
			const string resourceKey = "FooResourceKey";
			var resource = new object();
			var ambientResourceProvider = new AmbientResourceProvider
			{
				Resources =
				{
					[resourceKey] = resource
				}
			};
			var parentAmbientProvider = new SimpleAmbientProvider { Values = new[] { ambientResourceProvider } };
			using (var xr = GetReader("AmbientResourceWrapper.xml"))
			{
				var writer = new XamlObjectWriter(xr.SchemaContext, new XamlObjectWriterSettings(), parentAmbientProvider);
				XamlServices.Transform(xr, writer);
				var des = (AmbientResourceWrapper)writer.Result;
				Assert.AreSame(resource, des.Foo);
			}
		}
#endif

		[Test]
		public void Write_StaticExtensionWrapper()
		{
			var ex = Assert.Throws<XamlObjectWriterException>(() =>
			{
				using (var xr = GetReader("StaticExtensionWrapper.xml"))
				{
#pragma warning disable 219
					var des = (StaticExtensionWrapper)XamlServices.Load(xr);
#pragma warning restore 219
				}
			});
			Assert.AreEqual(ex.InnerException.GetType(), typeof(ArgumentException));

		}

		[Test]
		public void Write_StaticExtensionWrapper2()
		{
			using (var xr = GetReader("StaticExtensionWrapper2.xml"))
			{
				var des = (StaticExtensionWrapper2)XamlServices.Load(xr);
				Assert.IsNotNull(des.Param, "#1");
				Assert.AreEqual("foo", des.Param, "#2");
			}
		}

		[Test]
		public void Write_TypeExtensionWrapper()
		{
			var ex = Assert.Throws<XamlObjectWriterException>(() =>
			{
				// can't read a markup extension directly
				using (var xr = GetReader("TypeExtensionWrapper.xml"))
				{
#pragma warning disable 219
					var des = (TypeExtensionWrapper)XamlServices.Load(xr);
#pragma warning restore 219
				}
			});
			Assert.IsInstanceOf<XamlParseException>(ex.InnerException);
		}

		[Test]
		public void Write_TypeExtensionWrapper2()
		{
			//var obj = new TypeExtensionWrapper () { Param = new TypeExtension ("Foo") };
			using (var xr = GetReader("TypeExtensionWrapper2.xml"))
			{
				var des = (TypeExtensionWrapper2)XamlServices.Load(xr);
				Assert.IsNotNull(des.Param, "#1");
				Assert.AreEqual(typeof(NamedItem), des.Param, "#2");
			}
		}

		[Test]
		public void Write_NamedItems()
		{
			// foo
			// - bar
			// -- foo
			// - baz
			var obj = new NamedItem("foo");
			var obj2 = new NamedItem("bar");
			obj.References.Add(obj2);
			obj.References.Add(new NamedItem("baz"));
			obj2.References.Add(obj);

			using (var xr = GetReader("NamedItems.xml"))
			{
				var des = (NamedItem)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.AreEqual(2, des.References.Count, "#2");
				Assert.AreEqual(typeof(NamedItem), des.References[0].GetType(), "#3");
				Assert.AreEqual(typeof(NamedItem), des.References[1].GetType(), "#4");
				Assert.AreEqual(des, des.References[0].References[0], "#5");
			}
		}

		[Test]
		public void Write_NamedItems2()
		{
			// i1
			// - i2
			// -- i3
			// - i4
			// -- i3
			var obj = new NamedItem2("i1");
			var obj2 = new NamedItem2("i2");
			var obj3 = new NamedItem2("i3");
			var obj4 = new NamedItem2("i4");
			obj.References.Add(obj2);
			obj.References.Add(obj4);
			obj2.References.Add(obj3);
			obj4.References.Add(obj3);

			using (var xr = GetReader("NamedItems2.xml"))
			{
				var des = (NamedItem2)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.AreEqual(2, des.References.Count, "#2");
				Assert.AreEqual(typeof(NamedItem2), des.References[0].GetType(), "#3");
				Assert.AreEqual(typeof(NamedItem2), des.References[1].GetType(), "#4");
				Assert.AreEqual(1, des.References[0].References.Count, "#5");
				Assert.AreEqual(1, des.References[1].References.Count, "#6");
				Assert.AreEqual(des.References[0].References[0], des.References[1].References[0], "#7");
			}
		}

		/// <summary>
		/// Issue #9 - When using x:Name, the property indicated by RuntimeNameProperty attribute should also be set.
		/// </summary>
		[Test]
		public void Write_NamedItems3()
		{
			// i1
			// - i2
			// -- i3
			// - i4
			// -- i3
			var obj = new NamedItem2("i1");
			var obj2 = new NamedItem2("i2");
			var obj3 = new NamedItem2("i3");
			var obj4 = new NamedItem2("i4");
			obj.References.Add(obj2);
			obj.References.Add(obj4);
			obj2.References.Add(obj3);
			obj4.References.Add(obj3);

			using (var xr = GetReader("NamedItems3.xml"))
			{
				var des = (NamedItem2)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.AreEqual("i1", des.ItemName, "#2");
				Assert.AreEqual(2, des.References.Count, "#3");
				Assert.AreEqual(typeof(NamedItem2), des.References[0].GetType(), "#4");
				Assert.AreEqual(typeof(NamedItem2), des.References[1].GetType(), "#5");
				Assert.AreEqual("i2", des.References[0].ItemName, "#6");
				Assert.AreEqual("i4", des.References[1].ItemName, "#7");
				Assert.AreEqual(1, des.References[0].References.Count, "#8");
				Assert.AreEqual(1, des.References[1].References.Count, "#9");
				Assert.AreEqual("i3", des.References[0].References[0].ItemName, "#10");
				Assert.AreEqual(des.References[0].References[0], des.References[1].References[0], "#11");
			}
		}

		[Test]
		public void Write_NamedItems4()
		{
			using (var xr = GetReader("NamedItems4.xml"))
			{
				var des = (NamedItem2)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.AreEqual("i1", des.ItemName, "#2");
				Assert.AreEqual(2, des.References.Count, "#3");
				Assert.AreEqual(typeof(NamedItem2), des.References[0].GetType(), "#4");
				Assert.AreEqual(typeof(NamedItem2), des.References[1].GetType(), "#5");
				Assert.AreEqual("i4", des.References[0].ItemName, "#6");
				Assert.AreEqual("i2", des.References[1].ItemName, "#7");
				Assert.AreEqual(1, des.References[0].References.Count, "#8");
				Assert.AreEqual(1, des.References[1].References.Count, "#9");
				Assert.AreEqual("i3", des.References[0].References[0].ItemName, "#10");
				Assert.AreEqual(des.References[0].References[0], des.References[1].References[0], "#11");
			}
		}

		[Test]
		public void Write_XmlSerializableWrapper()
		{
			var assns = "clr-namespace:MonoTests.System.Xaml;assembly=" + GetType().GetTypeInfo().Assembly.GetName().Name;
			using (var xr = GetReader("XmlSerializableWrapper.xml"))
			{
				var des = (XmlSerializableWrapper)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
				Assert.IsNotNull(des.Value, "#2");
				Assert.AreEqual("<root xmlns=\"" + assns + "\" />", des.Value.GetRaw(), "#3");
			}
		}

		[Test]
		public void Write_XmlSerializable()
		{
			using (var xr = GetReader("XmlSerializable.xml"))
			{
				var des = (XmlSerializable)XamlServices.Load(xr);
				Assert.IsNotNull(des, "#1");
			}
		}

		[Test]
		public void Write_ListXmlSerializable()
		{
			using (var xr = GetReader("List_XmlSerializable.xml"))
			{
				var des = (List<XmlSerializable>)XamlServices.Load(xr);
				Assert.AreEqual(1, des.Count, "#1");
			}
		}

		[Test]
		public void Write_AttachedProperty()
		{
			using (var xr = GetReader("AttachedProperty.xml"))
			{
				AttachedWrapper des = null;
				try
				{
					des = (AttachedWrapper)XamlServices.Load(xr);
					Assert.IsNotNull(des.Value, "#1");
					Assert.AreEqual("x", Attachable.GetFoo(des), "#2");
					Assert.AreEqual("y", Attachable.GetFoo(des.Value), "#3");
				}
				finally
				{
					if (des != null)
					{
						Attachable.SetFoo(des, null);
						Attachable.SetFoo(des.Value, null);
					}
				}
			}
		}

		[Test]
		public void Write_EventStore()
		{
			using (var xr = GetReader("EventStore.xml"))
			{
				var res = (EventStore)XamlServices.Load(xr);
				Assert.AreEqual("foo", res.Examine(), "#1");
				Assert.IsTrue(res.Method1Invoked, "#2");
			}
		}

		[Test]
		// for two occurence of Event1 ...
		public void Write_EventStore2()
		{
			using (var xr = GetReader("EventStore2.xml"))
			{
				Assert.Throws<XamlDuplicateMemberException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		// attaching nonexistent method
		public void Write_EventStore3()
		{
			using (var xr = GetReader("EventStore3.xml"))
			{
				Assert.Throws<XamlObjectWriterException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		public void Write_EventStore4()
		{
			using (var xr = GetReader("EventStore4.xml"))
			{
				var res = (EventStore2<EventArgs>)XamlServices.Load(xr);
				Assert.AreEqual("foo", res.Examine(), "#1");
				Assert.IsTrue(res.Method1Invoked, "#2");
			}
		}

		/// <summary>
		/// Test binding an event to a method with a base EventArgs.
		/// </summary>
		/// <remarks>
		/// This allows you to bind to events that are legal
		/// </remarks>
		[Test]
		public void Write_EventStore5()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Binding events to methods with base class parameters is not supported in System.Xaml");

			using (var xr = GetReader("EventStore5.xml"))
			{
				var res = (EventStore)XamlServices.Load(xr);
				Assert.IsFalse(res.Method1Invoked, "#1");
				res.Examine();
				Assert.IsTrue(res.Method1Invoked, "#2");
			}
		}

		[Test]
		public void Write_AbstractWrapper()
		{
			using (var xr = GetReader("AbstractContainer.xml"))
			{
				var res = (AbstractContainer)XamlServices.Load(xr);
				Assert.IsNull(res.Value1, "#1");
				Assert.IsNotNull(res.Value2, "#2");
				Assert.AreEqual("x", res.Value2.Foo, "#3");
			}
		}

		[Test]
		public void Write_ReadOnlyPropertyContainer()
		{
			using (var xr = GetReader("ReadOnlyPropertyContainer.xml"))
			{
				var res = (ReadOnlyPropertyContainer)XamlServices.Load(xr);
				Assert.AreEqual("x", res.Foo, "#1");
				Assert.AreEqual("x", res.Bar, "#2");
			}
		}

		[Test]
		public void Write_TypeConverterOnListMember()
		{
			using (var xr = GetReader("TypeConverterOnListMember.xml"))
			{
				var res = (SecondTest.TypeOtherAssembly)XamlServices.Load(xr);
				Assert.AreEqual(3, res.Values.Count, "#1");
				Assert.AreEqual(3, res.Values[2], "#2");
			}
		}

		[Test]
		public void Write_EnumContainer()
		{
			using (var xr = GetReader("EnumContainer.xml"))
			{
				var res = (EnumContainer)XamlServices.Load(xr);
				Assert.AreEqual(EnumValueType.Two, res.EnumProperty, "#1");
			}
		}

		[Test]
		public void Write_CollectionContentProperty()
		{
			using (var xr = GetReader("CollectionContentProperty.xml"))
			{
				var res = (CollectionContentProperty)XamlServices.Load(xr);
				Assert.AreEqual(4, res.ListOfItems.Count, "#1");
			}
		}

		[Test]
		public void Write_CollectionContentProperty2()
		{
			using (var xr = GetReader("CollectionContentProperty2.xml"))
			{
				var res = (CollectionContentProperty)XamlServices.Load(xr);
				Assert.AreEqual(4, res.ListOfItems.Count, "#1");
			}
		}

		[Test]
		public void Write_AmbientPropertyContainer()
		{
			using (var xr = GetReader("AmbientPropertyContainer.xml"))
			{
				var res = (SecondTest.ResourcesDict)XamlServices.Load(xr);
				Assert.AreEqual(2, res.Count, "#1");
				Assert.IsTrue(res.ContainsKey("TestDictItem"), "#2");
				Assert.IsTrue(res.ContainsKey("okay"), "#3");
				var i1 = res["TestDictItem"] as SecondTest.TestObject;
				Assert.IsNull(i1.TestProperty, "#4");
				var i2 = res["okay"] as SecondTest.TestObject;
				Assert.AreEqual(i1, i2.TestProperty, "#5");
			}
		}

		[Test] // bug #682102
		public void Write_AmbientPropertyContainer2()
		{
			using (var xr = GetReader("AmbientPropertyContainer2.xml"))
			{
				var res = (SecondTest.ResourcesDict)XamlServices.Load(xr);
				Assert.AreEqual(2, res.Count, "#1");
				Assert.IsTrue(res.ContainsKey("TestDictItem"), "#2");
				Assert.IsTrue(res.ContainsKey("okay"), "#3");
				var i1 = res["TestDictItem"] as SecondTest.TestObject;
				Assert.IsNull(i1.TestProperty, "#4");
				var i2 = res["okay"] as SecondTest.TestObject;
				Assert.AreEqual(i1, i2.TestProperty, "#5");
			}
		}

		[Test]
		public void Write_NullableContainer()
		{
			using (var xr = GetReader("NullableContainer.xml"))
			{
				var res = (NullableContainer)XamlServices.Load(xr);
				Assert.AreEqual(5, res.TestProp, "#1");
			}
		}

		[Test]
		public void Write_DirectListContainer()
		{
			using (var xr = GetReader("DirectListContainer.xml"))
			{
				var res = (DirectListContainer)XamlServices.Load(xr);
				Assert.AreEqual(3, res.Items.Count, "#1");
				Assert.AreEqual("Hello3", res.Items[2].Value, "#2");
			}
		}

		[Test]
		public void Write_DirectDictionaryContainer()
		{
			using (var xr = GetReader("DirectDictionaryContainer.xml"))
			{
				var res = (DirectDictionaryContainer)XamlServices.Load(xr);
				Assert.AreEqual(3, res.Items.Count, "#1");
				Assert.AreEqual(40, res.Items[EnumValueType.Three], "#2");
			}
		}

		[Test]
		public void Write_DirectDictionaryContainer2()
		{
			using (var xr = GetReader("DirectDictionaryContainer2.xml"))
			{
				var res = (SecondTest.ResourcesDict2)XamlServices.Load(xr);
				Assert.AreEqual(2, res.Count, "#1");
				Assert.AreEqual("1", ((SecondTest.TestObject2)res["1"]).TestProperty, "#2");
				Assert.AreEqual("two", ((SecondTest.TestObject2)res["two"]).TestProperty, "#3");
			}
		}

		[Test]
		public void Write_NullableWithConverter()
		{
			using (var xr = GetReader("NullableWithConverter.xml"))
			{
				var res = (NullableWithTypeConverterContainer)XamlServices.Load(xr);
				Assert.IsNotNull(res.TestProp, "#1");
				Assert.AreEqual("SomeText", res.TestProp.Value.Text, "#2");
			}
		}

		[Test]
		public void Write_DeferredLoadingContainerMember()
		{
			using (var xr = GetReader("DeferredLoadingContainerMember.xml"))
			{
				var res = (DeferredLoadingContainerMember)XamlServices.Load(xr);
				Assert.IsNotNull(res, "#1");
				Assert.IsNotNull(res.Child, "#2");
				Assert.IsNull(res.Child.Foo, "#3");
				Assert.IsNotNull(res.Child.List, "#4");
				Assert.AreEqual(5, res.Child.List.Count, "#5");

				var obj = XamlServices.Load(res.Child.List.GetReader());
				Assert.IsNotNull(obj, "#6");
				Assert.IsInstanceOf<DeferredLoadingChild>(obj, "#7");
				Assert.AreEqual("Blah", ((DeferredLoadingChild)obj).Foo, "#8");
			}
		}		
		
		[Test]
		public void Write_DeferredLoadingContainerMember2()
		{
			using (var xr = GetReader("DeferredLoadingContainerMember2.xml"))
			{
				var res = (DeferredLoadingContainerMember2)XamlServices.Load(xr);
				var obj = res.Child();

				Assert.AreEqual("Blah", obj.Foo);
			}
		}

		[Test]
		public void Write_DeferredLoadingContainerType()
		{
			using (var xr = GetReader("DeferredLoadingContainerType.xml"))
			{
				var res = (DeferredLoadingContainerType)XamlServices.Load(xr);
				Assert.IsNotNull(res, "#1");
				Assert.IsNotNull(res.Child, "#2");
				Assert.IsNull(res.Child.Foo, "#3");
				Assert.IsNotNull(res.Child.List, "#4");
				Assert.AreEqual(5, res.Child.List.Count, "#5");

				var obj = XamlServices.Load(res.Child.List.GetReader());
				Assert.IsNotNull(obj, "#6");
				Assert.IsInstanceOf<DeferredLoadingChild2>(obj, "#7");
				Assert.AreEqual("Blah", ((DeferredLoadingChild2)obj).Foo, "#8");
			}
		}

		[Test]
		public void Write_DeferredLoadingWithInvalidType()
		{
			using (var xr = GetReader("DeferredLoadingWithInvalidType.xml"))
			{
				Assert.Throws<XamlSchemaException>(() => XamlServices.Load(xr));
			}
		}

		[Test]
		public void Write_DeferredLoadingContainerMemberStringType()
		{
			using (var xr = GetReader("DeferredLoadingContainerMemberStringType.xml"))
			{
				var res = (DeferredLoadingContainerMemberStringType)XamlServices.Load(xr);
				Assert.IsNotNull(res, "#1");
				Assert.IsNotNull(res.Child, "#2");
				Assert.IsNull(res.Child.Foo, "#3");
				Assert.IsNotNull(res.Child.List, "#4");
				Assert.AreEqual(5, res.Child.List.Count, "#5");

				var obj = XamlServices.Load(res.Child.List.GetReader());
				Assert.IsNotNull(obj, "#6");
				Assert.IsInstanceOf<DeferredLoadingChild>(obj, "#7");
				Assert.AreEqual("Blah", ((DeferredLoadingChild)obj).Foo, "#8");
			}
		}

		[Test]
		public void Write_DeferredLoadingCollectionContainer()
		{
			using (var xr = GetReader("DeferredLoadingCollectionContainer.xml"))
			{
				var res = (DeferredLoadingContainerType)XamlServices.Load(xr);
				Assert.IsNotNull(res, "#1");
				Assert.IsNotNull(res.Child, "#2");
				Assert.IsNull(res.Child.Foo, "#3");
				Assert.IsNotNull(res.Child.List, "#4");

				var obj = XamlServices.Load(res.Child.List.GetReader()) as DeferredLoadingChild2;
				Assert.IsNotNull(obj, "#6");
				Assert.IsNotNull(obj.Item, "#6");
				Assert.AreEqual(2, obj.Item.Items.Count, "#6");
			}
		}

#if !PCL136
		[Test]
		public void Write_ImmutableTypeWithNames()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");

			using (var xr = GetReader("ImmutableTypeWithNames.xml"))
			{
				var res = (NamedItem3)XamlServices.Load(xr);

				Assert.IsNotNull(res);
				Assert.AreEqual(2, res.ImmutableReferences.Length);
				Assert.AreEqual("i4", res.ImmutableReferences[0].ItemName);
				Assert.AreEqual(3, res.ImmutableReferences[0].ImmutableReferences.Length);
				Assert.AreEqual("i3", res.ImmutableReferences[0].ImmutableReferences[0].ItemName);
				Assert.AreEqual("i5", res.ImmutableReferences[0].ImmutableReferences[1].ItemName);
				Assert.AreEqual("i1", res.ImmutableReferences[0].ImmutableReferences[2].ItemName);
				Assert.AreEqual("i1", res.ImmutableReferences[0].Other.ItemName);

			}
		}
#endif

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeSingleArgument()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeSingleArgument.xml")) {
				var res = (ImmutableTypeSingleArgument)XamlServices.Load(xr);
				Assert.NotNull(res, "#1");
				Assert.AreEqual("hello", res.Name, "#2");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeMultipleArguments()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeMultipleArguments.xml")) {
				var res = (ImmutableTypeMultipleArguments)XamlServices.Load(xr);
				Assert.NotNull(res, "#1");
				Assert.AreEqual("hello", res.Name, "#2");
				Assert.IsTrue(res.Flag, "#3");
				Assert.AreEqual(100, res.Num, "#4");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeMultipleConstructors1()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeMultipleConstructors1.xml")) {
				var res = (ImmutableTypeMultipleConstructors)XamlServices.Load(xr);
				Assert.NotNull(res, "#1");
				Assert.AreEqual("hello", res.Name, "#2");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeMultipleConstructors2()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeMultipleConstructors2.xml")) {
				var res = (ImmutableTypeMultipleConstructors)XamlServices.Load(xr);
				Assert.NotNull(res, "#1");
				Assert.AreEqual("hello", res.Name, "#2");
				Assert.IsTrue(res.Flag, "#3");
				Assert.AreEqual(100, res.Num, "#4");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeMultipleConstructors3()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			// can't find constructor
			using (var xr = GetReader ("ImmutableTypeMultipleConstructors3.xml")) {
				Assert.Throws<XamlObjectWriterException> (() => XamlServices.Load(xr));
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeMultipleConstructors4()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			// found constructor, but one of the properties set is read only
			using (var xr = GetReader ("ImmutableTypeMultipleConstructors4.xml")) {
				Assert.Throws<XamlObjectWriterException> (() => XamlServices.Load(xr));
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeOptionalParameters1()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeOptionalParameters1.xml")) {
				var res = (ImmutableTypeOptionalParameters)XamlServices.Load(xr);
				Assert.AreEqual("hello", res.Name, "#1");
				Assert.AreEqual(true, res.Flag, "#2");
				Assert.AreEqual(100, res.Num, "#3");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeOptionalParameters2()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeOptionalParameters2.xml")) {
				var res = (ImmutableTypeOptionalParameters)XamlServices.Load(xr);
				Assert.AreEqual("hello", res.Name, "#1");
				Assert.AreEqual(true, res.Flag, "#2");
				Assert.AreEqual(200, res.Num, "#3");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeWithCollectionProperty()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeWithCollectionProperty.xml")) {
				var res = (ImmutableTypeWithCollectionProperty)XamlServices.Load(xr);
				Assert.AreEqual("hello", res.Name, "#1");
				Assert.AreEqual(true, res.Flag, "#2");
				Assert.AreEqual(200, res.Num, "#3");
				Assert.AreEqual(2, res.Collection.Count, "#4");
				Assert.AreEqual("Hello", res.Collection[0].Foo, "#5");
				Assert.AreEqual("There", res.Collection[1].Foo, "#6");
			}
		}

		[Test]
		[Category(Categories.NotOnSystemXaml)]
		public void Write_ImmutableTypeWithWritableProperty()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableTypeWithWritableProperty.xml")) {
				var res = (ImmutableTypeWithWritableProperty)XamlServices.Load(xr);
				Assert.AreEqual("hello", res.Name, "#1");
				Assert.AreEqual(true, res.Flag, "#2");
				Assert.AreEqual(200, res.Num, "#3");
				Assert.AreEqual("There", res.Foo, "#4");
			}
		}

#if !PCL136
		[Test]
		public void Write_ImmutableCollectionContainer()
		{
			if (!Compat.IsPortableXaml)
				Assert.Ignore("Not supported in System.Xaml");
			using (var xr = GetReader ("ImmutableCollectionContainer.xml")) {
				var res = (ImmutableCollectionContainer)XamlServices.Load(xr);
				Assert.IsNotNull(res, "#1");

				var expected = new [] { "Item1", "Item2", "Item3" };
				Assert.IsFalse(res.ImmutableArray.IsDefaultOrEmpty, "#2-1");
				CollectionAssert.AreEqual(expected, res.ImmutableArray.Select(r => r.Foo), "#2-2");

				Assert.IsFalse(res.ImmutableList.IsEmpty, "#3-1");
				CollectionAssert.AreEqual(expected, res.ImmutableList.Select(r => r.Foo), "#3-2");

				Assert.IsFalse(res.ImmutableQueue.IsEmpty, "#4-1");
				CollectionAssert.AreEqual(expected, res.ImmutableQueue.Select(r => r.Foo), "#4-2");

				Assert.IsFalse(res.ImmutableHashSet.IsEmpty, "#5-1");
				CollectionAssert.AreEquivalent(expected, res.ImmutableHashSet.Select(r => r.Foo), "#5-2");

				Assert.IsFalse(res.ImmutableStack.IsEmpty, "#6-1");
				CollectionAssert.AreEqual(expected.Reverse(), res.ImmutableStack.Select(r => r.Foo), "#6-2");

				Assert.IsFalse(res.ImmutableSortedSet.IsEmpty, "#7-1");
				CollectionAssert.AreEqual(expected, res.ImmutableSortedSet.Select(r => r.Foo), "#7-2");
			}
		}
#endif

		[Test]
		public void Write_GenericTypeWithClrNamespace ()
		{
			using (var xr = GetReader ("GenericTypeWithClrNamespace.xml")) {
				var des = (CustomGenericType<TestStruct>)XamlServices.Load (xr);
				Assert.AreEqual (4, des.Contents.Count, "#1");
				Assert.AreEqual ("1", des.Contents[0].Text, "#2");
				Assert.AreEqual ("2", des.Contents[1].Text, "#3");
				Assert.AreEqual ("3", des.Contents[2].Text, "#4");
				Assert.AreEqual ("4", des.Contents[3].Text, "#5");
			}
		}

		[Test]
		public void Write_GenericTypeWithXamlNamespace ()
		{
			using (var xr = GetReader ("GenericTypeWithXamlNamespace.xml")) {
				var des = (NamespaceTest.CustomGenericType<NamespaceTest.NamespaceTestClass>)XamlServices.Load (xr);
				Assert.AreEqual (4, des.Contents.Count, "#1");
				Assert.AreEqual ("1", des.Contents [0].Foo, "#2");
				Assert.AreEqual ("2", des.Contents [1].Foo, "#3");
				Assert.AreEqual ("3", des.Contents [2].Foo, "#4");
				Assert.AreEqual ("4", des.Contents [3].Foo, "#5");
			}
		}

		[Test]
		public void Read_InvalidPropertiesShouldThrowException()
		{
			Assert.Throws<XamlObjectWriterException>(() =>
			{
				XamlServices.Load(GetReader("InvalidPropertiesShouldThrowException.xml"));
			});
		}

		[Test]
		public void Write_Attached_Collection()
		{
			Attached4 result = null;

			var rsettings = new XamlXmlReaderSettings();
			using (var reader = new XamlXmlReader(new StringReader($@"<Attached4 xmlns=""{Compat.TestAssemblyNamespace}""><AttachedWrapper4.SomeCollection><TestClass4 Foo=""SomeValue""/></AttachedWrapper4.SomeCollection></Attached4>"), rsettings))
			{
				var wsettings = new XamlObjectWriterSettings();
				using (var writer = new XamlObjectWriter(reader.SchemaContext, wsettings))
				{
					XamlServices.Transform(reader, writer, false);
					result = (Attached4)writer.Result;
				}
			}

			Assert.AreEqual(1, result.Property.Count, "#1");
			Assert.AreEqual("SomeValue", result.Property[0].Foo, "#2");

		}

		[Test]
		public void Whitespace_ShouldBeCorrectlyHandled()
		{
			using (var xr = GetReader("Whitespace.xml"))
			{
				var des = (Whitespace)XamlServices.Load(xr);
				Assert.AreEqual("hello world", des.TabConvertedToSpaces);
				Assert.AreEqual("hello world", des.NewlineConvertedToSpaces);
				Assert.AreEqual("hello world", des.ConsecutiveSpaces);
				Assert.AreEqual("hello world", des.SpacesAroundTags);
				Assert.AreEqual("hello world", des.Child.Content);

				// TODO: xml:space="preserve" not yet implemented
				// Assert.AreEqual("  hello world\t", des.Preserve);
			}
		}

		[Test]
		public void CommandContainer()
		{
			using (var xr = GetReader("CommandContainer.xml"))
			{
				var commandContainer = (CommandContainer)XamlServices.Load(xr);
				Assert.IsNotNull(commandContainer);
				Assert.IsNotNull(commandContainer.Command1);
				Assert.IsInstanceOf<MyCommand>(commandContainer.Command1);
				Assert.IsNull(commandContainer.Command2);
			}
		}

		[Test]
		public void Write_UnknownContent()
		{
			var xw = new XamlObjectWriter(sctx);
			xw.WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, "x"));
			xw.WriteStartObject(xt3);

			Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartMember(XamlLanguage.UnknownContent));
		}

		[Test]
		public void Write_UnknownType()
		{
			var sw = new StringWriter();
			var xw = new XamlObjectWriter(sctx);
			xw.WriteStartObject(xt3);
			xw.WriteStartMember(xt3.GetMember("TestProp1"));

			// This is needed because .NET exception messages depend on the current UI culture, which may not always be English.
			CultureInfo.CurrentUICulture = new CultureInfo("en-us");

			var ex = Assert.Throws<XamlObjectWriterException>(() => xw.WriteStartObject(new XamlType("unk", "unknown", null, sctx)));
			Assert.AreEqual("Cannot create unknown type '{unk}unknown'.", ex.Message);
		}

		[Test]
		public void Write_DictionaryKeyProperty()
		{
			var xw = new XamlObjectWriter(sctx);
			var xDictionaryContainer = sctx.GetXamlType(typeof(DictionaryContainer));
			var xDictionaryContainerItems = xDictionaryContainer.GetMember(nameof(DictionaryContainer.Items));
			var xDictionaryItem = sctx.GetXamlType(typeof(DictionaryItem));
			var xDictionaryItemKey = xDictionaryItem.GetMember(nameof(DictionaryItem.Key));
			const string key = "Key";

			xw.WriteNamespace(new NamespaceDeclaration(XamlLanguage.Xaml2006Namespace, "x"));
			xw.WriteStartObject(xDictionaryContainer);
			xw.WriteStartMember(xDictionaryContainerItems);
			xw.WriteGetObject();
			xw.WriteStartMember(XamlLanguage.Items);

			xw.WriteStartObject(xDictionaryItem);
			xw.WriteStartMember(xDictionaryItemKey);
			xw.WriteValue(key);
			xw.WriteEndMember();
			xw.WriteEndObject();

			xw.WriteEndMember();
			xw.WriteEndObject();
			xw.WriteEndMember();
			xw.WriteEndObject();

			var result = (DictionaryContainer)xw.Result;
			Assert.IsTrue(result.Items.TryGetValue(key, out DictionaryItem item));
			Assert.AreEqual(key, item.Key);
		}

		[Test]
		public void TestISupportInitializeBeginInitEqualsEndInit()
		{
			var xml =
$@"<TestClass7 
		xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases' 
		xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' />".UpdateXml();
			
			XamlSchemaContext context = new XamlSchemaContext();

			TextReader tr = new StringReader(xml);

			XamlObjectWriterSettings xows = new XamlObjectWriterSettings()
			{
				RootObjectInstance = new TestClass7()
			};

			XamlObjectWriter ow = new XamlObjectWriter(context, xows);
			XamlXmlReader r = new XamlXmlReader(tr);

			XamlServices.Transform(r, ow);

			var testClass = (TestClass7)ow.Result;

			Assert.AreEqual(0, testClass.State);
		}
		
		[Test]
		public void TestIsUsableDuringInitializationCorrectUsingOnMemberStart()
		{
			//NOTE: The assertion are happen in the TestClass8! Here just invoking methods 

			if (Compat.IsPortableXaml && !Compat.HasISupportInitializeInterface)
				Assert.Ignore("The ISupportInitialize starts support from netstandard20");

			XamlSchemaContext context = new XamlSchemaContext();
		
			XamlObjectWriterSettings xows = new XamlObjectWriterSettings();

			XamlObjectWriter ow = new XamlObjectWriter(context, xows);

			var parentXamlType = new XamlType(typeof(TestClass8), context);
			var childXamlType = new XamlType(typeof(TestClass9), context);
			
			Assert.IsTrue(childXamlType.IsUsableDuringInitialization);
			
			var xamlMemberFoo = childXamlType.GetMember(nameof(TestClass9.Foo));
			var xamlMemberBaz = childXamlType.GetMember(nameof(TestClass9.Baz));
			var xamlMemberBar = parentXamlType.GetMember(nameof(TestClass8.Bar));

			ow.WriteStartObject(parentXamlType);
			ow.WriteStartMember(xamlMemberBar);

			ow.WriteStartObject(childXamlType);
			ow.WriteStartMember(xamlMemberFoo);
			ow.WriteStartObject(xamlMemberFoo.Type);
			ow.WriteEndObject();
			ow.WriteEndMember();
			ow.WriteStartMember(xamlMemberBaz);
			ow.WriteValue("Test");
			ow.WriteEndMember();
			ow.WriteEndObject();

			ow.WriteEndMember();
			ow.WriteEndObject();

			var result = (TestClass8)ow.Result;
			Assert.IsTrue(result.Bar.IsInitialized);
			Assert.IsNotNull(result.Bar.Foo);
			Assert.AreEqual(result.Bar.Baz, "Test");
		}

		[Test]
		public void TestIsUsableDuringInitializationWithCollection()
		{
			string xml =
				@"<TestClass10 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
					<TestClass9 Baz='Test1' Bar='42'/>
					<TestClass9 Baz='Test2'/>
					<TestClass9/>
					<TestClass9/>
				  </TestClass10>".UpdateXml();

			// Note: The most important assert is invoked inside the TestClass10 (CollectionChanged).
			var result = (TestClass10)XamlServices.Parse(xml);

			Assert.AreEqual(4, result.Items.Count);

			Assert.AreEqual("Test1", result.Items[0].Baz);
			Assert.AreEqual(42, result.Items[0].Bar);

			Assert.AreEqual("Test2", result.Items[1].Baz);
			Assert.Zero(result.Items[1].Bar);

			Assert.IsNull(result.Items[2].Baz);
			Assert.Zero(result.Items[2].Bar);
		}
		
		[Test]
		public void CollectionShouldNotBeAssigned()
		{
			var xml = $@"
<CollectionAssignnmentTest xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
    <TestClass4/>	
</CollectionAssignnmentTest>".UpdateXml();
			var result = (CollectionAssignnmentTest)XamlServices.Parse(xml);

			Assert.False(result.Assigned);
			Assert.AreEqual(1, result.Items.Count);
		}

		[Test]
		public void CollectionShouldNotBeAssigned2()
		{
			var xml = $@"
<CollectionAssignnmentTest xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
    <TestClass4/>	
    <TestClass4/>	
</CollectionAssignnmentTest>".UpdateXml();
			var result = (CollectionAssignnmentTest)XamlServices.Parse(xml);

			Assert.False(result.Assigned);
			Assert.AreEqual(2, result.Items.Count);
		}

		[Test]
		public void CollectionShouldBeAssigned()
		{
			var xml = $@"
<CollectionAssignnmentTest xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
					  	   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
						   xmlns:scg='clr-namespace:System.Collections.Generic;assembly=mscorlib'>
	<scg:List x:TypeArguments='TestClass4'>
		<TestClass4/>	
		<TestClass4/>	
	</scg:List>
</CollectionAssignnmentTest>".UpdateXml();
			var result = (CollectionAssignnmentTest)XamlServices.Parse(xml);

			Assert.True(result.Assigned);
			Assert.AreEqual(2, result.Items.Count);
		}

		[Test]
		public void ExceptionShouldBeThrownForNotFoundType()
		{
			string xml = @"<TestClass10 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
    <NotFound/>
</TestClass10>".UpdateXml();
			var ex = Assert.Throws<XamlObjectWriterException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(2, ex.LineNumber);
			Assert.AreEqual(6, ex.LinePosition);
		}

		[Test]
		public void ExceptionShouldBeThrownForNotFoundProperty()
		{
			string xml = @"<TestClass9 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
    Baz='baz'
    NotFound='foo'/>".UpdateXml();
			var ex = Assert.Throws<XamlObjectWriterException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(3, ex.LineNumber);
			Assert.AreEqual(5, ex.LinePosition);
		}

		[Test]
		public void ExceptionShouldBeThrownForInvalidPropertyValue()
		{
			string xml = @"<TestClass9 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
    Baz='baz'
    Bar='foo'/>".UpdateXml();
			var ex = Assert.Throws<XamlObjectWriterException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(3, ex.LineNumber);
			Assert.AreEqual(5, ex.LinePosition);
		}

		[Test]
		public void ExceptionShouldBeThrownWhenSetterThrows()
		{
			string xml = @"<SetterThatThrows xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
    Throw='foo'/>".UpdateXml();
			var ex = Assert.Throws<XamlObjectWriterException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(2, ex.LineNumber);
			Assert.AreEqual(5, ex.LinePosition);
			Assert.IsInstanceOf<NotSupportedException>(ex.InnerException);
			Assert.AreEqual("Whoops!", ex.InnerException.Message);
		}

		[Test]
		public void ExceptionShouldBeThrownForDuplicateAttribute()
		{
			string xml = @"<TestClass9 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
    Baz='foo'
    Baz='bar'/>".UpdateXml();
			var ex = Assert.Throws<XmlException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(3, ex.LineNumber);
			Assert.AreEqual(5, ex.LinePosition);
			Assert.AreEqual("'Baz' is a duplicate attribute name. Line 3, position 5.", ex.Message);
		}

		[Test]
		public void ExceptionShouldBeThrownForDuplicateContent()
		{
			string xml = @"<ContentIncludedClass xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'
												 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<x:String>Foo</x:String>
	<x:String>Bar</x:String>
</ContentIncludedClass>".UpdateXml();
			var ex = Assert.Throws<XamlDuplicateMemberException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(4, ex.LineNumber);
			Assert.AreEqual(3, ex.LinePosition);
			Assert.AreEqual(typeof(ContentIncludedClass), ex.ParentType.UnderlyingType);
			Assert.AreEqual("Content", ex.DuplicateMember.Name);
			Assert.AreEqual("''Content' property has already been set on 'ContentIncludedClass'.' Line number '4' and line position '3'.", ex.Message);
		}

		[Test]
		public void ExceptionShouldBeThrownForDuplicateElement()
		{
			string xml = @"<TestClass9 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases'>
  <TestClass9.Baz>foo</TestClass9.Baz>
  <TestClass9.Baz>bar</TestClass9.Baz>
</TestClass9>".UpdateXml();
			var ex = Assert.Throws<XamlDuplicateMemberException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(3, ex.LineNumber);

			// System.Xaml reports column 4 here but we report column 19. 19 actually makes more sense here so don't test this.
			//
			//Assert.AreEqual(4, ex.LinePosition);
			//Assert.AreEqual("''Baz' property has already been set on 'TestClass9'.' Line number '3' and line position '4'.", ex.Message);
		}

		[Test]
		public void ExceptionShouldBeThrownForDuplicateAttributeAndElement()
		{
			string xml = @"<TestClass9 xmlns='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.TestCases' Baz='foo'>
  <TestClass9.Baz>foo</TestClass9.Baz>
</TestClass9>".UpdateXml();
			var ex = Assert.Throws<XamlDuplicateMemberException>(() => ParseWithLineInfo(xml));
			Assert.AreEqual(2, ex.LineNumber);

			// System.Xaml reports column 4 here but we report column 19. 19 actually makes more sense here so don't test this.
			//
			// Assert.AreEqual(4, ex.LinePosition);
			// Assert.AreEqual("''Baz' property has already been set on 'TestClass9'.' Line number '2' and line position '4'.", ex.Message);
		}

		object ParseWithLineInfo(string xaml)
		{
			var stringReader = new StringReader(xaml);
			var settings = new XamlXmlReaderSettings { ProvideLineInfo = true };
			var xamlReader = new XamlXmlReader(stringReader, settings);
			return XamlServices.Load(xamlReader);
		}
	}
}
