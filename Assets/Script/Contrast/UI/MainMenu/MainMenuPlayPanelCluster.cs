namespace Contrast.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    /// <summary>
    /// Cluster 2: Left Column Play &amp; Level Selection Sub-Menu Panel (Campaign CTA,
    /// L01/L02/L03 direct level cards, Audio Settings button, and Exit button).
    /// </summary>
    internal sealed class MainMenuPlayPanelCluster
    {
        private static readonly string[] LevelButtonNames =
        {
            "SelectLevel_L01",
            "SelectLevel_L02",
            "SelectLevel_L03"
        };

        private static readonly string[] LevelBaseTitles =
        {
            "Bài 01  ·  Cánh cửa không phải vật cản",
            "Bài 02  ·  Cầu thang ma",
            "Bài 03  ·  Giếng ba tầng"
        };

        private static readonly string[] LevelWavelengthBadges =
        {
            "550 nm · Lục",
            "650 nm · Đỏ",
            "470 nm · Lam"
        };

        private static readonly Color[] LevelCompletedColors =
        {
            new Color(0.28f, 0.92f, 0.52f, 1f),
            new Color(0.98f, 0.36f, 0.38f, 1f),
            new Color(0.38f, 0.68f, 1f, 1f)
        };

        private static readonly Color UncompletedOrderCircleGray = new Color(0.60f, 0.64f, 0.70f, 1f);
        private static readonly Color UncompletedOrderTextBlack = new Color(0.02f, 0.03f, 0.05f, 1f);
        private static readonly Color CompletedOrderTextDark = new Color(0.06f, 0.10f, 0.16f, 1f);

        public GameObject RootPanel { get; private set; }

        public bool IsOpen => RootPanel != null && RootPanel.activeSelf;

        public void SetActive(bool active)
        {
            if (RootPanel != null)
            {
                if (active)
                    RefreshLevelCompletionVisuals();
                RootPanel.SetActive(active);
            }
        }

        public void RefreshLevelCompletionVisuals()
        {
            if (RootPanel == null)
                return;

            for (int i = 0; i < LevelButtonNames.Length; i++)
            {
                Transform tr = RootPanel.transform.Find(LevelButtonNames[i]);
                if (tr == null)
                    continue;

                bool completed = Contrast.Core.Config.Instance.IsLevelCompleted(i);
                Color activeBadgeColor = completed ? LevelCompletedColors[i] : UncompletedOrderCircleGray;

                Transform orderCircle = tr.Find("OrderCircle");
                if (orderCircle != null)
                {
                    Image cImg = orderCircle.GetComponent<Image>();
                    if (cImg != null)
                    {
                        cImg.sprite = LabStyleUI.GetSoftCircleSprite();
                        cImg.color = activeBadgeColor;
                    }
                    Text orderTxt = orderCircle.Find("OrderText")?.GetComponent<Text>();
                    if (orderTxt != null)
                    {
                        orderTxt.color = completed ? CompletedOrderTextDark : UncompletedOrderTextBlack;
                    }
                }

                Transform titleTr = tr.Find("Title");
                if (titleTr != null)
                {
                    Text titleTxt = titleTr.GetComponent<Text>();
                    if (titleTxt != null)
                    {
                        titleTxt.text = LevelBaseTitles[i];
                    }
                }

                Transform waveSlot = tr.Find("WavelengthSlot");
                if (waveSlot != null)
                {
                    waveSlot.gameObject.SetActive(completed);
                    if (completed)
                    {
                        Transform dot = waveSlot.Find("SpectrumDot");
                        if (dot != null)
                        {
                            Image dImg = dot.GetComponent<Image>();
                            if (dImg != null)
                                dImg.color = LevelCompletedColors[i];
                        }
                        Text waveTxt = waveSlot.Find("WaveText")?.GetComponent<Text>();
                        if (waveTxt != null)
                            waveTxt.text = LevelWavelengthBadges[i];
                    }
                }
            }
        }

        public bool BindExisting(
            Transform parent,
            UnityAction onPlayFromStart,
            Action<int> onSelectDirectLevel,
            UnityAction onOpenSettings,
            UnityAction onQuitApplication)
        {
            Transform existing = parent.Find("PlaySubMenuPanel");
            if (existing == null)
                return false;

            RootPanel = existing.gameObject;
            Image cardBg = RootPanel.GetComponent<Image>();
            if (cardBg != null)
                LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), false);

            BindButton(existing, "PlayFromStartButton", LabStyleUI.GetPrimaryButtonSprite(), LabStyleUI.IconKind.PlayTriangle, onPlayFromStart);
            BindLevelButton(existing, "SelectLevel_L01", "Vật cản pha tĩnh  ·  2 Quang điểm  ·  k = 5,  i = 3", () => onSelectDirectLevel(0));
            BindLevelButton(existing, "SelectLevel_L02", "Cầu thang lệch pha  ·  2 Quang điểm  ·  k = 4,  i = 2", () => onSelectDirectLevel(1));
            BindLevelButton(existing, "SelectLevel_L03", "Giếng ba tầng & bệ động  ·  3 Quang điểm  ·  k = 5,  i = 2", () => onSelectDirectLevel(2));
            BindButton(existing, "SettingsButton", LabStyleUI.GetPillSprite(), LabStyleUI.IconKind.SettingsSliders, onOpenSettings);
            BindButton(existing, "ExitButton", LabStyleUI.GetDangerButtonSprite(), LabStyleUI.IconKind.PowerExit, onQuitApplication);

            RefreshLevelCompletionVisuals();
            return true;
        }

        private static void BindButton(Transform parent, string name, Sprite bgSprite, LabStyleUI.IconKind iconKind, UnityAction onClick)
        {
            Transform tr = parent.Find(name);
            if (tr == null) return;
            Image img = tr.GetComponent<Image>();
            if (img != null)
            {
                LabStyleUI.ApplySlicedSprite(img, bgSprite, false);
                img.raycastTarget = true;
            }
            Transform iconTr = tr.Find("Icon");
            if (iconTr != null)
            {
                Image icImg = iconTr.GetComponent<Image>();
                if (icImg != null)
                    icImg.sprite = LabStyleUI.GetIconSprite(iconKind);
            }
            Graphic[] childGraphics = tr.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                if (childGraphics[i] != null && childGraphics[i].transform != tr)
                    childGraphics[i].raycastTarget = false;
            }
            Button btn = tr.GetComponent<Button>();
            if (btn == null)
                btn = tr.gameObject.AddComponent<Button>();
            btn.enabled = true;
            btn.interactable = true;
            if (img != null)
                btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClick);
        }

        private static void BindLevelButton(Transform parent, string name, string updatedSubText, UnityAction onClick)
        {
            Transform tr = parent.Find(name);
            if (tr == null) return;
            Transform subTr = tr.Find("Sub");
            if (subTr != null && !string.IsNullOrEmpty(updatedSubText))
            {
                Text subTxt = subTr.GetComponent<Text>();
                if (subTxt != null)
                    subTxt.text = updatedSubText;
            }
            Image img = tr.GetComponent<Image>();
            if (img != null)
            {
                LabStyleUI.ApplySlicedSprite(img, LabStyleUI.GetSecondaryButtonSprite(), false);
                img.raycastTarget = true;
            }
            Transform orderCircle = tr.Find("OrderCircle");
            if (orderCircle != null)
            {
                Image cImg = orderCircle.GetComponent<Image>();
                if (cImg != null)
                    cImg.sprite = LabStyleUI.GetSoftCircleSprite();
            }
            Transform waveSlot = tr.Find("WavelengthSlot");
            if (waveSlot != null)
            {
                Image sImg = waveSlot.GetComponent<Image>();
                if (sImg != null)
                    LabStyleUI.ApplySlicedSprite(sImg, LabStyleUI.GetRecessedSlotSprite(), false);
                Transform dot = waveSlot.Find("SpectrumDot");
                if (dot != null)
                {
                    Image dImg = dot.GetComponent<Image>();
                    if (dImg != null)
                        dImg.sprite = LabStyleUI.GetSoftCircleSprite();
                }
            }
            Graphic[] childGraphics = tr.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                if (childGraphics[i] != null && childGraphics[i].transform != tr)
                    childGraphics[i].raycastTarget = false;
            }
            Button btn = tr.GetComponent<Button>();
            if (btn == null)
                btn = tr.gameObject.AddComponent<Button>();
            btn.enabled = true;
            btn.interactable = true;
            if (img != null)
                btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClick);
        }

        public void Build(
            Transform parent,
            UnityAction onPlayFromStart,
            Action<int> onSelectDirectLevel,
            UnityAction onOpenSettings,
            UnityAction onQuitApplication)
        {
            RootPanel = MainMenuUIBuilder.NewUI("PlaySubMenuPanel", parent);
            Image cardBg = RootPanel.AddComponent<Image>();
            LabStyleUI.ApplySlicedSprite(cardBg, LabStyleUI.GetCardSprite(), addDropShadow: true, shadowOffsetY: -7f);

            RectTransform subRt = RootPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(856f, 736f);
            subRt.anchoredPosition = new Vector2(-452f, -64f);

            MainMenuUIBuilder.CreateSubLabel(
                RootPanel.transform,
                "Header",
                "Phòng thí nghiệm giao thoa  ·  Chọn bài thực hành",
                new Vector2(0f, 322f),
                new Vector2(780f, 34f),
                19,
                FontStyle.Bold,
                LabStyleUI.TextPrimary,
                TextAnchor.MiddleLeft);

            MainMenuUIBuilder.CreatePrimaryStartButton(
                RootPanel.transform,
                "PlayFromStartButton",
                "BẮT ĐẦU THÍ NGHIỆM   (MÀN 1 → MÀN 3)",
                new Vector2(0f, 246f),
                new Vector2(780f, 76f),
                onPlayFromStart);

            MainMenuUIBuilder.CreateSubLabel(
                RootPanel.transform,
                "DirectSelectHeader",
                "Danh sách bài đo trực tiếp  (k · i · bước sóng λ)",
                new Vector2(0f, 174f),
                new Vector2(780f, 26f),
                14,
                FontStyle.Bold,
                LabStyleUI.TextSecondary,
                TextAnchor.MiddleLeft);

            MainMenuUIBuilder.CreateLabLevelButton(
                RootPanel.transform,
                "SelectLevel_L01",
                "1",
                "Bài 01  ·  Cánh cửa không phải vật cản",
                "Vật cản pha tĩnh  ·  2 Quang điểm  ·  k = 5,  i = 3",
                "550 nm · Lục",
                new Color(0.28f, 0.92f, 0.52f, 1f),
                new Vector2(0f, 104f),
                new Vector2(780f, 82f),
                () => onSelectDirectLevel(0));

            MainMenuUIBuilder.CreateLabLevelButton(
                RootPanel.transform,
                "SelectLevel_L02",
                "2",
                "Bài 02  ·  Cầu thang ma",
                "Cầu thang lệch pha  ·  2 Quang điểm  ·  k = 4,  i = 2",
                "650 nm · Đỏ",
                new Color(0.98f, 0.36f, 0.38f, 1f),
                new Vector2(0f, 10f),
                new Vector2(780f, 82f),
                () => onSelectDirectLevel(1));

            MainMenuUIBuilder.CreateLabLevelButton(
                RootPanel.transform,
                "SelectLevel_L03",
                "3",
                "Bài 03  ·  Giếng ba tầng",
                "Giếng ba tầng & bệ động  ·  3 Quang điểm  ·  k = 5,  i = 2",
                "470 nm · Lam",
                new Color(0.38f, 0.68f, 1f, 1f),
                new Vector2(0f, -84f),
                new Vector2(780f, 82f),
                () => onSelectDirectLevel(2));

            GameObject div = MainMenuUIBuilder.NewUI("BottomDivider", RootPanel.transform);
            Image divImg = div.AddComponent<Image>();
            divImg.color = new Color(0.68f, 0.80f, 0.92f, 0.22f);
            divImg.raycastTarget = false;
            RectTransform divRt = div.GetComponent<RectTransform>();
            divRt.sizeDelta = new Vector2(780f, 1.5f);
            divRt.anchoredPosition = new Vector2(0f, -156f);

            MainMenuUIBuilder.CreatePillUtilityButton(
                RootPanel.transform,
                "SettingsButton",
                "Cài đặt âm thanh",
                LabStyleUI.IconKind.SettingsSliders,
                LabStyleUI.GetPillSprite(),
                new Vector2(-202f, -218f),
                new Vector2(376f, 62f),
                onOpenSettings);

            MainMenuUIBuilder.CreatePillUtilityButton(
                RootPanel.transform,
                "ExitButton",
                "Thoát trò chơi",
                LabStyleUI.IconKind.PowerExit,
                LabStyleUI.GetDangerButtonSprite(),
                new Vector2(202f, -218f),
                new Vector2(376f, 62f),
                onQuitApplication);

            GameObject backGo = MainMenuUIBuilder.NewUI("BackToMainMenuButton", RootPanel.transform);
            backGo.SetActive(false);

            MainMenuUIBuilder.CreateSubLabel(
                RootPanel.transform,
                "GameplayFooter",
                "Phím tắt:  ENTER / SPACE để bắt đầu   ·   Phím 1 / 2 / 3 mở nhanh Bài 01 / 02 / 03",
                new Vector2(0f, -314f),
                new Vector2(780f, 28f),
                13,
                FontStyle.Bold,
                LabStyleUI.TextMuted,
                TextAnchor.MiddleCenter);

            RefreshLevelCompletionVisuals();
            RootPanel.SetActive(true);
        }
    }
}

