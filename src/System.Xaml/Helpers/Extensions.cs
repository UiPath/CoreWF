using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq.Expressions;

namespace System.Xaml
{
	static class Extensions
	{
		public static IList<T> ToReadOnly<T>(this IEnumerable<T> enumerable)
		{
			return new ReadOnlyCollection<T>(enumerable.ToList());
		}

		public static bool Matches(this AssemblyName name, AssemblyName other)
		{
			if (ReferenceEquals(name, other))
				return true;
			// Name should match
			if (name.Name != other.Name)
				return false;

			// pk should match, if one is specified
			var namePk = name.GetPublicKeyToken();
			var otherPk = name.GetPublicKeyToken();
			if (namePk == null)
				return otherPk == null;

			return namePk.SequenceEqual(otherPk);
		}
	}
}

