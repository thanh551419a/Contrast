using Contrast.Color;
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
            Vector2 safeSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.y)));

            Aabb currentBounds = Aabb.FromCenter(
                currentPosition + playerOffset,
                safeSize);

            // 1. Current overlap state.
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

            // 2. Build ONE final intent vector.
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

            Vector2 combinedIntent =
                adjustedInput + jumpMove + pushMove + riderMove;

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

            // 3. Predict + resolve that ONE vector.
            CollisionResult collision = collisionResolver.Resolve(
                currentPosition,
                safeSize,
                playerOffset,
                combinedIntent,
                playerColor,
                overlap.IsPushingOut);

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
                    $"final={collision.FinalMove} " +
                    $"blockedX={collision.BlockedX} " +
                    $"blockedY={collision.BlockedY} " +
                    $"grounded={collision.IsGrounded}");
            }

            return new MovementResult(
                collision.FinalMove,
                collision.IsGrounded,
                false,
                overlap.IsPushingOut,
                overlap.OverlapPercent,
                overlap.PushDirection,
                collision.BlockedX,
                collision.BlockedY,
                combinedIntent,
                pushMove);
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
            Vector2 pushMove)
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
        }
    }
}
