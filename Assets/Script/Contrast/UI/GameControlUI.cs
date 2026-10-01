using Contrast.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Minimal in-game control panel with Restart and Retry.
    /// It is visible only while the game is Playing.
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
            GameObject canvasGo =
                new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new UnityEngine.Color(0f, 0f, 0f, 0.45f);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(24f, -24f);
            panelRt.sizeDelta = new Vector2(360f, 150f);

            CreateButton(
                panel.transform,
                "RESTART",
                new Vector2(90f, -42f),
                OnRestartClicked);

            CreateButton(
                panel.transform,
                "RETRY",
                new Vector2(270f, -42f),
                OnRetryClicked);
        }

        private static void CreateButton(
            Transform parent,
            string text,
            Vector2 position,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonGo = NewUI(text + "Button", parent);
            Image image = buttonGo.AddComponent<Image>();
            image.color = new UnityEngine.Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(callback);

            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150f, 70f);
            rt.anchoredPosition = position;

            GameObject labelGo = NewUI("Label", buttonGo.transform);
            Text label = labelGo.AddComponent<Text>();
            label.font = GetFont();
            label.text = text;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityEngine.Color.black;

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
