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

        private void Update()
        {
            if (rawSpriteRenderer != null && rawSpriteRenderer.enabled)
                rawSpriteRenderer.enabled = false;

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
}
