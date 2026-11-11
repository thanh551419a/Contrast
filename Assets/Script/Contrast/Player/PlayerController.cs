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
        private float moveSpeed = 5.625f; // HTML parity: 360 px/s / 64 px/unit = 5.625 units/s

        public float MoveSpeed => moveSpeed;

        [Header("Player Geometry")]
        [SerializeField] private Vector2 playerSize = Vector2.one;
        [SerializeField] private Vector2 playerOffset = Vector2.zero;
        [Header("Vertical Fall Death (Fixed World-Space deathY)")]
        [SerializeField] private float levelDeathY = -50f;
        private float customDeathY = float.NaN;
        private float lastSupportedFeet = float.NegativeInfinity;

        public float EffectiveDeathY => GetEffectiveDeathY();

        public void SetLevelDeathY(float y)
        {
            if (!float.IsNaN(y) && !float.IsInfinity(y))
                levelDeathY = y;
        }

        public void SetCustomDeathY(float y)
        {
            customDeathY = y;
        }

        public void ClearCustomDeathY()
        {
            customDeathY = float.NaN;
        }

        public float GetEffectiveDeathY()
        {
            if (!float.IsNaN(customDeathY) && !float.IsInfinity(customDeathY))
                return customDeathY;

            GameManager gm = GameManager.Instance;
            if (gm != null)
                return gm.DeathY;

            return levelDeathY;
        }

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

        public void SetRuntimeGeometry(Vector2 size)
        {
            playerSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(size.x)),
                Mathf.Max(0.0001f, Mathf.Abs(size.y)));
            GetComponent<Contrast.Presentation.PlayerVisualPresentation>()?.SyncVisualsNow();
        }
        public ColorPlatform GroundedPlatform { get; private set; }
        public bool IsGrounded => isGrounded;
        public bool IsJumping => jumpMotion != null && jumpMotion.IsJumping;
        public float CurrentVerticalVelocity => jumpMotion != null ? jumpMotion.CurrentVerticalVelocity : 0f;
        public float LastMoveInputX { get; private set; }

        public const float HtmlJumpBufferDuration = 0.12f;
        public const float HtmlCoyoteTimeDuration = 0.10f;

        private SpriteRenderer sr;
        private PlayerInput inputReader;
        private PlayerMovementResolver movementResolver;
        private PlayerCollisionResolver collisionResolver;
        private PlayerJumpMotion jumpMotion;
        private bool isGrounded;
        private Vector2 riderDisplacement;
        private bool inputEnabled = true;
        private float jumpBufferTimer;
        private float coyoteTimer;
        private int jumpSequenceCounter;

        public float JumpBufferTimer => jumpBufferTimer;
        public float CoyoteTimer => coyoteTimer;
        public int TotalJumpRequests { get; private set; }
        public int TotalJumpsExecuted { get; private set; }
        public string LastJumpRequestId { get; private set; } = string.Empty;
        public string LastJumpRejectionReason { get; private set; } = "NONE";

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

#if UNITY_EDITOR
        public void SetGroundedForTest(bool grounded, ColorPlatform platform)
        {
            isGrounded = grounded;
            GroundedPlatform = platform;
            coyoteTimer = grounded ? HtmlCoyoteTimeDuration : 0f;
            if (grounded)
            {
                if (platform != null)
                {
                    Rect platRect = platform.GetAabb();
                    transform.position = new Vector3(
                        platform.transform.position.x,
                        platRect.yMax + playerSize.y * 0.5f,
                        transform.position.z);
                }
                State = PlayerState.Grounded;
                jumpMotion?.NotifyGrounded(true);
            }
        }
