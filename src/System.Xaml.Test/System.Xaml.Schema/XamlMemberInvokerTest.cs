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
using System.Reflection;
using System.Text;
using System.Xml;
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

namespace MonoTests.System.Xaml.Schema
{
	[TestFixture]
	public class XamlMemberInvokerTest
	{
		XamlSchemaContext sctx = new XamlSchemaContext (new XamlSchemaContextSettings ());
		PropertyInfo str_len = typeof (string).GetProperty ("Length");
		PropertyInfo sb_len = typeof (StringBuilder).GetProperty ("Length");
		EventInfo eventStore_Event1 = typeof(EventStore).GetEvent("Event1");
		PropertyInfo testClass5_WriteOnly = typeof(TestClass5).GetProperty("WriteOnly");
		PropertyInfo testClass5_Baz = typeof(TestClass5).GetProperty("Baz");

		[Test]
		public void ConstructorNull ()
		{
			Assert.Throws<ArgumentNullException> (() => new XamlMemberInvoker (null));
		}

		// Property

		[Test]
		public void FromProperty ()
		{
			var pi = str_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.AreEqual (pi.GetGetMethod (), i.UnderlyingGetter, "#1");
			Assert.IsNull (i.UnderlyingSetter, "#2");
			Assert.AreEqual (5, i.GetValue ("hello"), "#3");
		}

		[Test]
		public void GetValueNullObject ()
		{
			var pi = str_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.Throws<ArgumentNullException> (() => i.GetValue (null));
		}

		[Test]
		public void SetValueNullObject ()
		{
			var pi = sb_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.Throws<ArgumentNullException> (() => i.SetValue (null, 5));
		}

		[Test]
		public void GetValueOnWriteOnlyProperty ()
		{
			var pi = testClass5_WriteOnly;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.Throws<NotSupportedException> (() => i.GetValue (new TestClass5 ()));
		}

		[Test]
		public void GetValueOnWriteInternalProperty()
		{
			var pi = testClass5_Baz;
			var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
			var val = i.GetValue(new TestClass5 { Baz = "hello" });
			Assert.AreEqual("hello", val);
		}

		[Test]
		public void SetValueOnReadOnlyProperty ()
		{
			var pi = str_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.Throws<NotSupportedException> (() => i.SetValue ("hello", 5));
		}

		[Test]
		public void SetValueOnReadWriteProperty ()
		{
			var pi = sb_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			var sb = new StringBuilder ();
			i.SetValue (sb, 5);
			Assert.AreEqual (5, sb.Length, "#1");
		}

		[Test]
		public void GetValueOnIrrelevantObject ()
		{
			var pi = str_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
#if WINDOWS_UWP
			try
			{
				i.GetValue(new StringBuilder());
				Assert.Fail("Expected TargetException");
			}
			catch (Exception e)
			{
				Assert.AreEqual("TargetException", e.GetType().Name);
			}
#else
			Assert.Throws<TargetException> (() => i.GetValue (new StringBuilder ()));
#endif
		}

		[Test]
		public void GetValueOnTypeValue ()
		{
			var xm = XamlLanguage.Type.GetMember ("Type");
			var i = new XamlMemberInvoker (xm);
			var o = i.GetValue (new TypeExtension (typeof (int)));
			Assert.AreEqual (typeof (int), o, "#1");
		}

		[Test]
		public void GetValueArrayExtension ()
		{
			var xt = sctx.GetXamlType (typeof (TestClass));
			var xm = xt.GetMember ("ArrayMember");
			Assert.IsNotNull (xm, "#-1");
			Assert.AreEqual (XamlLanguage.Array, xm.Type, "#0");
			var o = xm.Invoker.GetValue (new TestClass ());
			Assert.AreEqual (typeof (ArrayExtension), o.GetType (), "#1");
		}

		[Test]
		public void GetValueInitialization ()
		{
			var xm = XamlLanguage.Initialization;
			var i = xm.Invoker;
			Assert.Throws<NotSupportedException> (() => i.GetValue ("foo"));
		}

