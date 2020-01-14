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
using System.Runtime.CompilerServices;

namespace System.Xaml
{
	class ReferenceValueInternal
	{
		// static in a generic creates multiple copies
		public static readonly object NullValue = new object();
	}

	/// <summary>
	/// Struct to store a reference type and cache its value (even if null).
	/// </summary>
	struct ReferenceValue<T>
		where T: class
	{
		object _value;

		// automatically translates NullValue into null without an extra test
		public T Value => _value as T;

		public bool HasValue => !ReferenceEquals(_value, null);

		public ReferenceValue(T value)
		{
			_value = value ?? ReferenceValueInternal.NullValue;
		}

		public T Set(T value)
		{
			_value = value;
			if (ReferenceEquals(_value, null))
			{
				_value = ReferenceValueInternal.NullValue;
				return default(T);
			}
			return (T)_value;
		}

		public static implicit operator ReferenceValue<T>(T value)
		{
			return new ReferenceValue<T>(value);
		}

		public static bool operator ==(ReferenceValue<T> left, ReferenceValue<T> right)
		{
			return Equals(left._value, right._value);
		}

		public static bool operator !=(ReferenceValue<T> left, ReferenceValue<T> right)
		{
			return !Equals(left._value, right._value);
		}

		public override bool Equals(object obj)
		{
			return obj is ReferenceValue<T> && this == (ReferenceValue<T>)obj;
		}

		public override int GetHashCode()
		{
			return _value?.GetHashCode() ?? base.GetHashCode();
		}
	}

}
