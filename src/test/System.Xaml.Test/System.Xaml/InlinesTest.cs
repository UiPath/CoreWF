using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
#if PCL
using System.Xaml.Markup;
using System.Xaml.ComponentModel;
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
	public class InlinesTest
	{
		static readonly string ns = $"clr-namespace:{typeof(TextBlock).Namespace};assembly={typeof(TextBlock).Assembly.GetName().Name}";

		[Test]
		public void XamlReader_Should_Read_Inline_Collection_With_Text_And_Elements()
		{
			var xaml = $@"
<InlineCollection xmlns='{ns}'>
	Hello <Run>World</Run>!
</InlineCollection>";
			var r = GetReaderText(xaml);

			r.Read(); // xmlns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);

			r.Read(); // <InlineCollection>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(typeof(InlineCollection), r.Type.UnderlyingType);

			ReadBase(r);

			r.Read(); // StartMember (_Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(XamlLanguage.Items, r.Member);

			r.Read(); // "Hello"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("Hello ", r.Value);

			r.Read(); // <Run>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(typeof(Run), r.Type.UnderlyingType);

			r.Read(); // StartMember (Text)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(nameof(Run.Text), r.Member.Name);

			r.Read(); // "World"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("World", r.Value);

			r.Read(); // EndMember (Text)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </Run>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // "!"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("!", r.Value);

			r.Read(); // EndMember (_Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </InlineCollection>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read()); // EOF
		}

		[Test]
		public void XamlReader_Should_Read_TextBlock_With_Text_And_Elements()
		{
			var xaml = $@"
<TextBlock xmlns='{ns}'>
	Hello <Run>World</Run>!
</TextBlock>";
			var r = GetReaderText(xaml);

			r.Read(); // xmlns
			Assert.AreEqual(XamlNodeType.NamespaceDeclaration, r.NodeType);

			r.Read(); // <TextBlock>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(typeof(TextBlock), r.Type.UnderlyingType);

			ReadBase(r);

			r.Read(); // StartMember (Inlines)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(nameof(TextBlock.Inlines), r.Member.Name);

			r.Read(); // GetObject
			Assert.AreEqual(XamlNodeType.GetObject, r.NodeType);

			r.Read(); // StartMember (_Items)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(XamlLanguage.Items, r.Member);

			r.Read(); // "Hello"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("Hello ", r.Value);

			r.Read(); // <Run>
			Assert.AreEqual(XamlNodeType.StartObject, r.NodeType);
			Assert.AreEqual(typeof(Run), r.Type.UnderlyingType);

			r.Read(); // StartMember (Text)
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType);
			Assert.AreEqual(nameof(Run.Text), r.Member.Name);

			r.Read(); // "World"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("World", r.Value);

			r.Read(); // EndMember (Text)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </Run>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // "!"
			Assert.AreEqual(XamlNodeType.Value, r.NodeType);
			Assert.AreEqual("!", r.Value);

			r.Read(); // EndMember (_Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </GetObject>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			r.Read(); // EndMember (Items)
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType);

			r.Read(); // </TextBlock>
			Assert.AreEqual(XamlNodeType.EndObject, r.NodeType);

			Assert.IsFalse(r.Read()); // EOF
		}

		[Test]
		public void Inner_Text_And_Items_Should_Be_Added_To_InlineCollection_Via_IList()
		{
			var xaml = $@"
<InlineCollection xmlns='{ns}'>
	Hello <Span>World</Span>!
</InlineCollection>";
			var result = (InlineCollection)XamlServices.Parse(xaml);

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("Hello ", ((Run)result[0]).Text);
			Assert.AreEqual("World", ((Span)result[1]).Text);
			Assert.AreEqual("!", ((Run)result[2]).Text);
		}

		[Test]
		public void Inner_Text_And_Items_Should_Be_Added_To_TextBlock_InlineCollection_Via_IList()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"
<TextBlock xmlns='{ns}'>
	Hello <Span>World</Span>!
