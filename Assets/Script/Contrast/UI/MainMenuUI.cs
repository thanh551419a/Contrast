namespace Contrast.UI
{
    using Contrast.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Level Selection and Main Menu UI styled with the optical laboratory
    /// visual language of demo_quangbai1.html: dark translucent cards,
    /// subtle borders, optical concentric background rings, and clean typography.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
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
            if (panel != null)
                panel.SetActive(visible);
        }

        private void BuildUI()
        {
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
            // Dark gradient base #07111e -> #050812
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

            // Card 1: PLAY GAME
            CreateMenuCard(
                panel.transform,
                "PLAY GAME",
                "BẮT ĐẦU TRẢI NGHIỆM",
                new string[]
                {
                    "— Cơ chế dịch pha sóng liên tục (White / Gray / Black)",
                    "— Tương tác vật liệu & quỹ đạo thanh di chuyển",
                    "— Khảo sát & đo vân giao thoa Newton tại phòng quang học"
                },
                new Vector2(-280f, -20f),
                OnPlay);

            // Card 2: EDIT LEVEL
            CreateMenuCard(
                panel.transform,
                "EDIT LEVEL",
                "TRÌNH THIẾT KẾ MÀN CHƠI",
                new string[]
                {
                    "— Đặt, chọn, kéo thả và co giãn kích thước thanh",
                    "— Tùy chỉnh màu sắc, vị trí điểm lưu (Checkpoint) & Đích",
                    "— Thiết lập chuyển động quỹ đạo (Trajectory) & Xuất JSON"
                },
                new Vector2(280f, -20f),
                OnEdit);

            // Footer instructions
            GameObject footer = NewUI("Footer", panel.transform);
            Text footerText = footer.AddComponent<Text>();
            footerText.font = GetFont();
            footerText.text = "A / D hoặc ← / →  CHỌN      ENTER / SPACE  BẮT ĐẦU      R  CHƠI LẠI";
            footerText.fontSize = 14;
            footerText.fontStyle = FontStyle.Normal;
            footerText.alignment = TextAnchor.MiddleCenter;
            footerText.color = new Color(0.75f, 0.85f, 0.92f, 0.45f);
            footer.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 40f);
            footer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -380f);
        }

        private static void CreateMenuCard(
            Transform parent,
            string mainTitle,
            string subTitle,
            string[] bullets,
            Vector2 position,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject cardGo = NewUI("Card_" + mainTitle, parent);
            Image cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.051f, 0.078f, 0.114f, 0.92f); // #0d141d

            Outline outline = cardGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.28f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button button = cardGo.AddComponent<Button>();
            button.targetGraphic = cardImg;
            button.onClick.AddListener(callback);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.051f, 0.078f, 0.114f, 0.92f);
            colors.highlightedColor = new Color(0.110f, 0.153f, 0.200f, 0.98f);
            colors.pressedColor = new Color(0.180f, 0.240f, 0.320f, 1f);
            colors.fadeDuration = 0.15f;
            button.colors = colors;

            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(490f, 320f);
            rt.anchoredPosition = position;

            // Card Title
            GameObject titleGo = NewUI("Title", cardGo.transform);
            Text title = titleGo.AddComponent<Text>();
            title.font = GetFont();
            title.text = mainTitle;
            title.fontSize = 28;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(32f, -30f);
            titleRt.sizeDelta = new Vector2(-64f, 38f);

            // Card Subtitle
            GameObject subGo = NewUI("Sub", cardGo.transform);
            Text sub = subGo.AddComponent<Text>();
            sub.font = GetFont();
            sub.text = subTitle;
            sub.fontSize = 14;
            sub.fontStyle = FontStyle.Normal;
            sub.alignment = TextAnchor.MiddleLeft;
            sub.color = new Color(0.72f, 0.82f, 0.90f, 0.70f);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0f, 1f);
            subRt.anchoredPosition = new Vector2(32f, -70f);
            subRt.sizeDelta = new Vector2(-64f, 26f);

            // Bullets
            float startY = -120f;
            for (int i = 0; i < bullets.Length; i++)
            {
                GameObject bulletGo = NewUI("Bullet_" + i, cardGo.transform);
                Text bullet = bulletGo.AddComponent<Text>();
                bullet.font = GetFont();
                bullet.text = bullets[i];
                bullet.fontSize = 13;
                bullet.fontStyle = FontStyle.Normal;
                bullet.alignment = TextAnchor.MiddleLeft;
                bullet.color = new Color(0.80f, 0.88f, 0.94f, 0.55f);
                RectTransform bRt = bulletGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(0f, 1f);
                bRt.anchoredPosition = new Vector2(32f, startY - i * 36f);
                bRt.sizeDelta = new Vector2(-64f, 26f);
            }

            // Action prompt badge
            GameObject badgeGo = NewUI("Badge", cardGo.transform);
            Image badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(0.15f, 0.22f, 0.30f, 0.8f);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0f);
            badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0f);
            badgeRt.anchoredPosition = new Vector2(0f, 24f);
            badgeRt.sizeDelta = new Vector2(420f, 44f);

            GameObject badgeTextGo = NewUI("BadgeText", badgeGo.transform);
            Text badgeText = badgeTextGo.AddComponent<Text>();
            badgeText.font = GetFont();
            badgeText.text = "NHẤN ĐỂ MỞ";
            badgeText.fontSize = 13;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(0.90f, 0.95f, 1f, 0.90f);
            Stretch(badgeTextGo.GetComponent<RectTransform>());
        }

        private void OnPlay() => GameManager.Instance?.StartGame();
        private void OnEdit() => GameManager.Instance?.OpenEditor();

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
