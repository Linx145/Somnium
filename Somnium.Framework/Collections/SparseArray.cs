using System;
using System.Collections;
using System.Collections.Generic;

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
        public void Insert(uint index, T value)
        {
            EnsureCapacity(index);
            values[index] = value;
        }
        public bool WithinLength(uint index)
        {
            return index < values.Length;
        }
        public void Remove(uint index)
        {
            if (index > values.Length)
            {
                return;
            }
            values[index] = default(T);
        }
        public ref T GetRef(uint index)
        {
            if (index >= values.Length)
            {
                throw new KeyNotFoundException("Key '" + index + "' not found in the Sparse Array!");
            }
            return ref values[index];
        }
        public T this[uint index]
        {
            get
            {
                if (index >= values.Length)
                {
                    return defaultValue;
                }
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
        public T Pop(uint index)
        {
            if (index > values.Length)
            {
                throw new IndexOutOfRangeException("index " + index.ToString() + " is greater than values.Length");
            }
            T result = values[index];
            values[index] = defaultValue;
            return result;
        }
        /// <summary>
        /// WARNING: Uses Array.Clear, does not reset to defaultValue, but rather to default(T)
        /// </summary>
        public void Clear()
        {
            Array.Clear(values, 0, values.Length);
        }
    }
}