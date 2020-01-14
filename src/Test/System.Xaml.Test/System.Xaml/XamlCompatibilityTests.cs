using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
#if !NET_4_5
using System.Xaml;
using System.Xaml.Schema;
#else
using System.Xaml;
using System.Xaml.Schema;
#endif
namespace MonoTests.System.Xaml
{
	[TestFixture]
    public class XamlCompatibilityTests
    {

	    void XmlEquals(XElement first, XElement second)
	    {
		    Assert.AreEqual(first.ToString(), second.ToString());
	    }


#if !NET_4_5
		[Test]
	    public void CheckIgnorable()
		{
			var xml = @"
<Root xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' 
     xmlns:i1='i1Uri' xmlns:i2='i2Uri' 
	xmlns:compat='mapped' xmlns:ignoredCompat='mapped2' xmlns:ignoredCompat2='preserve' 
	mc:Ignorable='i1 ignoredCompat ignoredCompat2'>
    <Element i2:ShouldNotIgnore='1'/>
	<i1:ShouldIgnore/>
	<i1:ShouldIgnore>
		<i2:ShouldIgnore/>
	</i1:ShouldIgnore>
    <Element i2:ShouldNotIgnore='1'>
		<i1:ShouldIgnore/> 
		<i2:ShouldNotIgnore/>
		<i2:ShouldNotIgnore>
			<i2:ShouldNotIgnore/>
		</i2:ShouldNotIgnore>
		<i1:ShouldIgnore> 
			<i2:ShouldIgnore/>
		</i1:ShouldIgnore> 
	</Element>
    <Element i1:ShouldAlwaysIgnore='1' mc:Ignorable='i2' i2:ShouldIgnore='1'/>
    <Element i1:ShouldAlwaysIgnore='1' mc:Ignorable='i2' i2:ShouldIgnore='1'>
		<i2:ShouldIgnore/>
		<i2:ShouldIgnore>
			<ShouldIgnore/>
		</i2:ShouldIgnore>
		<Element i2:ShouldIgnore='1'/>
	</Element>
	<Element i2:ShouldNotIgnore='1'/>
    <Element compat:ShouldMap='1' ignoredCompat:ShouldMap='1' ignoredCompat2:ShouldMapAndPreserve='1'/>
</Root>";
			// TODO: Use XamlXmlParser directly with compaibility mode turned on.
			var rdr = new CompatibleXmlReader(XmlReader.Create(new StringReader(xml)),
				(string ns, out string mapped) =>
				{
					if (ns == "mapped")
					{
						mapped = "mappedTo";
						return true;
					}
					if (ns == "mapped2")
					{
						mapped = "mappedTo2";
						return true;
					}
					if (ns == "preserve")
					{
						mapped = "preserve";
						return true;
					}
					mapped = null;
					return false;
				});
			var actual = XElement.Load(rdr).ToString();
			var expected =
				@"
<Root xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' xmlns:i1='i1Uri' xmlns:i2='i2Uri' xmlns:compat='mappedTo' xmlns:ignoredCompat='mappedTo2' xmlns:ignoredCompat2='preserve'>
    <Element i2:ShouldNotIgnore='1' />
	<Element i2:ShouldNotIgnore='1'>
		<i2:ShouldNotIgnore />
		<i2:ShouldNotIgnore>
			<i2:ShouldNotIgnore />
		</i2:ShouldNotIgnore>
		</Element>
    <Element />
    <Element>
		<Element />
	</Element>
	<Element i2:ShouldNotIgnore='1' />
    <Element compat:ShouldMap='1' ignoredCompat:ShouldMap='1' ignoredCompat2:ShouldMapAndPreserve='1' />
</Root>";
			Func<string, string> filter = s => expected.Replace(" ", "").Replace('\'', '"').Replace("\n", "").Replace("\r", "");
			Assert.AreEqual(filter(expected), filter(actual));

		}
#endif
	    private static XamlDirective DesignContextDirective = new XamlDirective(
		    new[] { "http://schemas.microsoft.com/expression/blend/2008" },
		    "DataContext", XamlLanguage.Object, null, AllowedMemberLocations.Attribute);

	    class CustomContext : XamlSchemaContext
	    {
		    public bool IsDesignMode { get; set; }
			public string NamespaceMap { get; set; }

		    public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
		    {
			    //Forces XamlXmlReader to not ignore our namespace if mc:Ignorable is set
			    if (
				    IsDesignMode &&
				    xamlNamespace == DesignContextDirective.PreferredXamlNamespace)
			    {
				    compatibleNamespace = NamespaceMap ?? xamlNamespace;
				    return true;
			    }
			    return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
		    }
	    }

	    class CustomWriter : XamlObjectWriter
	    {
		    public bool IsDesignMode;
		    public CustomWriter(XamlSchemaContext schemaContext) : base(schemaContext)
		    {
		    }

		    public override void WriteStartMember(XamlMember property)
		    {
			    if (IsDesignMode && property == DesignContextDirective)
				    base.WriteStartMember(new XamlMember(typeof(ClassWithDataContext).GetProperty("DataContext"), SchemaContext));
			    else
				    base.WriteStartMember(property);
		    }

		}

		[Test]
		public void CheckDesignDataContext()
		{
			var type = typeof(ClassWithDataContext);
			var xaml = $@"
<{type.Name} xmlns='clr-namespace:{type.Namespace}'
xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
mc:Ignorable='d'
d:DataContext='value'>
</{type.Name}>";
			
			foreach (var designMode in new[] {true, false})
			{
				foreach (var useDirective in new[] {true, false})
				{
					ClassWithDataContext res;
					var settings = new XamlXmlReaderSettings {LocalAssembly = type.Assembly};
					var rdr = new XamlXmlReader(new StringReader(xaml), new CustomContext()
					{
						IsDesignMode = designMode,
						NamespaceMap = useDirective ? null : ("clr-namespace:" + typeof(ClassWithDataContext).Namespace)

					}, settings);
					if (useDirective)
					{
						var writer = new CustomWriter(rdr.SchemaContext) { IsDesignMode = designMode };
						XamlServices.Transform(rdr, writer);
						res = (ClassWithDataContext) writer.Result;
					}
					else
					{
						res = (ClassWithDataContext) XamlServices.Load(rdr);
					}

					if (designMode)
						Assert.AreEqual("value", res.DataContext);
					else
						Assert.IsNull(res.DataContext);
				}
			}
	    }
	}


	public class ClassWithDataContext
	{
		public object DataContext { get; set; }
	}
}

