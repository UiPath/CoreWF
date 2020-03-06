using System;
using NUnit.Framework;
#if PCL
using System.Windows.Markup;

using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.ComponentModel;
using System.Xaml;
using System.Xaml.Schema;
#endif

namespace MonoTests.System.Xaml
{
	[TestFixture]
	public class XamlDeferringLoaderTest
	{
		[Test]
		public void TestDeferredLoaderProperty()
		{
			var sc = new XamlSchemaContext();
			var xt = sc.GetXamlType(typeof(DeferredLoadingContainerMember));
			var xm = xt.GetMember("Child");
			Assert.IsNotNull(xm.DeferringLoader, "#1 Deferring Loader should be set on member");
			Assert.IsNull(xm.Type.DeferringLoader, "#2 Not set on type, should be null");
			Assert.AreEqual(typeof(TestDeferredLoader), xm.DeferringLoader.ConverterType, "#3");
			Assert.IsNull(xm.DeferringLoader.TargetType, "#4 This should be null for some reason");
		}

		[Test]
		public void TestDeferredLoaderType()
		{
			var sc = new XamlSchemaContext();
			var xt = sc.GetXamlType(typeof(DeferredLoadingContainerType));
			var xm = xt.GetMember("Child");
			Assert.IsNotNull(xm.DeferringLoader, "#1 Deferring Loader should be set on member");
			Assert.IsNotNull(xm.Type.DeferringLoader, "#2 Deferring Loader should be set on type");
			Assert.IsTrue(ReferenceEquals(xm.DeferringLoader, xm.Type.DeferringLoader), "#3 Deferring loaders should be the same instance from type or member");
			Assert.AreEqual(typeof(TestDeferredLoader2), xm.DeferringLoader.ConverterType, "#3");
			Assert.IsNull(xm.DeferringLoader.TargetType, "#4 This should be null for some reason");
		}
	}
}

