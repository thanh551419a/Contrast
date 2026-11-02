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
        [Header("Void Fall")]
        [SerializeField] private float voidGraceUnits = 3.75f;
        private float lastSupportedFeet = float.NegativeInfinity;

        [Header("Color & Phase")]
        [SerializeField] private LogicalColor currentColor = LogicalColor.White;
        [SerializeField, Range(0f, 255f)] private float currentColorValue = 255f;
        [SerializeField] private float currentPhase = 0f;
        [SerializeField] private bool autoCreateSprite = true;

        [Header("Debug")]
        [SerializeField] private bool logFlow = true;

        public PlayerState State { get; private set; } = PlayerState.Falling;
        public LogicalColor CurrentColor => currentColor;
        public float CurrentColorValue => currentColorValue;
        public float CurrentPhase => currentPhase;
        public float CurrentIntensity => CalculateIntensity(currentPhase);
        public bool NewtonMarkRequested { get; private set; }
        public event System.Action OnNewtonMarkRequested;

        public Vector2 PlayerSize => playerSize;
        public Vector2 PlayerOffset => playerOffset;
        public ColorPlatform GroundedPlatform { get; private set; }
        public bool IsGrounded => isGrounded;

        private SpriteRenderer sr;
        private PlayerInput inputReader;
        private PlayerMovementResolver movementResolver;
        private PlayerJumpMotion jumpMotion;
        private bool isGrounded;
        private Vector2 riderDisplacement;
        private bool inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

#if UNITY_EDITOR
        public void SetGroundedForTest(bool grounded, ColorPlatform platform)
        {
            isGrounded = grounded;
            GroundedPlatform = platform;
        }
