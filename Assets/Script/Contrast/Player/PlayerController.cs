using Contrast.Color;
using Contrast.Core;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Player
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovementResolver))]
    [RequireComponent(typeof(PlayerJumpMotion))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float moveSpeed = 6f;

        [Header("Player Geometry")]
        [SerializeField] private Vector2 playerSize = Vector2.one;
        [SerializeField] private Vector2 playerOffset = Vector2.zero;

        [Header("Color")]
        [SerializeField] private LogicalColor currentColor = LogicalColor.White;
        [SerializeField, Range(0f, 254f)] private float currentColorValue = 0f;
        [SerializeField] private bool autoCreateSprite = true;

        [Header("Debug")]
        [SerializeField] private bool logFlow = true;

        public PlayerState State { get; private set; } = PlayerState.Falling;
        public LogicalColor CurrentColor => currentColor;
        public float CurrentColorValue => currentColorValue;
        public Vector2 PlayerSize => playerSize;
        public Vector2 PlayerOffset => playerOffset;

        private SpriteRenderer sr;
        private PlayerInput inputReader;
        private PlayerMovementResolver movementResolver;
        private PlayerJumpMotion jumpMotion;
        private bool isGrounded;
        private Vector2 riderDisplacement;

        public void SetRiderDisplacement(Vector2 disp)
        {
            riderDisplacement = disp;
        }

        private void Awake()
        {
            inputReader = GetComponent<PlayerInput>();
            movementResolver = GetComponent<PlayerMovementResolver>();
            jumpMotion = GetComponent<PlayerJumpMotion>();
            sr = GetComponent<SpriteRenderer>();

            if (sr == null && autoCreateSprite)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = ColorPlatform.GetDefaultSprite();
                sr.sortingOrder = 1;
            }

            ApplyColorVisual();
        }

        /// <summary>
        /// The ONLY gameplay update entry for the Player.
        /// GameManager calls this once from its single Unity Update().
        /// </summary>
        public void ProcessUpdate(float deltaTime)
        {
            if (State == PlayerState.Dead)
                return;

            deltaTime = Mathf.Max(0f, deltaTime);

            PlayerInputIntent input =
                inputReader.ReadInput(deltaTime, moveSpeed, isGrounded);

            if (logFlow)
            {
                Debug.Log(
                    $"[FLOW][INPUT] " +
                    $"pos={transform.position} " +
                    $"inputMove={input.Move} " +
                    $"jumpRequested={input.JumpRequested} " +
                    $"colorChange={input.HasColorChange} " +
                    $"color={input.RequestedColor} " +
                    $"grounded={isGrounded}");
            }

            if (input.ColorChunkStep != 0)
                StepColorChunk(input.ColorChunkStep);
            else if (input.HasColorChange)
                SetColor(input.RequestedColor);

            if (input.JumpRequested)
                jumpMotion?.RequestJump();

            Vector2 jumpIntent = jumpMotion != null
                ? jumpMotion.GetMoveIntent(deltaTime, isGrounded)
                : Vector2.zero;

            if (logFlow)
                Debug.Log($"[FLOW][JUMP_INTENT] {jumpIntent}");

            MovementResult result =
                movementResolver.Resolve(
                    transform.position,
                    playerSize,
                    playerOffset,
                    input.Move,
                    jumpIntent,
                    currentColor,
                    deltaTime,
                    riderDisplacement);

            riderDisplacement = Vector2.zero;

            if (logFlow)
            {
                Debug.Log(
                    $"[FLOW] pos={transform.position} " +
                    $"input={input.Move} " +
                    $"jump={jumpIntent} " +
                    $"combined={result.CombinedIntent} " +
                    $"push={result.PushMove} " +
                    $"overlap={result.OverlapPercent:F2}% " +
                    $"pushDir={result.PushDirection} " +
                    $"blockedX={result.BlockedX} blockedY={result.BlockedY} " +
                    $"grounded={result.IsGrounded} stuck={result.IsStuck} " +
                    $"final={result.FinalMove}");
            }

            ApplyMove(result.FinalMove);

            isGrounded = result.IsGrounded;

            if (result.IsStuck)
            {
                jumpMotion?.ResetMotion();
                State = PlayerState.Stuck;
            }
            else if (isGrounded)
            {
                State = PlayerState.Grounded;
                jumpMotion?.NotifyGrounded(true);
            }
            else
            {
                State = PlayerState.Falling;
                jumpMotion?.NotifyGrounded(false);
            }

            GameManager gm = GameManager.Instance;
            if (gm != null && transform.position.y < gm.KillY)
                Die();
        }

        /// <summary>
        /// The ONLY normal gameplay method allowed to change Player position.
        /// </summary>
        public void ApplyMove(Vector2 finalMove)
        {
            Vector3 before = transform.position;

            if (finalMove != Vector2.zero)
            {
                transform.position = new Vector3(
                    before.x + finalMove.x,
                    before.y + finalMove.y,
                    before.z);
            }

            if (logFlow)
            {
                Debug.Log(
                    $"[POSITION] {before} + {finalMove} = {transform.position}");
            }
        }

        public void StepColorChunk(int direction)
        {
            // Direction > 0: advance to next chunk (White -> Gray -> Black -> White)
            // Direction < 0: go to previous chunk (Black -> Gray -> White -> Black)
            LogicalColor next = currentColor;
            if (direction > 0)
            {
                next = currentColor switch
                {
                    LogicalColor.White => LogicalColor.Gray,
                    LogicalColor.Gray => LogicalColor.Black,
                    LogicalColor.Black => LogicalColor.White,
                    _ => LogicalColor.White
                };
            }
            else if (direction < 0)
            {
                next = currentColor switch
                {
                    LogicalColor.White => LogicalColor.Black,
                    LogicalColor.Gray => LogicalColor.White,
                    LogicalColor.Black => LogicalColor.Gray,
                    _ => LogicalColor.White
                };
            }

            SetColor(next);
        }

        public void SetColor(LogicalColor color)
        {
            currentColor = color;
            currentColorValue = ColorClassifier.DefaultValue(color);
            ApplyColorVisual();

            if (logFlow)
            {
                Debug.Log(
                    $"[COLOR] {currentColor} (val={currentColorValue}) at {transform.position}");
            }
        }

        public void SetColorValue(float value)
        {
            currentColorValue = Mathf.Clamp(value, 0f, 254f);
            currentColor = ColorClassifier.Classify(currentColorValue);
            ApplyColorVisual();

            if (logFlow)
            {
                Debug.Log(
                    $"[COLOR_VALUE] val={currentColorValue} logical={currentColor} at {transform.position}");
            }
        }

        public void SetSpawnPosition(Vector2 worldPosition)
        {
            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                transform.position.z);
        }

        /// <summary>
        /// Respawns using both position and gameplay color.
        /// Retry and checkpoint activation both use this path.
        /// </summary>
        public void Respawn(Vector2 worldPosition, LogicalColor color)
        {
            SetColor(color);
            SetSpawnPosition(worldPosition);
            ResetState();
        }

        public void ResetState()
        {
            isGrounded = false;
            jumpMotion?.ResetMotion();
            State = PlayerState.Falling;
        }

        public void StopMovement()
        {
            isGrounded = false;
            jumpMotion?.ResetMotion();

            if (State != PlayerState.Dead)
                State = PlayerState.Falling;
        }

        private void Die()
        {
            State = PlayerState.Dead;
            isGrounded = false;
            jumpMotion?.ResetMotion();
            GameManager.Instance?.OnPlayerDied();
        }

        private void ApplyColorVisual()
        {
            if (sr == null)
                return;

            float value = ColorClassifier.ToValue(currentColor);
            sr.color = new UnityEngine.Color(value, value, value, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position + (Vector3)playerOffset;
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawWireCube(center, playerSize);
        }
    }
}
