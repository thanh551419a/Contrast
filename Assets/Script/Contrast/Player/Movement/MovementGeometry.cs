using UnityEngine;

namespace Contrast.Player
{
    internal readonly struct Aabb
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        public Vector2 Center => (Min + Max) * 0.5f;
        public Vector2 Size => Max - Min;

        private Aabb(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public static Aabb FromCenter(Vector2 center, Vector2 size)
        {
            Vector2 half = size * 0.5f;
            return new Aabb(center - half, center + half);
        }

        public Aabb WithCenterX(float x)
        {
            float halfWidth = Size.x * 0.5f;
            return new Aabb(
                new Vector2(x - halfWidth, Min.y),
                new Vector2(x + halfWidth, Max.y));
        }

        public Aabb WithCenterY(float y)
        {
            float halfHeight = Size.y * 0.5f;
            return new Aabb(
                new Vector2(Min.x, y - halfHeight),
                new Vector2(Min.x + Size.x, y + halfHeight));
        }

        public static bool Overlaps(Aabb a, Aabb b)
        {
            return a.Max.x > b.Min.x &&
                   a.Min.x < b.Max.x &&
                   a.Max.y > b.Min.y &&
                   a.Min.y < b.Max.y;
        }

        public static float OverlapArea(Aabb a, Aabb b)
        {
            float width = Mathf.Min(a.Max.x, b.Max.x) - Mathf.Max(a.Min.x, b.Min.x);
            float height = Mathf.Min(a.Max.y, b.Max.y) - Mathf.Max(a.Min.y, b.Min.y);

            if (width <= 0f || height <= 0f)
                return 0f;

            return width * height;
        }
    }
}
