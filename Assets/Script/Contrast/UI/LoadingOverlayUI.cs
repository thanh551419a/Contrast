using UnityEngine;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Full-screen loading overlay displayed during level loading, initialization, and restarts.
    /// Styled with the optical laboratory dark translucent aesthetics.
    /// </summary>
    public sealed class LoadingOverlayUI : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text subText;

        private void Awake()
        {
            BuildUI();
            Show(false);
        }

        public void Show(bool visible, string customTitle = null, string customSubtitle = null)
        {
            if (titleText != null && !string.IsNullOrEmpty(customTitle))
                titleText.text = customTitle;
            else if (titleText != null)
                titleText.text = "LOADING LEVEL...";

            if (subText != null && !string.IsNullOrEmpty(customSubtitle))
                subText.text = customSubtitle;
            else if (subText != null)
                subText.text = "ĐANG TẢI & THIẾT LẬP MÀN CHƠI · SYNCHRONIZING OPTICAL PHASE";

            if (panel != null)
                panel.SetActive(visible);
        }

        private void BuildUI()
        {
            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // Above all other menus

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0.015f, 0.025f, 0.045f, 0.94f);
            Stretch(panel.GetComponent<RectTransform>());

            // Title
            GameObject titleGo = NewUI("Title", panel.transform);
            titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "LOADING LEVEL...";
            titleText.fontSize = 52;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new UnityEngine.Color(0.94f, 0.97f, 1f, 1f);

            Outline titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor = new UnityEngine.Color(0.75f, 0.88f, 0.98f, 0.35f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1200f, 70f);
            titleRt.anchoredPosition = new Vector2(0f, 25f);

            // Subtitle
            GameObject subGo = NewUI("Subtitle", panel.transform);
            subText = subGo.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = "ĐANG TẢI & THIẾT LẬP MÀN CHƠI · SYNCHRONIZING OPTICAL PHASE";
            subText.fontSize = 15;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new UnityEngine.Color(0.72f, 0.82f, 0.90f, 0.70f);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(1000f, 30f);
            subRt.anchoredPosition = new Vector2(0f, -25f);
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
    }
}
