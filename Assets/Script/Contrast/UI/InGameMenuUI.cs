using System;

namespace Contrast.UI
{
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;
    /// <summary>
    /// In-game pause menu with context-aware navigation:
    /// - RESUME: closes menu and continues playing.
    /// - RESTART: reloads current level from its tracked source.
    /// - RETURN: leaves gameplay and returns to MainMenuUI (for MainMenuGameplay)
    ///           or LevelEditor session (for EditorTestPlay).
    /// Pure code-generated UGUI matching the dark optical styling of Contrast.
    /// </summary>
    public sealed class InGameMenuUI : MonoBehaviour
    {
        private GameObject panel;
        private Text contextBadgeText;
        private Text returnButtonSubText;
        private GameplayEntryContext currentContext = GameplayEntryContext.MainMenuGameplay;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            Show(false);
        }

        public void Show(bool visible, GameplayEntryContext context = GameplayEntryContext.MainMenuGameplay)
        {
            currentContext = context;
            UpdateContextVisuals();

            if (panel != null)
                panel.SetActive(visible);
        }

        private void UpdateContextVisuals()
        {
            if (contextBadgeText != null)
            {
                contextBadgeText.text = currentContext == GameplayEntryContext.EditorTestPlay
                    ? "CHẾ ĐỘ: MÀN CHƠI THỬ NGHIỆM (TEST PLAY)"
                    : "CHẾ ĐỘ: MÀN CHƠI CHÍNH (MAIN GAMEPLAY)";
                contextBadgeText.color = currentContext == GameplayEntryContext.EditorTestPlay
                    ? new Color(0.35f, 0.85f, 0.55f, 0.90f)
                    : new Color(0.45f, 0.75f, 1f, 0.90f);
            }

            if (returnButtonSubText != null)
            {
                returnButtonSubText.text = currentContext == GameplayEntryContext.EditorTestPlay
                    ? "QUAY LẠI TRÌNH THIẾT KẾ · LEVEL EDITOR"
                    : "QUAY LẠI MENU CHÍNH · MAIN MENU";
            }
        }

        private void OnResumeClicked()
        {
            GameManager.Instance?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            Show(false);
            GameManager.Instance?.Restart();
        }

        private void OnReturnClicked()
        {
            Show(false);
            GameManager.Instance?.ReturnFromGameplay();
        }

        private void BuildUI()
        {
            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220; // Above standard gameplay UI (90) and loading overlay (200)

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image backdrop = panel.AddComponent<Image>();
            // Dark vignette overlay backdrop (#03060c at 88%)
            backdrop.color = new Color(0.012f, 0.024f, 0.047f, 0.88f);
            Stretch(panel.GetComponent<RectTransform>());

            // Center modal dialog box
            GameObject modal = NewUI("Modal", panel.transform);
            Image modalBg = modal.AddComponent<Image>();
            modalBg.color = new Color(0.040f, 0.065f, 0.100f, 0.97f); // #0a111a

            Outline modalOutline = modal.AddComponent<Outline>();
            modalOutline.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.28f);
            modalOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform modalRt = modal.GetComponent<RectTransform>();
            modalRt.sizeDelta = new Vector2(560f, 520f);
            modalRt.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleGo = NewUI("Title", modal.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = "PAUSE";
            title.fontSize = 42;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.97f, 1f, 1f);

            Outline titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.85f, 0.95f, 1f, 0.35f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(500f, 50f);
            titleRt.anchoredPosition = new Vector2(0f, 195f);