#endif

        public void SetRiderDisplacement(Vector2 disp)
        {
            riderDisplacement = disp;
        }

        public bool IsTestModeActive { get; private set; }
        public static System.Func<Vector2> CustomMouseDeltaReader { get; set; }

        public void SetTestMode(bool enabled)
        {
            IsTestModeActive = enabled;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            if (enabled)
            {
                jumpMotion?.ResetMotion();
                isGrounded = false;
                GroundedPlatform = null;
                riderDisplacement = Vector2.zero;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                jumpMotion?.ResetMotion();
                State = PlayerState.Falling;
                RefreshGroundedStateImmediate();
            }
        }

        public static Vector2 ReadMouseDelta()
        {
            if (CustomMouseDeltaReader != null)
                return CustomMouseDeltaReader();

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
            return Vector2.zero;
#else
            return new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private void Awake()
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            inputReader = GetComponent<PlayerInput>();
            movementResolver = GetComponent<PlayerMovementResolver>();
            collisionResolver = GetComponent<PlayerCollisionResolver>();
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

            inputReader ??= GetComponent<PlayerInput>();
            jumpMotion ??= GetComponent<PlayerJumpMotion>();
            movementResolver ??= GetComponent<PlayerMovementResolver>();
            collisionResolver ??= GetComponent<PlayerCollisionResolver>();

            if (IsTestModeActive)
            {
                NewtonMarkRequested = false;

                // 1. Arrow keys phase tuning / color changes still allowed (HTML lines 1549-1552)
                if (inputReader != null)
                {
                    PlayerInputIntent testInput = inputReader.ReadInput(deltaTime, moveSpeed, false);
                    if (Mathf.Abs(testInput.PhaseTuneDelta) > Mathf.Epsilon)
                    {
                        TunePhase(testInput.PhaseTuneDelta);
                    }
                    else if (testInput.ColorChunkStep != 0)
                    {
                        StepColorChunk(testInput.ColorChunkStep);
                    }
                    else if (testInput.HasColorChange)
                    {
                        SetColor(testInput.RequestedColor);
                    }
                }

                // 2. Relative mouse movement displacement (HTML lines 1554-1576)
                Vector2 mouseDelta = ReadMouseDelta();
                UnityEngine.Camera cam = UnityEngine.Camera.main;
                float orthoSize = cam != null && cam.orthographic ? cam.orthographicSize : 5f;
                float screenH = Screen.height > 0 ? (float)Screen.height : 1080f;
                float unitsPerPixel = (2f * orthoSize) / screenH;
                float sensitivity = 1.0f;

                Vector2 worldDisplacement = new Vector2(
                    mouseDelta.x * unitsPerPixel * sensitivity,
                    mouseDelta.y * unitsPerPixel * sensitivity);

                if (worldDisplacement != Vector2.zero)
                {
                    transform.position = new Vector3(
                        transform.position.x + worldDisplacement.x,
                        transform.position.y + worldDisplacement.y,
                        transform.position.z);
                }

                // 3. Keep physics & velocities zeroed
                jumpMotion?.ResetMotion();
                isGrounded = false;
                GroundedPlatform = null;
                riderDisplacement = Vector2.zero;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;

                return;
            }

            NewtonMarkRequested = false;

            // Synchronize grounded state from geometry before reading input if not actively ascending
            if (!isGrounded && !(jumpMotion != null && jumpMotion.IsJumping))
            {
                RefreshGroundedStateImmediate();
            }

            PlayerInputIntent input =
                inputReader.ReadInput(deltaTime, moveSpeed, isGrounded);

            if (!inputEnabled)
            {
                if (input.JumpPressed)
                {
                    LastJumpRejectionReason = "INPUT_DISABLED";
                }
                input = new PlayerInputIntent(Vector2.zero, false, false, currentColor, 0);
            }

            LastMoveInputX = deltaTime > 0.00001f ? (input.Move.x / deltaTime) : input.Move.x;

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
                    $"jumpPressed={input.JumpPressed} " +
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

            // Re-evaluate ground support if color/phase change made underlying platform passable or solid
            if (isGrounded && GroundedPlatform != null && !GroundedPlatform.IsUniversal &&
                GroundedPlatform.Logical == currentColor)
            {
                isGrounded = false;
                GroundedPlatform = null;
                riderDisplacement = Vector2.zero;
                coyoteTimer = 0f;
                RefreshGroundedStateImmediate();
            }
            else if (!isGrounded && !(jumpMotion != null && jumpMotion.IsJumping))
            {
                RefreshGroundedStateImmediate();
            }

            if (input.JumpPressed || input.JumpRequested)
            {
                jumpSequenceCounter++;
                TotalJumpRequests++;
                LastJumpRequestId = $"JUMP_{jumpSequenceCounter:D4}";
                jumpBufferTimer = HtmlJumpBufferDuration;
            }
            else
            {
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);
            }

            if (isGrounded && !(jumpMotion != null && jumpMotion.IsJumping))
            {
                coyoteTimer = HtmlCoyoteTimeDuration;
            }
            else
            {
                coyoteTimer = Mathf.Max(0f, coyoteTimer - deltaTime);
            }

            bool canExecuteJump = (input.JumpRequested || jumpBufferTimer > 0f) &&
                                  (isGrounded || coyoteTimer > 0f) &&
                                  !(jumpMotion != null && jumpMotion.IsJumping && jumpMotion.CurrentVerticalVelocity > 0f);

            if (canExecuteJump)
            {
                jumpMotion?.RequestJump();
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isGrounded = false;
                GroundedPlatform = null;
                State = PlayerState.Falling;
                TotalJumpsExecuted++;
                LastJumpRejectionReason = "NONE";
                Contrast.Audio.ContrastAudioManager.Instance?.PlayJump();
            }
            else if (input.JumpPressed)
            {
                LastJumpRejectionReason = !isGrounded && coyoteTimer <= 0f
                    ? "AIRBORNE_BUFFERED"
                    : "ALREADY_ASCENDING";
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

            if (result.BlockedY && jumpIntent.y > 0f && !result.IsGrounded)
            {
                jumpMotion?.NotifyCeilingBlocked();
            }

            isGrounded = result.IsGrounded;
            GroundedPlatform = isGrounded ? result.GroundedPlatform : null;

            if (result.IsStuck)
            {
                jumpMotion?.ResetMotion();
                coyoteTimer = 0f;
                State = PlayerState.Stuck;
            }
            else if (isGrounded)
            {
                lastSupportedFeet = transform.position.y - playerSize.y * 0.5f;
                coyoteTimer = HtmlCoyoteTimeDuration;
                State = PlayerState.Grounded;
                jumpMotion?.NotifyGrounded(true);
            }
            else
            {
                State = PlayerState.Falling;
                jumpMotion?.NotifyGrounded(false);
            }

            float effectiveDeathY = GetEffectiveDeathY();
            if (!float.IsNaN(effectiveDeathY) && transform.position.y < effectiveDeathY)
            {
                if (logFlow)
                {
                    Debug.Log($"[DEATHY] playerY={transform.position.y:F3} deathY={effectiveDeathY:F3} triggered=True");
                }
                Die();
            }
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
            RefreshGroundedStateImmediate();
            GetComponent<Contrast.Presentation.PlayerVisualPresentation>()?.OnPlayerRespawned();
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
            RefreshGroundedStateImmediate();
            GetComponent<Contrast.Presentation.PlayerVisualPresentation>()?.OnPlayerRespawned();
        }

        public void ResetState()
        {
            jumpMotion ??= GetComponent<PlayerJumpMotion>();
            isGrounded = false;
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            LastMoveInputX = 0f;
            inputEnabled = true;
            jumpMotion?.ResetMotion();
            State = PlayerState.Falling;
        }

        public void RefreshGroundedStateImmediate()
        {
            collisionResolver ??= GetComponent<PlayerCollisionResolver>();
            jumpMotion ??= GetComponent<PlayerJumpMotion>();
            if (collisionResolver == null)
                return;

            Vector2 safeSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(playerSize.y)));

            Aabb bounds = Aabb.FromCenter(
                (Vector2)transform.position + playerOffset,
                safeSize);

            if (collisionResolver.TryGetGroundSupport(bounds, currentColor, out ColorPlatform supportPlatform))
            {
                isGrounded = true;
                GroundedPlatform = supportPlatform;
                coyoteTimer = HtmlCoyoteTimeDuration;
                lastSupportedFeet = transform.position.y - safeSize.y * 0.5f;
                State = PlayerState.Grounded;
                jumpMotion?.NotifyGrounded(true);
            }
        }

        public void StopMovement()
        {
            jumpMotion ??= GetComponent<PlayerJumpMotion>();
            isGrounded = false;
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            LastMoveInputX = 0f;
            jumpMotion?.ResetMotion();

            if (State != PlayerState.Dead)
                State = PlayerState.Falling;
        }

        private void Die()
        {
            if (IsTestModeActive)
                return;

            State = PlayerState.Dead;
            isGrounded = false;
            GroundedPlatform = null;
            riderDisplacement = Vector2.zero;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            LastMoveInputX = 0f;
            jumpMotion ??= GetComponent<PlayerJumpMotion>();
            jumpMotion?.ResetMotion();
            GetComponent<Contrast.Presentation.PlayerVisualPresentation>()?.SyncVisualsNow();
            Contrast.Audio.ContrastAudioManager.Instance?.PlayDeath();
            Contrast.Presentation.VisualPresentationManager.Instance?.NotifyPlayerDied();
            GameManager.Instance?.OnPlayerDied();
        }

        private void ApplyColorVisual()
        {
            if (sr != null)
            {
                float value = ColorClassifier.ToValue(currentColor);
                sr.color = new UnityEngine.Color(value, value, value, 1f);
            }

            GetComponent<Contrast.Presentation.PlayerVisualPresentation>()?.SyncVisualsNow();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position + (Vector3)playerOffset;
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawWireCube(center, playerSize);
        }
    }
}
