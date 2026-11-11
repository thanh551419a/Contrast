namespace Contrast.Presentation
{
    using System;
    using UnityEngine;
    using Contrast.Color;
    using Contrast.Player;
    using Color = UnityEngine.Color;

    public enum PlayerAnimationState
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Fall = 3,
        Landing = 4,
        Death = 5
    }

    /// <summary>
    /// Presentation-only component attached to the Player that integrates the
    /// SilhouetteMotion character (Idle / Run / Jump sprite sheets & Animator),
    /// scales the maximum animated character bounds to match the authoritative
    /// Player collider without modifying physics/collision, aligns the character's
    /// feet to the platform contact plane, and preserves Black/Gray/White phase legibility.
    ///
    /// Strict Boundary Rule:
    /// All visual scaling, facing flips, and offsets are applied ONLY to child visual
    /// transforms/renderers. The authoritative Player GameObject transform, hitbox,
    /// movement velocity, jump height, gravity, and AABB remain 100% unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerVisualPresentation : MonoBehaviour
    {
        // =====================================================================
        // Authoritative SilhouetteMotion Package Metrics (from Demo & Sheets)
        // =====================================================================
        public const float PixelsPerUnit = 128f;
        public const int SheetCellSizePx = 256;
        public const int SheetColumns = 8;

        // Union non-transparent bounding box across all 213 frames (120 Idle + 32 Run + 61 Jump),
        // horizontally symmetric around cell center x = 128 (x in [44, 212)) and anchored at
        // the foot baseline y_bottom = 32 (y in [32, 237), corresponding to pivot (0.5, 0.125) in 256x256):
        public const int CropMinXPx = 44;
        public const int CropMinYBottomPx = 32;
        public const int CropWidthPx = 168;
        public const int CropHeightPx = 205;

        public const float MaxAnimatedVisualWidth = CropWidthPx / PixelsPerUnit;    // 1.3125000 units
        public const float MaxAnimatedVisualHeight = CropHeightPx / PixelsPerUnit;  // 1.6015625 units
        public static readonly Vector2 VisualPivot = new Vector2(0.5f, 0.0f);

        // Clip timing constants (from Idle.json, Run.json, Jump.json)
        public const int IdleFrameCount = 120;
        public const int RunFrameCount = 32;
        public const int JumpFrameCount = 61;

        public const float IdleDuration = 2.40f;
        public const float RunDuration = 0.64f;
        public const float JumpAnticipationDuration = 0.06f;
        public const float JumpAirDuration = 0.70f;
        public const float JumpLandingDuration = 0.18f;
        public const float JumpRestDuration = 0.28f;
        public const float JumpTotalDuration = JumpAnticipationDuration + JumpAirDuration + JumpLandingDuration + JumpRestDuration; // 1.22s
        public const float ReferenceJumpSpeed = 8.2f;

        private PlayerController player;
        private SpriteRenderer rawSpriteRenderer;

        // Visual child hierarchy
        private Transform visualRoot;
        private Animator characterAnimator;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer rimRenderer;
        private SpriteRenderer haloRenderer;
        private SpriteRenderer coreRenderer;

        // Visual Colors (matching CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html drawPlayer)
        public static readonly Color BrightBody = new Color(1f, 1f, 1f, 1f); // #ffffff
        public static readonly Color BrightRim = new Color(1f, 1f, 1f, 1f); // #ffffff
        public static readonly Color BrightHalo = new Color(1f, 1f, 1f, 0.38f);

        public static readonly Color GrayBody = new Color(0.522f, 0.522f, 0.522f, 1f); // #858585
        public static readonly Color GrayRim = new Color(0.522f, 0.522f, 0.522f, 1f); // #858585
        public static readonly Color GrayHalo = new Color(0.522f, 0.522f, 0.522f, 0f);

        public static readonly Color DarkBody = new Color(0.020f, 0.020f, 0.020f, 1f); // #050505
        public static readonly Color DarkStrokeRim = new Color(0.455f, 0.506f, 0.545f, 1f); // #74818b
        public static readonly Color DarkHalo = new Color(0.455f, 0.506f, 0.545f, 0f);

        public Color CurrentBodyColor => bodyRenderer != null ? bodyRenderer.color : Color.clear;
        public Color CurrentRimColor => rimRenderer != null ? rimRenderer.color : Color.clear;

        public Transform VisualRoot => visualRoot;
        public Animator CharacterAnimator => characterAnimator;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer RimRenderer => rimRenderer;

        // Animation state & diagnostics
        public PlayerAnimationState CurrentAnimationState { get; private set; } = PlayerAnimationState.Idle;
        public string CurrentClipName { get; private set; } = "Idle";
        public int CurrentFrameIndex { get; private set; } = 0;
        public int FacingDirection { get; private set; } = 1; // +1 Right, -1 Left
        public float ComputedUniformWorldScale { get; private set; } = 0.45238096f;

        public Vector2 ComputedMaxWorldVisualSize => new Vector2(
            MaxAnimatedVisualWidth * ComputedUniformWorldScale,
            MaxAnimatedVisualHeight * ComputedUniformWorldScale);

        public float ComputedFeetWorldY
        {
            get
            {
                Vector2 pSize = GetAuthoritativeColliderSize();
                Vector2 pOffset = player != null ? player.PlayerOffset : Vector2.zero;
                return transform.position.y + pOffset.y - pSize.y * 0.5f;
            }
        }

        private float idleClock = 0f;
        private float runClock = 0f;
        private float landingTimer = 100f;
        private bool wasGroundedLastFrame = true;
        private Vector3 lastPlayerWorldPos;
        private Vector3 lastPlatformWorldPos;
        private Contrast.Level.ColorPlatform lastGroundedPlatform;
        private bool hasPositionSample = false;

        // Cached SilhouetteMotion frames (normalized white interior + rim contour sprites)
        private static Sprite[] cachedIdleBodyFrames;
        private static Sprite[] cachedRunBodyFrames;
        private static Sprite[] cachedJumpBodyFrames;

        private static Sprite[] cachedIdleRimFrames;
        private static Sprite[] cachedRunRimFrames;
        private static Sprite[] cachedJumpRimFrames;

        private static Sprite cachedHaloSprite;
        private static Sprite cachedCoreSprite;
        private static RuntimeAnimatorController cachedAnimatorController;

        // Animator parameter hashes
        private static readonly int AnimStateHash = Animator.StringToHash("State");
        private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
        private static readonly int AnimGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AnimVerticalVelHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int AnimDeadHash = Animator.StringToHash("IsDead");

        // Motion trail
        private struct TrailGhost
        {
            public Vector2 pos;
            public Color color;
            public float size;
            public float life;
            public float maxLife;
        }

        private readonly TrailGhost[] trailPool = new TrailGhost[24];
        private int trailCount = 0;
        private float trailSpawnTimer = 0f;
        private const float TrailSpawnInterval = 0.045f;

        public static int GetFrameCount(string clipName)
        {
            EnsureSilhouetteFramesLoaded();
            return clipName switch
            {
                "Idle" => cachedIdleBodyFrames != null ? cachedIdleBodyFrames.Length : 0,
                "Run" => cachedRunBodyFrames != null ? cachedRunBodyFrames.Length : 0,
                "Jump" => cachedJumpBodyFrames != null ? cachedJumpBodyFrames.Length : 0,
                _ => 0
            };
        }

        public void InitializeVisuals()
        {
            if (player == null)
                player = GetComponent<PlayerController>();
            if (rawSpriteRenderer == null)
                rawSpriteRenderer = GetComponent<SpriteRenderer>();

            if (rawSpriteRenderer != null)
            {
                rawSpriteRenderer.enabled = false;
            }

            EnsureSilhouetteFramesLoaded();

            if (visualRoot != null)
            {
                UpdateVisualGeometry();
                UpdateStateColors();
                ApplyCurrentAnimationFrame();
                return;
            }

            SetupVisualHierarchy();
        }

        private void Awake()
        {
            InitializeVisuals();
        }

        private static Material cachedUnlitMaterial;
        public static Material GetSharedUnlitMaterial()
        {
            if (cachedUnlitMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    cachedUnlitMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }
            }
            return cachedUnlitMaterial;
        }

        private void SetupVisualHierarchy()
        {
            GameObject rootGo = new GameObject("VisualPresentationRoot");
            visualRoot = rootGo.transform;
            visualRoot.SetParent(transform, false);

            // Attach Animator on VisualPresentationRoot with root motion strictly disabled
            characterAnimator = rootGo.AddComponent<Animator>();
            characterAnimator.applyRootMotion = false;
            characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (cachedAnimatorController == null)
            {
                cachedAnimatorController = Resources.Load<RuntimeAnimatorController>("SilhouetteMotion/SilhouettePlayerAnimator");
            }
            if (cachedAnimatorController != null)
            {
                characterAnimator.runtimeAnimatorController = cachedAnimatorController;
            }

            Material unlitMat = GetSharedUnlitMaterial();

            // Halo (outer soft optical glow positioned at torso midpoint)
            GameObject haloGo = new GameObject("GlowHalo");
            haloGo.transform.SetParent(visualRoot, false);
            haloRenderer = haloGo.AddComponent<SpriteRenderer>();
            haloRenderer.sprite = GetHaloSprite();
            if (unlitMat != null) haloRenderer.sharedMaterial = unlitMat;
            haloRenderer.sortingOrder = 9;

            // Body (SilhouetteMotion animated character interior)
            GameObject bodyGo = new GameObject("OpticalBody");
            bodyGo.transform.SetParent(visualRoot, false);
            bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = SampleBodySprite("Idle", 0f, out int initFrame);
            if (unlitMat != null) bodyRenderer.sharedMaterial = unlitMat;
            bodyRenderer.sortingOrder = 10;

            // Rim (SilhouetteMotion animated perimeter outline so Black #050505 has #74818b rim against dark backgrounds)
            GameObject rimGo = new GameObject("OpticalRim");
            rimGo.transform.SetParent(visualRoot, false);
            rimRenderer = rimGo.AddComponent<SpriteRenderer>();
            rimRenderer.sprite = SampleRimSprite("Idle", initFrame);
            if (unlitMat != null) rimRenderer.sharedMaterial = unlitMat;
            rimRenderer.sortingOrder = 11;

            // Core dot (bright center chest optical emitter for BRIGHT state)
            GameObject coreGo = new GameObject("CoreLight");
            coreGo.transform.SetParent(visualRoot, false);
            coreRenderer = coreGo.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = GetCoreSprite();
            if (unlitMat != null) coreRenderer.sharedMaterial = unlitMat;
            coreRenderer.sortingOrder = 12;

            UpdateVisualGeometry();
            UpdateStateColors();
            ApplyCurrentAnimationFrame();

            lastPlayerWorldPos = transform.position;
            wasGroundedLastFrame = player != null && player.IsGrounded;
            hasPositionSample = true;
        }

        private Vector2 GetAuthoritativeColliderSize()
        {
            if (player != null)
            {
                return new Vector2(
                    Mathf.Max(0.01f, Mathf.Abs(player.PlayerSize.x)),
                    Mathf.Max(0.01f, Mathf.Abs(player.PlayerSize.y)));
            }
            return new Vector2(0.59375f, 1.0f);
        }

        /// <summary>
        /// Computes uniform visual scale so the maximum animated pose fits within the
        /// existing authoritative Player collider while aligning the character's feet
        /// with the bottom of the collider.
        /// </summary>
        public static float CalculateUniformWorldScale(Vector2 colliderSize)
        {
            float safeW = Mathf.Max(0.01f, Mathf.Abs(colliderSize.x));
            float safeH = Mathf.Max(0.01f, Mathf.Abs(colliderSize.y));

            float widthRatio = safeW / MaxAnimatedVisualWidth;
            float heightRatio = safeH / MaxAnimatedVisualHeight;
            return Mathf.Min(widthRatio, heightRatio);
        }

        public void UpdateVisualGeometry()
        {
            if (visualRoot == null)
                return;

            Vector2 colliderSize = GetAuthoritativeColliderSize();
            Vector2 colliderOffset = player != null ? player.PlayerOffset : Vector2.zero;

            ComputedUniformWorldScale = CalculateUniformWorldScale(colliderSize);

            float parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));
            float parentScaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.y));

            // Place visualRoot at the exact ground-contact feet point of the authoritative collider:
            float feetWorldOffsetY = colliderOffset.y - colliderSize.y * 0.5f;
            visualRoot.localPosition = new Vector3(
                colliderOffset.x / parentScaleX,
                feetWorldOffsetY / parentScaleY,
                0f);

            // Apply uniform world scale compensated for parent localScale
            visualRoot.localScale = new Vector3(
                ComputedUniformWorldScale / parentScaleX,
                ComputedUniformWorldScale / parentScaleY,
                1f);

            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localPosition = Vector3.zero;
                bodyRenderer.transform.localScale = Vector3.one;
                bodyRenderer.flipX = FacingDirection < 0;
            }

            if (rimRenderer != null)
            {
                rimRenderer.transform.localPosition = Vector3.zero;
                rimRenderer.transform.localScale = Vector3.one;
                rimRenderer.flipX = FacingDirection < 0;
            }

            float torsoCenterLocalY = MaxAnimatedVisualHeight * 0.48f;
            if (haloRenderer != null)
            {
                haloRenderer.transform.localPosition = new Vector3(0f, torsoCenterLocalY, 0f);
                haloRenderer.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            }

            if (coreRenderer != null)
            {
                coreRenderer.transform.localPosition = new Vector3(0f, torsoCenterLocalY, 0f);
                coreRenderer.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            }
        }

        public void OnPlayerRespawned()
        {
            InitializeVisuals();
            landingTimer = 100f;
            idleClock = 0f;
            runClock = 0f;
            wasGroundedLastFrame = player != null && player.IsGrounded;
            lastPlayerWorldPos = transform.position;
            lastGroundedPlatform = player != null ? player.GroundedPlatform : null;
            lastPlatformWorldPos = lastGroundedPlatform != null ? lastGroundedPlatform.transform.position : Vector3.zero;
            hasPositionSample = true;

            EvaluateAnimationState(0f);
            UpdateVisualGeometry();
            UpdateStateColors();
            ApplyCurrentAnimationFrame();
        }

        public void SyncVisualsNow()
        {
            InitializeVisuals();
            if (player != null)
            {
                EvaluateAnimationState(0f);
                UpdateVisualGeometry();
                UpdateStateColors();
                ApplyCurrentAnimationFrame();
            }
        }

        public void AdvanceAnimation(float deltaTime)
        {
            if (player == null)
                player = GetComponent<PlayerController>();
            if (player == null)
                return;

            InitializeVisuals();

            if (rawSpriteRenderer != null && rawSpriteRenderer.enabled)
                rawSpriteRenderer.enabled = false;

            if (characterAnimator != null && characterAnimator.applyRootMotion)
                characterAnimator.applyRootMotion = false;

            EvaluateAnimationState(Mathf.Max(0f, deltaTime));
            UpdateVisualGeometry();
            UpdateStateColors();
            ApplyCurrentAnimationFrame();
        }

        private void Update()
        {
            if (player == null)
                return;

            AdvanceAnimation(Time.deltaTime);
            UpdateTrail();
        }

        private void LateUpdate()
        {
            if (player == null || visualRoot == null)
                return;

            if (characterAnimator != null && characterAnimator.applyRootMotion)
                characterAnimator.applyRootMotion = false;

            UpdateVisualGeometry();
            ApplyCurrentAnimationFrame();
        }

        private void EvaluateAnimationState(float dt)
        {
            if (player == null)
                return;

            // 1. Determine horizontal self-movement (excluding moving platform carrier displacement)
            float horizontalIntent = player.LastMoveInputX;
            float selfVelX = horizontalIntent;

            if (hasPositionSample && dt > 0.00001f)
            {
                float worldDeltaX = transform.position.x - lastPlayerWorldPos.x;
                float carrierDeltaX = 0f;
                if (player.GroundedPlatform != null && player.GroundedPlatform == lastGroundedPlatform)
                {
                    carrierDeltaX = player.GroundedPlatform.transform.position.x - lastPlatformWorldPos.x;
                }

                float measuredSelfVelX = (worldDeltaX - carrierDeltaX) / dt;
                if (Mathf.Abs(horizontalIntent) <= 0.001f && Mathf.Abs(measuredSelfVelX) > 0.05f)
                {
                    selfVelX = measuredSelfVelX;
                }
            }

            lastPlayerWorldPos = transform.position;
            lastGroundedPlatform = player.GroundedPlatform;
            lastPlatformWorldPos = lastGroundedPlatform != null ? lastGroundedPlatform.transform.position : Vector3.zero;
            hasPositionSample = true;

            // Update Facing Direction (+1 Right, -1 Left) when moving horizontally
            if (player.State != PlayerState.Dead)
            {
                if (selfVelX > 0.01f)
                    FacingDirection = 1;
                else if (selfVelX < -0.01f)
                    FacingDirection = -1;
            }

            // 2. Advance internal clocks
            idleClock += dt;
            landingTimer += dt;

            float refMoveSpeed = Mathf.Max(0.1f, player.MoveSpeed);
            float absSpeed = Mathf.Abs(selfVelX);
            runClock += dt * Mathf.Max(0.4f, absSpeed / refMoveSpeed);

            bool isGrounded = player.IsGrounded || player.State == PlayerState.Grounded;
            if (isGrounded && !wasGroundedLastFrame && player.State != PlayerState.Dead)
            {
                landingTimer = 0f;
            }
            wasGroundedLastFrame = isGrounded;

            // 3. Map authoritative PlayerController state -> SilhouetteMotion animation state
            if (player.State == PlayerState.Dead)
            {
                CurrentAnimationState = PlayerAnimationState.Death;
                CurrentClipName = "Jump";
            }
            else if (!isGrounded)
            {
                float vy = player.CurrentVerticalVelocity;
                if (vy > 0.01f)
                {
                    CurrentAnimationState = PlayerAnimationState.Jump;
                }
                else
                {
                    CurrentAnimationState = PlayerAnimationState.Fall;
                }
                CurrentClipName = "Jump";
            }
            else if (absSpeed > 0.12f)
            {
                CurrentAnimationState = PlayerAnimationState.Run;
                CurrentClipName = "Run";
            }
            else if (landingTimer < JumpLandingDuration)
            {
                CurrentAnimationState = PlayerAnimationState.Landing;
                CurrentClipName = "Jump";
            }
            else
            {
                CurrentAnimationState = PlayerAnimationState.Idle;
                CurrentClipName = "Idle";
            }

            // Synchronize Animator parameters if controller is present
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            {
                characterAnimator.SetInteger(AnimStateHash, (int)CurrentAnimationState);
                characterAnimator.SetFloat(AnimSpeedHash, absSpeed);
                characterAnimator.SetBool(AnimGroundedHash, isGrounded);
                characterAnimator.SetFloat(AnimVerticalVelHash, player.CurrentVerticalVelocity);
                characterAnimator.SetBool(AnimDeadHash, player.State == PlayerState.Dead);
            }
        }

        private void ApplyCurrentAnimationFrame()
        {
            if (bodyRenderer == null)
                return;

            float sampleTime = 0f;
            string clip = CurrentClipName;

            switch (CurrentAnimationState)
            {
                case PlayerAnimationState.Idle:
                    clip = "Idle";
                    sampleTime = idleClock;
                    break;

                case PlayerAnimationState.Run:
                    clip = "Run";
                    sampleTime = runClock;
                    break;

                case PlayerAnimationState.Jump:
                case PlayerAnimationState.Fall:
                {
                    clip = "Jump";
                    // SilhouettePlayer velocity-driven airborne pose mapping:
                    float vy = player != null ? player.CurrentVerticalVelocity : 0f;
                    float u = vy >= 0f
                        ? 0.5f * (1f - Mathf.Clamp01(vy / ReferenceJumpSpeed))
                        : 0.5f + 0.49f * Mathf.Clamp01(-vy / (ReferenceJumpSpeed * 1.2f));
                    sampleTime = JumpAnticipationDuration + JumpAirDuration * u;
                    break;
                }

                case PlayerAnimationState.Landing:
                    clip = "Jump";
                    sampleTime = JumpAnticipationDuration + JumpAirDuration + Mathf.Clamp(landingTimer, 0f, JumpLandingDuration);
                    break;

                case PlayerAnimationState.Death:
                    clip = "Jump";
                    // Peak compression crouch pose
                    sampleTime = JumpAnticipationDuration * 0.95f;
                    break;
            }

            Sprite bodySprite = SampleBodySprite(clip, sampleTime, out int frameIndex);
            CurrentFrameIndex = frameIndex;

            if (bodySprite != null)
                bodyRenderer.sprite = bodySprite;

            if (rimRenderer != null)
            {
                Sprite rimSprite = SampleRimSprite(clip, frameIndex);
                if (rimSprite != null)
                    rimRenderer.sprite = rimSprite;
            }

            bodyRenderer.flipX = FacingDirection < 0;
            if (rimRenderer != null)
                rimRenderer.flipX = FacingDirection < 0;
        }

        private void UpdateStateColors()
        {
            if (player == null || bodyRenderer == null)
                return;

            LogicalColor color = player.CurrentColor;

            switch (color)
            {
                case LogicalColor.White:
                    bodyRenderer.color = BrightBody;
                    if (rimRenderer != null) rimRenderer.color = BrightRim;
                    if (haloRenderer != null) haloRenderer.color = BrightHalo;
                    if (coreRenderer != null)
                    {
                        coreRenderer.gameObject.SetActive(true);
                        coreRenderer.color = Color.white;
                    }
                    break;

                case LogicalColor.Gray:
                    bodyRenderer.color = GrayBody;
                    if (rimRenderer != null) rimRenderer.color = GrayRim;
                    if (haloRenderer != null) haloRenderer.color = GrayHalo;
                    if (coreRenderer != null)
                        coreRenderer.gameObject.SetActive(false);
                    break;

                case LogicalColor.Black:
                default:
                    // Dark state has true black interior (#050505) with crisp #74818b rim stroke matching HTML
                    bodyRenderer.color = DarkBody;
                    if (rimRenderer != null) rimRenderer.color = DarkStrokeRim;
                    if (haloRenderer != null) haloRenderer.color = DarkHalo;
                    if (coreRenderer != null)
                        coreRenderer.gameObject.SetActive(false);
                    break;
            }
        }

        private void UpdateTrail()
        {
            float dt = Time.deltaTime;
            trailSpawnTimer += dt;

            if (trailSpawnTimer >= TrailSpawnInterval && player.State != PlayerState.Dead)
            {
                trailSpawnTimer = 0f;
                if (trailCount < trailPool.Length)
                {
                    Color trailColor = player.CurrentColor switch
                    {
                        LogicalColor.White => new Color(0.95f, 0.97f, 1f, 0.35f),
                        LogicalColor.Gray => new Color(0.55f, 0.6f, 0.65f, 0.25f),
                        _ => new Color(0.75f, 0.82f, 0.88f, 0.2f)
                    };

                    trailPool[trailCount++] = new TrailGhost
                    {
                        pos = transform.position,
                        color = trailColor,
                        size = player.PlayerSize.x * 0.7f,
                        life = 0.24f,
                        maxLife = 0.24f
                    };
                }
            }

            for (int i = trailCount - 1; i >= 0; i--)
            {
                trailPool[i].life -= dt;
                if (trailPool[i].life <= 0f)
                {
                    trailPool[i] = trailPool[trailCount - 1];
                    trailCount--;
                }
            }
        }

        #region SilhouetteMotion Frame Slicing & Sampling

        private static Sprite SampleBodySprite(string mode, float time, out int index)
        {
            EnsureSilhouetteFramesLoaded();
            Sprite[] frames = mode == "Idle"
                ? cachedIdleBodyFrames
                : mode == "Run"
                    ? cachedRunBodyFrames
                    : cachedJumpBodyFrames;

            float duration = mode == "Idle"
                ? IdleDuration
                : mode == "Run"
                    ? RunDuration
                    : JumpTotalDuration;

            if (frames == null || frames.Length == 0)
            {
                index = 0;
                return null;
            }

            index = Mathf.Min(
                frames.Length - 1,
                Mathf.FloorToInt(Mathf.Repeat(Mathf.Max(0f, time), duration) / duration * frames.Length));
            return frames[index];
        }

        private static Sprite SampleRimSprite(string mode, int index)
        {
            EnsureSilhouetteFramesLoaded();
            Sprite[] frames = mode == "Idle"
                ? cachedIdleRimFrames
                : mode == "Run"
                    ? cachedRunRimFrames
                    : cachedJumpRimFrames;

            if (frames == null || frames.Length == 0)
                return null;

            int safeIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            return frames[safeIndex];
        }

        private static void EnsureSilhouetteFramesLoaded()
        {
            if (cachedIdleBodyFrames != null && cachedIdleBodyFrames.Length == IdleFrameCount && cachedIdleBodyFrames[0] != null &&
                cachedRunBodyFrames != null && cachedRunBodyFrames.Length == RunFrameCount && cachedRunBodyFrames[0] != null &&
                cachedJumpBodyFrames != null && cachedJumpBodyFrames.Length == JumpFrameCount && cachedJumpBodyFrames[0] != null)
            {
                return;
            }

            SliceSheetOrFallback("Idle", IdleFrameCount, out cachedIdleBodyFrames, out cachedIdleRimFrames);
            SliceSheetOrFallback("Run", RunFrameCount, out cachedRunBodyFrames, out cachedRunRimFrames);
            SliceSheetOrFallback("Jump", JumpFrameCount, out cachedJumpBodyFrames, out cachedJumpRimFrames);
        }

        private static void SliceSheetOrFallback(
            string mode,
            int frameCount,
            out Sprite[] bodyFrames,
            out Sprite[] rimFrames)
        {
            bodyFrames = new Sprite[frameCount];
            rimFrames = new Sprite[frameCount];

            Texture2D rawSheet = Resources.Load<Texture2D>("SilhouetteMotion/Sheets/" + mode);
            if (rawSheet != null)
            {
                try
                {
                    Color32[] srcPixels = rawSheet.GetPixels32();
                    int texW = rawSheet.width;
                    int texH = rawSheet.height;

                    Color32[] bodyPixels = new Color32[srcPixels.Length];
                    Color32[] rimPixels = new Color32[srcPixels.Length];

                    // 1. Normalize character RGB to pure white (255,255,255,a) so phase tinting is 1:1
                    // and transparent texels retain white RGB to avoid dark bilinear edge bleeding.
                    for (int i = 0; i < srcPixels.Length; i++)
                    {
                        byte a = srcPixels[i].a;
                        bodyPixels[i] = new Color32(255, 255, 255, a);
                    }

                    // 2. Build 2-pixel perimeter rim stroke around the silhouette within each 256x256 cell
                    for (int f = 0; f < frameCount; f++)
                    {
                        int cellX = (f % SheetColumns) * SheetCellSizePx;
                        int cellYTop = f / SheetColumns;
                        int cellY = texH - (cellYTop + 1) * SheetCellSizePx;
                        if (cellY < 0 || cellX + SheetCellSizePx > texW)
                            continue;

                        for (int y = 2; y < SheetCellSizePx - 2; y++)
                        {
                            int py = cellY + y;
                            int rowOffset = py * texW;
                            for (int x = 2; x < SheetCellSizePx - 2; x++)
                            {
                                int px = cellX + x;
                                int idx = rowOffset + px;
                                byte centerA = srcPixels[idx].a;

                                byte maxNeighborA = 0;
                                byte minNeighborA = 255;

                                for (int dy = -2; dy <= 2; dy++)
                                {
                                    int nRow = (py + dy) * texW;
                                    for (int dx = -2; dx <= 2; dx++)
                                    {
                                        byte na = srcPixels[nRow + px + dx].a;
                                        if (na > maxNeighborA) maxNeighborA = na;
                                        if (na < minNeighborA) minNeighborA = na;
                                    }
                                }

                                // Perimeter edge detection: strong where maxNeighborA is high and minNeighborA is low
                                int edgeAlpha = maxNeighborA - (minNeighborA * 3) / 4;
                                if (centerA > 0 && minNeighborA < 200)
                                {
                                    byte rimA = (byte)Mathf.Clamp(edgeAlpha, 0, 255);
                                    rimPixels[idx] = new Color32(255, 255, 255, rimA);
                                }
                            }
                        }
                    }

                    Texture2D bodyTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.DontSave
                    };
                    bodyTex.SetPixels32(bodyPixels);
                    bodyTex.Apply();

                    Texture2D rimTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.DontSave
                    };
                    rimTex.SetPixels32(rimPixels);
                    rimTex.Apply();

                    for (int i = 0; i < frameCount; i++)
                    {
                        int cellX = (i % SheetColumns) * SheetCellSizePx;
                        int cellY = texH - (i / SheetColumns + 1) * SheetCellSizePx;

                        Rect cropRect = new Rect(
                            cellX + CropMinXPx,
                            cellY + CropMinYBottomPx,
                            CropWidthPx,
                            CropHeightPx);

                        bodyFrames[i] = Sprite.Create(
                            bodyTex,
                            cropRect,
                            VisualPivot,
                            PixelsPerUnit,
                            0,
                            SpriteMeshType.FullRect);
                        bodyFrames[i].name = $"{mode}_{i:D3}";

                        rimFrames[i] = Sprite.Create(
                            rimTex,
                            cropRect,
                            VisualPivot,
                            PixelsPerUnit,
                            0,
                            SpriteMeshType.FullRect);
                        rimFrames[i].name = $"{mode}_Rim_{i:D3}";
                    }
                    return;
                }
                catch (Exception)
                {
                    // Fall through to deterministic procedural silhouette fallback if texture is non-readable
                }
            }

            // Deterministic fallback matching SilhouetteMotion cell crop (168 x 205 px @ 128 PPU, pivot (0.5, 0.0))
            BuildProceduralFallbackFrames(mode, frameCount, out bodyFrames, out rimFrames);
        }

        private static void BuildProceduralFallbackFrames(
            string mode,
            int frameCount,
            out Sprite[] bodyFrames,
            out Sprite[] rimFrames)
        {
            bodyFrames = new Sprite[frameCount];
            rimFrames = new Sprite[frameCount];

            Texture2D bodyTex = new Texture2D(CropWidthPx, CropHeightPx, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Texture2D rimTex = new Texture2D(CropWidthPx, CropHeightPx, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color32[] bodyPx = new Color32[CropWidthPx * CropHeightPx];
            Color32[] rimPx = new Color32[CropWidthPx * CropHeightPx];
            for (int y = 0; y < CropHeightPx; y++)
            {
                for (int x = 0; x < CropWidthPx; x++)
                {
                    bodyPx[y * CropWidthPx + x] = new Color32(255, 255, 255, 255);
                    bool isBorder = x < 3 || x >= CropWidthPx - 3 || y < 3 || y >= CropHeightPx - 3;
                    rimPx[y * CropWidthPx + x] = isBorder ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            bodyTex.SetPixels32(bodyPx);
            bodyTex.Apply();
            rimTex.SetPixels32(rimPx);
            rimTex.Apply();

            Rect rect = new Rect(0, 0, CropWidthPx, CropHeightPx);
            for (int i = 0; i < frameCount; i++)
            {
                bodyFrames[i] = Sprite.Create(bodyTex, rect, VisualPivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                bodyFrames[i].name = $"{mode}_{i:D3}";
                rimFrames[i] = Sprite.Create(rimTex, rect, VisualPivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                rimFrames[i].name = $"{mode}_Rim_{i:D3}";
            }
        }

        private static Sprite GetHaloSprite()
        {
            if (cachedHaloSprite != null)
                return cachedHaloSprite;

            const int res = 128;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float t = Mathf.Clamp01(dist / radius);
                    float alpha = Mathf.Pow(1f - t, 2.2f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedHaloSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect);

            return cachedHaloSprite;
        }

        private static Sprite GetCoreSprite()
        {
            if (cachedCoreSprite != null)
                return cachedCoreSprite;

            const int res = 32;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedCoreSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect);

            return cachedCoreSprite;
        }

        #endregion
    }
}
