using Contrast.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Contrast.UI
{
    /// <summary>
    /// Result overlay for GAME OVER / YOU WIN with a Restart button.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Button retryButton;

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

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject(
                "Canvas",
                typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0f, 0f, 0f, 0.7f);
            Stretch(panel.GetComponent<RectTransform>());

            GameObject title = NewUI("Title", panel.transform);
            titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "GAME OVER";
            titleText.fontSize = 96;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = UnityEngine.Color.white;

            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1000f, 160f);
            titleRt.anchoredPosition = new Vector2(0f, 100f);

            GameObject btnGo = NewUI("RestartButton", panel.transform);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new UnityEngine.Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImg;
            button.onClick.AddListener(OnRestartClicked);

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(360f, 100f);
            btnRt.anchoredPosition = new Vector2(-200f, -60f);

            retryButton = CreateRetryButton(panel.transform);

            GameObject label = NewUI("Label", btnGo.transform);
            Text labelText = label.AddComponent<Text>();
            labelText.font = GetFont();
            labelText.text = "RESTART";
            labelText.fontSize = 48;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = UnityEngine.Color.black;
            Stretch(label.GetComponent<RectTransform>());

            GameObject hint = NewUI("Hint", panel.transform);
            Text hintText = hint.AddComponent<Text>();
            hintText.font = GetFont();
            hintText.text = "hoặc nhấn R / Enter";
            hintText.fontSize = 28;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new UnityEngine.Color(1f, 1f, 1f, 0.7f);

            RectTransform hintRt = hint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(600f, 50f);
            hintRt.anchoredPosition = new Vector2(0f, -150f);
        }


        private Button CreateRetryButton(Transform parent)
        {
            GameObject btnGo = NewUI("RetryButton", parent);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new UnityEngine.Color(0.75f, 0.75f, 0.75f, 1f);

            Button button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImg;
            button.onClick.AddListener(OnRetryClicked);

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(360f, 100f);
            btnRt.anchoredPosition = new Vector2(200f, -60f);

            GameObject label = NewUI("Label", btnGo.transform);
            Text labelText = label.AddComponent<Text>();
            labelText.font = GetFont();
            labelText.text = "RETRY";
            labelText.fontSize = 48;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = UnityEngine.Color.black;
            Stretch(label.GetComponent<RectTransform>());

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