#endif

        public void SetRiderDisplacement(Vector2 disp)
        {
            riderDisplacement = disp;
        }

        private void Awake()
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
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

        private void Reset()
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            playerSize = Vector2.one;
            playerOffset = Vector2.zero;
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

            NewtonMarkRequested = false;

            PlayerInputIntent input =
                inputReader.ReadInput(deltaTime, moveSpeed, isGrounded);

            if (!inputEnabled)
            {
                input = new PlayerInputIntent(Vector2.zero, false, false, currentColor, 0);
            }

            if (input.NewtonMarkRequested)
            {
                NewtonMarkRequested = true;
                OnNewtonMarkRequested?.Invoke();
            }

            if (logFlow)
            {
                Debug.Log(
                    $"[FLOW][INPUT] " +
                    $"pos={transform.position} " +
                    $"inputMove={input.Move} " +
                    $"jumpRequested={input.JumpRequested} " +
                    $"fastFall={input.FastFall} " +
                    $"phaseTuneDelta={input.PhaseTuneDelta:F3} " +
                    $"newtonMark={NewtonMarkRequested} " +
                    $"colorChange={input.HasColorChange} " +
                    $"color={input.RequestedColor} " +
                    $"phase={currentPhase:F2} " +
                    $"grounded={isGrounded}");
            }

            if (Mathf.Abs(input.PhaseTuneDelta) > Mathf.Epsilon)
            {
                TunePhase(input.PhaseTuneDelta);
            }
            else if (input.ColorChunkStep != 0)
            {
                StepColorChunk(input.ColorChunkStep);
            }
            else if (input.HasColorChange)
            {
                SetColor(input.RequestedColor);
            }

            if (input.JumpRequested)
            {
                jumpMotion?.RequestJump();
                Contrast.Audio.ContrastAudioManager.Instance?.PlayJump();
            }

            Vector2 jumpIntent = jumpMotion != null
                ? jumpMotion.GetMoveIntent(deltaTime, isGrounded, input.FastFall)
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
            GroundedPlatform = isGrounded ? result.GroundedPlatform : null;

            if (result.IsStuck)
            {
                jumpMotion?.ResetMotion();
                State = PlayerState.Stuck;
            }
            else if (isGrounded)
            {
                lastSupportedFeet = transform.position.y - playerSize.y * 0.5f;
                State = PlayerState.Grounded;
                jumpMotion?.NotifyGrounded(true);
            }
            else
            {
                State = PlayerState.Falling;
                jumpMotion?.NotifyGrounded(false);

                // Authoritative HTML free-world void death:
                // If the player drops beyond void grace distance with no potential reachable landing below, trigger death.
                float currentFeet = transform.position.y - playerSize.y * 0.5f;
                if (!float.IsNegativeInfinity(lastSupportedFeet) &&
                    currentFeet < lastSupportedFeet - voidGraceUnits &&
                    !HasPotentialLandingBelow())
                {
                    Die();
                }
            }

            GameManager gm = GameManager.Instance;
            if (gm != null && transform.position.y < gm.KillY)
                Die();
        }

        private bool HasPotentialLandingBelow()
        {
            float feet = transform.position.y - playerSize.y * 0.5f;
            float left = transform.position.x - playerSize.x * 0.5f;
            float right = transform.position.x + playerSize.x * 0.5f;

            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform p = ColorPlatform.All[i];
                if (p == null || !p.gameObject.activeInHierarchy || !p.enabled) continue;
                Rect aabb = p.GetAabb();
                float deltaY = feet - aabb.yMax;
                if (deltaY < -0.05f) continue; // Platform top is above player feet

                // Optimistic freefall reach
                float airborneTime = Mathf.Sqrt(2f * Mathf.Max(0f, deltaY) / 20f) + 0.28f;
                float horizontalReach = Mathf.Max(3.28f, moveSpeed * airborneTime) + playerSize.x;
                if (aabb.xMin <= right + horizontalReach && aabb.xMax >= left - horizontalReach)
                    return true;
            }
            return false;
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

        public static float WrapPhase(float phi)
        {
            float tau = Mathf.PI * 2f;
            phi %= tau;
            return phi < 0f ? phi + tau : phi;
        }

        public static float CalculateIntensity(float phase)
        {
            return Mathf.Clamp01((1f + Mathf.Cos(phase)) * 0.5f);
        }

        public static LogicalColor ClassifyIntensity(float intensity)
        {
            // Authoritative HTML thresholds:
            // WHITE: I >= 0.78
            // BLACK: I <= 0.22
            // GRAY:  0.22 < I < 0.78
            if (intensity >= 0.78f) return LogicalColor.White;
            if (intensity <= 0.22f) return LogicalColor.Black;
            return LogicalColor.Gray;
        }

        public void SetPhase(float phase)
        {
            currentPhase = WrapPhase(phase);
            UpdateColorFromPhase();
        }

        public void TunePhase(float delta)
        {
            currentPhase = WrapPhase(currentPhase + delta);
            UpdateColorFromPhase();
        }

        private void UpdateColorFromPhase()
        {
            float intensity = CalculateIntensity(currentPhase);
            LogicalColor newColor = ClassifyIntensity(intensity);
            if (currentColor != newColor)
            {
                Contrast.Audio.ContrastAudioManager.Instance?.PlayColorShift(newColor);
                currentColor = newColor;
            }

            currentColorValue = intensity * 255f;
            ApplyColorVisual();

            if (logFlow)
            {
                Debug.Log($"[PHASE_TUNE] phase={currentPhase:F3} intensity={intensity:F3} color={currentColor}");
            }
        }

        public void SetColor(LogicalColor color)
        {
            if (currentColor != color)
            {
                Contrast.Audio.ContrastAudioManager.Instance?.PlayColorShift(color);
            }

            currentColor = color;
            currentColorValue = ColorClassifier.DefaultValue(color);

            // Synchronize phase with chosen color
            currentPhase = color switch
            {
                LogicalColor.White => 0f,
                LogicalColor.Gray => Mathf.PI * 0.5f,
                LogicalColor.Black => Mathf.PI,
                _ => 0f
            };

            ApplyColorVisual();

            if (logFlow)
            {
                Debug.Log(
                    $"[COLOR] {currentColor} (val={currentColorValue}, phase={currentPhase:F2}) at {transform.position}");
            }
        }

        public void Respawn(Vector2 worldPosition, float phase)
        {
            SetSpawnPosition(worldPosition);
            SetPhase(phase);
            ResetState();
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
            lastSupportedFeet = worldPosition.y - playerSize.y * 0.5f;
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
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            inputEnabled = true;
            jumpMotion?.ResetMotion();
            State = PlayerState.Falling;
        }

        public void StopMovement()
        {
            isGrounded = false;
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            jumpMotion?.ResetMotion();

            if (State != PlayerState.Dead)
                State = PlayerState.Falling;
        }

        private void Die()
        {
            State = PlayerState.Dead;
            isGrounded = false;
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            jumpMotion?.ResetMotion();
            Contrast.Audio.ContrastAudioManager.Instance?.PlayDeath();
            Contrast.Presentation.VisualPresentationManager.Instance?.NotifyPlayerDied();
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
