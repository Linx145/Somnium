using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    /// <summary>
    /// A special case list where remove calls do not preserve item order, but is therefore an O(1) notation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    
    public class UnorderedList<T> : IList<T>, IList
    {
        private const int defaultCapacity = 4;
        /// <summary>
        ///         Exposed for unsafe operations
        /// </summary>
        public T[] internalArray;
        public int Count { get; private set; }
        int Capacity;
        object syncRoot;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                if (index >= Count || index < 0) throw new IndexOutOfRangeException();
                return internalArray[index];
            }
            set
            {
                if (index >= Count || index < 0) throw new IndexOutOfRangeException();
                internalArray[index] = value;
            }
        }

        public void Sort(IComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException();
            Array.Sort<T>(internalArray, 0, Count, comparer);
        }
        public void Sort()
        {
            Array.Sort<T>(internalArray, 0, Count);
        }

        public UnorderedList()
        {
            //if (initialCapacity < defaultCapacity) throw new ArgumentOutOfRangeException("initialCapacity");
            internalArray = new T[4];
            Capacity = 4;
        }
        public UnorderedList(int initialCapacity)
        {
            internalArray = new T[initialCapacity];
            Capacity = initialCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(internalArray, 0, Count);
            Count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (Count >= Capacity)
            {
                EnsureCapacity(Math.Max(Count, Capacity + 1));
            }
            internalArray[Count] = item;
            Count += 1;
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

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < Count; i++)
                    if ((object)internalArray[i] == null)
                        return true;
                return false;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < Count; i++)
                {
                    if (c.Equals(internalArray[i], item)) return true;
                }
                return false;
            }
        }

        public T[] ToArray()
        {
            T[] result = new T[Count];
            Array.Copy(internalArray, 0, result, 0, Count);
            return result;
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(internalArray, 0, array, arrayIndex, Count);
        }

        public int IndexOf(T item)
        {
            int result = Array.IndexOf(internalArray, item, 0, Count);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        { 
            if (index != Count - 1) internalArray[index] = internalArray[Count - 1];
            internalArray[Count - 1] = default;
            Count--;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T PopLast()
        {
            T lastItem = internalArray[Count - 1];
            internalArray[Count - 1] = default(T);
            Count--;
            return lastItem;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);//((IEnumerable<T>)internalArray).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadonlySpan()
        {
            return new ReadOnlySpan<T>(internalArray, 0, Count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return new Span<T>(internalArray, 0, Count);
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }
        object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (T)value;
            }
        }
        void IList.Remove(object? obj)
        {
            Remove((T)obj);
        }
        void IList.Insert(int index, object? obj)
        {
            Insert(index, (T)obj);
        }
        int IList.IndexOf(object? obj)
        {
            return IndexOf((T)obj);
        }
        bool IList.Contains(object? obj)
        {
            return Contains((T)obj);
        }
        int IList.Add(object? obj)
        {
            Add((T)obj);
            return Count - 1;
        }
        object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref syncRoot, new object(), null);
                }
                return syncRoot;
            }
        }
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }
        public void CopyTo(Array array, int startIndex)
        {
            CopyTo((T[])array, 0);
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private UnorderedList<T> list;
            private int index;
            private T current;

            internal Enumerator(UnorderedList<T> list)
            {
                this.list = list;
                index = 0;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                UnorderedList<T> localList = list;

                if (index >= localList.Count)
                {
                    return false;
                }
                current = localList.internalArray[index];
                index++;
                return true;
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list.Count + 1)
                    {
                        throw new IndexOutOfRangeException();
                        //ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                index = 0;
                current = default(T);
            }

        }
    }
}