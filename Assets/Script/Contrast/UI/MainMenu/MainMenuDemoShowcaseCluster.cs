namespace Contrast.UI
{
    using System.Collections.Generic;
    using Contrast.Color;
    using Contrast.Data;
    using Contrast.Presentation;
    using UnityEngine;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Cluster 4b: Right Column Live Animated Gameplay Showcase Stage.
    /// Builds and updates the viewport, scrolling optical grid, dynamic platforms,
    /// Quang Điểm collectible, 5-ring Newton apparatus, real SilhouetteMotion character
    /// (including the 2-armed <c>Celebrate</c> / <c>AnMung</c> animation), circular iris-in
    /// transition overlay, and synchronized mini phase bar.
    /// </summary>
    internal sealed class MainMenuDemoShowcaseCluster
    {
        public MainMenuDemoSimulation Simulation { get; } = new MainMenuDemoSimulation();

        public RectTransform CharacterRectTransform => demoRunnerRoot;
        public float TotalTime { get; private set; }

        private RectTransform demoRunnerRoot;
        private Image demoCharacterHaloImg;
        private Image demoCharacterBodyImg;
        private Image demoCharacterRimImg;
        private Image demoCharacterEyesImg;
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
        private RectTransform demoWhiteGateTagRt;
        private Vector2 demoWhiteGateTagWorldPx;
        private Image demoGrayBridgeImg;
        private Image demoWhiteStepImg;
        private RectTransform demoQuangDiemRt;
        private Image demoQuangDiemImg;

        private readonly List<RectTransform> demoGridVLines = new List<RectTransform>();
        private readonly List<float> demoGridVBaseX = new List<float>();
        private readonly List<RectTransform> demoGridHLines = new List<RectTransform>();
        private readonly List<float> demoGridHBaseY = new List<float>();

        private RectTransform demoNewtonRootRt;
        private readonly List<Image> demoNewtonRingImgs = new List<Image>();
        private readonly List<RectTransform> demoNewtonRingRts = new List<RectTransform>();
        private readonly RectTransform[] demoNewtonPointRts = new RectTransform[3];
        private readonly Image[] demoNewtonPointImgs = new Image[3];
        private RectTransform demoConnectLineBRt;
        private Image demoConnectLineBImg;
        private RectTransform demoConnectLineSmallBRt;
        private Image demoConnectLineSmallBImg;
        private GameObject demoNewtonFormulaBadgeGo;
        private Text demoNewtonFormulaText;
        private RectTransform demoReachPointerRt;
        private Image demoReachPointerImg;

        private GameObject demoIrisRootGo;
        private RectTransform demoIrisApertureRt;
        private RectTransform demoIrisTopBarRt;
        private RectTransform demoIrisBottomBarRt;
        private RectTransform demoIrisLeftBarRt;
        private RectTransform demoIrisRightBarRt;

        private RectTransform demoPhaseThumbRt;
        private Image demoPhaseThumbImg;
        private RectTransform demoSegWhiteRt;
        private RectTransform demoSegGrayRt;
        private RectTransform demoSegBlackRt;
        private Text demoPhaseBadgeText;
        private Image demoPhaseBadgeBg;
        private Image demoPhaseBadgeSwatchImg;
        private Text demoRuleCaptionText;
        private Text demoBotInputBadgeText;

        private float demoAnimClock;
        private int lastDemoColorIndex = -1;
        private float shiftPulseTimer = 99f;

        public void Build(Transform parent)
        {
            Simulation.Platforms.Clear();
            demoNewtonRingImgs.Clear();
            demoNewtonRingRts.Clear();
            demoGridVLines.Clear();
            demoGridVBaseX.Clear();
            demoGridHLines.Clear();
            demoGridHBaseY.Clear();

            Simulation.InitializeLevelData();
            LevelData demoLevelData = Simulation.LevelData;

            GameObject stageCard = MainMenuUIBuilder.NewUI("GameplayShowcaseCard", parent);
            Image stageBg = stageCard.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(stageBg, LabStyleUI.GetCardSprite(), true, -7f);

            RectTransform stageRt = stageCard.GetComponent<RectTransform>();
            stageRt.sizeDelta = new Vector2(856f, 736f);
            stageRt.anchoredPosition = new Vector2(452f, -64f);

            MainMenuUIBuilder.CreateSubLabel(
                stageCard.transform,
                "ShowcaseTitle",
                "Mô phỏng cơ chế giao thoa sóng (Cực đại · Trung gian · Cực tiểu)",
                new Vector2(-148f, 324f),
                new Vector2(500f, 32f),
                16,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleLeft);

            GameObject phaseBadge = MainMenuUIBuilder.NewUI("LivePhaseBadge", stageCard.transform);
            demoPhaseBadgeBg = phaseBadge.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(demoPhaseBadgeBg, LabStyleUI.GetPillSprite(), false);
            RectTransform pbRt = phaseBadge.GetComponent<RectTransform>();
            pbRt.sizeDelta = new Vector2(284f, 38f);
            pbRt.anchoredPosition = new Vector2(256f, 324f);

            EnsurePhaseBadgeSwatchAndLabel(phaseBadge.transform);

            GameObject vpBezel = MainMenuUIBuilder.NewUI("ViewportBezel", stageCard.transform);
            Image bezelImg = vpBezel.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(bezelImg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform bzRt = vpBezel.GetComponent<RectTransform>();
            bzRt.sizeDelta = new Vector2(804f, 466f);
            bzRt.anchoredPosition = new Vector2(0f, 74f);

            GameObject viewport = MainMenuUIBuilder.NewUI("DemoViewport", stageCard.transform);
            Image vpBg = viewport.AddComponent<Image>();
            vpBg.color = new Color(0.055f, 0.085f, 0.135f, 0.96f);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.sizeDelta = new Vector2(MainMenuUI.DemoViewportWidthPx, MainMenuUI.DemoViewportHeightPx);
            vpRt.anchoredPosition = new Vector2(0f, 74f);
            viewport.AddComponent<RectMask2D>();

            for (int gx = -480; gx <= 480; gx += 120)
            {
                GameObject vLine = MainMenuUIBuilder.NewUI("GridV_" + gx, viewport.transform);
                Image vlImg = vLine.AddComponent<Image>();
                vlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.06f);
                vlImg.raycastTarget = false;
                RectTransform vlRt = vLine.GetComponent<RectTransform>();
                vlRt.sizeDelta = new Vector2(1f, MainMenuUI.DemoViewportHeightPx);
                vlRt.anchoredPosition = new Vector2(gx, 0f);
                demoGridVLines.Add(vlRt);
                demoGridVBaseX.Add(gx);
            }
            for (int gy = -240; gy <= 240; gy += 80)
            {
                GameObject hLine = MainMenuUIBuilder.NewUI("GridH_" + gy, viewport.transform);
                Image hlImg = hLine.AddComponent<Image>();
                hlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.06f);
                hlImg.raycastTarget = false;
                RectTransform hlRt = hLine.GetComponent<RectTransform>();
                hlRt.sizeDelta = new Vector2(MainMenuUI.DemoViewportWidthPx, 1f);
                hlRt.anchoredPosition = new Vector2(0f, gy);
                demoGridHLines.Add(hlRt);
                demoGridHBaseY.Add(gy);
            }

            BuildDemoPlatformsFromLevelData(viewport.transform, demoLevelData);
            BuildDemoCollectibleFromLevelData(viewport.transform, demoLevelData);
            BuildDemoNewtonApparatusFromLevelData(viewport.transform, demoLevelData);
            BuildDemoCharacter(viewport.transform);
            demoBotInputBadgeText = null;

            BuildDemoCircularIrisOverlay(viewport.transform);

            GameObject ruleBox = MainMenuUIBuilder.NewUI("LiveRuleBox", stageCard.transform);
            Image ruleBg = ruleBox.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(ruleBg, LabStyleUI.GetPillSprite(), true, -4f);
            RectTransform ruleRt = ruleBox.GetComponent<RectTransform>();
            ruleRt.sizeDelta = new Vector2(796f, 34f);
            ruleRt.anchoredPosition = new Vector2(0f, -182f);

            GameObject ruleTxtGo = MainMenuUIBuilder.NewUI("RuleText", ruleBox.transform);
            demoRuleCaptionText = ruleTxtGo.AddComponent<Text>();
            demoRuleCaptionText.font = MainMenuUIBuilder.GetFont();
            demoRuleCaptionText.text = "Cùng trạng thái giao thoa (cùng màu) → đi xuyên qua   ·   Khác trạng thái → đứng vững";
            demoRuleCaptionText.fontSize = 13;
            demoRuleCaptionText.fontStyle = FontStyle.Bold;
            demoRuleCaptionText.alignment = TextAnchor.MiddleCenter;
            demoRuleCaptionText.color = LabStyleUI.TextPrimary;
            MainMenuUIBuilder.Stretch(ruleTxtGo.GetComponent<RectTransform>());

            BuildDemoPhaseBar(stageCard.transform);

            ResetDemoMiniGame();
        }

        public void ResetDemoMiniGame()
        {
            Simulation.ResetFull(out bool resetVisuals);
            if (resetVisuals)
                ResetVisualMarkers();
            ApplyDemoCameraToWorldTransforms(TotalTime);
        }

        public bool BindExisting(Transform parent)
        {
            Transform stageCard = parent.Find("GameplayShowcaseCard");
            if (stageCard == null)
                return false;

            Transform viewport = stageCard.Find("DemoViewport");
            if (viewport == null)
                return false;

            Image stageBg = stageCard.GetComponent<Image>();
            if (stageBg != null)
                LabStyleUI.ApplySlicedSprite(stageBg, LabStyleUI.GetCardSprite(), false);

            Transform vpBezel = stageCard.Find("ViewportBezel");
            if (vpBezel != null)
            {
                Image bzImg = vpBezel.GetComponent<Image>();
                if (bzImg != null)
                    LabStyleUI.ApplySlicedSprite(bzImg, LabStyleUI.GetRecessedSlotSprite(), false);
                RectTransform bzRt = vpBezel.GetComponent<RectTransform>();
                if (bzRt != null && Mathf.Abs(bzRt.anchoredPosition.y - 60f) < 2f)
                    bzRt.anchoredPosition = new Vector2(bzRt.anchoredPosition.x, 74f);
            }

            RectTransform vpExistRt = viewport.GetComponent<RectTransform>();
            if (vpExistRt != null && Mathf.Abs(vpExistRt.anchoredPosition.y - 60f) < 2f)
                vpExistRt.anchoredPosition = new Vector2(vpExistRt.anchoredPosition.x, 74f);

            Transform showcaseTitleTr = stageCard.Find("ShowcaseTitle");
            if (showcaseTitleTr != null)
            {
                Text stTxt = showcaseTitleTr.GetComponent<Text>();
                if (stTxt != null && stTxt.text == "Mô phỏng trực quan cơ chế đổi pha màu")
                    stTxt.text = "Mô phỏng cơ chế giao thoa sóng (Cực đại · Trung gian · Cực tiểu)";
            }

            Transform phaseBadge = stageCard.Find("LivePhaseBadge");
            if (phaseBadge != null)
            {
                demoPhaseBadgeBg = phaseBadge.GetComponent<Image>();
                if (demoPhaseBadgeBg != null)
                    LabStyleUI.ApplySlicedSprite(demoPhaseBadgeBg, LabStyleUI.GetPillSprite(), false);
                RectTransform pbRt = phaseBadge.GetComponent<RectTransform>();
                if (pbRt != null && pbRt.sizeDelta.x < 260f)
                {
                    pbRt.sizeDelta = new Vector2(284f, 38f);
                    pbRt.anchoredPosition = new Vector2(256f, pbRt.anchoredPosition.y);
                }
                EnsurePhaseBadgeSwatchAndLabel(phaseBadge);
            }

            Transform ruleBox = stageCard.Find("LiveRuleBox");
            if (ruleBox != null)
            {
                Image ruleBg = ruleBox.GetComponent<Image>();
                if (ruleBg != null)
                    LabStyleUI.ApplySlicedSprite(ruleBg, LabStyleUI.GetPillSprite(), false);
                RectTransform ruleRt = ruleBox.GetComponent<RectTransform>();
                if (ruleRt != null && Mathf.Abs(ruleRt.anchoredPosition.y - (-210f)) < 4f)
                {
                    ruleRt.sizeDelta = new Vector2(796f, 34f);
                    ruleRt.anchoredPosition = new Vector2(ruleRt.anchoredPosition.x, -182f);
                }
                Transform rTxt = ruleBox.Find("RuleText");
                if (rTxt != null)
                {
                    demoRuleCaptionText = rTxt.GetComponent<Text>();
                    if (demoRuleCaptionText != null)
                    {
                        demoRuleCaptionText.fontSize = 13;
                        RectTransform rTxtRt = demoRuleCaptionText.GetComponent<RectTransform>();
                        if (rTxtRt != null)
                        {
                            MainMenuUIBuilder.Stretch(rTxtRt);
                            rTxtRt.localScale = Vector3.one;
                        }
                    }
                }
            }

            Transform barWrap = stageCard.Find("DemoPhaseBarWrap");
            if (barWrap != null)
            {
                UpgradeOrBindDemoPhaseBar(barWrap);
            }

            // Rebuild dynamic viewport internals cleanly inside the existing DemoViewport
            for (int i = viewport.childCount - 1; i >= 0; i--)
            {
                GameObject childGo = viewport.GetChild(i).gameObject;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(childGo);
                else
                    UnityEngine.Object.DestroyImmediate(childGo);
            }

            Simulation.Platforms.Clear();
            demoNewtonRingImgs.Clear();
            demoNewtonRingRts.Clear();
            demoGridVLines.Clear();
            demoGridVBaseX.Clear();
            demoGridHLines.Clear();
            demoGridHBaseY.Clear();

            Simulation.InitializeLevelData();
            LevelData demoLevelData = Simulation.LevelData;

            for (int gx = -480; gx <= 480; gx += 120)
            {
                GameObject vLine = MainMenuUIBuilder.NewUI("GridV_" + gx, viewport);
                Image vlImg = vLine.AddComponent<Image>();
                vlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.06f);
                vlImg.raycastTarget = false;
                RectTransform vlRt = vLine.GetComponent<RectTransform>();
                vlRt.sizeDelta = new Vector2(1f, MainMenuUI.DemoViewportHeightPx);
                vlRt.anchoredPosition = new Vector2(gx, 0f);
                demoGridVLines.Add(vlRt);
                demoGridVBaseX.Add(gx);
            }
            for (int gy = -240; gy <= 240; gy += 80)
            {
                GameObject hLine = MainMenuUIBuilder.NewUI("GridH_" + gy, viewport);
                Image hlImg = hLine.AddComponent<Image>();
                hlImg.color = new Color(0.55f, 0.75f, 0.95f, 0.06f);
                hlImg.raycastTarget = false;
                RectTransform hlRt = hLine.GetComponent<RectTransform>();
                hlRt.sizeDelta = new Vector2(MainMenuUI.DemoViewportWidthPx, 1f);
                hlRt.anchoredPosition = new Vector2(0f, gy);
                demoGridHLines.Add(hlRt);
                demoGridHBaseY.Add(gy);
            }

            BuildDemoPlatformsFromLevelData(viewport, demoLevelData);
            BuildDemoCollectibleFromLevelData(viewport, demoLevelData);
            BuildDemoNewtonApparatusFromLevelData(viewport, demoLevelData);
            BuildDemoCharacter(viewport);
            demoBotInputBadgeText = null;

            BuildDemoCircularIrisOverlay(viewport);
            ResetDemoMiniGame();
            return true;
        }

        private void EnsurePhaseBadgeSwatchAndLabel(Transform phaseBadge)
        {
            Transform swatchTr = phaseBadge.Find("PhaseColorIcon");
            if (swatchTr == null)
            {
                GameObject swGo = MainMenuUIBuilder.NewUI("PhaseColorIcon", phaseBadge);
                swatchTr = swGo.transform;
                demoPhaseBadgeSwatchImg = swGo.AddComponent<Image>();
                Outline swOut = swGo.AddComponent<Outline>();
                swOut.effectColor = new Color(0.75f, 0.90f, 1f, 0.85f);
                swOut.effectDistance = new Vector2(1.2f, -1.2f);
            }
            else
            {
                demoPhaseBadgeSwatchImg = swatchTr.GetComponent<Image>();
            }

            if (demoPhaseBadgeSwatchImg != null)
            {
                demoPhaseBadgeSwatchImg.sprite = LabStyleUI.GetSoftCircleSprite();
                demoPhaseBadgeSwatchImg.color = new Color(0.08f, 0.11f, 0.17f, 1f);
                demoPhaseBadgeSwatchImg.raycastTarget = false;
                RectTransform swRt = demoPhaseBadgeSwatchImg.GetComponent<RectTransform>();
                swRt.anchorMin = new Vector2(0f, 0.5f);
                swRt.anchorMax = new Vector2(0f, 0.5f);
                swRt.pivot = new Vector2(0.5f, 0.5f);
                swRt.anchoredPosition = new Vector2(20f, 0f);
                swRt.sizeDelta = new Vector2(18f, 18f);
            }

            Transform lblTr = phaseBadge.Find("BadgeLabel");
            if (lblTr == null)
            {
                GameObject pbTxtGo = MainMenuUIBuilder.NewUI("BadgeLabel", phaseBadge);
                lblTr = pbTxtGo.transform;
                demoPhaseBadgeText = pbTxtGo.AddComponent<Text>();
            }
            else
            {
                demoPhaseBadgeText = lblTr.GetComponent<Text>();
            }

            if (demoPhaseBadgeText != null)
            {
                demoPhaseBadgeText.font = MainMenuUIBuilder.GetFont();
                demoPhaseBadgeText.text = "Giao thoa cực tiểu · Đen (Δφ=180°)";
                demoPhaseBadgeText.fontSize = 12;
                demoPhaseBadgeText.fontStyle = FontStyle.Bold;
                demoPhaseBadgeText.alignment = TextAnchor.MiddleLeft;
                demoPhaseBadgeText.color = Color.white;
                RectTransform lblRt = demoPhaseBadgeText.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = new Vector2(36f, 0f);
                lblRt.offsetMax = new Vector2(-10f, 0f);
                lblRt.localScale = Vector3.one;
            }
        }

        private void UpgradeOrBindDemoPhaseBar(Transform barWrap, bool isNewBuild = false)
        {
            RectTransform bwRt = barWrap.GetComponent<RectTransform>();
            if (bwRt != null && (isNewBuild || bwRt.sizeDelta.x < 100f))
            {
                bwRt.sizeDelta = new Vector2(796f, 146f);
                bwRt.anchoredPosition = new Vector2(0f, -278f);
            }

            // Hide legacy QuickControlsTag & RuleSummaryFooter so the 3 cards span cleanly across the full width
            Transform quickTag = barWrap.Find("QuickControlsTag");
            if (quickTag != null)
                quickTag.gameObject.SetActive(false);

            Transform footer = barWrap.Find("RuleSummaryFooter");
            if (footer != null)
                footer.gameObject.SetActive(false);

            EnsureThreePhaseHeaderBanner(barWrap);

            const float cardW = 252f;
            const float cardH = 98f;
            const float cardY = -18f;

            // Pha 1 — Giao thoa cực đại (Màu Trắng · Cùng pha Δφ=0°)
            ConfigureOrUpgradeDemoSegment(
                barWrap,
                "SegWhite",
                0,
                "Pha 1",
                "Giao thoa cực đại",
                "Màu Trắng",
                "Cùng pha (Δφ=0°)",
                new Vector2(-270f, cardY),
                new Vector2(cardW, cardH),
                new Color(0.11f, 0.19f, 0.30f, 0.98f),
                new Color(0.68f, 0.76f, 0.85f, 0.90f),
                new Color(0.55f, 0.65f, 0.78f, 0.65f),
                new Color(0.04f, 0.08f, 0.15f, 1.00f),
                new Color(0.18f, 0.30f, 0.45f, 0.96f),
                new Color(0.22f, 0.32f, 0.44f, 0.94f));

            // Pha 2 — Giao thoa trung gian (Màu Xám · Lệch pha Δφ≈90°)
            ConfigureOrUpgradeDemoSegment(
                barWrap,
                "SegGray",
                1,
                "Pha 2",
                "Giao thoa trung gian",
                "Màu Xám",
                "Lệch pha (Δφ≈90°)",
                new Vector2(0f, cardY),
                new Vector2(cardW, cardH),
                new Color(0.10f, 0.16f, 0.24f, 0.98f),
                new Color(0.75f, 0.82f, 0.90f, 0.90f),
                new Color(0.74f, 0.82f, 0.90f, 0.55f),
                new Color(1.00f, 1.00f, 1.00f, 1.00f),
                new Color(0.84f, 0.90f, 0.97f, 0.96f),
                new Color(0.90f, 0.94f, 0.99f, 0.94f));

            // Pha 3 — Giao thoa cực tiểu (Màu Đen · Ngược pha Δφ=180°)
            ConfigureOrUpgradeDemoSegment(
                barWrap,
                "SegBlack",
                2,
                "Pha 3",
                "Giao thoa cực tiểu",
                "Màu Đen",
                "Ngược pha (Δφ=180°)",
                new Vector2(270f, cardY),
                new Vector2(cardW, cardH),
                new Color(0.06f, 0.18f, 0.34f, 0.98f),
                new Color(0.18f, 0.78f, 1.00f, 0.96f),
                new Color(0.18f, 0.48f, 0.76f, 0.65f),
                new Color(1.00f, 1.00f, 1.00f, 1.00f),
                new Color(0.32f, 0.76f, 1.00f, 0.98f),
                new Color(0.86f, 0.94f, 1.00f, 0.95f));

            demoSegWhiteRt = barWrap.Find("SegWhite")?.GetComponent<RectTransform>();
            demoSegGrayRt = barWrap.Find("SegGray")?.GetComponent<RectTransform>();
            demoSegBlackRt = barWrap.Find("SegBlack")?.GetComponent<RectTransform>();

            Transform thumb = barWrap.Find("PhaseThumbIndicator");
            if (thumb == null)
            {
                GameObject thGo = MainMenuUIBuilder.NewUI("PhaseThumbIndicator", barWrap);
                thGo.AddComponent<Image>();
                thumb = thGo.transform;
            }
            demoPhaseThumbRt = thumb.GetComponent<RectTransform>();
            if (demoSegBlackRt != null)
            {
                demoPhaseThumbRt.anchorMin = demoSegBlackRt.anchorMin;
                demoPhaseThumbRt.anchorMax = demoSegBlackRt.anchorMax;
                demoPhaseThumbRt.pivot = demoSegBlackRt.pivot;
                demoPhaseThumbRt.sizeDelta = demoSegBlackRt.sizeDelta + new Vector2(10f, 10f);
                demoPhaseThumbRt.anchoredPosition = demoSegBlackRt.anchoredPosition;
                demoPhaseThumbRt.position = demoSegBlackRt.position;
            }
            else
            {
                demoPhaseThumbRt.sizeDelta = new Vector2(cardW + 10f, cardH + 10f);
                demoPhaseThumbRt.anchoredPosition = new Vector2(270f, cardY);
            }
            thumb.SetAsFirstSibling();
            demoPhaseThumbImg = thumb.GetComponent<Image>();
            if (demoPhaseThumbImg != null)
            {
                LabStyleUI.ApplySlicedSprite(demoPhaseThumbImg, LabStyleUI.GetPrimaryButtonSprite(), false);
                demoPhaseThumbImg.color = new Color(0.32f, 0.88f, 1f, 0.78f);
                demoPhaseThumbImg.raycastTarget = false;
            }
        }

        private static void EnsureThreePhaseHeaderBanner(Transform barWrap)
        {
            Transform headerTr = barWrap.Find("PhaseSectionHeader");
            bool isNewHeader = headerTr == null;
            GameObject headerGo = !isNewHeader
                ? headerTr.gameObject
                : MainMenuUIBuilder.NewUI("PhaseSectionHeader", barWrap);

            if (isNewHeader)
            {
                RectTransform hRt = headerGo.GetComponent<RectTransform>();
                hRt.anchoredPosition = new Vector2(0f, 52f);
                hRt.sizeDelta = new Vector2(796f, 30f);
            }

            // Left horizontal glowing cyan line
            CreateOrUpdateDecorativeImage(
                headerGo.transform,
                "LeftGlowLine",
                null,
                new Color(0.28f, 0.86f, 1.00f, 0.55f),
                new Vector2(-236f, 0f),
                new Vector2(210f, 1.5f));

            // Left "//" cyan parallelogram ticks
            CreateOrUpdateDecorativeImage(
                headerGo.transform,
                "LeftSlashes",
                MainMenuUIBuilder.GetDoubleSlashTicksSprite(),
                new Color(0.32f, 0.92f, 1.00f, 0.96f),
                new Vector2(-118f, 0f),
                new Vector2(22f, 12f));

            // Right "//" cyan parallelogram ticks
            CreateOrUpdateDecorativeImage(
                headerGo.transform,
                "RightSlashes",
                MainMenuUIBuilder.GetDoubleSlashTicksSprite(),
                new Color(0.32f, 0.92f, 1.00f, 0.96f),
                new Vector2(118f, 0f),
                new Vector2(22f, 12f));

            // Right horizontal glowing cyan line
            CreateOrUpdateDecorativeImage(
                headerGo.transform,
                "RightGlowLine",
                null,
                new Color(0.28f, 0.86f, 1.00f, 0.55f),
                new Vector2(236f, 0f),
                new Vector2(210f, 1.5f));

            // Center chamfered pill badge ("3 Pha Giao Thoa")
            Transform badgeTr = headerGo.transform.Find("HeaderBadge");
            bool isNewBadge = badgeTr == null;
            GameObject badgeGo = !isNewBadge
                ? badgeTr.gameObject
                : MainMenuUIBuilder.NewUI("HeaderBadge", headerGo.transform);

            Image badgeImg = badgeGo.GetComponent<Image>() ?? badgeGo.AddComponent<Image>();
            badgeImg.sprite = MainMenuUIBuilder.GetChamferedTagSprite();
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.08f, 0.16f, 0.28f, 0.96f);
            badgeImg.raycastTarget = false;

            Outline badgeOut = badgeGo.GetComponent<Outline>() ?? badgeGo.AddComponent<Outline>();
            badgeOut.effectColor = new Color(0.32f, 0.86f, 1.00f, 0.72f);
            badgeOut.effectDistance = new Vector2(1.2f, -1.2f);

            if (isNewBadge)
            {
                RectTransform bRt = badgeGo.GetComponent<RectTransform>();
                bRt.anchoredPosition = Vector2.zero;
                bRt.sizeDelta = new Vector2(198f, 28f);
            }

            Transform txtTr = badgeGo.transform.Find("HeaderText");
            Text hTxt;
            if (txtTr == null)
            {
                hTxt = MainMenuUIBuilder.CreateSubLabel(
                    badgeGo.transform,
                    "HeaderText",
                    "<color=#4CE0FF><size=15>3</size></color> Pha Giao Thoa",
                    Vector2.zero,
                    new Vector2(190f, 28f),
                    13,
                    FontStyle.Bold,
                    Color.white,
                    TextAnchor.MiddleCenter);
                hTxt.supportRichText = true;
            }
            else
            {
                hTxt = txtTr.GetComponent<Text>();
                if (hTxt != null)
                {
                    hTxt.supportRichText = true;
                    hTxt.text = "<color=#4CE0FF><size=15>3</size></color> Pha Giao Thoa";
                    hTxt.fontSize = 13;
                    hTxt.fontStyle = FontStyle.Bold;
                    hTxt.color = Color.white;
                    hTxt.alignment = TextAnchor.MiddleCenter;
                    RectTransform hTxtRt = hTxt.GetComponent<RectTransform>();
                    if (hTxtRt != null)
                    {
                        hTxtRt.sizeDelta = new Vector2(190f, 28f);
                        hTxtRt.localScale = Vector3.one;
                    }
                }
            }
        }

        private static void CreateOrUpdateDecorativeImage(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector2 pos,
            Vector2 size)
        {
            Transform tr = parent.Find(name);
            bool isNew = tr == null;
            GameObject go = !isNew ? tr.gameObject : MainMenuUIBuilder.NewUI(name, parent);
            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            if (isNew)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
        }

        private void ResetVisualMarkers()
        {
            for (int i = 0; i < 3; i++)
            {
                if (demoNewtonPointRts[i] != null)
                    demoNewtonPointRts[i].gameObject.SetActive(false);
            }
            if (demoConnectLineBRt != null)
                demoConnectLineBRt.sizeDelta = new Vector2(0f, 4.5f);
            if (demoConnectLineSmallBRt != null)
                demoConnectLineSmallBRt.sizeDelta = new Vector2(0f, 4.5f);
            if (demoNewtonFormulaBadgeGo != null)
                demoNewtonFormulaBadgeGo.SetActive(false);
            if (demoReachPointerImg != null)
                demoReachPointerImg.color = new Color(0.35f, 0.96f, 1f, 0f);
            if (demoRunnerRoot != null)
            {
                demoRunnerRoot.localScale = Vector3.one;
                demoRunnerRoot.localRotation = Quaternion.identity;
            }
        }

        public void UpdateShowcase(float time, float dt)
        {
            TotalTime = time;
            float safeDt = Mathf.Clamp(dt, 0.001f, 0.05f);

            // 1. Pulse 5 Concentric Newton Rings (highlighting pair i = 2, k = 4, outer diameter 164 px)
            Color c550 = NewtonRingsPresentation.WavelengthToRgb(550f);
            float[] baseDiameters = { 164f, 136f, 108f, 78f, 46f };
            for (int r = 0; r < demoNewtonRingImgs.Count && r < 5; r++)
            {
                if (demoNewtonRingImgs[r] == null) continue;
                int ringOrder = 5 - r;
                bool isPairIK = (ringOrder == 2 || ringOrder == 4);
                float pulse = 1f + (isPairIK ? 0.045f : 0.025f) * Mathf.Sin(time * 3.0f - r * 0.6f);
                float sz = baseDiameters[r] * pulse;
                demoNewtonRingRts[r].sizeDelta = new Vector2(sz, sz);

                if (isPairIK)
                {
                    Color hiCol = ringOrder == 4
                        ? Color.Lerp(c550, new Color(0.35f, 0.96f, 1f, 1f), 0.45f)
                        : Color.Lerp(c550, new Color(1f, 0.90f, 0.35f, 1f), 0.45f);
                    hiCol.a = 0.94f;
                    demoNewtonRingImgs[r].color = hiCol;
                }
                else
                {
                    demoNewtonRingImgs[r].color = new Color(c550.r, c550.g, c550.b, 0.44f);
                }
            }

            if (demoRunnerRoot == null)
                return;

            // 2. Step Mini-Game Bot & Physics Simulation with deterministic sub-stepping
            Simulation.StepSimulation(
                safeDt,
                out MainMenuUI.DemoBotInput lastBotInput,
                out string ruleText,
                out bool didResetVisualMarkers);

            if (didResetVisualMarkers)
                ResetVisualMarkers();

            if (demoBotInputBadgeText != null && !string.IsNullOrEmpty(lastBotInput.KeyBadgeText))
                demoBotInputBadgeText.text = lastBotInput.KeyBadgeText;

            ApplyDemoCameraToWorldTransforms(time);

            // 3. Compute Character Viewport Position, Rotation & Scale (including Vortex Suck-In at Stage 8)
            Vector2 feetPosPx = new Vector2(
                Simulation.PlayerWorldPos.x * MainMenuUI.DemoPixelsPerUnit,
                (Simulation.PlayerWorldPos.y - Simulation.PlayerSize.y * 0.5f) * MainMenuUI.DemoPixelsPerUnit);

            if (Simulation.BotStage == MainMenuUI.DemoBotStage.SuckedIntoNewtonCenter)
            {
                float u = Mathf.Clamp01(Simulation.StageTimer / 0.90f);
                float ease = u * u * (3f - 2f * u);
                Vector2 targetCenterBottom = Simulation.NewtonCenterPx - new Vector2(0f, 18f * (1f - ease));
                Vector2 worldCharPx = Vector2.Lerp(Simulation.SuckStartPosPx, targetCenterBottom, ease);
                worldCharPx.y += Mathf.Sin(u * Mathf.PI) * 38f;
                demoRunnerRoot.anchoredPosition = worldCharPx - Simulation.CameraPosPx;
                demoRunnerRoot.localRotation = Quaternion.Euler(0f, 0f, -u * 540f);
                float sc = Mathf.Lerp(1f, 0.04f, ease);
                demoRunnerRoot.localScale = new Vector3(sc, sc, 1f);
            }
            else if (Simulation.BotStage == MainMenuUI.DemoBotStage.CircularIrisCloseAndReset && !Simulation.ResetDoneInIris)
            {
                demoRunnerRoot.anchoredPosition = Simulation.NewtonCenterPx - Simulation.CameraPosPx;
                demoRunnerRoot.localScale = Vector3.zero;
            }
            else
            {
                demoRunnerRoot.anchoredPosition = feetPosPx - Simulation.CameraPosPx;
                demoRunnerRoot.localRotation = Quaternion.identity;
                demoRunnerRoot.localScale = Vector3.one;
            }

            int colorIdx = Simulation.PlayerColor == LogicalColor.White
                ? 0
                : (Simulation.PlayerColor == LogicalColor.Gray ? 1 : 2);

            if (colorIdx != lastDemoColorIndex)
            {
                lastDemoColorIndex = colorIdx;
                shiftPulseTimer = 0f;
            }
            shiftPulseTimer += safeDt;
            if (demoShiftPulseRt != null && demoShiftPulseImg != null)
            {
                if (shiftPulseTimer < 0.55f)
                {
                    float p = shiftPulseTimer / 0.55f;
                    float sz = Mathf.Lerp(54f, 220f, p);
                    demoShiftPulseRt.sizeDelta = new Vector2(sz, sz);
                    demoShiftPulseImg.color = new Color(0.42f, 0.92f, 1f, (1f - p) * 0.85f);
                }
                else
                {
                    demoShiftPulseImg.color = new Color(0.42f, 0.92f, 1f, 0f);
                }
            }

            // 4. Sample Authoritative SilhouetteMotion Character Sprites (including 2-armed "AnMung" / "Celebrate")
            bool isMovingHoriz = Mathf.Abs(lastBotInput.MoveAxisX) > 0.05f &&
                                 Simulation.BotStage <= MainMenuUI.DemoBotStage.ShiftBlackJumpToNewtonStage;
            bool isReachingPoints = Simulation.BotStage == MainMenuUI.DemoBotStage.ReachAndPickThreeHorizontalPoints;
            bool isCelebrating = Simulation.BotStage == MainMenuUI.DemoBotStage.CelebrateCompletion;

            string clipMode;
            if (isCelebrating)
            {
                // Only wave BOTH arms when celebrating completion after Newton ring measurement is done
                clipMode = "AnMung";
                demoAnimClock += safeDt * 1.45f;
            }
            else if (isReachingPoints)
            {
                // While creating the 3 Newton ring points (at StageTimer = 0.40s, 1.00s, 1.60s),
                // raise ONE arm up and lower it back down for each point (no 2-arm waving)
                float st = Simulation.StageTimer;
                if (st >= 0.10f && st <= 1.90f)
                {
                    clipMode = "PointMark";
                    demoAnimClock = (st - 0.10f) % 0.60f;
                }
                else
                {
                    clipMode = "Idle";
                    demoAnimClock += safeDt;
                }
            }
            else if (!Simulation.PlayerGrounded)
            {
                clipMode = "Jump";
                demoAnimClock = 0.28f;
            }
            else if (isMovingHoriz)
            {
                clipMode = "Run";
                demoAnimClock += safeDt;
            }
            else
            {
                clipMode = "Idle";
                demoAnimClock += safeDt;
            }

            Sprite bodySprite = PlayerVisualPresentation.SampleBodySprite(clipMode, demoAnimClock, out int frameIdx);
            Sprite rimSprite = PlayerVisualPresentation.SampleRimSprite(clipMode, frameIdx);
            Sprite eyesSprite = PlayerVisualPresentation.SampleEyesSprite(clipMode, frameIdx);
            bool hasRealSprites = bodySprite != null;

            Color bodyCol = colorIdx == 0
                ? PlayerVisualPresentation.BrightBody
                : (colorIdx == 1 ? PlayerVisualPresentation.GrayBody : PlayerVisualPresentation.DarkBody);
            Color rimCol = colorIdx == 0
                ? PlayerVisualPresentation.BrightRim
                : (colorIdx == 1 ? PlayerVisualPresentation.GrayRim : PlayerVisualPresentation.DarkStrokeRim);
            Color haloCol = colorIdx == 0
                ? PlayerVisualPresentation.BrightHalo
                : (colorIdx == 1
                    ? new Color(0.55f, 0.62f, 0.70f, 0.16f)
                    : new Color(0.45f, 0.75f, 0.95f, 0.22f));

            if (isCelebrating)
            {
                haloCol = new Color(1f, 0.90f, 0.38f, 0.42f + 0.18f * Mathf.Sin(time * 10f));
            }

            if (demoCharacterHaloImg != null)
                demoCharacterHaloImg.color = haloCol;

            if (demoCharacterBodyImg != null && demoCharacterRimImg != null)
            {
                demoCharacterBodyImg.gameObject.SetActive(hasRealSprites);
                demoCharacterRimImg.gameObject.SetActive(hasRealSprites);
                if (demoCharacterEyesImg != null)
                    demoCharacterEyesImg.gameObject.SetActive(hasRealSprites && eyesSprite != null);
                if (hasRealSprites)
                {
                    demoCharacterBodyImg.sprite = bodySprite;
                    demoCharacterBodyImg.color = bodyCol;
                    demoCharacterRimImg.sprite = rimSprite != null ? rimSprite : bodySprite;
                    demoCharacterRimImg.color = rimCol;
                    if (demoCharacterEyesImg != null && eyesSprite != null)
                    {
                        demoCharacterEyesImg.sprite = eyesSprite;
                        demoCharacterEyesImg.color = Color.white;
                    }
                }
            }

            // Toggle procedural fallback parts only if real sprite sheets aren't loaded
            if (demoHeadImg != null) demoHeadImg.gameObject.SetActive(!hasRealSprites);
            if (demoTorsoImg != null) demoTorsoImg.gameObject.SetActive(!hasRealSprites);
            if (demoLegLeftImg != null) demoLegLeftImg.gameObject.SetActive(!hasRealSprites);
            if (demoLegRightImg != null) demoLegRightImg.gameObject.SetActive(!hasRealSprites);
            if (!hasRealSprites)
            {
                if (demoHeadImg != null) demoHeadImg.color = bodyCol;
                if (demoTorsoImg != null) demoTorsoImg.color = bodyCol;
                if (demoLegLeftImg != null) demoLegLeftImg.color = bodyCol;
                if (demoLegRightImg != null) demoLegRightImg.color = bodyCol;
                float swing = !Simulation.PlayerGrounded ? 24f : (isMovingHoriz ? Mathf.Sin(time * 13f) * 28f : 0f);
                if (demoLegLeft != null) demoLegLeft.localRotation = Quaternion.Euler(0f, 0f, swing);
                if (demoLegRight != null) demoLegRight.localRotation = Quaternion.Euler(0f, 0f, -swing);
            }

            // 5. Update 3 Horizontal Points (K, I, K') & Connecting Line Animation (B = KI, b = IK')
            Vector2[] ptOffsets =
            {
                new Vector2(-68f, 0f),
                new Vector2(-39f, 0f),
                new Vector2(68f, 0f)
            };

            for (int i = 0; i < 3; i++)
            {
                if (demoNewtonPointRts[i] == null || demoNewtonPointImgs[i] == null)
                    continue;
                bool active = i < Simulation.NewtonMarksCount;
                demoNewtonPointRts[i].gameObject.SetActive(active);
                if (active)
                {
                    float pop = 14f + 2.5f * Mathf.Sin(time * 6f + i);
                    demoNewtonPointRts[i].sizeDelta = new Vector2(pop, pop);
                    Color c = demoNewtonPointImgs[i].color;
                    c.a = 0.98f;
                    demoNewtonPointImgs[i].color = c;
                }
            }

            if (demoReachPointerRt != null && demoReachPointerImg != null)
            {
                if (isReachingPoints)
                {
                    int targetIdx = Mathf.Clamp(Simulation.NewtonMarksCount, 0, 2);
                    Vector2 targetPx = Simulation.NewtonCenterPx + ptOffsets[targetIdx];
                    Vector2 handOriginPx = feetPosPx + new Vector2(44f, 108f);
                    float uReach = Mathf.PingPong(Simulation.StageTimer * 3.2f, 1f);
                    demoReachPointerRt.anchoredPosition = Vector2.Lerp(handOriginPx, targetPx, uReach) - Simulation.CameraPosPx;
                    demoReachPointerImg.color = new Color(0.38f, 0.96f, 1f, 0.88f);
                }
                else
                {
                    demoReachPointerImg.color = new Color(0.38f, 0.96f, 1f, 0f);
                }
            }

            if (demoConnectLineBRt != null && demoConnectLineSmallBRt != null)
            {
                float bProg = Mathf.Clamp01(Simulation.ConnectProgress * 2f);
                float smallBProg = Mathf.Clamp01((Simulation.ConnectProgress - 0.5f) * 2f);
                demoConnectLineBRt.sizeDelta = new Vector2(29f * bProg, 4.5f);
                demoConnectLineSmallBRt.sizeDelta = new Vector2(107f * smallBProg, 4.5f);
            }

            if (demoNewtonFormulaBadgeGo != null)
            {
                demoNewtonFormulaBadgeGo.SetActive(
                    Simulation.BotStage == MainMenuUI.DemoBotStage.AnimateConnectingLines ||
                    Simulation.BotStage == MainMenuUI.DemoBotStage.CelebrateCompletion ||
                    Simulation.BotStage == MainMenuUI.DemoBotStage.SuckedIntoNewtonCenter);
            }

            // 6. Update Circular Iris-In Overlay ("thu nhỏ tròn từ ngoài vào")
            UpdateDemoCircularIrisVisual();

            // 7. Highlight pass-through translucency when platform matches character phase
            if (demoWhiteGateImg != null)
                demoWhiteGateImg.color = new Color(0.95f, 0.98f, 1f, colorIdx == 0 ? 0.30f : 0.96f);
            if (demoWhiteStepImg != null)
                demoWhiteStepImg.color = new Color(0.95f, 0.98f, 1f, colorIdx == 0 ? 0.30f : 0.96f);
            if (demoGrayBridgeImg != null)
                demoGrayBridgeImg.color = new Color(0.53f, 0.57f, 0.62f, colorIdx == 1 ? 0.28f : 0.96f);

            // 8. Animate Quang Điểm collectible
            if (demoQuangDiemRt != null && demoQuangDiemImg != null)
            {
                if (Simulation.QuangDiemCollected)
                {
                    demoQuangDiemImg.color = new Color(1f, 0.90f, 0.40f, 0f);
                }
                else
                {
                    float qPulse = 28f + Mathf.Sin(time * 5f) * 5f;
                    demoQuangDiemRt.sizeDelta = new Vector2(qPulse, qPulse);
                    demoQuangDiemImg.color = new Color(1f, 0.90f, 0.38f, 0.96f);
                }
            }

            // 9. Update Live Phase Badge & Synchronized Mini Phase Bar Thumb
            if (demoPhaseThumbRt != null)
            {
                RectTransform targetSegRt = colorIdx == 0
                    ? demoSegWhiteRt
                    : (colorIdx == 1 ? demoSegGrayRt : demoSegBlackRt);

                if (targetSegRt != null)
                {
                    demoPhaseThumbRt.anchorMin = targetSegRt.anchorMin;
                    demoPhaseThumbRt.anchorMax = targetSegRt.anchorMax;
                    demoPhaseThumbRt.pivot = targetSegRt.pivot;
                    demoPhaseThumbRt.sizeDelta = targetSegRt.sizeDelta + new Vector2(10f, 10f);

                    Vector2 targetPos = targetSegRt.anchoredPosition;
                    if (!Application.isPlaying || TotalTime <= 0.85f)
                    {
                        demoPhaseThumbRt.anchoredPosition = targetPos;
                        demoPhaseThumbRt.position = targetSegRt.position;
                    }
                    else
                    {
                        demoPhaseThumbRt.anchoredPosition = Vector2.Lerp(
                            demoPhaseThumbRt.anchoredPosition,
                            targetPos,
                            Mathf.Clamp01(safeDt * 14f));
                    }
                }
                else
                {
                    float targetThumbX = colorIdx == 0 ? -270f : (colorIdx == 1 ? 0f : 270f);
                    Vector2 cur = demoPhaseThumbRt.anchoredPosition;
                    demoPhaseThumbRt.anchoredPosition = new Vector2(Mathf.Lerp(cur.x, targetThumbX, Mathf.Clamp01(safeDt * 14f)), -18f);
                }
            }

            if (demoPhaseBadgeBg != null && demoPhaseBadgeText != null)
            {
                demoPhaseBadgeBg.sprite = colorIdx == 0
                    ? LabStyleUI.GetPrimaryButtonSprite()
                    : LabStyleUI.GetPillSprite();
                demoPhaseBadgeBg.color = Color.white;
                demoPhaseBadgeText.text = colorIdx == 0
                    ? "Giao thoa cực đại · Trắng (Δφ=0°)"
                    : (colorIdx == 1
                        ? "Giao thoa trung gian · Xám (Δφ=90°)"
                        : "Giao thoa cực tiểu · Đen (Δφ=180°)");
                demoPhaseBadgeText.color = LabStyleUI.TextPrimary;
            }

            if (demoPhaseBadgeSwatchImg != null)
            {
                demoPhaseBadgeSwatchImg.sprite = LabStyleUI.GetSoftCircleSprite();
                demoPhaseBadgeSwatchImg.color = colorIdx == 0
                    ? new Color(0.98f, 0.99f, 1.00f, 1f)
                    : (colorIdx == 1
                        ? new Color(0.62f, 0.68f, 0.76f, 1f)
                        : new Color(0.06f, 0.09f, 0.14f, 1f));
            }

            if (demoRuleCaptionText != null)
                demoRuleCaptionText.text = ruleText;
        }

        private void ApplyDemoCameraToWorldTransforms(float time)
        {
            Vector2 camPx = Simulation.CameraPosPx;

            float modX = ((camPx.x % 120f) + 120f) % 120f;
            for (int g = 0; g < demoGridVLines.Count; g++)
            {
                if (demoGridVLines[g] != null)
                    demoGridVLines[g].anchoredPosition = new Vector2(demoGridVBaseX[g] - modX, 0f);
            }

            float modY = ((camPx.y % 80f) + 80f) % 80f;
            for (int g = 0; g < demoGridHLines.Count; g++)
            {
                if (demoGridHLines[g] != null)
                    demoGridHLines[g].anchoredPosition = new Vector2(0f, demoGridHBaseY[g] - modY);
            }

            for (int i = 0; i < Simulation.Platforms.Count; i++)
            {
                DemoPlatformActor plat = Simulation.Platforms[i];
                if (plat.Rect != null)
                    plat.Rect.anchoredPosition = plat.WorldPos * MainMenuUI.DemoPixelsPerUnit - camPx;
            }

            if (demoWhiteGateTagRt != null)
                demoWhiteGateTagRt.anchoredPosition = demoWhiteGateTagWorldPx - camPx;

            if (demoNewtonRootRt != null)
                demoNewtonRootRt.anchoredPosition = Simulation.NewtonCenterPx - camPx;

            if (demoQuangDiemRt != null)
            {
                Vector2 baseQdPx = new Vector2(-40f, -632f);
                LevelData data = Simulation.LevelData;
                if (data != null && data.collectibles != null && data.collectibles.Length > 0 && data.collectibles[0].position != null)
                {
                    baseQdPx = data.collectibles[0].position.ToVector2() * MainMenuUI.DemoPixelsPerUnit;
                }
                demoQuangDiemRt.anchoredPosition = new Vector2(baseQdPx.x, baseQdPx.y + Mathf.Sin(time * 3.5f) * 5f) - camPx;
            }

            if (demoRunnerRoot != null)
            {
                Vector2 feetPosPx = new Vector2(
                    Simulation.PlayerWorldPos.x * MainMenuUI.DemoPixelsPerUnit,
                    (Simulation.PlayerWorldPos.y - Simulation.PlayerSize.y * 0.5f) * MainMenuUI.DemoPixelsPerUnit);
                demoRunnerRoot.anchoredPosition = feetPosPx - camPx;
            }
        }

        private void BuildDemoPlatformsFromLevelData(Transform viewportTransform, LevelData data)
        {
            if (data == null || data.platforms == null)
                return;

            for (int i = 0; i < data.platforms.Length; i++)
            {
                ColorPlatformData p = data.platforms[i];
                if (p == null || p.position == null || p.size == null)
                    continue;

                Vector2 worldPos = p.position.ToVector2();
                Vector2 worldSize = p.size.ToVector2();
                Vector2 uiPos = worldPos * MainMenuUI.DemoPixelsPerUnit;
                Vector2 uiSize = worldSize * MainMenuUI.DemoPixelsPerUnit;

                LogicalColor logical = ColorClassifier.Classify(p.rawGrayscaleColor);
                bool isUniversal = p.rawGrayscaleColor >= 250f || logical == LogicalColor.Universal;
                Color baseVisual = ColorClassifier.GetPlatformVisualColor(p.rawGrayscaleColor);

                Color fillCol;
                Color borderCol;
                string objName;
                string labelStr = null;

                if (isUniversal)
                {
                    fillCol = new Color(baseVisual.r, baseVisual.g, baseVisual.b, 1f);
                    borderCol = new Color(1.00f, 0.92f, 0.45f, 0.88f);
                    objName = "FloorUniversal";
                    labelStr = "Nền cố định (mọi pha màu đều đứng được)";
                }
                else if (logical == LogicalColor.White)
                {
                    fillCol = new Color(0.95f, 0.98f, 1f, 0.96f);
                    borderCol = new Color(0.75f, 0.90f, 1f, 0.85f);
                    bool isWall = worldSize.y > worldSize.x;
                    objName = isWall ? "WhitePhaseGate" : "WhitePortalStep";
                    labelStr = isWall ? null : "Bệ Cực Đại (Trắng)";
                }
                else if (logical == LogicalColor.Gray)
                {
                    fillCol = new Color(0.55f, 0.60f, 0.66f, 0.96f);
                    borderCol = new Color(0.80f, 0.86f, 0.92f, 0.65f);
                    objName = "GrayPhaseBridge";
                    labelStr = "Bệ Trung Gian (Xám)";
                }
                else
                {
                    fillCol = new Color(0.08f, 0.10f, 0.15f, 0.98f);
                    borderCol = new Color(0.58f, 0.72f, 0.88f, 0.55f);
                    objName = "BlackPedestal";
                    labelStr = "Bệ Cực Tiểu (Đen)";
                }

                Image img = CreateDemoPlatform(viewportTransform, objName, uiPos, uiSize, fillCol, borderCol, labelStr);

                if (objName == "WhitePhaseGate")
                {
                    demoWhiteGateImg = img;
                    demoWhiteGateTagWorldPx = new Vector2(uiPos.x, uiPos.y + uiSize.y * 0.5f + 16f);
                    MainMenuUIBuilder.CreateSubLabel(
                        viewportTransform,
                        "WhiteGateTag",
                        "Tường Cực Đại (Trắng)",
                        demoWhiteGateTagWorldPx,
                        new Vector2(150f, 22f),
                        11,
                        FontStyle.Bold,
                        LabStyleUI.TextPrimary,
                        TextAnchor.MiddleCenter);
                    Transform tagTr = viewportTransform.Find("WhiteGateTag");
                    if (tagTr != null)
                        demoWhiteGateTagRt = tagTr.GetComponent<RectTransform>();
                }
                else if (objName == "GrayPhaseBridge")
                {
                    demoGrayBridgeImg = img;
                }
                else if (objName == "WhitePortalStep")
                {
                    demoWhiteStepImg = img;
                }

                Simulation.Platforms.Add(new DemoPlatformActor
                {
                    Rect = img.GetComponent<RectTransform>(),
                    Image = img,
                    WorldPos = worldPos,
                    WorldSize = worldSize,
                    RawGrayscale = p.rawGrayscaleColor,
                    Logical = logical,
                    IsUniversal = isUniversal
                });
            }
        }

        private static Image CreateDemoPlatform(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            Color fillCol,
            Color borderCol,
            string labelStr)
        {
            GameObject go = MainMenuUIBuilder.NewUI(name, parent);
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
                MainMenuUIBuilder.CreateSubLabel(
                    go.transform,
                    "Tag",
                    labelStr,
                    Vector2.zero,
                    size,
                    12,
                    FontStyle.Bold,
                    isLight ? new Color(0.08f, 0.12f, 0.18f, 0.92f) : new Color(0.90f, 0.95f, 1f, 0.90f),
                    TextAnchor.MiddleCenter);
            }

            return img;
        }

        private void BuildDemoCollectibleFromLevelData(Transform viewportTransform, LevelData data)
        {
            Sprite circleSprite = MainMenuUIBuilder.GetCircleSprite();
            Vector2 qdUiPos = new Vector2(-40f, -632f);
            if (data != null && data.collectibles != null && data.collectibles.Length > 0 && data.collectibles[0].position != null)
            {
                qdUiPos = data.collectibles[0].position.ToVector2() * MainMenuUI.DemoPixelsPerUnit;
            }

            GameObject qdGo = MainMenuUIBuilder.NewUI("DemoQuangDiem", viewportTransform);
            demoQuangDiemImg = qdGo.AddComponent<Image>();
            demoQuangDiemImg.sprite = circleSprite;
            demoQuangDiemImg.color = new Color(1f, 0.88f, 0.36f, 0.95f);
            demoQuangDiemImg.raycastTarget = false;
            demoQuangDiemRt = qdGo.GetComponent<RectTransform>();
            demoQuangDiemRt.anchoredPosition = qdUiPos;
            demoQuangDiemRt.sizeDelta = new Vector2(28f, 28f);
        }

        private void BuildDemoNewtonApparatusFromLevelData(Transform viewportTransform, LevelData data)
        {
            Sprite circle = MainMenuUIBuilder.GetCircleSprite();
            Sprite ring = MainMenuUIBuilder.GetRingSprite();

            GameObject newtonRoot = MainMenuUIBuilder.NewUI("DemoNewtonApparatus", viewportTransform);
            demoNewtonRootRt = newtonRoot.GetComponent<RectTransform>();
            demoNewtonRootRt.anchoredPosition = Simulation.NewtonCenterPx;
            demoNewtonRootRt.sizeDelta = new Vector2(174f, 174f);

            GameObject backGlow = MainMenuUIBuilder.NewUI("NewtonBackGlow", newtonRoot.transform);
            Image bgImg = backGlow.AddComponent<Image>();
            bgImg.sprite = circle;
            bgImg.color = new Color(0.18f, 0.72f, 0.56f, 0.24f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = backGlow.GetComponent<RectTransform>();
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(188f, 188f);

            float[] ringDiameters = { 164f, 136f, 108f, 78f, 46f };
            for (int r = 0; r < 5; r++)
            {
                int ringOrder = 5 - r;
                GameObject ringGo = MainMenuUIBuilder.NewUI("DemoNewtonRing_" + ringOrder, newtonRoot.transform);
                Image rImg = ringGo.AddComponent<Image>();
                rImg.sprite = ring;
                bool isTargetOrder = (ringOrder == 2 || ringOrder == 4);
                rImg.color = isTargetOrder
                    ? new Color(0.35f, 0.96f, 0.68f, 0.94f)
                    : new Color(0.24f, 0.78f, 0.54f, 0.50f);
                rImg.raycastTarget = false;

                RectTransform rRt = ringGo.GetComponent<RectTransform>();
                rRt.anchoredPosition = Vector2.zero;
                rRt.sizeDelta = new Vector2(ringDiameters[r], ringDiameters[r]);

                demoNewtonRingImgs.Add(rImg);
                demoNewtonRingRts.Add(rRt);
            }

            GameObject centerDot = MainMenuUIBuilder.NewUI("NewtonCenterO", newtonRoot.transform);
            Image cImg = centerDot.AddComponent<Image>();
            cImg.sprite = circle;
            cImg.color = new Color(0.85f, 0.98f, 1f, 0.92f);
            cImg.raycastTarget = false;
            RectTransform cRt = centerDot.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(8f, 8f);

            GameObject lineBGo = MainMenuUIBuilder.NewUI("ConnectLine_B_KI", newtonRoot.transform);
            demoConnectLineBImg = lineBGo.AddComponent<Image>();
            demoConnectLineBImg.color = new Color(0.25f, 0.95f, 1f, 0.96f);
            demoConnectLineBImg.raycastTarget = false;
            demoConnectLineBRt = lineBGo.GetComponent<RectTransform>();
            demoConnectLineBRt.pivot = new Vector2(0f, 0.5f);
            demoConnectLineBRt.anchoredPosition = new Vector2(-68f, 0f);
            demoConnectLineBRt.sizeDelta = new Vector2(0f, 4.5f);

            GameObject lineSmallBGo = MainMenuUIBuilder.NewUI("ConnectLine_b_IKp", newtonRoot.transform);
            demoConnectLineSmallBImg = lineSmallBGo.AddComponent<Image>();
            demoConnectLineSmallBImg.color = new Color(1f, 0.88f, 0.32f, 0.96f);
            demoConnectLineSmallBImg.raycastTarget = false;
            demoConnectLineSmallBRt = lineSmallBGo.GetComponent<RectTransform>();
            demoConnectLineSmallBRt.pivot = new Vector2(0f, 0.5f);
            demoConnectLineSmallBRt.anchoredPosition = new Vector2(-39f, 0f);
            demoConnectLineSmallBRt.sizeDelta = new Vector2(0f, 4.5f);

            Vector2[] pointOffsets =
            {
                new Vector2(-68f, 0f),
                new Vector2(-39f, 0f),
                new Vector2(68f, 0f)
            };
            string[] pointLabels = { "K", "I", "K'" };
            Color[] pointColors =
            {
                new Color(0.28f, 0.96f, 1f, 1f),
                new Color(1f, 0.88f, 0.32f, 1f),
                new Color(0.28f, 0.96f, 1f, 1f)
            };

            for (int p = 0; p < 3; p++)
            {
                GameObject ptGo = MainMenuUIBuilder.NewUI("NewtonPoint_" + pointLabels[p], newtonRoot.transform);
                Image ptImg = ptGo.AddComponent<Image>();
                ptImg.sprite = circle;
                ptImg.color = new Color(pointColors[p].r, pointColors[p].g, pointColors[p].b, 0f);
                ptImg.raycastTarget = false;

                RectTransform ptRt = ptGo.GetComponent<RectTransform>();
                ptRt.anchoredPosition = pointOffsets[p];
                ptRt.sizeDelta = new Vector2(14f, 14f);

                MainMenuUIBuilder.CreateSubLabel(
                    ptGo.transform,
                    "PtTag",
                    pointLabels[p],
                    new Vector2(0f, 15f),
                    new Vector2(28f, 16f),
                    11,
                    FontStyle.Bold,
                    pointColors[p],
                    TextAnchor.MiddleCenter);

                demoNewtonPointRts[p] = ptRt;
                demoNewtonPointImgs[p] = ptImg;
                ptGo.SetActive(false);
            }

            demoNewtonFormulaBadgeGo = MainMenuUIBuilder.NewUI("NewtonFormulaBadge", newtonRoot.transform);
            Image fBg = demoNewtonFormulaBadgeGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(fBg, LabStyleUI.GetRecessedSlotSprite(), false);
            fBg.color = new Color(0.06f, 0.14f, 0.22f, 0.94f);
            fBg.raycastTarget = false;
            RectTransform fRt = demoNewtonFormulaBadgeGo.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(0f, 102f);
            fRt.sizeDelta = new Vector2(210f, 26f);

            GameObject fTxtGo = MainMenuUIBuilder.NewUI("FormulaText", demoNewtonFormulaBadgeGo.transform);
            demoNewtonFormulaText = fTxtGo.AddComponent<Text>();
            demoNewtonFormulaText.font = MainMenuUIBuilder.GetFont();
            demoNewtonFormulaText.text = "i = 2, k = 4  ·  Nối B = KI, b = IK'";
            demoNewtonFormulaText.fontSize = 11;
            demoNewtonFormulaText.fontStyle = FontStyle.Bold;
            demoNewtonFormulaText.alignment = TextAnchor.MiddleCenter;
            demoNewtonFormulaText.color = new Color(0.45f, 0.98f, 0.78f, 1f);
            MainMenuUIBuilder.Stretch(fTxtGo.GetComponent<RectTransform>());
            demoNewtonFormulaBadgeGo.SetActive(false);

            MainMenuUIBuilder.CreateSubLabel(
                newtonRoot.transform,
                "NewtonTitleTag",
                "Vân Newton (5 vòng · i=2, k=4)",
                new Vector2(0f, -96f),
                new Vector2(190f, 20f),
                11,
                FontStyle.Bold,
                new Color(0.68f, 0.94f, 0.84f, 0.90f),
                TextAnchor.MiddleCenter);

            GameObject ptrGo = MainMenuUIBuilder.NewUI("DemoReachPointer", viewportTransform);
            demoReachPointerImg = ptrGo.AddComponent<Image>();
            demoReachPointerImg.sprite = ring;
            demoReachPointerImg.color = new Color(0.35f, 0.96f, 1f, 0f);
            demoReachPointerImg.raycastTarget = false;
            demoReachPointerRt = ptrGo.GetComponent<RectTransform>();
            demoReachPointerRt.sizeDelta = new Vector2(22f, 22f);
        }

        private void BuildDemoCharacter(Transform viewportTransform)
        {
            Sprite circle = MainMenuUIBuilder.GetCircleSprite();
            Sprite ring = MainMenuUIBuilder.GetRingSprite();

            GameObject root = MainMenuUIBuilder.NewUI("DemoCharacter", viewportTransform);
            demoRunnerRoot = root.GetComponent<RectTransform>();
            demoRunnerRoot.sizeDelta = new Vector2(MainMenuUI.DemoCharacterWidthPx, MainMenuUI.DemoCharacterHeightPx);
            demoRunnerRoot.pivot = new Vector2(0.5f, 0f); // Bottom-center feet pivot

            GameObject pulseGo = MainMenuUIBuilder.NewUI("PhaseShiftPulse", root.transform);
            demoShiftPulseImg = pulseGo.AddComponent<Image>();
            demoShiftPulseImg.sprite = ring;
            demoShiftPulseImg.color = new Color(0.45f, 0.92f, 1f, 0f);
            demoShiftPulseImg.raycastTarget = false;
            demoShiftPulseRt = pulseGo.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(demoShiftPulseRt);
            demoShiftPulseRt.pivot = new Vector2(0.5f, 0.5f);
            demoShiftPulseRt.anchoredPosition = new Vector2(0f, MainMenuUI.DemoCharacterHeightPx * 0.5f);
            demoShiftPulseRt.sizeDelta = new Vector2(72f, 72f);

            GameObject haloGo = MainMenuUIBuilder.NewUI("GlowHalo", root.transform);
            demoCharacterHaloImg = haloGo.AddComponent<Image>();
            demoCharacterHaloImg.sprite = circle;
            demoCharacterHaloImg.color = new Color(1f, 1f, 1f, 0.25f);
            demoCharacterHaloImg.raycastTarget = false;
            RectTransform haloRt = haloGo.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(haloRt);
            haloRt.pivot = new Vector2(0.5f, 0.5f);
            haloRt.anchoredPosition = new Vector2(0f, MainMenuUI.DemoCharacterHeightPx * 0.5f);
            haloRt.sizeDelta = new Vector2(150f, 150f);

            // Authoritative SilhouetteMotion Body & Rim Layers (includes Idle, Run, Jump, and 2-armed Celebrate/AnMung)
            GameObject bodyGo = MainMenuUIBuilder.NewUI("OpticalBody", root.transform);
            demoCharacterBodyImg = bodyGo.AddComponent<Image>();
            demoCharacterBodyImg.raycastTarget = false;
            demoCharacterBodyImg.preserveAspect = false;
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(bodyRt);
            bodyRt.pivot = new Vector2(0.5f, 0f);
            bodyRt.anchoredPosition = new Vector2(0f, -4f);
            bodyRt.sizeDelta = new Vector2(MainMenuUI.DemoCharacterWidthPx, MainMenuUI.DemoCharacterHeightPx);

            GameObject rimGo = MainMenuUIBuilder.NewUI("OpticalRim", root.transform);
            demoCharacterRimImg = rimGo.AddComponent<Image>();
            demoCharacterRimImg.raycastTarget = false;
            demoCharacterRimImg.preserveAspect = false;
            RectTransform rimRt = rimGo.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(rimRt);
            rimRt.pivot = new Vector2(0.5f, 0f);
            rimRt.anchoredPosition = new Vector2(0f, -4f);
            rimRt.sizeDelta = new Vector2(MainMenuUI.DemoCharacterWidthPx, MainMenuUI.DemoCharacterHeightPx);

            GameObject eyesGo = MainMenuUIBuilder.NewUI("OpticalEyes", root.transform);
            demoCharacterEyesImg = eyesGo.AddComponent<Image>();
            demoCharacterEyesImg.raycastTarget = false;
            demoCharacterEyesImg.preserveAspect = false;
            RectTransform eyesRt = eyesGo.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(eyesRt);
            eyesRt.pivot = new Vector2(0.5f, 0f);
            eyesRt.anchoredPosition = new Vector2(0f, -4f);
            eyesRt.sizeDelta = new Vector2(MainMenuUI.DemoCharacterWidthPx, MainMenuUI.DemoCharacterHeightPx);

            // Fallback procedural silhouette parts (only enabled if SilhouetteMotion sprite sheets are unavailable)
            GameObject legL = MainMenuUIBuilder.NewUI("LegL", root.transform);
            demoLegLeftImg = legL.AddComponent<Image>();
            demoLegLeftImg.color = Color.white;
            demoLegLeftImg.raycastTarget = false;
            demoLegLeft = legL.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(demoLegLeft);
            demoLegLeft.pivot = new Vector2(0.5f, 1f);
            demoLegLeft.anchoredPosition = new Vector2(-14f, 52f);
            demoLegLeft.sizeDelta = new Vector2(16f, 52f);

            GameObject legR = MainMenuUIBuilder.NewUI("LegR", root.transform);
            demoLegRightImg = legR.AddComponent<Image>();
            demoLegRightImg.color = Color.white;
            demoLegRightImg.raycastTarget = false;
            demoLegRight = legR.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(demoLegRight);
            demoLegRight.pivot = new Vector2(0.5f, 1f);
            demoLegRight.anchoredPosition = new Vector2(14f, 52f);
            demoLegRight.sizeDelta = new Vector2(16f, 52f);

            GameObject torso = MainMenuUIBuilder.NewUI("Torso", root.transform);
            demoTorsoImg = torso.AddComponent<Image>();
            demoTorsoImg.color = Color.white;
            demoTorsoImg.raycastTarget = false;
            Outline torsoOut = torso.AddComponent<Outline>();
            torsoOut.effectColor = new Color(0.55f, 0.85f, 1f, 0.65f);
            torsoOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform tRt = torso.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(tRt);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 50f);
            tRt.sizeDelta = new Vector2(54f, 64f);

            GameObject head = MainMenuUIBuilder.NewUI("Head", root.transform);
            demoHeadImg = head.AddComponent<Image>();
            demoHeadImg.sprite = circle;
            demoHeadImg.color = Color.white;
            demoHeadImg.raycastTarget = false;
            Outline headOut = head.AddComponent<Outline>();
            headOut.effectColor = new Color(0.55f, 0.85f, 1f, 0.65f);
            headOut.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform hRt = head.GetComponent<RectTransform>();
            MainMenuUIBuilder.AnchorToBottomCenter(hRt);
            hRt.pivot = new Vector2(0.5f, 0f);
            hRt.anchoredPosition = new Vector2(0f, 114f);
            hRt.sizeDelta = new Vector2(48f, 48f);

            GameObject eye = MainMenuUIBuilder.NewUI("Eye", head.transform);
            demoEyeImg = eye.AddComponent<Image>();
            demoEyeImg.color = new Color(0.20f, 0.85f, 1f, 1f);
            demoEyeImg.raycastTarget = false;
            RectTransform eRt = eye.GetComponent<RectTransform>();
            eRt.anchoredPosition = new Vector2(10f, 2f);
            eRt.sizeDelta = new Vector2(14f, 9f);
        }

        private void BuildDemoCircularIrisOverlay(Transform viewportTransform)
        {
            Color irisVoidColor = new Color(0.02f, 0.035f, 0.065f, 1f);

            demoIrisRootGo = MainMenuUIBuilder.NewUI("DemoCircularIrisMask", viewportTransform);
            MainMenuUIBuilder.Stretch(demoIrisRootGo.GetComponent<RectTransform>());

            GameObject apertureGo = MainMenuUIBuilder.NewUI("IrisAperture", demoIrisRootGo.transform);
            Image apImg = apertureGo.AddComponent<Image>();
            apImg.sprite = MainMenuUIBuilder.GetIrisApertureSprite();
            apImg.color = Color.white;
            apImg.raycastTarget = false;
            demoIrisApertureRt = apertureGo.GetComponent<RectTransform>();
            demoIrisApertureRt.anchoredPosition = Vector2.zero;
            demoIrisApertureRt.sizeDelta = new Vector2(1050f, 1050f);

            demoIrisTopBarRt = CreateIrisFillBar(demoIrisRootGo.transform, "IrisTopBar", irisVoidColor);
            demoIrisBottomBarRt = CreateIrisFillBar(demoIrisRootGo.transform, "IrisBottomBar", irisVoidColor);
            demoIrisLeftBarRt = CreateIrisFillBar(demoIrisRootGo.transform, "IrisLeftBar", irisVoidColor);
            demoIrisRightBarRt = CreateIrisFillBar(demoIrisRootGo.transform, "IrisRightBar", irisVoidColor);

            demoIrisRootGo.SetActive(false);
        }

        private static RectTransform CreateIrisFillBar(Transform parent, string name, Color col)
        {
            GameObject go = MainMenuUIBuilder.NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private void UpdateDemoCircularIrisVisual()
        {
            if (demoIrisRootGo == null || demoIrisApertureRt == null)
                return;

            float irisNorm = Simulation.IrisNormalized;
            if (irisNorm >= 0.995f)
            {
                if (demoIrisRootGo.activeSelf)
                    demoIrisRootGo.SetActive(false);
                return;
            }

            if (!demoIrisRootGo.activeSelf)
                demoIrisRootGo.SetActive(true);

            Vector2 center = Vector2.Lerp(new Vector2(120f, -36f), Vector2.zero, irisNorm);
            float diameter = Mathf.Max(0f, irisNorm * 1060f);
            demoIrisApertureRt.anchoredPosition = center;
            demoIrisApertureRt.sizeDelta = new Vector2(diameter, diameter);

            float half = diameter * 0.49f;
            const float vpHalfW = 420f;
            const float vpHalfH = 250f;

            float topBottom = center.y + half;
            float topH = Mathf.Max(0f, vpHalfH - topBottom);
            demoIrisTopBarRt.anchoredPosition = new Vector2(0f, topBottom + topH * 0.5f);
            demoIrisTopBarRt.sizeDelta = new Vector2(vpHalfW * 2f, topH + 4f);

            float botTop = center.y - half;
            float botH = Mathf.Max(0f, botTop - (-vpHalfH));
            demoIrisBottomBarRt.anchoredPosition = new Vector2(0f, -vpHalfH + botH * 0.5f);
            demoIrisBottomBarRt.sizeDelta = new Vector2(vpHalfW * 2f, botH + 4f);

            float leftRight = center.x - half;
            float leftW = Mathf.Max(0f, leftRight - (-vpHalfW));
            demoIrisLeftBarRt.anchoredPosition = new Vector2(-vpHalfW + leftW * 0.5f, center.y);
            demoIrisLeftBarRt.sizeDelta = new Vector2(leftW + 4f, diameter + 4f);

            float rightLeft = center.x + half;
            float rightW = Mathf.Max(0f, vpHalfW - rightLeft);
            demoIrisRightBarRt.anchoredPosition = new Vector2(rightLeft + rightW * 0.5f, center.y);
            demoIrisRightBarRt.sizeDelta = new Vector2(rightW + 4f, diameter + 4f);
        }

        private void BuildDemoPhaseBar(Transform stageTransform)
        {
            GameObject barWrap = MainMenuUIBuilder.NewUI("DemoPhaseBarWrap", stageTransform);
            UpgradeOrBindDemoPhaseBar(barWrap.transform, true);
        }

        private static void ConfigureOrUpgradeDemoSegment(
            Transform parent,
            string name,
            int phaseIndex,
            string phaseTagStr,
            string titleStr,
            string colorNameStr,
            string natureStr,
            Vector2 pos,
            Vector2 size,
            Color tagBgCol,
            Color slashCol,
            Color dividerCol,
            Color titleCol,
            Color colorNameCol,
            Color natureCol)
        {
            Transform segTr = parent.Find(name);
            GameObject seg;
            if (segTr == null)
            {
                seg = MainMenuUIBuilder.NewUI(name, parent);
                seg.AddComponent<Image>();
            }
            else
            {
                seg = segTr.gameObject;
            }

            Image img = seg.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = MainMenuUIBuilder.GetPhaseCardSprite(phaseIndex);
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                img.raycastTarget = false;
            }

            Shadow cardShadow = seg.GetComponent<Shadow>() ?? seg.AddComponent<Shadow>();
            cardShadow.effectColor = phaseIndex == 2
                ? new Color(0.10f, 0.65f, 1.00f, 0.35f)
                : new Color(0.02f, 0.05f, 0.12f, 0.42f);
            cardShadow.effectDistance = new Vector2(0f, -4f);

            RectTransform rt = seg.GetComponent<RectTransform>();
            if (segTr == null || rt.sizeDelta.x < 200f)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
            Vector2 cardSize = rt.sizeDelta;

            // 1. Top-Left Chamfered Phase Tag ("Pha 1" / "Pha 2" / "Pha 3") + "//" Slashes
            Transform tagTr = seg.transform.Find("PhaseTag");
            GameObject tagGo = tagTr != null ? tagTr.gameObject : MainMenuUIBuilder.NewUI("PhaseTag", seg.transform);
            Image tagImg = tagGo.GetComponent<Image>() ?? tagGo.AddComponent<Image>();
            tagImg.sprite = MainMenuUIBuilder.GetChamferedTagSprite();
            tagImg.type = Image.Type.Sliced;
            tagImg.color = tagBgCol;
            tagImg.raycastTarget = false;
            RectTransform tagRt = tagGo.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0f, 1f);
            tagRt.anchorMax = new Vector2(0f, 1f);
            tagRt.pivot = new Vector2(0f, 1f);
            tagRt.anchoredPosition = new Vector2(10f, -7f);
            tagRt.sizeDelta = new Vector2(56f, 20f);

            Transform tagTxtTr = tagGo.transform.Find("TagLbl");
            Text tagTxt = tagTxtTr != null
                ? tagTxtTr.GetComponent<Text>()
                : MainMenuUIBuilder.CreateSubLabel(tagGo.transform, "TagLbl", phaseTagStr, Vector2.zero, new Vector2(56f, 20f), 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            if (tagTxt != null)
            {
                tagTxt.text = phaseTagStr;
                tagTxt.fontSize = 11;
                tagTxt.fontStyle = FontStyle.Bold;
                tagTxt.color = Color.white;
                tagTxt.alignment = TextAnchor.MiddleCenter;
                RectTransform tagTxtRt = tagTxt.GetComponent<RectTransform>();
                MainMenuUIBuilder.Stretch(tagTxtRt);
                tagTxtRt.localScale = Vector3.one;
            }

            CreateOrUpdateDecorativeImage(
                seg.transform,
                "PhaseSlashes",
                MainMenuUIBuilder.GetDoubleSlashTicksSprite(),
                slashCol,
                new Vector2(-cardSize.x * 0.5f + 78f, cardSize.y * 0.5f - 17f),
                new Vector2(20f, 12f));

            // 2. Large Circle Color Swatch on Left showing exact in-game color (Trắng / Xám / Đen)
            Transform swTr = seg.transform.Find("ColorSwatch");
            GameObject swGo = swTr != null ? swTr.gameObject : MainMenuUIBuilder.NewUI("ColorSwatch", seg.transform);
            Image swImg = swGo.GetComponent<Image>() ?? swGo.AddComponent<Image>();
            swImg.sprite = MainMenuUIBuilder.GetPhaseCircleSwatchSprite(phaseIndex);
            swImg.type = Image.Type.Simple;
            swImg.color = Color.white;
            swImg.raycastTarget = false;
            Outline oldOut = swGo.GetComponent<Outline>();
            if (oldOut != null)
                oldOut.enabled = false;

            RectTransform swRt = swGo.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0f, 0.5f);
            swRt.anchorMax = new Vector2(0f, 0.5f);
            swRt.pivot = new Vector2(0.5f, 0.5f);
            swRt.anchoredPosition = new Vector2(36f, -9f);
            swRt.sizeDelta = new Vector2(44f, 44f);

            // 3. Vertical Divider Line between Circle Swatch and Text Block
            CreateOrUpdateDecorativeImage(
                seg.transform,
                "VertDivider",
                null,
                dividerCol,
                new Vector2(-cardSize.x * 0.5f + 68f, -9f),
                new Vector2(1.5f, 52f));

            // 4. Line 1: Interference Title ("Giao thoa cực đại / trung gian / cực tiểu")
            Transform lblTr = seg.transform.Find("Lbl");
            Text lblTxt = lblTr != null
                ? lblTr.GetComponent<Text>()
                : MainMenuUIBuilder.CreateSubLabel(seg.transform, "Lbl", titleStr, Vector2.zero, size, 14, FontStyle.Bold, titleCol, TextAnchor.MiddleLeft);
            if (lblTxt != null)
            {
                lblTxt.font = MainMenuUIBuilder.GetFont();
                lblTxt.text = titleStr;
                lblTxt.fontSize = 14;
                lblTxt.fontStyle = FontStyle.Bold;
                lblTxt.color = titleCol;
                lblTxt.alignment = TextAnchor.MiddleLeft;
                RectTransform lblRt = lblTxt.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0f, 0.5f);
                lblRt.anchorMax = new Vector2(1f, 0.5f);
                lblRt.pivot = new Vector2(0.5f, 0.5f);
                lblRt.offsetMin = new Vector2(78f, 4f);
                lblRt.offsetMax = new Vector2(-8f, 26f);
                lblRt.localScale = Vector3.one;
            }

            // 5. Line 2: In-Game Color Label ("Màu Trắng" / "Màu Xám" / "Màu Đen")
            Transform colLblTr = seg.transform.Find("ColorNameLbl");
            Text colTxt = colLblTr != null
                ? colLblTr.GetComponent<Text>()
                : MainMenuUIBuilder.CreateSubLabel(seg.transform, "ColorNameLbl", colorNameStr, Vector2.zero, size, 12, FontStyle.Bold, colorNameCol, TextAnchor.MiddleLeft);
            if (colTxt != null)
            {
                colTxt.font = MainMenuUIBuilder.GetFont();
                colTxt.text = colorNameStr;
                colTxt.fontSize = 12;
                colTxt.fontStyle = FontStyle.Bold;
                colTxt.color = colorNameCol;
                colTxt.alignment = TextAnchor.MiddleLeft;
                RectTransform colRt = colTxt.GetComponent<RectTransform>();
                colRt.anchorMin = new Vector2(0f, 0.5f);
                colRt.anchorMax = new Vector2(1f, 0.5f);
                colRt.pivot = new Vector2(0.5f, 0.5f);
                colRt.offsetMin = new Vector2(78f, -15f);
                colRt.offsetMax = new Vector2(-8f, 5f);
                colRt.localScale = Vector3.one;
            }

            // 6. Line 3: Physical Phase Relation ("Cùng pha (Δφ=0°)" / "Lệch pha (Δφ≈90°)" / "Ngược pha (Δφ=180°)")
            Transform natTr = seg.transform.Find("NatureLbl");
            Text natTxt = natTr != null
                ? natTr.GetComponent<Text>()
                : MainMenuUIBuilder.CreateSubLabel(seg.transform, "NatureLbl", natureStr, Vector2.zero, size, 11, FontStyle.Bold, natureCol, TextAnchor.MiddleLeft);
            if (natTxt != null)
            {
                natTxt.font = MainMenuUIBuilder.GetFont();
                natTxt.text = natureStr;
                natTxt.fontSize = 11;
                natTxt.fontStyle = FontStyle.Bold;
                natTxt.color = natureCol;
                natTxt.alignment = TextAnchor.MiddleLeft;
                RectTransform natRt = natTxt.GetComponent<RectTransform>();
                natRt.anchorMin = new Vector2(0f, 0.5f);
                natRt.anchorMax = new Vector2(1f, 0.5f);
                natRt.pivot = new Vector2(0.5f, 0.5f);
                natRt.offsetMin = new Vector2(78f, -34f);
                natRt.offsetMax = new Vector2(-8f, -14f);
                natRt.localScale = Vector3.one;
            }
        }
    }
}

