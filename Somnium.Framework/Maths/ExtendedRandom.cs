using System;
using System.Text;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somnium.Framework
{
    public class ExtendedRandom
    {
        private Random random;
        public ExtendedRandom()
        {
            random = new Random(Environment.TickCount);
        }
        public ExtendedRandom(int seed)
        {
            random = new Random(seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shuffle<T>(T[] array)
        {
            Shuffle(new Span<T>(array));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shuffle<T>(Span<T> values)
        {
            int n = values.Length;

            for (int i = 0; i < n - 1; i++)
            {
                int j = random.Next(i, n);

                if (j != i)
                {
                    T temp = values[i];
                    values[i] = values[j];
                    values[j] = temp;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] PickRandomNonRepeating<T>(ReadOnlySpan<T> values, int amount)
        {
            if (amount >= values.Length)
            {
                throw new IndexOutOfRangeException("Attempting to pick more non-repeating values than is provided");
            }
            T[] copy = new T[values.Length];
            values.CopyTo(copy.AsSpan());
            Shuffle(new Span<T>(copy));
            T[] result = new T[amount];
            Array.Copy(copy, result, amount);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next()
        {
            return random.Next();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int maxVal)
        {
            return random.Next(maxVal);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int minVal, int maxVal)
        {
            return random.Next(minVal, maxVal);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float minValue, float maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException("minValue/maxValue");
            }
            else if (maxValue == minValue)
            {
                return maxValue;
            }
            return (float)random.NextDouble() * (maxValue - minValue) + minValue;//minValue + ((maxValue - minValue) * (float)random.NextDouble());//(minValue * (float)random.NextDouble()) + (maxValue * (float)random.NextDouble());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float maxValue)
        {
            return (float)random.NextDouble() * maxValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat()
        {
            return (float)random.NextDouble();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextByte(byte[] buffer)
        {
            random.NextBytes(buffer);
        }

        public T NextWeighted<T>((float, T)[] weightToChoice)
        {
            float max = 0f;
            for (int i = 0;i < weightToChoice.Length;i++)
            {
                max += weightToChoice[i].Item1;
            }
            float rand = NextFloat(max);

            max = 0f;
            for (int i = 0; i < weightToChoice.Length; i++)
            {
                if (max <= rand && rand < max + weightToChoice[i].Item1)
                {
                    return weightToChoice[i].Item2;
                }
                max += weightToChoice[i].Item1;
            }
            throw new InvalidOperationException("Failed to get random value from weighted random pool!");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 NextVector(float min, float max)
        {
            return new Vector2(NextFloat(min, max), NextFloat(min, max));
        }
        /*public Vector2 NextVectorInBox(Box box, float rotation)
        {
            if (rotation == 0f)
            {
                return new Vector2(NextFloat(box.Left, box.Right), NextFloat(box.Top, box.Bottom));
            }
            else
            {
                Vector2 origin = new Vector2(box.X, box.Y);// - new Vector2(Mathf.lengthdirY(box.offsetX, rotation), Mathf.lengthdirX(box.offsetY, rotation));
                Vector2 result = origin + Mathf.lengthdir(NextFloat(0f, box.Width), rotation);
                result += Mathf.lengthdir(NextFloat(-box.Height * 0.5f, box.Height * 0.5f), rotation + 90f * Mathf.DegreeRadian);
                return result;
            }
        }*/
        public override int GetHashCode()
        {
            return random.GetHashCode();
        }
        public override string ToString()
        {
            return random.ToString();
        }
        public bool Equals(Random obj)
        {
            return random.Equals(obj);
        }
    }
}