		[Test]
		public void GetValuePositionalParameter ()
		{
			var xm = XamlLanguage.PositionalParameters;
			var i = xm.Invoker;
			Assert.Throws<NotSupportedException> (() => i.GetValue (new TypeExtension (typeof (int))));
		}

		[Test]
		public void SetValueOnIrrelevantObject ()
		{
			var pi = sb_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
#if WINDOWS_UWP
			try
			{
				i.SetValue("hello", 5);
				Assert.Fail("Expected TargetException");
			}
			catch (Exception e)
			{
				Assert.AreEqual("TargetException", e.GetType().Name);
			}
#else
			Assert.Throws<TargetException> (() => i.SetValue ("hello", 5));
#endif
		}

		// Event

		[Test]
		public void FromEvent ()
		{
			var ei = eventStore_Event1;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			Assert.IsNull (i.UnderlyingGetter, "#1");
			Assert.AreEqual (ei.GetAddMethod (), i.UnderlyingSetter, "#2");
		}

		[Test]
		public void GetValueOnEvent ()
		{
			var ei = eventStore_Event1;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			Assert.Throws<NotSupportedException> (() => i.GetValue (new EventStore()));
		}

		[Test]
		public void SetValueOnEventNull ()
		{
			var ei = eventStore_Event1;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			i.SetValue (new EventStore(), null);
		}

		[Test]
		public void SetValueOnEventValueMismatch ()
		{
			var ei = eventStore_Event1;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			Assert.Throws<ArgumentException> (() => i.SetValue (new EventStore(), 5));
		}

		void DummyEvent1 (object o, EventArgs e)
		{
		}

		[Test]
		public void SetValueOnEvent ()
		{
			var ei = eventStore_Event1;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			i.SetValue (new EventStore(), new EventHandler<EventArgs> (DummyEvent1));
		}

		[Test]
		public void CustomTypeDefaultValues ()
		{
			var i = new MyXamlMemberInvoker ();
			Assert.IsNull (i.UnderlyingGetter, "#1");
			Assert.IsNull (i.UnderlyingSetter, "#2");
		}

		[Test]
		public void UnderlyingGetter ()
		{
			var i = new XamlMemberInvoker (new MyXamlMember (str_len, sctx));
			// call XamlMember's UnderlyingGetter.
			Assert.Throws<MyException> (() => { var t = i.UnderlyingGetter; }, "#1");
		}

		[Test]
		public void UnderlyingSetter ()
		{
			var i = new XamlMemberInvoker (new MyXamlMember (str_len, sctx));
			// call XamlMember's UnderlyingSetter.
			Assert.Throws<MyException> (() => { var t = i.UnderlyingSetter; }, "#1");
		}

		class MyXamlMember : XamlMember
		{
			public MyXamlMember (PropertyInfo pi, XamlSchemaContext context)
				: base (pi, context)
			{
			}
			
			protected override MethodInfo LookupUnderlyingGetter ()
			{
				throw new MyException ();
			}

			protected override MethodInfo LookupUnderlyingSetter ()
			{
				throw new MyException ();
			}
		}

		class MyException : Exception
		{
		}

		class MyXamlMemberInvoker : XamlMemberInvoker
		{
		}

		class TestClass
		{
			public TestClass ()
			{
				ArrayMember = new ArrayExtension (typeof (int));
				ArrayMember.AddChild (5);
				ArrayMember.AddChild (3);
				ArrayMember.AddChild (-1);
			}

			public ArrayExtension ArrayMember { get; set; }
		}

		[Test]
		public void UnknownInvokerGetValue ()
		{
			Assert.Throws<NotSupportedException> (() => XamlMemberInvoker.UnknownInvoker.GetValue (new object ()));
		}

		[Test]
		public void UnknownInvokerSetValue ()
		{
			Assert.Throws<NotSupportedException> (() => XamlMemberInvoker.UnknownInvoker.SetValue (new object (), new object ()));
		}

		[Test]
		public void UnknownInvoker ()
		{
			Assert.IsNull (XamlMemberInvoker.UnknownInvoker.UnderlyingGetter, "#1");
			Assert.IsNull (XamlMemberInvoker.UnknownInvoker.UnderlyingSetter, "#2");
		}
	}
}
