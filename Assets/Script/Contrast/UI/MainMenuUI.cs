using Contrast.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    public sealed class MainMenuUI : MonoBehaviour
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

        private void BuildUI()
        {
            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0f, 0f, 0f, 0.88f);
            Stretch(panel.GetComponent<RectTransform>());

            GameObject title = NewUI("Title", panel.transform);
            Text titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "CONTRAST";
            titleText.fontSize = 100;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = UnityEngine.Color.white;
            titleText.rectTransform.sizeDelta = new Vector2(900f, 150f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 260f);

            CreateButton(panel.transform, "PLAY GAME", new Vector2(0f, 60f), OnPlay);
            CreateButton(panel.transform, "EDIT LEVEL", new Vector2(0f, -80f), OnEdit);
        }

        private static void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction callback)
        {
            GameObject go = NewUI(label + "Button", parent);
            Image image = go.AddComponent<Image>();
            image.color = new UnityEngine.Color(0.88f, 0.88f, 0.88f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(callback);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 110f);
            rt.anchoredPosition = position;

            GameObject textGo = NewUI("Label", go.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 46;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = UnityEngine.Color.black;
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private void OnPlay() => GameManager.Instance?.StartGame();
        private void OnEdit() => GameManager.Instance?.OpenEditor();

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
