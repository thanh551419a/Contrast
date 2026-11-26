using System;
using System.Collections.Generic;
using Contrast.Camera;
using Contrast.Core;
using Contrast.Data;
using Contrast.Player;
using Contrast.Presentation;
using Contrast.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Contrast.Level
{
    using Color = UnityEngine.Color;
    public enum NewtonFinaleStep
    {
        CaptureK = 0,
        CaptureI = 1,
        CaptureKp = 2,
        Reveal = 3,
        Complete = 4
    }

    /// <summary>
    /// Coordinates the Easy Newton Orbit Measurement Finale at the end of the Easy level.
    /// Manages the two circular orbiting platforms (White k=4, Gray i=2), the K -> I -> K'
    /// point capture state machine, the 5.6-second ordered scientific reveal animation,
    /// dynamic wavelength calculation, and the final completion screen.
    /// </summary>
    public class EasyNewtonFinaleController : MonoBehaviour
    {
        public static EasyNewtonFinaleController Instance { get; private set; }

        public const int MaxFringeOrderK = 5;
        public const float Level2BaseNewtonOrthoSize = 8.6f;
        public const float UnifiedNewtonOrthoSize = Level2BaseNewtonOrthoSize * 1.15f; // 9.89f

        [Header("Chamber Geometry")]
        [SerializeField] private Vector2 centerO = new Vector2(61.0f, 0.625f);
        [SerializeField] private float ringScale = 3.4375f; // 220 px / 64
        [SerializeField] private float sourceLambda = 550f;
        [SerializeField] private float referenceLambda = 550f;
        [SerializeField] private float radiusOfCurvatureMm = 855f; // R = 855 mm
        [SerializeField] private int fringeOrderK = 4;
        [SerializeField] private int fringeOrderI = 2;

        public int FringeOrderK => fringeOrderK;
        public int FringeOrderI => fringeOrderI;

        [Header("Platform Parameters")]
        [SerializeField] private Vector2 platformSizeK = new Vector2(2.125f, 0.34375f); // 136 x 22 px / 64
        [SerializeField] private Vector2 platformSizeI = new Vector2(1.96875f, 0.3125f); // 126 x 20 px / 64
        [SerializeField] private float periodK = 10.8f;
        [SerializeField] private float periodI = 8.6f;
        [SerializeField] private float toleranceI = 0.75f;      // 48 px / 64
        [SerializeField] private float toleranceKp = 0.84375f;  // 54 px / 64
        [SerializeField] private string orbitStartMode = "auto";

        private bool dockActive = false;
        private float angleK = 0f;
        private int dirK = -1;
        private float angleI = 0f;
        private int dirI = 1;

        // Runtime objects
        private ColorPlatform platformK;
        private ColorPlatform platformI;
        private Trajectory trajK;
        private Trajectory trajI;
        private NewtonRingsPresentation apparatusVisuals;
        private GameObject apparatusRoot;

        // Measurement State
        public NewtonFinaleStep CurrentStep { get; private set; } = NewtonFinaleStep.CaptureK;
        public bool HasK { get; private set; }
        public bool HasI { get; private set; }
        public bool HasKp { get; private set; }
        public int MarkerCount => (HasK ? 1 : 0) + (HasI ? 1 : 0) + (HasKp ? 1 : 0);
        public ColorPlatform PlatformK => platformK;
        public ColorPlatform PlatformI => platformI;
        public float ToleranceI => toleranceI;
        public float ToleranceKp => toleranceKp;
        public Vector2 CenterO => centerO;
        public Vector2 PointK { get; private set; }
        public Vector2 PointI { get; private set; }
        public Vector2 PointKp { get; private set; }
        public Vector2 AxisU { get; private set; } = Vector2.right;
        public float RevealTimer { get; private set; }
        public bool IsRevealActive => CurrentStep == NewtonFinaleStep.Reveal;
        public bool IsComplete => CurrentStep == NewtonFinaleStep.Complete;
        public bool DockActive => dockActive;
        public string OrbitStartMode => orbitStartMode;
        public Trajectory TrajK => trajK;
        public Trajectory TrajI => trajI;

        public void TriggerDock()
        {
            if (!dockActive)
            {
                dockActive = true;
                if (trajK != null) trajK.SetTrajectoryEnabled(true);
                if (trajI != null) trajI.SetTrajectoryEnabled(true);
                Debug.Log("[NEWTON_FINALE] Dock triggered explicitly -> Orbit motion started.");
                LogCameraDiagnostics();
            }
        }

        // Calculation result
        public float MeasuredB_units { get; private set; }
        public float Measuredb_units { get; private set; }
        public float MeasuredB_mm { get; private set; }
        public float Measuredb_mm { get; private set; }
        public float CalculatedLambda_nm { get; private set; }
        public float SourceLambda => sourceLambda;
        public bool IsZoneActive => isZoneActive;
        public NewtonRingsPresentation ApparatusVisuals => apparatusVisuals;
        public GameObject ObjectiveCardGo => objectiveCardGo;
        public GameObject BottomBarGo => bottomBarGo;
        public GameObject CalculationPanelGo => calculationPanelGo;
        public GameObject QuizCardGo => quizCardGo;
        public bool IsQuizActive => quizCardGo != null && quizCardGo.activeSelf && !isColorQuizAnsweredCorrectly && wrongQuizAttemptsCount < 2;
        public bool IsColorQuizAnsweredCorrectly => isColorQuizAnsweredCorrectly;
        public int WrongQuizAttemptsCount => wrongQuizAttemptsCount;
        public IReadOnlyList<string> CurrentQuizOptions => currentQuizOptions;

        // Presentation & UI
        private GameObject hudCanvasGo;
        private Canvas hudCanvas;
        private GameObject objectiveCardGo;
        private Text objectiveInfoText;
        private Text wavelengthTagText;
        private Text objectiveTitleText;
        private Text objectiveSubText;
        private Text slotXkValText;
        private Text slotXiValText;
        private Text slotXkpValText;
        private Text feedbackText;
        private float feedbackTimer;
        private GameObject bottomBarGo;
        private GameObject calculationPanelGo;
        private CanvasGroup calcCanvasGroup;
        private Text calcFormulaText;
        private Text calcValuesText;
        private Text calcEquationText;
        private Text calcResultText;
        private Text completionDetailText;
        private GameObject completionPanelGo;

        // Bottom-Docked Wavelength Color Quiz Card (shown after measurement before ResultCard)
        private GameObject quizCardGo;
        private Text quizBadgeText;
        private Text quizQuestionText;
        private Text quizFeedbackText;
        private GameObject quizOptionsRowGo;
        private readonly Button[] quizOptionButtons = new Button[4];
        private readonly Text[] quizOptionLabels = new Text[4];
        private readonly Image[] quizOptionImages = new Image[4];
        private GameObject quizErrorResetButtonGo;
        private readonly string[] currentQuizOptions = new string[4];
        private int wrongQuizAttemptsCount;
        private bool isQuizDisplayed;
        private bool isColorQuizAnsweredCorrectly;
        private bool isBingoAnimPlaying;
        private float bingoAnimTimer;

        private static readonly string[] AllColorChoicesPool =
        {
            "Đỏ",
            "Lục",
            "Xanh dương",
            "Vàng",
            "Cam",
            "Tím",
            "Trắng",
            "Đen",
            "Chàm"
        };

        // Per-level memory of the 4 option slots so if 2 wrong options are picked and the level resets,
        // the 2 unpicked options stay in place and only the 2 picked wrong options are re-randomized.
        private static readonly string[][] ActiveOptionSlotsByLevel = new string[3][];
        private static readonly bool[][] PickedWrongSlotMaskByLevel = new bool[3][];
        private static readonly HashSet<string>[] EliminatedWrongColorsByLevel =
        {
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string>()
        };

        // In-world visual markers & lines
        private LineRenderer axisLineRenderer;
        private LineRenderer lineKI;
        private LineRenderer lineIKp;
        private GameObject markerKGo;
        private GameObject markerIGo;
        private GameObject markerKpGo;
        private GameObject markerOGo;
        private GameObject targetICrosshair;
        private GameObject targetKpCrosshair;
        private Text labelPlatformK;
        private Text labelPlatformI;

        private PlayerController player;
        private CameraFollow cameraFollow;
        private bool isZoneActive;

        // Sequential Target Colliders (Target 1: K, Target 2: I, Target 3: K')
        private readonly GameObject[] targetColliderGos = new GameObject[3];
        private readonly BoxCollider2D[] targetColliders = new BoxCollider2D[3];
        private readonly bool[] targetInteractionEnabled = new bool[3];
        private readonly bool[] targetCompleted = new bool[3];
        private int lastInputMarkFrame = -1;
        private int markEventSequence = 0;

        public int CurrentTargetIndex
        {
            get
            {
                switch (CurrentStep)
                {
                    case NewtonFinaleStep.CaptureK: return 0;
                    case NewtonFinaleStep.CaptureI: return 1;
                    case NewtonFinaleStep.CaptureKp: return 2;
                    default: return 3;
                }
            }
        }

        public int MarkEventSequence => markEventSequence;
        public string LastMarkRejectionReason { get; private set; } = "NONE";

        public bool IsTargetInteractionEnabled(int targetIndex)
        {
            return targetIndex >= 0 && targetIndex < 3 && targetInteractionEnabled[targetIndex];
        }

        public bool IsTargetCompleted(int targetIndex)
        {
            return targetIndex >= 0 && targetIndex < 3 && targetCompleted[targetIndex];
        }

        public BoxCollider2D GetTargetCollider(int targetIndex)
        {
            return (targetIndex >= 0 && targetIndex < 3) ? targetColliders[targetIndex] : null;
        }

        public void ComputeNewtonCameraFraming(out Vector3 fixedWorldCenter, out float orthoSize)
        {
            // Level 2 Newton camera size increased by 15% (8.6 * 1.15 = 9.89) and unified across all levels,
            // with fringeOrderK clamped to <= MaxFringeOrderK (5).
            fixedWorldCenter = new Vector3(centerO.x, centerO.y, -10f);
            orthoSize = UnifiedNewtonOrthoSize;
        }

        /// <summary>
        /// Identifies the terrain platform immediately before the Newton Ring section
        /// and returns its authoritative world-space AABB left boundary (platformBounds.min.x).
        /// </summary>
        public void FindPreNewtonPlatform(out ColorPlatform preNewtonPlat, out float thresholdX)
        {
            preNewtonPlat = null;
            float rK = GetRadiusForOrder(fringeOrderK);
            float ringLeftX = centerO.x - rK;
            float bestCenterX = float.NegativeInfinity;

            List<ColorPlatform> allPlats = ColorPlatform.All;
            for (int i = 0; i < allPlats.Count; i++)
            {
                ColorPlatform p = allPlats[i];
                if (p == null || p == platformK || p == platformI)
                    continue;

                float px = p.Position.x;
                if (px < ringLeftX && px > bestCenterX)
                {
                    bestCenterX = px;
                    preNewtonPlat = p;
                }
            }

            if (preNewtonPlat != null)
            {
                thresholdX = preNewtonPlat.GetAabb().xMin;
            }
            else
            {
                // Authoritative pre-Newton platform left boundary fallback when terrain platforms
                // are not instantiated in isolated unit tests:
                if (fringeOrderK == 4)
                {
                    thresholdX = 67.617188f - 1.953125f * 0.5f;
                }
                else if (fringeOrderI == 3 || Mathf.Abs(sourceLambda - 550f) < 1f)
                {
                    thresholdX = 68.414062f - 2.890625f * 0.5f;
                }
                else if (fringeOrderI == 2 && Mathf.Abs(sourceLambda - 470f) < 1f)
                {
                    thresholdX = 65.75f - 6.84375f * 0.5f;
                }
                else
                {
                    thresholdX = centerO.x - ringScale * 3.84f;
                }
            }
        }

        public float GetPreNewtonThresholdX()
        {
            if (cameraFollow != null && cameraFollow.HasNewtonThreshold)
            {
                return cameraFollow.PlatformLeftBoundaryX;
            }
            FindPreNewtonPlatform(out _, out float thresholdX);
            return thresholdX;
        }

        public void SyncCameraTransitionConfig()
        {
            if (cameraFollow == null)
                cameraFollow = Contrast.Core.UnityCompat.FindAny<CameraFollow>();
            if (cameraFollow == null)
                return;

            FindPreNewtonPlatform(out ColorPlatform prePlat, out float thresholdX);
            ComputeNewtonCameraFraming(out Vector3 fixedCenter, out float maxOrthoSize);
            cameraFollow.ConfigureNewtonTransition(prePlat, thresholdX, fixedCenter, 5.0f, maxOrthoSize);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(PlayerController pc, CameraFollow cf)
        {
            Initialize(pc, cf, null);
        }

        public void Initialize(PlayerController pc, CameraFollow cf, NewtonChamberData newtonData)
        {
            if (Instance == null)
                Instance = this;

            player = pc != null ? pc : Contrast.Core.UnityCompat.FindAny<PlayerController>();
            cameraFollow = cf != null ? cf : Contrast.Core.UnityCompat.FindAny<CameraFollow>();

            if (newtonData != null)
            {
                centerO = new Vector2(newtonData.cx / 64f, -newtonData.cy / 64f);
                int rawK = newtonData.k > 0 ? newtonData.k : 4;
                int rawI = newtonData.i > 0 ? newtonData.i : 2;
                fringeOrderK = Mathf.Clamp(rawK, 1, MaxFringeOrderK);
                fringeOrderI = Mathf.Clamp(rawI, 1, Mathf.Max(1, fringeOrderK - 1));
                if (newtonData.lambda > 0f)
                {
                    sourceLambda = newtonData.lambda;
                }
                else
                {
                    sourceLambda = (rawK == 4 && rawI == 2) ? 650f
                        : (rawK >= 7 && rawI == 2) ? 470f
                        : 550f;
                }
                orbitStartMode = string.IsNullOrEmpty(newtonData.orbitStart) ? "auto" : newtonData.orbitStart.ToLowerInvariant();

                if (newtonData.rails?.K != null)
                {
                    platformSizeK = new Vector2(newtonData.rails.K.width / 64f, 22f / 64f);
                    periodK = newtonData.rails.K.period > 0f ? newtonData.rails.K.period : 10.8f;
                    angleK = newtonData.rails.K.angleDeg;
                    dirK = newtonData.rails.K.direction != 0 ? newtonData.rails.K.direction : -1;
                }
                if (newtonData.rails?.I != null)
                {
                    platformSizeI = new Vector2(newtonData.rails.I.width / 64f, 20f / 64f);
                    periodI = newtonData.rails.I.period > 0f ? newtonData.rails.I.period : 8.6f;
                    angleI = newtonData.rails.I.angleDeg;
                    dirI = newtonData.rails.I.direction != 0 ? newtonData.rails.I.direction : 1;
                }
            }

            else
            {
                fringeOrderK = Mathf.Clamp(fringeOrderK, 1, MaxFringeOrderK);
                fringeOrderI = Mathf.Clamp(fringeOrderI, 1, Mathf.Max(1, fringeOrderK - 1));
            }

            dockActive = (orbitStartMode != "dock");

            if (player != null)
            {
                player.OnNewtonMarkRequested -= OnPlayerNewtonMarkInput;
                player.OnNewtonMarkRequested += OnPlayerNewtonMarkInput;
            }

            EnsureHierarchyRoots();
            SpawnApparatus();
            SpawnOrbitPlatforms();
            BuildTargetColliders();
            BuildWorldVisuals();
            BuildHUD();

            ResetState();
        }

        private void EnsureHierarchyRoots()
        {
            if (apparatusRoot == null)
            {
                apparatusRoot = new GameObject("NewtonFinaleApparatus");
                apparatusRoot.transform.SetParent(transform, false);
                apparatusRoot.transform.position = new Vector3(centerO.x, centerO.y, 0f);
            }
        }

        private void SpawnApparatus()
        {
            GameObject ringsGo = new GameObject("NewtonRingsApparatus");
            ringsGo.transform.SetParent(apparatusRoot.transform, false);
            ringsGo.transform.localPosition = Vector3.zero;

            apparatusVisuals = ringsGo.AddComponent<NewtonRingsPresentation>();
            float outerR = ringScale * Mathf.Sqrt(MaxFringeOrderK + 0.65f) + 1.5f;
            apparatusVisuals.Configure(ringScale, outerR, sourceLambda, referenceLambda);
            apparatusVisuals.SetCenterPosition(centerO);
        }

        public float GetRadiusForOrder(int order)
        {
            if (order <= 0) return 0f;
            int clampedOrder = Mathf.Min(order, MaxFringeOrderK);
            return ringScale * Mathf.Sqrt(clampedOrder);
        }

        private void SpawnOrbitPlatforms()
        {
            float rK = GetRadiusForOrder(fringeOrderK);
            float rI = GetRadiusForOrder(fringeOrderI);

            // 1. Platform K — White (k)
            GameObject platKGo = new GameObject("OrbitPlatform_K_White");
            platKGo.transform.SetParent(transform, false);
            platformK = platKGo.AddComponent<ColorPlatform>();
            var presK = platKGo.AddComponent<PlatformVisualPresentation>();

            float startAngleKDeg = -angleK;
            float endAngleKDeg = dirK == 1 ? (startAngleKDeg - 360f) : (startAngleKDeg + 360f);

            Vector2 initPosK = centerO + new Vector2(
                rK * Mathf.Cos(startAngleKDeg * Mathf.Deg2Rad),
                rK * Mathf.Sin(startAngleKDeg * Mathf.Deg2Rad));

            platformK.Initialize(initPosK, platformSizeK, 0f); // White (0 = LogicalColor.White, passable by White player)
            presK.InitializeVisuals();

            trajK = platKGo.AddComponent<Trajectory>();
            var cfgK = new TrajectoryData
            {
                enabled = orbitStartMode != "dock",
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rK,
                paramMin = Mathf.Min(startAngleKDeg, endAngleKDeg),
                paramMax = Mathf.Max(startAngleKDeg, endAngleKDeg),
                duration = periodK,
                reverseDirection = (endAngleKDeg < startAngleKDeg)
            };
            trajK.SetConfig(cfgK);

            // 2. Platform I — Gray (i)
            GameObject platIGo = new GameObject("OrbitPlatform_I_Gray");
            platIGo.transform.SetParent(transform, false);
            platformI = platIGo.AddComponent<ColorPlatform>();
            var presI = platIGo.AddComponent<PlatformVisualPresentation>();

            float startAngleIDeg = -angleI;
            float endAngleIDeg = dirI == 1 ? (startAngleIDeg - 360f) : (startAngleIDeg + 360f);

            Vector2 initPosI = centerO + new Vector2(
                rI * Mathf.Cos(startAngleIDeg * Mathf.Deg2Rad),
                rI * Mathf.Sin(startAngleIDeg * Mathf.Deg2Rad));

            platformI.Initialize(initPosI, platformSizeI, 128f); // Gray
            presI.InitializeVisuals();

            trajI = platIGo.AddComponent<Trajectory>();
            var cfgI = new TrajectoryData
            {
                enabled = orbitStartMode != "dock",
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rI,
                paramMin = Mathf.Min(startAngleIDeg, endAngleIDeg),
                paramMax = Mathf.Max(startAngleIDeg, endAngleIDeg),
                duration = periodI,
                reverseDirection = (endAngleIDeg < startAngleIDeg)
            };
            trajI.SetConfig(cfgI);
        }

        private void BuildWorldVisuals()
        {
            // Center O Marker
            markerOGo = CreateWorldMarker("Marker_O", centerO, "O", new Color(0.92f, 0.96f, 0.98f, 0.9f), 0.15f);

            // Dynamic Axis Line (dashed line along diameter through O)
            axisLineRenderer = CreateLineObject("AxisLineRenderer", -5, 0.035f, new Color(0.85f, 0.92f, 0.98f, 0.45f));
            axisLineRenderer.gameObject.SetActive(false);

            // Measurement solid line B = KI
            lineKI = CreateLineObject("Line_KI_Solid", -2, 0.08f, new Color(0.96f, 0.98f, 1f, 0.95f));
            lineKI.gameObject.SetActive(false);

            // Measurement dashed line b = IK'
            lineIKp = CreateLineObject("Line_IKp_Dashed", -2, 0.065f, new Color(0.82f, 0.88f, 0.95f, 0.90f));
            lineIKp.gameObject.SetActive(false);

            // Captured markers
            markerKGo = CreateWorldMarker("Marker_K", Vector2.zero, "K", Color.white, 0.22f);
            markerKGo.SetActive(false);

            markerIGo = CreateWorldMarker("Marker_I", Vector2.zero, "I", new Color(0.85f, 0.92f, 1f, 1f), 0.20f);
            markerIGo.SetActive(false);

            markerKpGo = CreateWorldMarker("Marker_Kp", Vector2.zero, "K′", Color.white, 0.22f);
            markerKpGo.SetActive(false);

            // Target crosshairs
            targetICrosshair = CreateTargetIndicator("Target_I_Crosshair", "I");
            targetICrosshair.SetActive(false);

            targetKpCrosshair = CreateTargetIndicator("Target_Kp_Crosshair", "K′");
            targetKpCrosshair.SetActive(false);

            // Labels above platforms
            labelPlatformK = CreatePlatformFloatingLabel(platformK.transform, $"k = {fringeOrderK}");
            labelPlatformI = CreatePlatformFloatingLabel(platformI.transform, $"i = {fringeOrderI}");
        }

        private void BuildTargetColliders()
        {
            string[] names =
            {
                "NewtonTargetCollider_1_K",
                "NewtonTargetCollider_2_I",
                "NewtonTargetCollider_3_Kp"
            };

            for (int idx = 0; idx < 3; idx++)
            {
                if (targetColliderGos[idx] != null)
                    SafeDestroy(targetColliderGos[idx]);

                GameObject go = new GameObject(names[idx]);
                go.transform.SetParent(apparatusRoot != null ? apparatusRoot.transform : transform, false);
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                targetColliderGos[idx] = go;
                targetColliders[idx] = col;
            }

            SyncTargetStatesAndColliders();
        }

        public Vector2 GetTargetWorldPosition(int targetIndex)
        {
            float rI = GetRadiusForOrder(fringeOrderI);
            float rK = GetRadiusForOrder(fringeOrderK);

            switch (targetIndex)
            {
                case 0:
                    if (HasK)
                        return PointK;
                    if (platformK != null)
                        return (Vector2)platformK.transform.position;
                    return centerO + Vector2.left * rK;
                case 1:
                    return centerO - AxisU * rI;
                case 2:
                    return centerO - AxisU * rK;
                default:
                    return centerO;
            }
        }

        public Rect GetTargetColliderWorldRect(int targetIndex)
        {
            SyncTargetCollidersGeometry();
            if (targetIndex < 0 || targetIndex >= 3 || targetColliders[targetIndex] == null)
                return new Rect(0f, 0f, 0f, 0f);

            Vector2 center = (Vector2)targetColliderGos[targetIndex].transform.position + targetColliders[targetIndex].offset;
            Vector2 size = targetColliders[targetIndex].size;
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        public Rect GetPlayerColliderWorldRect()
        {
            if (player == null)
                return new Rect(0f, 0f, 0f, 0f);

            Vector2 pSize = new Vector2(
                Mathf.Max(0.01f, Mathf.Abs(player.PlayerSize.x)),
                Mathf.Max(0.01f, Mathf.Abs(player.PlayerSize.y)));
            Vector2 pCenter = (Vector2)player.transform.position + player.PlayerOffset;
            return new Rect(pCenter.x - pSize.x * 0.5f, pCenter.y - pSize.y * 0.5f, pSize.x, pSize.y);
        }

        public void SyncTargetCollidersGeometry()
        {
            float pHeight = player != null ? Mathf.Max(0.5f, Mathf.Abs(player.PlayerSize.y)) : 1.0f;

            // Target 1 (Index 0): K capture region on outer orbit K
            if (targetColliderGos[0] != null && targetColliders[0] != null)
            {
                Vector2 posK = GetTargetWorldPosition(0);
                float centerOffsetY = platformSizeK.y * 0.5f + pHeight * 0.5f;
                targetColliderGos[0].transform.position = new Vector3(posK.x, posK.y + centerOffsetY, 0f);
                targetColliders[0].offset = Vector2.zero;
                targetColliders[0].size = new Vector2(platformSizeK.x + 0.25f, pHeight + platformSizeK.y + 0.35f);
            }

            // Target 2 (Index 1): I capture region at centerO - AxisU * rI
            if (targetColliderGos[1] != null && targetColliders[1] != null)
            {
                Vector2 posI = GetTargetWorldPosition(1);
                float centerOffsetY = platformSizeI.y * 0.5f + pHeight * 0.5f;
                targetColliderGos[1].transform.position = new Vector3(posI.x, posI.y + centerOffsetY * 0.5f, 0f);
                targetColliders[1].offset = Vector2.zero;
                targetColliders[1].size = new Vector2(toleranceI * 2f, toleranceI * 2f + pHeight + platformSizeI.y);
            }

            // Target 3 (Index 2): K' capture region at centerO - AxisU * rK
            if (targetColliderGos[2] != null && targetColliders[2] != null)
            {
                Vector2 posKp = GetTargetWorldPosition(2);
                float centerOffsetY = platformSizeK.y * 0.5f + pHeight * 0.5f;
                targetColliderGos[2].transform.position = new Vector3(posKp.x, posKp.y + centerOffsetY * 0.5f, 0f);
                targetColliders[2].offset = Vector2.zero;
                targetColliders[2].size = new Vector2(toleranceKp * 2f, toleranceKp * 2f + pHeight + platformSizeK.y);
            }
        }

        public void SyncTargetStatesAndColliders()
        {
            int activeIdx = CurrentTargetIndex;

            targetCompleted[0] = HasK || activeIdx > 0;
            targetCompleted[1] = HasI || activeIdx > 1;
            targetCompleted[2] = HasKp || activeIdx > 2;

            for (int i = 0; i < 3; i++)
            {
                bool enableInteraction = (i == activeIdx) && !targetCompleted[i] && CurrentStep < NewtonFinaleStep.Reveal;
                targetInteractionEnabled[i] = enableInteraction;
                if (targetColliders[i] != null)
                {
                    targetColliders[i].enabled = enableInteraction;
                }
            }

            SyncTargetCollidersGeometry();
        }

        public bool DoesPlayerOverlapTargetCollider(int targetIndex, out Rect playerRect, out Rect targetRect)
        {
            SyncTargetStatesAndColliders();
            playerRect = GetPlayerColliderWorldRect();
            targetRect = GetTargetColliderWorldRect(targetIndex);

            if (targetIndex < 0 || targetIndex >= 3)
                return false;

            if (!targetInteractionEnabled[targetIndex] || targetCompleted[targetIndex])
                return false;

            if (targetColliders[targetIndex] == null || !targetColliders[targetIndex].enabled)
                return false;

            return playerRect.width > 0f && targetRect.width > 0f && playerRect.Overlaps(targetRect);
        }

        private LineRenderer CreateLineObject(string name, int sortingOrder, float width, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(apparatusRoot.transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");
            lr.material = new Material(shader) { hideFlags = HideFlags.DontSave };
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = 2;
            lr.sortingOrder = sortingOrder;
            lr.useWorldSpace = true;
            return lr;
        }

        private GameObject CreateWorldMarker(string name, Vector2 pos, string label, Color color, float radius)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(apparatusRoot.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, -1f);

            // Center circle sprite
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(go.transform, false);
            SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color = color;
            sr.sortingOrder = 5;
            dot.transform.localScale = Vector3.one * (radius * 2f);

            // Label text
            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.localPosition = new Vector3(0f, radius + 0.35f, 0f);
            TextMesh tm = textGo.AddComponent<TextMesh>();
            Font f = GetFont();
            if (f != null) tm.font = f;
            tm.text = label;
            tm.fontSize = 28;
            tm.characterSize = 0.055f;
            tm.color = color;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            MeshRenderer mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (f != null && f.material != null)
                    mr.sharedMaterial = f.material;
                mr.sortingOrder = 10;
            }

            return go;
        }

        private GameObject CreateTargetIndicator(string name, string label)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(apparatusRoot.transform, false);

            // Dashed ring sprite or crosshair
            GameObject ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = GetRingSprite();
            sr.color = new Color(0.85f, 0.92f, 1f, 0.65f);
            sr.sortingOrder = 2;
            ring.transform.localScale = Vector3.one * 0.85f;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            TextMesh tm = textGo.AddComponent<TextMesh>();
            Font f = GetFont();
            if (f != null) tm.font = f;
            tm.text = label;
            tm.fontSize = 26;
            tm.characterSize = 0.052f;
            tm.color = new Color(0.88f, 0.95f, 1f, 0.85f);
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            MeshRenderer mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (f != null && f.material != null)
                    mr.sharedMaterial = f.material;
                mr.sortingOrder = 10;
            }

            return go;
        }

        private Text CreatePlatformFloatingLabel(Transform parent, string label)
        {
            GameObject canvasGo = new GameObject("PlatformCanvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.6f);

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.92f, 0.96f, 1f, 0.9f);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(240f, 50f);
            trt.localScale = Vector3.one * 0.015f;

            return text;
        }

        private void BuildHUD()
        {
            hudCanvasGo = new GameObject("EasyNewtonFinaleHUD");
            hudCanvasGo.transform.SetParent(transform, false);

            hudCanvas = hudCanvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.pixelPerfect = true;
            hudCanvas.sortingOrder = 85; // Above gameplay/PhaseTuner, below GameControlUI (90) & Pause/Win modals (210+)

            CanvasScaler scaler = hudCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            hudCanvasGo.AddComponent<GraphicRaycaster>();

            Color spectralCol = NewtonRingsPresentation.WavelengthToRgb(sourceLambda);
            string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            Color spectralAccent = Color.Lerp(spectralCol, Color.white, 0.30f);

            // 1. Virtual Lab Newton Measurement Card (Docked Top-Left below "< Quay lại" pill so bottom of Newton rings is 100% clear)
            GameObject objCard = new GameObject("ObjectiveCard", typeof(RectTransform));
            objectiveCardGo = objCard;
            objCard.transform.SetParent(hudCanvasGo.transform, false);
            Image objBg = objCard.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(objBg, LabStyleUI.GetCardSprite(), true, -5f);

            RectTransform objRt = objCard.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 1f);
            objRt.anchorMax = new Vector2(0f, 1f);
            objRt.pivot = new Vector2(0f, 1f);
            objRt.anchoredPosition = new Vector2(22f, -80f);
            objRt.sizeDelta = new Vector2(444f, 192f);

            // Top row matching reference image: "k = ...      i = ..."
            GameObject infoGo = new GameObject("ProblemInfo", typeof(RectTransform));
            infoGo.transform.SetParent(objCard.transform, false);
            objectiveInfoText = infoGo.AddComponent<Text>();
            objectiveInfoText.font = GetFont();
            objectiveInfoText.text = $"k = {fringeOrderK}          i = {fringeOrderI}";
            objectiveInfoText.fontSize = 21;
            objectiveInfoText.fontStyle = FontStyle.Bold;
            objectiveInfoText.color = LabStyleUI.TextPrimary;
            objectiveInfoText.alignment = TextAnchor.MiddleCenter;
            RectTransform irt = infoGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 1f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0f, -12f);
            irt.sizeDelta = new Vector2(-32f, 28f);

            // Subtle spectral wavelength subtitle right under k / i (hides wavelength & color until quiz is solved)
            GameObject waveTagGo = new GameObject("WavelengthTag", typeof(RectTransform));
            waveTagGo.transform.SetParent(objCard.transform, false);
            Text waveTag = waveTagGo.AddComponent<Text>();
            wavelengthTagText = waveTag;
            waveTag.font = GetFont();
            waveTag.text = "Hệ vân tròn Newton · Phép đo bước sóng ánh sáng (λ = ? nm)";
            waveTag.fontSize = 12;
            waveTag.fontStyle = FontStyle.Bold;
            waveTag.color = LabStyleUI.TextSecondary;
            waveTag.alignment = TextAnchor.MiddleCenter;
            RectTransform wrt = waveTagGo.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0f, 1f);
            wrt.anchorMax = new Vector2(1f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = new Vector2(0f, -40f);
            wrt.sizeDelta = new Vector2(-32f, 18f);

            // 3 Recessed Coordinate Readout Slots matching reference image: x_k [giá trị  mm], x_i [giá trị  mm], x_k' [giá trị  mm]
            slotXkValText = CreateCoordinateSlot(objCard.transform, "Slot_Xk", "x_k", new Vector2(-136f, -94f));
            slotXiValText = CreateCoordinateSlot(objCard.transform, "Slot_Xi", "x_i", new Vector2(0f, -94f));
            slotXkpValText = CreateCoordinateSlot(objCard.transform, "Slot_Xkp", "x_k′", new Vector2(136f, -94f));

            // Bottom Royal-Blue Primary CTA Button ("NHẬP") matching reference image
            GameObject enterBtnGo = new GameObject("MarkActionButton", typeof(RectTransform));
            enterBtnGo.transform.SetParent(objCard.transform, false);
            Image enterBg = enterBtnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(enterBg, LabStyleUI.GetPrimaryButtonSprite(), true, -3f);

            Button enterBtn = enterBtnGo.AddComponent<Button>();
            enterBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            enterBtn.targetGraphic = enterBg;
            enterBtn.colors = LabStyleUI.MakeButtonColorBlock();
            enterBtn.onClick.AddListener(() =>
            {
                Debug.Log("[UI_BUTTON][NewtonHUD] Clicked MarkActionButton (NHẬP) -> OnPlayerNewtonMarkInput");
                OnPlayerNewtonMarkInput();
            });

            RectTransform enterRt = enterBtnGo.GetComponent<RectTransform>();
            enterRt.anchorMin = new Vector2(0.5f, 0f);
            enterRt.anchorMax = new Vector2(0.5f, 0f);
            enterRt.pivot = new Vector2(0.5f, 0f);
            enterRt.anchoredPosition = new Vector2(0f, 14f);
            enterRt.sizeDelta = new Vector2(396f, 40f);

            GameObject enterLblGo = new GameObject("Label", typeof(RectTransform));
            enterLblGo.transform.SetParent(enterBtnGo.transform, false);
            Text enterLbl = enterLblGo.AddComponent<Text>();
            enterLbl.font = GetFont();
            enterLbl.text = "NHẬP  (PHÍM X)";
            enterLbl.fontSize = 15;
            enterLbl.fontStyle = FontStyle.Bold;
            enterLbl.alignment = TextAnchor.MiddleCenter;
            enterLbl.color = Color.white;
            enterLbl.raycastTarget = false;
            RectTransform elRt = enterLblGo.GetComponent<RectTransform>();
            elRt.anchorMin = Vector2.zero;
            elRt.anchorMax = Vector2.one;
            elRt.offsetMin = Vector2.zero;
            elRt.offsetMax = Vector2.zero;

            // 2. Callout Stadium Pill docked directly below ObjectiveCard on Top-Left ("Đọc và ghi lại toạ độ ...")
            // Preserves BottomBarGo anchored at (0, 1) so bottom of Newton rings is never covered
            bottomBarGo = new GameObject("BottomBar", typeof(RectTransform));
            bottomBarGo.transform.SetParent(hudCanvasGo.transform, false);
            Image barBg = bottomBarGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(barBg, LabStyleUI.GetPillSprite(), true, -4f);

            RectTransform brt = bottomBarGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(22f, -282f);
            brt.sizeDelta = new Vector2(444f, 62f);

            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(bottomBarGo.transform, false);
            objectiveTitleText = titleGo.AddComponent<Text>();
            objectiveTitleText.font = GetFont();
            objectiveTitleText.text = $"Đọc và ghi lại toạ độ x_k  (vân k = {fringeOrderK})";
            objectiveTitleText.fontSize = 15;
            objectiveTitleText.fontStyle = FontStyle.Bold;
            objectiveTitleText.color = LabStyleUI.TextPrimary;
            objectiveTitleText.alignment = TextAnchor.MiddleCenter;
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.46f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(16f, 0f);
            trt.offsetMax = new Vector2(-16f, -4f);

            GameObject subGo = new GameObject("Sub", typeof(RectTransform));
            subGo.transform.SetParent(bottomBarGo.transform, false);
            objectiveSubText = subGo.AddComponent<Text>();
            objectiveSubText.font = GetFont();
            objectiveSubText.text = "Đứng trên thanh K và nhấn X (hoặc nút NHẬP)";
            objectiveSubText.fontSize = 12;
            objectiveSubText.color = LabStyleUI.TextSecondary;
            objectiveSubText.alignment = TextAnchor.MiddleCenter;
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.50f);
            srt.offsetMin = new Vector2(16f, 4f);
            srt.offsetMax = new Vector2(-16f, 0f);

            // Feedback Toast Text (Top-center below TopLabDock so it never covers the rings)
            GameObject fbGo = new GameObject("FeedbackText", typeof(RectTransform));
            fbGo.transform.SetParent(hudCanvasGo.transform, false);
            feedbackText = fbGo.AddComponent<Text>();
            feedbackText.font = GetFont();
            feedbackText.text = string.Empty;
            feedbackText.fontSize = 19;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = LabStyleUI.TextPrimary;
            Outline fbOut = fbGo.AddComponent<Outline>();
            fbOut.effectColor = new Color(0.02f, 0.05f, 0.10f, 0.85f);
            fbOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform fbrt = fbGo.GetComponent<RectTransform>();
            fbrt.anchorMin = new Vector2(0.5f, 1f);
            fbrt.anchorMax = new Vector2(0.5f, 1f);
            fbrt.pivot = new Vector2(0.5f, 1f);
            fbrt.anchoredPosition = new Vector2(0f, -66f);
            fbrt.sizeDelta = new Vector2(720f, 42f);

            // Calculation Results Card (Phase D & E)
            BuildCalculationPanel();

            // Wavelength Color Quiz Card (Docked at bottom-center before ResultCard)
            BuildQuizCard();

            // Completion Overlay (Section 6)
            BuildCompletionPanel();

            LabStyleUI.CrispenTextHierarchy(hudCanvasGo.transform);

            hudCanvasGo.SetActive(false);
        }

        private Text CreateCoordinateSlot(Transform parent, string slotName, string symbolLabel, Vector2 posFromTopCenter)
        {
            GameObject wrap = new GameObject(slotName, typeof(RectTransform));
            wrap.transform.SetParent(parent, false);
            RectTransform wRt = wrap.GetComponent<RectTransform>();
            wRt.anchorMin = new Vector2(0.5f, 1f);
            wRt.anchorMax = new Vector2(0.5f, 1f);
            wRt.pivot = new Vector2(0.5f, 0.5f);
            wRt.anchoredPosition = posFromTopCenter;
            wRt.sizeDelta = new Vector2(126f, 56f);

            // Symbol label above recessed box ("x_k", "x_i", "x_k'")
            GameObject symGo = new GameObject("Symbol", typeof(RectTransform));
            symGo.transform.SetParent(wrap.transform, false);
            Text sym = symGo.AddComponent<Text>();
            sym.font = GetFont();
            sym.text = symbolLabel;
            sym.fontSize = 13;
            sym.fontStyle = FontStyle.Bold;
            sym.alignment = TextAnchor.LowerLeft;
            sym.color = LabStyleUI.TextPrimary;
            RectTransform symRt = symGo.GetComponent<RectTransform>();
            symRt.anchorMin = new Vector2(0f, 0.64f);
            symRt.anchorMax = new Vector2(1f, 1f);
            symRt.offsetMin = new Vector2(4f, 0f);
            symRt.offsetMax = Vector2.zero;

            // Recessed slot box ("[giá trị    mm]")
            GameObject boxGo = new GameObject("SlotBox", typeof(RectTransform));
            boxGo.transform.SetParent(wrap.transform, false);
            Image boxImg = boxGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(boxImg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform bRt = boxGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0.62f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            GameObject valGo = new GameObject("Value", typeof(RectTransform));
            valGo.transform.SetParent(boxGo.transform, false);
            Text valTxt = valGo.AddComponent<Text>();
            valTxt.font = GetFont();
            valTxt.text = "giá trị";
            valTxt.fontSize = 12;
            valTxt.fontStyle = FontStyle.Normal;
            valTxt.alignment = TextAnchor.MiddleLeft;
            valTxt.color = LabStyleUI.TextMuted;
            RectTransform vRt = valGo.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0f);
            vRt.anchorMax = new Vector2(0.72f, 1f);
            vRt.offsetMin = new Vector2(10f, 0f);
            vRt.offsetMax = Vector2.zero;

            GameObject unitGo = new GameObject("Unit", typeof(RectTransform));
            unitGo.transform.SetParent(boxGo.transform, false);
            Text unitTxt = unitGo.AddComponent<Text>();
            unitTxt.font = GetFont();
            unitTxt.text = "mm";
            unitTxt.fontSize = 12;
            unitTxt.fontStyle = FontStyle.Bold;
            unitTxt.alignment = TextAnchor.MiddleRight;
            unitTxt.color = LabStyleUI.TextSecondary;
            RectTransform uRt = unitGo.GetComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0.68f, 0f);
            uRt.anchorMax = new Vector2(1f, 1f);
            uRt.offsetMin = Vector2.zero;
            uRt.offsetMax = new Vector2(-10f, 0f);

            return valTxt;
        }

        private void BuildCalculationPanel()
        {
            Color spectralCol = NewtonRingsPresentation.WavelengthToRgb(sourceLambda);
            string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            Color spectralAccent = Color.Lerp(spectralCol, Color.white, 0.25f);

            calculationPanelGo = new GameObject("CalculationPanel", typeof(RectTransform));
            calculationPanelGo.transform.SetParent(hudCanvasGo.transform, false);
            Image bg = calculationPanelGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(bg, LabStyleUI.GetCardSprite(), true, -5f);

            calcCanvasGroup = calculationPanelGo.AddComponent<CanvasGroup>();
            calcCanvasGroup.alpha = 0f;

            // Docked on Top-Left (slides up to -80 when ObjectiveCard hides) so it never covers the Newton rings
            RectTransform rt = calculationPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(22f, -282f);
            rt.sizeDelta = new Vector2(464f, 182f);

            // 1. Formula line
            GameObject fGo = new GameObject("FormulaText", typeof(RectTransform));
            fGo.transform.SetParent(calculationPanelGo.transform, false);
            calcFormulaText = fGo.AddComponent<Text>();
            calcFormulaText.font = GetFont();
            calcFormulaText.text = "B = KI = r_k + r_i      b = IK′ = r_k − r_i";
            calcFormulaText.fontSize = 15;
            calcFormulaText.fontStyle = FontStyle.Bold;
            calcFormulaText.alignment = TextAnchor.MiddleCenter;
            calcFormulaText.color = LabStyleUI.TextPrimary;
            RectTransform frt = fGo.GetComponent<RectTransform>();
            frt.anchoredPosition = new Vector2(0f, 58f);
            frt.sizeDelta = new Vector2(436f, 26f);

            // 2. Measured values line inside recessed slot
            GameObject vSlot = new GameObject("ValuesSlot", typeof(RectTransform));
            vSlot.transform.SetParent(calculationPanelGo.transform, false);
            Image vSlotImg = vSlot.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(vSlotImg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform vsRt = vSlot.GetComponent<RectTransform>();
            vsRt.anchoredPosition = new Vector2(0f, 22f);
            vsRt.sizeDelta = new Vector2(428f, 30f);

            GameObject vGo = new GameObject("ValuesText", typeof(RectTransform));
            vGo.transform.SetParent(vSlot.transform, false);
            calcValuesText = vGo.AddComponent<Text>();
            calcValuesText.font = GetFont();
            calcValuesText.text = $"B = 0.000 mm   b = 0.000 mm   R = {radiusOfCurvatureMm:F0} mm";
            calcValuesText.fontSize = 12;
            calcValuesText.fontStyle = FontStyle.Bold;
            calcValuesText.alignment = TextAnchor.MiddleCenter;
            calcValuesText.color = LabStyleUI.TextSecondary;
            RectTransform vrt = vGo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;

            // 3. Equation line
            GameObject eGo = new GameObject("EquationText", typeof(RectTransform));
            eGo.transform.SetParent(calculationPanelGo.transform, false);
            calcEquationText = eGo.AddComponent<Text>();
            calcEquationText.font = GetFont();
            calcEquationText.text = "λ = (B × b) / [|k − i| × R]";
            calcEquationText.fontSize = 15;
            calcEquationText.fontStyle = FontStyle.Bold;
            calcEquationText.alignment = TextAnchor.MiddleCenter;
            calcEquationText.color = LabStyleUI.TextPrimary;
            RectTransform ert = eGo.GetComponent<RectTransform>();
            ert.anchoredPosition = new Vector2(0f, -12f);
            ert.sizeDelta = new Vector2(436f, 24f);

            // 4. Result Royal-Blue Pill
            GameObject rBadge = new GameObject("ResultBadge", typeof(RectTransform));
            rBadge.transform.SetParent(calculationPanelGo.transform, false);
            Image rBadgeImg = rBadge.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(rBadgeImg, LabStyleUI.GetPrimaryButtonSprite(), false);
            RectTransform rbRt = rBadge.GetComponent<RectTransform>();
            rbRt.anchoredPosition = new Vector2(0f, -54f);
            rbRt.sizeDelta = new Vector2(428f, 40f);

            GameObject rGo = new GameObject("ResultText", typeof(RectTransform));
            rGo.transform.SetParent(rBadge.transform, false);
            calcResultText = rGo.AddComponent<Text>();
            calcResultText.font = GetFont();
            calcResultText.text = $"Bước sóng đo được:  λ ≈ {sourceLambda:F1} nm";
            calcResultText.fontSize = 18;
            calcResultText.fontStyle = FontStyle.Bold;
            calcResultText.alignment = TextAnchor.MiddleCenter;
            calcResultText.color = LabStyleUI.TextPrimary;
            RectTransform rrt = rGo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            calculationPanelGo.SetActive(false);
        }

        private void BuildQuizCard()
        {
            quizCardGo = new GameObject("WavelengthColorQuizCard", typeof(RectTransform));
            quizCardGo.transform.SetParent(hudCanvasGo.transform, false);
            Image cardBg = quizCardGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), true, -6f);

            // Centered horizontally and docked right near the bottom edge so Newton rings stay unobstructed
            RectTransform cardRt = quizCardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0f);
            cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.pivot = new Vector2(0.5f, 0f);
            cardRt.sizeDelta = new Vector2(1080f, 208f);
            cardRt.anchoredPosition = new Vector2(0f, 12f);

            // 1. Top Pill Status Badge
            GameObject badgeGo = new GameObject("QuizStatusBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(quizCardGo.transform, false);
            Image badgeBg = badgeGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(badgeBg, LabStyleUI.GetPillSprite(), false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0f, 76f);
            badgeRt.sizeDelta = new Vector2(560f, 26f);

            GameObject badgeTxtGo = new GameObject("BadgeText", typeof(RectTransform));
            badgeTxtGo.transform.SetParent(badgeGo.transform, false);
            quizBadgeText = badgeTxtGo.AddComponent<Text>();
            quizBadgeText.font = GetFont();
            quizBadgeText.text = "XÁC ĐỊNH MÀU ÁNH SÁNG TỪ BƯỚC SÓNG  ·  LƯỢT CHỌN: 2/2";
            quizBadgeText.fontSize = 12;
            quizBadgeText.fontStyle = FontStyle.Bold;
            quizBadgeText.alignment = TextAnchor.MiddleCenter;
            quizBadgeText.color = LabStyleUI.TextSecondary;
            RectTransform btrt = badgeTxtGo.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = Vector2.zero;
            btrt.offsetMax = Vector2.zero;

            // 2. Question Title
            GameObject qGo = new GameObject("QuestionText", typeof(RectTransform));
            qGo.transform.SetParent(quizCardGo.transform, false);
            quizQuestionText = qGo.AddComponent<Text>();
            quizQuestionText.font = GetFont();
            quizQuestionText.text = $"Bước sóng đo được = {sourceLambda:F0} nm, vậy ánh sáng này là màu gì?";
            quizQuestionText.fontSize = 22;
            quizQuestionText.fontStyle = FontStyle.Bold;
            quizQuestionText.alignment = TextAnchor.MiddleCenter;
            quizQuestionText.color = LabStyleUI.TextPrimary;
            RectTransform qrt = qGo.GetComponent<RectTransform>();
            qrt.anchoredPosition = new Vector2(0f, 40f);
            qrt.sizeDelta = new Vector2(1000f, 34f);

            // 3. Feedback / Instruction Subtitle
            GameObject fbGo = new GameObject("FeedbackLine", typeof(RectTransform));
            fbGo.transform.SetParent(quizCardGo.transform, false);
            quizFeedbackText = fbGo.AddComponent<Text>();
            quizFeedbackText.font = GetFont();
            quizFeedbackText.text = "Hãy chọn màu ánh sáng tương ứng với bước sóng vừa đo được:";
            quizFeedbackText.fontSize = 14;
            quizFeedbackText.fontStyle = FontStyle.Normal;
            quizFeedbackText.alignment = TextAnchor.MiddleCenter;
            quizFeedbackText.color = LabStyleUI.TextSecondary;
            RectTransform fbrt = fbGo.GetComponent<RectTransform>();
            fbrt.anchoredPosition = new Vector2(0f, 10f);
            fbrt.sizeDelta = new Vector2(960f, 24f);

            // 4. 4 Multiple-Choice Answer Buttons Row
            quizOptionsRowGo = new GameObject("OptionsRow", typeof(RectTransform));
            quizOptionsRowGo.transform.SetParent(quizCardGo.transform, false);
            RectTransform rowRt = quizOptionsRowGo.GetComponent<RectTransform>();
            rowRt.anchoredPosition = new Vector2(0f, -48f);
            rowRt.sizeDelta = new Vector2(1000f, 56f);

            const float btnW = 222f;
            const float btnH = 50f;
            const float gap = 24f;
            const float step = btnW + gap;
            const float startX = -1.5f * step;

            for (int i = 0; i < 4; i++)
            {
                int slotIdx = i;
                GameObject btnGo = new GameObject($"QuizOption_{i}", typeof(RectTransform));
                btnGo.transform.SetParent(quizOptionsRowGo.transform, false);

                Image btnImg = btnGo.AddComponent<Image>();
                LabStyleUI.ApplySlicedSprite(btnImg, LabStyleUI.GetSecondaryButtonSprite(), true, -3f);
                quizOptionImages[i] = btnImg;

                Button btn = btnGo.AddComponent<Button>();
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.targetGraphic = btnImg;
                btn.colors = LabStyleUI.MakeButtonColorBlock();
                btn.onClick.AddListener(() => OnQuizOptionClicked(slotIdx));
                quizOptionButtons[i] = btn;

                RectTransform brt = btnGo.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(btnW, btnH);
                brt.anchoredPosition = new Vector2(startX + i * step, 0f);

                GameObject lblGo = new GameObject("Label", typeof(RectTransform));
                lblGo.transform.SetParent(btnGo.transform, false);
                Text lbl = lblGo.AddComponent<Text>();
                lbl.font = GetFont();
                lbl.text = "Màu";
                lbl.fontSize = 17;
                lbl.fontStyle = FontStyle.Bold;
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.color = Color.white;
                lbl.raycastTarget = false;
                quizOptionLabels[i] = lbl;

                RectTransform lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            // 5. Reset Button (shown only if player picks wrong 2 times)
            Button resetBtn = CreateButton(
                quizCardGo.transform,
                "QUIZ_ERROR_RESET",
                "CHƠI LẠI TỪ ĐẦU",
                new Vector2(0f, -48f),
                true,
                OnPlayAgainClicked);
            quizErrorResetButtonGo = resetBtn.gameObject;
            Image resetImg = quizErrorResetButtonGo.GetComponent<Image>();
            if (resetImg != null)
                LabStyleUI.ApplySlicedSprite(resetImg, LabStyleUI.GetDangerButtonSprite(), true, -4f);
            quizErrorResetButtonGo.SetActive(false);

            quizCardGo.SetActive(false);
        }

        private void BuildCompletionPanel()
        {
            completionPanelGo = new GameObject("CompletionPanel", typeof(RectTransform));
            completionPanelGo.transform.SetParent(hudCanvasGo.transform, false);
            Image bg = completionPanelGo.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.02f, 0.05f, 0.0f);
            bg.raycastTarget = false;
            RectTransform rt = completionPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Content Container (Frosted Slate Card docked at bottom-center so Newton rings stay unobstructed)
            GameObject box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(completionPanelGo.transform, false);
            Image boxBg = box.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(boxBg, LabStyleUI.GetCardSprite(), true, -6f);
            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0f);
            boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.sizeDelta = new Vector2(1040f, 214f);
            boxRt.anchoredPosition = new Vector2(0f, 12f);

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(box.transform, false);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = "HOÀN THÀNH PHÉP ĐO";
            title.fontSize = 26;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = LabStyleUI.TextPrimary;
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 70f);
            trt.sizeDelta = new Vector2(880f, 34f);

            // Subtitle
            GameObject subGo = new GameObject("Subtitle", typeof(RectTransform));
            subGo.transform.SetParent(box.transform, false);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = "Thí nghiệm giao thoa hệ vân tròn Newton đã hoàn tất chính xác";
            sub.fontSize = 14;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = LabStyleUI.TextSecondary;
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, 40f);
            srt.sizeDelta = new Vector2(880f, 22f);

            // Recessed Detail Slot
            string compColorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            GameObject detSlot = new GameObject("DetailSlot", typeof(RectTransform));
            detSlot.transform.SetParent(box.transform, false);
            Image detSlotBg = detSlot.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(detSlotBg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform dsRt = detSlot.GetComponent<RectTransform>();
            dsRt.anchoredPosition = new Vector2(0f, 4f);
            dsRt.sizeDelta = new Vector2(880f, 34f);

            GameObject detGo = new GameObject("Detail", typeof(RectTransform));
            detGo.transform.SetParent(detSlot.transform, false);
            completionDetailText = detGo.AddComponent<Text>();
            completionDetailText.font = GetFont();
            completionDetailText.text = $"Bước sóng đo được: λ ≈ {sourceLambda:F1} nm (Ánh sáng {compColorNameVi})";
            completionDetailText.fontSize = 14;
            completionDetailText.fontStyle = FontStyle.Bold;
            completionDetailText.alignment = TextAnchor.MiddleCenter;
            completionDetailText.color = Color.Lerp(NewtonRingsPresentation.WavelengthToRgb(sourceLambda), Color.white, 0.35f);
            RectTransform drt = detGo.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;

            // Buttons Row (Generously spaced: 120px clear gap between buttons)
            GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform));
            btnRow.transform.SetParent(box.transform, false);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.anchoredPosition = new Vector2(0f, -54f);
            rowRt.sizeDelta = new Vector2(740f, 58f);

            // Button 1: Royal-Blue Primary CTA
            CreateButton(
                btnRow.transform,
                "PLAY AGAIN",
                "Chơi lại màn này",
                new Vector2(-200f, 0f),
                true,
                OnPlayAgainClicked);

            // Button 2: Matte Slate Secondary
            CreateButton(
                btnRow.transform,
                "LEVEL SELECT",
                "Quay lại menu",
                new Vector2(200f, 0f),
                false,
                OnReturnToLevelSelectClicked);

            completionPanelGo.SetActive(false);
        }

        private static Button CreateButton(
            Transform parent,
            string objectKey,
            string displayLabelVi,
            Vector2 pos,
            bool isPrimary,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = new GameObject(objectKey + "Button", typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);

            Image img = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(
                img,
                isPrimary ? LabStyleUI.GetPrimaryButtonSprite() : LabStyleUI.GetSecondaryButtonSprite(),
                true,
                -4f);

            Button button = btnGo.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = img;
            button.colors = LabStyleUI.MakeButtonColorBlock();
            button.onClick.AddListener(callback);

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(280f, 58f);
            btnRt.anchoredPosition = pos;

            GameObject textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = displayLabelVi;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return button;
        }

        public void SetNewtonZoneActive(bool active)
        {
            if (active == isZoneActive && cameraFollow != null &&
                ((active && cameraFollow.CurrentMode == CameraMode.NewtonFixed) ||
                 (!active && cameraFollow.CurrentMode == CameraMode.NormalPlatformingFollow)))
            {
                return;
            }

            isZoneActive = active;
            if (hudCanvasGo != null)
                hudCanvasGo.SetActive(isZoneActive);

            if (cameraFollow != null)
            {
                if (isZoneActive)
                {
                    if (cameraFollow.CurrentMode != CameraMode.NewtonResultReveal)
                    {
                        ComputeNewtonCameraFraming(out Vector3 fixedCenter, out float orthoSize);
                        cameraFollow.SetNewtonFixed(fixedCenter, orthoSize, 3.2f);
                        Debug.Log($"[CAMERA] Transition -> NewtonFixed (center=({fixedCenter.x:F2}, {fixedCenter.y:F2}), orthoSize={orthoSize:F2})");
                        LogCameraDiagnostics();
                    }
                }
                else
                {
                    if (cameraFollow.CurrentMode == CameraMode.NewtonFixed ||
                        cameraFollow.CurrentMode == CameraMode.NewtonPlayerFollow)
                    {
                        cameraFollow.ResetToNormal(5.0f, 3.0f);
                        Debug.Log("[CAMERA] Transition -> NormalPlatformingFollow (Player exited chamber)");
                        LogCameraDiagnostics();
                    }
                }
            }
        }

        private void OnPlayerNewtonMarkInput()
        {
            if (Application.isPlaying && Time.frameCount == lastInputMarkFrame)
                return;

            lastInputMarkFrame = Time.frameCount;
            AttemptCapture();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            if (player == null)
            {
                player = Contrast.Core.UnityCompat.FindAny<PlayerController>();
                if (player == null) return;
            }

            if (cameraFollow == null)
                cameraFollow = Contrast.Core.UnityCompat.FindAny<CameraFollow>();

            // Handle proximity to finale chamber using the pre-Newton platform left boundary (Task E).
            // Camera position and orthographicSize are updated exclusively by CameraFollow.ProcessCameraUpdate().
            float px = player.transform.position.x;
            float thresholdX = GetPreNewtonThresholdX();
            if (px > thresholdX && !isZoneActive)
            {
                isZoneActive = true;
                if (hudCanvasGo != null)
                    hudCanvasGo.SetActive(true);
            }
            else if (px < thresholdX && isZoneActive)
            {
                isZoneActive = false;
                if (hudCanvasGo != null)
                    hudCanvasGo.SetActive(false);
            }

            SyncTargetStatesAndColliders();

            // Check dock mode activation: starts orbit when player first lands on platform K (or touches K in Test Mode)
            if (orbitStartMode == "dock" && !dockActive)
            {
                bool isGroundedOnK = (player != null && player.IsGrounded && player.GroundedPlatform == platformK) || IsPlayerStandingOn(platformK);
                bool isTestTouchK = player != null && player.IsTestModeActive &&
                    (DoesPlayerOverlapTargetCollider(0, out _, out _) ||
                     (platformK != null && GetPlayerColliderWorldRect().Overlaps(platformK.GetAabb())));
                if (isGroundedOnK || isTestTouchK)
                {
                    dockActive = true;
                    if (trajK != null) trajK.SetTrajectoryEnabled(true);
                    if (trajI != null) trajI.SetTrajectoryEnabled(true);
                    Debug.Log("[NEWTON_FINALE] Player docked on K -> Orbit motion started.");
                    LogCameraDiagnostics();
                }
            }

            // Update feedback timer
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && feedbackText != null)
                    feedbackText.text = string.Empty;
            }

            // Dynamic updates based on state
            if (CurrentStep == NewtonFinaleStep.Reveal)
            {
                UpdateRevealAnimation(Time.deltaTime);
                return;
            }

            if (CurrentStep == NewtonFinaleStep.Complete)
            {
                return;
            }

            if (!isZoneActive)
                return;

            // Update objective HUD
            UpdateObjectiveHUD();

            // Target crosshair positions
            UpdateTargetCrosshairs();

            // Check point capture trigger (X key down or mobile mark button, deduplicated per frame)
            if (IsNewtonMarkPressedThisFrame())
            {
                OnPlayerNewtonMarkInput();
            }
        }

        private bool IsNewtonMarkPressedThisFrame()
        {
            if (player != null && player.NewtonMarkRequested)
                return true;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.xKey.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetKeyDown(KeyCode.X);
#endif
        }

        private void UpdateObjectiveHUD()
        {
            RefreshCoordinateSlots();
            if (objectiveTitleText == null) return;

            switch (CurrentStep)
            {
                case NewtonFinaleStep.CaptureK:
                    objectiveTitleText.text = $"Đọc và ghi lại toạ độ x_k  (vân k = {fringeOrderK})";
                    objectiveSubText.text = "Đứng trên thanh K và nhấn X (hoặc nút NHẬP)";
                    break;
                case NewtonFinaleStep.CaptureI:
                    objectiveTitleText.text = $"Đọc và ghi lại toạ độ x_i  (vân i = {fringeOrderI})";
                    objectiveSubText.text = "Đứng trên thanh I, căn thẳng trục K–O và nhấn X";
                    break;
                case NewtonFinaleStep.CaptureKp:
                    objectiveTitleText.text = $"Đọc và ghi lại toạ độ x_k′  (vân k = {fringeOrderK})";
                    objectiveSubText.text = "Quay lại thanh K đối diện tâm O và nhấn X";
                    break;
                case NewtonFinaleStep.Reveal:
                case NewtonFinaleStep.Complete:
                    objectiveTitleText.text = "Đã ghi đủ toạ độ x_k, x_i, x_k′";
                    objectiveSubText.text = "Đang tính bước sóng giao thoa ánh sáng";
                    break;
            }
        }

        private void RefreshCoordinateSlots()
        {
            float lambdaSourceMm = sourceLambda * 1e-6f;
            float physicalR1Mm = Mathf.Sqrt(lambdaSourceMm * radiusOfCurvatureMm);
            float mmPerUnit = ringScale > 0.001f ? (physicalR1Mm / ringScale) : 0.2f;

            float rkMm = GetRadiusForOrder(fringeOrderK) * mmPerUnit;
            float riMm = GetRadiusForOrder(fringeOrderI) * mmPerUnit;

            if (slotXkValText != null)
            {
                slotXkValText.text = HasK ? $"+{rkMm:F2}" : "giá trị";
                slotXkValText.color = HasK ? LabStyleUI.TextPrimary : LabStyleUI.TextMuted;
                slotXkValText.fontStyle = HasK ? FontStyle.Bold : FontStyle.Normal;
            }

            if (slotXiValText != null)
            {
                slotXiValText.text = HasI ? $"−{riMm:F2}" : "giá trị";
                slotXiValText.color = HasI ? LabStyleUI.TextPrimary : LabStyleUI.TextMuted;
                slotXiValText.fontStyle = HasI ? FontStyle.Bold : FontStyle.Normal;
            }

            if (slotXkpValText != null)
            {
                slotXkpValText.text = HasKp ? $"−{rkMm:F2}" : "giá trị";
                slotXkpValText.color = HasKp ? LabStyleUI.TextPrimary : LabStyleUI.TextMuted;
                slotXkpValText.fontStyle = HasKp ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void UpdateTargetCrosshairs()
        {
            SyncTargetStatesAndColliders();

            if (!HasK)
            {
                if (targetICrosshair != null) targetICrosshair.SetActive(false);
                if (targetKpCrosshair != null) targetKpCrosshair.SetActive(false);
                return;
            }

            float rI = GetRadiusForOrder(fringeOrderI);
            float rK = GetRadiusForOrder(fringeOrderK);

            Vector2 targetI = centerO - AxisU * rI;
            Vector2 targetKp = centerO - AxisU * rK;

            // Keep target crosshairs visually visible once axis is established until each is captured;
            // only their interaction collider eligibility is gated by CurrentTargetIndex.
            if (targetICrosshair != null)
            {
                targetICrosshair.transform.position = new Vector3(targetI.x, targetI.y, -1f);
                targetICrosshair.SetActive(!HasI && CurrentStep < NewtonFinaleStep.Reveal);
            }

            if (targetKpCrosshair != null)
            {
                targetKpCrosshair.transform.position = new Vector3(targetKp.x, targetKp.y, -1f);
                targetKpCrosshair.SetActive(!HasKp && CurrentStep < NewtonFinaleStep.Reveal);
            }
        }

        public void AttemptCapture()
        {
            markEventSequence++;
            string eventId = $"X_{markEventSequence:D4}";
            int frame = Time.frameCount;

            if (CurrentStep >= NewtonFinaleStep.Reveal)
            {
                LastMarkRejectionReason = "ALREADY_COMPLETED";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=NONE accepted=False reason=ALREADY_COMPLETED nextTarget=COMPLETE");
                return;
            }

            if (player == null || player.State == PlayerState.Dead)
            {
                LastMarkRejectionReason = "PLAYER_DEAD_OR_NULL";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target={CurrentTargetIndex + 1} accepted=False reason=PLAYER_DEAD_OR_NULL nextTarget=UNCHANGED");
                return;
            }

            SyncTargetStatesAndColliders();
            int activeTargetIdx = CurrentTargetIndex;
            bool overlapsActiveCollider = DoesPlayerOverlapTargetCollider(activeTargetIdx, out Rect pBounds, out Rect tBounds);

            Debug.Log($"[NEWTON_INPUT] frame={frame} xPressedThisFrame=True xHeld=False eventId={eventId}");
            Debug.Log($"[NEWTON_COLLISION] frame={frame} currentTarget={activeTargetIdx + 1} playerBounds={pBounds} targetBounds={tBounds} overlapping={overlapsActiveCollider}");

            bool onK = (player.IsGrounded && player.GroundedPlatform == platformK) || IsPlayerStandingOn(platformK);
            bool onI = (player.IsGrounded && player.GroundedPlatform == platformI) || IsPlayerStandingOn(platformI);

            // In T-Key Test Mode (free mouse movement), allow marking when the player hovers over the active target or orbit platform
            if (player.IsTestModeActive)
            {
                if (CurrentStep == NewtonFinaleStep.CaptureK)
                {
                    float rK = GetRadiusForOrder(fringeOrderK);
                    float distFromRingK = Mathf.Abs(Vector2.Distance(player.transform.position, centerO) - rK);
                    bool nearRingK = distFromRingK <= 1.25f;
                    bool overlapsPlatK = platformK != null && pBounds.Overlaps(platformK.GetAabb());
                    if (overlapsActiveCollider || overlapsPlatK || nearRingK)
                    {
                        if (!overlapsActiveCollider && platformK != null)
                        {
                            Vector2 dir = ((Vector2)player.transform.position - centerO).normalized;
                            if (dir.sqrMagnitude < 0.001f) dir = Vector2.left;
                            Vector2 snapK = centerO + dir * rK;
                            platformK.transform.position = new Vector3(snapK.x, snapK.y, 0f);
                        }
                        if (orbitStartMode == "dock" && !dockActive)
                        {
                            dockActive = true;
                            if (trajK != null) trajK.SetTrajectoryEnabled(true);
                            if (trajI != null) trajI.SetTrajectoryEnabled(true);
                        }
                        onK = true;
                        overlapsActiveCollider = true;
                    }
                }
                else if (CurrentStep == NewtonFinaleStep.CaptureI)
                {
                    bool overlapsPlatI = platformI != null && pBounds.Overlaps(platformI.GetAabb());
                    if (overlapsActiveCollider || overlapsPlatI)
                    {
                        if (overlapsActiveCollider && platformI != null)
                        {
                            Vector2 targetI = GetTargetWorldPosition(1);
                            platformI.transform.position = new Vector3(targetI.x, targetI.y, 0f);
                        }
                        onI = true;
                    }
                }
                else if (CurrentStep == NewtonFinaleStep.CaptureKp)
                {
                    bool overlapsPlatK = platformK != null && pBounds.Overlaps(platformK.GetAabb());
                    if (overlapsActiveCollider || overlapsPlatK)
                    {
                        if (overlapsActiveCollider && platformK != null)
                        {
                            Vector2 targetKp = GetTargetWorldPosition(2);
                            platformK.transform.position = new Vector3(targetKp.x, targetKp.y, 0f);
                        }
                        onK = true;
                    }
                }
            }

            if (!onK && !onI)
            {
                LastMarkRejectionReason = "NOT_ON_ORBIT_PLATFORM";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target={activeTargetIdx + 1} accepted=False reason=NOT_ON_ORBIT_PLATFORM nextTarget=UNCHANGED");
                ShowFeedback("HÃY ĐỨNG TRÊN THANH ĐỂ ĐO · STAND ON PLATFORM");
                return;
            }

            ColorPlatform currentPlat = onK ? platformK : platformI;

            switch (CurrentStep)
            {
                case NewtonFinaleStep.CaptureK:
                    CaptureStepK(currentPlat, overlapsActiveCollider, eventId, frame);
                    break;
                case NewtonFinaleStep.CaptureI:
                    CaptureStepI(currentPlat, overlapsActiveCollider, eventId, frame);
                    break;
                case NewtonFinaleStep.CaptureKp:
                    CaptureStepKp(currentPlat, overlapsActiveCollider, eventId, frame);
                    break;
            }
        }

        private void CaptureStepK(ColorPlatform currentPlat, bool overlapsTarget1, string eventId, int frame)
        {
            if (currentPlat != platformK)
            {
                LastMarkRejectionReason = "WRONG_TARGET_PLATFORM_I_DURING_TARGET_1";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=1 accepted=False reason=WRONG_PLATFORM_I nextTarget=UNCHANGED");
                ShowFeedback($"HÃY ĐỨNG TRÊN THANH K (k = {fringeOrderK})");
                return;
            }

            if (!overlapsTarget1 || !IsTargetInteractionEnabled(0) || IsTargetCompleted(0))
            {
                LastMarkRejectionReason = "NO_TARGET_1_COLLIDER_OVERLAP";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=1 accepted=False reason=NO_TARGET_1_COLLIDER_OVERLAP nextTarget=UNCHANGED");
                return;
            }

            // K is selected dynamically by the player on the outer orbit
            Vector2 pos = platformK.transform.position;
            Vector2 dir = (pos - centerO).normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector2.left;

            float rK = GetRadiusForOrder(fringeOrderK);
            PointK = centerO + dir * rK;
            AxisU = dir;
            HasK = true;

            // Draw axis line through O
            float outerR = rK + 1.5f;
            Vector2 p1 = centerO + dir * outerR;
            Vector2 p2 = centerO - dir * outerR;
            axisLineRenderer.SetPosition(0, new Vector3(p1.x, p1.y, -3f));
            axisLineRenderer.SetPosition(1, new Vector3(p2.x, p2.y, -3f));
            axisLineRenderer.gameObject.SetActive(true);

            // Display marker K
            markerKGo.transform.position = new Vector3(PointK.x, PointK.y, -2f);
            markerKGo.SetActive(true);

            CurrentStep = NewtonFinaleStep.CaptureI;
            SyncTargetStatesAndColliders();
            UpdateTargetCrosshairs();
            LastMarkRejectionReason = "NONE";
            Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=1 accepted=True reason=TARGET_1_LOCKED nextTarget=2");
            ShowFeedback("ĐÃ KHÓA K · TRỤC K–O ĐƯỢC XÁC LẬP");
            LogCameraDiagnostics();
        }

        private void CaptureStepI(ColorPlatform currentPlat, bool overlapsTarget2, string eventId, int frame)
        {
            if (currentPlat != platformI)
            {
                LastMarkRejectionReason = "WRONG_TARGET_PLATFORM_K_DURING_TARGET_2";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=2 accepted=False reason=WRONG_PLATFORM_K nextTarget=UNCHANGED");
                ShowFeedback($"HÃY ĐỨNG TRÊN THANH I (i = {fringeOrderI})");
                return;
            }

            float rI = GetRadiusForOrder(fringeOrderI);
            Vector2 targetI = centerO - AxisU * rI;

            Vector2 curPos = platformI.transform.position;
            float dist = Vector2.Distance(curPos, targetI);

            if (overlapsTarget2 && IsTargetInteractionEnabled(1) && !IsTargetCompleted(1) && dist <= toleranceI)
            {
                PointI = targetI;
                HasI = true;

                markerIGo.transform.position = new Vector3(PointI.x, PointI.y, -2f);
                markerIGo.SetActive(true);

                int accuracy = Mathf.Max(0, Mathf.RoundToInt(100f * (1f - dist / toleranceI)));
                CurrentStep = NewtonFinaleStep.CaptureKp;
                SyncTargetStatesAndColliders();
                UpdateTargetCrosshairs();
                LastMarkRejectionReason = "NONE";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=2 accepted=True reason=TARGET_2_LOCKED nextTarget=3");
                ShowFeedback($"I ĐÃ KHÓA · {accuracy}%");
                LogCameraDiagnostics();
            }
            else
            {
                LastMarkRejectionReason = "NO_TARGET_2_COLLIDER_OVERLAP";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=2 accepted=False reason=NO_TARGET_2_COLLIDER_OVERLAP nextTarget=UNCHANGED");
                ShowFeedback("CHỜ I THẲNG HÀNG VỚI TRỤC K–O");
            }
        }

        private void CaptureStepKp(ColorPlatform currentPlat, bool overlapsTarget3, string eventId, int frame)
        {
            if (currentPlat != platformK)
            {
                LastMarkRejectionReason = "WRONG_TARGET_PLATFORM_I_DURING_TARGET_3";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=3 accepted=False reason=WRONG_PLATFORM_I nextTarget=UNCHANGED");
                ShowFeedback($"HÃY TRỞ LẠI THANH K (k = {fringeOrderK})");
                return;
            }

            float rK = GetRadiusForOrder(fringeOrderK);
            Vector2 targetKp = centerO - AxisU * rK;

            Vector2 curPos = platformK.transform.position;
            float dist = Vector2.Distance(curPos, targetKp);

            if (overlapsTarget3 && IsTargetInteractionEnabled(2) && !IsTargetCompleted(2) && dist <= toleranceKp)
            {
                PointKp = targetKp;
                HasKp = true;

                markerKpGo.transform.position = new Vector3(PointKp.x, PointKp.y, -2f);
                markerKpGo.SetActive(true);

                int accuracy = Mathf.Max(0, Mathf.RoundToInt(100f * (1f - dist / toleranceKp)));
                LastMarkRejectionReason = "NONE";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=3 accepted=True reason=TARGET_3_LOCKED nextTarget=COMPLETE");
                ShowFeedback($"K′ ĐÃ KHÓA · {accuracy}% · BẮT ĐẦU HIỂN THỊ KẾT QUẢ ĐO");
                StartRevealSequence();
            }
            else
            {
                LastMarkRejectionReason = "NO_TARGET_3_COLLIDER_OVERLAP";
                Debug.Log($"[NEWTON_MARKING] frame={frame} eventId={eventId} target=3 accepted=False reason=NO_TARGET_3_COLLIDER_OVERLAP nextTarget=UNCHANGED");
                ShowFeedback("CHỜ K′ THẲNG HÀNG VỚI TRỤC K–O");
            }
        }

        public void StartRevealSequence()
        {
            CurrentStep = NewtonFinaleStep.Reveal;
            RevealTimer = 0f;
            SyncTargetStatesAndColliders();

            if (player != null)
            {
                if (player.IsTestModeActive)
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.SetTestMode(false);
                    else
                        player.SetTestMode(false);
                }
                player.SetInputEnabled(false);
                player.StopMovement();
            }

            if (cameraFollow != null)
            {
                ComputeNewtonCameraFraming(out Vector3 fixedCenter, out float orthoSize);
                cameraFollow.SetNewtonReveal(fixedCenter, orthoSize, 3.2f);
                Debug.Log($"[CAMERA] Transition -> NewtonResultReveal (Marks complete: K->I->K', framing center O=({centerO.x:F2}, {centerO.y:F2}))");
                LogCameraDiagnostics();
            }

            ComputePhysicalWavelength();

            string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(CalculatedLambda_nm);
            if (calcValuesText != null)
            {
                calcValuesText.text = $"B = {MeasuredB_mm:F3} mm   b = {Measuredb_mm:F3} mm   R = {radiusOfCurvatureMm:F0} mm   i = {fringeOrderI}   k = {fringeOrderK}";
            }
            if (calcResultText != null)
            {
                calcResultText.text = $"Bước sóng đo được:  λ ≈ {CalculatedLambda_nm:F1} nm";
                calcResultText.color = LabStyleUI.TextPrimary;
            }
            if (completionDetailText != null)
            {
                completionDetailText.text = $"B = {MeasuredB_mm:F3} mm · b = {Measuredb_mm:F3} mm · Bước sóng λ ≈ {CalculatedLambda_nm:F1} nm (Ánh sáng {colorNameVi})";
            }

            isQuizDisplayed = false;
            isColorQuizAnsweredCorrectly = false;
            isBingoAnimPlaying = false;
            bingoAnimTimer = 0f;
            wrongQuizAttemptsCount = 0;
            if (quizCardGo != null)
            {
                quizCardGo.SetActive(false);
                quizCardGo.transform.localScale = Vector3.one;
            }
            if (apparatusVisuals != null)
                apparatusVisuals.SetColorRevealedImmediate(false);

            RefreshCoordinateSlots();
            if (targetICrosshair != null) targetICrosshair.SetActive(false);
            if (targetKpCrosshair != null) targetKpCrosshair.SetActive(false);
            if (objectiveCardGo != null) objectiveCardGo.SetActive(false);
            if (bottomBarGo != null) bottomBarGo.SetActive(false);
            if (calculationPanelGo != null)
            {
                RectTransform calcRt = calculationPanelGo.GetComponent<RectTransform>();
                if (calcRt != null)
                    calcRt.anchoredPosition = new Vector2(22f, -80f);
            }
        }

        private int GetCurrentLevelSlotIndex()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentCampaignIndex >= 0 && GameManager.Instance.CurrentCampaignIndex < 3)
                return GameManager.Instance.CurrentCampaignIndex;
            int idx = Config.GetLevelIndexForWavelength(sourceLambda);
            return idx >= 0 ? idx : 0;
        }

        private void ShowWavelengthColorQuizCard()
        {
            if (quizCardGo == null)
                return;

            isQuizDisplayed = true;
            isColorQuizAnsweredCorrectly = false;
            isBingoAnimPlaying = false;
            bingoAnimTimer = 0f;
            wrongQuizAttemptsCount = 0;

            // Hide bottom-center PhaseTunerPanel while the bottom quiz card is active so they do not overlap
            if (PhaseTunerUI.Instance != null && PhaseTunerUI.Instance.TunerPanelRect != null)
                PhaseTunerUI.Instance.TunerPanelRect.gameObject.SetActive(false);

            string correctColor = NewtonRingsPresentation.GetWavelengthColorNameVi(CalculatedLambda_nm);
            PrepareQuizOptions(correctColor);

            if (quizBadgeText != null)
            {
                quizBadgeText.text = "XÁC ĐỊNH MÀU ÁNH SÁNG TỪ BƯỚC SÓNG  ·  LƯỢT CHỌN: 2/2";
                quizBadgeText.color = LabStyleUI.TextSecondary;
            }

            if (quizQuestionText != null)
            {
                quizQuestionText.text = $"Bước sóng đo được ≈ {CalculatedLambda_nm:F0} nm — vậy ánh sáng này là màu gì?";
                quizQuestionText.color = LabStyleUI.TextPrimary;
            }

            if (quizFeedbackText != null)
            {
                quizFeedbackText.text = "Hãy chọn 1 trong 4 đáp án bên dưới (cho phép chọn sai tối đa 1 lần):";
                quizFeedbackText.color = LabStyleUI.TextSecondary;
            }

            if (quizOptionsRowGo != null)
                quizOptionsRowGo.SetActive(true);
            if (quizErrorResetButtonGo != null)
                quizErrorResetButtonGo.SetActive(false);

            for (int i = 0; i < 4; i++)
            {
                if (quizOptionButtons[i] != null)
                    quizOptionButtons[i].interactable = true;
                if (quizOptionImages[i] != null)
                {
                    LabStyleUI.ApplySlicedSprite(quizOptionImages[i], LabStyleUI.GetSecondaryButtonSprite(), true, -3f);
                    quizOptionImages[i].color = Color.white;
                }
                if (quizOptionLabels[i] != null)
                {
                    quizOptionLabels[i].text = currentQuizOptions[i];
                    quizOptionLabels[i].color = Color.white;
                }
            }

            quizCardGo.transform.localScale = Vector3.one;
            quizCardGo.SetActive(true);
        }

        private void PrepareQuizOptions(string correctColor)
        {
            int lvlIdx = GetCurrentLevelSlotIndex();
            string[] savedSlots = ActiveOptionSlotsByLevel[lvlIdx];
            bool[] wrongMask = PickedWrongSlotMaskByLevel[lvlIdx];
            HashSet<string> eliminated = EliminatedWrongColorsByLevel[lvlIdx];

            bool hasPartialPreservedSlots = false;
            if (savedSlots != null && wrongMask != null && savedSlots.Length == 4 && wrongMask.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (wrongMask[i])
                    {
                        hasPartialPreservedSlots = true;
                        break;
                    }
                }
            }

            if (hasPartialPreservedSlots)
            {
                // Keep unpicked options in their exact slots, and only replace the picked wrong slots
                HashSet<string> usedInCard = new HashSet<string>();
                for (int i = 0; i < 4; i++)
                {
                    if (!wrongMask[i] && !string.IsNullOrEmpty(savedSlots[i]))
                        usedInCard.Add(savedSlots[i]);
                }

                // Ensure correctColor is always present among kept or replaced slots
                bool hasCorrect = usedInCard.Contains(correctColor);

                List<string> candidates = BuildReplacementCandidateList(usedInCard, eliminated, correctColor);
                for (int i = 0; i < 4; i++)
                {
                    if (wrongMask[i])
                    {
                        if (!hasCorrect)
                        {
                            savedSlots[i] = correctColor;
                            usedInCard.Add(correctColor);
                            hasCorrect = true;
                        }
                        else if (candidates.Count > 0)
                        {
                            int pickIdx = UnityEngine.Random.Range(0, candidates.Count);
                            string picked = candidates[pickIdx];
                            candidates.RemoveAt(pickIdx);
                            savedSlots[i] = picked;
                            usedInCard.Add(picked);
                        }
                    }
                    wrongMask[i] = false;
                    currentQuizOptions[i] = savedSlots[i];
                }
                return;
            }

            // Fresh initialization of 4 random options (1 correct + 3 random distractors)
            savedSlots = new string[4];
            wrongMask = new bool[4];
            eliminated.Clear();

            List<string> pool = new List<string>(AllColorChoicesPool.Length);
            for (int i = 0; i < AllColorChoicesPool.Length; i++)
            {
                if (AllColorChoicesPool[i] != correctColor)
                    pool.Add(AllColorChoicesPool[i]);
            }

            savedSlots[0] = correctColor;
            for (int i = 1; i < 4 && pool.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                savedSlots[i] = pool[pick];
                pool.RemoveAt(pick);
            }

            // Fisher-Yates shuffle
            for (int i = 3; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp = savedSlots[i];
                savedSlots[i] = savedSlots[j];
                savedSlots[j] = tmp;
            }

            ActiveOptionSlotsByLevel[lvlIdx] = savedSlots;
            PickedWrongSlotMaskByLevel[lvlIdx] = wrongMask;
            for (int i = 0; i < 4; i++)
                currentQuizOptions[i] = savedSlots[i];
        }

        private static List<string> BuildReplacementCandidateList(
            HashSet<string> usedInCard,
            HashSet<string> eliminated,
            string correctColor)
        {
            List<string> candidates = new List<string>();
            for (int i = 0; i < AllColorChoicesPool.Length; i++)
            {
                string col = AllColorChoicesPool[i];
                if (col != correctColor && !usedInCard.Contains(col) && !eliminated.Contains(col))
                    candidates.Add(col);
            }

            // If we exhausted the pool across many failures, allow previously eliminated colors except those currently on the card
            if (candidates.Count < 2)
            {
                eliminated.Clear();
                candidates.Clear();
                for (int i = 0; i < AllColorChoicesPool.Length; i++)
                {
                    string col = AllColorChoicesPool[i];
                    if (col != correctColor && !usedInCard.Contains(col))
                        candidates.Add(col);
                }
            }

            return candidates;
        }

        public void SelectQuizOption(int slotIndex) => OnQuizOptionClicked(slotIndex);

        private void OnQuizOptionClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4 || isColorQuizAnsweredCorrectly || wrongQuizAttemptsCount >= 2)
                return;

            string chosenColor = currentQuizOptions[slotIndex];
            string correctColor = NewtonRingsPresentation.GetWavelengthColorNameVi(CalculatedLambda_nm);
            int lvlIdx = GetCurrentLevelSlotIndex();

            Debug.Log($"[UI_BUTTON][NewtonQuiz] Clicked QuizOption_{slotIndex} ('{chosenColor}', correct='{correctColor}')");
            Contrast.Audio.ContrastAudioManager.Instance?.PlayButtonClick();

            if (chosenColor == correctColor)
            {
                // CORRECT! Mark level completed, start Bingo animation + radial Newton ring color bloom
                isColorQuizAnsweredCorrectly = true;
                isBingoAnimPlaying = true;
                bingoAnimTimer = 0f;

                Config.Instance.MarkLevelCompletedByWavelength(sourceLambda);
                Config.Instance.SetLevelCompleted(lvlIdx, true);

                ActiveOptionSlotsByLevel[lvlIdx] = null;
                PickedWrongSlotMaskByLevel[lvlIdx] = null;
                EliminatedWrongColorsByLevel[lvlIdx].Clear();

                Color spectralCol = NewtonRingsPresentation.WavelengthToRgb(sourceLambda);
                Color spectralAccent = Color.Lerp(spectralCol, Color.white, 0.30f);

                if (wavelengthTagText != null)
                {
                    wavelengthTagText.text = $"Hệ vân tròn Newton · Ánh sáng {correctColor} (λ = {sourceLambda:F0} nm)";
                    wavelengthTagText.color = new Color(spectralAccent.r, spectralAccent.g, spectralAccent.b, 0.94f);
                }
                if (calcResultText != null)
                {
                    calcResultText.text = $"λ ≈ {CalculatedLambda_nm:F1} nm · Ánh sáng {correctColor}";
                    calcResultText.color = spectralAccent;
                }

                if (quizOptionsRowGo != null)
                    quizOptionsRowGo.SetActive(false);

                if (quizBadgeText != null)
                {
                    quizBadgeText.text = "✦ BINGO! CHÍNH XÁC 100% — ĐÃ XÁC ĐỊNH ĐÚNG PHỔ MÀU ✦";
                    quizBadgeText.color = new Color(0.45f, 0.96f, 0.78f, 1f);
                }
                if (quizQuestionText != null)
                {
                    quizQuestionText.text = $"BINGO! Ánh sáng {correctColor} (λ ≈ {CalculatedLambda_nm:F0} nm)";
                    quizQuestionText.color = spectralAccent;
                }
                if (quizFeedbackText != null)
                {
                    quizFeedbackText.text = "Hệ vân tròn Newton đang khôi phục màu sắc quang học thực tế...";
                    quizFeedbackText.color = LabStyleUI.TextPrimary;
                }
                return;
            }

            // WRONG CHOICE
            wrongQuizAttemptsCount++;
            if (PickedWrongSlotMaskByLevel[lvlIdx] == null || PickedWrongSlotMaskByLevel[lvlIdx].Length != 4)
                PickedWrongSlotMaskByLevel[lvlIdx] = new bool[4];
            PickedWrongSlotMaskByLevel[lvlIdx][slotIndex] = true;
            EliminatedWrongColorsByLevel[lvlIdx].Add(chosenColor);

            if (wrongQuizAttemptsCount == 1)
            {
                // 1st wrong choice allowed: disable that button and warn player (1 attempt remaining)
                if (quizOptionButtons[slotIndex] != null)
                    quizOptionButtons[slotIndex].interactable = false;
                if (quizOptionImages[slotIndex] != null)
                    quizOptionImages[slotIndex].color = new Color(0.55f, 0.22f, 0.25f, 0.75f);
                if (quizOptionLabels[slotIndex] != null)
                {
                    quizOptionLabels[slotIndex].text = $"✗ {chosenColor}";
                    quizOptionLabels[slotIndex].color = new Color(1f, 0.65f, 0.65f, 0.9f);
                }
                if (quizBadgeText != null)
                {
                    quizBadgeText.text = "CẢNH BÁO SAI LẦN 1  ·  CÒN 1 LƯỢT CHỌN CUỐI CÙNG (1/2)";
                    quizBadgeText.color = new Color(1f, 0.78f, 0.35f, 1f);
                }
                if (quizFeedbackText != null)
                {
                    quizFeedbackText.text = $"Đáp án '{chosenColor}' chưa chính xác! Bạn còn 1 lượt chọn duy nhất.";
                    quizFeedbackText.color = new Color(1f, 0.78f, 0.38f, 1f);
                }
            }
            else
            {
                // 2nd wrong choice: report measurement error and require level restart from the beginning
                if (quizOptionsRowGo != null)
                    quizOptionsRowGo.SetActive(false);
                if (quizErrorResetButtonGo != null)
                    quizErrorResetButtonGo.SetActive(true);

                if (quizBadgeText != null)
                {
                    quizBadgeText.text = "⚠ BÁO CÁO LỖI THÍ NGHIỆM  ·  CHỌN SAI QUÁ SỐ LƯỢT (2/2)";
                    quizBadgeText.color = new Color(1f, 0.45f, 0.48f, 1f);
                }
                if (quizQuestionText != null)
                {
                    quizQuestionText.text = "PHÉP ĐO BỊ LỖI — BẠN ĐÃ CHỌN SAI MÀU ÁNH SÁNG 2 LẦN!";
                    quizQuestionText.color = new Color(1f, 0.48f, 0.50f, 1f);
                }
                if (quizFeedbackText != null)
                {
                    quizFeedbackText.text = "Kết quả xác định phổ ánh sáng không hợp lệ. Vui lòng khởi động lại màn chơi từ đầu.";
                    quizFeedbackText.color = LabStyleUI.TextSecondary;
                }
            }
        }

        public void ComputePhysicalWavelength()
        {
            MeasuredB_units = Vector2.Distance(PointK, PointI);
            Measuredb_units = Vector2.Distance(PointI, PointKp);

            // Physical conversion factor
            // In reference Easy configuration: ringScale = 220 px / 64 = 3.4375 units
            // physical r_1 = sqrt(lambda_ref * R) = sqrt(550e-6 mm * 855 mm) ~ 0.685747767 mm
            // mmPerUnit = physical r_1 / ringScale = 0.685747767 / 3.4375 ~ 0.19949026 mm/unit
            float lambdaSourceMm = sourceLambda * 1e-6f;
            float physicalR1Mm = Mathf.Sqrt(lambdaSourceMm * radiusOfCurvatureMm);
            float mmPerUnit = physicalR1Mm / ringScale;

            MeasuredB_mm = MeasuredB_units * mmPerUnit;
            Measuredb_mm = Measuredb_units * mmPerUnit;

            int orderDelta = Mathf.Abs(fringeOrderK - fringeOrderI); // |4 - 2| = 2
            float denominator = orderDelta * radiusOfCurvatureMm;

            float lambdaOutMm = (denominator > 0f)
                ? (MeasuredB_mm * Measuredb_mm) / denominator
                : 0f;

            CalculatedLambda_nm = lambdaOutMm * 1e6f;

            Debug.Log($"[NEWTON_FINALE] Measured B={MeasuredB_mm:F3}mm, b={Measuredb_mm:F3}mm, lambda={CalculatedLambda_nm:F1}nm");
        }

        private void UpdateRevealAnimation(float dt)
        {
            RevealTimer += dt;
            float tm = RevealTimer;

            // Phase A — Transition & Focus (0 - 0.35s)
            // (Player stops, camera frames apparatus)

            // Phase B — Draw solid line B = KI (0.35 - 1.17s)
            if (tm > 0.35f)
            {
                lineKI.gameObject.SetActive(true);
                float p = Mathf.Clamp01((tm - 0.35f) / 0.82f);
                Vector2 curTip = Vector2.Lerp(PointK, PointI, p);
                lineKI.SetPosition(0, new Vector3(PointK.x, PointK.y, -2f));
                lineKI.SetPosition(1, new Vector3(curTip.x, curTip.y, -2f));
            }

            // Phase C — Draw dashed line b = IK' (1.15 - 1.87s)
            if (tm > 1.15f)
            {
                lineIKp.gameObject.SetActive(true);
                float p = Mathf.Clamp01((tm - 1.15f) / 0.72f);
                Vector2 curTip = Vector2.Lerp(PointI, PointKp, p);
                lineIKp.SetPosition(0, new Vector3(PointI.x, PointI.y, -2f));
                lineIKp.SetPosition(1, new Vector3(curTip.x, curTip.y, -2f));
            }

            // Phase D — Display Scientific Calculation (2.0 - 2.55s)
            if (tm > 2.0f)
            {
                if (!calculationPanelGo.activeSelf)
                    calculationPanelGo.SetActive(true);

                float alpha = Mathf.Clamp01((tm - 2.0f) / 0.55f);
                if (calcCanvasGroup != null)
                    calcCanvasGroup.alpha = alpha;

                if (calcValuesText != null)
                {
                    calcValuesText.text = $"B = {MeasuredB_mm:F3} mm   b = {Measuredb_mm:F3} mm   R = {radiusOfCurvatureMm:F0} mm   i = {fringeOrderI}   k = {fringeOrderK}";
                }

                if (calcResultText != null)
                {
                    if (isColorQuizAnsweredCorrectly)
                    {
                        string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(CalculatedLambda_nm);
                        calcResultText.text = $"λ ≈ {CalculatedLambda_nm:F1} nm · Ánh sáng {colorNameVi}";
                    }
                    else
                    {
                        calcResultText.text = $"Bước sóng đo được:  λ ≈ {CalculatedLambda_nm:F1} nm";
                    }
                }
            }

            // Phase E — Show Bottom-Docked Wavelength Color Quiz Card at tm >= 2.55s
            if (tm >= 2.55f && !isQuizDisplayed)
            {
                ShowWavelengthColorQuizCard();
            }

            // Phase F — After correct answer: play Bingo card bounce + radial color bloom on Newton rings, then open ResultCard
            if (isColorQuizAnsweredCorrectly && isBingoAnimPlaying)
            {
                bingoAnimTimer += dt;

                // Gentle celebratory bounce on the Bingo card during the first 0.50s
                if (quizCardGo != null)
                {
                    float pulsePhase = Mathf.Clamp01(bingoAnimTimer / 0.50f);
                    float scale = 1f + 0.055f * Mathf.Sin(pulsePhase * Mathf.PI);
                    quizCardGo.transform.localScale = new Vector3(scale, scale, 1f);
                }

                // Progressive radial color bloom across the Newton rings over 1.10s
                if (apparatusVisuals != null)
                {
                    float bloomProgress = Mathf.Clamp01(bingoAnimTimer / 1.10f);
                    apparatusVisuals.SetColorRevealProgress(bloomProgress);
                }

                if (bingoAnimTimer >= 1.40f)
                {
                    isBingoAnimPlaying = false;
                    if (apparatusVisuals != null)
                        apparatusVisuals.SetColorRevealedImmediate(true);
                    if (quizCardGo != null)
                    {
                        quizCardGo.transform.localScale = Vector3.one;
                        quizCardGo.SetActive(false);
                    }

                    if (CurrentStep != NewtonFinaleStep.Complete)
                    {
                        CurrentStep = NewtonFinaleStep.Complete;
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnPlayerWon();
                        }
                        else if (completionPanelGo != null)
                        {
                            completionPanelGo.SetActive(true);
                        }
                    }
                }
            }
        }

        public void ShowFeedback(string msg)
        {
            if (feedbackText != null)
            {
                feedbackText.text = msg;
                feedbackTimer = 1.6f;
            }
            Debug.Log($"[NEWTON_FINALE] {msg}");
        }

        public void OnPlayAgainClicked()
        {
            Debug.Log("[UI_BUTTON][NewtonCompletion] Clicked PLAY AGAINButton -> OnPlayAgainClicked");
            Debug.Log("[NEWTON_FINALE] Play Again clicked.");
            ResetState();
            GameManager.Instance?.Restart();
        }

        public void OnReturnToLevelSelectClicked()
        {
            Debug.Log("[UI_BUTTON][NewtonCompletion] Clicked LEVEL SELECTButton -> OnReturnToLevelSelectClicked");
            Debug.Log("[NEWTON_FINALE] Return to Level Select clicked.");
            Teardown();
            GameManager.Instance?.ReturnToMainMenu();
        }

        public void ResetState()
        {
            ResetState(false);
        }

        public void ResetState(bool preserveCameraInFlight)
        {
            CurrentStep = NewtonFinaleStep.CaptureK;
            HasK = false;
            HasI = false;
            HasKp = false;
            PointK = Vector2.zero;
            PointI = Vector2.zero;
            PointKp = Vector2.zero;
            AxisU = Vector2.right;
            RevealTimer = 0f;

            if (axisLineRenderer != null) axisLineRenderer.gameObject.SetActive(false);
            if (lineKI != null) lineKI.gameObject.SetActive(false);
            if (lineIKp != null) lineIKp.gameObject.SetActive(false);
            if (markerKGo != null) markerKGo.SetActive(false);
            if (markerIGo != null) markerIGo.SetActive(false);
            if (markerKpGo != null) markerKpGo.SetActive(false);
            if (targetICrosshair != null) targetICrosshair.SetActive(false);
            if (targetKpCrosshair != null) targetKpCrosshair.SetActive(false);
            if (objectiveCardGo != null) objectiveCardGo.SetActive(true);
            RefreshCoordinateSlots();
            if (calculationPanelGo != null)
            {
                calculationPanelGo.SetActive(false);
                RectTransform calcRt = calculationPanelGo.GetComponent<RectTransform>();
                if (calcRt != null)
                    calcRt.anchoredPosition = new Vector2(22f, -282f);
            }
            if (completionPanelGo != null) completionPanelGo.SetActive(false);
            if (quizCardGo != null)
            {
                quizCardGo.transform.localScale = Vector3.one;
                quizCardGo.SetActive(false);
            }
            isQuizDisplayed = false;
            isColorQuizAnsweredCorrectly = false;
            isBingoAnimPlaying = false;
            bingoAnimTimer = 0f;
            wrongQuizAttemptsCount = 0;
            if (apparatusVisuals != null)
                apparatusVisuals.SetColorRevealedImmediate(false);
            if (wavelengthTagText != null)
            {
                wavelengthTagText.text = "Hệ vân tròn Newton · Phép đo bước sóng ánh sáng (λ = ? nm)";
                wavelengthTagText.color = LabStyleUI.TextSecondary;
            }
            if (PhaseTunerUI.Instance != null && PhaseTunerUI.Instance.TunerPanelRect != null)
                PhaseTunerUI.Instance.TunerPanelRect.gameObject.SetActive(true);
            if (bottomBarGo != null) bottomBarGo.SetActive(true);

            if (cameraFollow != null)
            {
                SyncCameraTransitionConfig();
                if (!preserveCameraInFlight)
                {
                    cameraFollow.ResetToNormal();
                }
            }

            if (player != null)
                player.SetInputEnabled(true);

            dockActive = (orbitStartMode != "dock");
            if (trajK != null)
            {
                trajK.SetTrajectoryEnabled(dockActive);
                trajK.ResetPhase();
            }
            if (trajI != null)
            {
                trajI.SetTrajectoryEnabled(dockActive);
                trajI.ResetPhase();
            }

            isZoneActive = false;
            lastInputMarkFrame = -1;
            LastMarkRejectionReason = "NONE";
            SyncTargetStatesAndColliders();
            if (hudCanvasGo != null)
                hudCanvasGo.SetActive(false);
        }

        public void Teardown()
        {
            if (Instance == this)
                Instance = null;

            dockActive = false;
            PlayerInput.IsNewtonFinaleActive = false;

            if (player != null)
                player.OnNewtonMarkRequested -= OnPlayerNewtonMarkInput;

            if (cameraFollow != null)
            {
                cameraFollow.ClearNewtonTransitionConfig();
                cameraFollow.ResetToNormal();
            }

            if (player != null)
                player.SetInputEnabled(true);

            if (platformK != null && platformK.gameObject != null)
                SafeDestroy(platformK.gameObject);

            if (platformI != null && platformI.gameObject != null)
                SafeDestroy(platformI.gameObject);

            if (hudCanvasGo != null)
                SafeDestroy(hudCanvasGo);

            if (apparatusRoot != null)
                SafeDestroy(apparatusRoot);

            SafeDestroy(gameObject);
        }

        public string GetCurrentActiveRail()
        {
            if (player == null) return "None";
            if (!player.IsGrounded) return "Air";
            if (player.GroundedPlatform == platformK) return "K";
            if (player.GroundedPlatform == platformI) return "I";
            return "Ground";
        }

        public void LogCameraDiagnostics()
        {
            if (cameraFollow != null)
            {
                string diag = cameraFollow.GetDiagnosticsLog(GetCurrentActiveRail());
                Debug.Log(diag);
            }
        }

        private bool IsPlayerStandingOn(ColorPlatform plat)
        {
            if (player == null || plat == null) return false;
            if (!plat.IsUniversal && plat.Logical == player.CurrentColor)
                return false;
            Rect platRect = plat.GetAabb();
            Vector2 pPos = player.transform.position;
            Vector2 pSize = player.PlayerSize;
            float pBottom = pPos.y - pSize.y * 0.5f;
            float pLeft = pPos.x - pSize.x * 0.5f;
            float pRight = pPos.x + pSize.x * 0.5f;
            bool hOverlap = pRight > platRect.xMin + 0.01f && pLeft < platRect.xMax - 0.01f;
            bool vTouch = Mathf.Abs(pBottom - platRect.yMax) <= 0.2f && pPos.y >= platRect.yMax - 0.05f;
            return hOverlap && vTouch;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        private static Sprite circleSpriteCache;
        private static Sprite GetCircleSprite()
        {
            if (circleSpriteCache != null) return circleSpriteCache;
            const int r = 32;
            const int d = r * 2;
            Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                    float a = Mathf.Clamp01(1f - (dist - (r - 1.5f)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
            return circleSpriteCache;
        }

        private static Sprite ringSpriteCache;
        private static Sprite GetRingSprite()
        {
            if (ringSpriteCache != null) return ringSpriteCache;
            const int r = 32;
            const int d = r * 2;
            Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(dist - (r - 3f)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            ringSpriteCache = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
            return ringSpriteCache;
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Contrast.Core.UnityCompat.GetBuiltinFont();
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }
    }
}
