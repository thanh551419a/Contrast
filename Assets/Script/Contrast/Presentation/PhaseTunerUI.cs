namespace Contrast.Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using Contrast.Color;
    using Contrast.Core;
    using Contrast.Player;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only UI reproducing the Wave-First Cyclic Phase Tuner HUD,
    /// animated dual-waveform interference graph (Wave A fixed vs Wave B phase-shifted),
    /// cyclic ombre strip, status badge, control hint card, and checkpoint toast
    /// from demo_quangbai1.html.
    ///
    /// Does NOT modify any player state or gameplay rules.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhaseTunerUI : MonoBehaviour
    {
        private static PhaseTunerUI instance;
        public static PhaseTunerUI Instance => instance;

        private GameObject tunerPanel;
        private RawImage waveGraphImage;
        private Texture2D waveTexture;
        private Color32[] wavePixels;
        private const int WaveTexWidth = 280;
        private const int WaveTexHeight = 80;

        private RawImage ombreStripImage;
        private Texture2D ombreTexture;
        private RectTransform cursorMarker;
        private readonly RectTransform[] phaseDividers = new RectTransform[4];

        public RectTransform[] PhaseDividers => phaseDividers;
        public RectTransform CursorMarker => cursorMarker;
        public RectTransform TunerPanelRect => tunerPanel != null ? tunerPanel.GetComponent<RectTransform>() : null;
        public GameObject HintPanelObject => hintPanel;

        private static readonly Vector2 NormalTunerAnchoredPos = new Vector2(0f, 18f);
        private static readonly Vector2 NewtonTunerAnchoredPos = new Vector2(-680f, 14f);
        private const float NormalTunerScale = 1.0f;
        private const float NewtonTunerScale = 0.82f;

        private Text stateBadgeTitle;
        private Text stateBadgeRelation;
        private Image stateBadgeBg;

        // Control hint card
        private GameObject hintPanel;

        // Checkpoint toast
        private GameObject checkpointToast;
        private Text checkpointToastText;
        private CanvasGroup checkpointToastGroup;
        private float checkpointFlashTimer = 0f;

        // Quang Điểm HUD counter card
        private GameObject quangHudCard;
        private Text quangHudValueText;

        private PlayerController player;
        private Contrast.Camera.CameraFollow cachedCameraFollow;
        private float simulatedPhase = 0f;

        private void Awake()
        {
            instance = this;
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (tunerPanel != null)
                return;
            BuildUI();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        public void TriggerCheckpointFlash()
        {
            checkpointFlashTimer = 2.0f;
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("ContrastPresentationCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80; // Above gameplay, below menus

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            BuildTunerPanel(canvasGo.transform);
            BuildControlHint(canvasGo.transform);
            BuildCheckpointToast(canvasGo.transform);
            BuildQuangDiemHud(canvasGo.transform);
        }

        private void BuildQuangDiemHud(Transform parent)
        {
            quangHudCard = new GameObject("QuangDiemHUDCard", typeof(RectTransform));
            quangHudCard.transform.SetParent(parent, false);

            Image bg = quangHudCard.AddComponent<Image>();
            bg.color = new Color(0.027f, 0.043f, 0.071f, 0.80f); // rgba(7,11,18,0.80)

            Outline outline = quangHudCard.AddComponent<Outline>();
            outline.effectColor = new Color(1.000f, 0.863f, 0.580f, 0.34f); // rgba(255,220,148,0.34)
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform rt = quangHudCard.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -96f);
            rt.sizeDelta = new Vector2(210f, 40f);

            GameObject starGo = new GameObject("StarLabel", typeof(RectTransform));
            starGo.transform.SetParent(quangHudCard.transform, false);
            Text starText = starGo.AddComponent<Text>();
            starText.font = GetFont();
            starText.text = "✦  QUANG ĐIỂM";
            starText.fontSize = 12;
            starText.fontStyle = FontStyle.Bold;
            starText.color = new Color(1.000f, 0.851f, 0.541f, 0.95f); // #ffd98a
            starText.alignment = TextAnchor.MiddleLeft;
            RectTransform starRt = starGo.GetComponent<RectTransform>();
            starRt.anchorMin = new Vector2(0f, 0f);
            starRt.anchorMax = new Vector2(0.7f, 1f);
            starRt.offsetMin = new Vector2(14f, 0f);
            starRt.offsetMax = Vector2.zero;

            GameObject valGo = new GameObject("CountValue", typeof(RectTransform));
            valGo.transform.SetParent(quangHudCard.transform, false);
            quangHudValueText = valGo.AddComponent<Text>();
            quangHudValueText.font = GetFont();
            quangHudValueText.text = "0 / 0";
            quangHudValueText.fontSize = 15;
            quangHudValueText.fontStyle = FontStyle.Bold;
            quangHudValueText.color = new Color(1.000f, 0.961f, 0.839f, 0.98f);
            quangHudValueText.alignment = TextAnchor.MiddleRight;
            RectTransform valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.55f, 0f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = new Vector2(-14f, 0f);
        }

        private void BuildTunerPanel(Transform parent)
        {
            // Tuner Card: Bottom Center (540 x 150)
            tunerPanel = new GameObject("PhaseTunerPanel", typeof(RectTransform));
            tunerPanel.transform.SetParent(parent, false);

            Image panelBg = tunerPanel.AddComponent<Image>();
            panelBg.color = new Color(0.020f, 0.035f, 0.067f, 0.90f); // #050911 at 90% alpha

            RectTransform rt = tunerPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(560f, 155f);
            rt.anchoredPosition = new Vector2(0f, 18f);

            // 1px Border Outline
            Outline outline = tunerPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.804f, 0.863f, 0.902f, 0.22f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // Header Title
            GameObject titleGo = new GameObject("HeaderLabel", typeof(RectTransform));
            titleGo.transform.SetParent(tunerPanel.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "GIAO THOA · VÒNG PHA (INTERFERENCE PHASE TUNER)";
            titleText.fontSize = 11;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.88f, 0.92f, 0.94f, 0.65f);
            titleText.alignment = TextAnchor.MiddleLeft;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(18f, -12f);
            titleRt.sizeDelta = new Vector2(-36f, 18f);

            // Dual Wave Graph Image
            GameObject waveGo = new GameObject("WaveGraph", typeof(RectTransform));
            waveGo.transform.SetParent(tunerPanel.transform, false);
            waveGraphImage = waveGo.AddComponent<RawImage>();

            waveTexture = new Texture2D(WaveTexWidth, WaveTexHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            wavePixels = new Color32[WaveTexWidth * WaveTexHeight];
            waveGraphImage.texture = waveTexture;

            RectTransform waveRt = waveGo.GetComponent<RectTransform>();
            waveRt.anchorMin = new Vector2(0f, 0f);
            waveRt.anchorMax = new Vector2(0f, 0f);
            waveRt.pivot = new Vector2(0f, 0f);
            waveRt.anchoredPosition = new Vector2(70f, 42f);
            waveRt.sizeDelta = new Vector2(WaveTexWidth, WaveTexHeight);

            // Wave A and B Labels
            CreateLabel(tunerPanel.transform, "A  CỐ ĐỊNH", new Vector2(12f, 88f), 10, FontStyle.Bold, new Color(0.96f, 0.98f, 1f, 0.85f));
            CreateLabel(tunerPanel.transform, "B  DỊCH PHA", new Vector2(12f, 54f), 10, FontStyle.Bold, new Color(0.65f, 0.72f, 0.78f, 0.85f));

            // Status Badge (Right side)
            GameObject badgeGo = new GameObject("StateBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(tunerPanel.transform, false);
            stateBadgeBg = badgeGo.AddComponent<Image>();
            stateBadgeBg.color = new Color(0.12f, 0.16f, 0.22f, 0.85f);

            Outline badgeOutline = badgeGo.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.9f, 0.95f, 1f, 0.35f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);

            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 0.5f);
            badgeRt.anchorMax = new Vector2(1f, 0.5f);
            badgeRt.pivot = new Vector2(1f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(-16f, 16f);
            badgeRt.sizeDelta = new Vector2(165f, 65f);

            // Badge Title Text
            GameObject badgeTitleGo = new GameObject("Title", typeof(RectTransform));
            badgeTitleGo.transform.SetParent(badgeGo.transform, false);
            stateBadgeTitle = badgeTitleGo.AddComponent<Text>();
            stateBadgeTitle.font = GetFont();
            stateBadgeTitle.fontSize = 17;
            stateBadgeTitle.fontStyle = FontStyle.Bold;
            stateBadgeTitle.alignment = TextAnchor.MiddleCenter;
            stateBadgeTitle.color = Color.white;
            stateBadgeTitle.text = "SÁNG";

            RectTransform btRt = badgeTitleGo.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0f, 0.5f);
            btRt.anchorMax = new Vector2(1f, 1f);
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            // Badge Relation Text
            GameObject badgeRelGo = new GameObject("Relation", typeof(RectTransform));
            badgeRelGo.transform.SetParent(badgeGo.transform, false);
            stateBadgeRelation = badgeRelGo.AddComponent<Text>();
            stateBadgeRelation.font = GetFont();
            stateBadgeRelation.fontSize = 9;
            stateBadgeRelation.fontStyle = FontStyle.Normal;
            stateBadgeRelation.alignment = TextAnchor.MiddleCenter;
            stateBadgeRelation.color = new Color(0.85f, 0.90f, 0.95f, 0.70f);
            stateBadgeRelation.text = "CÙNG PHA (Δφ = 0°)";

            RectTransform brRt = badgeRelGo.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0f, 0f);
            brRt.anchorMax = new Vector2(1f, 0.5f);
            brRt.offsetMin = Vector2.zero;
            brRt.offsetMax = Vector2.zero;

            // Cyclic Ombre Strip (Bottom of card)
            GameObject ombreGo = new GameObject("OmbreStrip", typeof(RectTransform));
            ombreGo.transform.SetParent(tunerPanel.transform, false);
            ombreStripImage = ombreGo.AddComponent<RawImage>();

            const int ombreW = 240;
            ombreTexture = new Texture2D(ombreW, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            // Fill cyclic ombre gradient matching HTML stripColorForPhase(localPhase)
            Color[] ombreColors = new Color[ombreW];
            for (int i = 0; i < ombreW; i++)
            {
                float t = (i + 0.5f) / ombreW;
                float angle = t * Mathf.PI * 2f;
                ombreColors[i] = EvaluateHtmlStripColor(angle);
            }
            ombreTexture.SetPixels(ombreColors);
            ombreTexture.Apply();
            ombreStripImage.texture = ombreTexture;

            Outline ombreOutline = ombreGo.AddComponent<Outline>();
            ombreOutline.effectColor = new Color(0.922f, 0.945f, 0.957f, 0.22f); // rgba(235,241,244,0.22)
            ombreOutline.effectDistance = new Vector2(1f, -1f);

            RectTransform ombreRt = ombreGo.GetComponent<RectTransform>();
            ombreRt.anchorMin = new Vector2(0f, 0f);
            ombreRt.anchorMax = new Vector2(0f, 0f);
            ombreRt.pivot = new Vector2(0f, 0f);
            ombreRt.anchoredPosition = new Vector2(110f, 14f);
            ombreRt.sizeDelta = new Vector2(240f, 12f);

            // Phase zone boundary divider lines (BRIGHT / TUNING / DARK / TUNING)
            // Transitions occur at I = 0.78 (cos(phi) = 0.56) and I = 0.22 (cos(phi) = -0.56)
            float t1 = Mathf.Acos(0.56f) / (Mathf.PI * 2f);   // ~0.155401 (BRIGHT -> TUNING)
            float t2 = Mathf.Acos(-0.56f) / (Mathf.PI * 2f);  // ~0.344599 (TUNING -> DARK)
            float t3 = 1f - t2;                               // ~0.655401 (DARK -> TUNING)
            float t4 = 1f - t1;                               // ~0.844599 (TUNING -> BRIGHT)
            float[] dividerNormalized = { t1, t2, t3, t4 };

            for (int d = 0; d < dividerNormalized.Length; d++)
            {
                GameObject divGo = new GameObject($"PhaseDivider_{d}", typeof(RectTransform));
                divGo.transform.SetParent(ombreGo.transform, false);
                Image divImg = divGo.AddComponent<Image>();
                divImg.color = new Color(0.961f, 0.973f, 0.980f, 0.48f); // Crisp optical boundary divider

                RectTransform divRt = divGo.GetComponent<RectTransform>();
                divRt.anchorMin = new Vector2(0f, 0.5f);
                divRt.anchorMax = new Vector2(0f, 0.5f);
                divRt.pivot = new Vector2(0.5f, 0.5f);
                divRt.sizeDelta = new Vector2(1.5f, 14f);
                divRt.anchoredPosition = new Vector2(dividerNormalized[d] * ombreW, 0f);
                phaseDividers[d] = divRt;
            }

            // Cursor on ombre strip
            GameObject cursorGo = new GameObject("CursorMarker", typeof(RectTransform));
            cursorGo.transform.SetParent(ombreGo.transform, false);
            Image cursorImg = cursorGo.AddComponent<Image>();
            cursorImg.color = new Color(0.969f, 0.980f, 0.984f, 1f); // #f7fafb

            cursorMarker = cursorGo.GetComponent<RectTransform>();
            cursorMarker.anchorMin = new Vector2(0f, 0.5f);
            cursorMarker.anchorMax = new Vector2(0f, 0.5f);
            cursorMarker.pivot = new Vector2(0.5f, 0.5f);
            cursorMarker.sizeDelta = new Vector2(3f, 20f);

            CreateLabel(tunerPanel.transform, "SÁNG · ĐIỀU PHA · TỐI · ĐIỀU PHA · LẶP", new Vector2(110f, 2f), 8, FontStyle.Normal, new Color(0.85f, 0.90f, 0.95f, 0.45f));
            CreateLabel(tunerPanel.transform, "← / → ĐỔI PHA TUẦN HOÀN", new Vector2(370f, 14f), 9, FontStyle.Bold, new Color(0.88f, 0.92f, 0.95f, 0.55f));
        }

        public static Color EvaluateHtmlStripColor(float localPhase)
        {
            float I = Mathf.Clamp01(0.5f + 0.5f * Mathf.Cos(localPhase));
            float v = Mathf.Round(18f + I * 226f);
            float r = v, g = v, b = v;
            if (I >= 0.78f)
            {
                r = Mathf.Min(245f, r + 6f);
                g = Mathf.Min(247f, g + 6f);
                b = Mathf.Min(248f, b + 6f);
            }
            else if (I <= 0.22f)
            {
                r = Mathf.Max(10f, r - 6f);
                g = Mathf.Max(12f, g - 6f);
                b = Mathf.Max(14f, b - 6f);
            }
            else
            {
                r = Mathf.Round(v * 0.92f + 8f);
                g = Mathf.Round(v * 0.95f + 10f);
                b = Mathf.Round(v * 1.02f + 18f);
            }
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private void BuildControlHint(Transform parent)
        {
            // Control Hint in top-left: 280 x 140
            hintPanel = new GameObject("ControlHintCard", typeof(RectTransform));
            hintPanel.transform.SetParent(parent, false);

            Image panelBg = hintPanel.AddComponent<Image>();
            panelBg.color = new Color(0.020f, 0.035f, 0.067f, 0.75f);

            Outline outline = hintPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.804f, 0.863f, 0.902f, 0.20f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform rt = hintPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            // Controls text
            CreateLabel(hintPanel.transform, "ĐIỀU KHIỂN QUANG HỌC", new Vector2(16f, 130f), 11, FontStyle.Bold, new Color(0.95f, 0.98f, 1f, 0.9f));
            CreateLabel(hintPanel.transform, "A / D          DI CHUYỂN", new Vector2(16f, 108f), 11, FontStyle.Normal, new Color(0.85f, 0.90f, 0.95f, 0.75f));
            CreateLabel(hintPanel.transform, "SPACE / W      NHẢY", new Vector2(16f, 88f), 11, FontStyle.Normal, new Color(0.85f, 0.90f, 0.95f, 0.75f));
            CreateLabel(hintPanel.transform, "S              FAST FALL", new Vector2(16f, 68f), 11, FontStyle.Normal, new Color(0.85f, 0.90f, 0.95f, 0.75f));
            CreateLabel(hintPanel.transform, "← / →          ĐỔI PHA (TUNING)", new Vector2(16f, 48f), 11, FontStyle.Normal, new Color(0.85f, 0.90f, 0.95f, 0.75f));
            CreateLabel(hintPanel.transform, "X              ĐO VÂN NEWTON", new Vector2(16f, 28f), 11, FontStyle.Normal, new Color(1f, 0.88f, 0.45f, 0.85f));
            CreateLabel(hintPanel.transform, "R              RESTART LEVEL", new Vector2(16f, 10f), 10, FontStyle.Normal, new Color(0.75f, 0.82f, 0.88f, 0.50f));
        }

        private void BuildCheckpointToast(Transform parent)
        {
            checkpointToast = new GameObject("CheckpointToast", typeof(RectTransform));
            checkpointToast.transform.SetParent(parent, false);

            checkpointToastGroup = checkpointToast.AddComponent<CanvasGroup>();
            checkpointToastGroup.alpha = 0f;

            Image bg = checkpointToast.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);

            Outline outline = checkpointToast.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.95f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform rt = checkpointToast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -45f);
            rt.sizeDelta = new Vector2(360f, 44f);

            GameObject textGo = new GameObject("ToastText", typeof(RectTransform));
            textGo.transform.SetParent(checkpointToast.transform, false);
            checkpointToastText = textGo.AddComponent<Text>();
            checkpointToastText.font = GetFont();
            checkpointToastText.fontSize = 13;
            checkpointToastText.fontStyle = FontStyle.Bold;
            checkpointToastText.alignment = TextAnchor.MiddleCenter;
            checkpointToastText.color = new Color(0.92f, 0.97f, 1f, 1f);
            checkpointToastText.text = "✦  ĐÃ LƯU ĐIỂM HỒI SINH  ✦";

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateLabel(Transform parent, string text, Vector2 pos, int size, FontStyle style, Color col)
        {
            GameObject go = new GameObject("Label_" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = col;
            t.alignment = TextAnchor.MiddleLeft;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320f, 22f);
        }

        public void UpdateNewtonFramingLayout(bool isNewtonFramingActive, float dt, bool instant = false)
        {
            if (tunerPanel != null)
            {
                RectTransform rt = tunerPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 targetPos = isNewtonFramingActive ? NewtonTunerAnchoredPos : NormalTunerAnchoredPos;
                    float targetScale = isNewtonFramingActive ? NewtonTunerScale : NormalTunerScale;
                    if (instant || dt <= 0f)
                    {
                        rt.anchoredPosition = targetPos;
                        rt.localScale = Vector3.one * targetScale;
                    }
                    else
                    {
                        float alpha = 1f - Mathf.Exp(-7.5f * dt); // Matches HTML smooth(7.5, dt)
                        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, alpha);
                        float s = Mathf.Lerp(rt.localScale.x, targetScale, alpha);
                        rt.localScale = Vector3.one * s;
                    }
                }
            }

            if (hintPanel != null)
            {
                GameManager gm = GameManager.Instance;
                bool isPlaying = gm == null || gm.State == GameState.Playing;
                hintPanel.SetActive(isPlaying && !isNewtonFramingActive);
            }
        }

        private void Update()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            // Only show tuner and hints during Gameplay (Playing state)
            GameManager gm = GameManager.Instance;
            bool isPlaying = gm != null && gm.State == GameState.Playing;

            if (cachedCameraFollow == null)
                cachedCameraFollow = FindAnyObjectByType<Contrast.Camera.CameraFollow>();
            var newtonCtrl = Contrast.Level.EasyNewtonFinaleController.Instance;
            var camFollow = cachedCameraFollow;
            bool isNewtonFramingActive =
                (newtonCtrl != null && newtonCtrl.IsZoneActive) ||
                (camFollow != null &&
                 (camFollow.CurrentMode == Contrast.Camera.CameraMode.NewtonFixed ||
                  camFollow.CurrentMode == Contrast.Camera.CameraMode.NewtonResultReveal));

            if (tunerPanel != null)
                tunerPanel.SetActive(isPlaying);
            if (hintPanel != null)
                hintPanel.SetActive(isPlaying && !isNewtonFramingActive);
            if (quangHudCard != null)
            {
                bool showQuang = isPlaying && Contrast.Level.QuangDiemItem.TotalCount > 0;
                quangHudCard.SetActive(showQuang);
                if (showQuang && quangHudValueText != null)
                {
                    quangHudValueText.text = $"{Contrast.Level.QuangDiemItem.CollectedCount} / {Contrast.Level.QuangDiemItem.TotalCount}";
                }
            }

            if (!isPlaying)
                return;

            UpdateNewtonFramingLayout(isNewtonFramingActive, Time.unscaledDeltaTime, false);

            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            float currentPhase = player != null ? player.CurrentPhase : simulatedPhase;
            LogicalColor color = player != null ? player.CurrentColor : LogicalColor.White;

            UpdateWaveTexture(currentPhase);
            UpdateStatusBadge(color, currentPhase);
            UpdateOmbreCursor(currentPhase);
            UpdateCheckpointToast();
        }

        private void UpdateWaveTexture(float phaseB)
        {
            if (wavePixels == null || waveTexture == null)
                return;

            // Clear background
            Color32 clearCol = new Color32(8, 14, 24, 255);
            for (int i = 0; i < wavePixels.Length; i++)
                wavePixels[i] = clearCol;

            // Vertical guides (faint alignment lines)
            for (int gx = 20; gx < WaveTexWidth; gx += 48)
            {
                for (int gy = 4; gy < WaveTexHeight - 4; gy++)
                {
                    wavePixels[gy * WaveTexWidth + gx] = new Color32(28, 42, 60, 255);
                }
            }

            // Draw Wave A (Fixed reference wave, yCenter = 56)
            int yCenterA = 54;
            int yCenterB = 26;
            const float spatialFreq = 0.075f;
            const float amplitude = 12f;

            Color32 waveACol = new Color32(245, 248, 250, 255);
            Color32 waveBCol = new Color32(160, 185, 205, 255);

            for (int x = 0; x < WaveTexWidth; x++)
            {
                // Wave A
                float yA = yCenterA + Mathf.Sin(x * spatialFreq) * amplitude;
                int pyA = Mathf.Clamp(Mathf.RoundToInt(yA), 0, WaveTexHeight - 1);
                wavePixels[pyA * WaveTexWidth + x] = waveACol;
                if (pyA + 1 < WaveTexHeight) wavePixels[(pyA + 1) * WaveTexWidth + x] = waveACol;

                // Wave B
                float yB = yCenterB + Mathf.Sin(x * spatialFreq + phaseB) * amplitude;
                int pyB = Mathf.Clamp(Mathf.RoundToInt(yB), 0, WaveTexHeight - 1);
                wavePixels[pyB * WaveTexWidth + x] = waveBCol;
                if (pyB + 1 < WaveTexHeight) wavePixels[(pyB + 1) * WaveTexWidth + x] = waveBCol;
            }

            waveTexture.SetPixels32(wavePixels);
            waveTexture.Apply();
        }

        private void UpdateStatusBadge(LogicalColor color, float phase)
        {
            if (stateBadgeTitle == null || stateBadgeRelation == null || stateBadgeBg == null)
                return;

            switch (color)
            {
                case LogicalColor.White:
                    stateBadgeTitle.text = "SÁNG";
                    stateBadgeTitle.color = Color.white;
                    stateBadgeRelation.text = "CÙNG PHA (Δφ ≈ 0°)";
                    stateBadgeBg.color = new Color(0.25f, 0.32f, 0.42f, 0.90f);
                    break;

                case LogicalColor.Gray:
                    stateBadgeTitle.text = "ĐIỀU PHA";
                    stateBadgeTitle.color = new Color(0.85f, 0.90f, 0.95f, 1f);
                    stateBadgeRelation.text = "LỆCH PHA (Δφ ≈ 90°)";
                    stateBadgeBg.color = new Color(0.16f, 0.22f, 0.30f, 0.90f);
                    break;

                case LogicalColor.Black:
                default:
                    stateBadgeTitle.text = "TỐI";
                    stateBadgeTitle.color = new Color(0.75f, 0.82f, 0.90f, 1f);
                    stateBadgeRelation.text = "NGƯỢC PHA (Δφ ≈ 180°)";
                    stateBadgeBg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);
                    break;
            }
        }

        public void SyncPhaseNow(float phase, LogicalColor color)
        {
            UpdateWaveTexture(phase);
            UpdateStatusBadge(color, phase);
            UpdateOmbreCursor(phase);
        }

        private void UpdateOmbreCursor(float phase)
        {
            if (cursorMarker == null)
                return;

            float normalized = Mathf.Repeat(phase / (Mathf.PI * 2f), 1f);
            float posX = normalized * 240f;
            cursorMarker.anchoredPosition = new Vector2(posX, 0f);
        }

        private void UpdateCheckpointToast()
        {
            if (checkpointToastGroup == null)
                return;

            if (checkpointFlashTimer > 0f)
            {
                checkpointFlashTimer -= Time.deltaTime;
                checkpointToastGroup.alpha = Mathf.Clamp01(checkpointFlashTimer * 1.5f);
            }
            else
            {
                checkpointToastGroup.alpha = 0f;
            }
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private void OnDestroy()
        {
            if (waveTexture != null)
                DestroyImmediate(waveTexture);
            if (ombreTexture != null)
                DestroyImmediate(ombreTexture);
        }
    }
}
