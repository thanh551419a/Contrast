namespace Contrast.Presentation
{
    using UnityEngine;
    using Contrast.Color;
    using Contrast.Level;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only component attached to LevelMarker instances that enhances
    /// StartPos, Checkpoint, and End with optical beacon pillars, concentric rings,
    /// and pulsing portal glow matching demo_quangbai1.html.
    ///
    /// Preserves existing LevelMarker geometry and AABB contact evaluation.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelMarkerVisualPresentation : MonoBehaviour
    {
        private LevelMarker marker;
        private SpriteRenderer rawSpriteRenderer;

        private Transform visualRoot;
        private SpriteRenderer baseRenderer;
        private SpriteRenderer glowRenderer;
        private SpriteRenderer beamRenderer;

        private static Sprite cachedRingSprite;
        private static Sprite cachedBeamSprite;

        private float pulsePhase = 0f;

        private void Awake()
        {
            marker = GetComponent<LevelMarker>();
            EnsureVisuals();
        }

        public void InitializeVisuals()
        {
            EnsureVisuals();
        }

        private void EnsureVisuals()
        {
            if (marker == null)
                marker = GetComponent<LevelMarker>();

            // Hide raw primitive SpriteRenderer on child "Visual"
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
                GameObject rootGo = new GameObject("MarkerVisualPresentationRoot");
                visualRoot = rootGo.transform;
                visualRoot.SetParent(transform, false);
            }

            // Marker base
            if (baseRenderer == null)
            {
                GameObject baseGo = new GameObject("Base");
                baseGo.transform.SetParent(visualRoot, false);
                baseRenderer = baseGo.AddComponent<SpriteRenderer>();
                baseRenderer.sprite = GetRingSprite();
                baseRenderer.sortingOrder = 4;
            }

            // Inner glow
            if (glowRenderer == null)
            {
                GameObject glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(visualRoot, false);
                glowRenderer = glowGo.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = GetRingSprite();
                glowRenderer.sortingOrder = 5;
            }

            // Vertical beacon beam
            if (beamRenderer == null && marker != null && marker.Type != LevelMarkerType.StartPos)
            {
                GameObject beamGo = new GameObject("Beam");
                beamGo.transform.SetParent(visualRoot, false);
                beamRenderer = beamGo.AddComponent<SpriteRenderer>();
                beamRenderer.sprite = GetBeamSprite();
                beamRenderer.sortingOrder = 3;
            }

            UpdateMarkerStyle();
        }

        private void UpdateMarkerStyle()
        {
            if (marker == null || visualRoot == null)
                return;

            Vector2 size = marker.Size;
            pulsePhase = Random.Range(0f, Mathf.PI * 2f);

            if (marker.Type == LevelMarkerType.CheckPoint)
            {
                baseRenderer.transform.localScale = new Vector3(size.x * 1.2f, size.y * 1.2f, 1f);
                glowRenderer.transform.localScale = new Vector3(size.x * 0.7f, size.y * 0.7f, 1f);

                Color col = marker.LogicalColor switch
                {
                    LogicalColor.White => new Color(0.95f, 0.98f, 1f, 0.75f),
                    LogicalColor.Gray => new Color(0.65f, 0.72f, 0.80f, 0.75f),
                    _ => new Color(0.35f, 0.45f, 0.55f, 0.75f)
                };

                baseRenderer.color = col;
                glowRenderer.color = Color.white;

                if (beamRenderer != null)
                {
                    beamRenderer.transform.localPosition = new Vector3(0f, size.y * 1.5f, 0f);
                    beamRenderer.transform.localScale = new Vector3(size.x * 0.8f, size.y * 3f, 1f);
                    beamRenderer.color = new Color(col.r, col.g, col.b, 0.28f);
                }

                // Task D: Hide all checkpoint visuals during gameplay while keeping trigger GameObject active
                HideCheckpointRenderers();
            }
            else if (marker.Type == LevelMarkerType.End)
            {
                // Optical Portal entrance
                baseRenderer.transform.localScale = new Vector3(size.x * 1.5f, size.y * 1.5f, 1f);
                glowRenderer.transform.localScale = new Vector3(size.x * 0.9f, size.y * 0.9f, 1f);

                baseRenderer.color = new Color(0.90f, 0.95f, 1f, 0.85f);
                glowRenderer.color = Color.white;

                if (beamRenderer != null)
                {
                    beamRenderer.transform.localPosition = new Vector3(0f, size.y * 2f, 0f);
                    beamRenderer.transform.localScale = new Vector3(size.x * 1.2f, size.y * 4f, 1f);
                    beamRenderer.color = new Color(0.85f, 0.95f, 1f, 0.35f);
                }
            }
            else
            {
                // StartPos
                baseRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
                glowRenderer.transform.localScale = new Vector3(size.x * 0.5f, size.y * 0.5f, 1f);
                baseRenderer.color = new Color(0.8f, 0.88f, 0.95f, 0.45f);
                glowRenderer.color = new Color(1f, 1f, 1f, 0.6f);
            }
        }

        private void HideCheckpointRenderers()
        {
            if (rawSpriteRenderer != null) rawSpriteRenderer.enabled = false;
            if (baseRenderer != null) baseRenderer.enabled = false;
            if (glowRenderer != null) glowRenderer.enabled = false;
            if (beamRenderer != null) beamRenderer.enabled = false;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }
        }

        private void Update()
        {
            if (rawSpriteRenderer != null && rawSpriteRenderer.enabled)
                rawSpriteRenderer.enabled = false;

            if (marker != null && marker.Type == LevelMarkerType.CheckPoint)
            {
                if ((baseRenderer != null && baseRenderer.enabled) ||
                    (glowRenderer != null && glowRenderer.enabled) ||
                    (beamRenderer != null && beamRenderer.enabled))
                {
                    HideCheckpointRenderers();
                }
                return;
            }

            pulsePhase += Time.deltaTime * 3.5f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulsePhase);

            if (glowRenderer != null)
            {
                float baseAlpha = marker != null && marker.Type == LevelMarkerType.End ? 0.8f : 0.5f;
                glowRenderer.color = new Color(1f, 1f, 1f, baseAlpha * (0.7f + 0.3f * pulse));
            }
        }

        #region Procedural Sprites

        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite != null)
                return cachedRingSprite;

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
                    float ringDist = Mathf.Abs(dist - (radius - 4f));
                    float alpha = Mathf.Clamp01(1f - ringDist / 4f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return cachedRingSprite;
        }

        private static Sprite GetBeamSprite()
        {
            if (cachedBeamSprite != null)
                return cachedBeamSprite;

            const int w = 32, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[w * h];
            float centerX = w * 0.5f;

            for (int y = 0; y < h; y++)
            {
                float yFade = Mathf.Clamp01(1f - (float)y / h);
                for (int x = 0; x < w; x++)
                {
                    float xDist = Mathf.Abs(x + 0.5f - centerX) / centerX;
                    float xFade = Mathf.Clamp01(1f - xDist * xDist);
                    float alpha = xFade * yFade;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedBeamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f, 0, SpriteMeshType.FullRect);
            return cachedBeamSprite;
        }

        #endregion
    }

    /// <summary>
    /// Presentation component for QuangDiemItem matching HTML drawQuangItems:
    /// - Warm golden radial halo (rgba(255,224,150,0.95) -> rgba(255,196,92,0.28) -> 0)
    /// - 4-pointed golden star (#ffdfa0 fill, #ffb74d stroke)
    /// - Inner highlight dot (#fffef1)
    /// - Bobbing + gentle rotation animation
    /// - 18-particle burst on collection
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuangDiemVisualPresentation : MonoBehaviour
    {
        public static readonly Color StarFillColor = new Color(1.000f, 0.875f, 0.627f, 1f);    // #ffdfa0
        public static readonly Color StarStrokeColor = new Color(1.000f, 0.718f, 0.302f, 0.92f); // #ffb74d
        public static readonly Color HighlightDotColor = new Color(1.000f, 0.996f, 0.945f, 0.92f); // #fffef1
        public static readonly Color GlowInnerColor = new Color(1.000f, 0.878f, 0.588f, 0.95f);  // rgba(255,224,150,0.95)
        public static readonly Color GlowMidColor = new Color(1.000f, 0.769f, 0.361f, 0.28f);    // rgba(255,196,92,0.28)

        private QuangDiemItem item;
        private Transform visualRoot;
        private SpriteRenderer glowRenderer;
        private SpriteRenderer starRenderer;
        private SpriteRenderer highlightRenderer;

        private static Sprite cachedStarSprite;
        private static Sprite cachedGlowSprite;
        private static Sprite cachedDotSprite;

        private struct BurstParticle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public Color baseColor;
        }

        private BurstParticle[] burstParticles;
        private bool isBursting;

        public SpriteRenderer StarRenderer => starRenderer;
        public SpriteRenderer GlowRenderer => glowRenderer;
        public SpriteRenderer HighlightRenderer => highlightRenderer;

        private void Awake()
        {
            item = GetComponent<QuangDiemItem>();
            InitializeVisuals();
        }

        public void InitializeVisuals()
        {
            if (item == null)
                item = GetComponent<QuangDiemItem>();

            if (visualRoot != null)
                return;

            GameObject rootGo = new GameObject("QuangDiemVisualRoot");
            visualRoot = rootGo.transform;
            visualRoot.SetParent(transform, false);

            Material unlitMat = PlayerVisualPresentation.GetSharedUnlitMaterial();

            // 1. Outer radial golden halo (r * 2.6 radius = 1.3 world units diameter)
            GameObject glowGo = new GameObject("GoldenGlow");
            glowGo.transform.SetParent(visualRoot, false);
            glowRenderer = glowGo.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = GetGlowSprite();
            if (unlitMat != null) glowRenderer.sharedMaterial = unlitMat;
            glowRenderer.sortingOrder = 6;
            glowRenderer.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            // 2. 4-pointed golden star core (#ffdfa0 with #ffb74d border)
            GameObject starGo = new GameObject("GoldenStar");
            starGo.transform.SetParent(visualRoot, false);
            starRenderer = starGo.AddComponent<SpriteRenderer>();
            starRenderer.sprite = GetStarSprite();
            if (unlitMat != null) starRenderer.sharedMaterial = unlitMat;
            starRenderer.sortingOrder = 7;
            starRenderer.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            // 3. Inner highlight dot (#fffef1 at (-r*0.22, +r*0.22))
            GameObject dotGo = new GameObject("HighlightDot");
            dotGo.transform.SetParent(starGo.transform, false);
            dotGo.transform.localPosition = new Vector3(-0.09f, 0.09f, 0f);
            dotGo.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            highlightRenderer = dotGo.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = GetDotSprite();
            if (unlitMat != null) highlightRenderer.sharedMaterial = unlitMat;
            highlightRenderer.color = HighlightDotColor;
            highlightRenderer.sortingOrder = 8;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isBursting)
            {
                bool anyAlive = false;
                if (burstParticles != null)
                {
                    for (int i = 0; i < burstParticles.Length; i++)
                    {
                        if (burstParticles[i].life <= 0f)
                            continue;

                        anyAlive = true;
                        burstParticles[i].life -= dt;
                        Vector2 vel = burstParticles[i].velocity;
                        vel.y -= 1.875f * dt; // HTML: vy += 120 * dt px/s^2 -> -1.875 units/s^2
                        burstParticles[i].velocity = vel;

                        if (burstParticles[i].transform != null)
                        {
                            burstParticles[i].transform.localPosition += (Vector3)(vel * dt);
                            float alpha = Mathf.Clamp01(burstParticles[i].life / burstParticles[i].maxLife);
                            Color c = burstParticles[i].baseColor;
                            burstParticles[i].renderer.color = new Color(c.r, c.g, c.b, alpha);
                        }
                    }
                }

                if (!anyAlive)
                {
                    isBursting = false;
                    gameObject.SetActive(false);
                }
                return;
            }

            if (item == null || item.IsCollected)
                return;

            // HTML: q.bob = (q.bob || 0) + dt * 3.1; wy = q.y + Math.sin(q.bob) * 3; rotate(Math.sin(q.bob * .65) * .18)
            item.BobPhase += dt * 3.1f;
            float bobOffset = Mathf.Sin(item.BobPhase) * (3f / 64f);
            transform.position = new Vector3(item.BasePosition.x, item.BasePosition.y + bobOffset, 0f);

            if (starRenderer != null)
            {
                float angleDeg = Mathf.Sin(item.BobPhase * 0.65f) * (0.18f * Mathf.Rad2Deg);
                starRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            }

            if (glowRenderer != null)
            {
                float pulse = 0.92f + 0.08f * Mathf.Sin(item.BobPhase * 1.4f);
                glowRenderer.transform.localScale = new Vector3(1.35f * pulse, 1.35f * pulse, 1f);
            }
        }

        public void PlayPickupBurst()
        {
            InitializeVisuals();

            if (glowRenderer != null) glowRenderer.enabled = false;
            if (starRenderer != null) starRenderer.enabled = false;
            if (highlightRenderer != null) highlightRenderer.enabled = false;

            if (!Application.isPlaying)
            {
                gameObject.SetActive(false);
                return;
            }

            isBursting = true;
            burstParticles = new BurstParticle[18];
            Material unlitMat = PlayerVisualPresentation.GetSharedUnlitMaterial();
            Sprite dotSprite = GetDotSprite();

            for (int i = 0; i < 18; i++)
            {
                float a = (Mathf.PI * 2f * i) / 18f + Random.Range(-0.12f, 0.12f);
                float sp = Random.Range(0.85f, 2.25f); // 55..145 px/s in world units
                float maxLife = Random.Range(0.32f, 0.54f);
                Color col = (i % 2 == 0)
                    ? new Color(1f, 0.851f, 0.541f, 1f) // #ffd98a
                    : Color.white;

                GameObject pGo = new GameObject($"Burst_{i}");
                pGo.transform.SetParent(visualRoot, false);
                pGo.transform.localPosition = Vector3.zero;
                float size = Random.Range(0.06f, 0.11f);
                pGo.transform.localScale = new Vector3(size, size, 1f);

                SpriteRenderer sr = pGo.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                if (unlitMat != null) sr.sharedMaterial = unlitMat;
                sr.color = col;
                sr.sortingOrder = 9;

                burstParticles[i] = new BurstParticle
                {
                    transform = pGo.transform,
                    renderer = sr,
                    velocity = new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp),
                    life = maxLife,
                    maxLife = maxLife,
                    baseColor = col
                };
            }
        }

        private static Sprite GetStarSprite()
        {
            if (cachedStarSprite != null)
                return cachedStarSprite;

            const int res = 128;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float center = res * 0.5f;
            float outerR = res * 0.44f;
            float innerR = outerR * 0.42f;

            // Build 8 vertices of the 4-pointed star
            Vector2[] poly = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = -Mathf.PI * 0.5f + i * (Mathf.PI / 4f);
                float rad = (i % 2 == 0) ? outerR : innerR;
                poly[i] = new Vector2(center + Mathf.Cos(angle) * rad, center + Mathf.Sin(angle) * rad);
            }

            Color[] pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = IsPointInPolygon(p, poly);
                    float edgeDist = DistanceToPolygonEdge(p, poly);

                    if (!inside && edgeDist > 1.8f)
                    {
                        pixels[y * res + x] = Color.clear;
                        continue;
                    }

                    float alpha = inside ? 1f : Mathf.Clamp01(1f - (edgeDist / 1.8f));
                    float borderBlend = inside ? Mathf.Clamp01(1f - (edgeDist / 3.2f)) : 1f;
                    Color c = Color.Lerp(StarFillColor, StarStrokeColor, borderBlend);
                    c.a *= alpha;
                    pixels[y * res + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0, SpriteMeshType.FullRect);
            return cachedStarSprite;
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y + 1e-6f) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float DistanceToPolygonEdge(Vector2 p, Vector2[] poly)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, Vector2.Dot(ab, ab)));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        private static Sprite GetGlowSprite()
        {
            if (cachedGlowSprite != null)
                return cachedGlowSprite;

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
                    float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius);
                    Color c;
                    if (t < 0.45f)
                    {
                        c = Color.Lerp(GlowInnerColor, GlowMidColor, t / 0.45f);
                    }
                    else
                    {
                        float outerT = (t - 0.45f) / 0.55f;
                        c = new Color(GlowMidColor.r, GlowMidColor.g, GlowMidColor.b, GlowMidColor.a * Mathf.Pow(1f - outerT, 1.8f));
                    }
                    pixels[y * res + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0, SpriteMeshType.FullRect);
            return cachedGlowSprite;
        }

        private static Sprite GetDotSprite()
        {
            if (cachedDotSprite != null)
                return cachedDotSprite;

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
            cachedDotSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0, SpriteMeshType.FullRect);
            return cachedDotSprite;
        }
    }
}
