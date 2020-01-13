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
using System.ComponentModel;
using System.Reflection;
using NUnit.Framework;
using MonoTests.System.Xaml;
#if PCL
using System.Xaml.Markup;
using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
#endif

using Category = NUnit.Framework.CategoryAttribute;

namespace MonoTests.System.Xaml.Markup
{
	[TestFixture]
	public class StaticExtensionTest
	{
		[Test]
		public void ProvideValueWithoutType ()
		{
			var x = new StaticExtension ();
			// it fails because it cannot be resolved to a static member.
			// This possibly mean, there might be a member that 
			// could be resolved only with the name, without type.
			x.Member = "Foo";
			Assert.Throws<ArgumentException> (() => x.ProvideValue (null));
		}

		[Test]
		public void ProvideValueWithoutMember ()
		{
			var x = new StaticExtension ();
			x.MemberType = typeof (int);
			Assert.Throws<InvalidOperationException> (() => x.ProvideValue (null));
		}

		[Test]
		public void ProvideValueInstanceProperty ()
		{
			var x = new StaticExtension ();
			x.MemberType = typeof (StaticExtension);
			x.Member = "MemberType"; // instance property is out of scope.
			Assert.Throws<ArgumentException> (() => x.ProvideValue (null));
		}

		[Test]
		public void ProvideValueStaticProperty ()
		{
			var x = new StaticExtension ();
			x.MemberType = typeof (XamlLanguage);
			x.Member = "Array";
			Assert.AreEqual (XamlLanguage.Array, x.ProvideValue (null), "#1");
		}

		[Test]
		public void ProvideValueConst ()
		{
			var x = new StaticExtension ();
			x.MemberType = typeof (XamlLanguage);
			x.Member = "Xaml2006Namespace";
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, x.ProvideValue (null), "#1");
		}

		[Test]
		public void ProvideValuePrivateConst ()
		{
			var x = new StaticExtension ();
			x.MemberType = GetType ();
			x.Member = "FooBar"; // private const could not be resolved.
			Assert.Throws<ArgumentException> (() => x.ProvideValue (null), "#1");
		}

		const string FooBar = "foobar";


		[Test]
		public void ProvideValueEvent ()
		{
			var x = new StaticExtension ();
			x.MemberType = GetType ();
			x.Member = "FooEvent"; // private const could not be resolved.
			Assert.Throws<ArgumentException> (() => x.ProvideValue (null), "#1");
		}

		#pragma warning disable 67
		public static event EventHandler<EventArgs> FooEvent;
		#pragma warning restore 67

		[Test]
		public void ProvideValueWithMemberOnly()
		{
			const string xaml = "<x:Static xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:foo='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.Test' Member='foo:StaticClass1.FooBar' />";
			var result = XamlServices.Parse(xaml.UpdateXml());
			Assert.AreEqual("test", result);
		}

		[Test]
		public void ProvideValueFromChildEnum()
		{
			const string xaml = "<x:Static xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:foo='clr-namespace:MonoTests.System.Xaml;assembly=System.Xaml.Test' Member='foo:StaticClass1+MyEnum.EnumValue2' />";
			var result = XamlServices.Parse(xaml.UpdateXml());
			Assert.AreEqual(StaticClass1.MyEnum.EnumValue2, result);
		}
	}
}
