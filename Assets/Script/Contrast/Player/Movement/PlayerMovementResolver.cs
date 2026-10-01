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
            overlapResolver =
                GetComponent<PlayerOverlapResolver>();

            collisionResolver =
                GetComponent<PlayerCollisionResolver>();
        }

        public MovementResult Resolve(
            Vector2 currentPosition,
            Vector2 playerSize,
            Vector2 playerOffset,
            Vector2 inputMove,
            Vector2 jumpMove,
            LogicalColor playerColor,
            float deltaTime)
        {
            Vector2 safeSize = new Vector2(
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(playerSize.x)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(playerSize.y)));

            Aabb currentBounds =
                Aabb.FromCenter(
                    currentPosition + playerOffset,
                    safeSize);

            // 1. Evaluate current overlap.
            OverlapResult overlap =
                overlapResolver.Evaluate(
                    currentBounds,
                    playerColor);

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

            // 2. Apply the existing 80% movement reduction
            // while pushing out.
            float controlMultiplier =
                overlapResolver.GetControlMultiplier(
                    overlap.IsPushingOut);

            Vector2 adjustedInput =
                inputMove * controlMultiplier;

            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            // Convert jump/fall displacement intent back to
            // its current vertical velocity.
            //
            // jumpMove.y = velocityY * deltaTime
            //
            // Therefore:
            // velocityY = jumpMove.y / deltaTime
            float currentVerticalSpeed =
                safeDeltaTime > Mathf.Epsilon
                    ? jumpMove.y / safeDeltaTime
                    : 0f;

            // 3. Calculate one push speed.
            //
            // Horizontal:
            //     uses the existing configurable push speed.
            //
            // Vertical:
            //     automatically becomes greater than the current
            //     vertical velocity by VerticalPushSpeedMargin.
            float pushSpeed =
                overlap.IsPushingOut
                    ? overlapResolver.GetPushSpeed(
                        overlap.PushDirection,
                        currentVerticalSpeed)
                    : 0f;

            Vector2 pushMove =
                overlap.IsPushingOut
                    ? overlap.PushDirection *
                      pushSpeed *
                      safeDeltaTime
                    : Vector2.zero;

            // 4. Still build ONE final intent vector.
            Vector2 combinedIntent =
                adjustedInput +
                jumpMove +
                pushMove;

            if (logIntent)
            {
                Debug.Log(
                    $"[INTENT] input={inputMove} " +
                    $"multiplier={controlMultiplier:F2} " +
                    $"adjustedInput={adjustedInput} " +
                    $"jump={jumpMove} " +
                    $"push={pushMove} " +
                    $"combined={combinedIntent}");
            }

            // 5. Predict + resolve that ONE vector.
            CollisionResult collision =
                collisionResolver.Resolve(
                    currentPosition,
                    safeSize,
                    playerOffset,
                    combinedIntent,
                    playerColor,
                    overlap.IsPushingOut);

            if (logIntent)
            {
                Debug.Log(
                    $"[PUSH][RESULT] " +
                    $"active={overlap.IsPushingOut} " +
                    $"direction={overlap.PushDirection} " +
                    $"verticalVelocity={currentVerticalSpeed:F3} " +
                    $"speed={pushSpeed:F3} " +
                    $"horizontalBase={overlapResolver.OverlapPushSpeed:F3} " +
                    $"verticalBase={overlapResolver.VerticalPushSpeed:F3} " +
                    $"verticalMargin={overlapResolver.VerticalPushSpeedMargin:F3} " +
                    $"move={pushMove}");

                Debug.Log(
                    $"[RESOLVE][RESULT] " +
                    $"current={currentPosition} " +
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