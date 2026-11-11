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

        public CollisionResult ResolveCarrier(
            Vector2 currentPosition,
            Vector2 playerSize,
            Vector2 playerOffset,
            Vector2 carrierMove,
            LogicalColor playerColor)
        {
            if (carrierMove == Vector2.zero)
            {
                return new CollisionResult(
                    Vector2.zero,
                    false,
                    false,
                    false,
                    null);
            }

            Aabb current = Aabb.FromCenter(
                currentPosition + playerOffset,
                playerSize);

            Vector2 resolvedPosition = currentPosition;
            bool blockedX;
            bool blockedY;
            bool blockedDownward;
            ColorPlatform downwardPlatform;

            // When the supporting platform has an upward velocity component, its
            // transform has already moved up in TrajectorySystem.UpdateAll().
            // Resolve Y first so the rider is lifted onto the new top surface
            // before horizontal carrier motion is tested against platform sides.
            // When moving horizontally or downward, resolve X first while the
            // rider is still at the previous top height, then lower Y.
            if (carrierMove.y > 0f)
            {
                float resolvedCenterY = ResolveVertical(
                    current,
                    current.Center.y + carrierMove.y,
                    carrierMove.y,
                    playerSize,
                    playerColor,
                    false,
                    out blockedDownward,
                    out blockedY,
                    out downwardPlatform);

                resolvedPosition.y = resolvedCenterY - playerOffset.y;

                Aabb afterY = Aabb.FromCenter(
                    resolvedPosition + playerOffset,
                    playerSize);

                float resolvedCenterX = ResolveHorizontal(
                    afterY,
                    afterY.Center.x + carrierMove.x,
                    carrierMove.x,
                    playerSize,
                    playerColor,
                    false,
                    out blockedX);

                resolvedPosition.x = resolvedCenterX - playerOffset.x;
            }
            else
            {
                float resolvedCenterX = ResolveHorizontal(
                    current,
                    current.Center.x + carrierMove.x,
                    carrierMove.x,
                    playerSize,
                    playerColor,
                    false,
                    out blockedX);

                resolvedPosition.x = resolvedCenterX - playerOffset.x;

                Aabb afterX = Aabb.FromCenter(
                    resolvedPosition + playerOffset,
                    playerSize);

                float resolvedCenterY = ResolveVertical(
                    afterX,
                    afterX.Center.y + carrierMove.y,
                    carrierMove.y,
                    playerSize,
                    playerColor,
                    false,
                    out blockedDownward,
                    out blockedY,
                    out downwardPlatform);

                resolvedPosition.y = resolvedCenterY - playerOffset.y;
            }

            Vector2 finalMove = resolvedPosition - currentPosition;

            return new CollisionResult(
                finalMove,
                blockedDownward,
                blockedX,
                blockedY,
                downwardPlatform);
        }

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
                out bool blockedY,
                out ColorPlatform downwardPlatform);

            resolvedPosition.y = resolvedCenterY - playerOffset.y;

            Aabb finalBounds = Aabb.FromCenter(
                resolvedPosition + playerOffset,
                playerSize);

            ColorPlatform groundedPlatform = null;
            if (blockedDownward && downwardPlatform != null)
            {
                groundedPlatform = downwardPlatform;
            }
            else if (intentMove.y <= collisionEpsilon &&
                     HasGroundSupport(finalBounds, playerColor, out ColorPlatform supportPlatform))
            {
                groundedPlatform = supportPlatform;
            }

            bool grounded = groundedPlatform != null;

            Vector2 finalMove = resolvedPosition - currentPosition;

            if (logCollision)
            {
                Debug.Log(
                    $"[COLLISION][FINAL] resolvedPosition={resolvedPosition} " +
                    $"finalMove={finalMove} blockedX={blockedX} " +
                    $"blockedY={blockedY} grounded={grounded} " +
                    $"platform={(groundedPlatform != null ? groundedPlatform.name : "none")}");
            }

            return new CollisionResult(
                finalMove,
                grounded,
                blockedX,
                blockedY,
                groundedPlatform);
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

            if (Mathf.Approximately(moveX, 0f))
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

                // If a rising platform swept across the player's feet this frame,
                // do not treat its top surface as a horizontal wall before vertical
                // resolution snaps the player onto the top surface.
                float prevPlatformMaxY = GetPreviousPlatformMaxY(platform, platformBounds);
                if (current.Min.y >= prevPlatformMaxY - collisionEpsilon &&
                    current.Center.y > platformBounds.Max.y)
                {
                    continue;
                }

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
            out bool blocked,
            out ColorPlatform downwardPlatform)
        {
            blockedDownward = false;
            blocked = false;
            downwardPlatform = null;

            if (Mathf.Abs(moveY) <= collisionEpsilon)
            {
                float snapY = predictedY;
                for (int i = 0; i < ColorPlatform.All.Count; i++)
                {
                    ColorPlatform platform = ColorPlatform.All[i];
                    if (!IsBlockingPlatform(platform, playerColor))
                        continue;

                    Aabb platformBounds = FromPlatform(platform);
                    if (!Aabb.Overlaps(current, platformBounds))
                        continue;

                    float prevMaxY = GetPreviousPlatformMaxY(platform, platformBounds);
                    if (current.Min.y >= prevMaxY - collisionEpsilon &&
                        current.Min.y < platformBounds.Max.y + collisionEpsilon &&
                        current.Center.y > platformBounds.Max.y)
                    {
                        float candidateSnapY = platformBounds.Max.y + playerSize.y * 0.5f + collisionEpsilon;
                        if (candidateSnapY >= snapY)
                        {
                            snapY = candidateSnapY;
                            blocked = true;
                            blockedDownward = true;
                            downwardPlatform = platform;
                        }
                    }
                }

                return snapY;
            }

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
                bool isCandidateDownward = false;

                if (moveY < 0f)
                {
                    float prevPlatformMaxY = GetPreviousPlatformMaxY(platform, platformBounds);
                    if (current.Min.y >= prevPlatformMaxY - collisionEpsilon)
                    {
                        correctedY = platformBounds.Max.y
                                      + playerSize.y * 0.5f
                                      + collisionEpsilon;
                        isCandidateDownward = true;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    float prevPlatformMaxY = GetPreviousPlatformMaxY(platform, platformBounds);
                    if (current.Min.y >= prevPlatformMaxY - groundEpsilon &&
                        current.Center.y > platformBounds.Max.y)
                    {
                        float minTakeoffY = platformBounds.Max.y + playerSize.y * 0.5f + collisionEpsilon + moveY;
                        if (bestY < minTakeoffY)
                            bestY = minTakeoffY;
                        continue;
                    }

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
                    blockedDownward = isCandidateDownward;
                    downwardPlatform = isCandidateDownward ? platform : null;

                    if (logCollision)
                        Debug.Log($"[COLLISION][Y] BLOCK platform={platform.name} correctedY={correctedY:F4} downward={blockedDownward}");
                }
            }

            return bestY;
        }

        internal bool TryGetGroundSupport(Aabb playerBounds, LogicalColor playerColor, out ColorPlatform supportPlatform)
        {
            return HasGroundSupport(playerBounds, playerColor, out supportPlatform);
        }

        private bool HasGroundSupport(Aabb playerBounds, LogicalColor playerColor, out ColorPlatform supportPlatform)
        {
            supportPlatform = null;
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
                    supportPlatform = platform;
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

        private static float GetPreviousPlatformMaxY(ColorPlatform platform, Aabb platformBounds)
        {
            if (platform != null)
            {
                Trajectory traj = platform.GetComponent<Trajectory>();
                if (traj != null && traj.IsEnabled && traj.FrameDisplacement.y > 0f)
                {
                    return platformBounds.Max.y - traj.FrameDisplacement.y;
                }
            }

            return platformBounds.Max.y;
        }
    }

    public readonly struct CollisionResult
    {
        public readonly Vector2 FinalMove;
        public readonly bool IsGrounded;
        public readonly bool BlockedX;
        public readonly bool BlockedY;
        public readonly ColorPlatform GroundedPlatform;

        public CollisionResult(
            Vector2 finalMove,
            bool isGrounded,
            bool blockedX,
            bool blockedY,
            ColorPlatform groundedPlatform = null)
        {
            FinalMove = finalMove;
            IsGrounded = isGrounded;
            BlockedX = blockedX;
            BlockedY = blockedY;
            GroundedPlatform = groundedPlatform;
        }
    }
}
