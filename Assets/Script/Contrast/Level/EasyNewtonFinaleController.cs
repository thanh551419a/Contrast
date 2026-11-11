using System;
using System.Collections.Generic;
using Contrast.Camera;
using Contrast.Core;
using Contrast.Data;
using Contrast.Player;
using Contrast.Presentation;
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

        [Header("Chamber Geometry")]
        [SerializeField] private Vector2 centerO = new Vector2(61.0f, 0.625f);
        [SerializeField] private float ringScale = 3.4375f; // 220 px / 64
        [SerializeField] private float sourceLambda = 550f;
        [SerializeField] private float referenceLambda = 550f;
        [SerializeField] private float radiusOfCurvatureMm = 855f; // R = 855 mm
        [SerializeField] private int fringeOrderK = 4;
        [SerializeField] private int fringeOrderI = 2;

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

        // Presentation & UI
        private GameObject hudCanvasGo;
        private Canvas hudCanvas;
        private GameObject objectiveCardGo;
        private Text objectiveInfoText;
        private Text objectiveTitleText;
        private Text objectiveSubText;
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

        public int CurrentTargetIndex => CurrentStep switch
        {
            NewtonFinaleStep.CaptureK => 0,
            NewtonFinaleStep.CaptureI => 1,
            NewtonFinaleStep.CaptureKp => 2,
            _ => 3
        };

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
            // HTML prototype (newtonViewLayout in CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html):
            // Fixed world-space camera anchored on Newton center O = (cx, cy), scaled so the outer dark ring k
            // and all 3 targets (K, I, K') plus orbiting platforms K & I remain simultaneously inside the frame.
            float rK = GetRadiusForOrder(fringeOrderK);
            fixedWorldCenter = new Vector3(centerO.x, centerO.y, -10f);
            orthoSize = Mathf.Max(8.6f, (rK + 0.70f) / 0.89f);
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
                // L02 (k=4): 67.617188 - 1.953125 * 0.5 = 66.6406255f
                // L01 (k=6): 68.414062 - 2.890625 * 0.5 = 66.9687495f
                // L03 (k=7): 65.75 - 6.84375 * 0.5      = 62.3281250f
                thresholdX = fringeOrderK switch
                {
                    4 => 67.617188f - 1.953125f * 0.5f,
                    6 => 68.414062f - 2.890625f * 0.5f,
                    7 => 65.75f - 6.84375f * 0.5f,
                    _ => centerO.x - ringScale * 3.84f
                };
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
                cameraFollow = FindAnyObjectByType<CameraFollow>();
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

            player = pc != null ? pc : FindAnyObjectByType<PlayerController>();
            cameraFollow = cf != null ? cf : FindAnyObjectByType<CameraFollow>();

            if (newtonData != null)
            {
                centerO = new Vector2(newtonData.cx / 64f, -newtonData.cy / 64f);
                fringeOrderK = newtonData.k;
                fringeOrderI = newtonData.i;
                if (newtonData.lambda > 0f)
                {
                    sourceLambda = newtonData.lambda;
                }
                else
                {
                    sourceLambda = (fringeOrderK == 4 && fringeOrderI == 2) ? 650f
                        : (fringeOrderK == 7 && fringeOrderI == 2) ? 470f
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
            float outerR = ringScale * Mathf.Sqrt(7f + 0.65f) + 1.5f; // outer optical radius ~10.8 units
            apparatusVisuals.Configure(ringScale, outerR, sourceLambda, referenceLambda);
            apparatusVisuals.SetCenterPosition(centerO);
        }

        public float GetRadiusForOrder(int order)
        {
            if (order <= 0) return 0f;
            return ringScale * Mathf.Sqrt(order);
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
            tm.text = label;
            tm.fontSize = 32;
            tm.characterSize = 0.05f;
            tm.color = color;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            MeshRenderer mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

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
            tm.text = label;
            tm.fontSize = 28;
            tm.characterSize = 0.045f;
            tm.color = new Color(0.88f, 0.95f, 1f, 0.85f);
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            MeshRenderer mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

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
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 0.96f, 1f, 0.9f);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(2f, 0.6f);
            trt.localScale = Vector3.one * 0.02f;

            return text;
        }

        private void BuildHUD()
        {
            hudCanvasGo = new GameObject("EasyNewtonFinaleHUD");
            hudCanvasGo.transform.SetParent(transform, false);

            hudCanvas = hudCanvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 150;

            CanvasScaler scaler = hudCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            hudCanvasGo.AddComponent<GraphicRaycaster>();

            Color spectralCol = NewtonRingsPresentation.WavelengthToRgb(sourceLambda);
            string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            Color spectralAccent = Color.Lerp(spectralCol, Color.white, 0.30f);

            // Objective Card (Top Left)
            GameObject objCard = new GameObject("ObjectiveCard");
            objectiveCardGo = objCard;
            objCard.transform.SetParent(hudCanvasGo.transform, false);
            Image objBg = objCard.AddComponent<Image>();
            objBg.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            Outline objOut = objCard.AddComponent<Outline>();
            objOut.effectColor = new Color(spectralAccent.r, spectralAccent.g, spectralAccent.b, 0.28f);
            objOut.effectDistance = new Vector2(1f, -1f);
            RectTransform objRt = objCard.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 1f);
            objRt.anchorMax = new Vector2(0f, 1f);
            objRt.pivot = new Vector2(0f, 1f);
            objRt.anchoredPosition = new Vector2(28f, -24f);
            objRt.sizeDelta = new Vector2(460f, 136f);

            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(objCard.transform, false);
            Text header = headerGo.AddComponent<Text>();
            header.font = GetFont();
            header.text = "NHIỆM VỤ · ĐO VÂN NEWTON";
            header.fontSize = 12;
            header.fontStyle = FontStyle.Bold;
            header.color = new Color(0.85f, 0.92f, 0.98f, 0.85f);
            RectTransform hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(18f, -14f);
            hrt.sizeDelta = new Vector2(424f, 20f);
            header.alignment = TextAnchor.UpperLeft;

            GameObject infoGo = new GameObject("ProblemInfo");
            infoGo.transform.SetParent(objCard.transform, false);
            objectiveInfoText = infoGo.AddComponent<Text>();
            objectiveInfoText.font = GetFont();
            objectiveInfoText.text = $"ĐỀ BÀI: i = {fringeOrderI}   k = {fringeOrderK}   ·   Vân sáng {colorNameVi} (λ = {sourceLambda:F0} nm)";
            objectiveInfoText.fontSize = 13;
            objectiveInfoText.fontStyle = FontStyle.Bold;
            objectiveInfoText.color = new Color(spectralAccent.r, spectralAccent.g, spectralAccent.b, 0.95f);
            RectTransform irt = infoGo.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(18f, -36f);
            irt.sizeDelta = new Vector2(424f, 22f);
            objectiveInfoText.alignment = TextAnchor.UpperLeft;

            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(objCard.transform, false);
            objectiveTitleText = titleGo.AddComponent<Text>();
            objectiveTitleText.font = GetFont();
            objectiveTitleText.text = $"CHỌN K TRÊN VÂN k = {fringeOrderK}";
            objectiveTitleText.fontSize = 18;
            objectiveTitleText.fontStyle = FontStyle.Bold;
            objectiveTitleText.color = new Color(0.96f, 0.98f, 1f, 1f);
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(18f, -62f);
            trt.sizeDelta = new Vector2(424f, 28f);
            objectiveTitleText.alignment = TextAnchor.UpperLeft;

            GameObject subGo = new GameObject("Sub");
            subGo.transform.SetParent(objCard.transform, false);
            objectiveSubText = subGo.AddComponent<Text>();
            objectiveSubText.font = GetFont();
            objectiveSubText.text = "Đứng trên thanh K và nhấn X để đánh dấu";
            objectiveSubText.fontSize = 13;
            objectiveSubText.color = new Color(0.72f, 0.82f, 0.90f, 0.78f);
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(18f, -94f);
            srt.sizeDelta = new Vector2(424f, 30f);
            objectiveSubText.alignment = TextAnchor.UpperLeft;

            // Feedback Text (Top-center so it never covers the rings)
            GameObject fbGo = new GameObject("FeedbackText");
            fbGo.transform.SetParent(hudCanvasGo.transform, false);
            feedbackText = fbGo.AddComponent<Text>();
            feedbackText.font = GetFont();
            feedbackText.text = string.Empty;
            feedbackText.fontSize = 22;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = new Color(0.95f, 0.98f, 1f, 1f);
            Outline fbOut = fbGo.AddComponent<Outline>();
            fbOut.effectColor = new Color(0f, 0f, 0f, 0.8f);
            fbOut.effectDistance = new Vector2(2f, -2f);
            RectTransform fbrt = fbGo.GetComponent<RectTransform>();
            fbrt.anchorMin = new Vector2(0.5f, 1f);
            fbrt.anchorMax = new Vector2(0.5f, 1f);
            fbrt.pivot = new Vector2(0.5f, 1f);
            fbrt.anchoredPosition = new Vector2(0f, -24f);
            fbrt.sizeDelta = new Vector2(720f, 48f);

            // Control Bar docked directly under ObjectiveCard on Top-Left so bottom Newton rings stay 100% clear
            bottomBarGo = new GameObject("BottomBar");
            bottomBarGo.transform.SetParent(hudCanvasGo.transform, false);
            Image barBg = bottomBarGo.AddComponent<Image>();
            barBg.color = new Color(0.02f, 0.04f, 0.07f, 0.82f);
            Outline barOut = bottomBarGo.AddComponent<Outline>();
            barOut.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.18f);
            barOut.effectDistance = new Vector2(1f, -1f);
            RectTransform brt = bottomBarGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(28f, -168f);
            brt.sizeDelta = new Vector2(460f, 42f);

            GameObject barTextGo = new GameObject("Controls");
            barTextGo.transform.SetParent(bottomBarGo.transform, false);
            Text barText = barTextGo.AddComponent<Text>();
            barText.font = GetFont();
            barText.text = "A/D: Di chuyển · SPACE: Nhảy · ←/→: Đổi pha · X: Đánh dấu";
            barText.fontSize = 13;
            barText.fontStyle = FontStyle.Bold;
            barText.alignment = TextAnchor.MiddleCenter;
            barText.color = new Color(0.85f, 0.92f, 0.98f, 0.9f);
            RectTransform btrt = barTextGo.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.sizeDelta = Vector2.zero;

            // Calculation Results Card (Phase D & E)
            BuildCalculationPanel();

            // Completion Overlay (Section 6)
            BuildCompletionPanel();

            hudCanvasGo.SetActive(false);
        }

        private void BuildCalculationPanel()
        {
            Color spectralCol = NewtonRingsPresentation.WavelengthToRgb(sourceLambda);
            string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            Color spectralAccent = Color.Lerp(spectralCol, Color.white, 0.25f);

            calculationPanelGo = new GameObject("CalculationPanel");
            calculationPanelGo.transform.SetParent(hudCanvasGo.transform, false);
            Image bg = calculationPanelGo.AddComponent<Image>();
            bg.color = new Color(0.015f, 0.03f, 0.055f, 0.90f);
            Outline outl = calculationPanelGo.AddComponent<Outline>();
            outl.effectColor = new Color(spectralAccent.r, spectralAccent.g, spectralAccent.b, 0.38f);
            outl.effectDistance = new Vector2(1.5f, -1.5f);

            calcCanvasGroup = calculationPanelGo.AddComponent<CanvasGroup>();
            calcCanvasGroup.alpha = 0f;

            // Docked on Top-Left below ObjectiveCard so it never covers the Newton rings
            RectTransform rt = calculationPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -168f);
            rt.sizeDelta = new Vector2(460f, 168f);

            // 1. Formula line
            GameObject fGo = new GameObject("FormulaText");
            fGo.transform.SetParent(calculationPanelGo.transform, false);
            calcFormulaText = fGo.AddComponent<Text>();
            calcFormulaText.font = GetFont();
            calcFormulaText.text = "B = KI = r_k + r_i      b = IK′ = r_k − r_i";
            calcFormulaText.fontSize = 15;
            calcFormulaText.fontStyle = FontStyle.Bold;
            calcFormulaText.alignment = TextAnchor.MiddleCenter;
            calcFormulaText.color = new Color(0.88f, 0.94f, 0.98f, 0.95f);
            RectTransform frt = fGo.GetComponent<RectTransform>();
            frt.anchoredPosition = new Vector2(0f, 52f);
            frt.sizeDelta = new Vector2(436f, 26f);

            // 2. Measured values line
            GameObject vGo = new GameObject("ValuesText");
            vGo.transform.SetParent(calculationPanelGo.transform, false);
            calcValuesText = vGo.AddComponent<Text>();
            calcValuesText.font = GetFont();
            calcValuesText.text = $"B = 0.000 mm   b = 0.000 mm   R = {radiusOfCurvatureMm:F0} mm   i = {fringeOrderI}   k = {fringeOrderK}";
            calcValuesText.fontSize = 13;
            calcValuesText.alignment = TextAnchor.MiddleCenter;
            calcValuesText.color = new Color(0.72f, 0.84f, 0.92f, 0.88f);
            RectTransform vrt = vGo.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(0f, 22f);
            vrt.sizeDelta = new Vector2(436f, 24f);

            // 3. Equation line
            GameObject eGo = new GameObject("EquationText");
            eGo.transform.SetParent(calculationPanelGo.transform, false);
            calcEquationText = eGo.AddComponent<Text>();
            calcEquationText.font = GetFont();
            calcEquationText.text = "λ = (B × b) / [|k − i| × R]";
            calcEquationText.fontSize = 17;
            calcEquationText.fontStyle = FontStyle.Bold;
            calcEquationText.alignment = TextAnchor.MiddleCenter;
            calcEquationText.color = new Color(0.92f, 0.96f, 1f, 1f);
            RectTransform ert = eGo.GetComponent<RectTransform>();
            ert.anchoredPosition = new Vector2(0f, -8f);
            ert.sizeDelta = new Vector2(436f, 26f);

            // 4. Result line
            GameObject rGo = new GameObject("ResultText");
            rGo.transform.SetParent(calculationPanelGo.transform, false);
            calcResultText = rGo.AddComponent<Text>();
            calcResultText.font = GetFont();
            calcResultText.text = $"λ ≈ {sourceLambda:F1} nm · Ánh sáng {colorNameVi}";
            calcResultText.fontSize = 22;
            calcResultText.fontStyle = FontStyle.Bold;
            calcResultText.alignment = TextAnchor.MiddleCenter;
            calcResultText.color = spectralAccent;
            Outline rOut = rGo.AddComponent<Outline>();
            rOut.effectColor = new Color(spectralCol.r * 0.5f, spectralCol.g * 0.5f, spectralCol.b * 0.5f, 0.65f);
            rOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform rrt = rGo.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0f, -46f);
            rrt.sizeDelta = new Vector2(436f, 36f);

            calculationPanelGo.SetActive(false);
        }

        private void BuildCompletionPanel()
        {
            completionPanelGo = new GameObject("CompletionPanel");
            completionPanelGo.transform.SetParent(hudCanvasGo.transform, false);
            Image bg = completionPanelGo.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.02f, 0.04f, 0.95f);
            RectTransform rt = completionPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Content Container
            GameObject box = new GameObject("Box");
            box.transform.SetParent(completionPanelGo.transform, false);
            Image boxBg = box.AddComponent<Image>();
            boxBg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            Outline outl = box.AddComponent<Outline>();
            outl.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.35f);
            outl.effectDistance = new Vector2(2f, -2f);
            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(880f, 480f);
            boxRt.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box.transform, false);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = "MEASUREMENT COMPLETE";
            title.fontSize = 52;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.96f, 0.98f, 1f, 1f);
            Outline tOut = titleGo.AddComponent<Outline>();
            tOut.effectColor = new Color(0.5f, 0.8f, 1f, 0.4f);
            tOut.effectDistance = new Vector2(2f, -2f);
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 145f);
            trt.sizeDelta = new Vector2(800f, 70f);

            // Subtitle
            GameObject subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(box.transform, false);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = "HOÀN THÀNH PHÉP ĐO QUANG HỌC VÂN TRÒN NEWTON";
            sub.fontSize = 17;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.72f, 0.84f, 0.92f, 0.85f);
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, 85f);
            srt.sizeDelta = new Vector2(800f, 32f);

            // Detail line
            string compColorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(sourceLambda);
            GameObject detGo = new GameObject("Detail");
            detGo.transform.SetParent(box.transform, false);
            completionDetailText = detGo.AddComponent<Text>();
            completionDetailText.font = GetFont();
            completionDetailText.text = $"Xác lập trục K–O · Đo đối xứng qua tâm · Bước sóng λ ≈ {sourceLambda:F1} nm (Ánh sáng {compColorNameVi})";
            completionDetailText.fontSize = 16;
            completionDetailText.fontStyle = FontStyle.Italic;
            completionDetailText.alignment = TextAnchor.MiddleCenter;
            completionDetailText.color = Color.Lerp(NewtonRingsPresentation.WavelengthToRgb(sourceLambda), Color.white, 0.35f);
            RectTransform drt = detGo.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, 35f);
            drt.sizeDelta = new Vector2(800f, 30f);

            // Buttons Row
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(box.transform, false);
            RectTransform rowRt = btnRow.AddComponent<RectTransform>();
            rowRt.anchoredPosition = new Vector2(0f, -80f);
            rowRt.sizeDelta = new Vector2(650f, 80f);

            // Button 1: PLAY AGAIN
            CreateButton(
                btnRow.transform,
                "PLAY AGAIN",
                "CHƠI LẠI MÀN NÀY",
                new Vector2(-160f, 0f),
                OnPlayAgainClicked);

            // Button 2: RETURN TO LEVEL SELECT
            CreateButton(
                btnRow.transform,
                "LEVEL SELECT",
                "CHỌN MÀN CHƠI",
                new Vector2(160f, 0f),
                OnReturnToLevelSelectClicked);

            completionPanelGo.SetActive(false);
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            string sub,
            Vector2 pos,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(parent, false);

            Image img = btnGo.AddComponent<Image>();
            img.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.40f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button button = btnGo.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(callback);

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.08f, 0.14f, 0.22f, 0.95f);
            cb.highlightedColor = new Color(0.15f, 0.24f, 0.36f, 1f);
            cb.pressedColor = new Color(0.22f, 0.34f, 0.48f, 1f);
            button.colors = cb;

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(280f, 72f);
            btnRt.anchoredPosition = pos;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 19;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.98f, 1f, 1f);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 10f);
            trt.sizeDelta = new Vector2(260f, 32f);

            GameObject subGo = new GameObject("Sub");
            subGo.transform.SetParent(btnGo.transform, false);
            Text subText = subGo.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = sub;
            subText.fontSize = 12;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.72f, 0.82f, 0.90f, 0.70f);
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, -15f);
            srt.sizeDelta = new Vector2(260f, 22f);

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
                player = FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            if (cameraFollow == null)
                cameraFollow = FindAnyObjectByType<CameraFollow>();

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

            // Check dock mode activation: starts orbit when player first lands on platform K
            if (orbitStartMode == "dock" && !dockActive)
            {
                bool isGroundedOnK = (player != null && player.IsGrounded && player.GroundedPlatform == platformK) || IsPlayerStandingOn(platformK);
                if (isGroundedOnK)
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
            if (player != null && player.IsTestModeActive)
                return false;

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
            if (objectiveTitleText == null) return;

            switch (CurrentStep)
            {
                case NewtonFinaleStep.CaptureK:
                    objectiveTitleText.text = $"CHỌN K TRÊN VÂN k = {fringeOrderK}";
                    objectiveSubText.text = "Đứng trên thanh K và nhấn X để đánh dấu";
                    break;
                case NewtonFinaleStep.CaptureI:
                    objectiveTitleText.text = $"TÌM I TRÊN VÂN i = {fringeOrderI}";
                    objectiveSubText.text = "Đứng trên thanh I, căn đúng trục K–O và nhấn X";
                    break;
                case NewtonFinaleStep.CaptureKp:
                    objectiveTitleText.text = $"TÌM K′ TRÊN VÂN k = {fringeOrderK}";
                    objectiveSubText.text = "Quay lại thanh K, căn phía đối diện và nhấn X";
                    break;
                case NewtonFinaleStep.Reveal:
                case NewtonFinaleStep.Complete:
                    objectiveTitleText.text = "ĐÃ HOÀN THÀNH K – I – K′";
                    objectiveSubText.text = "Xác lập trục K–O và tính toán bước sóng";
                    break;
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

            if (player != null && player.IsTestModeActive)
            {
                LastMarkRejectionReason = "TEST_MODE_ACTIVE";
                return;
            }

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
                calcResultText.text = $"λ ≈ {CalculatedLambda_nm:F1} nm · Ánh sáng {colorNameVi}";
            }
            if (completionDetailText != null)
            {
                completionDetailText.text = $"B = {MeasuredB_mm:F3} mm · b = {Measuredb_mm:F3} mm · Bước sóng λ ≈ {CalculatedLambda_nm:F1} nm (Ánh sáng {colorNameVi})";
            }

            if (targetICrosshair != null) targetICrosshair.SetActive(false);
            if (targetKpCrosshair != null) targetKpCrosshair.SetActive(false);
            if (objectiveCardGo != null) objectiveCardGo.SetActive(false);
            if (bottomBarGo != null) bottomBarGo.SetActive(false);
            if (calculationPanelGo != null)
            {
                RectTransform calcRt = calculationPanelGo.GetComponent<RectTransform>();
                if (calcRt != null)
                    calcRt.anchoredPosition = new Vector2(28f, -24f);
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
                    string colorNameVi = NewtonRingsPresentation.GetWavelengthColorNameVi(CalculatedLambda_nm);
                    calcResultText.text = $"λ ≈ {CalculatedLambda_nm:F1} nm · Ánh sáng {colorNameVi}";
                }
            }

            // Phase E — Result hold (2.55 - 5.6s), then Complete
            if (tm >= 5.6f)
            {
                if (CurrentStep != NewtonFinaleStep.Complete)
                {
                    CurrentStep = NewtonFinaleStep.Complete;
                    if (GameManager.Instance != null && GameManager.Instance.CurrentEntryContext == GameplayEntryContext.Campaign)
                    {
                        GameManager.Instance.OnPlayerWon();
                    }
                    else
                    {
                        if (completionPanelGo != null)
                            completionPanelGo.SetActive(true);
                        GameManager.Instance?.OnPlayerWon();
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
            Debug.Log("[NEWTON_FINALE] Play Again clicked.");
            ResetState();
            GameManager.Instance?.Restart();
        }

        public void OnReturnToLevelSelectClicked()
        {
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
            if (calculationPanelGo != null)
            {
                calculationPanelGo.SetActive(false);
                RectTransform calcRt = calculationPanelGo.GetComponent<RectTransform>();
                if (calcRt != null)
                    calcRt.anchoredPosition = new Vector2(28f, -168f);
            }
            if (completionPanelGo != null) completionPanelGo.SetActive(false);
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
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }
    }
}
