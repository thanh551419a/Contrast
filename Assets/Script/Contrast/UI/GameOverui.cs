using Contrast.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Màn hình "GAME OVER" + nút Restart. Tự tạo Canvas/Panel/Button lúc chạy,
    /// nên chỉ cần có script này (GameManager tự tạo nếu chưa có trong scene).
    /// </summary>
    public class GameOverUI : MonoBehaviour
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
            if (panel != null) panel.SetActive(visible);
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.Restart();
        }

        // ---------------- dựng UI ----------------

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            // Nền mờ phủ toàn màn hình
            panel = NewUI("Panel", canvasGo.transform);
            var bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0f, 0f, 0f, 0.7f);
            Stretch(panel.GetComponent<RectTransform>());

            // Chữ GAME OVER
            var title = NewUI("Title", panel.transform);
            var titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "GAME OVER";
            titleText.fontSize = 96;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = UnityEngine.Color.white;
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1000, 160);
            titleRt.anchoredPosition = new Vector2(0, 100);

            // Nút Restart
            var btnGo = NewUI("RestartButton", panel.transform);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new UnityEngine.Color(0.9f, 0.9f, 0.9f, 1f);
            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImg;
            button.onClick.AddListener(OnRestartClicked);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(360, 100);
            btnRt.anchoredPosition = new Vector2(0, -60);

            var label = NewUI("Label", btnGo.transform);
            var labelText = label.AddComponent<Text>();
            labelText.font = GetFont();
            labelText.text = "RESTART";
            labelText.fontSize = 48;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = UnityEngine.Color.black;
            Stretch(label.GetComponent<RectTransform>());

            // Gợi ý phím tắt
            var hint = NewUI("Hint", panel.transform);
            var hintText = hint.AddComponent<Text>();
            hintText.font = GetFont();
            hintText.text = "hoặc nhấn R / Enter";
            hintText.fontSize = 28;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new UnityEngine.Color(1f, 1f, 1f, 0.7f);
            var hintRt = hint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(600, 50);
            hintRt.anchoredPosition = new Vector2(0, -150);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        // Nút UI cần EventSystem mới bấm được
        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}