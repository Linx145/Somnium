using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    /// <summary>
    /// Similar to a List, except items are not added: Instead, they are automatically instantiated on retrieve where applicable and are to be edited by reference.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PooledVector<T> where T : struct
    {
        private const int defaultCapacity = 4;
        /// <summary>
        ///         Exposed for unsafe operations
        /// </summary>
        public T[] internalArray;
        public int Count { get; private set; }
        int Capacity;

        public ref T this[int index]
        {
            get
            {
                if (index >= Count || index < 0) throw new IndexOutOfRangeException();
                return ref internalArray[index];
            }
        }

        public void Sort(IComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException();
            Array.Sort<T>(internalArray, 0, Count, comparer);
        }

        public PooledVector(int initialCapacity = 4)
        {
            //if (initialCapacity < defaultCapacity) throw new ArgumentOutOfRangeException("initialCapacity");
            internalArray = new T[initialCapacity];
            Capacity = initialCapacity;
            EnsureCapacity(Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            //don't clear the existing items
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int min)
        {
            if (internalArray.Length < min)
            {
                int newCapacity = internalArray.Length == 0 ? defaultCapacity : internalArray.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > 0X7FEFFFFF) newCapacity = 0X7FEFFFFF;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
                Array.Resize(ref internalArray, newCapacity);
            }
        }

        public ref T Get()
        {
            if (Count == Capacity)
            {
                EnsureCapacity(Capacity + 1);
            }
            return ref internalArray[Count++];
        }
    }
}
