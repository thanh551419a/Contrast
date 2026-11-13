using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Contrast.UI
{
    using Color = UnityEngine.Color;
    /// <summary>
    /// Shared Virtual Physics Lab UI Design System for Contrast.
    /// Generates anti-aliased 9-sliced rounded cards, stadium pills, royal-blue
    /// primary CTA buttons, recessed measurement slots, circular toolbar buttons,
    /// and crisp procedural vector icons matching the Vietnamese Virtual Lab reference UI.
    /// </summary>
    public static class LabStyleUI
    {
        // ── Core Virtual-Lab Palette (extracted from Reference UI) ───────────
        public static readonly Color CardTop = new Color(0.165f, 0.220f, 0.306f, 0.95f);        // #2A384E
        public static readonly Color CardBottom = new Color(0.110f, 0.153f, 0.224f, 0.96f);     // #1C2739
        public static readonly Color CardBorder = new Color(0.600f, 0.698f, 0.816f, 0.65f);     // #99B2D0

        public static readonly Color PillTop = new Color(0.145f, 0.200f, 0.286f, 0.96f);        // #253349
        public static readonly Color PillBottom = new Color(0.098f, 0.141f, 0.208f, 0.97f);     // #192435
        public static readonly Color PillBorder = new Color(0.580f, 0.682f, 0.808f, 0.62f);     // #94CE...

        // Royal Sapphire Primary CTA ("NHẬP" button in reference UI)
        public static readonly Color PrimaryTop = new Color(0.086f, 0.306f, 0.682f, 0.98f);     // #164EAE
        public static readonly Color PrimaryBottom = new Color(0.031f, 0.157f, 0.416f, 0.99f);  // #08286A
        public static readonly Color PrimaryBorder = new Color(0.365f, 0.588f, 0.945f, 0.78f);  // #5D96F1

        // Slate Secondary Action Button
        public static readonly Color SecondaryTop = new Color(0.184f, 0.247f, 0.341f, 0.96f);   // #2F3F57
        public static readonly Color SecondaryBottom = new Color(0.122f, 0.169f, 0.243f, 0.97f);// #1F2B3E
        public static readonly Color SecondaryBorder = new Color(0.576f, 0.675f, 0.796f, 0.58f);// #93ACCB

        // Warm Danger / Exit Action Button (harmonized slate-crimson)
        public static readonly Color DangerTop = new Color(0.345f, 0.153f, 0.204f, 0.96f);
        public static readonly Color DangerBottom = new Color(0.216f, 0.090f, 0.129f, 0.98f);
        public static readonly Color DangerBorder = new Color(0.855f, 0.465f, 0.510f, 0.62f);

        // Recessed Input / Readout Slot ("giá trị   mm" in reference UI)
        public static readonly Color SlotTop = new Color(0.220f, 0.278f, 0.365f, 0.92f);        // #38475D
        public static readonly Color SlotBottom = new Color(0.267f, 0.329f, 0.420f, 0.92f);     // #44546B
        public static readonly Color SlotBorder = new Color(0.655f, 0.745f, 0.851f, 0.52f);

        // Right-Edge Circular Icon Button ("Toàn màn hình / Hướng dẫn / Khác")
        public static readonly Color CircleFillTop = new Color(0.176f, 0.239f, 0.333f, 0.94f);  // #2D3D55
        public static readonly Color CircleFillBottom = new Color(0.118f, 0.165f, 0.239f, 0.96f);// #1E2A3D
        public static readonly Color CircleRim = new Color(0.765f, 0.835f, 0.914f, 0.78f);      // #C3D5E9

        public static readonly Color TextPrimary = new Color(0.965f, 0.980f, 1.000f, 1.00f);
        public static readonly Color TextSecondary = new Color(0.755f, 0.824f, 0.906f, 0.90f);
        public static readonly Color TextMuted = new Color(0.620f, 0.702f, 0.800f, 0.78f);
        public static readonly Color DropShadowColor = new Color(0.0f, 0.02f, 0.06f, 0.46f);

        public enum IconKind
        {
            ChevronLeft,
            Fullscreen,
            Retry,
            Restart,
            GuideBook,
            MenuBars,
            SettingsSliders,
            PlayTriangle,
            Speaker,
            PowerExit
        }

        private static Sprite cardSprite;
        private static Sprite pillSprite;
        private static Sprite primaryBtnSprite;
        private static Sprite secondaryBtnSprite;
        private static Sprite dangerBtnSprite;
        private static Sprite slotSprite;
        private static Sprite topDockSprite;
        private static Sprite circleBtnSprite;
        private static Sprite softCircleSprite;
        private static Sprite pointerRightSprite;
        private static readonly Dictionary<IconKind, Sprite> iconCache = new Dictionary<IconKind, Sprite>();

        public static Sprite GetCardSprite()
        {
            if (cardSprite == null)
                cardSprite = CreateRoundedBoxSprite(96, 96, 20f, 2.0f, CardTop, CardBottom, CardBorder, 24);
            return cardSprite;
        }

        public static Sprite GetPillSprite()
        {
            if (pillSprite == null)
                pillSprite = CreateRoundedBoxSprite(96, 64, 30f, 1.8f, PillTop, PillBottom, PillBorder, 31);
            return pillSprite;
        }

        public static Sprite GetPrimaryButtonSprite()
        {
            if (primaryBtnSprite == null)
                primaryBtnSprite = CreateRoundedBoxSprite(96, 64, 13f, 1.8f, PrimaryTop, PrimaryBottom, PrimaryBorder, 18);
            return primaryBtnSprite;
        }

        public static Sprite GetSecondaryButtonSprite()
        {
            if (secondaryBtnSprite == null)
                secondaryBtnSprite = CreateRoundedBoxSprite(96, 64, 13f, 1.6f, SecondaryTop, SecondaryBottom, SecondaryBorder, 18);
            return secondaryBtnSprite;
        }

        public static Sprite GetDangerButtonSprite()
        {
            if (dangerBtnSprite == null)
                dangerBtnSprite = CreateRoundedBoxSprite(96, 64, 13f, 1.6f, DangerTop, DangerBottom, DangerBorder, 18);
            return dangerBtnSprite;
        }

        public static Sprite GetRecessedSlotSprite()
        {
            if (slotSprite == null)
                slotSprite = CreateRoundedBoxSprite(80, 48, 10f, 1.4f, SlotTop, SlotBottom, SlotBorder, 14);
            return slotSprite;
        }

        public static Sprite GetTopDockSprite()
        {
            if (topDockSprite == null)
                topDockSprite = CreateRoundedBoxSprite(96, 64, 18f, 1.6f,
                    new Color(0.145f, 0.196f, 0.278f, 0.95f),
                    new Color(0.102f, 0.141f, 0.208f, 0.97f),
                    new Color(0.520f, 0.625f, 0.760f, 0.48f), 22);
            return topDockSprite;
        }

        public static Sprite GetCircleButtonSprite()
        {
            if (circleBtnSprite == null)
                circleBtnSprite = CreateCircleBadgeSprite(80, 2.2f, CircleFillTop, CircleFillBottom, CircleRim);
            return circleBtnSprite;
        }

        public static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite == null)
                softCircleSprite = CreateCircleBadgeSprite(64, 0f, Color.white, Color.white, Color.white);
            return softCircleSprite;
        }

        public static Sprite GetPointerRightSprite()
        {
            if (pointerRightSprite != null)
                return pointerRightSprite;

            const int w = 32, h = 40;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < h; y++)
            {
                float ny = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f; // 0 at center, 1 at top/bottom
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w; // 0 at left base, 1 at right tip
                    float edge = (1f - nx) - ny;
                    float alpha = Mathf.Clamp01(edge * 14f);
                    bool isRim = edge > 0f && edge < 0.11f;
                    Color baseCol = Color.Lerp(PillBottom, PillTop, (float)y / h);
                    Color c = isRim ? Color.Lerp(baseCol, PillBorder, 0.75f) : baseCol;
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            pointerRightSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
            return pointerRightSprite;
        }

        public static void ApplySlicedSprite(Image img, Sprite sprite, bool addDropShadow = true, float shadowOffsetY = -5f)
        {
            if (img == null || sprite == null)
                return;

            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            if (addDropShadow)
            {
                AddSoftDropShadow(img.gameObject, shadowOffsetY);
            }
        }

        public static void AddSoftDropShadow(GameObject go, float offsetY = -4f, float alphaScale = 1f)
        {
            if (go == null)
                return;

            Shadow sh = go.GetComponent<Shadow>();
            if (sh == null)
                sh = go.AddComponent<Shadow>();
            Color c = DropShadowColor;
            c.a *= alphaScale;
            sh.effectColor = c;
            sh.effectDistance = new Vector2(0f, offsetY);
        }

        public static ColorBlock MakeButtonColorBlock()
        {
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.15f, 1.20f, 1f);
            cb.pressedColor = new Color(0.82f, 0.86f, 0.92f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.58f, 0.65f, 0.65f);
            cb.colorMultiplier = 1.08f;
            cb.fadeDuration = 0.10f;
            return cb;
        }

        /// <summary>
        /// Creates a reference-accurate circular toolbar icon button with caption below
        /// (matching "Toàn màn hình / Túi đồ / Hướng dẫn / Khác" on the right edge of the reference UI).
        /// </summary>
        public static Button CreateCircularIconAction(
            Transform parent,
            string objectName,
            IconKind icon,
            string captionVi,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPos,
            UnityEngine.Events.UnityAction onClick,
            float circleDiameter = 54f)
        {
            GameObject wrap = new GameObject(objectName, typeof(RectTransform));
            wrap.transform.SetParent(parent, false);

            RectTransform wrapRt = wrap.GetComponent<RectTransform>();
            wrapRt.anchorMin = anchor;
            wrapRt.anchorMax = anchor;
            wrapRt.pivot = pivot;
            wrapRt.anchoredPosition = anchoredPos;
            wrapRt.sizeDelta = new Vector2(96f, circleDiameter + 26f);

            // Circle Button
            GameObject circleGo = new GameObject("CircleBg", typeof(RectTransform));
            circleGo.transform.SetParent(wrap.transform, false);
            RectTransform cRt = circleGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 1f);
            cRt.anchorMax = new Vector2(0.5f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(circleDiameter, circleDiameter);

            Image circleImg = circleGo.AddComponent<Image>();
            circleImg.sprite = GetCircleButtonSprite();
            circleImg.type = Image.Type.Simple;
            circleImg.color = Color.white;

            Shadow cShadow = circleGo.AddComponent<Shadow>();
            cShadow.effectColor = DropShadowColor;
            cShadow.effectDistance = new Vector2(0f, -4f);

            Button btn = wrap.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = circleImg;
            btn.colors = MakeButtonColorBlock();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            // Crisp Vector Icon inside Circle
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(circleGo.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = GetIconSprite(icon);
            iconImg.color = TextPrimary;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0.5f);
            iRt.anchorMax = new Vector2(0.5f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = Vector2.zero;
            float iconSz = Mathf.Round(circleDiameter * 0.48f);
            iRt.sizeDelta = new Vector2(iconSz, iconSz);

            // Clean Vietnamese Caption Below Circle
            if (!string.IsNullOrEmpty(captionVi))
            {
                GameObject lblGo = new GameObject("Label", typeof(RectTransform));
                lblGo.transform.SetParent(wrap.transform, false);
                Text lbl = lblGo.AddComponent<Text>();
                lbl.font = GetFont();
                lbl.text = captionVi;
                lbl.fontSize = 12;
                lbl.fontStyle = FontStyle.Bold;
                lbl.alignment = TextAnchor.UpperCenter;
                lbl.color = new Color(0.95f, 0.97f, 1f, 0.95f);
                lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                lbl.raycastTarget = false;

                Outline txtShadow = lblGo.AddComponent<Outline>();
                txtShadow.effectColor = new Color(0.02f, 0.04f, 0.08f, 0.78f);
                txtShadow.effectDistance = new Vector2(1f, -1f);

                RectTransform lRt = lblGo.GetComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0f, 0f);
                lRt.anchorMax = new Vector2(1f, 0f);
                lRt.pivot = new Vector2(0.5f, 0f);
                lRt.anchoredPosition = new Vector2(0f, 0f);
                lRt.sizeDelta = new Vector2(24f, 20f);
            }

            return btn;
        }

        /// <summary>
        /// Creates a top-left rounded stadium "< Quay lại" navigation pill button
        /// matching the reference UI's "< Quay lại" pill.
        /// </summary>
        public static Button CreateBackPillButton(
            Transform parent,
            string objectName,
            string textVi,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = go.AddComponent<Image>();
            ApplySlicedSprite(bg, GetPillSprite(), true, -4f);

            Button btn = go.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = bg;
            btn.colors = MakeButtonColorBlock();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            // Chevron "<" icon
            GameObject iconGo = new GameObject("Chevron", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = GetIconSprite(IconKind.ChevronLeft);
            iconImg.color = TextPrimary;
            iconImg.raycastTarget = false;
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(26f, 0f);
            iRt.sizeDelta = new Vector2(18f, 18f);

            // Label "Quay lại"
            GameObject lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(go.transform, false);
            Text lbl = lblGo.AddComponent<Text>();
            lbl.font = GetFont();
            lbl.text = textVi;
            lbl.fontSize = 17;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = TextPrimary;
            lbl.raycastTarget = false;
            RectTransform lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(44f, 0f);
            lRt.offsetMax = new Vector2(-14f, 0f);

            return btn;
        }

        public static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PROCEDURAL SDF ROUNDED BOX, CIRCLE & VECTOR ICON GENERATORS
        // ═══════════════════════════════════════════════════════════════════════

        private static Sprite CreateRoundedBoxSprite(
            int width,
            int height,
            float radius,
            float borderThickness,
            Color fillTop,
            Color fillBottom,
            Color borderColor,
            int sliceBorder)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Vector2 halfSize = new Vector2(width * 0.5f - 1.5f, height * 0.5f - 1.5f);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float r = Mathf.Min(radius, Mathf.Min(halfSize.x, halfSize.y));

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float tY = (float)y / Mathf.Max(1, height - 1);
                Color gradFill = Color.Lerp(fillBottom, fillTop, tY);

                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(r, r));
                    float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;

                    float outerAlpha = Mathf.Clamp01(0.5f - dist);
                    if (outerAlpha <= 0.001f)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    if (borderThickness > 0.01f)
                    {
                        float innerBlend = Mathf.Clamp01(0.5f - (dist + borderThickness));
                        Color mixed = Color.Lerp(borderColor, gradFill, innerBlend);
                        mixed.a *= outerAlpha;
                        pixels[y * width + x] = mixed;
                    }
                    else
                    {
                        Color c = gradFill;
                        c.a *= outerAlpha;
                        pixels[y * width + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Vector4 border = new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite CreateCircleBadgeSprite(
            int diameter,
            float borderThickness,
            Color fillTop,
            Color fillBottom,
            Color rimColor)
        {
            Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float center = diameter * 0.5f;
            float outerRadius = diameter * 0.5f - 1.5f;
            Color[] pixels = new Color[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                float tY = (float)y / Mathf.Max(1, diameter - 1);
                Color fill = Color.Lerp(fillBottom, fillTop, tY);

                for (int x = 0; x < diameter; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center)) - outerRadius;
                    float outerAlpha = Mathf.Clamp01(0.5f - dist);
                    if (outerAlpha <= 0.001f)
                    {
                        pixels[y * diameter + x] = Color.clear;
                        continue;
                    }

                    if (borderThickness > 0.01f)
                    {
                        float innerBlend = Mathf.Clamp01(0.5f - (dist + borderThickness));
                        Color c = Color.Lerp(rimColor, fill, innerBlend);
                        c.a *= outerAlpha;
                        pixels[y * diameter + x] = c;
                    }
                    else
                    {
                        Color c = fill;
                        c.a *= outerAlpha;
                        pixels[y * diameter + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite GetIconSprite(IconKind kind)
        {
            if (iconCache.TryGetValue(kind, out Sprite cached) && cached != null)
                return cached;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float[,] alphaBuf = new float[size, size];

            switch (kind)
            {
                case IconKind.ChevronLeft:
                    DrawSegment(alphaBuf, new Vector2(39f, 49f), new Vector2(22f, 32f), 3.2f);
                    DrawSegment(alphaBuf, new Vector2(22f, 32f), new Vector2(39f, 15f), 3.2f);
                    break;

                case IconKind.Fullscreen:
                    // 4 diagonal expand arrows matching "Toàn màn hình" in reference image
                    DrawSegment(alphaBuf, new Vector2(25f, 39f), new Vector2(13f, 51f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(13f, 51f), new Vector2(24f, 51f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(13f, 51f), new Vector2(13f, 40f), 2.5f);

                    DrawSegment(alphaBuf, new Vector2(39f, 39f), new Vector2(51f, 51f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(51f, 51f), new Vector2(40f, 51f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(51f, 51f), new Vector2(51f, 40f), 2.5f);

                    DrawSegment(alphaBuf, new Vector2(25f, 25f), new Vector2(13f, 13f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(13f, 13f), new Vector2(24f, 13f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(13f, 13f), new Vector2(13f, 24f), 2.5f);

                    DrawSegment(alphaBuf, new Vector2(39f, 25f), new Vector2(51f, 13f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(51f, 13f), new Vector2(40f, 13f), 2.5f);
                    DrawSegment(alphaBuf, new Vector2(51f, 13f), new Vector2(51f, 24f), 2.5f);
                    break;

                case IconKind.Retry:
                    // Checkpoint flag / rewind pin
                    DrawArc(alphaBuf, new Vector2(33f, 31f), 15f, 35f, 315f, 2.8f);
                    DrawSegment(alphaBuf, new Vector2(44f, 41f), new Vector2(33f, 43f), 2.7f);
                    DrawSegment(alphaBuf, new Vector2(44f, 41f), new Vector2(45f, 30f), 2.7f);
                    DrawDisc(alphaBuf, new Vector2(33f, 31f), 3.6f);
                    break;

                case IconKind.Restart:
                    // Clean circular refresh loop
                    DrawArc(alphaBuf, new Vector2(32f, 32f), 16f, -35f, 235f, 2.8f);
                    DrawSegment(alphaBuf, new Vector2(19f, 41f), new Vector2(16f, 29f), 2.7f);
                    DrawSegment(alphaBuf, new Vector2(19f, 41f), new Vector2(30f, 41f), 2.7f);
                    break;

                case IconKind.GuideBook:
                    // Book icon matching "Hướng dẫn" in reference image
                    DrawSegment(alphaBuf, new Vector2(19f, 14f), new Vector2(45f, 14f), 2.6f);
                    DrawSegment(alphaBuf, new Vector2(19f, 50f), new Vector2(45f, 50f), 2.6f);
                    DrawSegment(alphaBuf, new Vector2(19f, 14f), new Vector2(19f, 50f), 2.8f);
                    DrawSegment(alphaBuf, new Vector2(45f, 14f), new Vector2(45f, 50f), 2.6f);
                    DrawSegment(alphaBuf, new Vector2(25f, 14f), new Vector2(25f, 50f), 1.8f);
                    DrawSegment(alphaBuf, new Vector2(31f, 38f), new Vector2(40f, 38f), 2.0f);
                    DrawSegment(alphaBuf, new Vector2(31f, 30f), new Vector2(40f, 30f), 2.0f);
                    break;

                case IconKind.MenuBars:
                    // 3 bullet-list bars matching "Khác" in reference image
                    float[] ys = { 44f, 32f, 20f };
                    for (int i = 0; i < ys.Length; i++)
                    {
                        DrawDisc(alphaBuf, new Vector2(16f, ys[i]), 3.0f);
                        DrawSegment(alphaBuf, new Vector2(25f, ys[i]), new Vector2(49f, ys[i]), 2.8f);
                    }
                    break;

                case IconKind.SettingsSliders:
                    DrawSegment(alphaBuf, new Vector2(14f, 43f), new Vector2(50f, 43f), 2.4f);
                    DrawDisc(alphaBuf, new Vector2(26f, 43f), 5.0f);
                    DrawSegment(alphaBuf, new Vector2(14f, 21f), new Vector2(50f, 21f), 2.4f);
                    DrawDisc(alphaBuf, new Vector2(39f, 21f), 5.0f);
                    break;

                case IconKind.PlayTriangle:
                    DrawTriangle(alphaBuf, new Vector2(22f, 16f), new Vector2(22f, 48f), new Vector2(48f, 32f));
                    break;

                case IconKind.Speaker:
                    DrawSegment(alphaBuf, new Vector2(16f, 27f), new Vector2(24f, 27f), 4.0f);
                    DrawSegment(alphaBuf, new Vector2(16f, 37f), new Vector2(24f, 37f), 4.0f);
                    DrawSegment(alphaBuf, new Vector2(16f, 27f), new Vector2(16f, 37f), 3.0f);
                    DrawTriangle(alphaBuf, new Vector2(21f, 26f), new Vector2(34f, 16f), new Vector2(34f, 48f));
                    DrawTriangle(alphaBuf, new Vector2(21f, 26f), new Vector2(21f, 38f), new Vector2(34f, 48f));
                    DrawArc(alphaBuf, new Vector2(34f, 32f), 9f, -45f, 45f, 2.2f);
                    DrawArc(alphaBuf, new Vector2(34f, 32f), 15f, -50f, 50f, 2.2f);
                    break;

                case IconKind.PowerExit:
                    DrawArc(alphaBuf, new Vector2(32f, 30f), 15f, -55f, 235f, 2.8f);
                    DrawSegment(alphaBuf, new Vector2(32f, 33f), new Vector2(32f, 50f), 3.0f);
                    break;
            }

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaBuf[x, y]));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            iconCache[kind] = sp;
            return sp;
        }

        private static void DrawSegment(float[,] buf, Vector2 a, Vector2 b, float radius)
        {
            int size = buf.GetLength(0);
            Vector2 ab = b - a;
            float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 2f));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 2f));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 2f));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                    Vector2 proj = a + ab * t;
                    float d = Vector2.Distance(p, proj) - radius;
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha > buf[x, y])
                        buf[x, y] = alpha;
                }
            }
        }

        private static void DrawDisc(float[,] buf, Vector2 center, float radius)
        {
            int size = buf.GetLength(0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) - radius;
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha > buf[x, y])
                        buf[x, y] = alpha;
                }
            }
        }

        private static void DrawArc(float[,] buf, Vector2 center, float radius, float degStart, float degEnd, float thickness)
        {
            int steps = 28;
            Vector2 prev = Vector2.zero;
            for (int i = 0; i <= steps; i++)
            {
                float ang = Mathf.Lerp(degStart, degEnd, (float)i / steps) * Mathf.Deg2Rad;
                Vector2 pt = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                if (i > 0)
                    DrawSegment(buf, prev, pt, thickness);
                prev = pt;
            }
        }

        private static void DrawTriangle(float[,] buf, Vector2 a, Vector2 b, Vector2 c)
        {
            int size = buf.GetLength(0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(p, a, b, c))
                        buf[x, y] = 1f;
                }
            }
            DrawSegment(buf, a, b, 1.0f);
            DrawSegment(buf, b, c, 1.0f);
            DrawSegment(buf, c, a, 1.0f);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
