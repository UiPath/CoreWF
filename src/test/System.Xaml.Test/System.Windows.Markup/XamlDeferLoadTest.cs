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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Windows.Markup;
using MonoTests.System.Xaml;
#if PCL
using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
#endif

using Category = NUnit.Framework.CategoryAttribute;

namespace MonoTests.System.Windows.Markup
{
	[TestFixture]
	public class XamlDeferLoadTest
	{
		[Test]
		[TestCase("something", null)]
		[TestCase(null, "something")]
		[TestCase(null, null)]
		public void ConstructorNullNameString (string loaderType, string contentType)
		{
			Assert.Throws<ArgumentNullException> (() => new XamlDeferLoadAttribute(loaderType, contentType));
		}

		[Test]
		[TestCase(typeof(TestDeferredLoader), null)]
		[TestCase(null, typeof(DeferredLoadingChild))]
		[TestCase(null, null)]
		public void ConstructorNullNameType (Type loaderType, Type contentType)
		{
			Assert.Throws<ArgumentNullException> (() => new XamlDeferLoadAttribute(loaderType, contentType));
		}

		[Test]
		public void TypeShouldReturnName()
		{
			var attr = new XamlDeferLoadAttribute(typeof(TestDeferredLoader), typeof(DeferredLoadingChild));
			Assert.AreEqual(typeof(TestDeferredLoader).AssemblyQualifiedName, attr.LoaderTypeName, "#1");
			Assert.AreEqual(typeof(DeferredLoadingChild).AssemblyQualifiedName, attr.ContentTypeName, "#2");
		}

		[Test]
		public void TypeNameShouldNotSetType()
		{
			var attr = new XamlDeferLoadAttribute(typeof(TestDeferredLoader).AssemblyQualifiedName, typeof(DeferredLoadingChild).AssemblyQualifiedName);
			Assert.IsNull(attr.LoaderType, "#1");
			Assert.IsNull(attr.ContentType, "#2");
		}
	}
}
