using System;

namespace Contrast.UI
{
    using Contrast.Audio;
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Virtual Physics Lab Pause Menu UI matching the Vietnamese Virtual Lab reference style:
    /// - Frosted rounded slate-navy card with silver-steel rim & soft drop shadow
    /// - Recessed audio volume slots (Music & SFX) with royal-blue fill tracks
    /// - Clean single-line Vietnamese buttons: Royal-Blue "TIẾP TỤC", Matte-Slate "CHƠI LẠI MÀN NÀY",
    ///   and Stadium Pill "< Quay lại Menu chính"
    /// </summary>
    public sealed class InGameMenuUI : MonoBehaviour
    {
        private GameObject panel;
        private Text contextBadgeText;
        private Text returnButtonTitleText;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Text musicValText;
        private Text sfxValText;
        private bool suppressSliderEvents;
        private GameplayEntryContext currentContext = GameplayEntryContext.MainMenuGameplay;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            BindAudioEvents();
            Show(false);
        }

        private void OnDestroy()
        {
            if (ContrastAudioManager.Instance != null)
                ContrastAudioManager.Instance.OnVolumeChanged -= HandleExternalVolumeChanged;
        }

        private void BindAudioEvents()
        {
            var audio = ContrastAudioManager.EnsureInstance();
            if (audio != null)
            {
                audio.OnVolumeChanged -= HandleExternalVolumeChanged;
                audio.OnVolumeChanged += HandleExternalVolumeChanged;
                SyncSlidersFromAudio(audio.MusicVolume, audio.SfxVolume);
            }
        }

        private void HandleExternalVolumeChanged(float musicVol, float sfxVol)
        {
            SyncSlidersFromAudio(musicVol, sfxVol);
        }

        private void SyncSlidersFromAudio(float musicVol, float sfxVol)
        {
            suppressSliderEvents = true;
            if (musicSlider != null)
                musicSlider.value = musicVol;
            if (sfxSlider != null)
                sfxSlider.value = sfxVol;
            if (musicValText != null)
                musicValText.text = $"{Mathf.RoundToInt(musicVol * 100f)}%";
            if (sfxValText != null)
                sfxValText.text = $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            suppressSliderEvents = false;
        }

        public void Show(bool visible, GameplayEntryContext context = GameplayEntryContext.MainMenuGameplay)
        {
            currentContext = context;
            UpdateContextVisuals();

            if (visible)
            {
                var audio = ContrastAudioManager.EnsureInstance();
                if (audio != null)
                    SyncSlidersFromAudio(audio.MusicVolume, audio.SfxVolume);
            }

            if (panel != null)
                panel.SetActive(visible);
        }

        private void UpdateContextVisuals()
        {
            if (contextBadgeText != null)
            {
                string levelInfo = "Thí nghiệm giao thoa ánh sáng";
                if (GameManager.Instance != null)
                {
                    int idx = GameManager.Instance.CurrentCampaignIndex;
                    if (idx >= 0 && idx < GameManager.CampaignLevelTitles.Length)
                        levelInfo = $"Đang thực hiện: {GameManager.CampaignLevelTitles[idx]}";
                }

                contextBadgeText.text = currentContext == GameplayEntryContext.EditorTestPlay
                    ? "Chế độ thử nghiệm màn chơi"
                    : levelInfo;
            }

            if (returnButtonTitleText != null)
            {
                returnButtonTitleText.text = currentContext == GameplayEntryContext.EditorTestPlay
                    ? "Quay lại trình thiết kế"
                    : "Quay lại Menu chính";
            }
        }

