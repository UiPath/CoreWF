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
using System.Xaml.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xaml.Markup;
using System.Xaml.Schema;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Xaml
{
	/// <summary>
	/// Struct to store flags and cache their value
	/// </summary>
	/// <remarks>
	/// Usage (for best performance):  flags.Get(MyFlag) ?? flags.Set(MyFlag, SomeValueToBeCached());
	/// </remarks>
	struct FlagValue
	{
		int _set;
		int _value;

		public bool? Get(int flag)
		{
			if ((_set & flag) != 0)
				return (_value & flag) != 0;
			return null;
		}

		public bool Set(int flag, bool value)
		{
			_set |= flag;
			if (value)
				_value |= flag;
			else
				_value &= ~flag;
			return value;
		}
	}
}