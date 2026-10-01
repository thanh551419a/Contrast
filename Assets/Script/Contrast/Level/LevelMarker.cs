using System.Collections.Generic;
using Contrast.Color;
using UnityEngine;

namespace Contrast.Level
{
    public enum LevelMarkerType
    {
        StartPos,
        CheckPoint,
        End
    }

    /// <summary>
    /// Runtime rectangle used for StartPos, CheckPoint and End.
    /// Uses the same authoritative AABB geometry as the custom movement system.
    /// Does not use Unity Physics2D for gameplay collision.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelMarker : MonoBehaviour
    {
        public static readonly List<LevelMarker> All =
            new List<LevelMarker>();

        private const float VisualAlpha = 0.45f;

        private LevelMarkerType markerType;
        private Vector2 size = Vector2.one;
        private float rawColor;
        private bool consumed;
        private SpriteRenderer spriteRenderer;

        public LevelMarkerType Type => markerType;

        public Vector2 Position =>
            transform.position;

        public Vector2 Size =>
            size;

        public LogicalColor LogicalColor =>
            ColorClassifier.Classify(rawColor);

        public float RawColor =>
            rawColor;

        public bool IsConsumed =>
            consumed;

        public void Initialize(
            LevelMarkerType type,
            Vector2 position,
            Vector2 markerSize,
            float color = 0f)
        {
            EnsureVisual();

            markerType = type;

            size = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(markerSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(markerSize.y)));

            rawColor = color;
            consumed = false;

            transform.position = new Vector3(
                position.x,
                position.y,
                0f);

            transform.localScale = Vector3.one;

            if (!All.Contains(this))
                All.Add(this);

            gameObject.name = type.ToString();

            spriteRenderer.sprite =
                ColorPlatform.GetDefaultSprite();

            spriteRenderer.drawMode =
                SpriteDrawMode.Simple;

            spriteRenderer.transform.localScale =
                new Vector3(
                    size.x,
                    size.y,
                    1f);

            spriteRenderer.sortingOrder = 5;

            float visualValue =
                type == LevelMarkerType.End
                    ? 1f
                    : ColorClassifier.ToValue(
                        LogicalColor);

            spriteRenderer.color = new UnityEngine.Color(
                visualValue,
                visualValue,
                visualValue,
                VisualAlpha);
        }

        internal bool Overlaps(
            Contrast.Player.Aabb playerBounds)
        {
            if (consumed ||
                !gameObject.activeInHierarchy)
            {
                return false;
            }

            return Contrast.Player.Aabb.Overlaps(
                GetAabb(),
                playerBounds);
        }

        /// <summary>
        /// Logical contact callback.
        /// StartPos remains persistent.
        /// CheckPoint and End are one-shot.
        /// </summary>
        public void OnPlayerTouched()
        {
            if (consumed)
                return;

            if (markerType ==
                LevelMarkerType.StartPos)
            {
                return;
            }

            consumed = true;

            All.Remove(this);

            gameObject.SetActive(false);

            Destroy(gameObject);
        }

        internal Contrast.Player.Aabb GetAabb()
        {
            return Contrast.Player.Aabb.FromCenter(
                Position,
                size);
        }

        public static void ClearRegistry()
        {
            All.Clear();
        }

        private void EnsureVisual()
        {
            if (spriteRenderer != null)
                return;

            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                GameObject visual =
                    new GameObject("Visual");

                visual.transform.SetParent(
                    transform,
                    false);

                spriteRenderer =
                    visual.AddComponent<SpriteRenderer>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = UnityEngine.Color.cyan;

            Gizmos.DrawWireCube(
                transform.position,
                size);
        }
    }
}