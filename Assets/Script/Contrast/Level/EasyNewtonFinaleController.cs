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

        // Calculation result
        public float MeasuredB_units { get; private set; }
        public float Measuredb_units { get; private set; }
        public float MeasuredB_mm { get; private set; }
        public float Measuredb_mm { get; private set; }
        public float CalculatedLambda_nm { get; private set; }

        // Presentation & UI
        private GameObject hudCanvasGo;
        private Canvas hudCanvas;
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

            EnsureHierarchyRoots();
            SpawnApparatus();
            SpawnOrbitPlatforms();
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
            return ringScale * Mathf.Sqrt(order * (sourceLambda / referenceLambda));
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

            platformK.Initialize(initPosK, platformSizeK, 255f); // White
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
            labelPlatformK = CreatePlatformFloatingLabel(platformK.transform, "k = 4");
            labelPlatformI = CreatePlatformFloatingLabel(platformI.transform, "i = 2");
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

            // Objective Card (Top Left)
            GameObject objCard = new GameObject("ObjectiveCard");
            objCard.transform.SetParent(hudCanvasGo.transform, false);
            Image objBg = objCard.AddComponent<Image>();
            objBg.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            Outline objOut = objCard.AddComponent<Outline>();
            objOut.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.22f);
            objOut.effectDistance = new Vector2(1f, -1f);
            RectTransform objRt = objCard.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 1f);
            objRt.anchorMax = new Vector2(0f, 1f);
            objRt.pivot = new Vector2(0f, 1f);
            objRt.anchoredPosition = new Vector2(40f, -40f);
            objRt.sizeDelta = new Vector2(480f, 130f);

            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(objCard.transform, false);
            Text header = headerGo.AddComponent<Text>();
            header.font = GetFont();
            header.text = "NHIỆM VỤ · ĐO VÂN NEWTON (EASY FINALE)";
            header.fontSize = 13;
            header.fontStyle = FontStyle.Bold;
            header.color = new Color(0.85f, 0.92f, 0.98f, 0.85f);
            RectTransform hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(20f, -18f);
            hrt.sizeDelta = new Vector2(440f, 24f);
            header.alignment = TextAnchor.UpperLeft;

            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(objCard.transform, false);
            objectiveTitleText = titleGo.AddComponent<Text>();
            objectiveTitleText.font = GetFont();
            objectiveTitleText.text = "CHỌN ĐIỂM K TRÊN THANH TRẮNG k = 4";
            objectiveTitleText.fontSize = 19;
            objectiveTitleText.fontStyle = FontStyle.Bold;
            objectiveTitleText.color = new Color(0.96f, 0.98f, 1f, 1f);
            RectTransform trt = titleGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(20f, -48f);
            trt.sizeDelta = new Vector2(440f, 32f);
            objectiveTitleText.alignment = TextAnchor.UpperLeft;

            GameObject subGo = new GameObject("Sub");
            subGo.transform.SetParent(objCard.transform, false);
            objectiveSubText = subGo.AddComponent<Text>();
            objectiveSubText.font = GetFont();
            objectiveSubText.text = "Đứng trên thanh k · Nhấn Space để khóa K và xác lập trục K–O";
            objectiveSubText.fontSize = 14;
            objectiveSubText.color = new Color(0.72f, 0.82f, 0.90f, 0.75f);
            RectTransform srt = subGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(20f, -84f);
            srt.sizeDelta = new Vector2(440f, 34f);
            objectiveSubText.alignment = TextAnchor.UpperLeft;

            // Feedback Text (Center screen)
            GameObject fbGo = new GameObject("FeedbackText");
            fbGo.transform.SetParent(hudCanvasGo.transform, false);
            feedbackText = fbGo.AddComponent<Text>();
            feedbackText.font = GetFont();
            feedbackText.text = string.Empty;
            feedbackText.fontSize = 24;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = new Color(0.95f, 0.98f, 1f, 1f);
            Outline fbOut = fbGo.AddComponent<Outline>();
            fbOut.effectColor = new Color(0f, 0f, 0f, 0.8f);
            fbOut.effectDistance = new Vector2(2f, -2f);
            RectTransform fbrt = fbGo.GetComponent<RectTransform>();
            fbrt.anchoredPosition = new Vector2(0f, 180f);
            fbrt.sizeDelta = new Vector2(800f, 60f);

            // Bottom Control Bar
            bottomBarGo = new GameObject("BottomBar");
            bottomBarGo.transform.SetParent(hudCanvasGo.transform, false);
            Image barBg = bottomBarGo.AddComponent<Image>();
            barBg.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            Outline barOut = bottomBarGo.AddComponent<Outline>();
            barOut.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.20f);
            barOut.effectDistance = new Vector2(1f, -1f);
            RectTransform brt = bottomBarGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 30f);
            brt.sizeDelta = new Vector2(840f, 52f);

            GameObject barTextGo = new GameObject("Controls");
            barTextGo.transform.SetParent(bottomBarGo.transform, false);
            Text barText = barTextGo.AddComponent<Text>();
            barText.font = GetFont();
            barText.text = "A / D  DI CHUYỂN   ·   W / SPACE  NHẢY   ·   S  FALL   ·   ← / →  ĐỔI PHA   ·   X  ĐO / KHÓA ĐIỂM";
            barText.fontSize = 15;
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
            calculationPanelGo = new GameObject("CalculationPanel");
            calculationPanelGo.transform.SetParent(hudCanvasGo.transform, false);
            Image bg = calculationPanelGo.AddComponent<Image>();
            bg.color = new Color(0.015f, 0.03f, 0.055f, 0.92f);
            Outline outl = calculationPanelGo.AddComponent<Outline>();
            outl.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.35f);
            outl.effectDistance = new Vector2(1.5f, -1.5f);

            calcCanvasGroup = calculationPanelGo.AddComponent<CanvasGroup>();
            calcCanvasGroup.alpha = 0f;

            RectTransform rt = calculationPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta = new Vector2(880f, 180f);

            // 1. Formula line
            GameObject fGo = new GameObject("FormulaText");
            fGo.transform.SetParent(calculationPanelGo.transform, false);
            calcFormulaText = fGo.AddComponent<Text>();
            calcFormulaText.font = GetFont();
            calcFormulaText.text = "B = KI = r_k + r_i        b = IK′ = r_k − r_i";
            calcFormulaText.fontSize = 18;
            calcFormulaText.fontStyle = FontStyle.Bold;
            calcFormulaText.alignment = TextAnchor.MiddleCenter;
            calcFormulaText.color = new Color(0.88f, 0.94f, 0.98f, 0.95f);
            RectTransform frt = fGo.GetComponent<RectTransform>();
            frt.anchoredPosition = new Vector2(0f, 55f);
            frt.sizeDelta = new Vector2(840f, 30f);

            // 2. Measured values line
            GameObject vGo = new GameObject("ValuesText");
            vGo.transform.SetParent(calculationPanelGo.transform, false);
            calcValuesText = vGo.AddComponent<Text>();
            calcValuesText.font = GetFont();
            calcValuesText.text = "B = 0.000 mm    b = 0.000 mm    R = 855 mm    i = 2    k = 4";
            calcValuesText.fontSize = 15;
            calcValuesText.alignment = TextAnchor.MiddleCenter;
            calcValuesText.color = new Color(0.72f, 0.84f, 0.92f, 0.85f);
            RectTransform vrt = vGo.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(0f, 24f);
            vrt.sizeDelta = new Vector2(840f, 26f);

            // 3. Equation line
            GameObject eGo = new GameObject("EquationText");
            eGo.transform.SetParent(calculationPanelGo.transform, false);
            calcEquationText = eGo.AddComponent<Text>();
            calcEquationText.font = GetFont();
            calcEquationText.text = "λ = (B × b) / [|k − i| × R]";
            calcEquationText.fontSize = 20;
            calcEquationText.fontStyle = FontStyle.Bold;
            calcEquationText.alignment = TextAnchor.MiddleCenter;
            calcEquationText.color = new Color(0.92f, 0.96f, 1f, 1f);
            RectTransform ert = eGo.GetComponent<RectTransform>();
            ert.anchoredPosition = new Vector2(0f, -8f);
            ert.sizeDelta = new Vector2(840f, 30f);

            // 4. Result line
            GameObject rGo = new GameObject("ResultText");
            rGo.transform.SetParent(calculationPanelGo.transform, false);
            calcResultText = rGo.AddComponent<Text>();
            calcResultText.font = GetFont();
            calcResultText.text = "λ ≈ 550.0 nm";
            calcResultText.fontSize = 28;
            calcResultText.fontStyle = FontStyle.Bold;
            calcResultText.alignment = TextAnchor.MiddleCenter;
            calcResultText.color = new Color(0.96f, 0.98f, 1f, 1f);
            Outline rOut = rGo.AddComponent<Outline>();
            rOut.effectColor = new Color(0.4f, 0.75f, 1f, 0.5f);
            rOut.effectDistance = new Vector2(2f, -2f);
            RectTransform rrt = rGo.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0f, -48f);
            rrt.sizeDelta = new Vector2(840f, 40f);

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
            GameObject detGo = new GameObject("Detail");
            detGo.transform.SetParent(box.transform, false);
            Text det = detGo.AddComponent<Text>();
            det.font = GetFont();
            det.text = "Xác lập trục K–O · Đo đối xứng qua tâm · Tính bước sóng λ ≈ 550.0 nm";
            det.fontSize = 16;
            det.fontStyle = FontStyle.Italic;
            det.alignment = TextAnchor.MiddleCenter;
            det.color = new Color(0.85f, 0.92f, 0.98f, 0.95f);
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

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            if (cameraFollow == null)
                cameraFollow = FindAnyObjectByType<CameraFollow>();

            // Handle proximity to finale chamber
            float px = player.transform.position.x;
            float chamberZoneThreshold = centerO.x - ringScale * 4.25f;
            bool inZone = px >= chamberZoneThreshold;
            if (inZone != isZoneActive)
            {
                isZoneActive = inZone;
                if (hudCanvasGo != null)
                    hudCanvasGo.SetActive(isZoneActive);
            }

            // Check dock mode activation: starts orbit when player first lands on platform K
            if (orbitStartMode == "dock" && !dockActive)
            {
                if (player != null && player.IsGrounded && player.GroundedPlatform == platformK)
                {
                    dockActive = true;
                    if (trajK != null) trajK.enabled = true;
                    if (trajI != null) trajI.enabled = true;
                    Debug.Log("[NEWTON_FINALE] Player docked on K -> Orbit motion started.");
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

            // Check point capture trigger (X key down or mobile mark button)
            if (IsNewtonMarkPressedThisFrame())
            {
                AttemptCapture();
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

            if (targetICrosshair != null)
            {
                targetICrosshair.transform.position = new Vector3(targetI.x, targetI.y, -1f);
                targetICrosshair.SetActive(CurrentStep == NewtonFinaleStep.CaptureI);
            }

            if (targetKpCrosshair != null)
            {
                targetKpCrosshair.transform.position = new Vector3(targetKp.x, targetKp.y, -1f);
                targetKpCrosshair.SetActive(CurrentStep == NewtonFinaleStep.CaptureKp);
            }
        }

        public void AttemptCapture()
        {
            if (CurrentStep >= NewtonFinaleStep.Reveal)
                return;

            if (player == null || !player.IsGrounded)
            {
                ShowFeedback("HÃY ĐỨNG TRÊN THANH ĐỂ ĐO · STAND ON PLATFORM");
                return;
            }

            ColorPlatform currentPlat = player.GroundedPlatform;

            switch (CurrentStep)
            {
                case NewtonFinaleStep.CaptureK:
                    CaptureStepK(currentPlat);
                    break;
                case NewtonFinaleStep.CaptureI:
                    CaptureStepI(currentPlat);
                    break;
                case NewtonFinaleStep.CaptureKp:
                    CaptureStepKp(currentPlat);
                    break;
            }
        }

        private void CaptureStepK(ColorPlatform currentPlat)
        {
            if (currentPlat != platformK)
            {
                ShowFeedback($"HÃY ĐỨNG TRÊN THANH K (k = {fringeOrderK})");
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
            ShowFeedback("ĐÃ KHÓA K · TRỤC K–O ĐƯỢC XÁC LẬP");
        }

        private void CaptureStepI(ColorPlatform currentPlat)
        {
            if (currentPlat != platformI)
            {
                ShowFeedback($"HÃY ĐỨNG TRÊN THANH I (i = {fringeOrderI})");
                return;
            }

            float rI = GetRadiusForOrder(fringeOrderI);
            Vector2 targetI = centerO - AxisU * rI;

            Vector2 curPos = platformI.transform.position;
            float dist = Vector2.Distance(curPos, targetI);

            if (dist <= toleranceI)
            {
                PointI = targetI;
                HasI = true;

                markerIGo.transform.position = new Vector3(PointI.x, PointI.y, -2f);
                markerIGo.SetActive(true);

                int accuracy = Mathf.Max(0, Mathf.RoundToInt(100f * (1f - dist / toleranceI)));
                CurrentStep = NewtonFinaleStep.CaptureKp;
                ShowFeedback($"I ĐÃ KHÓA · {accuracy}%");
            }
            else
            {
                ShowFeedback("CHỜ I THẲNG HÀNG VỚI TRỤC K–O");
            }
        }

        private void CaptureStepKp(ColorPlatform currentPlat)
        {
            if (currentPlat != platformK)
            {
                ShowFeedback($"HÃY TRỞ LẠI THANH K (k = {fringeOrderK})");
                return;
            }

            float rK = GetRadiusForOrder(fringeOrderK);
            Vector2 targetKp = centerO - AxisU * rK;

            Vector2 curPos = platformK.transform.position;
            float dist = Vector2.Distance(curPos, targetKp);

            if (dist <= toleranceKp)
            {
                PointKp = targetKp;
                HasKp = true;

                markerKpGo.transform.position = new Vector3(PointKp.x, PointKp.y, -2f);
                markerKpGo.SetActive(true);

                int accuracy = Mathf.Max(0, Mathf.RoundToInt(100f * (1f - dist / toleranceKp)));
                ShowFeedback($"K′ ĐÃ KHÓA · {accuracy}% · BẮT ĐẦU HIỂN THỊ KẾT QUẢ ĐO");
                StartRevealSequence();
            }
            else
            {
                ShowFeedback("CHỜ K′ THẲNG HÀNG VỚI TRỤC K–O");
            }
        }

        public void StartRevealSequence()
        {
            CurrentStep = NewtonFinaleStep.Reveal;
            RevealTimer = 0f;

            if (player != null)
            {
                player.SetInputEnabled(false);
                player.StopMovement();
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetOverride(new Vector3(centerO.x, centerO.y, -10f), 9.6f, 3.2f);
            }

            ComputePhysicalWavelength();

            if (targetICrosshair != null) targetICrosshair.SetActive(false);
            if (targetKpCrosshair != null) targetKpCrosshair.SetActive(false);
            if (bottomBarGo != null) bottomBarGo.SetActive(false);
        }

        public void ComputePhysicalWavelength()
        {
            MeasuredB_units = Vector2.Distance(PointK, PointI);
            Measuredb_units = Vector2.Distance(PointI, PointKp);

            // Physical conversion factor
            // In reference Easy configuration: ringScale = 220 px / 64 = 3.4375 units
            // physical r_1 = sqrt(lambda_ref * R) = sqrt(550e-6 mm * 855 mm) ~ 0.685747767 mm
            // mmPerUnit = physical r_1 / ringScale = 0.685747767 / 3.4375 ~ 0.19949026 mm/unit
            float lambdaRefMm = referenceLambda * 1e-6f;
            float physicalR1Mm = Mathf.Sqrt(lambdaRefMm * radiusOfCurvatureMm);
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
                    calcValuesText.text = $"B = {MeasuredB_mm:F3} mm    b = {Measuredb_mm:F3} mm    R = {radiusOfCurvatureMm:F0} mm    i = {fringeOrderI}    k = {fringeOrderK}";
                }

                if (calcResultText != null)
                {
                    calcResultText.text = $"λ ≈ {CalculatedLambda_nm:F1} nm";
                }
            }

            // Phase E — Result hold (2.55 - 5.6s), then Complete
            if (tm >= 5.6f)
            {
                CurrentStep = NewtonFinaleStep.Complete;
                if (completionPanelGo != null)
                    completionPanelGo.SetActive(true);
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
            GameManager.Instance?.StartGame();
        }

        public void ResetState()
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
            if (calculationPanelGo != null) calculationPanelGo.SetActive(false);
            if (completionPanelGo != null) completionPanelGo.SetActive(false);
            if (bottomBarGo != null) bottomBarGo.SetActive(true);

            if (cameraFollow != null)
                cameraFollow.ClearOverride();

            if (player != null)
                player.SetInputEnabled(true);

            if (trajK != null) trajK.ResetPhase();
            if (trajI != null) trajI.ResetPhase();
        }

        public void Teardown()
        {
            if (Instance == this)
                Instance = null;

            PlayerInput.IsNewtonFinaleActive = false;

            if (cameraFollow != null)
                cameraFollow.ClearOverride();

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
