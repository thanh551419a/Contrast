using System.Collections.Generic;
using Contrast.Color;
using Contrast.Player;
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
    ///
    /// The player movement system does not use Unity Physics2D for gameplay
    /// collision, so contact is evaluated through the same authoritative AABB
    /// geometry used by the movement resolver. A successful contact is exposed
    /// as the logical OnPlayerTouched event below.
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
        public Vector2 Position => transform.position;
        public Vector2 Size => size;
        public LogicalColor LogicalColor =>
            ColorClassifier.Classify(rawColor);
        public float RawColor => rawColor;
        public bool IsConsumed => consumed;

        internal void SetEditorPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        internal void SetEditorData(Vector2 position, Vector2 markerSize, float color)
        {
            size = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(markerSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(markerSize.y)));
            rawColor = color;
            transform.position = new Vector3(position.x, position.y, 0f);

            EnsureVisual();
            spriteRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            float visualValue = markerType == LevelMarkerType.End
                ? 1f
                : ColorClassifier.ToValue(LogicalColor);
            spriteRenderer.color = new UnityEngine.Color(
                visualValue, visualValue, visualValue, VisualAlpha);
        }

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

            spriteRenderer.sprite = ColorPlatform.GetDefaultSprite();
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.transform.localScale =
                new Vector3(size.x, size.y, 1f);
            spriteRenderer.sortingOrder = 5;

            float visualValue = type == LevelMarkerType.End
                ? 1f
                : ColorClassifier.ToValue(LogicalColor);

            spriteRenderer.color = new UnityEngine.Color(
                visualValue,
                visualValue,
                visualValue,
                VisualAlpha);

            // Task D: Hide checkpoint visuals during gameplay while preserving AABB overlap and respawn
            if (type == LevelMarkerType.CheckPoint)
            {
                spriteRenderer.enabled = false;
            }
        }

        internal bool Overlaps(Aabb playerBounds)
        {
            if (consumed || !gameObject.activeInHierarchy)
                return false;

            return Aabb.Overlaps(GetAabb(), playerBounds);
        }

        /// <summary>
        /// Logical contact callback. The marker is one-shot for CheckPoint/End.
        /// StartPos remains persistent.
        /// </summary>
        public void OnPlayerTouched()
        {
            if (consumed)
                return;

            if (markerType == LevelMarkerType.StartPos)
                return;

            consumed = true;
            All.Remove(this);

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        internal Aabb GetAabb()
        {
            return Aabb.FromCenter(
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

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(transform, false);
                spriteRenderer = visual.AddComponent<SpriteRenderer>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireCube(transform.position, size);
        }
    }

    /// <summary>
    /// Runtime collectible (Quang Điểm) instantiated from LevelData.collectibles.
    /// Evaluates contact against the Player's AABB during LevelLoader.ProcessPlayerInteractions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuangDiemItem : MonoBehaviour
    {
        public static readonly List<QuangDiemItem> All = new List<QuangDiemItem>();
        public static int TotalCount { get; private set; }
        public static int CollectedCount { get; private set; }
        public static event System.Action<QuangDiemItem> OnCollected;

        private string itemId;
        private Vector2 basePosition;
        private float radius = 0.25f; // HTML: r = 16 px / 64 px/unit = 0.25 units
        private float bobPhase;
        private bool collected;

        public string ItemId => itemId;
        public Vector2 BasePosition => basePosition;
        public Vector2 Position => transform.position;
        public float Radius => radius;
        public float BobPhase
        {
            get => bobPhase;
            set => bobPhase = value;
        }
        public bool IsCollected => collected;

        public void Initialize(string id, Vector2 position, float itemRadius = 0.25f, float initialBobPhase = 0f)
        {
            itemId = string.IsNullOrEmpty(id) ? $"quang_{All.Count + 1}" : id;
            basePosition = position;
            radius = Mathf.Max(0.1f, itemRadius);
            bobPhase = initialBobPhase;
            collected = false;

            transform.position = new Vector3(position.x, position.y, 0f);
            transform.localScale = Vector3.one;
            gameObject.name = $"QuangDiem_{itemId}";

            if (!All.Contains(this))
                All.Add(this);
        }

        internal bool Overlaps(Aabb playerBounds)
        {
            if (collected || !gameObject.activeInHierarchy)
                return false;

            Vector2 playerCenter = (playerBounds.Min + playerBounds.Max) * 0.5f;
            Vector2 playerSize = playerBounds.Size;
            float pr = Mathf.Max(playerSize.x, playerSize.y) * 0.45f;
            float dist = Vector2.Distance(playerCenter, transform.position);
            if (dist <= radius + pr)
                return true;

            Aabb itemBox = Aabb.FromCenter(transform.position, new Vector2(radius * 2f, radius * 2f));
            return Aabb.Overlaps(itemBox, playerBounds);
        }

        public void Collect()
        {
            if (collected)
                return;

            collected = true;
            CollectedCount++;

            Debug.Log($"[QUANG_DIEM] Collected id={itemId} position={basePosition} ({CollectedCount}/{TotalCount})");
            Contrast.Audio.ContrastAudioManager.Instance?.PlayCheckpoint();
            OnCollected?.Invoke(this);

            var pres = GetComponent<Contrast.Presentation.QuangDiemVisualPresentation>();
            if (pres != null)
            {
                pres.PlayPickupBurst();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public static void ResetLevelProgress(int totalCount = 0)
        {
            All.Clear();
            TotalCount = Mathf.Max(0, totalCount);
            CollectedCount = 0;
        }
    }
}
