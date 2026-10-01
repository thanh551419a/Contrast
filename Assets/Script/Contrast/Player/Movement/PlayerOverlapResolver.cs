using Contrast.Color;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Player
{
    /// <summary>
    /// Examines ONLY the current AABB state.
    /// It does not move the player and does not perform predictive collision resolution.
    ///
    /// Rules:
    /// - overlap >= StuckOverlapPercent -> hard STUCK.
    /// - overlap > 0 and below threshold -> create an outward push intent.
    /// - push direction is the nearest escape edge from the player centre.
    ///
    /// Push speed:
    /// - Horizontal push keeps the existing configurable speed.
    /// - Vertical push is guaranteed to exceed the current vertical velocity
    ///   by VerticalPushSpeedMargin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerOverlapResolver : MonoBehaviour
    {
        [Header("Overlap")]
        [SerializeField, Range(0f, 100f)]
        private float stuckOverlapPercent = 50f;

        [SerializeField, Min(0f)]
        private float overlapPushSpeed = 2f;

        [Header("Vertical Push")]
        [SerializeField, Min(0f)]
        private float verticalPushSpeed = 2f;

        [SerializeField, Min(0f)]
        private float verticalPushSpeedMargin = 2f;

        [SerializeField, Range(0f, 100f)]
        private float overlapSpeedReductionPercent = 80f;

        [Header("Debug")]
        [SerializeField] private bool logOverlap = true;

        public float StuckOverlapPercent => stuckOverlapPercent;
        public float OverlapPushSpeed => overlapPushSpeed;
        public float VerticalPushSpeed => verticalPushSpeed;
        public float VerticalPushSpeedMargin => verticalPushSpeedMargin;
        public float OverlapSpeedReductionPercent => overlapSpeedReductionPercent;

        internal float GetPushSpeed(
            Vector2 pushDirection,
            float currentVerticalSpeed)
        {
            // Horizontal push keeps the existing configurable speed.
            if (Mathf.Abs(pushDirection.y) <= Mathf.Epsilon)
                return overlapPushSpeed;

            // Vertical push must always beat the current vertical motion
            // by a configurable margin.
            //
            // Example:
            // current vertical velocity = -3.0
            // margin = 2.0
            // required push speed = 5.0
            float requiredVerticalSpeed =
                Mathf.Abs(currentVerticalSpeed) + verticalPushSpeedMargin;

            return Mathf.Max(verticalPushSpeed, requiredVerticalSpeed);
        }

        internal OverlapResult Evaluate(
            Aabb playerBounds,
            LogicalColor playerColor)
        {
            float playerArea =
                playerBounds.Size.x * playerBounds.Size.y;

            if (playerArea <= Mathf.Epsilon)
                return default;

            float largestOverlapPercent = 0f;
            float bestEscapeDistance = float.PositiveInfinity;
            Vector2 bestPushDirection = Vector2.zero;
            bool anyOverlap = false;
            bool stuck = false;

            Vector2 center = playerBounds.Center;

            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform platform = ColorPlatform.All[i];

                if (!IsBlockingPlatform(platform, playerColor))
                    continue;

                Aabb platformBounds = FromPlatform(platform);

                float overlapArea =
                    Aabb.OverlapArea(
                        playerBounds,
                        platformBounds);

                if (overlapArea <= 0f)
                    continue;

                anyOverlap = true;

                float overlapPercent =
                    overlapArea / playerArea * 100f;

                largestOverlapPercent =
                    Mathf.Max(
                        largestOverlapPercent,
                        overlapPercent);

                if (logOverlap)
                {
                    Debug.Log(
                        $"[OVERLAP] platform={platform.name} " +
                        $"platformColor={platform.Logical} " +
                        $"playerColor={playerColor} " +
                        $"overlapArea={overlapArea:F4} " +
                        $"playerArea={playerArea:F4} " +
                        $"overlap={overlapPercent:F2}% " +
                        $"threshold={stuckOverlapPercent:F2}% " +
                        $"playerCenter={center} " +
                        $"platformCenter={platformBounds.Center}");
                }

                if (overlapPercent >= stuckOverlapPercent)
                {
                    stuck = true;

                    if (logOverlap)
                    {
                        Debug.Log(
                            $"[OVERLAP][DECISION] " +
                            $"platform={platform.name} " +
                            $"{overlapPercent:F2}% => STUCK");
                    }

                    continue;
                }

                if (logOverlap)
                {
                    Debug.Log(
                        $"[OVERLAP][DECISION] " +
                        $"platform={platform.name} " +
                        $"{overlapPercent:F2}% => CANDIDATE_PUSH");
                }

                float left =
                    Mathf.Abs(
                        center.x - platformBounds.Min.x);

                float right =
                    Mathf.Abs(
                        platformBounds.Max.x - center.x);

                float down =
                    Mathf.Abs(
                        center.y - platformBounds.Min.y);

                float up =
                    Mathf.Abs(
                        platformBounds.Max.y - center.y);

                if (logOverlap)
                {
                    Debug.Log(
                        $"[PUSH][DISTANCE] platform={platform.name} " +
                        $"left={left:F4} " +
                        $"right={right:F4} " +
                        $"down={down:F4} " +
                        $"up={up:F4}");
                }

                SelectDirection(
                    left,
                    Vector2.left,
                    ref bestEscapeDistance,
                    ref bestPushDirection);

                SelectDirection(
                    right,
                    Vector2.right,
                    ref bestEscapeDistance,
                    ref bestPushDirection);

                SelectDirection(
                    down,
                    Vector2.down,
                    ref bestEscapeDistance,
                    ref bestPushDirection);

                SelectDirection(
                    up,
                    Vector2.up,
                    ref bestEscapeDistance,
                    ref bestPushDirection);
            }

            if (stuck)
            {
                if (logOverlap)
                {
                    Debug.Log(
                        $"[OVERLAP][RESULT] " +
                        $"STUCK largestOverlap=" +
                        $"{largestOverlapPercent:F2}%");
                }

                return new OverlapResult(
                    true,
                    false,
                    largestOverlapPercent,
                    Vector2.zero);
            }

            bool isPushingOut =
                anyOverlap &&
                bestPushDirection != Vector2.zero;

            if (logOverlap)
            {
                Debug.Log(
                    $"[PUSH][SELECTED] " +
                    $"anyOverlap={anyOverlap} " +
                    $"isPushingOut={isPushingOut} " +
                    $"bestDistance={bestEscapeDistance:F4} " +
                    $"direction={bestPushDirection} " +
                    $"largestOverlap={largestOverlapPercent:F2}%");
            }

            return new OverlapResult(
                false,
                isPushingOut,
                largestOverlapPercent,
                bestPushDirection);
        }

        private static void SelectDirection(
            float distance,
            Vector2 direction,
            ref float bestDistance,
            ref Vector2 bestDirection)
        {
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDirection = direction;
            }
        }

        internal float GetControlMultiplier(
            bool isPushingOut)
        {
            if (!isPushingOut)
                return 1f;

            // Default = 20% remaining movement speed.
            return 1f -
                   Mathf.Clamp01(
                       overlapSpeedReductionPercent / 100f);
        }

        private static bool IsBlockingPlatform(
            ColorPlatform platform,
            LogicalColor playerColor)
        {
            return platform != null &&
                   platform.gameObject.activeInHierarchy &&
                   platform.enabled &&
                   platform.Logical != playerColor;
        }

        private static Aabb FromPlatform(
            ColorPlatform platform)
        {
            Rect rect = platform.GetAabb();

            return Aabb.FromCenter(
                rect.center,
                rect.size);
        }
    }

    public readonly struct OverlapResult
    {
        public readonly bool IsStuck;
        public readonly bool IsPushingOut;
        public readonly float OverlapPercent;
        public readonly Vector2 PushDirection;

        public OverlapResult(
            bool isStuck,
            bool isPushingOut,
            float overlapPercent,
            Vector2 pushDirection)
        {
            IsStuck = isStuck;
            IsPushingOut = isPushingOut;
            OverlapPercent = overlapPercent;
            PushDirection = pushDirection;
        }
    }
}