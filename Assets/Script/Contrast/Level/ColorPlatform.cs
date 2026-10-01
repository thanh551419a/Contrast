using System.Collections.Generic;
using Contrast.Color;
using UnityEngine;

namespace Contrast.Level
{
    /// <summary>
    /// Runtime platform generated from JSON.
    /// The platform stores its gameplay AABB as data. Unity Physics2D is not
    /// used by the Contrast movement/collision system.
    /// </summary>
    public class ColorPlatform : MonoBehaviour
    {
        [Header("Gameplay Geometry")]
        [SerializeField] private Vector2 colliderPadding = Vector2.zero;
        [SerializeField] private Vector2 colliderOffset = Vector2.zero;

        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private float rawGrayscaleColor = 255f;

        public LogicalColor Logical { get; private set; }
        public Vector2 Position => transform.position;
        public Vector2 Size => size + colliderPadding;

        private SpriteRenderer spriteRenderer;

        private static Sprite cachedSprite;
        private const float VisualOverlap = 0.02f;

        public static readonly List<ColorPlatform> All =
            new List<ColorPlatform>();

        private void Awake()
        {
            EnsureComponents();
        }

        public void Initialize(
            Vector2 position,
            Vector2 platformSize,
            float rawColor)
        {
            EnsureComponents();

            size = platformSize;
            rawGrayscaleColor = rawColor;
            Logical = ColorClassifier.Classify(rawColor);

            transform.position = new Vector3(
                position.x,
                position.y,
                0f);
            transform.localScale = Vector3.one;

            if (!All.Contains(this))
                All.Add(this);

            if (spriteRenderer == null)
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(transform, false);
                spriteRenderer = visual.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.transform.localScale = new Vector3(
                platformSize.x + VisualOverlap,
                platformSize.y + VisualOverlap,
                1f);
            spriteRenderer.sprite = GetDefaultSprite();
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.sortingOrder = 0;

            float v = Mathf.Clamp01(rawColor / 255f);
            spriteRenderer.color = new UnityEngine.Color(v, v, v, 1f);
        }

        /// <summary>
        /// Gameplay AABB. This is the only platform geometry used by
        /// PlayerMovementResolver.
        /// </summary>
        public Rect GetAabb()
        {
            Vector2 actualSize = new Vector2(
                Mathf.Max(0.0001f, Size.x),
                Mathf.Max(0.0001f, Size.y));

            Vector2 center = Position + colliderOffset;
            return new Rect(
                center - actualSize * 0.5f,
                actualSize);
        }

        public Bounds Bounds => new Bounds(
            GetAabb().center,
            GetAabb().size);

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public static void ClearRegistry()
        {
            All.Clear();
        }

        public static Sprite GetDefaultSprite()
        {
            if (cachedSprite != null)
                return cachedSprite;

            Texture2D tex = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            UnityEngine.Color white = UnityEngine.Color.white;
            tex.SetPixels(new[] { white, white, white, white });
            tex.Apply();

            cachedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, 2, 2),
                new Vector2(0.5f, 0.5f),
                2f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(0, 0, 0, 0));

            return cachedSprite;
        }

        private void OnDrawGizmosSelected()
        {
            Rect aabb = GetAabb();
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireCube(aabb.center, aabb.size);
        }
    }
}
