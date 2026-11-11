namespace Contrast.UI
{
    using UnityEngine;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Debug HUD overlay displaying HTML-equivalent Vietnamese control hints
    /// while the T-key Test Mode (free-move / no-clip) is active.
    /// Canonical HTML reference: CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html lines 2485–2502.
    /// </summary>
    public sealed class TestModeUI : MonoBehaviour
    {
        private GameObject panel;

        private void Awake()
        {
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
            GameObject canvasGo = new GameObject("TestModeCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Above normal gameplay HUDs

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = new GameObject("TestModePanel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.04f, 0.08f, 0.82f); // Deep translucent navy

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.7f, 1.0f, 0.65f); // Cyan accent border
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(24f, -24f);
            panelRt.sizeDelta = new Vector2(380f, 180f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            // 1. Header: CHẾ ĐỘ THỬ — DI CHUYỂN TỰ DO
            CreateTextLine(panel.transform, font, "CHẾ ĐỘ THỬ — DI CHUYỂN TỰ DO", 16, FontStyle.Bold, new Color(0.3f, 0.85f, 1.0f));

            // 2. CHUỘT — DI CHUYỂN NHÂN VẬT
            CreateTextLine(panel.transform, font, "CHUỘT — DI CHUYỂN NHÂN VẬT", 14, FontStyle.Normal, Color.white);

            // 3. VẬT LÝ — TẮT
            CreateTextLine(panel.transform, font, "VẬT LÝ — TẮT", 14, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));

            // 4. VA CHẠM — TẮT
            CreateTextLine(panel.transform, font, "VA CHẠM — TẮT", 14, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));

            // 5. T — THOÁT CHẾ ĐỘ THỬ
            CreateTextLine(panel.transform, font, "T — THOÁT CHẾ ĐỘ THỬ", 14, FontStyle.Bold, new Color(1.0f, 0.85f, 0.3f));

            // 6. ESC — GIẢI PHÓNG CHUỘT
            CreateTextLine(panel.transform, font, "ESC — GIẢI PHÓNG CHUỘT", 13, FontStyle.Italic, new Color(0.7f, 0.75f, 0.8f));
        }

        private static void CreateTextLine(Transform parent, Font font, string content, int fontSize, FontStyle style, Color color)
        {
            GameObject lineGo = new GameObject("Line", typeof(RectTransform));
            lineGo.transform.SetParent(parent, false);

            LayoutElement le = lineGo.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 6f;
            le.preferredHeight = fontSize + 6f;

            Text txt = lineGo.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
