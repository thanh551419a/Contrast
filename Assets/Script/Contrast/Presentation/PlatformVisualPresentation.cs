namespace Contrast.Presentation
{
    using UnityEngine;
    using Contrast.Color;
    using Contrast.Level;
    using Contrast.Player;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only component attached to ColorPlatform that provides
    /// rich surface gradients, optical patterns (white stipple, gray diagonal grating,
    /// black micro-dashes, universal amber), crisp borders, proximity resonance pulses,
    /// and moving-platform trajectory visualization matching demo_quangbai1.html.
    ///
    /// Strict Boundary Rule:
    /// Does NOT modify ColorPlatform.Position, Size, or GetAabb().
    /// All presentation geometry resides strictly in child visual objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlatformVisualPresentation : MonoBehaviour
    {
        private ColorPlatform platform;
        private SpriteRenderer rawSpriteRenderer;

        // Visual child hierarchy
        private Transform visualRoot;
        private SpriteRenderer surfaceRenderer;
        private SpriteRenderer borderRenderer;
        private SpriteRenderer resonanceRenderer;

        // Colors matching demo_quangbai1.html
        // White: #e8f0f4 -> #cfdde5
        private static readonly Color WhiteTop = new Color(0.910f, 0.941f, 0.957f, 1f);
        private static readonly Color WhiteBottom = new Color(0.812f, 0.867f, 0.898f, 1f);
        // High-contrast slate perimeter for White platform so it is clearly framed and identifiable in all viewports
        private static readonly Color WhiteBorder = new Color(0.18f, 0.25f, 0.35f, 0.95f);

        // Gray: #7d858f -> #646d78
        private static readonly Color GrayTop = new Color(0.490f, 0.522f, 0.561f, 1f);
        private static readonly Color GrayBottom = new Color(0.392f, 0.427f, 0.471f, 1f);
        private static readonly Color GrayBorder = new Color(0.769f, 0.800f, 0.835f, 0.50f);

        // Black: #151d28 -> #0d141f
        private static readonly Color BlackTop = new Color(0.082f, 0.114f, 0.157f, 1f);
        private static readonly Color BlackBottom = new Color(0.051f, 0.078f, 0.122f, 1f);
        private static readonly Color BlackBorder = new Color(0.322f, 0.380f, 0.451f, 0.35f);

        // Universal: #f5c738 -> #d49b1a
        private static readonly Color UniversalTop = new Color(0.961f, 0.780f, 0.220f, 1f);
        private static readonly Color UniversalBottom = new Color(0.831f, 0.608f, 0.102f, 1f);
        private static readonly Color UniversalBorder = new Color(1f, 0.88f, 0.45f, 0.85f);

        // Cached procedural sprites
        private static Sprite cachedWhiteSprite;
        private static Sprite cachedGraySprite;
        private static Sprite cachedBlackSprite;
        private static Sprite cachedUniversalSprite;
        private static Sprite cachedBorderSprite;

        private PlayerController cachedPlayer;
        private float pulsePhase = 0f;

        private void Awake()
        {
            platform = GetComponent<ColorPlatform>();
            EnsureVisuals();
        }

        private Vector2 lastRenderedSize = Vector2.zero;
        private float lastRenderedColor = -1f;

        public void InitializeVisuals()
        {
            EnsureVisuals();
            UpdateSurfaceStyle();
        }

        /// <summary>
        /// Immediately synchronizes surface, border, and resonance visuals with the underlying ColorPlatform.
        /// </summary>
        public void RefreshVisuals()
        {
            EnsureVisuals();
            UpdateSurfaceStyle();
        }

        private void EnsureVisuals()
        {
            if (platform == null)
                platform = GetComponent<ColorPlatform>();

            // Find and hide original single-color quad SpriteRenderer on child "Visual"
            SpriteRenderer[] existingRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer r in existingRenderers)
            {
                if (r.gameObject.name == "Visual")
                {
                    rawSpriteRenderer = r;
                    rawSpriteRenderer.enabled = false;
                }
            }

            if (visualRoot == null)
            {
                Transform existing = transform.Find("PlatformVisualPresentationRoot");
                if (existing != null)
                {
                    visualRoot = existing;
                }
                else
                {
                    GameObject rootGo = new GameObject("PlatformVisualPresentationRoot");
                    visualRoot = rootGo.transform;
                    visualRoot.SetParent(transform, false);
                }
            }

            // Surface child
            if (surfaceRenderer == null)
            {
                Transform surfaceT = visualRoot.Find("Surface");
                GameObject surfaceGo = surfaceT != null ? surfaceT.gameObject : new GameObject("Surface");
                surfaceGo.transform.SetParent(visualRoot, false);
                surfaceRenderer = surfaceGo.GetComponent<SpriteRenderer>();
                if (surfaceRenderer == null)
                    surfaceRenderer = surfaceGo.AddComponent<SpriteRenderer>();
                surfaceRenderer.sortingOrder = 0;
            }

            // Border child (crisp 9-slice or framed perimeter)
            if (borderRenderer == null)
            {
                Transform borderT = visualRoot.Find("Border");
                GameObject borderGo = borderT != null ? borderT.gameObject : new GameObject("Border");
                borderGo.transform.SetParent(visualRoot, false);
                borderRenderer = borderGo.GetComponent<SpriteRenderer>();
                if (borderRenderer == null)
                    borderRenderer = borderGo.AddComponent<SpriteRenderer>();
                borderRenderer.sprite = GetBorderSprite();
                borderRenderer.drawMode = SpriteDrawMode.Sliced;
                borderRenderer.sortingOrder = 1;
            }

            // Resonance ripple child (additive proximity highlight)
            if (resonanceRenderer == null)
            {
                Transform resT = visualRoot.Find("Resonance");
                GameObject resGo = resT != null ? resT.gameObject : new GameObject("Resonance");
                resGo.transform.SetParent(visualRoot, false);
                resonanceRenderer = resGo.GetComponent<SpriteRenderer>();
                if (resonanceRenderer == null)
                    resonanceRenderer = resGo.AddComponent<SpriteRenderer>();
                resonanceRenderer.sprite = GetBorderSprite();
                resonanceRenderer.drawMode = SpriteDrawMode.Sliced;
                resonanceRenderer.sortingOrder = 2;
                resonanceRenderer.color = new Color(0.93f, 0.96f, 0.98f, 0f);
            }

            UpdateSurfaceStyle();
        }

        private void UpdateSurfaceStyle()
        {
            if (platform == null || surfaceRenderer == null)
                return;

            Vector2 size = platform.Size;
            surfaceRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            if (borderRenderer != null)
            {
                borderRenderer.drawMode = SpriteDrawMode.Sliced;
                borderRenderer.size = size;
                borderRenderer.transform.localScale = Vector3.one;
            }
            if (resonanceRenderer != null)
            {
                resonanceRenderer.drawMode = SpriteDrawMode.Sliced;
                resonanceRenderer.size = size;
                resonanceRenderer.transform.localScale = Vector3.one;
            }

            float raw = platform.RawGrayscaleColor;
            LogicalColor logical = ColorClassifier.Classify(raw);

            lastRenderedSize = size;
            lastRenderedColor = raw;

            if (platform.IsUniversal || raw >= 254.5f)
            {
                surfaceRenderer.sprite = GetUniversalSprite();
                surfaceRenderer.color = Color.white;
                if (borderRenderer != null)
                    borderRenderer.color = UniversalBorder;
            }
            else if (logical == LogicalColor.White)
            {
                surfaceRenderer.sprite = GetWhiteSprite();
                surfaceRenderer.color = Color.white;
                if (borderRenderer != null)
                    borderRenderer.color = WhiteBorder;
            }
            else if (logical == LogicalColor.Gray)
            {
                surfaceRenderer.sprite = GetGraySprite();
                surfaceRenderer.color = Color.white;
                if (borderRenderer != null)
                    borderRenderer.color = GrayBorder;
            }
            else
            {
                surfaceRenderer.sprite = GetBlackSprite();
                surfaceRenderer.color = Color.white;
                if (borderRenderer != null)
                    borderRenderer.color = BlackBorder;
            }
        }

        private void Update()
        {
            // Detect if platform size or color changed in editor or runtime
            if (platform != null)
            {
                Vector2 currentSize = platform.Size;
                float currentColor = platform.RawGrayscaleColor;
                if (currentSize != lastRenderedSize || Mathf.Abs(currentColor - lastRenderedColor) > 0.001f)
                {
                    UpdateSurfaceStyle();
                }
            }

            // Maintain raw renderer disabled
            if (rawSpriteRenderer != null && rawSpriteRenderer.enabled)
                rawSpriteRenderer.enabled = false;

            // Update proximity resonance ripple
            UpdateProximityResonance();
        }

        private void UpdateProximityResonance()
        {
            if (cachedPlayer == null)
                cachedPlayer = FindAnyObjectByType<PlayerController>();

            if (cachedPlayer == null || resonanceRenderer == null)
                return;

            float distX = Mathf.Abs(cachedPlayer.transform.position.x - transform.position.x);
            const float resonanceDistance = 5f; // world units

            if (distX < resonanceDistance)
            {
                pulsePhase += Time.deltaTime * 6f;
                float localProximity = Mathf.Clamp01(1f - distX / resonanceDistance);
                bool colorMatches = (platform.Logical == cachedPlayer.CurrentColor) || platform.IsUniversal;

                float matchMultiplier = colorMatches ? 1.4f : 0.4f;
                float wave = 0.5f + 0.5f * Mathf.Sin(pulsePhase);
                float alpha = (0.08f + 0.22f * localProximity) * (0.6f + 0.4f * wave) * matchMultiplier;

                resonanceRenderer.color = new Color(0.95f, 0.98f, 1f, alpha);
            }
            else
            {
                resonanceRenderer.color = new Color(0.95f, 0.98f, 1f, 0f);
            }
        }

        #region Procedural Sprite Textures

        private static Sprite GetWhiteSprite()
        {
            if (cachedWhiteSprite != null)
                return cachedWhiteSprite;

            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color baseGrad = Color.Lerp(WhiteBottom, WhiteTop, t);
                for (int x = 0; x < w; x++)
                {
                    // Subtle optical stipple pattern
                    float stipple = ((x * 17 + y * 31) % 43 == 0) ? 0.08f : 0f;
                    pixels[y * w + x] = baseGrad + new Color(stipple, stipple, stipple, 0f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return cachedWhiteSprite;
        }

        private static Sprite GetGraySprite()
        {
            if (cachedGraySprite != null)
                return cachedGraySprite;

            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color baseGrad = Color.Lerp(GrayBottom, GrayTop, t);
                for (int x = 0; x < w; x++)
                {
                    // Diagonal diffraction grating lines (45 degrees)
                    bool isLine = ((x + y) % 16) < 2;
                    float grating = isLine ? 0.09f : 0f;
                    pixels[y * w + x] = baseGrad + new Color(grating, grating, grating, 0f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedGraySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return cachedGraySprite;
        }

        private static Sprite GetBlackSprite()
        {
            if (cachedBlackSprite != null)
                return cachedBlackSprite;

            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color baseGrad = Color.Lerp(BlackBottom, BlackTop, t);
                for (int x = 0; x < w; x++)
                {
                    // Subtle horizontal dashes
                    bool isDash = (y % 12 == 0) && ((x % 20) < 8);
                    float dash = isDash ? 0.06f : 0f;
                    pixels[y * w + x] = baseGrad + new Color(dash, dash, dash, 0f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedBlackSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return cachedBlackSprite;
        }

        private static Sprite GetUniversalSprite()
        {
            if (cachedUniversalSprite != null)
                return cachedUniversalSprite;

            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color baseGrad = Color.Lerp(UniversalBottom, UniversalTop, t);
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = baseGrad;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedUniversalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return cachedUniversalSprite;
        }

        private static Sprite GetBorderSprite()
        {
            if (cachedBorderSprite != null)
                return cachedBorderSprite;

            const int res = 64;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[res * res];
            const int borderPx = 2;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    bool isBorder = (x < borderPx || x >= res - borderPx || y < borderPx || y >= res - borderPx);
                    pixels[y * res + x] = isBorder ? Color.white : new Color(1f, 1f, 1f, 0f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 9-slice border
            cachedBorderSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx));

            return cachedBorderSprite;
        }

        #endregion
    }
}
