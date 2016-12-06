using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrid.Collections
{
    // Adapted from CoreCLR's ArraySortHelper.
    internal static class NativeSortHelper<TKey, TValue>
        where TKey : struct
        where TValue : struct
    {
        public static void Sort(NativeList<TKey> keys, NativeList<TValue> values, uint index, uint length, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null, "Check the arguments in the caller!");  // Precondition on interface method
            Debug.Assert(index >= 0 && length >= 0 && (keys.Count - index >= length), "Check the arguments in the caller!");
            Debug.Assert(comparer != null);

            // Add a try block here to detect IComparers (or their underlying IComparables, etc) that are bogus.
            try
            {
                IntrospectiveSort(keys, values, index, length, comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Bad comparer.", e);
            }
        }

        private static void SwapIfGreaterWithItems(NativeList<TKey> keys, NativeList<TValue> values, IComparer<TKey> comparer, uint a, uint b)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values == null || values.Count >= keys.Count);
            Debug.Assert(comparer != null);
            Debug.Assert(0 <= a && a < keys.Count);
            Debug.Assert(0 <= b && b < keys.Count);

            if (a != b)
            {
                if (comparer.Compare(keys[a], keys[b]) > 0)
                {
                    TKey key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                    if (values != null)
                    {
                        TValue value = values[a];
                        values[a] = values[b];
                        values[b] = value;
                    }
                }
            }
        }

        private static void Swap(NativeList<TKey> keys, NativeList<TValue> values, uint i, uint j)
        {
            if (i != j)
            {
                TKey k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;
                if (values != null)
                {
                    TValue v = values[i];
                    values[i] = values[j];
                    values[j] = v;
                }
            }
        }

        internal static void DepthLimitedQuickSort(NativeList<TKey> keys, NativeList<TValue> values, uint left, uint right, IComparer<TKey> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, values, left, right, comparer);
                    return;
                }

                uint i = left;
                uint j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or 
                // data that is made up of multiple sorted runs appended together.
                uint middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, values, comparer, i, middle);  // swap the low with the mid point
                SwapIfGreaterWithItems(keys, values, comparer, i, j);   // swap the low with the high
                SwapIfGreaterWithItems(keys, values, comparer, middle, j); // swap the middle with the high

                TKey x = keys[middle];
                do
                {
                    while (comparer.Compare(keys[i], x) < 0) i++;
                    while (comparer.Compare(x, keys[j]) < 0) j--;
                    Debug.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j) break;
                    if (i < j)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                        if (values != null)
                        {
                            TValue value = values[i];
                            values[i] = values[j];
                            values[j] = value;
                        }
                    }
                    i++;
                    j--;
                } while (i <= j);

                // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                // both sorts see the new value.
                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(keys, values, left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(keys, values, i, right, comparer, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        internal static void IntrospectiveSort(NativeList<TKey> keys, NativeList<TValue> values, uint left, uint length, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(comparer != null);
            Debug.Assert(length <= keys.Count);
            Debug.Assert(length + left <= keys.Count);
            Debug.Assert(length + left <= values.Count);

            if (length < 2)
                return;

            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Count), comparer);
        }

        private static void IntroSort(NativeList<TKey> keys, NativeList<TValue> values, uint lo, uint hi, uint depthLimit, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(comparer != null);
            Debug.Assert(lo >= 0);
            Debug.Assert(hi < keys.Count);

            while (hi > lo)
            {
                uint partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi - 1);
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        SwapIfGreaterWithItems(keys, values, comparer, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi, comparer);
                    return;
                }
                depthLimit--;

                uint p = PickPivotAndPartition(keys, values, lo, hi, comparer);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, values, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static uint PickPivotAndPartition(NativeList<TKey> keys, NativeList<TValue> values, uint lo, uint hi, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(comparer != null);
            Debug.Assert(lo >= 0);
            Debug.Assert(hi > lo);
            Debug.Assert(hi < keys.Count);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            uint middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithItems(keys, values, comparer, lo, middle);  // swap the low with the mid point
            SwapIfGreaterWithItems(keys, values, comparer, lo, hi);   // swap the low with the high
            SwapIfGreaterWithItems(keys, values, comparer, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            uint left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void Heapsort(NativeList<TKey> keys, NativeList<TValue> values, uint lo, uint hi, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(comparer != null);
            Debug.Assert(lo >= 0);
            Debug.Assert(hi > lo);
            Debug.Assert(hi < keys.Count);

            uint n = hi - lo + 1;
            for (uint i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo, comparer);
            }
            for (uint i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(NativeList<TKey> keys, NativeList<TValue> values, uint i, uint n, uint lo, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(comparer != null);
            Debug.Assert(lo >= 0);
            Debug.Assert(lo < keys.Count);

            TKey d = keys[lo + i - 1];
            TValue dValue = (values != null) ? values[lo + i - 1] : default(TValue);
            uint child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                {
                    child++;
                }
                if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                if (values != null)
                    values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
            if (values != null)
                values[lo + i - 1] = dValue;
        }

        private static void InsertionSort(NativeList<TKey> keys, NativeList<TValue> values, uint lo, uint hi, IComparer<TKey> comparer)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(comparer != null);
            Debug.Assert(lo >= 0);
            Debug.Assert(hi >= lo);
            Debug.Assert(hi <= keys.Count);

            uint i, j;
            TKey t;
            TValue tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = (values != null) ? values[i + 1] : default(TValue);

                while (j >= lo && j < hi && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    if (values != null)
                        values[j + 1] = values[j];
                    j--;
                }
                keys[j + 1] = t;
                if (values != null)
                    values[j + 1] = tValue;
            }
        }
    }

    internal static class IntrospectiveSortUtilities
    {
        // This is the threshold where Introspective sort switches to Insertion sort.
        // Imperically, 16 seems to speed up most cases without slowing down others, at least for integers.
        // Large value types may benefit from a smaller number.
        internal const uint IntrosortSizeThreshold = 16;

        internal const uint QuickSortDepthThreshold = 32;

        internal static uint FloorLog2(uint n)
        {
            uint result = 0;
            while (n >= 1)
            {
                result++;
                n = n / 2;
            }
            return result;
        }
    }
}