            // Subtitle
            GameObject subGo = NewUI("Subtitle", modal.transform);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = "TẠM DỪNG TRÒ CHƠI · CONTRAST SESSION PAUSED";
            sub.fontSize = 13;
            sub.fontStyle = FontStyle.Normal;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.72f, 0.82f, 0.90f, 0.65f);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(500f, 25f);
            subRt.anchoredPosition = new Vector2(0f, 155f);

            // Context Badge Pill
            GameObject badgeGo = NewUI("ContextBadge", modal.transform);
            Image badgeBg = badgeGo.AddComponent<Image>();
            badgeBg.color = new Color(0.08f, 0.13f, 0.19f, 0.85f);

            Outline badgeOutline = badgeGo.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.22f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);

            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(440f, 32f);
            badgeRt.anchoredPosition = new Vector2(0f, 115f);

            GameObject badgeTextGo = NewUI("BadgeText", badgeGo.transform);
            contextBadgeText = badgeTextGo.AddComponent<Text>();
            contextBadgeText.font = GetFont();
            contextBadgeText.text = "CHẾ ĐỘ: MÀN CHƠI CHÍNH (MAIN GAMEPLAY)";
            contextBadgeText.fontSize = 12;
            contextBadgeText.fontStyle = FontStyle.Bold;
            contextBadgeText.alignment = TextAnchor.MiddleCenter;
            contextBadgeText.color = new Color(0.45f, 0.75f, 1f, 0.90f);
            Stretch(badgeTextGo.GetComponent<RectTransform>());

            // ── Buttons ──────────────────────────────────────────────
            float btnStartY = 45f;
            float btnGap = 82f;

            // 1. RESUME Button
            CreateMenuButton(
                modal.transform,
                "RESUME",
                "TIẾP TỤC TRÒ CHƠI · CONTINUE",
                new Vector2(0f, btnStartY),
                new Color(0.09f, 0.15f, 0.22f, 0.95f),
                new Color(0.25f, 0.82f, 0.95f, 0.40f),
                OnResumeClicked);

            // 2. RESTART Button
            CreateMenuButton(
                modal.transform,
                "RESTART",
                "CHƠI LẠI MÀN HIỆN TẠI · RELOAD LEVEL",
                new Vector2(0f, btnStartY - btnGap),
                new Color(0.07f, 0.11f, 0.17f, 0.95f),
                new Color(0.75f, 0.88f, 0.98f, 0.25f),
                OnRestartClicked);

            // 3. RETURN Button (Context-Aware)
            returnButtonSubText = CreateMenuButton(
                modal.transform,
                "RETURN",
                "QUAY LẠI MENU CHÍNH · MAIN MENU",
                new Vector2(0f, btnStartY - btnGap * 2f),
                new Color(0.12f, 0.08f, 0.08f, 0.95f),
                new Color(0.95f, 0.45f, 0.45f, 0.35f),
                OnReturnClicked);

            // Footer hint
            GameObject hintGo = NewUI("Hint", modal.transform);
            Text hint = hintGo.AddComponent<Text>();
            hint.font = GetFont();
            hint.text = "Nhấn ESC hoặc nút RESUME để tiếp tục chơi";
            hint.fontSize = 12;
            hint.fontStyle = FontStyle.Normal;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.72f, 0.82f, 0.90f, 0.45f);

            RectTransform hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(500f, 25f);
            hintRt.anchoredPosition = new Vector2(0f, -215f);
        }

        private static Text CreateMenuButton(
            Transform parent,
            string mainTitle,
            string subTitle,
            Vector2 position,
            Color bgColor,
            Color outlineColor,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = NewUI("Btn_" + mainTitle, parent);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = bgColor;

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(callback);

            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = new Color(bgColor.r + 0.06f, bgColor.g + 0.06f, bgColor.b + 0.08f, 1f);
            cb.pressedColor = new Color(bgColor.r + 0.12f, bgColor.g + 0.12f, bgColor.b + 0.15f, 1f);
            cb.fadeDuration = 0.12f;
            btn.colors = cb;

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 66f);
            rt.anchoredPosition = position;

            // Main Title
            GameObject titleGo = NewUI("Title", btnGo.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = mainTitle;
            title.fontSize = 20;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.98f, 1f, 1f);

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(10f, 0f);
            titleRt.offsetMax = new Vector2(-10f, -4f);

            // Subtitle
            GameObject subGo = NewUI("Sub", btnGo.transform);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = subTitle;
            sub.fontSize = 11;
            sub.fontStyle = FontStyle.Normal;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.75f, 0.85f, 0.92f, 0.65f);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.5f);
            subRt.offsetMin = new Vector2(10f, 4f);
            subRt.offsetMax = new Vector2(-10f, 0f);

            return sub;
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
    }
}
