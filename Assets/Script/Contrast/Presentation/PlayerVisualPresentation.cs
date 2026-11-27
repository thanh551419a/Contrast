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
        Death = 5,
        Celebrate = 6
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

        // Clip timing constants (from CatHandoff Idle.json, Run.json, Jump.json, Fall.json, Land.json, Celebrate.json)
        public const int IdleFrameCount = 110;
        public const int RunFrameCount = 28;
        public const int JumpFrameCount = 61;
        public const int FallFrameCount = 24;
        public const int LandFrameCount = 9;
        public const int CelebrateFrameCount = 32;
        public const int PointMarkFrameCount = 32;

        public const float IdleDuration = 2.20f;
        public const float RunDuration = 0.56f;
        public const float FallDuration = 0.48f;
        public const float LandDuration = 0.18f;
        public const float CelebrateDuration = 0.64f;
        public const float PointMarkDuration = 0.60f;
        public const float JumpAnticipationDuration = 0.07f;
        public const float JumpAirDuration = 0.64f;
        public const float JumpLandingDuration = 0.18f;
        public const float JumpRestDuration = 0.32f;
        public const float JumpTotalDuration = JumpAnticipationDuration + JumpAirDuration + JumpLandingDuration + JumpRestDuration; // 1.21s
        public const float ReferenceJumpSpeed = 7.4f;

        /// <summary>
        /// Duration of continuous downward falling before transitioning from the Jump apex/descent
        /// pose into the dedicated looping "Fall" animation state (24 frames, 0.48s loop).
        /// </summary>
        public const float LongFallThresholdSeconds = 0.25f;

        private PlayerController player;
        private SpriteRenderer rawSpriteRenderer;

        // Visual child hierarchy
        private Transform visualRoot;
        private Animator characterAnimator;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer rimRenderer;
        private SpriteRenderer eyesRenderer;
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
        public SpriteRenderer EyesRenderer => eyesRenderer;

        // Animation state & diagnostics
        public PlayerAnimationState CurrentAnimationState { get; private set; } = PlayerAnimationState.Idle;
        public string CurrentClipName { get; private set; } = "Idle";
        public int CurrentFrameIndex { get; private set; } = 0;
        public int FacingDirection { get; private set; } = 1; // +1 Right, -1 Left
        public float ComputedUniformWorldScale { get; private set; } = 0.45238096f;
        public float CurrentFallDuration => fallDurationTimer;
        public bool IsLongFalling => CurrentAnimationState == PlayerAnimationState.Fall && CurrentClipName == "Fall";

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
        private float fallClock = 0f;
        private float fallDurationTimer = 0f;
        private float landingTimer = 100f;
        private bool wasGroundedLastFrame = true;
        private Vector3 lastPlayerWorldPos;
        private Vector3 lastPlatformWorldPos;
        private Contrast.Level.ColorPlatform lastGroundedPlatform;
        private bool hasPositionSample = false;

        // Cached SilhouetteMotion frames (normalized white interior + rim contour + emerald eyes #13D47A sprites)
        private static Sprite[] cachedIdleBodyFrames;
        private static Sprite[] cachedRunBodyFrames;
        private static Sprite[] cachedJumpBodyFrames;
        private static Sprite[] cachedFallBodyFrames;
        private static Sprite[] cachedLandBodyFrames;
        private static Sprite[] cachedCelebrateBodyFrames;
        private static Sprite[] cachedPointMarkBodyFrames;

        private static Sprite[] cachedIdleRimFrames;
        private static Sprite[] cachedRunRimFrames;
        private static Sprite[] cachedJumpRimFrames;
        private static Sprite[] cachedFallRimFrames;
        private static Sprite[] cachedLandRimFrames;
        private static Sprite[] cachedCelebrateRimFrames;
        private static Sprite[] cachedPointMarkRimFrames;

        private static Sprite[] cachedIdleEyesFrames;
        private static Sprite[] cachedRunEyesFrames;
        private static Sprite[] cachedJumpEyesFrames;
        private static Sprite[] cachedFallEyesFrames;
        private static Sprite[] cachedLandEyesFrames;
        private static Sprite[] cachedCelebrateEyesFrames;
        private static Sprite[] cachedPointMarkEyesFrames;

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
            switch (clipName)
            {
                case "Idle":
                    return cachedIdleBodyFrames != null ? cachedIdleBodyFrames.Length : 0;
                case "Run":
                    return cachedRunBodyFrames != null ? cachedRunBodyFrames.Length : 0;
                case "Jump":
                    return cachedJumpBodyFrames != null ? cachedJumpBodyFrames.Length : 0;
                case "Fall":
                    return cachedFallBodyFrames != null ? cachedFallBodyFrames.Length : 0;
                case "Land":
                    return cachedLandBodyFrames != null ? cachedLandBodyFrames.Length : 0;
                case "Celebrate":
                case "AnMung":
                    return cachedCelebrateBodyFrames != null ? cachedCelebrateBodyFrames.Length : 0;
                case "PointMark":
                case "ChonDiem":
                    return cachedPointMarkBodyFrames != null ? cachedPointMarkBodyFrames.Length : 0;
                default:
                    return 0;
            }
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

            // Eyes (Chibi Cat emerald green #13D47A eyes layer rendered untinted on top of body & rim)
            GameObject eyesGo = new GameObject("OpticalEyes");
            eyesGo.transform.SetParent(visualRoot, false);
            eyesRenderer = eyesGo.AddComponent<SpriteRenderer>();
            eyesRenderer.sprite = SampleEyesSprite("Idle", initFrame);
            eyesRenderer.color = Color.white;
            if (unlitMat != null) eyesRenderer.sharedMaterial = unlitMat;
            eyesRenderer.sortingOrder = 12;

            // Core dot (legacy chest emitter kept inactive so Chibi Cat face eyes remain clean)
            GameObject coreGo = new GameObject("CoreLight");
            coreGo.transform.SetParent(visualRoot, false);
            coreRenderer = coreGo.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = GetCoreSprite();
            if (unlitMat != null) coreRenderer.sharedMaterial = unlitMat;
            coreRenderer.sortingOrder = 13;
            coreRenderer.enabled = false;

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

            if (eyesRenderer != null)
            {
                eyesRenderer.transform.localPosition = Vector3.zero;
                eyesRenderer.transform.localScale = Vector3.one;
                eyesRenderer.flipX = FacingDirection < 0;
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
            fallClock = 0f;
            fallDurationTimer = 0f;
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
            if (isGrounded)
            {
                fallDurationTimer = 0f;
                fallClock = 0f;
            }
            wasGroundedLastFrame = isGrounded;

            // 3. Map authoritative PlayerController state -> Chibi Cat animation state
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
                    fallDurationTimer = 0f;
                    fallClock = 0f;
                    CurrentAnimationState = PlayerAnimationState.Jump;
                    CurrentClipName = "Jump";
                }
                else
                {
                    fallDurationTimer += dt;
                    fallClock += dt;
                    if (fallDurationTimer >= LongFallThresholdSeconds)
                    {
                        // Falling for too long -> switch into the dedicated looping "Fall" animation set
                        CurrentAnimationState = PlayerAnimationState.Fall;
                        CurrentClipName = "Fall";
                    }
                    else
                    {
                        // Initial apex turnover before long-fall threshold -> remain in Jump arc frames
                        CurrentAnimationState = PlayerAnimationState.Jump;
                        CurrentClipName = "Jump";
                    }
                }
            }
            else if (absSpeed > 0.12f)
            {
                CurrentAnimationState = PlayerAnimationState.Run;
                CurrentClipName = "Run";
            }
            else if (landingTimer < LandDuration)
            {
                CurrentAnimationState = PlayerAnimationState.Landing;
                CurrentClipName = "Land";
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
                {
                    clip = "Jump";
                    // Velocity-driven airborne pose mapping for ascent and initial apex turnover:
                    float vy = player != null ? player.CurrentVerticalVelocity : 0f;
                    float u = vy >= 0f
                        ? 0.5f * (1f - Mathf.Clamp01(vy / ReferenceJumpSpeed))
                        : 0.5f + 0.49f * Mathf.Clamp01(-vy / (ReferenceJumpSpeed * 1.2f));
                    sampleTime = JumpAnticipationDuration + JumpAirDuration * u;
                    break;
                }

                case PlayerAnimationState.Fall:
                    // Dedicated looping "Fall" animation set when falling for too long
                    clip = "Fall";
                    sampleTime = fallClock;
                    break;

                case PlayerAnimationState.Landing:
                    clip = "Land";
                    sampleTime = Mathf.Clamp(landingTimer, 0f, LandDuration - 0.001f);
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

            if (eyesRenderer != null)
            {
                Sprite eyesSprite = SampleEyesSprite(clip, frameIndex);
                eyesRenderer.sprite = eyesSprite;
                eyesRenderer.color = Color.white;
            }

            bodyRenderer.flipX = FacingDirection < 0;
            if (rimRenderer != null)
                rimRenderer.flipX = FacingDirection < 0;
            if (eyesRenderer != null)
                eyesRenderer.flipX = FacingDirection < 0;
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
                    Color trailColor;
                    switch (player.CurrentColor)
                    {
                        case LogicalColor.White:
                            trailColor = new Color(0.95f, 0.97f, 1f, 0.35f);
                            break;
                        case LogicalColor.Gray:
                            trailColor = new Color(0.55f, 0.6f, 0.65f, 0.25f);
                            break;
                        default:
                            trailColor = new Color(0.75f, 0.82f, 0.88f, 0.2f);
                            break;
                    }

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

        public static Sprite SampleBodySprite(string mode, float time, out int index)
        {
            EnsureSilhouetteFramesLoaded();
            bool isCelebrate = mode == "Celebrate" || mode == "AnMung";
            bool isPointMark = mode == "PointMark" || mode == "ChonDiem";
            Sprite[] frames = mode == "Idle"
                ? cachedIdleBodyFrames
                : mode == "Run"
                    ? cachedRunBodyFrames
                    : mode == "Fall"
                        ? cachedFallBodyFrames
                        : mode == "Land"
                            ? cachedLandBodyFrames
                            : isCelebrate
                                ? cachedCelebrateBodyFrames
                                : isPointMark
                                    ? cachedPointMarkBodyFrames
                                    : cachedJumpBodyFrames;

            float duration = mode == "Idle"
                ? IdleDuration
                : mode == "Run"
                    ? RunDuration
                    : mode == "Fall"
                        ? FallDuration
                        : mode == "Land"
                            ? LandDuration
                            : isCelebrate
                                ? CelebrateDuration
                                : isPointMark
                                    ? PointMarkDuration
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

        public static Sprite SampleRimSprite(string mode, int index)
        {
            EnsureSilhouetteFramesLoaded();
            bool isCelebrate = mode == "Celebrate" || mode == "AnMung";
            bool isPointMark = mode == "PointMark" || mode == "ChonDiem";
            Sprite[] frames = mode == "Idle"
                ? cachedIdleRimFrames
                : mode == "Run"
                    ? cachedRunRimFrames
                    : mode == "Fall"
                        ? cachedFallRimFrames
                        : mode == "Land"
                            ? cachedLandRimFrames
                            : isCelebrate
                                ? cachedCelebrateRimFrames
                                : isPointMark
                                    ? cachedPointMarkRimFrames
                                    : cachedJumpRimFrames;

            if (frames == null || frames.Length == 0)
                return null;

            int safeIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            return frames[safeIndex];
        }

        public static Sprite SampleEyesSprite(string mode, int index)
        {
            EnsureSilhouetteFramesLoaded();
            bool isCelebrate = mode == "Celebrate" || mode == "AnMung";
            bool isPointMark = mode == "PointMark" || mode == "ChonDiem";
            Sprite[] frames = mode == "Idle"
                ? cachedIdleEyesFrames
                : mode == "Run"
                    ? cachedRunEyesFrames
                    : mode == "Fall"
                        ? cachedFallEyesFrames
                        : mode == "Land"
                            ? cachedLandEyesFrames
                            : isCelebrate
                                ? cachedCelebrateEyesFrames
                                : isPointMark
                                    ? cachedPointMarkEyesFrames
                                    : cachedJumpEyesFrames;

            if (frames == null || frames.Length == 0)
                return null;

            int safeIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            return frames[safeIndex];
        }

        private static bool AreFramesValid(Sprite[] frames, int expectedCount)
        {
            return frames != null &&
                   frames.Length == expectedCount &&
                   frames[0] != null &&
                   frames[0].texture != null;
        }

        private static void EnsureSilhouetteFramesLoaded()
        {
            if (AreFramesValid(cachedIdleBodyFrames, IdleFrameCount) &&
                AreFramesValid(cachedRunBodyFrames, RunFrameCount) &&
                AreFramesValid(cachedJumpBodyFrames, JumpFrameCount) &&
                AreFramesValid(cachedFallBodyFrames, FallFrameCount) &&
                AreFramesValid(cachedLandBodyFrames, LandFrameCount) &&
                AreFramesValid(cachedCelebrateBodyFrames, CelebrateFrameCount) &&
                AreFramesValid(cachedPointMarkBodyFrames, PointMarkFrameCount))
            {
                return;
            }

            SliceSheetOrFallback("Idle", IdleFrameCount, out cachedIdleBodyFrames, out cachedIdleRimFrames, out cachedIdleEyesFrames);
            SliceSheetOrFallback("Run", RunFrameCount, out cachedRunBodyFrames, out cachedRunRimFrames, out cachedRunEyesFrames);
            SliceSheetOrFallback("Jump", JumpFrameCount, out cachedJumpBodyFrames, out cachedJumpRimFrames, out cachedJumpEyesFrames);
            SliceSheetOrFallback("Fall", FallFrameCount, out cachedFallBodyFrames, out cachedFallRimFrames, out cachedFallEyesFrames);
            SliceSheetOrFallback("Land", LandFrameCount, out cachedLandBodyFrames, out cachedLandRimFrames, out cachedLandEyesFrames);
            SliceSheetOrFallback("Celebrate", CelebrateFrameCount, out cachedCelebrateBodyFrames, out cachedCelebrateRimFrames, out cachedCelebrateEyesFrames);
            SliceSheetOrFallback("PointMark", PointMarkFrameCount, out cachedPointMarkBodyFrames, out cachedPointMarkRimFrames, out cachedPointMarkEyesFrames);
        }

        private static void SliceSheetOrFallback(
            string mode,
            int frameCount,
            out Sprite[] bodyFrames,
            out Sprite[] rimFrames,
            out Sprite[] eyesFrames)
        {
            bodyFrames = new Sprite[frameCount];
            rimFrames = new Sprite[frameCount];
            eyesFrames = new Sprite[frameCount];

            Texture2D rawSheet = Resources.Load<Texture2D>("SilhouetteMotion/Sheets/" + mode);
            if (rawSheet == null && (mode == "PointMark" || mode == "ChonDiem"))
                rawSheet = Resources.Load<Texture2D>("SilhouetteMotion/Sheets/Celebrate");

            Color32[] srcPixels = null;
            int texW = 0;
            int texH = 0;

            if (rawSheet != null)
            {
                try
                {
                    srcPixels = rawSheet.GetPixels32();
                    texW = rawSheet.width;
                    texH = rawSheet.height;
                }
                catch (Exception)
                {
                    srcPixels = null;
                }
            }

            if (srcPixels == null && (mode == "Celebrate" || mode == "AnMung"))
            {
                try
                {
                    srcPixels = SynthesizeCelebrateSheetPixels(frameCount, out texW, out texH);
                }
                catch (Exception)
                {
                    srcPixels = null;
                }
            }

            if (srcPixels != null && texW > 0 && texH > 0)
            {
                try
                {
                    float sheetScale = Mathf.Max(0.125f, (float)texW / (SheetColumns * SheetCellSizePx));
                    int cellSize = Mathf.Max(16, Mathf.RoundToInt(SheetCellSizePx * sheetScale));
                    // Symmetric horizontal crop [32..224) centered at x=128 so Chibi Cat tail (x>=38) and paws (x<=198) never clip
                    int cropMinX = Mathf.RoundToInt(32 * sheetScale);
                    int cropMinY = Mathf.RoundToInt(CropMinYBottomPx * sheetScale);
                    int cropW = Mathf.Max(8, Mathf.RoundToInt(192 * sheetScale));
                    int cropH = Mathf.Max(8, Mathf.RoundToInt(212 * sheetScale));
                    float effectivePpu = PixelsPerUnit * sheetScale;

                    Color32[] bodyPixels = new Color32[srcPixels.Length];
                    Color32[] rimPixels = new Color32[srcPixels.Length];
                    Color32[] eyesPixels = new Color32[srcPixels.Length];

                    // 1. Normalize character RGB to pure white (255,255,255,a) so phase tinting is 1:1
                    // and extract the Chibi Cat emerald green eyes (#13D47A, R=19, G=212, B=122) into eyesPixels.
                    for (int i = 0; i < srcPixels.Length; i++)
                    {
                        Color32 src = srcPixels[i];
                        byte a = src.a;
                        bodyPixels[i] = new Color32(255, 255, 255, a);

                        if (a > 0 && src.g > src.r + 6)
                        {
                            float eyeBlend = Mathf.Clamp01(((src.g - src.r) - 2f) / 191f);
                            byte eyeA = (byte)Mathf.Clamp(Mathf.RoundToInt(eyeBlend * a), 0, 255);
                            if (eyeA > 0)
                                eyesPixels[i] = new Color32(19, 212, 122, eyeA);
                        }
                    }

                    // 2. Build 2-pixel perimeter rim stroke around the silhouette within each cell
                    for (int f = 0; f < frameCount; f++)
                    {
                        int cellX = (f % SheetColumns) * cellSize;
                        int cellYTop = f / SheetColumns;
                        int cellY = texH - (cellYTop + 1) * cellSize;
                        if (cellY < 0 || cellX + cellSize > texW)
                            continue;

                        for (int y = 2; y < cellSize - 2; y++)
                        {
                            int py = cellY + y;
                            int rowOffset = py * texW;
                            for (int x = 2; x < cellSize - 2; x++)
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

                    Texture2D eyesTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.DontSave
                    };
                    eyesTex.SetPixels32(eyesPixels);
                    eyesTex.Apply();

                    for (int i = 0; i < frameCount; i++)
                    {
                        int cellX = (i % SheetColumns) * cellSize;
                        int cellY = texH - (i / SheetColumns + 1) * cellSize;

                        Rect cropRect = new Rect(
                            Mathf.Clamp(cellX + cropMinX, 0, texW - cropW),
                            Mathf.Clamp(cellY + cropMinY, 0, texH - cropH),
                            cropW,
                            cropH);

                        bodyFrames[i] = Sprite.Create(
                            bodyTex,
                            cropRect,
                            VisualPivot,
                            effectivePpu,
                            0,
                            SpriteMeshType.FullRect);
                        bodyFrames[i].name = $"{mode}_{i:D3}";
                        bodyFrames[i].hideFlags = HideFlags.DontSave;

                        rimFrames[i] = Sprite.Create(
                            rimTex,
                            cropRect,
                            VisualPivot,
                            effectivePpu,
                            0,
                            SpriteMeshType.FullRect);
                        rimFrames[i].name = $"{mode}_Rim_{i:D3}";
                        rimFrames[i].hideFlags = HideFlags.DontSave;

                        eyesFrames[i] = Sprite.Create(
                            eyesTex,
                            cropRect,
                            VisualPivot,
                            effectivePpu,
                            0,
                            SpriteMeshType.FullRect);
                        eyesFrames[i].name = $"{mode}_Eyes_{i:D3}";
                        eyesFrames[i].hideFlags = HideFlags.DontSave;
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

        /// <summary>
        /// Synthesizes the 32-frame Celebrate ("AnMung") sprite sheet directly from the authoritative
        /// SilhouetteMotion character model (identical head, tapered torso, and grounded legs; ONLY the
        /// 2 arms move upward from the shoulders into a joyful celebration wave).
        /// </summary>
        private static Color32[] SynthesizeCelebrateSheetPixels(int frameCount, out int texW, out int texH)
        {
            const int cellSize = 128; // 0.5x sheet scale (1024 x 512) for fast runtime synthesis
            int rows = (frameCount + SheetColumns - 1) / SheetColumns;
            texW = SheetColumns * cellSize;
            texH = rows * cellSize;
            Color32[] pixels = new Color32[texW * texH];

            for (int f = 0; f < frameCount; f++)
            {
                float phase = (f / (float)frameCount) * Mathf.PI * 2f;
                float v = Mathf.Sin(phase);
                float sx = 1f - 0.018f * v * 0.45f;
                float sy = 1f + 0.018f * v;
                float hip = -1.2f * (0.5f - 0.5f * Mathf.Cos(phase * 2f));
                float wave = Mathf.Sin(phase);

                int cellX = (f % SheetColumns) * cellSize;
                int cellYTop = f / SheetColumns;
                int cellYBottom = texH - (cellYTop + 1) * cellSize;

                // Legs in model coords: s in {-1, +1}, quadratic Bezier from (s*6, -29+hip) -> (s*10, -17+hip) -> (s*12, -5), r=5
                // Arms in hip-relative coords (y_hip = my - (-29+hip)): from (s*8, -28) -> (s*(18.5+wave*2.5), -42-wave*1.6) -> (s*(24.5+wave*5), -59.5-|wave|*3.8), r=4.5
                Vector2 armL0 = new Vector2(-8f, -28f);
                Vector2 armL1 = new Vector2(-(18.5f + wave * 2.5f), -42f - wave * 1.6f);
                Vector2 armL2 = new Vector2(-(24.5f + wave * 5.0f), -59.5f - Mathf.Abs(wave) * 3.8f);

                Vector2 armR0 = new Vector2(8f, -28f);
                Vector2 armR1 = new Vector2(18.5f + wave * 2.5f, -42f - wave * 1.6f);
                Vector2 armR2 = new Vector2(24.5f + wave * 5.0f, -59.5f - Mathf.Abs(wave) * 3.8f);

                Vector2 legL0 = new Vector2(-6f, -29f + hip);
                Vector2 legL1 = new Vector2(-10f, -17f + hip);
                Vector2 legL2 = new Vector2(-12f, -5f);

                Vector2 legR0 = new Vector2(6f, -29f + hip);
                Vector2 legR1 = new Vector2(10f, -17f + hip);
                Vector2 legR2 = new Vector2(12f, -5f);

                for (int ly = 14; ly < cellSize - 6; ly++)
                {
                    int py = cellYBottom + ly;
                    float canvasY = (cellSize - 1 - ly) * 2f; // mapped to 256-cell canvas coords
                    float my = (canvasY - 224f) / (2f * sy);
                    float hy = my - (-29f + hip);

                    for (int lx = 20; lx < cellSize - 20; lx++)
                    {
                        int px = cellX + lx;
                        float canvasX = lx * 2f;
                        float mx = (canvasX - 128f) / (2f * sx);
                        Vector2 mp = new Vector2(mx, my);
                        Vector2 hp = new Vector2(mx, hy);

                        // Signed distance to head circle at (0, -51), r = 14
                        float dHead = Vector2.Distance(hp, new Vector2(0f, -51f)) - 14f;

                        // Signed distance to tapered torso: hy in [-34.5, 0], halfWidth tapers from 15 at hy=0 to 10 at hy=-29
                        float dTorso = 99f;
                        if (hy <= 0.5f && hy >= -35.5f)
                        {
                            float u = Mathf.Clamp01(-hy / 29f);
                            float halfW = Mathf.Lerp(15f, 10f, u * u);
                            float topCapY = -29f - 5.5f * Mathf.Max(0f, 1f - (mx * mx) / 100f);
                            float dx = Mathf.Abs(mx) - halfW;
                            float dy = Mathf.Max(hy - 0f, topCapY - hy);
                            dTorso = Mathf.Max(dx, dy);
                        }

                        // Signed distance to legs (r = 5.0) & raised celebration arms (r = 4.5)
                        float dLegL = DistanceToQuadBezier(mp, legL0, legL1, legL2) - 5.0f;
                        float dLegR = DistanceToQuadBezier(mp, legR0, legR1, legR2) - 5.0f;
                        float dArmL = DistanceToQuadBezier(hp, armL0, armL1, armL2) - 4.5f;
                        float dArmR = DistanceToQuadBezier(hp, armR0, armR1, armR2) - 4.5f;

                        float dMin = Mathf.Min(Mathf.Min(dHead, dTorso), Mathf.Min(Mathf.Min(dLegL, dLegR), Mathf.Min(dArmL, dArmR)));
                        if (dMin < 0.75f)
                        {
                            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(0.75f - dMin) * 255f), 0, 255);
                            if (a > 0)
                                pixels[py * texW + px] = new Color32(136, 137, 135, a);
                        }
                    }
                }
            }

            return pixels;
        }

        private static float DistanceToQuadBezier(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float minDistSq = float.MaxValue;
            Vector2 prev = a;
            const int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float inv = 1f - t;
                Vector2 cur = inv * inv * a + 2f * inv * t * b + t * t * c;
                Vector2 seg = cur - prev;
                float lenSq = seg.sqrMagnitude;
                float proj = lenSq > 1e-5f ? Mathf.Clamp01(Vector2.Dot(p - prev, seg) / lenSq) : 0f;
                Vector2 closest = prev + seg * proj;
                float dSq = (p - closest).sqrMagnitude;
                if (dSq < minDistSq) minDistSq = dSq;
                prev = cur;
            }
            return Mathf.Sqrt(minDistSq);
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
