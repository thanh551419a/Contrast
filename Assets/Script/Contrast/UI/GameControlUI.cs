namespace Contrast.UI
{
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Minimal in-game control panel with Restart and Retry.
    /// Positioned in the top-right corner with translucent optical styling
    /// matching demo_quangbai1.html so it never overlaps control hints.
    /// </summary>
    public sealed class GameControlUI : MonoBehaviour
    {
        private GameObject panel;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            Show(false);
        }

        public void Show(bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }

        private void OnMenuClicked()
        {
            GameManager.Instance?.OpenInGameMenu();
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.Restart();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.Retry();
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.020f, 0.035f, 0.067f, 0.75f); // #050911 at 75%

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.804f, 0.863f, 0.902f, 0.20f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-24f, -24f);
            panelRt.sizeDelta = new Vector2(366f, 62f);

            CreateButton(
                panel.transform,
                "MENU",
                new Vector2(64f, 0f),
                OnMenuClicked,
                100f);

            CreateButton(
                panel.transform,
                "RETRY",
                new Vector2(183f, 0f),
                OnRetryClicked,
                100f);

            CreateButton(
                panel.transform,
                "RESTART",
                new Vector2(302f, 0f),
                OnRestartClicked,
                100f);
        }

        private static void CreateButton(
            Transform parent,
            string text,
            Vector2 position,
            UnityEngine.Events.UnityAction callback,
            float width = 120f)
        {
            GameObject buttonGo = NewUI(text + "Button", parent);
            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            Outline outline = buttonGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.88f, 0.98f, 0.32f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(callback);

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            cb.highlightedColor = new Color(0.14f, 0.20f, 0.28f, 0.98f);
            cb.pressedColor = new Color(0.20f, 0.28f, 0.38f, 1f);
            button.colors = cb;

            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 42f);
            rt.anchoredPosition = position;

            GameObject labelGo = NewUI("Label", buttonGo.transform);
            Text label = labelGo.AddComponent<Text>();
            label.font = GetFont();
            label.text = text;
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.92f, 0.96f, 1f, 1f);

            Stretch(labelGo.GetComponent<RectTransform>());
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
