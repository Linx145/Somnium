namespace Somnium.Framework
{
    public static class Ext
    {
        public static class Vector3
        {
            public static readonly System.Numerics.Vector3 Up = new System.Numerics.Vector3(0f, 1f, 0f);
            public static readonly System.Numerics.Vector3 Down = new System.Numerics.Vector3(0f, -1f, 0f);
            public static readonly System.Numerics.Vector3 Right = new System.Numerics.Vector3(1f, 0f, 0f);
            public static readonly System.Numerics.Vector3 Left = new System.Numerics.Vector3(-1f, 0f, 0f);
            public static readonly System.Numerics.Vector3 Forward = new System.Numerics.Vector3(0f, 0f, -1f);
            public static readonly System.Numerics.Vector3 Backward = new System.Numerics.Vector3(0f, 0f, 1f);
        }
        public static class Vector2
        {
            public static readonly System.Numerics.Vector2 Up = new System.Numerics.Vector2(0f, -1f);
            public static readonly System.Numerics.Vector2 Down = new System.Numerics.Vector2(0f, 1f);
            public static readonly System.Numerics.Vector2 Left = new System.Numerics.Vector2(-1f, 0f);
            public static readonly System.Numerics.Vector2 Right = new System.Numerics.Vector2(1f, 0f);
        }
        public static Point ToPoint(this System.Numerics.Vector2 vector)
        {
            return new Point((int)vector.X, (int)vector.Y);
        }
    }
}
