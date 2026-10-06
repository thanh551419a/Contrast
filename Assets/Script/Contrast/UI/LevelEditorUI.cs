using System;
using System.Collections.Generic;
using Contrast.Core;
using Contrast.Data;
using Contrast.Level;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Runtime editor UI: import/export, test play, selected platform data,
    /// dynamic trajectory editing, safe platform deletion, and palette.
    /// </summary>
    public sealed class LevelEditorUI : MonoBehaviour
    {
        private GameObject root;
        private Text statusText;
        private Text selectedInfoLabel;
        private InputField posX;
        private InputField posY;
        private InputField sizeX;
        private InputField sizeY;
        private PlatformColorSubComponent colorSubComponent;
        private bool suppressFields;
        private ColorPlatform selectedPlatform;
        private LevelEditorController controller;

        // ── Delete button with 2-step confirmation ────────────────
        private Button deleteButton;
        private Text deleteButtonLabel;
        private Image deleteButtonImage;
        private bool pendingDeleteConfirm;
        private float deleteConfirmExpiry;

        private ScrollRect inspectorScrollRect;
        private RectTransform contentRt;

        // ── Trajectory controls ────────────────────────────────────
        private GameObject trajSection;
        private RectTransform trajSectionRt;
        private Toggle     trajEnabled;
        private Dropdown   trajCurveType;
        private Dropdown   trajDomainMode;
        private Dropdown   trajPlaybackMode;
        private InputField trajOriginX;
        private InputField trajOriginY;
        private InputField trajRotation;
        private InputField trajScaleX;
        private InputField trajParamMin;
        private InputField trajParamMax;
        private InputField trajRadius;
        private InputField trajDuration;
        private Toggle     trajReverse;

        // Row GameObjects for dynamic layout repacking
        private GameObject rowCurveType;
        private GameObject rowDomainMode;
        private GameObject rowPlaybackMode;
        private GameObject rowOriginX;
        private GameObject rowOriginY;
        private GameObject rowRotation;
        private GameObject rowScale;
        private GameObject rowRadius;
        private GameObject rowParamMin;
        private GameObject rowParamMax;
        private GameObject boundsGroup;
        private GameObject rowDuration;
        private GameObject rowReverse;
        private GameObject summaryCard;
        private Text       summaryText;
        private GameObject validationGo;
        private Text       trajValidation;

        private InputField trajBoundsXMin;
        private InputField trajBoundsXMax;
        private InputField trajBoundsYMin;
        private InputField trajBoundsYMax;

        private bool suppressTraj;
        private float trajStartY;

        private void Update()
        {
            if (pendingDeleteConfirm && Time.realtimeSinceStartup > deleteConfirmExpiry)
            {
                pendingDeleteConfirm = false;
                UpdateDeleteButtonVisual();
            }
        }

        public void Show(bool visible)
        {
            if (root == null)
                BuildUI();
            root.SetActive(visible);
            if (visible && statusText != null && string.IsNullOrEmpty(statusText.text))
            {
                SetStatus("Level Editor Ready. Drag objects from Palette. Click platform to edit.");
            }
        }

        public void SetSelectedPlatform(ColorPlatform platform)
        {
            SetSelectedPlatform(platform, true);
        }

        public void RefreshSelectedPlatform(ColorPlatform platform)
        {
            if (selectedPlatform == platform)
                SetSelectedPlatform(platform, false);
        }

        private void SetSelectedPlatform(ColorPlatform platform, bool resetScroll)
        {
            selectedPlatform = platform;
            if (platform == null)
            {
                ClearSelectedPlatform();
                return;
            }

            suppressFields = true;
            posX.text = platform.Position.x.ToString("F3");
            posY.text = platform.Position.y.ToString("F3");
            sizeX.text = platform.Size.x.ToString("F3");
            sizeY.text = platform.Size.y.ToString("F3");
            colorSubComponent?.SetRawColor(platform.RawGrayscaleColor);
            suppressFields = false;

            if (selectedInfoLabel != null)
            {
                string colType = platform.IsUniversal ? "Universal (ALL)" : platform.Logical.ToString();
                selectedInfoLabel.text = $"Selected: {platform.name} [{colType}]";
            }

            SetPlatformFieldsInteractable(true);

            pendingDeleteConfirm = false;
            if (deleteButton != null)
            {
                deleteButton.interactable = true;
                UpdateDeleteButtonVisual();
            }

            RefreshTrajectoryUI(platform);

            if (resetScroll && inspectorScrollRect != null)
                inspectorScrollRect.verticalNormalizedPosition = 1f;
        }

        public void ClearSelectedPlatform()
        {
            selectedPlatform = null;
            if (posX == null)
                return;

            suppressFields = true;
            posX.text = string.Empty;
            posY.text = string.Empty;
            sizeX.text = string.Empty;
            sizeY.text = string.Empty;
            colorSubComponent?.SetRawColor(255f);
            suppressFields = false;

            if (selectedInfoLabel != null)
                selectedInfoLabel.text = "No Platform Selected";

            SetPlatformFieldsInteractable(false);

            pendingDeleteConfirm = false;
            if (deleteButton != null)
            {
                deleteButton.interactable = false;
                UpdateDeleteButtonVisual();
            }

            ClearTrajectoryUI();

            if (inspectorScrollRect != null)
                inspectorScrollRect.verticalNormalizedPosition = 1f;
        }

        private void SetPlatformFieldsInteractable(bool interactable)
        {
            if (posX != null) posX.interactable = interactable;
            if (posY != null) posY.interactable = interactable;
            if (sizeX != null) sizeX.interactable = interactable;
            if (sizeY != null) sizeY.interactable = interactable;
            colorSubComponent?.SetInteractable(interactable);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log("[LEVEL_EDITOR] " + message);
        }

        private void BuildUI()
        {
            controller = FindAnyObjectByType<LevelEditorController>();
            EnsureEventSystem();

            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            root = NewUI("EditorRoot", canvasGo.transform);
            Stretch(root.GetComponent<RectTransform>());

            // ── Top Toolbar ────────────────────────────────────────────
            GameObject top = NewUI("TopBar", root.transform);
            Image topImage = top.AddComponent<Image>();
            topImage.color = new UnityEngine.Color(0.04f, 0.04f, 0.05f, 0.90f);
            RectTransform topRt = top.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0f, 76f);
            topRt.anchoredPosition = Vector2.zero;

            CreateTopBarButton(top.transform, "IMPORT", new Vector2(75f, -38f), new Vector2(110f, 48f), new UnityEngine.Color(0.85f, 0.85f, 0.85f, 1f), UnityEngine.Color.black, () => controller?.RequestImport());
            CreateTopBarButton(top.transform, "EXPORT", new Vector2(195f, -38f), new Vector2(110f, 48f), new UnityEngine.Color(0.85f, 0.85f, 0.85f, 1f), UnityEngine.Color.black, () => controller?.RequestExport());
            CreateTopBarButton(top.transform, "TEST PLAY", new Vector2(335f, -38f), new Vector2(145f, 48f), new UnityEngine.Color(0.18f, 0.72f, 0.38f, 1f), UnityEngine.Color.white, () => controller?.RequestPlayLevel());
            CreateTopBarButton(top.transform, "BACK", new Vector2(470f, -38f), new Vector2(100f, 48f), new UnityEngine.Color(0.35f, 0.35f, 0.38f, 1f), UnityEngine.Color.white, () => GameManager.Instance?.ReturnToMainMenu());

            GameObject status = NewUI("Status", top.transform);
            statusText = status.AddComponent<Text>();
            statusText.font = GetFont();
            statusText.fontSize = 20;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.color = new UnityEngine.Color(0.9f, 0.9f, 0.95f, 1f);
            statusText.text = "Level Editor Ready. Drag objects from Palette. Click platform to edit.";
            RectTransform statusRt = status.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0.5f);
            statusRt.anchorMax = new Vector2(1f, 0.5f);
            statusRt.pivot = new Vector2(0f, 0.5f);
            statusRt.offsetMin = new Vector2(540f, -25f);
            statusRt.offsetMax = new Vector2(-20f, 25f);

            // ── Bottom Help Banner ──────────────────────────────────────
            GameObject bottomBar = NewUI("BottomHelpBar", root.transform);
            Image botImg = bottomBar.AddComponent<Image>();
            botImg.color = new UnityEngine.Color(0.04f, 0.04f, 0.05f, 0.88f);
            RectTransform botRt = bottomBar.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0f, 0f);
            botRt.anchorMax = new Vector2(1f, 0f);
            botRt.pivot = new Vector2(0.5f, 0f);
            botRt.sizeDelta = new Vector2(0f, 38f);
            botRt.anchoredPosition = Vector2.zero;

            GameObject helpTextGo = NewUI("HelpText", bottomBar.transform);
            Text helpText = helpTextGo.AddComponent<Text>();
            helpText.font = GetFont();
            helpText.fontSize = 15;
            helpText.alignment = TextAnchor.MiddleCenter;
            helpText.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 1f);
            helpText.text = "CONTROLS:  Left-Click/Drag: Select & Move Platform  |  White Handles: Resize  |  Colored Handles: Trajectory  |  Mid-Mouse: Pan Camera  |  Del: Delete Selected  |  TEST PLAY: Instant Run";
            Stretch(helpTextGo.GetComponent<RectTransform>());

            BuildInspector(root.transform);
            BuildPalette(root.transform);

            root.SetActive(false);
        }

        private void BuildInspector(Transform parent)
        {
            // Scrollable inspector panel
            GameObject panel = NewUI("Inspector", parent);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0.06f, 0.07f, 0.09f, 0.92f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(0f, 38f);
            rt.offsetMax = new Vector2(360f, -76f);

            // Add scroll view inside the inspector panel
            GameObject scrollViewGo = NewUI("ScrollView", panel.transform);
            inspectorScrollRect = scrollViewGo.AddComponent<ScrollRect>();
            inspectorScrollRect.horizontal = false;
            inspectorScrollRect.vertical = true;
            inspectorScrollRect.scrollSensitivity = 40f;
            Image scrollBg = scrollViewGo.AddComponent<Image>();
            scrollBg.color = new UnityEngine.Color(0f, 0f, 0f, 0f);
            RectTransform scrollRt = scrollViewGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(-14f, 0f); // Reserve space for scrollbar

            // Native Vertical Scrollbar
            GameObject scrollbarGo = NewUI("Scrollbar", panel.transform);
            Scrollbar scrollbar = scrollbarGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            RectTransform sbRt = scrollbarGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.sizeDelta = new Vector2(12f, 0f);
            sbRt.anchoredPosition = new Vector2(-1f, 0f);

            Image sbBg = scrollbarGo.AddComponent<Image>();
            sbBg.color = new UnityEngine.Color(0.12f, 0.12f, 0.14f, 0.8f);

            GameObject slidingArea = NewUI("SlidingArea", scrollbarGo.transform);
            Stretch(slidingArea.GetComponent<RectTransform>());

            GameObject sbHandle = NewUI("Handle", slidingArea.transform);
            sbHandle.AddComponent<Image>().color = new UnityEngine.Color(0.45f, 0.48f, 0.55f, 0.9f);
            RectTransform sbHandleRt = sbHandle.GetComponent<RectTransform>();
            sbHandleRt.sizeDelta = Vector2.zero;

            scrollbar.handleRect = sbHandleRt;
            scrollbar.targetGraphic = sbHandle.GetComponent<Image>();
            inspectorScrollRect.verticalScrollbar = scrollbar;
            inspectorScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // Viewport
            GameObject viewport = NewUI("Viewport", scrollViewGo.transform);
            viewport.AddComponent<RectMask2D>();
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            Stretch(viewportRt);

            // Content container
            GameObject content = NewUI("Content", viewport.transform);
            contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 1800f);
            contentRt.anchoredPosition = Vector2.zero;

            inspectorScrollRect.viewport = viewportRt;
            inspectorScrollRect.content = contentRt;
            inspectorScrollRect.verticalNormalizedPosition = 1f;

            // === Selected Platform Info Badge ===
            GameObject infoCard = NewUI("SelectedInfoCard", content.transform);
            Image infoBg = infoCard.AddComponent<Image>();
            infoBg.color = new UnityEngine.Color(0.12f, 0.16f, 0.22f, 0.95f);
            RectTransform infoRt = infoCard.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 1f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.pivot = new Vector2(0f, 1f);
            infoRt.sizeDelta = new Vector2(-24f, 32f);
            infoRt.anchoredPosition = new Vector2(12f, -10f);

            GameObject infoTextGo = NewUI("InfoLabel", infoCard.transform);
            selectedInfoLabel = infoTextGo.AddComponent<Text>();
            selectedInfoLabel.font = GetFont();
            selectedInfoLabel.fontSize = 15;
            selectedInfoLabel.fontStyle = FontStyle.Bold;
            selectedInfoLabel.alignment = TextAnchor.MiddleCenter;
            selectedInfoLabel.color = new UnityEngine.Color(0.4f, 0.85f, 1f, 1f);
            selectedInfoLabel.text = "No Platform Selected";
            Stretch(infoTextGo.GetComponent<RectTransform>());

            // === Platform properties section ===
            CreateLabel(content.transform, "PLATFORM", new Vector2(22f, -50f), 28);
            posX = CreateField(content.transform, "Position X", -105f);
            posY = CreateField(content.transform, "Position Y", -160f);
            sizeX = CreateField(content.transform, "Size X", -215f);
            sizeY = CreateField(content.transform, "Size Y", -270f);

            AddFieldCallback(posX);
            AddFieldCallback(posY);
            AddFieldCallback(sizeX);
            AddFieldCallback(sizeY);

            float curY = -325f;
            colorSubComponent = new PlatformColorSubComponent();
            colorSubComponent.BuildUI(
                content.transform,
                ref curY,
                NewUI,
                GetFont,
                CreateLabel,
                (p, l, opts, yPos) => CreateDropdown(p, l, opts, ref yPos),
                _ => ApplyFields());

            // === Safe Delete platform button (Two-Step Confirmation) ===
            float deleteY = curY - 15f;
            deleteButton = CreateSafeDeleteButton(
                content.transform,
                new Vector2(22f, deleteY),
                new Vector2(300f, 44f),
                OnDeleteButtonClicked);
            deleteButton.interactable = false;

            // === Trajectory section ===
            trajStartY = deleteY - 65f;
            trajSection = NewUI("TrajSection", content.transform);
            trajSectionRt = trajSection.GetComponent<RectTransform>();
            trajSectionRt.anchorMin = new Vector2(0f, 1f);
            trajSectionRt.anchorMax = new Vector2(1f, 1f);
            trajSectionRt.pivot = new Vector2(0f, 1f);
            trajSectionRt.anchoredPosition = new Vector2(0f, trajStartY);
            trajSectionRt.sizeDelta = new Vector2(0f, 1100f);

            CreateLabel(trajSection.transform, "TRAJECTORY", new Vector2(22f, 0f), 26);

            // Enabled toggle
            float y = -38f;
            trajEnabled = CreateToggle(trajSection.transform, "Enabled", ref y);
            trajEnabled.onValueChanged.AddListener(_ => ApplyTrajectory());

            // Presets label & buttons
            y -= 40f;
            CreateLabel(trajSection.transform, "PRESETS", new Vector2(22f, y), 17);
            y -= 28f;
            CreateSmallButton(trajSection.transform, "Line H", new Vector2(22f, y), new Vector2(68f, 30f), ApplyPresetLineH);
            CreateSmallButton(trajSection.transform, "Line V", new Vector2(96f, y), new Vector2(68f, 30f), ApplyPresetLineV);
            CreateSmallButton(trajSection.transform, "Circle", new Vector2(170f, y), new Vector2(68f, 30f), ApplyPresetCircle);
            CreateSmallButton(trajSection.transform, "Arc 180°", new Vector2(244f, y), new Vector2(72f, 30f), ApplyPresetArc180);

            // Build dynamic trajectory rows
            rowCurveType = CreateDropdownRow(trajSection.transform, "Curve Type", new string[] { "Line", "Circle" }, out trajCurveType);
            trajCurveType.onValueChanged.AddListener(_ =>
            {
                UpdateTrajectoryVisibility();
                ApplyTrajectory();
            });

            rowDomainMode = CreateDropdownRow(trajSection.transform, "Domain Mode", new string[] { "ParameterRange", "SpatialBounds" }, out trajDomainMode);
            trajDomainMode.onValueChanged.AddListener(_ =>
            {
                UpdateTrajectoryVisibility();
                ApplyTrajectory();
            });

            rowPlaybackMode = CreateDropdownRow(trajSection.transform, "Playback", new string[] { "Loop", "PingPong", "Once" }, out trajPlaybackMode);
            trajPlaybackMode.onValueChanged.AddListener(_ => ApplyTrajectory());

            rowOriginX = CreateFieldRow(trajSection.transform, "Origin/Center X", out trajOriginX);
            rowOriginY = CreateFieldRow(trajSection.transform, "Origin/Center Y", out trajOriginY);

            rowRotation = CreateFieldRow(trajSection.transform, "Rotation°", out trajRotation);
            rowScale = CreateFieldRow(trajSection.transform, "Length/Scale", out trajScaleX);
            rowRadius = CreateFieldRow(trajSection.transform, "Radius", out trajRadius);

            rowParamMin = CreateFieldRow(trajSection.transform, "Param/Angle Min", out trajParamMin);
            rowParamMax = CreateFieldRow(trajSection.transform, "Param/Angle Max", out trajParamMax);

            boundsGroup = CreateSpatialBoundsGroup(trajSection.transform,
                out trajBoundsXMin, out trajBoundsXMax, out trajBoundsYMin, out trajBoundsYMax);

            rowDuration = CreateFieldRow(trajSection.transform, "Duration (s)", out trajDuration);
            rowReverse = CreateToggleRow(trajSection.transform, "Reverse Dir", out trajReverse);
            trajReverse.onValueChanged.AddListener(_ => ApplyTrajectory());

            // Real-time Trajectory Mathematical Summary Card
            summaryCard = CreateSummaryCard(trajSection.transform, out summaryText);

            // Validation error text
            validationGo = NewUI("TrajValidation", trajSection.transform);
            trajValidation = validationGo.AddComponent<Text>();
            trajValidation.font = GetFont();
            trajValidation.fontSize = 16;
            trajValidation.color = new UnityEngine.Color(1f, 0.35f, 0.35f, 1f);
            trajValidation.alignment = TextAnchor.UpperLeft;
            RectTransform valRt = validationGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0f, 1f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.pivot = new Vector2(0f, 1f);
            valRt.sizeDelta = new Vector2(320f, 40f);

            RepositionTrajectoryRows();
            ClearSelectedPlatform();
        }

        private void BuildPalette(Transform parent)
        {
            GameObject panel = NewUI("Palette", parent);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0.06f, 0.07f, 0.09f, 0.92f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(320f, -114f);
            rt.anchoredPosition = new Vector2(0f, -19f);

            CreateLabel(panel.transform, "PALETTE", new Vector2(-22f, -40f), 32, TextAnchor.MiddleRight);
            CreatePaletteButton(panel.transform, "PLATFORM", LevelMarkerType.End, false, -110f);
            CreatePaletteButton(panel.transform, "START POS", LevelMarkerType.StartPos, true, -185f);
            CreatePaletteButton(panel.transform, "CHECKPOINT", LevelMarkerType.CheckPoint, true, -260f);
            CreatePaletteButton(panel.transform, "END", LevelMarkerType.End, true, -335f);
        }

        // ── Safe Delete Button Handling ────────────────────────────

        private void OnDeleteButtonClicked()
        {
            if (selectedPlatform == null)
                return;

            if (!pendingDeleteConfirm)
            {
                pendingDeleteConfirm = true;
                deleteConfirmExpiry = Time.realtimeSinceStartup + 3f;
                UpdateDeleteButtonVisual();
                SetStatus($"Confirm deletion of {selectedPlatform.name}: Click button again within 3s.");
            }
            else
            {
                pendingDeleteConfirm = false;
                UpdateDeleteButtonVisual();
                controller?.DeleteSelectedPlatform();
            }
        }

        private void UpdateDeleteButtonVisual()
        {
            if (deleteButton == null)
                return;

            if (deleteButtonLabel == null)
                deleteButtonLabel = deleteButton.GetComponentInChildren<Text>();
            if (deleteButtonImage == null)
                deleteButtonImage = deleteButton.GetComponent<Image>();

            if (pendingDeleteConfirm)
            {
                if (deleteButtonLabel != null) deleteButtonLabel.text = "CONFIRM DELETE (CLICK AGAIN)";
                if (deleteButtonImage != null) deleteButtonImage.color = new UnityEngine.Color(0.95f, 0.5f, 0.1f, 1f);
            }
            else
            {
                if (deleteButtonLabel != null) deleteButtonLabel.text = "DELETE PLATFORM";
                if (deleteButtonImage != null) deleteButtonImage.color = new UnityEngine.Color(0.85f, 0.2f, 0.2f, 1f);
            }
        }

        // ── Dynamic Trajectory Row Repacking ───────────────────────

        private void RepositionTrajectoryRows()
        {
            if (trajCurveType == null)
                return;

            bool isLine = trajCurveType.value == (int)TrajectoryCurveType.Line;
            bool isCircle = trajCurveType.value == (int)TrajectoryCurveType.Circle;
            bool isSpatialBounds = isLine && trajDomainMode.value == (int)TrajectoryDomainMode.SpatialBounds;

            float curY = -140f;

            SetRowY(rowCurveType, ref curY, 44f);
            SetRowY(rowDomainMode, ref curY, 44f);
            SetRowY(rowPlaybackMode, ref curY, 44f);
            SetRowY(rowOriginX, ref curY, 40f);
            SetRowY(rowOriginY, ref curY, 40f);

            if (isLine)
            {
                rowRotation.SetActive(true);
                SetRowY(rowRotation, ref curY, 40f);
                rowScale.SetActive(true);
                SetRowY(rowScale, ref curY, 40f);
            }
            else
            {
                rowRotation.SetActive(false);
                rowScale.SetActive(false);
            }

            if (isCircle)
            {
                rowRadius.SetActive(true);
                SetRowY(rowRadius, ref curY, 40f);
            }
            else
            {
                rowRadius.SetActive(false);
            }

            SetRowY(rowParamMin, ref curY, 40f);
            SetRowY(rowParamMax, ref curY, 40f);

            if (isSpatialBounds)
            {
                boundsGroup.SetActive(true);
                SetRowY(boundsGroup, ref curY, 175f);
            }
            else
            {
                boundsGroup.SetActive(false);
            }

            SetRowY(rowDuration, ref curY, 42f);
            SetRowY(rowReverse, ref curY, 36f);
            SetRowY(summaryCard, ref curY, 56f);
            SetRowY(validationGo, ref curY, 40f);

            float totalSectionHeight = Mathf.Abs(curY) + 20f;
            if (trajSectionRt != null)
                trajSectionRt.sizeDelta = new Vector2(0f, totalSectionHeight);

            if (contentRt != null)
                contentRt.sizeDelta = new Vector2(0f, Mathf.Abs(trajStartY) + totalSectionHeight + 40f);
        }

        private static void SetRowY(GameObject go, ref float curY, float step)
        {
            if (go == null)
                return;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(0f, curY);
            curY -= step;
        }

        private void UpdateTrajectoryVisibility()
        {
            if (trajCurveType == null)
                return;

            bool isCircle = trajCurveType.value == (int)TrajectoryCurveType.Circle;

            // SpatialBounds is Line-only
            if (isCircle && trajDomainMode.value == (int)TrajectoryDomainMode.SpatialBounds)
            {
                suppressTraj = true;
                trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
                suppressTraj = false;
            }

            RepositionTrajectoryRows();
            UpdateTrajectorySummary();
        }

        private void UpdateTrajectorySummary()
        {
            if (summaryText == null)
                return;

            TrajectoryData cfg = BuildTrajectoryDataFromUI();
            if (cfg == null || !cfg.enabled)
            {
                summaryText.text = "Trajectory: Disabled (Static Platform)";
                return;
            }

            if (cfg.curveType == TrajectoryCurveType.Line)
            {
                float pMin, pMax;
                Trajectory.GetLineDomain(cfg, out pMin, out pMax);
                float length = Mathf.Abs(pMax - pMin) * Mathf.Abs(cfg.scaleX);
                float speed = cfg.duration > 0f
                    ? (cfg.playbackMode == TrajectoryPlaybackMode.PingPong ? (length * 2f / cfg.duration) : (length / cfg.duration))
                    : 0f;
                string dirLabel = cfg.reverseDirection ? " [Reverse]" : "";
                summaryText.text = $"LINE | Len: {length:F1}m | {cfg.playbackMode}{dirLabel} | Dur: {cfg.duration:F1}s | Spd: {speed:F1}m/s";
            }
            else if (cfg.curveType == TrajectoryCurveType.Circle)
            {
                float span = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                float r = Mathf.Max(0.001f, cfg.radius);
                float arcLen = (span * Mathf.Deg2Rad) * r;
                float speed = cfg.duration > 0f
                    ? (cfg.playbackMode == TrajectoryPlaybackMode.PingPong ? (arcLen * 2f / cfg.duration) : (arcLen / cfg.duration))
                    : 0f;
                string kind = span >= 355f ? "CIRCLE" : $"ARC ({span:F0}°)";
                string dirLabel = cfg.reverseDirection ? " [Reverse]" : "";
                summaryText.text = $"{kind} | R: {r:F1}m | Arc: {arcLen:F1}m | {cfg.playbackMode}{dirLabel} | Spd: {speed:F1}m/s";
            }
        }

        // ── Presets ────────────────────────────────────────────────

        private void ApplyPresetLineH()
        {
            if (selectedPlatform == null) return;
            suppressTraj = true;
            trajEnabled.isOn = true;
            trajCurveType.value = (int)TrajectoryCurveType.Line;
            trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
            trajPlaybackMode.value = (int)TrajectoryPlaybackMode.PingPong;
            trajOriginX.text = selectedPlatform.Position.x.ToString("F3");
            trajOriginY.text = selectedPlatform.Position.y.ToString("F3");
            trajRotation.text = "0.0";
            trajScaleX.text = "5.00";
            trajParamMin.text = "0.00";
            trajParamMax.text = "1.00";
            trajDuration.text = "4.00";
            trajReverse.isOn = false;
            suppressTraj = false;
            UpdateTrajectoryVisibility();
            ApplyTrajectory();
        }

        private void ApplyPresetLineV()
        {
            if (selectedPlatform == null) return;
            suppressTraj = true;
            trajEnabled.isOn = true;
            trajCurveType.value = (int)TrajectoryCurveType.Line;
            trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
            trajPlaybackMode.value = (int)TrajectoryPlaybackMode.PingPong;
            trajOriginX.text = selectedPlatform.Position.x.ToString("F3");
            trajOriginY.text = selectedPlatform.Position.y.ToString("F3");
            trajRotation.text = "90.0";
            trajScaleX.text = "5.00";
            trajParamMin.text = "0.00";
            trajParamMax.text = "1.00";
            trajDuration.text = "4.00";
            trajReverse.isOn = false;
            suppressTraj = false;
            UpdateTrajectoryVisibility();
            ApplyTrajectory();
        }

        private void ApplyPresetCircle()
        {
            if (selectedPlatform == null) return;
            suppressTraj = true;
            trajEnabled.isOn = true;
            trajCurveType.value = (int)TrajectoryCurveType.Circle;
            trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
            trajPlaybackMode.value = (int)TrajectoryPlaybackMode.Loop;
            trajOriginX.text = selectedPlatform.Position.x.ToString("F3");
            trajOriginY.text = selectedPlatform.Position.y.ToString("F3");
            trajRadius.text = "2.00";
            trajParamMin.text = "0.00";
            trajParamMax.text = "360.00";
            trajDuration.text = "4.00";
            trajReverse.isOn = false;
            suppressTraj = false;
            UpdateTrajectoryVisibility();
            ApplyTrajectory();
        }

        private void ApplyPresetArc180()
        {
            if (selectedPlatform == null) return;
            suppressTraj = true;
            trajEnabled.isOn = true;
            trajCurveType.value = (int)TrajectoryCurveType.Circle;
            trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
            trajPlaybackMode.value = (int)TrajectoryPlaybackMode.PingPong;
            trajOriginX.text = selectedPlatform.Position.x.ToString("F3");
            trajOriginY.text = selectedPlatform.Position.y.ToString("F3");
            trajRadius.text = "2.00";
            trajParamMin.text = "0.00";
            trajParamMax.text = "180.00";
            trajDuration.text = "4.00";
            trajReverse.isOn = false;
            suppressTraj = false;
            UpdateTrajectoryVisibility();
            ApplyTrajectory();
        }

        // ── Trajectory UI helpers ──────────────────────────────────

        private void RefreshTrajectoryUI(ColorPlatform platform)
        {
            if (trajSection == null)
                return;

            trajSection.SetActive(true);

            Trajectory traj = platform.GetTrajectory();
            TrajectoryData cfg = traj?.Config;

            suppressTraj = true;

            if (cfg == null)
            {
                trajEnabled.isOn = false;
                trajCurveType.value = (int)TrajectoryCurveType.Line;
                trajDomainMode.value = (int)TrajectoryDomainMode.ParameterRange;
                trajPlaybackMode.value = (int)TrajectoryPlaybackMode.Loop;
                trajOriginX.text = platform.Position.x.ToString("F3");
                trajOriginY.text = platform.Position.y.ToString("F3");
                trajRotation.text = "0.0";
                trajScaleX.text = "5.00";
                trajParamMin.text = "0.00";
                trajParamMax.text = "1.00";
                trajRadius.text = "2.00";
                trajDuration.text = "4.00";
                trajReverse.isOn = false;
                trajBoundsXMin.text = (platform.Position.x - 5f).ToString("F1");
                trajBoundsXMax.text = (platform.Position.x + 5f).ToString("F1");
                trajBoundsYMin.text = (platform.Position.y - 5f).ToString("F1");
                trajBoundsYMax.text = (platform.Position.y + 5f).ToString("F1");
                if (trajValidation != null)
                    trajValidation.text = "";
                SetTrajectoryFieldsInteractable(false);
                suppressTraj = false;
                UpdateTrajectoryVisibility();
                return;
            }

            trajEnabled.isOn = cfg.enabled;
            trajCurveType.value = (int)cfg.curveType;
            trajDomainMode.value = (int)cfg.domainMode;
            trajPlaybackMode.value = (int)cfg.playbackMode;
            trajOriginX.text = cfg.originX.ToString("F3");
            trajOriginY.text = cfg.originY.ToString("F3");
            trajRotation.text = cfg.rotation.ToString("F1");
            trajScaleX.text = cfg.scaleX.ToString("F2");
            trajParamMin.text = cfg.paramMin.ToString("F2");
            trajParamMax.text = cfg.paramMax.ToString("F2");
            trajRadius.text = cfg.radius.ToString("F2");
            trajDuration.text = cfg.duration.ToString("F2");
            trajReverse.isOn = cfg.reverseDirection;

            trajBoundsXMin.text = cfg.boundsXMin.ToString("F1");
            trajBoundsXMax.text = cfg.boundsXMax.ToString("F1");
            trajBoundsYMin.text = cfg.boundsYMin.ToString("F1");
            trajBoundsYMax.text = cfg.boundsYMax.ToString("F1");

            string error = Trajectory.Validate(cfg);
            if (trajValidation != null)
                trajValidation.text = error ?? "";

            SetTrajectoryFieldsInteractable(cfg.enabled);
            suppressTraj = false;

            UpdateTrajectoryVisibility();
        }

        private void ClearTrajectoryUI()
        {
            if (trajSection == null)
                return;

            suppressTraj = true;
            trajEnabled.isOn = false;
            trajSection.SetActive(false);
            if (trajValidation != null)
                trajValidation.text = "";
            suppressTraj = false;
        }

        private void SetTrajectoryFieldsInteractable(bool interactable)
        {
            if (trajCurveType != null) trajCurveType.interactable = interactable;
            if (trajDomainMode != null) trajDomainMode.interactable = interactable;
            if (trajPlaybackMode != null) trajPlaybackMode.interactable = interactable;
            if (trajOriginX != null) trajOriginX.interactable = interactable;
            if (trajOriginY != null) trajOriginY.interactable = interactable;
            if (trajRotation != null) trajRotation.interactable = interactable;
            if (trajScaleX != null) trajScaleX.interactable = interactable;
            if (trajParamMin != null) trajParamMin.interactable = interactable;
            if (trajParamMax != null) trajParamMax.interactable = interactable;
            if (trajRadius != null) trajRadius.interactable = interactable;
            if (trajDuration != null) trajDuration.interactable = interactable;
            if (trajReverse != null) trajReverse.interactable = interactable;
            if (trajBoundsXMin != null) trajBoundsXMin.interactable = interactable;
            if (trajBoundsXMax != null) trajBoundsXMax.interactable = interactable;
            if (trajBoundsYMin != null) trajBoundsYMin.interactable = interactable;
            if (trajBoundsYMax != null) trajBoundsYMax.interactable = interactable;
        }

        private void ApplyTrajectory()
        {
            if (suppressTraj || selectedPlatform == null || controller == null)
                return;

            TrajectoryData data = BuildTrajectoryDataFromUI();
            if (data == null)
                return;

            string error = Trajectory.Validate(data);
            if (trajValidation != null)
                trajValidation.text = error ?? "";

            controller.ApplySelectedTrajectoryFromUI(data);
            SetTrajectoryFieldsInteractable(data.enabled);
            UpdateTrajectoryVisibility();
        }

        private TrajectoryData BuildTrajectoryDataFromUI()
        {
            TrajectoryData data = new TrajectoryData();
            data.enabled = trajEnabled != null && trajEnabled.isOn;
            data.curveType = (TrajectoryCurveType)(trajCurveType != null ? trajCurveType.value : 0);
            data.domainMode = (TrajectoryDomainMode)(trajDomainMode != null ? trajDomainMode.value : 0);
            data.playbackMode = (TrajectoryPlaybackMode)(trajPlaybackMode != null ? trajPlaybackMode.value : 0);

            data.originX = TryParseField(trajOriginX, 0f);
            data.originY = TryParseField(trajOriginY, 0f);
            data.rotation = TryParseField(trajRotation, 0f);
            data.scaleX = TryParseField(trajScaleX, 5f);
            data.paramMin = TryParseField(trajParamMin, 0f);
            data.paramMax = TryParseField(trajParamMax, 1f);
            data.radius = TryParseField(trajRadius, 2f);
            data.duration = TryParseField(trajDuration, 4f);
            data.reverseDirection = trajReverse != null && trajReverse.isOn;

            data.boundsXMin = TryParseField(trajBoundsXMin, -10f);
            data.boundsXMax = TryParseField(trajBoundsXMax, 10f);
            data.boundsYMin = TryParseField(trajBoundsYMin, -10f);
            data.boundsYMax = TryParseField(trajBoundsYMax, 10f);

            return data;
        }

        private static float TryParseField(InputField field, float fallback)
        {
            if (field == null || string.IsNullOrEmpty(field.text))
                return fallback;
            return float.TryParse(field.text, out float v) ? v : fallback;
        }

        // ── UI creation helpers ────────────────────────────────────

        private void CreatePaletteButton(Transform parent, string label, LevelMarkerType type, bool marker, float y)
        {
            GameObject go = NewUI(label + "PaletteItem", parent);
            Image image = go.AddComponent<Image>();
            image.color = new UnityEngine.Color(0.88f, 0.88f, 0.90f, 1f);
            LevelEditorPaletteItem item = go.AddComponent<LevelEditorPaletteItem>();
            item.Initialize(controller, type, marker);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(250f, 56f);
            rt.anchoredPosition = new Vector2(-35f, y);

            GameObject textGo = NewUI("Label", go.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new UnityEngine.Color(0.1f, 0.1f, 0.12f, 1f);
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private InputField CreateField(Transform parent, string label, float y)
        {
            CreateLabel(parent, label, new Vector2(22f, y), 20);
            GameObject go = NewUI(label + "Field", parent);
            Image image = go.AddComponent<Image>();
            image.color = UnityEngine.Color.white;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(170f, 38f);
            rt.anchoredPosition = new Vector2(155f, y + 8f);

            GameObject textGo = NewUI("Text", go.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 20;
            text.color = UnityEngine.Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(textGo.GetComponent<RectTransform>());

            InputField field = go.AddComponent<InputField>();
            field.textComponent = text;
            field.contentType = InputField.ContentType.DecimalNumber;
            return field;
        }

        private GameObject CreateFieldRow(Transform parent, string label, out InputField outField)
        {
            GameObject row = NewUI(label + "Row", parent);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.sizeDelta = new Vector2(0f, 38f);

            CreateLabel(row.transform, label, new Vector2(22f, 0f), 17);

            GameObject fieldGo = NewUI("Field", row.transform);
            Image image = fieldGo.AddComponent<Image>();
            image.color = UnityEngine.Color.white;
            RectTransform ft = fieldGo.GetComponent<RectTransform>();
            ft.anchorMin = new Vector2(0f, 1f);
            ft.anchorMax = new Vector2(0f, 1f);
            ft.pivot = new Vector2(0f, 1f);
            ft.sizeDelta = new Vector2(125f, 32f);
            ft.anchoredPosition = new Vector2(175f, 4f);

            GameObject textGo = NewUI("Text", fieldGo.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 17;
            text.color = UnityEngine.Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(textGo.GetComponent<RectTransform>());

            outField = fieldGo.AddComponent<InputField>();
            outField.textComponent = text;
            outField.contentType = InputField.ContentType.DecimalNumber;
            outField.onEndEdit.AddListener(_ => ApplyTrajectory());

            return row;
        }

        private GameObject CreateToggleRow(Transform parent, string label, out Toggle outToggle)
        {
            GameObject row = NewUI(label + "Row", parent);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.sizeDelta = new Vector2(0f, 34f);

            CreateLabel(row.transform, label, new Vector2(22f, 0f), 17);

            GameObject toggleGo = NewUI("Toggle", row.transform);
            RectTransform rt = toggleGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.anchoredPosition = new Vector2(175f, 2f);

            GameObject bgGo = NewUI("Background", toggleGo.transform);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new UnityEngine.Color(0.25f, 0.25f, 0.28f, 1f);
            Stretch(bgGo.GetComponent<RectTransform>());

            GameObject checkGo = NewUI("Checkmark", bgGo.transform);
            Image checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new UnityEngine.Color(0.2f, 0.9f, 0.3f, 1f);
            RectTransform checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.15f, 0.15f);
            checkRt.anchorMax = new Vector2(0.85f, 0.85f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            outToggle = toggleGo.AddComponent<Toggle>();
            outToggle.targetGraphic = bgImg;
            outToggle.graphic = checkImg;
            outToggle.isOn = false;

            return row;
        }

        private GameObject CreateDropdownRow(Transform parent, string label, string[] options, out Dropdown outDropdown)
        {
            GameObject row = NewUI(label + "Row", parent);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.sizeDelta = new Vector2(0f, 40f);

            CreateLabel(row.transform, label, new Vector2(22f, 0f), 17);

            float localY = 0f;
            outDropdown = CreateDropdown(row.transform, label, options, ref localY);
            RectTransform ddRt = outDropdown.GetComponent<RectTransform>();
            ddRt.anchoredPosition = new Vector2(150f, 4f);

            return row;
        }

        private GameObject CreateSpatialBoundsGroup(
            Transform parent,
            out InputField xMin, out InputField xMax,
            out InputField yMin, out InputField yMax)
        {
            GameObject grp = NewUI("BoundsGroup", parent);
            RectTransform grpRt = grp.GetComponent<RectTransform>();
            grpRt.anchorMin = new Vector2(0f, 1f);
            grpRt.anchorMax = new Vector2(1f, 1f);
            grpRt.pivot = new Vector2(0f, 1f);
            grpRt.sizeDelta = new Vector2(0f, 175f);

            float curY = 0f;
            CreateLabel(grp.transform, "SPATIAL BOUNDS (AABB)", new Vector2(22f, curY), 16, TextAnchor.MiddleLeft);
            curY -= 35f;

            CreateFieldRowInParent(grp.transform, "Bounds X Min", curY, out xMin);
            curY -= 36f;
            CreateFieldRowInParent(grp.transform, "Bounds X Max", curY, out xMax);
            curY -= 36f;
            CreateFieldRowInParent(grp.transform, "Bounds Y Min", curY, out yMin);
            curY -= 36f;
            CreateFieldRowInParent(grp.transform, "Bounds Y Max", curY, out yMax);

            return grp;
        }

        private void CreateFieldRowInParent(Transform parent, string label, float y, out InputField outField)
        {
            CreateLabel(parent, label, new Vector2(30f, y), 16);

            GameObject fieldGo = NewUI("Field", parent);
            Image image = fieldGo.AddComponent<Image>();
            image.color = UnityEngine.Color.white;
            RectTransform ft = fieldGo.GetComponent<RectTransform>();
            ft.anchorMin = new Vector2(0f, 1f);
            ft.anchorMax = new Vector2(0f, 1f);
            ft.pivot = new Vector2(0f, 1f);
            ft.sizeDelta = new Vector2(125f, 30f);
            ft.anchoredPosition = new Vector2(175f, y + 4f);

            GameObject textGo = NewUI("Text", fieldGo.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 16;
            text.color = UnityEngine.Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(textGo.GetComponent<RectTransform>());

            outField = fieldGo.AddComponent<InputField>();
            outField.textComponent = text;
            outField.contentType = InputField.ContentType.DecimalNumber;
            outField.onEndEdit.AddListener(_ => ApplyTrajectory());
        }

        private GameObject CreateSummaryCard(Transform parent, out Text outSummaryText)
        {
            GameObject card = NewUI("SummaryCard", parent);
            Image img = card.AddComponent<Image>();
            img.color = new UnityEngine.Color(0.10f, 0.14f, 0.18f, 0.9f);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(1f, 1f);
            cardRt.pivot = new Vector2(0f, 1f);
            cardRt.sizeDelta = new Vector2(-24f, 48f);
            cardRt.anchoredPosition = new Vector2(12f, 0f);

            GameObject textGo = NewUI("SummaryText", card.transform);
            outSummaryText = textGo.AddComponent<Text>();
            outSummaryText.font = GetFont();
            outSummaryText.fontSize = 14;
            outSummaryText.color = new UnityEngine.Color(0.45f, 0.85f, 1f, 1f);
            outSummaryText.alignment = TextAnchor.MiddleCenter;
            Stretch(textGo.GetComponent<RectTransform>());

            return card;
        }

        private Toggle CreateToggle(Transform parent, string label, ref float y)
        {
            CreateLabel(parent, label, new Vector2(22f, y), 18);

            GameObject go = NewUI(label + "Toggle", parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.anchoredPosition = new Vector2(175f, y + 2f);

            GameObject bgGo = NewUI("Background", go.transform);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new UnityEngine.Color(0.25f, 0.25f, 0.28f, 1f);
            Stretch(bgGo.GetComponent<RectTransform>());

            GameObject checkGo = NewUI("Checkmark", bgGo.transform);
            Image checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new UnityEngine.Color(0.2f, 0.9f, 0.3f, 1f);
            RectTransform checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.15f, 0.15f);
            checkRt.anchorMax = new Vector2(0.85f, 0.85f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = false;

            return toggle;
        }

        private Dropdown CreateDropdown(Transform parent, string label, string[] options, ref float y)
        {
            CreateLabel(parent, label, new Vector2(22f, y), 18);

            GameObject go = NewUI(label + "Dropdown", parent);
            Image ddImg = go.AddComponent<Image>();
            ddImg.color = new UnityEngine.Color(0.9f, 0.92f, 0.95f, 1f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(150f, 34f);
            rt.anchoredPosition = new Vector2(150f, y + 5f);

            GameObject captionGo = NewUI("Label", go.transform);
            Text captionText = captionGo.AddComponent<Text>();
            captionText.font = GetFont();
            captionText.fontSize = 16;
            captionText.color = UnityEngine.Color.black;
            captionText.alignment = TextAnchor.MiddleLeft;
            Stretch(captionGo.GetComponent<RectTransform>());

            GameObject template = NewUI("Template", go.transform);
            Image templateImg = template.AddComponent<Image>();
            templateImg.color = new UnityEngine.Color(0.95f, 0.95f, 0.95f, 1f);
            RectTransform templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.sizeDelta = new Vector2(0f, 120f);
            ScrollRect templateScroll = template.AddComponent<ScrollRect>();
            templateScroll.horizontal = false;

            GameObject templateViewport = NewUI("Viewport", template.transform);
            templateViewport.AddComponent<RectMask2D>();
            Stretch(templateViewport.GetComponent<RectTransform>());
            templateScroll.viewport = templateViewport.GetComponent<RectTransform>();

            GameObject templateContent = NewUI("Content", templateViewport.transform);
            RectTransform contentRtLocal = templateContent.GetComponent<RectTransform>();
            contentRtLocal.anchorMin = new Vector2(0f, 1f);
            contentRtLocal.anchorMax = new Vector2(1f, 1f);
            contentRtLocal.pivot = new Vector2(0.5f, 1f);
            contentRtLocal.sizeDelta = new Vector2(0f, 28f);
            templateScroll.content = contentRtLocal;

            GameObject item = NewUI("Item", templateContent.transform);
            RectTransform itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 28f);
            item.AddComponent<Toggle>();

            GameObject itemLabelGo = NewUI("Item Label", item.transform);
            Text itemLabelText = itemLabelGo.AddComponent<Text>();
            itemLabelText.font = GetFont();
            itemLabelText.fontSize = 16;
            itemLabelText.color = UnityEngine.Color.black;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            Stretch(itemLabelGo.GetComponent<RectTransform>());

            template.SetActive(false);

            Dropdown dd = go.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            dd.captionText = captionText;
            dd.itemText = itemLabelText;
            dd.template = templateRt;

            dd.ClearOptions();
            var optList = new List<Dropdown.OptionData>();
            foreach (string opt in options)
                optList.Add(new Dropdown.OptionData(opt));
            dd.AddOptions(optList);

            return dd;
        }

        private Button CreateSafeDeleteButton(
            Transform parent, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = NewUI("DeletePlatformButton", parent);
            deleteButtonImage = go.AddComponent<Image>();
            deleteButtonImage.color = new UnityEngine.Color(0.85f, 0.2f, 0.2f, 1f);
            deleteButton = go.AddComponent<Button>();
            deleteButton.targetGraphic = deleteButtonImage;
            deleteButton.onClick.AddListener(action);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            GameObject textGo = NewUI("Label", go.transform);
            deleteButtonLabel = textGo.AddComponent<Text>();
            deleteButtonLabel.font = GetFont();
            deleteButtonLabel.text = "DELETE PLATFORM";
            deleteButtonLabel.fontSize = 17;
            deleteButtonLabel.fontStyle = FontStyle.Bold;
            deleteButtonLabel.alignment = TextAnchor.MiddleCenter;
            deleteButtonLabel.color = UnityEngine.Color.white;
            deleteButtonLabel.raycastTarget = false;
            Stretch(textGo.GetComponent<RectTransform>());

            return deleteButton;
        }

        private Button CreateSmallButton(
            Transform parent, string label, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = NewUI(label + "PresetBtn", parent);
            Image img = go.AddComponent<Image>();
            img.color = new UnityEngine.Color(0.22f, 0.45f, 0.75f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(action);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            GameObject textGo = NewUI("Label", go.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 13;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = UnityEngine.Color.white;
            text.raycastTarget = false;
            Stretch(textGo.GetComponent<RectTransform>());

            return button;
        }

        private static void CreateTopBarButton(
            Transform parent, string label, Vector2 position, Vector2 size,
            UnityEngine.Color bgColor, UnityEngine.Color textColor,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = NewUI(label + "TopButton", parent);
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(action);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            GameObject textGo = NewUI("Label", go.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 19;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.raycastTarget = false;
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private void AddFieldCallback(InputField field)
        {
            field.onEndEdit.AddListener(_ => ApplyFields());
        }

        private void ApplyFields()
        {
            if (suppressFields || selectedPlatform == null || controller == null)
                return;

            if (!float.TryParse(posX.text, out float x) ||
                !float.TryParse(posY.text, out float y) ||
                !float.TryParse(sizeX.text, out float sx) ||
                !float.TryParse(sizeY.text, out float sy))
                return;

            float raw = colorSubComponent != null ? colorSubComponent.CurrentRawColor : 255f;

            controller.ApplySelectedPlatformFromUI(
                new Vector2(x, y),
                new Vector2(Mathf.Max(0.1f, sx), Mathf.Max(0.1f, sy)),
                Mathf.Clamp(raw, 0f, 255f));
        }

        private static void CreateLabel(Transform parent, string textValue, Vector2 position, int fontSize, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject go = NewUI(textValue + "Label", parent);
            Text text = go.AddComponent<Text>();
            text.font = GetFont();
            text.text = textValue;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = UnityEngine.Color.white;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(280f, fontSize + 10f);
            rt.anchoredPosition = position;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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
    }
}
