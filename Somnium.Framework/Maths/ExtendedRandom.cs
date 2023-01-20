using System;
using System.Text;
using System.Numerics;

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

        public int Next()
        {
            return random.Next();
        }
        public int Next(int maxVal)
        {
            return random.Next(maxVal);
        }
        public int Next(int minVal, int maxVal)
        {
            return random.Next(minVal, maxVal);
        }
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
        public float NextFloat(float maxValue)
        {
            return (float)random.NextDouble() * maxValue;
        }
        public float NextFloat()
        {
            return (float)random.NextDouble();
        }
        public void NextByte(byte[] buffer)
        {
            random.NextBytes(buffer);
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
        public string NextString(int maxChars, int maxVal = 26, int minVal = 0)
        {
            if (maxChars < 1)
            {
                throw new Exception("ERROR: maxchars cannot be negative or zero.");
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < maxChars; i++)
            {

                /*if (rand.Next(0, 1) == 0)
                {*/

                int num = Next(minVal, maxVal);

                //char let = (char)('a' + num);
                char let = Convert.ToChar(num);
                builder.Append(let);
                /*}
                else
                {
                    int num = rand.Next(int.MinValue + 1, int.MaxValue - 1);
                    builder.Append(num);
                }*/

            }
            return builder.ToString();
        }
        public Vector2 NextVector(float min, float max)
        {
            return new Vector2(NextFloat(min, max), NextFloat(min, max));
        }
    }
}
