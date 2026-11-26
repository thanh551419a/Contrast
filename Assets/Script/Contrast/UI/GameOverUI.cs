namespace Contrast.UI
{
    using System.Collections.Generic;
    using Contrast.Audio;
    using Contrast.Core;
    using Contrast.Level;
    using Contrast.Presentation;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Virtual Physics Lab Result & Level Completion UI for Contrast.
    /// Styled with frosted rounded slate-navy cards, recessed measurement readout slot,
    /// royal-blue primary CTA button, matte-slate secondary buttons, and generous horizontal spacing.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private GameObject panel;
        private Image badgeBg;
        private Text badgeText;
        private Text titleText;
        private Text subText;
        private GameObject statsBox;
        private Text statsText;
        private Button retryButton;
        private Button restartButton;
        private Button returnButton;
        private Text hintLabel;

        private void Awake()
        {
            EnsureInitialized();
            ShowResult(false, "GAME OVER", false);
        }

        public void EnsureInitialized()
        {
            if (panel != null)
                return;

            EnsureEventSystem();

            Transform existingPanel = transform.Find("Canvas/Panel");
            if (existingPanel != null && TryBindExistingUI(existingPanel))
            {
                if (panel != null)
                    LabStyleUI.CrispenTextHierarchy(panel.transform);
                return;
            }

            Transform staleCanvas = transform.Find("Canvas");
            if (staleCanvas != null)
            {
                if (Application.isPlaying)
                    Destroy(staleCanvas.gameObject);
                else
                    DestroyImmediate(staleCanvas.gameObject);
            }

            BuildUI();
            if (panel != null)
                LabStyleUI.CrispenTextHierarchy(panel.transform);
        }

        private bool TryBindExistingUI(Transform existingPanel)
        {
            Transform card = existingPanel.Find("ResultCard");
            if (card == null)
                return false;

            panel = existingPanel.gameObject;
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg != null)
            {
                panelBg.color = new Color(0.016f, 0.030f, 0.056f, 0.0f);
                panelBg.raycastTarget = false;
            }

            Image cardBg = card.GetComponent<Image>();
            if (cardBg != null)
                LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), false);

            Transform badgeGo = card.Find("StatusBadge");
            if (badgeGo != null)
            {
                badgeBg = badgeGo.GetComponent<Image>();
                if (badgeBg != null)
                    LabStyleUI.ApplySlicedSprite(badgeBg, LabStyleUI.GetPillSprite(), false);
                badgeText = badgeGo.Find("BadgeText")?.GetComponent<Text>();
            }

            titleText = card.Find("Title")?.GetComponent<Text>();
            subText = card.Find("Subtitle")?.GetComponent<Text>();

            Transform sBox = card.Find("StatsBox");
            if (sBox != null)
            {
                statsBox = sBox.gameObject;
                Image sBg = statsBox.GetComponent<Image>();
                if (sBg != null)
                    LabStyleUI.ApplySlicedSprite(sBg, LabStyleUI.GetRecessedSlotSprite(), false);
                statsText = sBox.Find("StatsText")?.GetComponent<Text>();
            }

            Transform btnRow = card.Find("ButtonRow");
            if (btnRow != null)
            {
                retryButton = btnRow.Find("RETRYButton")?.GetComponent<Button>();
                restartButton = btnRow.Find("RESTARTButton")?.GetComponent<Button>();
                returnButton = btnRow.Find("RETURNButton")?.GetComponent<Button>();
            }

            hintLabel = (card.Find("KeyboardHint") ?? card.Find("Hint"))?.GetComponent<Text>();
            ApplyBottomDockedCardLayout(card.GetComponent<RectTransform>());
            return retryButton != null && restartButton != null && returnButton != null;
        }

        private static void ApplyBottomDockedCardLayout(RectTransform cardRt)
        {
            if (cardRt == null)
                return;

            // Dock centered horizontally and flush near the bottom edge so Newton rings stay unobstructed
            cardRt.anchorMin = new Vector2(0.5f, 0f);
            cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.pivot = new Vector2(0.5f, 0f);
            cardRt.sizeDelta = new Vector2(1120f, 224f);
            cardRt.anchoredPosition = new Vector2(0f, 12f);

            RectTransform badgeRt = cardRt.Find("StatusBadge")?.GetComponent<RectTransform>();
            if (badgeRt != null)
            {
                badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
                badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRt.pivot = new Vector2(0.5f, 0.5f);
                badgeRt.sizeDelta = new Vector2(560f, 26f);
                badgeRt.anchoredPosition = new Vector2(0f, 88f);
            }

            RectTransform titleRt = cardRt.Find("Title")?.GetComponent<RectTransform>();
            if (titleRt != null)
            {
                titleRt.anchorMin = new Vector2(0.5f, 0.5f);
                titleRt.anchorMax = new Vector2(0.5f, 0.5f);
                titleRt.pivot = new Vector2(0.5f, 0.5f);
                titleRt.sizeDelta = new Vector2(1000f, 34f);
                titleRt.anchoredPosition = new Vector2(0f, 54f);
                Text t = titleRt.GetComponent<Text>();
                if (t != null)
                {
                    bool isSupersampled = Mathf.Abs(titleRt.localScale.x - 0.5f) < 0.05f;
                    t.fontSize = isSupersampled ? 52 : 26;
                }
            }

            RectTransform subRt = cardRt.Find("Subtitle")?.GetComponent<RectTransform>();
            if (subRt != null)
            {
                subRt.anchorMin = new Vector2(0.5f, 0.5f);
                subRt.anchorMax = new Vector2(0.5f, 0.5f);
                subRt.pivot = new Vector2(0.5f, 0.5f);
                subRt.sizeDelta = new Vector2(960f, 22f);
                subRt.anchoredPosition = new Vector2(0f, 24f);
                Text st = subRt.GetComponent<Text>();
                if (st != null)
                {
                    bool isSupersampled = Mathf.Abs(subRt.localScale.x - 0.5f) < 0.05f;
                    st.fontSize = isSupersampled ? 28 : 14;
                }
            }

            RectTransform statsRt = cardRt.Find("StatsBox")?.GetComponent<RectTransform>();
            if (statsRt != null)
            {
                statsRt.anchorMin = new Vector2(0.5f, 0.5f);
                statsRt.anchorMax = new Vector2(0.5f, 0.5f);
                statsRt.pivot = new Vector2(0.5f, 0.5f);
                statsRt.sizeDelta = new Vector2(960f, 32f);
                statsRt.anchoredPosition = new Vector2(0f, -8f);
            }

            RectTransform rowRt = cardRt.Find("ButtonRow")?.GetComponent<RectTransform>();
            if (rowRt != null)
            {
                rowRt.anchorMin = new Vector2(0.5f, 0.5f);
                rowRt.anchorMax = new Vector2(0.5f, 0.5f);
                rowRt.pivot = new Vector2(0.5f, 0.5f);
                rowRt.sizeDelta = new Vector2(1040f, 54f);
                rowRt.anchoredPosition = new Vector2(0f, -56f);
            }

            RectTransform hintRt = (cardRt.Find("KeyboardHint") ?? cardRt.Find("Hint"))?.GetComponent<RectTransform>();
            if (hintRt != null)
            {
                hintRt.anchorMin = new Vector2(0.5f, 0.5f);
                hintRt.anchorMax = new Vector2(0.5f, 0.5f);
                hintRt.pivot = new Vector2(0.5f, 0.5f);
                hintRt.sizeDelta = new Vector2(900f, 18f);
                hintRt.anchoredPosition = new Vector2(0f, -97f);
            }
        }

        public void Show(bool visible)
        {
            ShowResult(visible, "GAME OVER", false);
        }

        public void ShowResult(bool visible, string title, bool allowRetry)
        {
            EnsureInitialized();
            bool isWin = title == "YOU WIN" || title.Contains("COMPLETE") || title.Contains("HOÀN THÀNH");
            Color accent = isWin
                ? new Color(0.45f, 0.92f, 0.80f, 1f)
                : new Color(0.98f, 0.56f, 0.58f, 1f);

            ApplyAccentTheme(accent, isWin
                ? "Hoàn thành thí nghiệm giao thoa"
                : "Mất đồng bộ pha quang học");

            if (titleText != null)
                titleText.text = isWin ? "HOÀN THÀNH MÀN CHƠI" : "THẤT BẠI";

            if (subText != null)
            {
                subText.text = isWin
                    ? "Đã xác lập trục giao thoa K–O và hoàn tất phép đo bước sóng ánh sáng"
                    : "Hãy khôi phục điểm lưu hoặc bắt đầu lại thí nghiệm";
            }

            RefreshOpticalStats(isWin);

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(allowRetry);
                ConfigureButton(
                    retryButton,
                    "ĐIỂM LƯU GẦN NHẤT",
                    LabStyleUI.GetPrimaryButtonSprite(),
                    LabStyleUI.IconKind.Retry,
                    OnRetryClicked);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                ConfigureButton(
                    restartButton,
                    "CHƠI LẠI MÀN",
                    allowRetry ? LabStyleUI.GetSecondaryButtonSprite() : LabStyleUI.GetPrimaryButtonSprite(),
                    LabStyleUI.IconKind.Restart,
                    OnRestartClicked);
            }

            if (returnButton != null)
            {
                returnButton.gameObject.SetActive(true);
                ConfigureButton(
                    returnButton,
                    "Quay lại Menu",
                    LabStyleUI.GetPillSprite(),
                    LabStyleUI.IconKind.ChevronLeft,
                    OnReturnClicked);
            }

            if (hintLabel != null)
            {
                hintLabel.text = isWin
                    ? "Nhấn ENTER / R để tiếp tục  ·  ESC để quay lại Menu chính"
                    : "Nhấn R để chơi lại màn  ·  ESC để quay lại Menu chính";
            }

            ArrangeActiveButtons();

            if (panel != null)
                panel.SetActive(visible);
        }

        public void ShowLevelComplete(
            string levelTitle,
            string nextLevelTitle,
            UnityEngine.Events.UnityAction onNextLevel,
            UnityEngine.Events.UnityAction onReplayLevel,
            UnityEngine.Events.UnityAction onMainMenu)
        {
            EnsureInitialized();
            Color accent = GetCurrentNewtonAccent(new Color(0.45f, 0.92f, 0.80f, 1f));
            ApplyAccentTheme(accent, $"Hoàn thành · {levelTitle}");

            if (titleText != null)
                titleText.text = "HOÀN THÀNH THÍ NGHIỆM";

            if (subText != null)
                subText.text = $"Kết quả đo đã được ghi nhận. Tiếp theo: {nextLevelTitle}";

            RefreshOpticalStats(true);

            // Button 1: Royal-Blue Primary CTA ("MÀN TIẾP THEO")
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
                ConfigureButton(
                    retryButton,
                    "MÀN TIẾP THEO",
                    LabStyleUI.GetPrimaryButtonSprite(),
                    LabStyleUI.IconKind.PlayTriangle,
                    onNextLevel ?? OnRestartClicked);
            }

            // Button 2: Matte-Slate Secondary ("CHƠI LẠI MÀN")
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                ConfigureButton(
                    restartButton,
                    "CHƠI LẠI MÀN",
                    LabStyleUI.GetSecondaryButtonSprite(),
                    LabStyleUI.IconKind.Restart,
                    onReplayLevel ?? OnRestartClicked);
            }

            // Button 3: Stadium Pill ("Quay lại Menu")
            if (returnButton != null)
            {
                returnButton.gameObject.SetActive(true);
                ConfigureButton(
                    returnButton,
                    "Quay lại Menu",
                    LabStyleUI.GetPillSprite(),
                    LabStyleUI.IconKind.ChevronLeft,
                    onMainMenu ?? OnReturnClicked);
            }

            if (hintLabel != null)
                hintLabel.text = "Nhấn ENTER sang màn tiếp theo  ·  R chơi lại  ·  ESC quay lại Menu chính";

            ArrangeActiveButtons();

            if (panel != null)
                panel.SetActive(true);
        }

        public void ShowCampaignComplete(
            UnityEngine.Events.UnityAction onPlayAgain,
            UnityEngine.Events.UnityAction onMainMenu)
        {
            EnsureInitialized();
            Color accent = GetCurrentNewtonAccent(new Color(0.48f, 0.86f, 1f, 1f));
            ApplyAccentTheme(accent, "Hoàn tất trọn vẹn 3 thí nghiệm quang học (L01 · L02 · L03)");

            if (titleText != null)
                titleText.text = "HOÀN THÀNH TOÀN BỘ THÍ NGHIỆM";

            if (subText != null)
                subText.text = "Chúc mừng! Bạn đã đo thành công bước sóng ánh sáng Lục, Đỏ và Xanh dương";

            RefreshOpticalStats(true);

            // Button 1: Matte-Slate Secondary ("CHƠI LẠI MÀN 3")
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
                ConfigureButton(
                    retryButton,
                    "CHƠI LẠI MÀN 3",
                    LabStyleUI.GetSecondaryButtonSprite(),
                    LabStyleUI.IconKind.Restart,
                    OnRestartClicked);
            }

            // Button 2: Royal-Blue Primary CTA ("CHƠI TỪ ĐẦU")
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                ConfigureButton(
                    restartButton,
                    "CHƠI TỪ ĐẦU",
                    LabStyleUI.GetPrimaryButtonSprite(),
                    LabStyleUI.IconKind.PlayTriangle,
                    onPlayAgain ?? OnRestartClicked);
            }

            // Button 3: Stadium Pill ("Quay lại Menu")
            if (returnButton != null)
            {
                returnButton.gameObject.SetActive(true);
                ConfigureButton(
                    returnButton,
                    "Quay lại Menu",
                    LabStyleUI.GetPillSprite(),
                    LabStyleUI.IconKind.ChevronLeft,
                    onMainMenu ?? OnReturnClicked);
            }

            if (hintLabel != null)
                hintLabel.text = "Cảm ơn bạn đã hoàn thành chuỗi thí nghiệm CONTRAST  ·  ESC quay lại Menu chính";

            ArrangeActiveButtons();

            if (panel != null)
                panel.SetActive(true);
        }

        private void ApplyAccentTheme(Color accent, string badgeStr)
        {
            if (badgeText != null)
            {
                badgeText.text = badgeStr;
                badgeText.color = Color.Lerp(accent, Color.white, 0.35f);
            }
        }

        private void RefreshOpticalStats(bool showStats)
        {
            if (statsBox == null || statsText == null)
                return;

            if (!showStats)
            {
                statsBox.SetActive(false);
                return;
            }

            statsBox.SetActive(true);
            int collected = QuangDiemItem.CollectedCount;
            int total = QuangDiemItem.TotalCount;
            string qdPart = total > 0
                ? $"✦ Quang Điểm: {collected} / {total}"
                : "✦ Giao thoa hoàn tất";

            EasyNewtonFinaleController newton = EasyNewtonFinaleController.Instance;
            if (newton != null && newton.CalculatedLambda_nm > 1f)
            {
                string colVi = NewtonRingsPresentation.GetWavelengthColorNameVi(newton.CalculatedLambda_nm);
                statsText.text = $"{qdPart}      ·      ◎ Bước sóng đo được:  λ ≈ {newton.CalculatedLambda_nm:F1} nm  (Ánh sáng {colVi})";
            }
            else
            {
                statsText.text = $"{qdPart}      ·      ◎ Đồng bộ pha quang học chính xác";
            }
        }

        private static Color GetCurrentNewtonAccent(Color fallback)
        {
            EasyNewtonFinaleController newton = EasyNewtonFinaleController.Instance;
            if (newton != null && newton.SourceLambda > 1f)
            {
                Color c = NewtonRingsPresentation.WavelengthToRgb(newton.SourceLambda);
                return Color.Lerp(c, Color.white, 0.25f);
            }
            return fallback;
        }

        /// <summary>
        /// Dynamically centers all visible buttons with generous horizontal spacing (>= 78px).
        /// </summary>
        private void ArrangeActiveButtons()
        {
            List<Button> active = new List<Button>(3);
            if (retryButton != null && retryButton.gameObject.activeSelf)
                active.Add(retryButton);
            if (restartButton != null && restartButton.gameObject.activeSelf)
                active.Add(restartButton);
            if (returnButton != null && returnButton.gameObject.activeSelf)
                active.Add(returnButton);

            if (active.Count == 0)
                return;

            float buttonWidth = active.Count <= 2 ? 296f : 280f;
            float buttonHeight = 50f;
            float gap = active.Count <= 2 ? 112f : 78f;
            float step = buttonWidth + gap;
            float startX = -0.5f * (active.Count - 1) * step;

            for (int i = 0; i < active.Count; i++)
            {
                RectTransform rt = active[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                    rt.anchoredPosition = new Vector2(startX + i * step, 0f);
                }
            }
        }

        private static void ConfigureButton(
            Button btn,
            string mainText,
            Sprite bgSprite,
            LabStyleUI.IconKind iconKind,
            UnityEngine.Events.UnityAction onClick)
        {
            if (btn == null)
                return;

            Image img = btn.GetComponent<Image>();
            if (img != null)
                LabStyleUI.ApplySlicedSprite(img, bgSprite, true, -4f);

            btn.colors = LabStyleUI.MakeButtonColorBlock();

            Image iconImg = btn.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
                iconImg.sprite = LabStyleUI.GetIconSprite(iconKind);

            Text label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.text = mainText;

            btn.onClick.RemoveAllListeners();
            string btnName = btn.gameObject.name;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[UI_BUTTON][GameOverUI] Clicked {btnName} ({mainText})");
                ContrastAudioManager.Instance?.PlayButtonClick();
                onClick?.Invoke();
            });
        }

        private void OnRestartClicked()
        {
            Debug.Log("[UI_BUTTON][GameOverUI] OnRestartClicked -> Restart");
            GameManager.Instance?.Restart();
        }

        private void OnRetryClicked()
        {
            Debug.Log("[UI_BUTTON][GameOverUI] OnRetryClicked -> Retry");
            GameManager.Instance?.Retry();
        }

        private void OnReturnClicked()
        {
            Debug.Log("[UI_BUTTON][GameOverUI] OnReturnClicked -> ReturnFromGameplay");
            GameManager.Instance?.ReturnFromGameplay();
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.016f, 0.030f, 0.056f, 0.0f);
            bg.raycastTarget = false;
            Stretch(panel.GetComponent<RectTransform>());

            // Frosted Slate-Navy Modal Card (Docked at bottom-center so Newton rings remain visible)
            GameObject card = NewUI("ResultCard", panel.transform);
            Image cardBg = card.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), true, -7f);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0f);
            cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.pivot = new Vector2(0.5f, 0f);
            cardRt.sizeDelta = new Vector2(1120f, 224f);
            cardRt.anchoredPosition = new Vector2(0f, 12f);

            // Status Stadium Pill
            GameObject badgeGo = NewUI("StatusBadge", card.transform);
            badgeBg = badgeGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(badgeBg, LabStyleUI.GetPillSprite(), false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(560f, 26f);
            badgeRt.anchoredPosition = new Vector2(0f, 88f);

            GameObject badgeTxtGo = NewUI("BadgeText", badgeGo.transform);
            badgeText = badgeTxtGo.AddComponent<Text>();
            badgeText.font = LabStyleUI.GetFont();
            badgeText.text = "Hoàn thành thí nghiệm giao thoa";
            badgeText.fontSize = 13;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = LabStyleUI.TextSecondary;
            Stretch(badgeTxtGo.GetComponent<RectTransform>());

            // Main Title
            GameObject title = NewUI("Title", card.transform);
            titleText = title.AddComponent<Text>();
            titleText.font = LabStyleUI.GetFont();
            titleText.text = "HOÀN THÀNH THÍ NGHIỆM";
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = LabStyleUI.TextPrimary;

            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1000f, 34f);
            titleRt.anchoredPosition = new Vector2(0f, 54f);

            // Subtitle
            GameObject sub = NewUI("Subtitle", card.transform);
            subText = sub.AddComponent<Text>();
            subText.font = LabStyleUI.GetFont();
            subText.text = "Đã xác lập trục giao thoa K–O và hoàn tất phép đo bước sóng ánh sáng";
            subText.fontSize = 14;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = LabStyleUI.TextSecondary;

            RectTransform subRt = sub.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(960f, 22f);
            subRt.anchoredPosition = new Vector2(0f, 24f);

            // Recessed Measurement Summary Slot (matching [giá trị  mm] slot aesthetic)
            statsBox = NewUI("StatsBox", card.transform);
            Image statsBg = statsBox.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(statsBg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform statsRt = statsBox.GetComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(960f, 32f);
            statsRt.anchoredPosition = new Vector2(0f, -8f);

            GameObject statsTxtGo = NewUI("StatsText", statsBox.transform);
            statsText = statsTxtGo.AddComponent<Text>();
            statsText.font = LabStyleUI.GetFont();
            statsText.text = string.Empty;
            statsText.fontSize = 14;
            statsText.fontStyle = FontStyle.Bold;
            statsText.alignment = TextAnchor.MiddleCenter;
            statsText.color = LabStyleUI.TextPrimary;
            Stretch(statsTxtGo.GetComponent<RectTransform>());

            // Button Row Container
            GameObject btnRow = NewUI("ButtonRow", card.transform);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(1040f, 54f);
            rowRt.anchoredPosition = new Vector2(0f, -56f);

            retryButton = CreateStyledButton(
                btnRow.transform,
                "RETRY",
                "ĐIỂM LƯU GẦN NHẤT",
                LabStyleUI.IconKind.Retry,
                new Vector2(-358f, 0f),
                OnRetryClicked);

            restartButton = CreateStyledButton(
                btnRow.transform,
                "RESTART",
                "CHƠI LẠI MÀN",
                LabStyleUI.IconKind.Restart,
                new Vector2(0f, 0f),
                OnRestartClicked);

            returnButton = CreateStyledButton(
                btnRow.transform,
                "RETURN",
                "Quay lại Menu",
                LabStyleUI.IconKind.ChevronLeft,
                new Vector2(358f, 0f),
                OnReturnClicked);

            // Footer Hint
            GameObject hint = NewUI("Hint", card.transform);
            hintLabel = hint.AddComponent<Text>();
            hintLabel.font = LabStyleUI.GetFont();
            hintLabel.text = "Nhấn R để chơi lại  ·  ESC để quay lại Menu chính";
            hintLabel.fontSize = 12;
            hintLabel.fontStyle = FontStyle.Normal;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.color = LabStyleUI.TextMuted;

            RectTransform hintRt = hint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(900f, 18f);
            hintRt.anchoredPosition = new Vector2(0f, -97f);
            ApplyBottomDockedCardLayout(cardRt);
        }

        private static Button CreateStyledButton(
            Transform parent,
            string keyName,
            string labelText,
            LabStyleUI.IconKind iconKind,
            Vector2 pos,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = NewUI(keyName + "Button", parent);
            Image btnImg = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(btnImg, LabStyleUI.GetSecondaryButtonSprite(), true, -4f);

            Button button = btnGo.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = btnImg;
            button.colors = LabStyleUI.MakeButtonColorBlock();
            button.onClick.AddListener(callback);

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(280f, 62f);
            btnRt.anchoredPosition = pos;

            GameObject iconGo = NewUI("Icon", btnGo.transform);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = LabStyleUI.GetIconSprite(iconKind);
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(28f, 0f);
            iRt.sizeDelta = new Vector2(20f, 20f);

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = LabStyleUI.GetFont();
            text.text = labelText;
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(44f, 0f);
            textRt.offsetMax = new Vector2(-14f, 0f);

            return button;
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

        private static void EnsureEventSystem()
        {
            LabStyleUI.EnsureEventSystem();
        }
    }
}
