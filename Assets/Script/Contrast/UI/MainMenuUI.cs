namespace Contrast.UI
{
    using System.Collections.Generic;
    using Contrast.Audio;
    using Contrast.Color;
    using Contrast.Core;
    using Contrast.Presentation;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    using Color = UnityEngine.Color;

    /// <summary>
    /// Redesigned Main Menu UI for CONTRAST featuring:
    /// - Moonlit Optical Interference Sky backdrop ("Calima - Full Moon" theme)
    /// - Live Animated Gameplay Showcase Stage demonstrating White/Gray/Black phase shifting,
    ///   same-color pass-through, contrasting-color support, Quang Điểm collection & Newton Rings
    /// - Sleek 3-level campaign & direct level selection panel
    /// - Audio Settings modal (Music & SFX volume sliders) and Exit Game button
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private GameObject panel;
        private GameObject cardsRoot;
        private GameObject playSubPanel;
        private GameObject settingsModal;

        // Settings UI controls
        private Slider musicSlider;
        private Slider sfxSlider;
        private Text musicPercentText;
        private Text sfxPercentText;
        private bool suppressSliderCallback;

        // Procedural cached sprites so crisp circles/gradients render without external textures
        private static Sprite cachedCircleSprite;
        private static Sprite cachedRingSprite;
        private static Sprite cachedSkySprite;

        // Live animated backdrop & gameplay diorama references
        private RectTransform moonHaloOuter;
        private RectTransform moonHaloMid;
        private readonly List<RectTransform> skyMotes = new List<RectTransform>();
        private readonly List<Vector4> skyMoteParams = new List<Vector4>(); // x0, y0, speed, phase

        // Demo stage actors
        private RectTransform demoRunnerRoot;
        private Image demoHeadImg;
        private Image demoTorsoImg;
        private Image demoEyeImg;
        private RectTransform demoLegLeft;
        private RectTransform demoLegRight;
        private Image demoLegLeftImg;
        private Image demoLegRightImg;
        private RectTransform demoShiftPulseRt;
        private Image demoShiftPulseImg;

        private Image demoWhiteGateImg;
        private Image demoGrayBridgeImg;
        private Image demoWhiteStepImg;
        private RectTransform demoQuangDiemRt;
        private Image demoQuangDiemImg;
        private readonly List<Image> demoNewtonRingImgs = new List<Image>();
        private readonly List<RectTransform> demoNewtonRingRts = new List<RectTransform>();

        private RectTransform demoPhaseThumbRt;
        private Image demoPhaseThumbImg;
        private Text demoPhaseBadgeText;
        private Image demoPhaseBadgeBg;
        private Text demoRuleCaptionText;

        private int lastDemoColorIndex = -1;
        private float shiftPulseTimer = 99f;

        public bool IsPlaySubMenuOpen => playSubPanel != null && playSubPanel.activeSelf;
        public bool IsSettingsOpen => settingsModal != null && settingsModal.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
            Show(false);
        }

        private void OnDestroy()
        {
            if (ContrastAudioManager.Instance != null)
                ContrastAudioManager.Instance.OnVolumeChanged -= HandleAudioVolumeChanged;
        }

        public void EnsureInitialized()
        {
            if (panel != null)
                return;
            EnsureEventSystem();
            BuildUI();
            BindAudioSettings();
        }

        private void BindAudioSettings()
        {
            var audio = ContrastAudioManager.EnsureInstance();
            if (audio != null)
            {
                audio.OnVolumeChanged -= HandleAudioVolumeChanged;
                audio.OnVolumeChanged += HandleAudioVolumeChanged;
                SyncSettingsUI(audio.MusicVolume, audio.SfxVolume);
            }
        }

        private void HandleAudioVolumeChanged(float musicVol, float sfxVol)
        {
            SyncSettingsUI(musicVol, sfxVol);
        }

        private void SyncSettingsUI(float musicVol, float sfxVol)
        {
            suppressSliderCallback = true;
            if (musicSlider != null)
                musicSlider.value = musicVol;
            if (sfxSlider != null)
                sfxSlider.value = sfxVol;
            if (musicPercentText != null)
                musicPercentText.text = $"{Mathf.RoundToInt(musicVol * 100f)}%";
            if (sfxPercentText != null)
                sfxPercentText.text = $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            suppressSliderCallback = false;
        }

        public void Show(bool visible)
        {
            EnsureInitialized();
            if (panel != null)
                panel.SetActive(visible);

            if (visible)
            {
                if (settingsModal != null)
                    settingsModal.SetActive(false);
                OpenPlaySubMenu();
                ContrastAudioManager.EnsureInstance()?.PlayMenuMusic();
            }
        }

        public void OpenPlaySubMenu()
        {
            EnsureInitialized();
            if (cardsRoot != null)
                cardsRoot.SetActive(false);
            if (playSubPanel != null)
                playSubPanel.SetActive(true);
        }

        public void ClosePlaySubMenu()
        {
            EnsureInitialized();
            if (cardsRoot != null)
                cardsRoot.SetActive(false);
            if (playSubPanel != null)
                playSubPanel.SetActive(true);
        }

        public void OpenSettings()
        {
            EnsureInitialized();
            ContrastAudioManager.Instance?.PlayButtonClick();
            var audio = ContrastAudioManager.EnsureInstance();
            if (audio != null)
                SyncSettingsUI(audio.MusicVolume, audio.SfxVolume);
            if (settingsModal != null)
                settingsModal.SetActive(true);
        }

        public void CloseSettings()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            if (settingsModal != null)
                settingsModal.SetActive(false);
        }

        public void QuitApplication()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        public void SelectPlayFromBeginning()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.StartCampaign();
        }

        public void SelectDirectLevel(int levelIndex)
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.PlayDirectLevel(levelIndex);
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy)
                return;

            if (playSubPanel != null && !playSubPanel.activeSelf)
                playSubPanel.SetActive(true);

            // Always animate the background sky & live gameplay showcase
            UpdateAnimatedBackdropAndDemo(Time.unscaledTime, Time.unscaledDeltaTime);

            // If Settings Modal is open, ESC closes Settings and level hotkeys are paused
            if (IsSettingsOpen)
            {
                if (IsEscapePressed())
                    CloseSettings();
                return;
            }

            if (IsKey1Pressed())
            {
                SelectDirectLevel(0);
                return;
            }

            if (IsKey2Pressed())
            {
                SelectDirectLevel(1);
                return;
            }

            if (IsKey3Pressed())
            {
                SelectDirectLevel(2);
                return;
            }

            if (IsConfirmPressed())
            {
                SelectPlayFromBeginning();
                return;
            }
        }

        private void BuildUI()
        {
            skyMotes.Clear();
            skyMoteParams.Clear();
            demoNewtonRingImgs.Clear();
            demoNewtonRingRts.Clear();

            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.sprite = GetSkyGradientSprite();
            bg.color = Color.white;
            Stretch(panel.GetComponent<RectTransform>());

            // 1. Atmospheric Moon & Floating Starlight Backdrop
            BuildMoonlitSkyLayer(panel.transform);

            // 2. Top Brand Header Bar
            BuildTopHeaderBar(panel.transform);

            // 3. Hidden legacy root (preserved for test compatibility)
            cardsRoot = NewUI("MainCardsRoot", panel.transform);
            cardsRoot.SetActive(false);

            // 4. Right Column: Live Animated Gameplay Showcase Stage
            BuildLiveGameplayShowcase(panel.transform);

            // 5. Left Column: Sleek 3-Level Play Menu + Settings + Exit
            BuildPlaySubMenu(panel.transform);
            playSubPanel.SetActive(true);

            // 6. Audio Settings Modal (rendered on top when opened)
            BuildSettingsModal(panel.transform);
            settingsModal.SetActive(false);

            // Initialize frame 0 of demo animation so even static snapshots look crisp
            UpdateAnimatedBackdropAndDemo(0.8f, 0.016f);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 1. MOONLIT SKY & INTERFERENCE HALO BACKDROP
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildMoonlitSkyLayer(Transform parent)
        {
            GameObject skyLayer = NewUI("AnimatedSkyLayer", parent);
            Stretch(skyLayer.GetComponent<RectTransform>());

            Sprite circle = GetCircleSprite();
            Sprite ring = GetRingSprite();

            // Luminous Full Moon positioned in upper-right sky
            Vector2 moonPos = new Vector2(590f, 340f);

            GameObject outerGlow = NewUI("MoonAtmosphereGlow", skyLayer.transform);
            Image ogImg = outerGlow.AddComponent<Image>();
            ogImg.sprite = circle;
            ogImg.color = new Color(0.22f, 0.52f, 0.85f, 0.14f);
            ogImg.raycastTarget = false;
            moonHaloOuter = outerGlow.GetComponent<RectTransform>();
            moonHaloOuter.anchoredPosition = moonPos;
            moonHaloOuter.sizeDelta = new Vector2(540f, 540f);

            GameObject midRing = NewUI("MoonInterferenceRing", skyLayer.transform);
            Image mrImg = midRing.AddComponent<Image>();
            mrImg.sprite = ring;
            mrImg.color = new Color(0.48f, 0.84f, 1f, 0.16f);
            mrImg.raycastTarget = false;
            moonHaloMid = midRing.GetComponent<RectTransform>();
            moonHaloMid.anchoredPosition = moonPos;
            moonHaloMid.sizeDelta = new Vector2(330f, 330f);

            GameObject innerGlow = NewUI("MoonInnerHalo", skyLayer.transform);
            Image igImg = innerGlow.AddComponent<Image>();
            igImg.sprite = circle;
            igImg.color = new Color(0.70f, 0.90f, 1f, 0.22f);
            igImg.raycastTarget = false;
            RectTransform igRt = innerGlow.GetComponent<RectTransform>();
            igRt.anchoredPosition = moonPos;
            igRt.sizeDelta = new Vector2(190f, 190f);

            GameObject moonCore = NewUI("FullMoonCore", skyLayer.transform);
            Image mcImg = moonCore.AddComponent<Image>();
            mcImg.sprite = circle;
            mcImg.color = new Color(0.95f, 0.98f, 1f, 0.92f);
            mcImg.raycastTarget = false;
            RectTransform mcRt = moonCore.GetComponent<RectTransform>();
            mcRt.anchoredPosition = moonPos;
            mcRt.sizeDelta = new Vector2(104f, 104f);

            // Drifting ambient optical dust motes across the canvas
            for (int i = 0; i < 24; i++)
            {
                GameObject mote = NewUI("OpticalMote_" + i, skyLayer.transform);
                Image mImg = mote.AddComponent<Image>();
                mImg.sprite = circle;
                float hue = (i % 3 == 0) ? 0.55f : (i % 3 == 1 ? 0.38f : 0.62f);
                Color mCol = Color.HSVToRGB(hue, 0.35f, 1f);
                mCol.a = 0.16f + (i % 4) * 0.06f;
                mImg.color = mCol;
                mImg.raycastTarget = false;

                RectTransform mRt = mote.GetComponent<RectTransform>();
                float sz = 6f + (i % 5) * 3.5f;
                mRt.sizeDelta = new Vector2(sz, sz);

                float x0 = -900f + ((i * 173) % 1800);
                float y0 = -480f + ((i * 239) % 960);
                float spd = 12f + (i % 5) * 6f;
                float ph = i * 0.77f;
                mRt.anchoredPosition = new Vector2(x0, y0);

                skyMotes.Add(mRt);
                skyMoteParams.Add(new Vector4(x0, y0, spd, ph));
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 2. TOP VIRTUAL-LAB DOCKED HEADER BAR & RIGHT CIRCULAR FULLSCREEN BUTTON
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildTopHeaderBar(Transform parent)
        {
            GameObject headerRoot = NewUI("BrandHeader", parent);
            Stretch(headerRoot.GetComponent<RectTransform>());

            // Top-Left Ambient Music Stadium Pill
            GameObject bgmPill = NewUI("NowPlayingBadge", headerRoot.transform);
            Image pillBg = bgmPill.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(pillBg, LabStyleUI.GetPillSprite(), true, -4f);
            RectTransform pillRt = bgmPill.GetComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0f, 1f);
            pillRt.anchorMax = new Vector2(0f, 1f);
            pillRt.pivot = new Vector2(0f, 1f);
            pillRt.anchoredPosition = new Vector2(44f, -22f);
            pillRt.sizeDelta = new Vector2(286f, 46f);

            GameObject spkGo = NewUI("SpeakerIcon", bgmPill.transform);
            Image spkImg = spkGo.AddComponent<Image>();
            spkImg.sprite = LabStyleUI.GetIconSprite(LabStyleUI.IconKind.Speaker);
            spkImg.color = LabStyleUI.TextPrimary;
            spkImg.raycastTarget = false;
            RectTransform spkRt = spkGo.GetComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0f, 0.5f);
            spkRt.anchorMax = new Vector2(0f, 0.5f);
            spkRt.pivot = new Vector2(0.5f, 0.5f);
            spkRt.anchoredPosition = new Vector2(28f, 0f);
            spkRt.sizeDelta = new Vector2(20f, 20f);

            CreateSubLabel(
                bgmPill.transform,
                "NowPlayingText",
                "Calima — Full Moon",
                new Vector2(14f, 0f),
                new Vector2(226f, 32f),
                14,
                FontStyle.Bold,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleLeft);

            // Top-Center Docked Slate-Navy Bar (matching reference Virtual Lab top bar)
            GameObject topDock = NewUI("TopLabDock", headerRoot.transform);
            Image dockBg = topDock.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(dockBg, LabStyleUI.GetTopDockSprite(), true, -5f);
            RectTransform dockRt = topDock.GetComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(0.5f, 1f);
            dockRt.anchorMax = new Vector2(0.5f, 1f);
            dockRt.pivot = new Vector2(0.5f, 1f);
            dockRt.anchoredPosition = new Vector2(0f, 6f);
            dockRt.sizeDelta = new Vector2(780f, 64f);

            GameObject groove = NewUI("HeaderGroove", topDock.transform);
            Image grooveBg = groove.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(grooveBg, LabStyleUI.GetRecessedSlotSprite(), false);
            grooveBg.color = new Color(0.68f, 0.77f, 0.88f, 0.90f);
            RectTransform gRt = groove.GetComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0f, -4f);
            gRt.sizeDelta = new Vector2(660f, 34f);

            CreateSubLabel(
                groove.transform,
                "LabSubHeader",
                "PHÒNG THÍ NGHIỆM QUANG HỌC ẢO  ·  GIAO THOA & HỆ VÂN TRÒN NEWTON",
                Vector2.zero,
                new Vector2(640f, 30f),
                13,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);

            // Brand Title centered above Left Column
            GameObject title = NewUI("Title", headerRoot.transform);
            Text titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "CONTRAST";
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = LabStyleUI.TextPrimary;
            Shadow tShadow = title.AddComponent<Shadow>();
            tShadow.effectColor = LabStyleUI.DropShadowColor;
            tShadow.effectDistance = new Vector2(0f, -4f);
            RectTransform tRt = title.GetComponent<RectTransform>();
            tRt.sizeDelta = new Vector2(620f, 72f);
            tRt.anchoredPosition = new Vector2(-566f, 404f);

            GameObject subTitle = NewUI("Subtitle", headerRoot.transform);
            Text subText = subTitle.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = "Mô phỏng giao thoa sóng & đo bước sóng vân tròn Newton";
            subText.fontSize = 15;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleLeft;
            subText.color = LabStyleUI.TextSecondary;
            RectTransform sRt = subTitle.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(620f, 26f);
            sRt.anchoredPosition = new Vector2(-566f, 362f);

            // Top-Right Circular "Toàn màn hình" Icon Button (exact match to reference UI)
            LabStyleUI.CreateCircularIconAction(
                headerRoot.transform,
                "FullscreenButton",
                LabStyleUI.IconKind.Fullscreen,
                "Toàn màn hình",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-64f, -20f),
                () =>
                {
                    ContrastAudioManager.Instance?.PlayButtonClick();
                    Screen.fullScreen = !Screen.fullScreen;
                },
                54f);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 3. LIVE ANIMATED GAMEPLAY SHOWCASE STAGE (RIGHT COLUMN — FROSTED SLATE CARD)
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildLiveGameplayShowcase(Transform parent)
        {
            GameObject stageCard = NewUI("GameplayShowcaseCard", parent);
            Image stageBg = stageCard.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(stageBg, LabStyleUI.GetCardSprite(), true, -7f);

            RectTransform stageRt = stageCard.GetComponent<RectTransform>();
            stageRt.sizeDelta = new Vector2(856f, 736f);
            stageRt.anchoredPosition = new Vector2(452f, -64f);

            // Header Title inside Showcase
            CreateSubLabel(
                stageCard.transform,
                "ShowcaseTitle",
                "Mô phỏng trực quan cơ chế đổi pha màu",
                new Vector2(-120f, 324f),
                new Vector2(540f, 32f),
                17,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleLeft);

            // Live Phase Stadium Pill (Top-Right of Showcase)
            GameObject phaseBadge = NewUI("LivePhaseBadge", stageCard.transform);
            demoPhaseBadgeBg = phaseBadge.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(demoPhaseBadgeBg, LabStyleUI.GetPillSprite(), false);
            RectTransform pbRt = phaseBadge.GetComponent<RectTransform>();
            pbRt.sizeDelta = new Vector2(216f, 38f);
            pbRt.anchoredPosition = new Vector2(286f, 324f);

            GameObject pbTxtGo = NewUI("BadgeLabel", phaseBadge.transform);
            demoPhaseBadgeText = pbTxtGo.AddComponent<Text>();
            demoPhaseBadgeText.font = GetFont();
            demoPhaseBadgeText.text = "Pha hiện tại: Trắng";
            demoPhaseBadgeText.fontSize = 13;
            demoPhaseBadgeText.fontStyle = FontStyle.Bold;
            demoPhaseBadgeText.alignment = TextAnchor.MiddleCenter;
            demoPhaseBadgeText.color = Color.white;
            Stretch(pbTxtGo.GetComponent<RectTransform>());

            // Recessed Bezel around Simulation Viewport
            GameObject vpBezel = NewUI("ViewportBezel", stageCard.transform);
            Image bezelImg = vpBezel.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(bezelImg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform bzRt = vpBezel.GetComponent<RectTransform>();
            bzRt.sizeDelta = new Vector2(804f, 466f);
            bzRt.anchoredPosition = new Vector2(0f, 60f);

            // Inner Simulation Viewport (796 x 458)
            GameObject viewport = NewUI("DemoViewport", stageCard.transform);
            Image vpBg = viewport.AddComponent<Image>();
            vpBg.color = new Color(0.055f, 0.085f, 0.135f, 0.96f);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.sizeDelta = new Vector2(796f, 458f);
            vpRt.anchoredPosition = new Vector2(0f, 60f);
            viewport.AddComponent<RectMask2D>();

            // Subtle Optical Grid Lines inside Viewport
            for (int gx = -300; gx <= 300; gx += 150)
            {
                GameObject vLine = NewUI("GridV_" + gx, viewport.transform);
                Image vlImg = vLine.AddComponent<Image>();
                vlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.05f);
                vlImg.raycastTarget = false;
                RectTransform vlRt = vLine.GetComponent<RectTransform>();
                vlRt.sizeDelta = new Vector2(1f, 458f);
                vlRt.anchoredPosition = new Vector2(gx, 0f);
            }
            for (int gy = -160; gy <= 160; gy += 80)
            {
                GameObject hLine = NewUI("GridH_" + gy, viewport.transform);
                Image hlImg = hLine.AddComponent<Image>();
                hlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.05f);
                hlImg.raycastTarget = false;
                RectTransform hlRt = hLine.GetComponent<RectTransform>();
                hlRt.sizeDelta = new Vector2(796f, 1f);
                hlRt.anchoredPosition = new Vector2(0f, gy);
            }

            // Newton Interference Portal on Right Side of Viewport (matching "Hệ vân tròn Newton" in reference image)
            Sprite ringSprite = GetRingSprite();
            Sprite circleSprite = GetCircleSprite();
            Vector2 portalPos = new Vector2(282f, 22f);

            // Crisp circular white rim around Newton scope (matching circular scope frame in reference image)
            GameObject scopeRim = NewUI("NewtonScopeRim", viewport.transform);
            Image rimImg = scopeRim.AddComponent<Image>();
            rimImg.sprite = LabStyleUI.GetCircleButtonSprite();
            rimImg.color = new Color(0.95f, 0.98f, 1f, 0.92f);
            rimImg.raycastTarget = false;
            RectTransform rimRt = scopeRim.GetComponent<RectTransform>();
            rimRt.anchoredPosition = portalPos;
            rimRt.sizeDelta = new Vector2(192f, 192f);

            float[] ringSizes = { 172f, 132f, 92f, 52f };
            for (int r = 0; r < ringSizes.Length; r++)
            {
                GameObject nr = NewUI("DemoNewtonRing_" + r, viewport.transform);
                Image nrImg = nr.AddComponent<Image>();
                nrImg.sprite = ringSprite;
                nrImg.color = new Color(0.25f, 0.95f, 0.55f, 0.65f - r * 0.08f);
                nrImg.raycastTarget = false;
                RectTransform nrRt = nr.GetComponent<RectTransform>();
                nrRt.anchoredPosition = portalPos;
                nrRt.sizeDelta = new Vector2(ringSizes[r], ringSizes[r]);
                demoNewtonRingImgs.Add(nrImg);
                demoNewtonRingRts.Add(nrRt);
            }
            CreateSubLabel(
                viewport.transform,
                "PortalLabel",
                "Hệ vân tròn Newton",
                new Vector2(282f, -92f),
                new Vector2(200f, 24f),
                13,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);

            // Structural Base Floor (Universal #233140, y = -172)
            CreateDemoPlatform(
                viewport.transform,
                "FloorUniversal",
                new Vector2(0f, -172f),
                new Vector2(756f, 26f),
                new Color(0.18f, 0.25f, 0.34f, 1f),
                new Color(0.65f, 0.78f, 0.90f, 0.45f),
                "Nền cố định (mọi pha màu đều đứng được)");

            // Obstacle 1: Tall White Phase Wall (x = -175, y = -98, size 30 x 122)
            demoWhiteGateImg = CreateDemoPlatform(
                viewport.transform,
                "WhitePhaseGate",
                new Vector2(-175f, -98f),
                new Vector2(30f, 122f),
                new Color(0.95f, 0.98f, 1f, 0.96f),
                new Color(0.75f, 0.90f, 1f, 0.85f),
                null);
            CreateSubLabel(
                viewport.transform,
                "WhiteGateTag",
                "Tường Trắng",
                new Vector2(-175f, -22f),
                new Vector2(120f, 20f),
                11,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);

            // Obstacle 2: Elevated Gray Phase Bridge (x = 10, y = -68, size 186 x 20)
            demoGrayBridgeImg = CreateDemoPlatform(
                viewport.transform,
                "GrayPhaseBridge",
                new Vector2(10f, -68f),
                new Vector2(186f, 20f),
                new Color(0.55f, 0.60f, 0.66f, 0.96f),
                new Color(0.80f, 0.86f, 0.92f, 0.65f),
                "Bệ Xám");

            // Obstacle 3: Black Solid Pedestal below Gray Bridge (x = 18, y = -146, size 150 x 26)
            CreateDemoPlatform(
                viewport.transform,
                "BlackPedestal",
                new Vector2(18f, -146f),
                new Vector2(150f, 26f),
                new Color(0.08f, 0.10f, 0.15f, 0.98f),
                new Color(0.58f, 0.72f, 0.88f, 0.55f),
                "Bệ Đen");

            // Collectible Quang Điểm glowing above Black Pedestal (x = 24, y = -112)
            GameObject qdGo = NewUI("DemoQuangDiem", viewport.transform);
            demoQuangDiemImg = qdGo.AddComponent<Image>();
            demoQuangDiemImg.sprite = circleSprite;
            demoQuangDiemImg.color = new Color(1f, 0.88f, 0.36f, 0.95f);
            demoQuangDiemImg.raycastTarget = false;
            demoQuangDiemRt = qdGo.GetComponent<RectTransform>();
            demoQuangDiemRt.anchoredPosition = new Vector2(24f, -112f);
            demoQuangDiemRt.sizeDelta = new Vector2(22f, 22f);

            // Obstacle 4: White Step leading into Newton Portal (x = 152, y = -96, size 98 x 20)
            demoWhiteStepImg = CreateDemoPlatform(
                viewport.transform,
                "WhitePortalStep",
                new Vector2(152f, -96f),
                new Vector2(98f, 20f),
                new Color(0.95f, 0.98f, 1f, 0.96f),
                new Color(0.75f, 0.90f, 1f, 0.85f),
                "Bệ Trắng");

            // Animated Character Silhouette Root
            BuildDemoCharacter(viewport.transform);

            // Stadium Callout Pill below Viewport (matching "Đọc và ghi lại toạ độ ..." callout pill in reference image)
            GameObject ruleBox = NewUI("LiveRuleBox", stageCard.transform);
            Image ruleBg = ruleBox.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(ruleBg, LabStyleUI.GetPillSprite(), true, -4f);
            RectTransform ruleRt = ruleBox.GetComponent<RectTransform>();
            ruleRt.sizeDelta = new Vector2(796f, 52f);
            ruleRt.anchoredPosition = new Vector2(0f, -210f);

            GameObject ruleTxtGo = NewUI("RuleText", ruleBox.transform);
            demoRuleCaptionText = ruleTxtGo.AddComponent<Text>();
            demoRuleCaptionText.font = GetFont();
            demoRuleCaptionText.text = "Cùng màu → đi xuyên qua vật cản   ·   Khác màu → đứng vững trên bệ đỡ";
            demoRuleCaptionText.fontSize = 14;
            demoRuleCaptionText.fontStyle = FontStyle.Bold;
            demoRuleCaptionText.alignment = TextAnchor.MiddleCenter;
            demoRuleCaptionText.color = LabStyleUI.TextPrimary;
            Stretch(ruleTxtGo.GetComponent<RectTransform>());

            // Synchronized Mini Phase Bar (TRẮNG | XÁM | ĐEN) + Controls Legend
            BuildDemoPhaseBar(stageCard.transform);
        }

        private Image CreateDemoPlatform(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            Color fillCol,
            Color borderCol,
            string labelStr)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = fillCol;
            img.raycastTarget = false;

            Outline outl = go.AddComponent<Outline>();
            outl.effectColor = borderCol;
            outl.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            if (!string.IsNullOrEmpty(labelStr))
            {
                bool isLight = (fillCol.r + fillCol.g + fillCol.b) > 1.8f;
                CreateSubLabel(
                    go.transform,
                    "Tag",
                    labelStr,
                    Vector2.zero,
                    size,
                    11,
                    FontStyle.Bold,
                    isLight ? new Color(0.08f, 0.12f, 0.18f, 0.90f) : new Color(0.90f, 0.95f, 1f, 0.88f),
                    TextAnchor.MiddleCenter);
            }

            return img;
        }

        private void BuildDemoCharacter(Transform viewportTransform)
        {
            Sprite circle = GetCircleSprite();
            Sprite ring = GetRingSprite();

            GameObject root = NewUI("DemoCharacter", viewportTransform);
            demoRunnerRoot = root.GetComponent<RectTransform>();
            demoRunnerRoot.sizeDelta = new Vector2(36f, 54f);
            demoRunnerRoot.pivot = new Vector2(0.5f, 0f); // Feet pivot!

            // Expanding Phase-Shift Shockwave Ring
            GameObject pulseGo = NewUI("PhaseShiftPulse", root.transform);
            demoShiftPulseImg = pulseGo.AddComponent<Image>();
            demoShiftPulseImg.sprite = ring;
            demoShiftPulseImg.color = new Color(0.45f, 0.92f, 1f, 0f);
            demoShiftPulseImg.raycastTarget = false;
            demoShiftPulseRt = pulseGo.GetComponent<RectTransform>();
            demoShiftPulseRt.anchoredPosition = new Vector2(0f, 26f);
            demoShiftPulseRt.sizeDelta = new Vector2(32f, 32f);

            // Legs
            GameObject legL = NewUI("LegL", root.transform);
            demoLegLeftImg = legL.AddComponent<Image>();
            demoLegLeftImg.color = Color.white;
            demoLegLeftImg.raycastTarget = false;
            demoLegLeft = legL.GetComponent<RectTransform>();
            demoLegLeft.pivot = new Vector2(0.5f, 1f);
            demoLegLeft.anchoredPosition = new Vector2(-5f, 18f);
            demoLegLeft.sizeDelta = new Vector2(6f, 18f);

            GameObject legR = NewUI("LegR", root.transform);
            demoLegRightImg = legR.AddComponent<Image>();
            demoLegRightImg.color = Color.white;
            demoLegRightImg.raycastTarget = false;
            demoLegRight = legR.GetComponent<RectTransform>();
            demoLegRight.pivot = new Vector2(0.5f, 1f);
            demoLegRight.anchoredPosition = new Vector2(5f, 18f);
            demoLegRight.sizeDelta = new Vector2(6f, 18f);

            // Torso Cloak
            GameObject torso = NewUI("Torso", root.transform);
            demoTorsoImg = torso.AddComponent<Image>();
            demoTorsoImg.color = Color.white;
            demoTorsoImg.raycastTarget = false;
            Outline torsoOut = torso.AddComponent<Outline>();
            torsoOut.effectColor = new Color(0.55f, 0.85f, 1f, 0.65f);
            torsoOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform tRt = torso.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 26f);
            tRt.sizeDelta = new Vector2(20f, 22f);

            // Head
            GameObject head = NewUI("Head", root.transform);
            demoHeadImg = head.AddComponent<Image>();
            demoHeadImg.sprite = circle;
            demoHeadImg.color = Color.white;
            demoHeadImg.raycastTarget = false;
            Outline headOut = head.AddComponent<Outline>();
            headOut.effectColor = new Color(0.55f, 0.85f, 1f, 0.65f);
            headOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform hRt = head.GetComponent<RectTransform>();
            hRt.anchoredPosition = new Vector2(0f, 44f);
            hRt.sizeDelta = new Vector2(18f, 18f);

            // Optical Eye Visor
            GameObject eye = NewUI("Eye", head.transform);
            demoEyeImg = eye.AddComponent<Image>();
            demoEyeImg.color = new Color(0.20f, 0.85f, 1f, 1f);
            demoEyeImg.raycastTarget = false;
            RectTransform eRt = eye.GetComponent<RectTransform>();
            eRt.anchoredPosition = new Vector2(4f, 1f);
            eRt.sizeDelta = new Vector2(6f, 4f);
        }

        private void BuildDemoPhaseBar(Transform stageTransform)
        {
            GameObject barWrap = NewUI("DemoPhaseBarWrap", stageTransform);
            RectTransform bwRt = barWrap.GetComponent<RectTransform>();
            bwRt.sizeDelta = new Vector2(796f, 84f);
            bwRt.anchoredPosition = new Vector2(0f, -294f);

            // 3 Recessed Phase Slots: Trắng | Xám | Đen
            float segW = 158f;
            CreateDemoSegment(barWrap.transform, "SegWhite", "Pha Trắng", new Vector2(-240f, 14f), segW, new Color(0.94f, 0.97f, 1f, 0.96f), new Color(0.08f, 0.12f, 0.18f, 1f));
            CreateDemoSegment(barWrap.transform, "SegGray", "Pha Xám", new Vector2(-74f, 14f), segW, new Color(0.54f, 0.59f, 0.66f, 0.96f), new Color(0.96f, 0.98f, 1f, 1f));
            CreateDemoSegment(barWrap.transform, "SegBlack", "Pha Đen", new Vector2(92f, 14f), segW, new Color(0.14f, 0.18f, 0.25f, 0.98f), new Color(0.88f, 0.94f, 1f, 1f));

            // Sliding Royal-Blue Highlight Frame
            GameObject thumb = NewUI("PhaseThumbIndicator", barWrap.transform);
            demoPhaseThumbImg = thumb.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(demoPhaseThumbImg, LabStyleUI.GetPrimaryButtonSprite(), false);
            demoPhaseThumbImg.color = new Color(0.55f, 0.82f, 1f, 0.42f);
            demoPhaseThumbImg.raycastTarget = false;
            demoPhaseThumbRt = thumb.GetComponent<RectTransform>();
            demoPhaseThumbRt.sizeDelta = new Vector2(segW + 10f, 40f);
            demoPhaseThumbRt.anchoredPosition = new Vector2(-240f, 14f);

            // Quick Key Legend on Right
            CreateSubLabel(
                barWrap.transform,
                "QuickControlsTag",
                "← / → : Đổi pha màu\nSPACE : Nhảy   ·   X : Ghi toạ độ",
                new Vector2(288f, 14f),
                new Vector2(224f, 44f),
                12,
                FontStyle.Bold,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleRight);

            CreateSubLabel(
                barWrap.transform,
                "RuleSummaryFooter",
                "Quy tắc giao thoa: Đi xuyên qua vật cản cùng pha màu và đứng vững trên vật cản khác pha màu",
                new Vector2(0f, -26f),
                new Vector2(796f, 24f),
                12,
                FontStyle.Normal,
                LabStyleUI.TextMuted,
                TextAnchor.MiddleCenter);
        }

        private void CreateDemoSegment(Transform parent, string name, string textStr, Vector2 pos, float width, Color bgCol, Color textCol)
        {
            GameObject seg = NewUI(name, parent);
            Image img = seg.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetRecessedSlotSprite(), false);
            img.color = bgCol;
            img.raycastTarget = false;
            RectTransform rt = seg.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 32f);

            CreateSubLabel(seg.transform, "Lbl", textStr, Vector2.zero, new Vector2(width, 32f), 12, FontStyle.Bold, textCol, TextAnchor.MiddleCenter);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LIVE ANIMATION UPDATE (SKY + PHASE-SHIFT GAMEPLAY CHOREOGRAPHY)
        // ═══════════════════════════════════════════════════════════════════════

        private void UpdateAnimatedBackdropAndDemo(float time, float dt)
        {
            // 1. Breathe Moon Halos & Drift Optical Motes
            if (moonHaloOuter != null)
            {
                float s = 540f + Mathf.Sin(time * 0.9f) * 28f;
                moonHaloOuter.sizeDelta = new Vector2(s, s);
            }
            if (moonHaloMid != null)
            {
                float s = 330f + Mathf.Cos(time * 1.3f) * 16f;
                moonHaloMid.sizeDelta = new Vector2(s, s);
            }

            for (int i = 0; i < skyMotes.Count; i++)
            {
                RectTransform mRt = skyMotes[i];
                if (mRt == null) continue;
                Vector4 p = skyMoteParams[i];
                float y = p.y + ((time * p.z) % 980f);
                if (y > 500f) y -= 980f;
                float x = p.x + Mathf.Sin(time * 0.8f + p.w) * 26f;
                mRt.anchoredPosition = new Vector2(x, y);
            }

            // 2. Cycle Newton Portal Spectral Colors smoothly (550nm Green -> 650nm Red -> 470nm Blue)
            float waveCycle = (time * 0.28f) % 3f;
            Color c550 = NewtonRingsPresentation.WavelengthToRgb(550f);
            Color c650 = NewtonRingsPresentation.WavelengthToRgb(650f);
            Color c470 = NewtonRingsPresentation.WavelengthToRgb(470f);
            Color portalCol = waveCycle < 1f
                ? Color.Lerp(c550, c650, waveCycle)
                : (waveCycle < 2f ? Color.Lerp(c650, c470, waveCycle - 1f) : Color.Lerp(c470, c550, waveCycle - 2f));

            for (int r = 0; r < demoNewtonRingImgs.Count; r++)
            {
                if (demoNewtonRingImgs[r] == null) continue;
                float pulse = 1f + 0.035f * Mathf.Sin(time * 2.4f - r * 0.7f);
                float baseSz = 176f - r * 42f;
                demoNewtonRingRts[r].sizeDelta = new Vector2(baseSz * pulse, baseSz * pulse);
                demoNewtonRingImgs[r].color = new Color(portalCol.r, portalCol.g, portalCol.b, 0.62f - r * 0.11f);
            }

            if (demoRunnerRoot == null)
                return;

            // 3. 10-Second Gameplay Loop Demonstrating Phase Shifting & Pass-Through
            const float loopDuration = 10.0f;
            float t = time % loopDuration;

            float floorY = -159f;
            float grayBridgeTopY = -58f;
            float blackPedestalTopY = -133f;
            float whiteStepTopY = -86f;

            Vector2 pos;
            int colorIdx; // 0 = White, 1 = Gray, 2 = Black
            bool airborne = false;
            bool qdCollected = false;
            string ruleText;

            if (t < 1.1f)
            {
                // Stage 1A: Starts in BLACK, runs toward White Gate
                float u = t / 1.1f;
                pos = new Vector2(Mathf.Lerp(-340f, -235f, u), floorY);
                colorIdx = 2; // Black
                ruleText = "ĐANG TIẾN ĐẾN TƯỜNG TRẮNG  ·  CHUẨN BỊ ĐỔI PHA MÀU...";
            }
            else if (t < 2.8f)
            {
                // Stage 1B: Shifts to WHITE -> Passes straight through White Gate!
                float u = (t - 1.1f) / 1.7f;
                pos = new Vector2(Mathf.Lerp(-235f, -95f, u), floorY);
                colorIdx = 0; // White
                ruleText = "ĐỔI SANG MÀU TRẮNG (CÙNG MÀU)  →  ĐI XUYÊN QUA TƯỜNG TRẮNG!";
            }
            else if (t < 4.9f)
            {
                // Stage 2: Stays WHITE -> Jumps up and stands solidly on GRAY Bridge (White != Gray)
                float u = (t - 2.8f) / 2.1f;
                if (u < 0.42f)
                {
                    float ju = u / 0.42f;
                    float arc = Mathf.Sin(ju * Mathf.PI) * 46f;
                    pos = new Vector2(Mathf.Lerp(-95f, -35f, ju), Mathf.Lerp(floorY, grayBridgeTopY, ju) + arc);
                    airborne = true;
                }
                else
                {
                    float ru = (u - 0.42f) / 0.58f;
                    pos = new Vector2(Mathf.Lerp(-35f, 24f, ru), grayBridgeTopY);
                }
                colorIdx = 0; // White
                ruleText = "KHÁC MÀU (TRẮNG ≠ XÁM)  →  NHẢY & ĐỨNG VỮNG TRÊN BỆ XÁM!";
            }
            else if (t < 7.2f)
            {
                // Stage 3: Shifts to GRAY while on Gray Bridge -> Immediately drops through onto Black Pedestal & collects Quang Điểm!
                float u = (t - 4.9f) / 2.3f;
                colorIdx = 1; // Gray
                if (u < 0.28f)
                {
                    float du = u / 0.28f;
                    pos = new Vector2(24f, Mathf.Lerp(grayBridgeTopY, blackPedestalTopY, du * du));
                    airborne = true;
                }
                else
                {
                    float ru = (u - 0.28f) / 0.72f;
                    pos = new Vector2(Mathf.Lerp(24f, 88f, ru), blackPedestalTopY);
                    qdCollected = true;
                }
                ruleText = "ĐỔI SANG MÀU XÁM (CÙNG MÀU)  →  RƠI XUYÊN BỆ XÁM & THU THẬP QUANG ĐIỂM!";
            }
            else
            {
                // Stage 4: Shifts to BLACK -> Hops onto White Step (Black != White) and leaps into Newton Rings!
                float u = (t - 7.2f) / 2.8f;
                colorIdx = 2; // Black
                qdCollected = true;
                if (u < 0.36f)
                {
                    float ju = u / 0.36f;
                    float arc = Mathf.Sin(ju * Mathf.PI) * 38f;
                    pos = new Vector2(Mathf.Lerp(88f, 165f, ju), Mathf.Lerp(blackPedestalTopY, whiteStepTopY, ju) + arc);
                    airborne = true;
                }
                else if (u < 0.65f)
                {
                    float ru = (u - 0.36f) / 0.29f;
                    pos = new Vector2(Mathf.Lerp(165f, 205f, ru), whiteStepTopY);
                }
                else
                {
                    float pu = (u - 0.65f) / 0.35f;
                    float arc = Mathf.Sin(pu * Mathf.PI) * 34f;
                    pos = new Vector2(Mathf.Lerp(205f, 285f, pu), Mathf.Lerp(whiteStepTopY, 2f, pu) + arc);
                    airborne = true;
                }
                ruleText = "ĐỔI SANG MÀU ĐEN (KHÁC MÀU BỆ TRẮNG)  →  TIẾN VÀO ĐO VÂN TRÒN NEWTON!";
            }

            demoRunnerRoot.anchoredPosition = pos;

            // Trigger expanding shockwave ring whenever character changes phase color
            if (colorIdx != lastDemoColorIndex)
            {
                lastDemoColorIndex = colorIdx;
                shiftPulseTimer = 0f;
            }
            shiftPulseTimer += dt;
            if (demoShiftPulseRt != null && demoShiftPulseImg != null)
            {
                if (shiftPulseTimer < 0.55f)
                {
                    float p = shiftPulseTimer / 0.55f;
                    float sz = Mathf.Lerp(24f, 108f, p);
                    demoShiftPulseRt.sizeDelta = new Vector2(sz, sz);
                    demoShiftPulseImg.color = new Color(0.42f, 0.92f, 1f, (1f - p) * 0.85f);
                }
                else
                {
                    demoShiftPulseImg.color = new Color(0.42f, 0.92f, 1f, 0f);
                }
            }

            // Apply character visual color & running leg swing
            Color bodyCol = colorIdx == 0
                ? new Color(0.97f, 0.99f, 1f, 1f)
                : (colorIdx == 1 ? new Color(0.55f, 0.60f, 0.66f, 1f) : new Color(0.08f, 0.10f, 0.15f, 1f));
            Color eyeCol = colorIdx == 0
                ? new Color(0.08f, 0.45f, 0.85f, 1f)
                : new Color(0.38f, 0.92f, 1f, 1f);

            if (demoHeadImg != null) demoHeadImg.color = bodyCol;
            if (demoTorsoImg != null) demoTorsoImg.color = bodyCol;
            if (demoLegLeftImg != null) demoLegLeftImg.color = bodyCol;
            if (demoLegRightImg != null) demoLegRightImg.color = bodyCol;
            if (demoEyeImg != null) demoEyeImg.color = eyeCol;

            float swing = airborne ? 24f : Mathf.Sin(time * 13f) * 28f;
            if (demoLegLeft != null) demoLegLeft.localRotation = Quaternion.Euler(0f, 0f, swing);
            if (demoLegRight != null) demoLegRight.localRotation = Quaternion.Euler(0f, 0f, -swing);

            // Highlight pass-through translucency when platform matches character phase!
            if (demoWhiteGateImg != null)
                demoWhiteGateImg.color = new Color(0.95f, 0.98f, 1f, colorIdx == 0 ? 0.30f : 0.96f);
            if (demoWhiteStepImg != null)
                demoWhiteStepImg.color = new Color(0.95f, 0.98f, 1f, colorIdx == 0 ? 0.30f : 0.96f);
            if (demoGrayBridgeImg != null)
                demoGrayBridgeImg.color = new Color(0.53f, 0.57f, 0.62f, colorIdx == 1 ? 0.28f : 0.96f);

            // Animate Quang Điểm collectible
            if (demoQuangDiemRt != null && demoQuangDiemImg != null)
            {
                if (qdCollected)
                {
                    demoQuangDiemImg.color = new Color(1f, 0.90f, 0.40f, 0f);
                }
                else
                {
                    float qPulse = 20f + Mathf.Sin(time * 5f) * 4f;
                    demoQuangDiemRt.sizeDelta = new Vector2(qPulse, qPulse);
                    demoQuangDiemRt.anchoredPosition = new Vector2(24f, -110f + Mathf.Sin(time * 3.5f) * 4f);
                    demoQuangDiemImg.color = new Color(1f, 0.90f, 0.38f, 0.96f);
                }
            }

            // Update Live Phase Badge & Synchronized Mini Phase Bar Thumb
            float targetThumbX = colorIdx == 0 ? -240f : (colorIdx == 1 ? -74f : 92f);
            if (demoPhaseThumbRt != null)
            {
                Vector2 cur = demoPhaseThumbRt.anchoredPosition;
                demoPhaseThumbRt.anchoredPosition = new Vector2(Mathf.Lerp(cur.x, targetThumbX, Mathf.Clamp01(dt * 14f)), 14f);
            }

            if (demoPhaseBadgeBg != null && demoPhaseBadgeText != null)
            {
                demoPhaseBadgeBg.sprite = colorIdx == 0
                    ? LabStyleUI.GetPrimaryButtonSprite()
                    : LabStyleUI.GetPillSprite();
                demoPhaseBadgeBg.color = UnityEngine.Color.white;
                demoPhaseBadgeText.text = colorIdx == 0
                    ? "Pha hiện tại: Trắng"
                    : (colorIdx == 1 ? "Pha hiện tại: Xám" : "Pha hiện tại: Đen");
                demoPhaseBadgeText.color = LabStyleUI.TextPrimary;
            }

            if (demoRuleCaptionText != null)
                demoRuleCaptionText.text = ruleText;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 4. PLAY SUB-MENU PANEL (FROSTED SLATE LAB CARD + ROYAL-BLUE CTA + LEVEL SLOTS)
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildPlaySubMenu(Transform parent)
        {
            playSubPanel = NewUI("PlaySubMenuPanel", parent);
            Image cardBg = playSubPanel.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), addDropShadow: true, shadowOffsetY: -7f);

            RectTransform subRt = playSubPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(856f, 736f);
            subRt.anchoredPosition = new Vector2(-452f, -64f);

            // Section Header
            CreateSubLabel(
                playSubPanel.transform,
                "Header",
                "Phòng thí nghiệm giao thoa  ·  Chọn bài thực hành",
                new Vector2(0f, 322f),
                new Vector2(780f, 34f),
                19,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleLeft);

            // Primary Royal-Blue CTA (matching "NHẬP" button style in reference Virtual Physics Lab)
            CreatePrimaryStartButton(
                playSubPanel.transform,
                "PlayFromStartButton",
                "BẮT ĐẦU THÍ NGHIỆM   (MÀN 1 → MÀN 3)",
                new Vector2(0f, 246f),
                new Vector2(780f, 76f),
                SelectPlayFromBeginning);

            // Direct Level Section Header
            CreateSubLabel(
                playSubPanel.transform,
                "DirectSelectHeader",
                "Danh sách bài đo trực tiếp  (k · i · bước sóng λ)",
                new Vector2(0f, 174f),
                new Vector2(780f, 26f),
                14,
                FontStyle.Bold,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleLeft);

            // Level 1 Button (Green 550nm readout slot)
            CreateLabLevelButton(
                playSubPanel.transform,
                "SelectLevel_L01",
                "1",
                "Bài 01  ·  Cánh cửa không phải vật cản",
                "Vật cản pha tĩnh  ·  2 Quang điểm  ·  k = 6,  i = 3",
                "550 nm · Lục",
                new Color(0.28f, 0.92f, 0.52f, 1f),
                new Vector2(0f, 104f),
                new Vector2(780f, 82f),
                () => SelectDirectLevel(0));

            // Level 2 Button (Red 650nm readout slot)
            CreateLabLevelButton(
                playSubPanel.transform,
                "SelectLevel_L02",
                "2",
                "Bài 02  ·  Cầu thang ma",
                "Cầu thang lệch pha  ·  2 Quang điểm  ·  k = 4,  i = 2",
                "650 nm · Đỏ",
                new Color(0.98f, 0.36f, 0.38f, 1f),
                new Vector2(0f, 10f),
                new Vector2(780f, 82f),
                () => SelectDirectLevel(1));

            // Level 3 Button (Blue 470nm readout slot)
            CreateLabLevelButton(
                playSubPanel.transform,
                "SelectLevel_L03",
                "3",
                "Bài 03  ·  Giếng ba tầng",
                "Giếng ba tầng & bệ động  ·  3 Quang điểm  ·  k = 7,  i = 2",
                "470 nm · Lam",
                new Color(0.38f, 0.68f, 1f, 1f),
                new Vector2(0f, -84f),
                new Vector2(780f, 82f),
                () => SelectDirectLevel(2));

            // Divider Line before Utility Pill Buttons
            GameObject div = NewUI("BottomDivider", playSubPanel.transform);
            Image divImg = div.AddComponent<Image>();
            divImg.color = new Color(0.68f, 0.80f, 0.92f, 0.22f);
            divImg.raycastTarget = false;
            RectTransform divRt = div.GetComponent<RectTransform>();
            divRt.sizeDelta = new Vector2(780f, 1.5f);
            divRt.anchoredPosition = new Vector2(0f, -156f);

            // Bottom Stadium Pill Utility Buttons: Cài đặt âm thanh (Left) + Thoát trò chơi (Right)
            CreatePillUtilityButton(
                playSubPanel.transform,
                "SettingsButton",
                "Cài đặt âm thanh",
                LabStyleUI.IconKind.SettingsSliders,
                LabStyleUI.GetPillSprite(),
                new Vector2(-202f, -218f),
                new Vector2(376f, 62f),
                OpenSettings);

            CreatePillUtilityButton(
                playSubPanel.transform,
                "ExitButton",
                "Thoát trò chơi",
                LabStyleUI.IconKind.PowerExit,
                LabStyleUI.GetDangerButtonSprite(),
                new Vector2(202f, -218f),
                new Vector2(376f, 62f),
                QuitApplication);

            // Hidden compatibility node preserved for EditorLevelTests
            GameObject backGo = NewUI("BackToMainMenuButton", playSubPanel.transform);
            backGo.SetActive(false);

            CreateSubLabel(
                playSubPanel.transform,
                "GameplayFooter",
                "Phím tắt:  ENTER / SPACE để bắt đầu   ·   Phím 1 / 2 / 3 mở nhanh Bài 01 / 02 / 03",
                new Vector2(0f, -314f),
                new Vector2(780f, 28f),
                13,
                FontStyle.Bold,
                LabStyleUI.TextMuted,
                TextAnchor.MiddleCenter);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 5. AUDIO SETTINGS MODAL (FROSTED SLATE CARD + RECESSED SLIDER SLOTS)
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildSettingsModal(Transform parent)
        {
            settingsModal = NewUI("SettingsModalPanel", parent);
            Image backdrop = settingsModal.AddComponent<Image>();
            backdrop.color = new Color(0.03f, 0.05f, 0.09f, 0.78f);
            Stretch(settingsModal.GetComponent<RectTransform>());

            GameObject card = NewUI("SettingsCard", settingsModal.transform);
            Image cardBg = card.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), addDropShadow: true, shadowOffsetY: -8f);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(720f, 470f);
            cardRt.anchoredPosition = Vector2.zero;

            CreateSubLabel(
                card.transform,
                "SettingsTitle",
                "Cài đặt âm thanh",
                new Vector2(0f, 178f),
                new Vector2(640f, 42f),
                26,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);

            CreateSubLabel(
                card.transform,
                "SettingsSub",
                "Điều chỉnh độ lớn Nhạc nền (Full Moon / Evening Tone) và Hiệu ứng âm thanh",
                new Vector2(0f, 138f),
                new Vector2(650f, 24f),
                13,
                FontStyle.Normal,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleCenter);

            // Music Slider Row
            musicSlider = BuildSettingsSliderRow(
                card.transform,
                "MusicSliderRow",
                "Âm lượng Nhạc nền",
                new Vector2(0f, 54f),
                ContrastAudioManager.DefaultMusicVolume,
                out musicPercentText,
                val =>
                {
                    if (suppressSliderCallback) return;
                    ContrastAudioManager.EnsureInstance().SetMusicVolume(val);
                    if (musicPercentText != null)
                        musicPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            // SFX Slider Row
            sfxSlider = BuildSettingsSliderRow(
                card.transform,
                "SfxSliderRow",
                "Âm lượng Hiệu ứng",
                new Vector2(0f, -38f),
                ContrastAudioManager.DefaultSfxVolume,
                out sfxPercentText,
                val =>
                {
                    if (suppressSliderCallback) return;
                    ContrastAudioManager.EnsureInstance().SetSfxVolume(val);
                    if (sfxPercentText != null)
                        sfxPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            // Clean Single-Line Action Row inside Settings Modal
            CreatePillUtilityButton(
                card.transform,
                "TestSfxButton",
                "Nghe thử SFX",
                LabStyleUI.IconKind.Speaker,
                LabStyleUI.GetSecondaryButtonSprite(),
                new Vector2(-216f, -158f),
                new Vector2(194f, 58f),
                () => ContrastAudioManager.EnsureInstance().PlayCheckpoint());

            CreatePillUtilityButton(
                card.transform,
                "ResetAudioButton",
                "Mặc định",
                LabStyleUI.IconKind.Restart,
                LabStyleUI.GetSecondaryButtonSprite(),
                new Vector2(0f, -158f),
                new Vector2(194f, 58f),
                () =>
                {
                    ContrastAudioManager.EnsureInstance().ResetDefaultVolumes();
                    ContrastAudioManager.EnsureInstance().PlayButtonClick();
                });

            CreatePrimaryModalButton(
                card.transform,
                "CloseSettingsButton",
                "XÁC NHẬN",
                new Vector2(216f, -158f),
                new Vector2(194f, 58f),
                CloseSettings);
        }

        private static Slider BuildSettingsSliderRow(
            Transform parent,
            string name,
            string titleStr,
            Vector2 pos,
            float defaultVal,
            out Text percentText,
            UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            GameObject row = NewUI(name, parent);
            Image rowBg = row.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(rowBg, LabStyleUI.GetRecessedSlotSprite(), addDropShadow: false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(632f, 74f);
            rowRt.anchoredPosition = pos;

            GameObject lblGo = NewUI("Label", row.transform);
            Text lbl = lblGo.AddComponent<Text>();
            lbl.font = GetFont();
            lbl.text = titleStr;
            lbl.fontSize = 15;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = LabStyleUI.TextPrimary;
            RectTransform lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.sizeDelta = new Vector2(210f, 36f);
            lblRt.anchoredPosition = new Vector2(-188f, 0f);

            GameObject sliderGo = NewUI("Slider", row.transform);
            RectTransform sRt = sliderGo.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(290f, 28f);
            sRt.anchoredPosition = new Vector2(82f, 0f);

            GameObject bgGo = NewUI("Background", sliderGo.transform);
            Image bgImg = bgGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(bgImg, LabStyleUI.GetPillSprite(), addDropShadow: false);
            bgImg.color = new Color(0.75f, 0.82f, 0.92f, 0.95f);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 14f);

            GameObject fillArea = NewUI("Fill Area", sliderGo.transform);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.5f);
            faRt.anchorMax = new Vector2(1f, 0.5f);
            faRt.offsetMin = new Vector2(2f, -6f);
            faRt.offsetMax = new Vector2(-2f, 6f);

            GameObject fillGo = NewUI("Fill", fillArea.transform);
            Image fillImg = fillGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(fillImg, LabStyleUI.GetPrimaryButtonSprite(), addDropShadow: false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            Stretch(fillRt);

            GameObject handleArea = NewUI("Handle Slide Area", sliderGo.transform);
            Stretch(handleArea.GetComponent<RectTransform>());

            GameObject handleGo = NewUI("Handle", handleArea.transform);
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = LabStyleUI.GetCircleButtonSprite();
            handleImg.type = Image.Type.Simple;
            handleImg.color = UnityEngine.Color.white;
            RectTransform hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(26f, 26f);

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = defaultVal;
            slider.onValueChanged.AddListener(onValueChanged);

            GameObject pctGo = NewUI("PercentText", row.transform);
            percentText = pctGo.AddComponent<Text>();
            percentText.font = GetFont();
            percentText.text = $"{Mathf.RoundToInt(defaultVal * 100f)}%";
            percentText.fontSize = 15;
            percentText.fontStyle = FontStyle.Bold;
            percentText.alignment = TextAnchor.MiddleRight;
            percentText.color = LabStyleUI.TextPrimary;
            RectTransform pctRt = pctGo.GetComponent<RectTransform>();
            pctRt.sizeDelta = new Vector2(60f, 36f);
            pctRt.anchoredPosition = new Vector2(268f, 0f);

            return slider;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // VIRTUAL LAB BUTTON BUILDERS
        // ═══════════════════════════════════════════════════════════════════════

        private void CreatePrimaryStartButton(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = NewUI(name, parent);
            Image img = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetPrimaryButtonSprite(), addDropShadow: true, shadowOffsetY: -5f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(onClick);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject iconGo = NewUI("Icon", btnGo.transform);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = LabStyleUI.GetIconSprite(LabStyleUI.IconKind.PlayTriangle);
            iconImg.color = UnityEngine.Color.white;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.sizeDelta = new Vector2(24f, 24f);
            iRt.anchoredPosition = new Vector2(-218f, 0f);

            CreateSubLabel(
                btnGo.transform,
                "Title",
                label,
                new Vector2(14f, 0f),
                new Vector2(size.x - 96f, size.y),
                19,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);
        }

        private void CreateLabLevelButton(
            Transform parent,
            string name,
            string indexStr,
            string mainText,
            string subText,
            string wavelengthBadge,
            Color badgeColor,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = NewUI(name, parent);
            Image img = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetSecondaryButtonSprite(), addDropShadow: true, shadowOffsetY: -4f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(onClick);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            // Circular Spectral Order Badge on the Left
            GameObject numCircle = NewUI("OrderCircle", btnGo.transform);
            Image numImg = numCircle.AddComponent<Image>();
            numImg.sprite = LabStyleUI.GetSoftCircleSprite();
            numImg.color = badgeColor;
            numImg.raycastTarget = false;
            RectTransform numRt = numCircle.GetComponent<RectTransform>();
            numRt.sizeDelta = new Vector2(44f, 44f);
            numRt.anchoredPosition = new Vector2(-size.x * 0.5f + 42f, 0f);

            CreateSubLabel(
                numCircle.transform,
                "OrderText",
                indexStr,
                Vector2.zero,
                new Vector2(44f, 44f),
                18,
                FontStyle.Bold,
                new Color(0.06f, 0.10f, 0.16f, 1f),
                TextAnchor.MiddleCenter);

            // Title & Clean Mechanics Subtitle
            CreateSubLabel(
                btnGo.transform,
                "Title",
                mainText,
                new Vector2(-46f, 13f),
                new Vector2(490f, 28f),
                17,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleLeft);

            CreateSubLabel(
                btnGo.transform,
                "Sub",
                subText,
                new Vector2(-46f, -14f),
                new Vector2(490f, 22f),
                12,
                FontStyle.Normal,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleLeft);

            // Recessed Wavelength Readout Slot on the Right (matching [giá trị   mm] slot in reference UI)
            GameObject waveSlot = NewUI("WavelengthSlot", btnGo.transform);
            Image slotImg = waveSlot.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(slotImg, LabStyleUI.GetRecessedSlotSprite(), addDropShadow: false);
            slotImg.raycastTarget = false;
            RectTransform slotRt = waveSlot.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(148f, 42f);
            slotRt.anchoredPosition = new Vector2(size.x * 0.5f - 94f, 0f);

            GameObject dotGo = NewUI("SpectrumDot", waveSlot.transform);
            Image dotImg = dotGo.AddComponent<Image>();
            dotImg.sprite = LabStyleUI.GetSoftCircleSprite();
            dotImg.color = badgeColor;
            dotImg.raycastTarget = false;
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(10f, 10f);
            dotRt.anchoredPosition = new Vector2(-54f, 0f);

            CreateSubLabel(
                waveSlot.transform,
                "WaveText",
                wavelengthBadge,
                new Vector2(8f, 0f),
                new Vector2(118f, 36f),
                13,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);
        }

        private void CreatePillUtilityButton(
            Transform parent,
            string name,
            string label,
            LabStyleUI.IconKind iconKind,
            Sprite sprite,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = NewUI(name, parent);
            Image img = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(img, sprite, addDropShadow: true, shadowOffsetY: -4f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(onClick);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject iconGo = NewUI("Icon", btnGo.transform);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = LabStyleUI.GetIconSprite(iconKind);
            iconImg.color = LabStyleUI.TextPrimary;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.sizeDelta = new Vector2(22f, 22f);
            iRt.anchoredPosition = new Vector2(-size.x * 0.5f + 34f, 0f);

            CreateSubLabel(
                btnGo.transform,
                "Title",
                label,
                new Vector2(14f, 0f),
                new Vector2(size.x - 68f, size.y),
                16,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);
        }

        private void CreatePrimaryModalButton(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = NewUI(name, parent);
            Image img = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetPrimaryButtonSprite(), addDropShadow: true, shadowOffsetY: -4f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(onClick);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            CreateSubLabel(
                btnGo.transform,
                "Title",
                label,
                Vector2.zero,
                new Vector2(size.x - 24f, size.y),
                16,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleCenter);
        }

        private void CreateSubLabel(
            Transform parent,
            string name,
            string content,
            Vector2 pos,
            Vector2 size,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor anchor)
        {
            GameObject go = NewUI(name, parent);
            Text txt = go.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = anchor;
            txt.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        public void OnPlay() => OpenPlaySubMenu();

        private static Sprite GetSkyGradientSprite()
        {
            if (cachedSkySprite != null)
                return cachedSkySprite;

            const int w = 64, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color bottom = new Color(0.018f, 0.034f, 0.068f, 1f);
            Color mid = new Color(0.042f, 0.095f, 0.178f, 1f);
            Color top = new Color(0.022f, 0.052f, 0.112f, 1f);
            Color moonGlowTint = new Color(0.10f, 0.24f, 0.42f, 1f);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                Color rowCol = v < 0.52f
                    ? Color.Lerp(bottom, mid, v / 0.52f)
                    : Color.Lerp(mid, top, (v - 0.52f) / 0.48f);

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    float distMoon = Vector2.Distance(new Vector2(u, v), new Vector2(0.80f, 0.82f));
                    float glow = Mathf.Clamp01(1f - distMoon * 1.65f);
                    Color px = Color.Lerp(rowCol, moonGlowTint, glow * glow * 0.65f);
                    tex.SetPixel(x, y, px);
                }
            }
            tex.Apply();
            cachedSkySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return cachedSkySprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite != null)
                return cachedCircleSprite;

            const int d = 64;
            const float r = d * 0.5f;
            Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01((r - 0.5f - dist) * 0.85f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 64f);
            return cachedCircleSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite != null)
                return cachedRingSprite;

            const int d = 96;
            const float r = d * 0.5f;
            Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float ringDist = Mathf.Abs(dist - (r - 4.5f));
                    float a = Mathf.Clamp01(1f - ringDist / 2.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 96f);
            return cachedRingSprite;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsKey1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        private static bool IsKey2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        private static bool IsKey3Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
#endif
        }

        private static bool IsConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }
    }
}
