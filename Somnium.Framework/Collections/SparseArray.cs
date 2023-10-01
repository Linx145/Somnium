using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    /// <summary>
    /// Implementation of a sparse array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SparseArray<T>
    {
        T defaultValue;
        public T[] values;
        private object valuesLock;

        public SparseArray()
        {
            valuesLock = new object();
        }
        public SparseArray(T defaultValue)
        {
            this.defaultValue = defaultValue;
            values = new T[0];
            valuesLock = new object();
        }
        
        private void EnsureCapacity(uint index)
        {
            if (index >= values.Length)
            {
                lock (valuesLock)
                {
                    int oldSize = values.Length;
                    int newSize = Math.Max(1, values.Length);
                    while (newSize <= index)
                    {
                        newSize *= 2;
                    }
                    Array.Resize(ref values, newSize);
                    //if (newSize > oldSize) this is always true
                    {
                        for (int i = oldSize; i < newSize; i++)
                        {
                            values[i] = defaultValue;
                        }
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(uint index, T value)
        {
            EnsureCapacity(index);
            values[index] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WithinLength(uint index)
        {
            return index < values.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(uint index)
        {
            if (index > values.Length)
            {
                return;
            }
            values[index] = default(T);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef(uint index)
        {
            return ref values[index];
        }
        public T this[uint index]
        {
            get
            {
                return values[index];
            }
            set
            {
                values[index] = value;
            }
        }
        /// <summary>
        /// Retrieves then removes a value from the sparsearray
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is more than the length of values</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop(uint index)
        {
            T result = values[index];
            values[index] = defaultValue;
            return result;
        }
        /// <summary>
        /// WARNING: Uses Array.Clear, does not reset to defaultValue, but rather to default(T)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(values, 0, values.Length);
        }
    }
}