</TextBlock>";
			var result = (TextBlock)XamlServices.Parse(xaml);

			Assert.AreEqual(3, result.Inlines.Count);
			Assert.AreEqual("Hello ", ((Run)result.Inlines[0]).Text);
			Assert.AreEqual("World", ((Span)result.Inlines[1]).Text);
			Assert.AreEqual("!", ((Run)result.Inlines[2]).Text);
		}

		[Test]
		public void Respects_WhitespaceSignificantCollectionAttribute()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"
<TextBlock xmlns='{ns}'>
	TextBlock <Span>test</Span> <Run>for</Run><Run>spacing</Run> test. 
</TextBlock>";
			var result = (TextBlock)XamlServices.Parse(xaml);

			Assert.AreEqual(6, result.Inlines.Count);
			Assert.AreEqual("TextBlock ", ((Run)result.Inlines[0]).Text);
			Assert.AreEqual("test", ((Span)result.Inlines[1]).Text);
			Assert.AreEqual(" ", ((Run)result.Inlines[2]).Text);
			Assert.AreEqual("for", ((Run)result.Inlines[3]).Text);
			Assert.AreEqual("spacing", ((Run)result.Inlines[4]).Text);
			Assert.AreEqual(" test.", ((Run)result.Inlines[5]).Text);
		}

		[Test]
		public void Respects_TrimSurroundingWhitespaceAttribute()
		{
			var assembly = this.GetType().GetTypeInfo().Assembly.FullName;
			var xaml = $@"
<TextBlock xmlns='{ns}'>
	TextBlock with <LineBreak/> line break.
</TextBlock>";
			var result = (TextBlock)XamlServices.Parse(xaml);

			Assert.AreEqual(3, result.Inlines.Count);
			Assert.AreEqual("TextBlock with", ((Run)result.Inlines[0]).Text);
			Assert.IsInstanceOf<LineBreak>(result.Inlines[1]);
			Assert.AreEqual("line break.", ((Run)result.Inlines[2]).Text);
		}

		XamlReader GetReaderText(string xml, XamlXmlReaderSettings settings = null)
		{
			xml = xml.UpdateXml();
			return new XamlXmlReader(new StringReader(xml), new XamlSchemaContext(), settings);
		}

		protected void ReadBase(XamlReader r)
		{
			if (!(r is XamlXmlReader))
				return;
#if !PCL
			if (Type.GetType ("Mono.Runtime") == null)
				return;
#endif
			// we include the xml declaration, MS.NET does not?
			Assert.IsTrue(r.Read(), "sbase#1");
			Assert.AreEqual(XamlNodeType.StartMember, r.NodeType, "sbase#2");
			Assert.AreEqual(XamlLanguage.Base, r.Member, "sbase#3");

			Assert.IsTrue(r.Read(), "vbase#1");
			Assert.AreEqual(XamlNodeType.Value, r.NodeType, "vbase#2");
			Assert.IsTrue(r.Value is string, "vbase#3");

			Assert.IsTrue(r.Read(), "ebase#1");
			Assert.AreEqual(XamlNodeType.EndMember, r.NodeType, "ebase#2");
		}
	}

	[ContentProperty(nameof(TextBlock.Inlines))]
	public class TextBlock
	{
		public InlineCollection Inlines { get; set; } = new InlineCollection();
	}

	[WhitespaceSignificantCollection]
	public class InlineCollection : List<Inline>, IList
	{
		int IList.Add(object value)
		{
			if (value is Inline i) Add(i);
			else Add(new Run { Text = value.ToString() });
			return Count;
		}
	}

	public abstract class Inline
	{
	}

	[ContentProperty(nameof(Run.Text))]
	public class Run : Inline
	{
		public string Text { get; set; }
	}

	[ContentProperty(nameof(Span.Text))]
	public class Span : Inline
	{
		public string Text { get; set; }
	}

	[TrimSurroundingWhitespace]
	public class LineBreak : Inline
	{
	}

}
