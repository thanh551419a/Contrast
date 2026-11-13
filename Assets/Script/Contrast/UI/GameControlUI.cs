namespace Contrast.UI
{
    using Contrast.Audio;
    using Contrast.Core;
    using Contrast.Level;
    using Contrast.Player;
    using Contrast.Presentation;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Virtual Physics Lab in-game HUD chrome matching the Vietnamese Virtual Lab reference UI:
    /// - Top-Left "< Quay lại" stadium capsule pill button
    /// - Top-Center docked slate-navy header bar with recessed progress groove
    /// - Right-Edge vertical column of circular icon buttons ("Toàn màn hình", "Điểm lưu",
    ///   "Chơi lại", "Hướng dẫn", "Khác")
    /// </summary>
    public sealed class GameControlUI : MonoBehaviour
    {
        private GameObject panel;
        private RectTransform grooveFillRt;
        private PlayerController cachedPlayer;

        public GameObject RootPanel => panel;

        private void Awake()
        {
            EnsureInitialized();
            Show(false);
        }

        public void EnsureInitialized()
        {
            if (panel != null)
                return;
            EnsureEventSystem();
            BuildUI();
        }

        public void Show(bool visible)
        {
            EnsureInitialized();
            if (panel != null)
                panel.SetActive(visible);
        }

        private void OnMenuClicked()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.OpenInGameMenu();
        }

        private void OnRestartClicked()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.Restart();
        }

        private void OnRetryClicked()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.Retry();
        }

        private void OnFullscreenClicked()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            ContrastAudioManager.Instance?.PlayButtonClick();
            Screen.fullScreen = !Screen.fullScreen;
        }

        private void OnGuideClicked()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            ContrastAudioManager.Instance?.PlayButtonClick();
            PhaseTunerUI.Instance?.ToggleUserGuide();
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy || grooveFillRt == null)
                return;

            if (cachedPlayer == null)
                cachedPlayer = FindAnyObjectByType<PlayerController>();

            float progress = ComputeExperimentProgress();
            grooveFillRt.anchorMax = new Vector2(Mathf.Clamp(progress, 0.04f, 1f), 1f);
        }

        private float ComputeExperimentProgress()
        {
            EasyNewtonFinaleController newton = EasyNewtonFinaleController.Instance;
            if (newton != null && newton.IsZoneActive)
            {
                if (newton.IsRevealActive || newton.IsComplete)
                    return 1.0f;
                return 0.72f + (newton.MarkerCount / 3f) * 0.28f;
            }

            if (cachedPlayer != null && newton != null)
            {
                float span = Mathf.Max(10f, newton.CenterO.x + 2f);
                float norm = Mathf.Clamp01((cachedPlayer.transform.position.x + 4f) / span);
                return Mathf.Lerp(0.06f, 0.72f, norm);
            }

            if (QuangDiemItem.TotalCount > 0)
            {
                return Mathf.Lerp(0.08f, 1.0f, (float)QuangDiemItem.CollectedCount / QuangDiemItem.TotalCount);
            }

            return 0.15f;
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

            // Full-screen non-blocking HUD root so top-left, top-center, and right-edge dock independently
            panel = NewUI("Panel", canvasGo.transform);
            Stretch(panel.GetComponent<RectTransform>());

            // 1. Top-Left "< Quay lại" Stadium Pill Button (matches reference image top-left)
            LabStyleUI.CreateBackPillButton(
                panel.transform,
                "BackPillButton",
                "Quay lại",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -20f),
                new Vector2(156f, 48f),
                OnMenuClicked);

            // 2. Top-Center Docked Slate-Navy Bar with Recessed Groove Track (matches reference image top-center)
            BuildTopLabDock(panel.transform);

            // 3. Right-Edge Vertical Column of Circular Icon Buttons (matches reference image right toolbar)
            const float rightX = -56f;

            // Top-right Fullscreen icon button ("Toàn màn hình")
            LabStyleUI.CreateCircularIconAction(
                panel.transform,
                "FullscreenButton",
                LabStyleUI.IconKind.Fullscreen,
                "Toàn màn hình",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(rightX, -20f),
                OnFullscreenClicked,
                54f);

            // Vertical lab action stack along right edge
            LabStyleUI.CreateCircularIconAction(
                panel.transform,
                "RETRYButton",
                LabStyleUI.IconKind.Retry,
                "Điểm lưu",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(rightX, -184f),
                OnRetryClicked,
                54f);

            LabStyleUI.CreateCircularIconAction(
                panel.transform,
                "RESTARTButton",
                LabStyleUI.IconKind.Restart,
                "Chơi lại",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(rightX, -284f),
                OnRestartClicked,
                54f);

            LabStyleUI.CreateCircularIconAction(
                panel.transform,
                "GuideButton",
                LabStyleUI.IconKind.GuideBook,
                "Hướng dẫn",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(rightX, -384f),
                OnGuideClicked,
                54f);

            LabStyleUI.CreateCircularIconAction(
                panel.transform,
                "MENUButton",
                LabStyleUI.IconKind.MenuBars,
                "Khác",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(rightX, -484f),
                OnMenuClicked,
                54f);
        }

        private void BuildTopLabDock(Transform parent)
        {
            GameObject dock = NewUI("TopLabDock", parent);
            Image dockBg = dock.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(dockBg, LabStyleUI.GetTopDockSprite(), true, -5f);
            dockBg.raycastTarget = false;

            RectTransform dockRt = dock.GetComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(0.5f, 1f);
            dockRt.anchorMax = new Vector2(0.5f, 1f);
            dockRt.pivot = new Vector2(0.5f, 1f);
            dockRt.anchoredPosition = new Vector2(0f, 6f);
            dockRt.sizeDelta = new Vector2(680f, 56f);

            // Recessed groove track inside top dock (exact visual match to reference screenshot)
            GameObject groove = NewUI("GrooveTrack", dock.transform);
            Image grooveBg = groove.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(grooveBg, LabStyleUI.GetRecessedSlotSprite(), false);
            grooveBg.color = new Color(0.66f, 0.75f, 0.86f, 0.90f);
            grooveBg.raycastTarget = false;

            RectTransform grooveRt = groove.GetComponent<RectTransform>();
            grooveRt.anchorMin = new Vector2(0.5f, 0.5f);
            grooveRt.anchorMax = new Vector2(0.5f, 0.5f);
            grooveRt.pivot = new Vector2(0.5f, 0.5f);
            grooveRt.anchoredPosition = new Vector2(0f, -3f);
            grooveRt.sizeDelta = new Vector2(560f, 14f);

            // Royal-Blue progress fill inside groove
            GameObject fill = NewUI("GrooveFill", groove.transform);
            Image fillImg = fill.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(fillImg, LabStyleUI.GetPrimaryButtonSprite(), false);
            fillImg.raycastTarget = false;

            grooveFillRt = fill.GetComponent<RectTransform>();
            grooveFillRt.anchorMin = new Vector2(0f, 0f);
            grooveFillRt.anchorMax = new Vector2(0.15f, 1f);
            grooveFillRt.offsetMin = new Vector2(2f, 2f);
            grooveFillRt.offsetMax = new Vector2(-2f, -2f);
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
