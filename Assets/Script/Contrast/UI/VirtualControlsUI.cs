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

            // ── Movement Controls (Bottom Left — Circular Slate Lab Pads) ──
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnLeft",
                "◄",
                new Vector2(0f, 0f),
                new Vector2(90f, 90f),
                new Vector2(108f, 108f),
                held => touchAdapter.SetMoveLeft(held),
                isCircular: true);

            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnRight",
                "►",
                new Vector2(0f, 0f),
                new Vector2(216f, 90f),
                new Vector2(108f, 108f),
                held => touchAdapter.SetMoveRight(held),
                isCircular: true);

            // Fast Fall Button (Bottom Left, Stadium Pill above Left/Right)
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnFastFall",
                "▼ Hạ nhanh",
                new Vector2(0f, 0f),
                new Vector2(153f, 212f),
                new Vector2(138f, 54f),
                held => touchAdapter.SetFastFall(held),
                fontSize: 15,
                isCircular: false);

            // ── Phase Tuning Controls (Bottom Center — Stadium Pills) ─────
            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnPhaseLeft",
                "◄ Pha",
                new Vector2(0.5f, 0f),
                new Vector2(-92f, 52f),
                new Vector2(156f, 54f),
                held => touchAdapter.SetPhaseTuneLeft(held),
                fontSize: 15,
                isCircular: false);

            CreateTouchHoldButton(
                rootPanel.transform,
                "BtnPhaseRight",
                "Pha ►",
                new Vector2(0.5f, 0f),
                new Vector2(92f, 52f),
                new Vector2(156f, 54f),
                held => touchAdapter.SetPhaseTuneRight(held),
                fontSize: 15,
                isCircular: false);

            // ── Action Controls (Bottom Right) ────────────────────────────
            // Jump Button (large circular Slate-Navy Lab button)
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnJump",
                "NHẢY",
                new Vector2(1f, 0f),
                new Vector2(-110f, 96f),
                new Vector2(124f, 124f),
                LabStyleUI.GetCircleButtonSprite(),
                isSliced: false,
                tintColor: UnityEngine.Color.white,
                onTap: () => touchAdapter.TriggerJump(),
                textColor: LabStyleUI.TextPrimary,
                fontSize: 20);

            // Newton Mark X Button (Royal-Blue "NHẬP (X)" CTA button matching reference image)
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnNewtonMark",
                "NHẬP (X)",
                new Vector2(1f, 0f),
                new Vector2(-110f, 242f),
                new Vector2(144f, 60f),
                LabStyleUI.GetPrimaryButtonSprite(),
                isSliced: true,
                tintColor: UnityEngine.Color.white,
                onTap: () => touchAdapter.TriggerNewtonMark(),
                textColor: LabStyleUI.TextPrimary,
                fontSize: 16);

            // Direct Color Switching: White (1), Gray (2), Black (3) rounded lab pills
            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorWhite",
                "1 · Trắng",
                new Vector2(1f, 0f),
                new Vector2(-436f, 92f),
                new Vector2(104f, 56f),
                LabStyleUI.GetPillSprite(),
                isSliced: true,
                tintColor: new UnityEngine.Color(1f, 1f, 1f, 1f),
                onTap: () => touchAdapter.TriggerDirectColor(LogicalColor.White),
                textColor: LabStyleUI.TextPrimary,
                fontSize: 14);

            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorGray",
                "2 · Xám",
                new Vector2(1f, 0f),
                new Vector2(-320f, 92f),
                new Vector2(104f, 56f),
                LabStyleUI.GetPillSprite(),
                isSliced: true,
                tintColor: new UnityEngine.Color(0.88f, 0.92f, 0.98f, 1f),
                onTap: () => touchAdapter.TriggerDirectColor(LogicalColor.Gray),
                textColor: LabStyleUI.TextSecondary,
                fontSize: 14);

            CreateTouchTapButton(
                rootPanel.transform,
                "BtnColorBlack",
                "3 · Đen",
                new Vector2(1f, 0f),
                new Vector2(-204f, 92f),
                new Vector2(104f, 56f),
                LabStyleUI.GetPillSprite(),
                isSliced: true,
                tintColor: new UnityEngine.Color(0.72f, 0.78f, 0.88f, 1f),
                onTap: () => touchAdapter.TriggerDirectColor(LogicalColor.Black),
                textColor: LabStyleUI.TextPrimary,
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
            int fontSize = 32,
            bool isCircular = false)
        {
            GameObject btnGo = NewUI(name, parent);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            if (isCircular)
            {
                img.sprite = LabStyleUI.GetCircleButtonSprite();
                img.type = Image.Type.Simple;
                img.color = UnityEngine.Color.white;
                LabStyleUI.AddSoftDropShadow(btnGo, -4f, 0.42f);
            }
            else
            {
                LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetPillSprite(), addDropShadow: true, shadowOffsetY: -4f);
            }

            VirtualTouchHoldHandler holdHandler = btnGo.AddComponent<VirtualTouchHoldHandler>();
            holdHandler.OnHoldStateChanged = held =>
            {
                img.color = held
                    ? new UnityEngine.Color(0.82f, 0.90f, 1f, 1f)
                    : UnityEngine.Color.white;
                onHoldChanged?.Invoke(held);
            };

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text txt = textGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = LabStyleUI.TextPrimary;
            Stretch(textGo.GetComponent<RectTransform>());
        }

        private static void CreateTouchTapButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            bool isSliced,
            UnityEngine.Color tintColor,
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
            if (isSliced)
            {
                LabStyleUI.ApplySlicedSprite(img, sprite, addDropShadow: true, shadowOffsetY: -4f);
                img.color = tintColor;
            }
            else
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = tintColor;
                LabStyleUI.AddSoftDropShadow(btnGo, -4f, 0.42f);
            }

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(() => onTap?.Invoke());

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text txt = textGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor ?? LabStyleUI.TextPrimary;
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
