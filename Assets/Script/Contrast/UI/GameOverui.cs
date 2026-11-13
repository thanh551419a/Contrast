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
            EnsureEventSystem();
            BuildUI();
            ShowResult(false, "GAME OVER", false);
        }

        public void Show(bool visible)
        {
            ShowResult(visible, "GAME OVER", false);
        }

        public void ShowResult(bool visible, string title, bool allowRetry)
        {
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
            float buttonHeight = 62f;
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
            btn.onClick.AddListener(() =>
            {
                ContrastAudioManager.Instance?.PlayButtonClick();
                onClick?.Invoke();
            });
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.Restart();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.Retry();
        }

        private void OnReturnClicked()
        {
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
            bg.color = new Color(0.016f, 0.030f, 0.056f, 0.86f);
            Stretch(panel.GetComponent<RectTransform>());

            // Frosted Slate-Navy Modal Card
            GameObject card = NewUI("ResultCard", panel.transform);
            Image cardBg = card.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), true, -7f);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(1120f, 528f);
            cardRt.anchoredPosition = Vector2.zero;

            // Status Stadium Pill
            GameObject badgeGo = NewUI("StatusBadge", card.transform);
            badgeBg = badgeGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(badgeBg, LabStyleUI.GetPillSprite(), false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(580f, 40f);
            badgeRt.anchoredPosition = new Vector2(0f, 194f);

            GameObject badgeTxtGo = NewUI("BadgeText", badgeGo.transform);
            badgeText = badgeTxtGo.AddComponent<Text>();
            badgeText.font = LabStyleUI.GetFont();
            badgeText.text = "Hoàn thành thí nghiệm giao thoa";
            badgeText.fontSize = 14;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = LabStyleUI.TextSecondary;
            Stretch(badgeTxtGo.GetComponent<RectTransform>());

            // Main Title
            GameObject title = NewUI("Title", card.transform);
            titleText = title.AddComponent<Text>();
            titleText.font = LabStyleUI.GetFont();
            titleText.text = "HOÀN THÀNH THÍ NGHIỆM";
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = LabStyleUI.TextPrimary;

            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1000f, 68f);
            titleRt.anchoredPosition = new Vector2(0f, 122f);

            // Subtitle
            GameObject sub = NewUI("Subtitle", card.transform);
            subText = sub.AddComponent<Text>();
            subText.font = LabStyleUI.GetFont();
            subText.text = "Đã xác lập trục giao thoa K–O và hoàn tất phép đo bước sóng ánh sáng";
            subText.fontSize = 17;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = LabStyleUI.TextSecondary;

            RectTransform subRt = sub.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(960f, 34f);
            subRt.anchoredPosition = new Vector2(0f, 64f);

            // Recessed Measurement Summary Slot (matching [giá trị  mm] slot aesthetic)
            statsBox = NewUI("StatsBox", card.transform);
            Image statsBg = statsBox.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(statsBg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform statsRt = statsBox.GetComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(920f, 54f);
            statsRt.anchoredPosition = new Vector2(0f, 2f);

            GameObject statsTxtGo = NewUI("StatsText", statsBox.transform);
            statsText = statsTxtGo.AddComponent<Text>();
            statsText.font = LabStyleUI.GetFont();
            statsText.text = string.Empty;
            statsText.fontSize = 16;
            statsText.fontStyle = FontStyle.Bold;
            statsText.alignment = TextAnchor.MiddleCenter;
            statsText.color = LabStyleUI.TextPrimary;
            Stretch(statsTxtGo.GetComponent<RectTransform>());

            // Button Row Container
            GameObject btnRow = NewUI("ButtonRow", card.transform);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(1040f, 84f);
            rowRt.anchoredPosition = new Vector2(0f, -108f);

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
            hintLabel.fontSize = 13;
            hintLabel.fontStyle = FontStyle.Normal;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.color = LabStyleUI.TextMuted;

            RectTransform hintRt = hint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(900f, 26f);
            hintRt.anchoredPosition = new Vector2(0f, -204f);
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
