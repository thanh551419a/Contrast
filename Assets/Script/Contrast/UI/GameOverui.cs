namespace Contrast.UI
{
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Result overlay for GAME OVER / YOU WIN styled with the dark optical
    /// translucent aesthetics of demo_quangbai1.html.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text subText;
        private Button retryButton;
        private Button restartButton;
        private Button returnButton;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            ShowResult(false, "GAME OVER", false);
        }

        // Kept for compatibility with older callers.
        public void Show(bool visible)
        {
            ShowResult(visible, "GAME OVER", false);
        }

        public void ShowResult(bool visible, string title, bool allowRetry)
        {
            if (titleText != null)
                titleText.text = title;

            if (subText != null)
            {
                subText.text = title == "YOU WIN"
                    ? "HOÀN THÀNH PHÉP ĐO QUANG HỌC · OPTICAL MEASUREMENT COMPLETE"
                    : "MẤT ĐỒNG BỘ PHA · THỬ LẠI ĐỂ TÁI LẬP GIAO THOA";
            }

            if (retryButton != null)
                retryButton.gameObject.SetActive(allowRetry);

            if (panel != null)
                panel.SetActive(visible);
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
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            // Dark vignette backdrop #03060c at 92%
            bg.color = new Color(0.012f, 0.024f, 0.047f, 0.92f);
            Stretch(panel.GetComponent<RectTransform>());

            // Title
            GameObject title = NewUI("Title", panel.transform);
            titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "GAME OVER";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.96f, 0.98f, 1f, 1f);

            Outline titleOutline = title.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.85f, 0.95f, 1f, 0.35f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1200f, 100f);
            titleRt.anchoredPosition = new Vector2(0f, 120f);

            // Subtitle
            GameObject sub = NewUI("Subtitle", panel.transform);
            subText = sub.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = "MẤT ĐỒNG BỘ PHA · THỬ LẠI ĐỂ TÁI LẬP GIAO THOA";
            subText.fontSize = 16;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.72f, 0.82f, 0.90f, 0.70f);

            RectTransform subRt = sub.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(1000f, 36f);
            subRt.anchoredPosition = new Vector2(0f, 55f);

            // Button container
            GameObject btnRow = NewUI("ButtonRow", panel.transform);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(800f, 80f);
            rowRt.anchoredPosition = new Vector2(0f, -60f);

            // Retry Button
            retryButton = CreateStyledButton(
                btnRow.transform,
                "RETRY",
                "ĐIỂM LƯU GẦN NHẤT",
                new Vector2(-240f, 0f),
                OnRetryClicked);

            // Restart Button
            restartButton = CreateStyledButton(
                btnRow.transform,
                "RESTART",
                "CHƠI LẠI TỪ ĐẦU",
                new Vector2(0f, 0f),
                OnRestartClicked);

            // Return Button
            returnButton = CreateStyledButton(
                btnRow.transform,
                "RETURN",
                "QUAY LẠI",
                new Vector2(240f, 0f),
                OnReturnClicked);

            // Hint
            GameObject hint = NewUI("Hint", panel.transform);
            Text hintText = hint.AddComponent<Text>();
            hintText.font = GetFont();
            hintText.text = "hoặc nhấn R để chơi lại · ESC để quay lại";
            hintText.fontSize = 13;
            hintText.fontStyle = FontStyle.Normal;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.75f, 0.85f, 0.92f, 0.50f);

            RectTransform hintRt = hint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(600f, 30f);
            hintRt.anchoredPosition = new Vector2(0f, -145f);
        }

        private static Button CreateStyledButton(
            Transform parent,
            string label,
            string sub,
            Vector2 pos,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = NewUI(label + "Button", parent);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.32f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            Button button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImg;
            button.onClick.AddListener(callback);

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            cb.highlightedColor = new Color(0.14f, 0.20f, 0.28f, 0.98f);
            cb.pressedColor = new Color(0.20f, 0.28f, 0.38f, 1f);
            button.colors = cb;

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(260f, 68f);
            btnRt.anchoredPosition = pos;

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.94f, 0.97f, 1f, 1f);

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.35f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            GameObject subGo = NewUI("SubLabel", btnGo.transform);
            Text subText = subGo.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = sub;
            subText.fontSize = 10;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.72f, 0.82f, 0.90f, 0.65f);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.45f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;

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
