using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Somnium.Framework
{
    public struct GenerationalIndex
    {
        public uint index;
        public uint generation;

        public GenerationalIndex(uint index, uint generation)
        {
            this.index = index;
            this.generation = generation;
        }
    }
    public class GenerationalArray<T>
    {
        private SparseArray<T> Values;
        private SparseArray<uint> Generations;
        private ConcurrentBag<uint> FreeIndices;
        private uint MaxIndex;

        public GenerationalArray(T defaultValue)
        {
            Values = new SparseArray<T>(defaultValue);
            Generations = new SparseArray<uint>(0);
            FreeIndices = new ConcurrentBag<uint>();
        }
        /// <summary>
        /// Inserts the value into the collection and returns the index and generation of the slot that it was inserted in
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GenerationalIndex Add(T value)
        {
            uint index;
            if (!FreeIndices.TryTake(out index))
            {
                index = MaxIndex;
                Interlocked.Increment(ref MaxIndex);
            }
            if (!Generations.WithinLength(index))
            {
                lock (Generations)
                {
                    Generations.Insert(index, 0);
                }
            }
            Values.Insert(index, value);
            return new GenerationalIndex(index, Generations[index]);
        }
        /// <summary>
        /// Removes an item at the given index, if the generation of the index matches the generation of the array
        /// </summary>
        /// <param name="index">The index of the item to remove</param>
        /// <returns>True if the remove is successful, false otherwise</returns>
        public bool Remove(GenerationalIndex index)
        {
            if (!Values.WithinLength(index.index) || Generations.WithinLength(index.index))
            {
                return false;
            }
            if (Generations[index.index] != index.generation)
            {
                return false;
            }
            FreeIndices.Add(index.index);
            Values.Remove(index.index);
            Generations[index.index]++;
            return true;
        }

        public T Get(GenerationalIndex index)
        {
            if (!IsValid(index))
            {
                throw new System.Collections.Generic.KeyNotFoundException("Generational index " + index.ToString() + " is invalid!");
            }
            return Values[index.index];
        }

        public bool IsValid(GenerationalIndex index)
        {
            if (!Values.WithinLength(index.index) || !Generations.WithinLength(index.index) || Generations[index.index] != index.generation)
            {
                return false;
            }
            return true;
        }

        public ReadOnlySpan<T> internalArray
        {
            get
            {
                return Values.values;
            }
        }

        public T this[GenerationalIndex index]
        {
            get
            {
                return Get(index);
            }
        }

        public void Clear()
        {
            Values.Clear();
            Generations.Clear();
            FreeIndices.Clear();
            MaxIndex = 0;
        }
    }
}
