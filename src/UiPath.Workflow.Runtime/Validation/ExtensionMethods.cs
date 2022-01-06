// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Activities.Validation;

internal static class ExtensionMethods
{
    public static bool IsNullOrEmpty(this ICollection c) => (c == null || c.Count == 0);

    public static string AsCommaSeparatedValues(this IEnumerable<string> c) => string.Join(", ", c);

    public static int BinarySearch<T>(this IList<T> items, T value, IComparer<T> comparer) => BinarySearch(items, 0, items.Count, value, comparer);

    public static void QuickSort<T>(this IList<T> items, IComparer<T> comparer) => QuickSort(items, 0, items.Count - 1, comparer);

    private static int BinarySearch<T>(IList<T> items, int startIndex, int length, T value, IComparer<T> comparer)
    {
        int start = startIndex;
        int end = (startIndex + length) - 1;
        while (start <= end)
        {
            int mid = start + ((end - start) >> 1);
            int result = comparer.Compare(items[mid], value);
            if (result == 0)
            {
                return mid;
            }
            if (result < 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }
        }
        return ~start;
    }

    private static void QuickSort<T>(IList<T> items, int startIndex, int endIndex, IComparer<T> comparer)
    {
        Stack<int> bounds = new();
        do
        {
            if (bounds.Count != 0)
            {
                endIndex = bounds.Pop();
                startIndex = bounds.Pop();
            }

            T pivot = items[startIndex];
            int pivotIndex = startIndex;

            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                if (comparer.Compare(pivot, items[i]) > 0)
                {
                    pivotIndex++;
                    if (pivotIndex != i)
                    {
                        items.Swap(pivotIndex, i);
                    }
                }
            }

            if (startIndex != pivotIndex)
            {
                items.Swap(startIndex, pivotIndex);
            }

            if (pivotIndex + 1 < endIndex)
            {
                bounds.Push(pivotIndex + 1);
                bounds.Push(endIndex);
            }

            if (startIndex < pivotIndex - 1)
            {
                bounds.Push(startIndex);
                bounds.Push(pivotIndex - 1);
            }

        } while (bounds.Count != 0);
    }

    private static void Swap<T>(this IList<T> items, int i, int j)
    {
        T temp = items[i];
        items[i] = items[j];
        items[j] = temp;
    }
}
