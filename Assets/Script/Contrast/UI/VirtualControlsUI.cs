using Contrast.Color;
using Contrast.Core;
using Contrast.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Virtual touchscreen controls for Android mobile runtime.
    /// Provides ergonomic on-screen touch buttons for directional movement, jumping,
    /// and optical color phase switching (White / Gray / Black).
    /// Forwards pointer events into <see cref="MobileTouchInputAdapter"/>.
    /// </summary>
    public sealed class VirtualControlsUI : MonoBehaviour
    {
        public static VirtualControlsUI Instance { get; private set; }

        [SerializeField] private bool forceVisibleForTesting = false;

        private GameObject rootPanel;
        private MobileTouchInputAdapter touchAdapter;
        private CanvasGroup canvasGroup;

        public MobileTouchInputAdapter TouchAdapter => touchAdapter;

        public static void SetForceVisible(bool force)
        {
            if (Instance != null)
            {
                Instance.forceVisibleForTesting = force;
                Instance.UpdateVisibility(GameManager.Instance != null && GameManager.Instance.State == GameState.Playing);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            touchAdapter = new MobileTouchInputAdapter();
            BuildUI();
            Show(false);
        }

        public void Show(bool visible)
        {
            if (rootPanel != null)
            {
                bool shouldShow = visible && (Application.isMobilePlatform || forceVisibleForTesting || UnityEngine.Input.touchSupported);
                rootPanel.SetActive(shouldShow);
            }
        }

        public void UpdateVisibility(bool isPlaying)
        {
            Show(isPlaying);
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("VirtualControlsCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // In front of background and game, behind menus

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            rootPanel = NewUI("VirtualControlsPanel", canvasGo.transform);
            Stretch(rootPanel.GetComponent<RectTransform>());

            canvasGroup = rootPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.85f;

            // ── Movement Controls (Bottom Left) ───────────────────────────
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnLeft",
                "◄",
                new Vector2(0f, 0f),
                new Vector2(90f, 90f),
                new Vector2(110f, 110f),
                held => touchAdapter.SetMoveLeft(held));

            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnRight",
                "►",
                new Vector2(0f, 0f),
                new Vector2(220f, 90f),
                new Vector2(110f, 110f),
                held => touchAdapter.SetMoveRight(held));

            // Fast Fall Button (Bottom Left, above Left/Right)
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnFastFall",
                "▼ FALL",
                new Vector2(0f, 0f),
                new Vector2(155f, 210f),
                new Vector2(120f, 70f),
                held => touchAdapter.SetFastFall(held),
                fontSize: 16);

            // ── Phase Tuning Controls (Bottom Center) ─────────────────────
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnPhaseLeft",
                "◄ PHASE",
                new Vector2(0.5f, 0f),
                new Vector2(-90f, 50f),
                new Vector2(150f, 60f),
                held => touchAdapter.SetPhaseTuneLeft(held),
                fontSize: 16);

            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnPhaseRight",
                "PHASE ►",
                new Vector2(0.5f, 0f),
                new Vector2(90f, 50f),
                new Vector2(150f, 60f),
                held => touchAdapter.SetPhaseTuneRight(held),
                fontSize: 16);

            // ── Action Controls (Bottom Right) ────────────────────────────
            // Jump Button (large, prominent)
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnJump",
                "JUMP",
                new Vector2(1f, 0f),
                new Vector2(-110f, 100f),
                new Vector2(130f, 130f),
                new UnityEngine.Color(0.12f, 0.22f, 0.35f, 0.85f),
                new UnityEngine.Color(0.85f, 0.95f, 1f, 0.6f),
                () => touchAdapter.TriggerJump(),
                fontSize: 22);

            // Newton Mark X Button (prominent, gold/cyan accent)
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnNewtonMark",
                "X (MARK)",
                new Vector2(1f, 0f),
                new Vector2(-110f, 250f),
                new Vector2(130f, 80f),
                new UnityEngine.Color(0.24f, 0.18f, 0.08f, 0.90f),
                new UnityEngine.Color(1f, 0.85f, 0.35f, 0.85f),
                () => touchAdapter.TriggerNewtonMark(),
                textColor: new UnityEngine.Color(1f, 0.92f, 0.55f, 1f),
                fontSize: 18);

            // Direct Color Switching: White (1), Gray (2), Black (3)
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorWhite",
                "1 WHITE",
                new Vector2(1f, 0f),
                new Vector2(-420f, 85f),
                new Vector2(100f, 75f),
                new UnityEngine.Color(0.90f, 0.90f, 0.90f, 0.85f),
                new UnityEngine.Color(1f, 1f, 1f, 0.7f),
                () => touchAdapter.TriggerDirectColor(LogicalColor.White),
                textColor: UnityEngine.Color.black,
                fontSize: 14);

            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorGray",
                "2 GRAY",
                new Vector2(1f, 0f),
                new Vector2(-310f, 85f),
                new Vector2(100f, 75f),
                new UnityEngine.Color(0.45f, 0.45f, 0.48f, 0.85f),
                new UnityEngine.Color(0.7f, 0.7f, 0.8f, 0.6f),
                () => touchAdapter.TriggerDirectColor(LogicalColor.Gray),
                textColor: UnityEngine.Color.white,
                fontSize: 14);

            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorBlack",
                "3 BLACK",
                new Vector2(1f, 0f),
                new Vector2(-200f, 85f),
                new Vector2(100f, 75f),
                new UnityEngine.Color(0.08f, 0.08f, 0.10f, 0.85f),
                new UnityEngine.Color(0.5f, 0.5f, 0.6f, 0.6f),
                () => touchAdapter.TriggerDirectColor(LogicalColor.Black),
                textColor: UnityEngine.Color.white,
                fontSize: 14);
        }

        private static void CreateTouchHoldButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            System.Action<bool> onHoldChanged,
            int fontSize = 36)
        {
            GameObject btnGo = NewUI(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.color = new UnityEngine.Color(0.06f, 0.09f, 0.14f, 0.82f);

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new UnityEngine.Color(0.7f, 0.85f, 0.95f, 0.35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            VirtualTouchHoldHandler holdHandler = btnGo.AddComponent<VirtualTouchHoldHandler>();
            holdHandler.OnHoldStateChanged = onHoldChanged;

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text txt = textGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new UnityEngine.Color(0.9f, 0.95f, 1f, 0.95f);
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private static void CreateTouchTapButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            UnityEngine.Color bgColor,
            UnityEngine.Color outlineColor,
            System.Action onTap,
            UnityEngine.Color? textColor = null,
            int fontSize = 16)
        {
            GameObject btnGo = NewUI(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onTap?.Invoke());

            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.2f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text txt = textGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor ?? new UnityEngine.Color(0.95f, 0.97f, 1f, 1f);
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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

    /// <summary>
    /// Event trigger component tracking pointer down/up state for continuous hold controls.
    /// </summary>
    public sealed class VirtualTouchHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action<bool> OnHoldStateChanged;
        private bool isHeld;

        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;
            OnHoldStateChanged?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isHeld)
            {
                isHeld = false;
                OnHoldStateChanged?.Invoke(false);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHeld)
            {
                isHeld = false;
                OnHoldStateChanged?.Invoke(false);
            }
        }
    }
}
