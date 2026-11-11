using Contrast.Color;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Player
{
    /// <summary>
    /// Top-level movement pipeline.
    ///
    /// Read intents -> evaluate current overlap -> combine intents -> predict ->
    /// collision resolution -> return one final movement vector.
    /// This component never changes Player position.
    /// </summary>
    [RequireComponent(typeof(PlayerOverlapResolver))]
    [RequireComponent(typeof(PlayerCollisionResolver))]
    public sealed class PlayerMovementResolver : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logIntent = true;

        private PlayerOverlapResolver overlapResolver;
        private PlayerCollisionResolver collisionResolver;

        private void Awake()
        {
            overlapResolver = GetComponent<PlayerOverlapResolver>();
            collisionResolver = GetComponent<PlayerCollisionResolver>();
        }

        public MovementResult Resolve(
            Vector2 currentPosition,
            Vector2 playerSize,
            Vector2 playerOffset,
            Vector2 inputMove,
            Vector2 jumpMove,
            LogicalColor playerColor,
            float deltaTime,
            Vector2 riderMove = default)
        {
            overlapResolver ??= GetComponent<PlayerOverlapResolver>();
            collisionResolver ??= GetComponent<PlayerCollisionResolver>();

            Vector2 safeSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.y)));

            // 0. Pre-resolve supporting platform carrier displacement so that
            // overlap evaluation and player self-movement operate in the
            // post-carry reference frame (matching TrajectorySystem.UpdateAll).
            Vector2 carrierMove = Vector2.zero;
            bool carrierBlockedX = false;
            bool carrierBlockedY = false;
            if (riderMove != Vector2.zero)
            {
                CollisionResult carrierCollision = collisionResolver.ResolveCarrier(
                    currentPosition,
                    safeSize,
                    playerOffset,
                    riderMove,
                    playerColor);
                carrierMove = carrierCollision.FinalMove;
                carrierBlockedX = carrierCollision.BlockedX;
                carrierBlockedY = carrierCollision.BlockedY;
            }

            Vector2 carriedPosition = currentPosition + carrierMove;

            Aabb currentBounds = Aabb.FromCenter(
                carriedPosition + playerOffset,
                safeSize);

            // 1. Current overlap state (evaluated after carrier transport).
            OverlapResult overlap =
                overlapResolver.Evaluate(currentBounds, playerColor);

            if (overlap.IsStuck)
            {
                return new MovementResult(
                    Vector2.zero,
                    false,
                    true,
                    false,
                    overlap.OverlapPercent,
                    Vector2.zero,
                    false,
                    false,
                    Vector2.zero,
                    Vector2.zero);
            }

            // 2. Build player self-intent vector and combined total intent.
            float controlMultiplier =
                overlapResolver.GetControlMultiplier(overlap.IsPushingOut);

            Vector2 adjustedInput = inputMove * controlMultiplier;

            float safeDeltaTime = Mathf.Max(0f, deltaTime);

            // Convert the vertical movement intent back to its current vertical
            // speed so the overlap resolver can guarantee that vertical push-out
            // remains faster than the opposing vertical motion.
            float currentVerticalSpeed = safeDeltaTime > Mathf.Epsilon
                ? jumpMove.y / safeDeltaTime
                : 0f;

            float pushSpeed = overlap.IsPushingOut
                ? overlapResolver.GetPushSpeed(
                    overlap.PushDirection,
                    currentVerticalSpeed)
                : 0f;

            Vector2 pushMove = overlap.IsPushingOut
                ? overlap.PushDirection * pushSpeed * safeDeltaTime
                : Vector2.zero;

            Vector2 selfIntent =
                adjustedInput + jumpMove + pushMove;

            Vector2 combinedIntent =
                selfIntent + riderMove;

            if (logIntent)
            {
                Debug.Log(
                    $"[INTENT] input={inputMove} " +
                    $"multiplier={controlMultiplier:F2} " +
                    $"adjustedInput={adjustedInput} " +
                    $"jump={jumpMove} " +
                    $"push={pushMove} " +
                    $"rider={riderMove} " +
                    $"combined={combinedIntent}");
            }

            // 3. Predict + resolve self-intent from the carried position.
            CollisionResult collision = collisionResolver.Resolve(
                carriedPosition,
                safeSize,
                playerOffset,
                selfIntent,
                playerColor,
                overlap.IsPushingOut);

            Vector2 finalMove = carrierMove + collision.FinalMove;
            bool finalBlockedX = carrierBlockedX || collision.BlockedX;
            bool finalBlockedY = carrierBlockedY || collision.BlockedY;

            if (logIntent)
            {
                Debug.Log(
                    $"[PUSH][RESULT] active={overlap.IsPushingOut} " +
                    $"direction={overlap.PushDirection} " +
                    $"verticalVelocity={currentVerticalSpeed:F3} " +
                    $"speed={pushSpeed:F3} " +
                    $"horizontalBase={overlapResolver.OverlapPushSpeed:F3} " +
                    $"verticalBase={overlapResolver.VerticalPushSpeed:F3} " +
                    $"verticalMargin={overlapResolver.VerticalPushSpeedMargin:F3} " +
                    $"move={pushMove}");

                Debug.Log(
                    $"[RESOLVE][RESULT] current={currentPosition} " +
                    $"combined={combinedIntent} " +
                    $"final={finalMove} " +
                    $"blockedX={finalBlockedX} " +
                    $"blockedY={finalBlockedY} " +
                    $"grounded={collision.IsGrounded}");
            }

            return new MovementResult(
                finalMove,
                collision.IsGrounded,
                false,
                overlap.IsPushingOut,
                overlap.OverlapPercent,
                overlap.PushDirection,
                finalBlockedX,
                finalBlockedY,
                combinedIntent,
                pushMove,
                collision.GroundedPlatform);
        }
    }

    public readonly struct MovementResult
    {
        public readonly Vector2 FinalMove;
        public readonly bool IsGrounded;
        public readonly bool IsStuck;
        public readonly bool IsPushingOut;
        public readonly float OverlapPercent;
        public readonly Vector2 PushDirection;
        public readonly bool BlockedX;
        public readonly bool BlockedY;
        public readonly Vector2 CombinedIntent;
        public readonly Vector2 PushMove;
        public readonly ColorPlatform GroundedPlatform;

        public MovementResult(
            Vector2 finalMove,
            bool isGrounded,
            bool isStuck,
            bool isPushingOut,
            float overlapPercent,
            Vector2 pushDirection,
            bool blockedX,
            bool blockedY,
            Vector2 combinedIntent,
            Vector2 pushMove,
            ColorPlatform groundedPlatform = null)
        {
            FinalMove = finalMove;
            IsGrounded = isGrounded;
            IsStuck = isStuck;
            IsPushingOut = isPushingOut;
            OverlapPercent = overlapPercent;
            PushDirection = pushDirection;
            BlockedX = blockedX;
            BlockedY = blockedY;
            CombinedIntent = combinedIntent;
            PushMove = pushMove;
            GroundedPlatform = groundedPlatform;
        }
    }
}
