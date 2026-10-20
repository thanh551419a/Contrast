namespace Contrast.Presentation
{
    using UnityEngine;
    using Contrast.Color;
    using Contrast.Player;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only component attached to the Player that generates and manages
    /// the optical silhouette, state-dependent rim glow, central core, motion trail,
    /// and landing squash/stretch feedback matching demo_quangbai1.html.
    /// 
    /// Strict Boundary Rule:
    /// All visual scaling and offsets are applied ONLY to local child visual transforms.
    /// The authoritative Player GameObject transform, hitbox, and AABB remain unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerVisualPresentation : MonoBehaviour
    {
        private PlayerController player;
        private SpriteRenderer rawSpriteRenderer;

        // Visual child hierarchy
        private Transform visualRoot;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer haloRenderer;
        private SpriteRenderer coreRenderer;

        // Visual Colors (matching demo_quangbai1.html)
        private static readonly Color BrightBody = new Color(0.953f, 0.965f, 0.973f, 1f); // #f3f6f8
        private static readonly Color BrightHalo = new Color(1f, 1f, 1f, 0.45f);

        private static readonly Color GrayBody = new Color(0.439f, 0.475f, 0.502f, 1f); // #707980
        private static readonly Color GrayHalo = new Color(0.835f, 0.867f, 0.886f, 0.25f);

        private static readonly Color DarkBody = new Color(0.047f, 0.063f, 0.078f, 1f); // #0c1014
        private static readonly Color DarkStrokeRim = new Color(0.851f, 0.882f, 0.902f, 1f); // #d9e1e6
        private static readonly Color DarkHalo = new Color(0.851f, 0.882f, 0.902f, 0.12f);

        // Procedural cached sprites
        private static Sprite cachedBodySprite;
        private static Sprite cachedHaloSprite;
        private static Sprite cachedCoreSprite;

        // Animation / Squash & Stretch
        private Vector3 currentVisualScale = Vector3.one;
        private Vector3 targetVisualScale = Vector3.one;
        private bool wasGroundedLastFrame = true;
        private PlayerState lastState = PlayerState.Falling;

        // Motion trail
        private struct TrailGhost
        {
            public Vector2 pos;
            public Color color;
            public float size;
            public float life;
            public float maxLife;
        }

        private readonly TrailGhost[] trailPool = new TrailGhost[24];
        private int trailCount = 0;
        private float trailSpawnTimer = 0f;
        private const float TrailSpawnInterval = 0.045f;

        public void InitializeVisuals()
        {
            if (visualRoot != null)
                return;

            if (player == null)
                player = GetComponent<PlayerController>();
            if (rawSpriteRenderer == null)
                rawSpriteRenderer = GetComponent<SpriteRenderer>();

            // Hide the raw primitive quad renderer if present
            if (rawSpriteRenderer != null)
            {
                rawSpriteRenderer.enabled = false;
            }

            SetupVisualHierarchy();
        }

        private void Awake()
        {
            InitializeVisuals();
        }

        private void SetupVisualHierarchy()
        {
            // Create container for visual children
            GameObject rootGo = new GameObject("VisualPresentationRoot");
            visualRoot = rootGo.transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = player != null ? (Vector3)player.PlayerOffset : Vector3.zero;

            // Halo (outer soft optical glow)
            GameObject haloGo = new GameObject("GlowHalo");
            haloGo.transform.SetParent(visualRoot, false);
            haloRenderer = haloGo.AddComponent<SpriteRenderer>();
            haloRenderer.sprite = GetHaloSprite();
            haloRenderer.sortingOrder = 9;

            // Body (optical silhouette with smooth borders)
            GameObject bodyGo = new GameObject("OpticalBody");
            bodyGo.transform.SetParent(visualRoot, false);
            bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = GetBodySprite();
            bodyRenderer.sortingOrder = 10;

            // Core dot (bright center optical emitter for BRIGHT state)
            GameObject coreGo = new GameObject("CoreLight");
            coreGo.transform.SetParent(visualRoot, false);
            coreRenderer = coreGo.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = GetCoreSprite();
            coreRenderer.sortingOrder = 11;

            UpdateVisualGeometry();
        }

        private void UpdateVisualGeometry()
        {
            if (player == null || visualRoot == null)
                return;

            Vector2 size = player.PlayerSize;
            bodyRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            haloRenderer.transform.localScale = new Vector3(size.x * 2.2f, size.y * 2.2f, 1f);
            coreRenderer.transform.localScale = new Vector3(size.x * 0.35f, size.y * 0.35f, 1f);
        }

        private void Update()
        {
            if (player == null)
                return;

            // Hide raw renderer if it was re-enabled
            if (rawSpriteRenderer != null && rawSpriteRenderer.enabled)
                rawSpriteRenderer.enabled = false;

            UpdateStateColors();
            UpdateSquashAndStretch();
            UpdateTrail();
        }

        private void UpdateStateColors()
        {
            LogicalColor color = player.CurrentColor;

            switch (color)
            {
                case LogicalColor.White:
                    bodyRenderer.color = BrightBody;
                    haloRenderer.color = BrightHalo;
                    coreRenderer.gameObject.SetActive(true);
                    coreRenderer.color = Color.white;
                    break;

                case LogicalColor.Gray:
                    bodyRenderer.color = GrayBody;
                    haloRenderer.color = GrayHalo;
                    coreRenderer.gameObject.SetActive(false);
                    break;

                case LogicalColor.Black:
                default:
                    // Dark state has dark interior with crisp rim stroke so it is always visible
                    bodyRenderer.color = DarkStrokeRim;
                    haloRenderer.color = DarkHalo;
                    coreRenderer.gameObject.SetActive(false);
                    break;
            }
        }

        private void UpdateSquashAndStretch()
        {
            PlayerState currentState = player.State;
            bool isGrounded = currentState == PlayerState.Grounded;

            // Landing squish
            if (isGrounded && !wasGroundedLastFrame)
            {
                currentVisualScale = new Vector3(1.22f, 0.78f, 1f);
            }
            // Jump stretch
            else if (!isGrounded && wasGroundedLastFrame && currentState != PlayerState.Dead)
            {
                currentVisualScale = new Vector3(0.82f, 1.25f, 1f);
            }

            wasGroundedLastFrame = isGrounded;
            lastState = currentState;

            // Smooth spring back to neutral scale (1, 1, 1)
            currentVisualScale = Vector3.Lerp(currentVisualScale, targetVisualScale, Time.deltaTime * 14f);
            if (visualRoot != null)
            {
                visualRoot.localScale = currentVisualScale;
            }
        }

        private void UpdateTrail()
        {
            float dt = Time.deltaTime;
            trailSpawnTimer += dt;

            // Spawn trail particle if moving
            if (trailSpawnTimer >= TrailSpawnInterval && player.State != PlayerState.Dead)
            {
                trailSpawnTimer = 0f;
                if (trailCount < trailPool.Length)
                {
                    Color trailColor = player.CurrentColor switch
                    {
                        LogicalColor.White => new Color(0.95f, 0.97f, 1f, 0.35f),
                        LogicalColor.Gray => new Color(0.55f, 0.6f, 0.65f, 0.25f),
                        _ => new Color(0.75f, 0.82f, 0.88f, 0.2f)
                    };

                    trailPool[trailCount++] = new TrailGhost
                    {
                        pos = transform.position,
                        color = trailColor,
                        size = player.PlayerSize.x * 0.7f,
                        life = 0.24f,
                        maxLife = 0.24f
                    };
                }
            }

            // Update existing trail particles
            for (int i = trailCount - 1; i >= 0; i--)
            {
                trailPool[i].life -= dt;
                if (trailPool[i].life <= 0f)
                {
                    // Swap remove
                    trailPool[i] = trailPool[trailCount - 1];
                    trailCount--;
                }
            }
        }

        #region Procedural Sprite Generation

        private static Sprite GetBodySprite()
        {
            if (cachedBodySprite != null)
                return cachedBodySprite;

            const int res = 64;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // Rounded silhouette with soft anti-aliased edge and crisp rim stroke
                    float edge = Mathf.Clamp01((radius - dist) / 1.5f);
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(dist - (radius - 2.5f)) / 2f);

                    // Combine solid body with subtle bright rim
                    float alpha = edge;
                    float brightness = Mathf.Lerp(0.85f, 1f, rim);
                    pixels[y * res + x] = new Color(brightness, brightness, brightness, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedBodySprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect);

            return cachedBodySprite;
        }

        private static Sprite GetHaloSprite()
        {
            if (cachedHaloSprite != null)
                return cachedHaloSprite;

            const int res = 128;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float t = Mathf.Clamp01(dist / radius);
                    // Inverse quadratic falloff for soft natural optical glow
                    float alpha = Mathf.Pow(1f - t, 2.2f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedHaloSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect);

            return cachedHaloSprite;
        }

        private static Sprite GetCoreSprite()
        {
            if (cachedCoreSprite != null)
                return cachedCoreSprite;

            const int res = 32;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            cachedCoreSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                res,
                0,
                SpriteMeshType.FullRect);

            return cachedCoreSprite;
        }

        #endregion
    }
}
