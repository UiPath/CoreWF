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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.IO;
#if PCL
using System.Xaml.Markup;
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
	public class XamlSchemaContextTest
	{
		XamlSchemaContext NewStandardContext ()
		{
			return new XamlSchemaContext (new Assembly [] {typeof (XamlSchemaContext).GetTypeInfo().Assembly });
		}

		XamlSchemaContext NewThisAssemblyContext ()
		{
			return new XamlSchemaContext (new Assembly [] {GetType ().GetTypeInfo().Assembly });
		}

		[Test]
		public void ConstructorNullAssemblies ()
		{
			// allowed.
			var ctx = new XamlSchemaContext ((Assembly []) null);
			Assert.IsFalse (ctx.FullyQualifyAssemblyNamesInClrNamespaces, "#1");
			Assert.IsFalse (ctx.SupportMarkupExtensionsWithDuplicateArity, "#2");
			Assert.IsNull (ctx.ReferenceAssemblies, "#3");
		}

		[Test]
		public void ConstructorNullSettings ()
		{
			// allowed.
			new XamlSchemaContext ((XamlSchemaContextSettings) null);
		}

		[Test]
		public void ConstructorNoAssembly ()
		{
			new XamlSchemaContext (new Assembly [0]);
		}

		[Test]
		public void Constructor ()
		{
			var ctx = new XamlSchemaContext (new Assembly [] {typeof (XamlSchemaContext).GetTypeInfo().Assembly });
			Assert.AreEqual (1, ctx.ReferenceAssemblies.Count, "#1");
		}

		[Test]
		public void GetAllXamlNamespaces ()
		{
			var ctx = new XamlSchemaContext (null, null);
			var arr = ctx.GetAllXamlNamespaces ().ToArray ();
			Assert.AreEqual (6, arr.Length, "#1");
			Assert.IsTrue (arr.Contains (XamlLanguage.Xaml2006Namespace), "#1-2");
			Assert.IsTrue (arr.Contains ("urn:mono-test"), "#1-3");
            Assert.IsTrue(arr.Contains("urn:mono-test2"), "#1-4");
            Assert.IsTrue(arr.Contains("urn:bar"), "#1-5");

            ctx = NewStandardContext ();
			arr = ctx.GetAllXamlNamespaces ().ToArray ();
			Assert.AreEqual (1, arr.Length, "#2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, arr [0], "#2-2");

			ctx = NewThisAssemblyContext ();
			arr = ctx.GetAllXamlNamespaces ().ToArray ();
			Assert.AreEqual (5, arr.Length, "#3");
			Assert.IsTrue (arr.Contains ("urn:mono-test"), "#3-2");
			Assert.IsTrue (arr.Contains ("urn:mono-test2"), "#3-3");
            Assert.IsTrue(arr.Contains("urn:bar"), "#3-4");
        }

        [Test]
		public void GetPreferredPrefixNull ()
		{
			var ctx = new XamlSchemaContext (null, null);
			Assert.Throws<ArgumentNullException> (() => ctx.GetPreferredPrefix (null));
		}

		[Test]
		public void GetPreferredPrefix ()
		{
			var ctx = new XamlSchemaContext (null, null);
			Assert.AreEqual ("x", ctx.GetPreferredPrefix (XamlLanguage.Xaml2006Namespace), "#1");
			Assert.AreEqual ("p", ctx.GetPreferredPrefix ("urn:4mbw93w89mbh"), "#2"); // ... WTF "p" ?
			Assert.AreEqual ("p", ctx.GetPreferredPrefix ("urn:etbeoesmj"), "#3"); // ... WTF "p" ?
		}

		[Test]
		public void TryGetCompatibleXamlNamespaceNull ()
		{
			var ctx = new XamlSchemaContext (null, null);
			string dummy;
			Assert.Throws<ArgumentNullException> (() => ctx.TryGetCompatibleXamlNamespace (null, out dummy));
		}

		[Test]
		public void TryGetCompatibleXamlNamespace ()
		{
			var ctx = new XamlSchemaContext (null, null);
			string dummy;
			Assert.IsFalse (ctx.TryGetCompatibleXamlNamespace (String.Empty, out dummy), "#1");
			Assert.IsNull (dummy, "#1-2"); // this shows the fact that the out result value for false case is not trustworthy.

			ctx = NewThisAssemblyContext ();
			Assert.IsFalse (ctx.TryGetCompatibleXamlNamespace (String.Empty, out dummy), "#2");

            // test XmlnsCompatibleWith when subsuming namespace is defined, should find both.
            Assert.IsTrue (ctx.TryGetCompatibleXamlNamespace ("urn:bar", out dummy), "#3");
			Assert.IsTrue (ctx.TryGetCompatibleXamlNamespace ("urn:foo", out dummy), "#4");
			Assert.AreEqual ("urn:bar", dummy, "#5");

            // should not find a compatible namespace when XmlnsCompatibleWith is used with undefined subsuming namespace
            Assert.IsFalse(ctx.TryGetCompatibleXamlNamespace("urn:bar2", out dummy), "#6");
            Assert.IsFalse(ctx.TryGetCompatibleXamlNamespace("urn:foo2", out dummy), "#7");
        }

        /*
                    var settings = new XamlSchemaContextSettings () { FullyQualifyAssemblyNamesInClrNamespaces = true };
                    ctx = new XamlSchemaContext (new Assembly [] {typeof (XamlSchemaContext).Assembly }, settings);

                    ctx = new XamlSchemaContext (new Assembly [] {GetType ().Assembly }, settings);
                    arr = ctx.GetAllXamlNamespaces ().ToArray ();
                    Assert.AreEqual (2, arr.Length, "#5");
                    Assert.IsTrue (arr.Contains ("urn:mono-test"), "#5-2");
                    Assert.IsTrue (arr.Contains ("urn:mono-test2"), "#5-3");
                }
        */

        [Test]
		public void GetXamlTypeAndAllXamlTypes ()
		{
			var ctx = new XamlSchemaContext (new Assembly [] {typeof (string).GetTypeInfo().Assembly }); // build with corlib.
			Assert.AreEqual (0, ctx.GetAllXamlTypes (XamlLanguage.Xaml2006Namespace).Count (), "#0"); // premise

			var xt = ctx.GetXamlType (typeof (string));
			Assert.IsNotNull (xt, "#1");
			Assert.AreEqual (typeof (string), xt.UnderlyingType, "#2");
			Assert.IsTrue (object.ReferenceEquals (xt, ctx.GetXamlType (typeof (string))), "#3");

			// non-primitive type example
			Assert.IsTrue (object.ReferenceEquals (ctx.GetXamlType (GetType ()), ctx.GetXamlType (GetType ())), "#4");

			// after getting these types, it still returns 0. So it's not all about caching.
			Assert.AreEqual (0, ctx.GetAllXamlTypes (XamlLanguage.Xaml2006Namespace).Count (), "#5");
		}

		[Test]
		public void AddGetAllXamlTypesToEmpty ()
		{
			var ctx = NewStandardContext ();
			Assert.Throws<NotSupportedException> (() => ctx.GetAllXamlTypes ("urn:foo").Add (new XamlType (typeof (int), ctx)));
		}

		[Test]
		public void GetAllXamlTypesInXaml2006Namespace ()
		{
			var ctx = NewStandardContext ();

			// There are some special types that have non-default name: MemberDefinition, PropertyDefinition

			var l = ctx.GetAllXamlTypes (XamlLanguage.Xaml2006Namespace);
			Assert.IsTrue (l.Count () > 40, "#1");
			Assert.IsTrue (l.Any (t => t.UnderlyingType == typeof (MemberDefinition)), "#2");
			Assert.IsTrue (l.Any (t => t.Name == "AmbientAttribute"), "#3");
			Assert.IsTrue (l.Any (t => t.Name == "XData"), "#4");
			Assert.IsTrue (l.Any (t => t.Name == "ArrayExtension"), "#5");
			Assert.IsTrue (l.Any (t => t.Name == "StaticExtension"), "#6");
			// FIXME: enable these tests when I sort out how these special names are filled.
			//Assert.IsTrue (l.Any (t => t.Name == "Member"), "#7");
			//Assert.IsTrue (l.Any (t => t.Name == "Property"), "#8");
			//Assert.IsFalse (l.Any (t => t.Name == "MemberDefinition"), "#9");
			//Assert.IsFalse (l.Any (t => t.Name == "PropertyDefinition"), "#10");
			//Assert.AreEqual ("MemberDefinition", new XamlType (typeof (MemberDefinition), new XamlSchemaContext (null, null)).Name);
			//Assert.AreEqual ("Member", l.GetAllXamlTypes (XamlLanguage.Xaml2006Namespace).First (t => t.UnderlyingType == typeof (MemberDefinition)));
			Assert.IsFalse (l.Any (t => t.Name == "Array"), "#11");
			Assert.IsFalse (l.Any (t => t.Name == "Null"), "#12");
			Assert.IsFalse (l.Any (t => t.Name == "Static"), "#13");
			Assert.IsFalse (l.Any (t => t.Name == "Type"), "#14");
			Assert.IsTrue (l.Contains (XamlLanguage.Type), "#15");
			Assert.IsFalse (l.Contains (XamlLanguage.String), "#16"); // huh?
			Assert.IsFalse (l.Contains (XamlLanguage.Object), "#17"); // huh?
			Assert.IsTrue (l.Contains (XamlLanguage.Array), "#18");
			Assert.IsFalse (l.Contains (XamlLanguage.Uri), "#19");
		}

		[Test]
		public void GetXamlTypeByName ()
		{
			var ns = XamlLanguage.Xaml2006Namespace;
			var ctx = NewThisAssemblyContext ();
			//var ctx = NewStandardContext ();
			XamlType xt;

			Assert.IsNull (ctx.GetXamlType (new XamlTypeName ("urn:foobarbaz", "bar")));

			xt = ctx.GetXamlType (new XamlTypeName (ns, "Int32"));
			Assert.IsNotNull (xt, "#1");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Int32", new XamlTypeName [] {new XamlTypeName (ns, "Int32")}));
			Assert.IsNull (xt, "#1-2");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Uri"));
			Assert.IsNotNull (xt, "#2");

			// Compare those results to GetAllXamlTypesInXaml2006Namespace() results,
			// which asserts that types with those names are *not* included.
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Array"));
			Assert.IsNotNull (xt, "#3");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Property"));
			Assert.IsNotNull (xt, "#4");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Null"));
			Assert.IsNotNull (xt, "#5");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Static"));
			Assert.IsNotNull (xt, "#6");
			xt = ctx.GetXamlType (new XamlTypeName (ns, "Type"));
			Assert.IsNotNull (xt, "#7");
		}

		[Test]
		public void GetTypeForRuntimeType ()
		{
			var ctx = NewStandardContext ();

			// There are some special types that have non-default name: MemberDefinition, PropertyDefinition

			var xt = ctx.GetXamlType (typeof (Type));
			Assert.AreEqual ("Type", xt.Name, "#1-1");
			Assert.AreEqual (typeof (Type), xt.UnderlyingType, "#1-2");

			xt = ctx.GetXamlType (new XamlTypeName (XamlLanguage.Xaml2006Namespace, "Type")); // becomes TypeExtension, not Type
			Assert.AreEqual ("TypeExtension", xt.Name, "#2-1");
			Assert.AreEqual (typeof (TypeExtension), xt.UnderlyingType, "#2-2");
		}

		[TestCase(typeof(bool))]
		[TestCase(typeof(byte))]
		[TestCase(typeof(char))]
		[TestCase(typeof(DateTime))]
		[TestCase(typeof(decimal))]
		[TestCase(typeof(double))]
		[TestCase(typeof(Int16))]
		[TestCase(typeof(Int32))]
		[TestCase(typeof(Int64))]
		[TestCase(typeof(float))]
		[TestCase(typeof(string))]
		[TestCase(typeof(TimeSpan))]
		public void GetTypeFromXamlTypeNameWithClrName (Type type)
		{
			// ensure that this does *not* resolve clr type name.
			var xn = new XamlTypeName ("clr-namespace:System;assembly=mscorlib", type.Name);
			var ctx = NewStandardContext ();
			var xt = ctx.GetXamlType (xn);
			Assert.IsNull (xt, "#1");

			ctx = new XamlSchemaContext ();
			xt = ctx.GetXamlType (xn);
			Assert.IsNotNull (xt, "#2");
		}

		[Test]
		public void GetAbstractType()
		{
			var ctx = new XamlSchemaContext ();
			var xt = ctx.GetXamlType (typeof(AbstractObject));
			Assert.IsNotNull (xt, "#1");
		}

		[Test]
		public void GetAbstractTypeFromClrNamespace()
		{
			var ctx = new XamlSchemaContext();
			var tn = new XamlTypeName(Compat.TestAssemblyNamespace, "AbstractObject");
			var xt = ctx.GetXamlType(tn);
			Assert.IsNotNull(xt, "#1");
			Assert.IsNotNull(xt.UnderlyingType, "#2");
		}

		[Test]
		public void GetAbstractTypeFromUriNamespace()
		{
			var ctx = new XamlSchemaContext();
			var tn = new XamlTypeName("urn:mono-test", "AbstractObject");
			var xt = ctx.GetXamlType(tn);
			Assert.IsNotNull(xt, "#1");
			Assert.IsNotNull(xt.UnderlyingType, "#2");
		}

		[Test]
		public void AttachableMemberTypeShouldBeCorrectWhenReadOnly()
		{
			var ctx = new XamlSchemaContext();
			var xt = ctx.GetXamlType(typeof(AttachedWrapper4));
			Assert.IsNotNull(xt, "#1");
			var xm = xt.GetAttachableMember("SomeCollection");
			Assert.IsNotNull(xm, "#2");
			Assert.AreEqual(typeof(List<TestClass4>), xm.Type.UnderlyingType, "#3");
		}
		[Test]
		public void AttachableMemberTypeShouldBeCorrect()
		{
			var ctx = new XamlSchemaContext();
			var xt = ctx.GetXamlType(typeof(AttachedWrapper5));
			Assert.IsNotNull(xt, "#1");
			var xm = xt.GetAttachableMember("SomeCollection");
			Assert.IsNotNull(xm, "#2");
			Assert.AreEqual(typeof(List<TestClass4>), xm.Type.UnderlyingType, "#3");
		}

		[Test]
		public void PassesNullToGetXamlType_typeArguments_ForNoArguments()
		{
			var xml = File.ReadAllText(Compat.GetTestFile("Int32.xml")).UpdateXml();
			var ctx = new TestGetXamlTypeArgumentsNull();
			var reader = new XamlXmlReader(new StringReader(xml), ctx);
			var writer = new XamlObjectWriter(ctx);

			XamlServices.Transform(reader, writer);

			Assert.True(ctx.Invoked);
		}

		private class TestGetXamlTypeArgumentsNull : XamlSchemaContext
		{
			public bool Invoked { get; set; }

			protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
			{
				Assert.IsNull(typeArguments);
				Invoked = true;
				return base.GetXamlType(xamlNamespace, name, typeArguments);
			}
		}
	}
}
