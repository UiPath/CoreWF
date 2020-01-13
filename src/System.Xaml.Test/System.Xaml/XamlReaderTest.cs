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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
#if PCL
using System.Xaml.Markup;
using System.Xaml;
using System.Xaml.Schema;
#else
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
#endif

using CategoryAttribute = NUnit.Framework.CategoryAttribute;

namespace MonoTests.System.Xaml
{
	[TestFixture]
	public partial class XamlReaderTest
	{
		
		XamlReader GetReader(string filename)
		{
			string xml = File.ReadAllText(Compat.GetTestFile(filename)).UpdateXml();
			return new XamlXmlReader(XmlReader.Create(new StringReader(xml)), new XamlXmlReaderSettings { ProvideLineInfo = true });
		}
		
		[Test]
		public void ReadSubtree1 ()
		{
			var xr = new XamlObjectReader (5);
			var sr = xr.ReadSubtree ();
			Assert.AreEqual (XamlNodeType.None, sr.NodeType, "#1-2");
			Assert.AreEqual (XamlNodeType.None, xr.NodeType, "#1-3");
			Assert.IsTrue (sr.Read (), "#2");
			Assert.AreEqual (XamlNodeType.None, sr.NodeType, "#2-2");
			Assert.AreEqual (XamlNodeType.None, xr.NodeType, "#2-3");
			Assert.IsFalse (sr.Read (), "#3");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, xr.NodeType, "#3-2");
		}

		[Test]
		public void ReadSubtree2 ()
		{
			var xr = new XamlObjectReader (5);
			xr.Read ();
			var sr = xr.ReadSubtree ();
			Assert.AreEqual (XamlNodeType.None, sr.NodeType, "#1-2");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, xr.NodeType, "#1-3");
			Assert.IsTrue (sr.Read (), "#2");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, sr.NodeType, "#2-2");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, xr.NodeType, "#2-3");
			Assert.IsFalse (sr.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartObject, xr.NodeType, "#3-2");
		}

		[Test]
		public void ReadSubtree3 ()
		{
			var xr = new XamlObjectReader (5);
			xr.Read ();
			xr.Read ();
			var sr = xr.ReadSubtree ();
			Assert.AreEqual (XamlNodeType.None, sr.NodeType, "#1-2");
			Assert.AreEqual (XamlNodeType.StartObject, xr.NodeType, "#1-3");
			Assert.IsTrue (sr.Read (), "#2");
			Assert.AreEqual (XamlNodeType.StartObject, sr.NodeType, "#2-2");
			Assert.AreEqual (XamlNodeType.StartObject, xr.NodeType, "#2-3");
			Assert.IsTrue (sr.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, sr.NodeType, "#3-2");
			Assert.AreEqual (XamlNodeType.StartMember, xr.NodeType, "#3-3");
			Assert.IsTrue (sr.Read (), "#4");
			Assert.AreEqual (XamlNodeType.Value, sr.NodeType, "#4-2");
			Assert.AreEqual (XamlNodeType.Value, xr.NodeType, "#4-3");
			Assert.IsTrue (sr.Read (), "#5");
			Assert.AreEqual (XamlNodeType.EndMember, sr.NodeType, "#5-2");
			Assert.AreEqual (XamlNodeType.EndMember, xr.NodeType, "#5-3");
			Assert.IsTrue (sr.Read (), "#6");
			Assert.AreEqual (XamlNodeType.EndObject, sr.NodeType, "#6-2");
			Assert.AreEqual (XamlNodeType.EndObject, xr.NodeType, "#6-3");
			Assert.IsFalse (sr.Read (), "#7");
			Assert.AreEqual (XamlNodeType.None, xr.NodeType, "#7-2");
		}
		
		[Test]
		public void CheckReaderPosition()
		{
			using (var reader = GetReader("PropertyNotFound.xml"))
			{
				var lineInfo = reader as IXamlLineInfo;
				while (reader.Read())
				{
					if (reader.NodeType == XamlNodeType.StartMember)
					{
						if (reader.Member.Name == "Baz")
						{
							Assert.AreEqual(1, lineInfo.LineNumber, "Wrong LineNumber");
							Assert.AreEqual(13, lineInfo.LinePosition, "Wrong LinePosition");
						}
					}
				}
			}
		}
	}
}
