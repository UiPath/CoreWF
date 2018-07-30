// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Debugger
{
    using System.Collections.Generic;

    internal static class ListExtensions
    {
        internal static BinarySearchResult MyBinarySearch<T>(this List<T> input, T item)
        {
            return new BinarySearchResult(input.BinarySearch(item), input.Count);
        }
    }
}
