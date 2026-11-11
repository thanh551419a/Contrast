namespace Contrast.UI
{
    using System.Collections.Generic;
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    using Color = UnityEngine.Color;

    /// <summary>
    /// Level Selection and Main Menu UI styled with the optical laboratory
    /// visual language of CONTRAST: dark translucent cards, subtle borders,
    /// optical concentric background rings, and clean typography.
    /// Features 3 distinct entry paths:
    /// 1. CHƠI THỬ: Opens play-mode sub-panel with CHƠI TỪ ĐẦU (L01 -> L02 -> L03)
    ///              and CHỌN MÀN (direct selection of L01, L02, or L03)
    /// 2. LOAD LEVEL: External custom JSON levels from disk
    /// 3. EDIT LEVEL: In-game level editor
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private GameObject panel;
        private GameObject cardsRoot;
        private GameObject playSubPanel;
        private readonly List<Outline> cardOutlines = new List<Outline>();
        private int selectedIndex = 0;

        public bool IsPlaySubMenuOpen => playSubPanel != null && playSubPanel.activeSelf;

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

            if (visible)
            {
                ClosePlaySubMenu();
                SelectCard(0);
            }
        }

        public void OpenPlaySubMenu()
        {
            EnsureInitialized();
            if (cardsRoot != null)
                cardsRoot.SetActive(false);
            if (playSubPanel != null)
                playSubPanel.SetActive(true);
        }

        public void ClosePlaySubMenu()
        {
            EnsureInitialized();
            if (playSubPanel != null)
                playSubPanel.SetActive(false);
            if (cardsRoot != null)
                cardsRoot.SetActive(true);
        }

        public void SelectPlayFromBeginning()
        {
            GameManager.Instance?.StartCampaign();
        }

        public void SelectDirectLevel(int levelIndex)
        {
            GameManager.Instance?.PlayDirectLevel(levelIndex);
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy)
                return;

            if (IsPlaySubMenuOpen)
            {
                if (IsEscapePressed())
                {
                    ClosePlaySubMenu();
                    return;
                }

                if (IsKey1Pressed())
                {
                    SelectDirectLevel(0);
                    return;
                }

                if (IsKey2Pressed())
                {
                    SelectDirectLevel(1);
                    return;
                }

                if (IsKey3Pressed())
                {
                    SelectDirectLevel(2);
                    return;
                }

                if (IsConfirmPressed())
                {
                    SelectPlayFromBeginning();
                    return;
                }

                return;
            }

            if (IsKey1Pressed())
            {
                SelectCard(0);
                OpenPlaySubMenu();
                return;
            }

            if (IsKey2Pressed())
            {
                SelectCard(1);
                OnLoadLevel();
                return;
            }

            if (IsKey3Pressed())
            {
                SelectCard(2);
                OnEdit();
                return;
            }

            if (IsLeftPressed())
            {
                SelectCard((selectedIndex + 2) % 3);
                return;
            }

            if (IsRightPressed())
            {
                SelectCard((selectedIndex + 1) % 3);
                return;
            }

            if (IsConfirmPressed())
            {
                if (selectedIndex == 0) OpenPlaySubMenu();
                else if (selectedIndex == 1) OnLoadLevel();
                else if (selectedIndex == 2) OnEdit();
            }
        }

        private void SelectCard(int index)
        {
            if (cardOutlines.Count == 0)
                return;

            selectedIndex = Mathf.Clamp(index, 0, cardOutlines.Count - 1);
            for (int i = 0; i < cardOutlines.Count; i++)
            {
                if (cardOutlines[i] == null) continue;
                if (i == selectedIndex)
                {
                    cardOutlines[i].effectColor = new Color(0.35f, 0.85f, 1f, 0.95f);
                    cardOutlines[i].effectDistance = new Vector2(2.5f, -2.5f);
                }
                else
                {
                    cardOutlines[i].effectColor = new Color(0.75f, 0.85f, 0.95f, 0.28f);
                    cardOutlines[i].effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        private void BuildUI()
        {
            cardOutlines.Clear();

            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.027f, 0.043f, 0.067f, 0.95f);
            Stretch(panel.GetComponent<RectTransform>());

            // Title
            GameObject title = NewUI("Title", panel.transform);
            Text titleText = title.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "CONTRAST";
            titleText.fontSize = 82;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.95f, 0.97f, 1f, 1f);
            titleText.rectTransform.sizeDelta = new Vector2(1000f, 110f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 320f);

            // Subtitle
            GameObject subTitle = NewUI("Subtitle", panel.transform);
            Text subText = subTitle.AddComponent<Text>();
            subText.font = GetFont();
            subText.text = "GIẢI ĐỐ GIAO THOA ÁNH SÁNG · OPTICAL INTERFERENCE PUZZLE";
            subText.fontSize = 18;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.72f, 0.82f, 0.90f, 0.65f);
            subTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 40f);
            subTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 250f);

            // Root container for the 3 top-level cards
            cardsRoot = NewUI("MainCardsRoot", panel.transform);
            Stretch(cardsRoot.GetComponent<RectTransform>());

            // Card 1: CHƠI THỬ (Built-in Campaign + Direct Level Selection)
            CreateMenuCard(
                cardsRoot.transform,
                "CHƠI THỬ",
                "CHIẾN DỊCH GỐC & CHỌN MÀN (L01 · L02 · L03)",
                new string[]
                {
                    "— Chơi từ đầu (L01 → L02 → L03) hoặc Chọn màn trực tiếp",
                    "— L01: Cánh cửa không phải vật cản",
                    "— L02: Cầu thang ma  |  L03: Giếng ba tầng"
                },
                new Vector2(-480f, -20f),
                "NHẤN HOẶC PHÍM 1",
                0,
                OpenPlaySubMenu);

            // Card 2: LOAD LEVEL (External Custom Levels)
            CreateMenuCard(
                cardsRoot.transform,
                "LOAD LEVEL",
                "MÀN CHƠI TÙY CHỌN TỪ BỘ NHỚ",
                new string[]
                {
                    "— Tải và trải nghiệm các file JSON ngoài",
                    "— Tự động kiểm tra & sửa lỗi định dạng",
                    "— Khám phá các màn do cộng đồng thiết kế"
                },
                new Vector2(0f, -20f),
                "NHẤN HOẶC PHÍM 2",
                1,
                OnLoadLevel);

            // Card 3: EDIT LEVEL (Level Editor)
            CreateMenuCard(
                cardsRoot.transform,
                "EDIT LEVEL",
                "TRÌNH THIẾT KẾ MÀN CHƠI",
                new string[]
                {
                    "— Đặt, chọn, kéo thả và co giãn thanh",
                    "— Tùy chỉnh màu sắc, Checkpoint & Đích",
                    "— Thiết lập chuyển động quỹ đạo & Xuất JSON"
                },
                new Vector2(480f, -20f),
                "NHẤN HOẶC PHÍM 3",
                2,
                OnEdit);

            // Footer instructions
            GameObject footer = NewUI("Footer", cardsRoot.transform);
            Text footerText = footer.AddComponent<Text>();
            footerText.font = GetFont();
            footerText.text = "1: CHƠI THỬ      2: LOAD LEVEL      3: EDIT LEVEL      ← / →  CHỌN      ENTER / SPACE  MỞ";
            footerText.fontSize = 14;
            footerText.fontStyle = FontStyle.Normal;
            footerText.alignment = TextAnchor.MiddleCenter;
            footerText.color = new Color(0.75f, 0.85f, 0.92f, 0.55f);
            footer.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 40f);
            footer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -380f);

            // Build the Play Sub-Menu (CHƠI TỪ ĐẦU + CHỌN MÀN L01/L02/L03 + QUAY LẠI)
            BuildPlaySubMenu(panel.transform);
            playSubPanel.SetActive(false);

            SelectCard(0);
        }

        private void BuildPlaySubMenu(Transform parent)
        {
            playSubPanel = NewUI("PlaySubMenuPanel", parent);
            Image cardBg = playSubPanel.AddComponent<Image>();
            cardBg.color = new Color(0.043f, 0.067f, 0.102f, 0.96f);

            Outline cardOutline = playSubPanel.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.35f, 0.85f, 1f, 0.55f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform subRt = playSubPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(1080f, 540f);
            subRt.anchoredPosition = new Vector2(0f, -45f);

            // Section Header
            CreateSubLabel(
                playSubPanel.transform,
                "Header",
                "CHƠI THỬ · CHỌN CHẾ ĐỘ CHƠI",
                new Vector2(0f, 226f),
                new Vector2(960f, 36f),
                22,
                FontStyle.Bold,
                new Color(0.94f, 0.97f, 1f, 1f),
                TextAnchor.MiddleCenter);

            // Option A: CHƠI TỪ ĐẦU
            CreateSubActionButton(
                playSubPanel.transform,
                "PlayFromStartButton",
                "▶   CHƠI TỪ ĐẦU   (CHIẾN DỊCH TUẦN TỰ: L01 → L02 → L03)",
                "Bắt đầu từ màn L01 và tự động chuyển tiếp qua L02, L03 sau khi hoàn thành đo vân Newton",
                new Vector2(0f, 142f),
                new Vector2(960f, 84f),
                new Color(0.10f, 0.24f, 0.36f, 0.96f),
                new Color(0.40f, 0.88f, 1f, 0.80f),
                SelectPlayFromBeginning);

            // Option B Header: CHỌN MÀN
            CreateSubLabel(
                playSubPanel.transform,
                "DirectSelectHeader",
                "CHỌN MÀN TRỰC TIẾP (KHÔNG KHÓA THỨ TỰ)",
                new Vector2(0f, 68f),
                new Vector2(960f, 28f),
                15,
                FontStyle.Bold,
                new Color(0.76f, 0.88f, 0.96f, 0.85f),
                TextAnchor.MiddleLeft);

            // Level 1 Button
            CreateSubActionButton(
                playSubPanel.transform,
                "SelectLevel_L01",
                "L01 — Cánh cửa không phải vật cản",
                "Vật cản pha tĩnh · 2 Quang Điểm · Thí nghiệm vân tròn Newton (k = 4, i = 2)",
                new Vector2(0f, 10f),
                new Vector2(960f, 64f),
                new Color(0.07f, 0.11f, 0.17f, 0.95f),
                new Color(0.75f, 0.85f, 0.95f, 0.38f),
                () => SelectDirectLevel(0));

            // Level 2 Button
            CreateSubActionButton(
                playSubPanel.transform,
                "SelectLevel_L02",
                "L02 — Cầu thang ma",
                "Cầu thang lệch pha · 2 Quang Điểm · Quỹ đạo Newton tự động (k = 4, i = 2)",
                new Vector2(0f, -64f),
                new Vector2(960f, 64f),
                new Color(0.07f, 0.11f, 0.17f, 0.95f),
                new Color(0.75f, 0.85f, 0.95f, 0.38f),
                () => SelectDirectLevel(1));

            // Level 3 Button
            CreateSubActionButton(
                playSubPanel.transform,
                "SelectLevel_L03",
                "L03 — Giếng ba tầng",
                "Giếng ba tầng & bệ di động · 3 Quang Điểm · Thí nghiệm vân tròn Newton (k = 5, i = 2)",
                new Vector2(0f, -138f),
                new Vector2(960f, 64f),
                new Color(0.07f, 0.11f, 0.17f, 0.95f),
                new Color(0.75f, 0.85f, 0.95f, 0.38f),
                () => SelectDirectLevel(2));

            // Back Button
            GameObject backGo = NewUI("BackToMainMenuButton", playSubPanel.transform);
            Image backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.14f, 0.18f, 0.24f, 0.92f);

            Outline backOutline = backGo.AddComponent<Outline>();
            backOutline.effectColor = new Color(0.80f, 0.88f, 0.95f, 0.40f);
            backOutline.effectDistance = new Vector2(1.2f, -1.2f);

            Button backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.onClick.AddListener(ClosePlaySubMenu);

            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.sizeDelta = new Vector2(280f, 46f);
            backRt.anchoredPosition = new Vector2(0f, -218f);

            CreateSubLabel(
                backGo.transform,
                "Label",
                "←   QUAY LẠI MENU CHÍNH (ESC)",
                Vector2.zero,
                new Vector2(280f, 46f),
                14,
                FontStyle.Bold,
                new Color(0.92f, 0.96f, 1f, 0.95f),
                TextAnchor.MiddleCenter);
        }

        private void CreateSubActionButton(
            Transform parent,
            string name,
            string mainText,
            string subText,
            Vector2 pos,
            Vector2 size,
            Color bgCol,
            Color borderCol,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = NewUI(name, parent);
            Image img = btnGo.AddComponent<Image>();
            img.color = bgCol;

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = borderCol;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            ColorBlock cb = btn.colors;
            cb.normalColor = bgCol;
            cb.highlightedColor = new Color(
                Mathf.Min(1f, bgCol.r + 0.08f),
                Mathf.Min(1f, bgCol.g + 0.10f),
                Mathf.Min(1f, bgCol.b + 0.12f),
                1f);
            cb.pressedColor = new Color(
                Mathf.Min(1f, bgCol.r + 0.14f),
                Mathf.Min(1f, bgCol.g + 0.16f),
                Mathf.Min(1f, bgCol.b + 0.18f),
                1f);
            btn.colors = cb;

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            CreateSubLabel(
                btnGo.transform,
                "Title",
                mainText,
                new Vector2(0f, 11f),
                new Vector2(size.x - 48f, 28f),
                18,
                FontStyle.Bold,
                new Color(0.96f, 0.98f, 1f, 1f),
                TextAnchor.MiddleLeft);

            CreateSubLabel(
                btnGo.transform,
                "Sub",
                subText,
                new Vector2(0f, -14f),
                new Vector2(size.x - 48f, 22f),
                13,
                FontStyle.Normal,
                new Color(0.75f, 0.85f, 0.93f, 0.78f),
                TextAnchor.MiddleLeft);
        }

        private void CreateSubLabel(
            Transform parent,
            string name,
            string content,
            Vector2 pos,
            Vector2 size,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor anchor)
        {
            GameObject go = NewUI(name, parent);
            Text txt = go.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = anchor;
            txt.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private void CreateMenuCard(
            Transform parent,
            string mainTitle,
            string subTitle,
            string[] bullets,
            Vector2 position,
            string badgeTextStr,
            int cardIndex,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject cardGo = NewUI("Card_" + mainTitle, parent);
            Image cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.051f, 0.078f, 0.114f, 0.92f); // #0d141d

            Outline outline = cardGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.28f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            cardOutlines.Add(outline);

            Button button = cardGo.AddComponent<Button>();
            button.targetGraphic = cardImg;
            button.onClick.AddListener(() =>
            {
                SelectCard(cardIndex);
                callback?.Invoke();
            });

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.051f, 0.078f, 0.114f, 0.92f);
            colors.highlightedColor = new Color(0.110f, 0.153f, 0.200f, 0.98f);
            colors.pressedColor = new Color(0.180f, 0.240f, 0.320f, 1f);
            colors.fadeDuration = 0.15f;
            button.colors = colors;

            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 330f);
            rt.anchoredPosition = position;

            // Card Title
            GameObject titleGo = NewUI("Title", cardGo.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = mainTitle;
            title.fontSize = 26;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(28f, -26f);
            titleRt.sizeDelta = new Vector2(-56f, 36f);

            // Card Subtitle
            GameObject subGo = NewUI("Sub", cardGo.transform);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = subTitle;
            sub.fontSize = 13;
            sub.fontStyle = FontStyle.Normal;
            sub.alignment = TextAnchor.MiddleLeft;
            sub.color = new Color(0.72f, 0.82f, 0.90f, 0.70f);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0f, 1f);
            subRt.anchoredPosition = new Vector2(28f, -64f);
            subRt.sizeDelta = new Vector2(-56f, 24f);

            // Bullets
            float startY = -106f;
            for (int i = 0; i < bullets.Length; i++)
            {
                GameObject bulletGo = NewUI("Bullet_" + i, cardGo.transform);
                Text bullet = bulletGo.AddComponent<Text>();
                bullet.font = GetFont();
                bullet.text = bullets[i];
                bullet.fontSize = 13;
                bullet.fontStyle = FontStyle.Normal;
                bullet.alignment = TextAnchor.MiddleLeft;
                bullet.color = new Color(0.80f, 0.88f, 0.94f, 0.60f);
                RectTransform bRt = bulletGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(0f, 1f);
                bRt.anchoredPosition = new Vector2(28f, startY - i * 34f);
                bRt.sizeDelta = new Vector2(-56f, 26f);
            }

            // Action prompt badge
            GameObject badgeGo = NewUI("Badge", cardGo.transform);
            Image badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(0.15f, 0.22f, 0.30f, 0.85f);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0f);
            badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0f);
            badgeRt.anchoredPosition = new Vector2(0f, 20f);
            badgeRt.sizeDelta = new Vector2(380f, 42f);

            GameObject badgeTextGo = NewUI("BadgeText", badgeGo.transform);
            Text badgeText = badgeTextGo.AddComponent<Text>();
            badgeText.font = GetFont();
            badgeText.text = badgeTextStr;
            badgeText.fontSize = 13;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(0.90f, 0.95f, 1f, 0.90f);
            Stretch(badgeTextGo.GetComponent<RectTransform>());
        }

        private void OnPlayCampaign() => GameManager.Instance?.StartCampaign();
        private void OnLoadLevel() => GameManager.Instance?.OpenLevelSelect();
        private void OnEdit() => GameManager.Instance?.OpenEditor();
        public void OnPlay() => OpenPlaySubMenu();

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

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsKey1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        private static bool IsKey2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        private static bool IsKey3Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
#endif
        }

        private static bool IsLeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
        }

        private static bool IsRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
        }

        private static bool IsConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }
    }
}