        private void OnResumeClicked()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            Show(false);
            GameManager.Instance?.Restart();
        }

        private void OnReturnClicked()
        {
            ContrastAudioManager.Instance?.PlayButtonClick();
            Show(false);
            GameManager.Instance?.ReturnFromGameplay();
        }

        private void BuildUI()
        {
            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(0.018f, 0.032f, 0.058f, 0.84f);
            Stretch(panel.GetComponent<RectTransform>());

            // Center Frosted Slate-Navy Modal Card
            GameObject modal = NewUI("Modal", panel.transform);
            Image modalBg = modal.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(modalBg, LabStyleUI.GetCardSprite(), true, -7f);

            RectTransform modalRt = modal.GetComponent<RectTransform>();
            modalRt.sizeDelta = new Vector2(640f, 628f);
            modalRt.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleGo = NewUI("Title", modal.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = LabStyleUI.GetFont();
            title.text = "TẠM DỪNG";
            title.fontSize = 38;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = LabStyleUI.TextPrimary;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(540f, 48f);
            titleRt.anchoredPosition = new Vector2(0f, 250f);

            // Context Stadium Pill Badge
            GameObject badgeGo = NewUI("ContextBadge", modal.transform);
            Image badgeBg = badgeGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(badgeBg, LabStyleUI.GetPillSprite(), false);

            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(536f, 40f);
            badgeRt.anchoredPosition = new Vector2(0f, 194f);

            GameObject badgeTextGo = NewUI("BadgeText", badgeGo.transform);
            contextBadgeText = badgeTextGo.AddComponent<Text>();
            contextBadgeText.font = LabStyleUI.GetFont();
            contextBadgeText.text = "Đang thực hiện: L01 — Cánh cửa không phải vật cản";
            contextBadgeText.fontSize = 14;
            contextBadgeText.fontStyle = FontStyle.Bold;
            contextBadgeText.alignment = TextAnchor.MiddleCenter;
            contextBadgeText.color = LabStyleUI.TextSecondary;
            Stretch(badgeTextGo.GetComponent<RectTransform>());

            // ── Recessed Audio Volume Card ──────────────────────────────
            GameObject audioCard = NewUI("AudioSettingsCard", modal.transform);
            Image audioBg = audioCard.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(audioBg, LabStyleUI.GetRecessedSlotSprite(), false);
            RectTransform audioRt = audioCard.GetComponent<RectTransform>();
            audioRt.sizeDelta = new Vector2(536f, 144f);
            audioRt.anchoredPosition = new Vector2(0f, 82f);

            musicSlider = CreateVolumeSliderRow(
                audioCard.transform,
                "MusicRow",
                "Nhạc nền",
                new Vector2(0f, 32f),
                ContrastAudioManager.DefaultMusicVolume,
                out musicValText,
                val =>
                {
                    if (suppressSliderEvents) return;
                    ContrastAudioManager.EnsureInstance().SetMusicVolume(val);
                    if (musicValText != null)
                        musicValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            sfxSlider = CreateVolumeSliderRow(
                audioCard.transform,
                "SfxRow",
                "Hiệu ứng âm thanh",
                new Vector2(0f, -32f),
                ContrastAudioManager.DefaultSfxVolume,
                out sfxValText,
                val =>
                {
                    if (suppressSliderEvents) return;
                    ContrastAudioManager.EnsureInstance().SetSfxVolume(val);
                    if (sfxValText != null)
                        sfxValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            // ── Clean Single-Line Virtual Lab Buttons ───────────────────
            // 1. Primary Royal-Blue CTA ("TIẾP TỤC")
            CreateLabModalButton(
                modal.transform,
                "ResumeButton",
                "TIẾP TỤC",
                LabStyleUI.GetPrimaryButtonSprite(),
                LabStyleUI.IconKind.PlayTriangle,
                new Vector2(0f, -48f),
                OnResumeClicked);

            // 2. Secondary Matte-Slate Button ("CHƠI LẠI MÀN NÀY")
            CreateLabModalButton(
                modal.transform,
                "RestartButton",
                "CHƠI LẠI MÀN NÀY",
                LabStyleUI.GetSecondaryButtonSprite(),
                LabStyleUI.IconKind.Restart,
                new Vector2(0f, -128f),
                OnRestartClicked);

            // 3. Stadium Navigation Pill ("< Quay lại Menu chính")
            returnButtonTitleText = CreateLabModalButton(
                modal.transform,
                "ReturnButton",
                "Quay lại Menu chính",
                LabStyleUI.GetPillSprite(),
                LabStyleUI.IconKind.ChevronLeft,
                new Vector2(0f, -208f),
                OnReturnClicked);

            // Footer hint
            GameObject hintGo = NewUI("Hint", modal.transform);
            Text hint = hintGo.AddComponent<Text>();
            hint.font = LabStyleUI.GetFont();
            hint.text = "Nhấn phím ESC để tiếp tục thí nghiệm";
            hint.fontSize = 12;
            hint.fontStyle = FontStyle.Normal;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = LabStyleUI.TextMuted;

            RectTransform hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(520f, 24f);
            hintRt.anchoredPosition = new Vector2(0f, -276f);
        }

        private static Slider CreateVolumeSliderRow(
            Transform parent,
            string name,
            string labelStr,
            Vector2 pos,
            float initialVal,
            out Text valueLabel,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject row = NewUI(name, parent);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(496f, 48f);
            rowRt.anchoredPosition = pos;

            GameObject lblGo = NewUI("Label", row.transform);
            Text lbl = lblGo.AddComponent<Text>();
            lbl.font = LabStyleUI.GetFont();
            lbl.text = labelStr;
            lbl.fontSize = 14;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = LabStyleUI.TextPrimary;
            RectTransform lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.sizeDelta = new Vector2(180f, 32f);
            lblRt.anchoredPosition = new Vector2(-156f, 0f);

            // Slider track
            GameObject sliderGo = NewUI("Slider", row.transform);
            RectTransform sRt = sliderGo.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(226f, 28f);
            sRt.anchoredPosition = new Vector2(68f, 0f);

            GameObject bgGo = NewUI("Background", sliderGo.transform);
            Image bgImg = bgGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(bgImg, LabStyleUI.GetPillSprite(), false);
            bgImg.color = new Color(0.68f, 0.76f, 0.86f, 0.95f);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 12f);

            GameObject fillArea = NewUI("Fill Area", sliderGo.transform);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.5f);
            faRt.anchorMax = new Vector2(1f, 0.5f);
            faRt.offsetMin = new Vector2(2f, -5f);
            faRt.offsetMax = new Vector2(-2f, 5f);

            GameObject fillGo = NewUI("Fill", fillArea.transform);
            Image fillImg = fillGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(fillImg, LabStyleUI.GetPrimaryButtonSprite(), false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            Stretch(fillRt);

            GameObject handleArea = NewUI("Handle Slide Area", sliderGo.transform);
            Stretch(handleArea.GetComponent<RectTransform>());

            GameObject handleGo = NewUI("Handle", handleArea.transform);
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = LabStyleUI.GetCircleButtonSprite();
            handleImg.color = Color.white;
            RectTransform hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(24f, 24f);

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = initialVal;
            slider.onValueChanged.AddListener(onChanged);

            GameObject valGo = NewUI("ValueText", row.transform);
            valueLabel = valGo.AddComponent<Text>();
            valueLabel.font = LabStyleUI.GetFont();
            valueLabel.text = $"{Mathf.RoundToInt(initialVal * 100f)}%";
            valueLabel.fontSize = 14;
            valueLabel.fontStyle = FontStyle.Bold;
            valueLabel.alignment = TextAnchor.MiddleRight;
            valueLabel.color = LabStyleUI.TextPrimary;
            RectTransform valRt = valGo.GetComponent<RectTransform>();
            valRt.sizeDelta = new Vector2(56f, 32f);
            valRt.anchoredPosition = new Vector2(220f, 0f);

            return slider;
        }

        private static Text CreateLabModalButton(
            Transform parent,
            string objectName,
            string titleText,
            Sprite bgSprite,
            LabStyleUI.IconKind iconKind,
            Vector2 position,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = NewUI(objectName, parent);
            Image btnImg = btnGo.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(btnImg, bgSprite, true, -4f);

            Button btn = btnGo.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = btnImg;
            btn.colors = LabStyleUI.MakeButtonColorBlock();
            btn.onClick.AddListener(callback);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(536f, 58f);
            rt.anchoredPosition = position;

            // Crisp vector icon on left
            GameObject iconGo = NewUI("Icon", btnGo.transform);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = LabStyleUI.GetIconSprite(iconKind);
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(36f, 0f);
            iRt.sizeDelta = new Vector2(22f, 22f);

            GameObject titleGo = NewUI("Title", btnGo.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = LabStyleUI.GetFont();
            title.text = titleText;
            title.fontSize = 18;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;
            Stretch(titleGo.GetComponent<RectTransform>());

            return title;
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
