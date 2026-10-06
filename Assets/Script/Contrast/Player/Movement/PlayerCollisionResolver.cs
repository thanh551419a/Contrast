using Contrast.Color;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Player
{
    /// <summary>
    /// Predictive collision resolver.
    /// Receives ONE combined intent vector and returns ONE final move vector.
    /// It never changes Transform or Rigidbody2D position.
    ///
    /// Current minor overlap is treated as an escape condition: when the overlap
    /// resolver has produced a push intent, currently-overlapped platforms are
    /// not allowed to cancel that escape movement. Other platforms remain solid.
    /// X then Y resolution preserves tangential movement, giving the expected
    /// slide behaviour along platform surfaces/corners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCollisionResolver : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField, Min(0f)]
        private float collisionEpsilon = 0.0001f;

        [SerializeField, Min(0f)]
        private float groundEpsilon = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool logCollision = true;

        public CollisionResult Resolve(
            Vector2 currentPosition,
            Vector2 playerSize,
            Vector2 playerOffset,
            Vector2 intentMove,
            LogicalColor playerColor,
            bool allowCurrentOverlapEscape)
        {
            Aabb current = Aabb.FromCenter(
                currentPosition + playerOffset,
                playerSize);

            if (logCollision)
            {
                Debug.Log(
                    $"[COLLISION][START] currentPos={currentPosition} " +
                    $"intent={intentMove} " +
                    $"currentMin={current.Min} currentMax={current.Max} " +
                    $"escapeOverlap={allowCurrentOverlapEscape}");
            }

            float resolvedCenterX = ResolveHorizontal(
                current,
                current.Center.x + intentMove.x,
                intentMove.x,
                playerSize,
                playerColor,
                allowCurrentOverlapEscape,
                out bool blockedX);

            Vector2 resolvedPosition = new Vector2(
                resolvedCenterX - playerOffset.x,
                currentPosition.y);

            Aabb afterX = Aabb.FromCenter(
                resolvedPosition + playerOffset,
                playerSize);

            float resolvedCenterY = ResolveVertical(
                afterX,
                afterX.Center.y + intentMove.y,
                intentMove.y,
                playerSize,
                playerColor,
                allowCurrentOverlapEscape,
                out bool blockedDownward,
                out bool blockedY);

            resolvedPosition.y = resolvedCenterY - playerOffset.y;

            Aabb finalBounds = Aabb.FromCenter(
                resolvedPosition + playerOffset,
                playerSize);

            bool grounded = blockedDownward ||
                            HasGroundSupport(finalBounds, playerColor);

            Vector2 finalMove = resolvedPosition - currentPosition;

            if (logCollision)
            {
                Debug.Log(
                    $"[COLLISION][FINAL] resolvedPosition={resolvedPosition} " +
                    $"finalMove={finalMove} blockedX={blockedX} " +
                    $"blockedY={blockedY} grounded={grounded}");
            }

            return new CollisionResult(
                finalMove,
                grounded,
                blockedX,
                blockedY);
        }

        private float ResolveHorizontal(
            Aabb current,
            float predictedX,
            float moveX,
            Vector2 playerSize,
            LogicalColor playerColor,
            bool allowCurrentOverlapEscape,
            out bool blocked)
        {
            blocked = false;

            if (Mathf.Abs(moveX) <= collisionEpsilon)
                return current.Center.x;

            Aabb candidate = current.WithCenterX(predictedX);
            float bestX = predictedX;
            float bestCorrection = float.PositiveInfinity;

            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform platform = ColorPlatform.All[i];
                if (!IsBlockingPlatform(platform, playerColor))
                    continue;

                Aabb platformBounds = FromPlatform(platform);

                // If this platform is already overlapping the current player and
                // this update has an escape intent, it must not cancel the escape.
                if (allowCurrentOverlapEscape && Aabb.Overlaps(current, platformBounds))
                {
                    if (logCollision)
                        Debug.Log($"[COLLISION][X] skip current-overlap platform={platform.name} for escape");
                    continue;
                }

                if (!Aabb.Overlaps(candidate, platformBounds))
                    continue;

                if (logCollision)
                {
                    Debug.Log(
                        $"[COLLISION][X] candidate overlap platform={platform.name} " +
                        $"moveX={moveX:F4} current={current.Center} candidateX={predictedX:F4}");
                }

                float correctedX;

                if (moveX > 0f)
                {
                    if (current.Max.x <= platformBounds.Min.x + collisionEpsilon)
                    {
                        correctedX = platformBounds.Min.x
                                      - playerSize.x * 0.5f
                                      - collisionEpsilon;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (current.Min.x >= platformBounds.Max.x - collisionEpsilon)
                    {
                        correctedX = platformBounds.Max.x
                                      + playerSize.x * 0.5f
                                      + collisionEpsilon;
                    }
                    else
                    {
                        continue;
                    }
                }

                float correction = Mathf.Abs(correctedX - current.Center.x);

                if (correction < bestCorrection)
                {
                    bestCorrection = correction;
                    bestX = correctedX;
                    blocked = true;

                    if (logCollision)
                        Debug.Log($"[COLLISION][X] BLOCK platform={platform.name} correctedX={correctedX:F4}");
                }
            }

            return bestX;
        }

        private float ResolveVertical(
            Aabb current,
            float predictedY,
            float moveY,
            Vector2 playerSize,
            LogicalColor playerColor,
            bool allowCurrentOverlapEscape,
            out bool blockedDownward,
            out bool blocked)
        {
            blockedDownward = false;
            blocked = false;

            if (Mathf.Abs(moveY) <= collisionEpsilon)
                return current.Center.y;

            Aabb candidate = current.WithCenterY(predictedY);
            float bestY = predictedY;
            float bestCorrection = float.PositiveInfinity;

            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform platform = ColorPlatform.All[i];
                if (!IsBlockingPlatform(platform, playerColor))
                    continue;

                Aabb platformBounds = FromPlatform(platform);

                if (allowCurrentOverlapEscape && Aabb.Overlaps(current, platformBounds))
                {
                    if (logCollision)
                        Debug.Log($"[COLLISION][Y] skip current-overlap platform={platform.name} for escape");
                    continue;
                }

                if (!Aabb.Overlaps(candidate, platformBounds))
                    continue;

                if (logCollision)
                {
                    Debug.Log(
                        $"[COLLISION][Y] candidate overlap platform={platform.name} " +
                        $"moveY={moveY:F4} current={current.Center} candidateY={predictedY:F4}");
                }

                float correctedY;

                if (moveY < 0f)
                {
                    if (current.Min.y >= platformBounds.Max.y - collisionEpsilon)
                    {
                        correctedY = platformBounds.Max.y
                                      + playerSize.y * 0.5f
                                      + collisionEpsilon;
                        blockedDownward = true;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (current.Max.y <= platformBounds.Min.y + collisionEpsilon)
                    {
                        correctedY = platformBounds.Min.y
                                      - playerSize.y * 0.5f
                                      - collisionEpsilon;
                    }
                    else
                    {
                        continue;
                    }
                }

                float correction = Mathf.Abs(correctedY - current.Center.y);

                if (correction < bestCorrection)
                {
                    bestCorrection = correction;
                    bestY = correctedY;
                    blocked = true;

                    if (logCollision)
                        Debug.Log($"[COLLISION][Y] BLOCK platform={platform.name} correctedY={correctedY:F4} downward={blockedDownward}");
                }
            }

            return bestY;
        }

        private bool HasGroundSupport(Aabb playerBounds, LogicalColor playerColor)
        {
            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform platform = ColorPlatform.All[i];
                if (!IsBlockingPlatform(platform, playerColor))
                    continue;

                Aabb platformBounds = FromPlatform(platform);

                bool horizontalOverlap =
                    playerBounds.Max.x > platformBounds.Min.x + collisionEpsilon &&
                    playerBounds.Min.x < platformBounds.Max.x - collisionEpsilon;

                if (!horizontalOverlap)
                    continue;

                float verticalGap = Mathf.Abs(
                    playerBounds.Min.y - platformBounds.Max.y);

                if (verticalGap <= groundEpsilon &&
                    playerBounds.Center.y >= platformBounds.Center.y)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlockingPlatform(
            ColorPlatform platform,
            LogicalColor playerColor)
        {
            if (platform == null || !platform.gameObject.activeInHierarchy || !platform.enabled)
                return false;

            // Value 255 (Universal / Everything): player can stand on this platform with any color
            if (platform.IsUniversal || platform.Logical == LogicalColor.Universal || platform.RawGrayscaleColor >= 254.5f)
                return true;

            return platform.Logical != playerColor;
        }

        private static Aabb FromPlatform(ColorPlatform platform)
        {
            Rect rect = platform.GetAabb();
            return Aabb.FromCenter(rect.center, rect.size);
        }
    }

    public readonly struct CollisionResult
    {
        public readonly Vector2 FinalMove;
        public readonly bool IsGrounded;
        public readonly bool BlockedX;
        public readonly bool BlockedY;

        public CollisionResult(
            Vector2 finalMove,
            bool isGrounded,
            bool blockedX,
            bool blockedY)
        {
            FinalMove = finalMove;
            IsGrounded = isGrounded;
            BlockedX = blockedX;
            BlockedY = blockedY;
        }
    }
}
