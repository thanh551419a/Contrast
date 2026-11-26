namespace Contrast.Presentation
{
    using UnityEngine;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only component responsible for rendering the continuous
    /// Newton Rings interference field, glass lens shading, spherical highlight,
    /// and optical mount rim matching demo_quangbai1.html.
    ///
    /// Scientific Formula from HTML prototype:
    ///   order = (r / ringScale)^2 * (referenceLambda / sourceLambda)
    ///   I = ((1 - cos(2 * PI * order)) / 2)^0.72
    ///
    /// Evaluated continuously to produce continuous optical fringes rather than
    /// discrete circular lines.
    /// </summary>
    [ExecuteAlways]
    public class NewtonRingsPresentation : MonoBehaviour
    {
        [Header("Optical Parameters (Matching HTML Prototype)")]
        [SerializeField] private float ringScale = 3.6f;
        [SerializeField] private float outerRadius = 8.5f;
        [SerializeField] private float sourceLambda = 550f;
        [SerializeField] private float referenceLambda = 550f;
        [SerializeField, Range(0f, 1f)] private float colorRevealProgress = 0f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material apparatusMaterial;
        private Texture2D interferenceTexture;
        private Color32[] grayscalePixels;
        private Color32[] spectralPixels;
        private Color32[] workingPixels;
        private float[] pixelRNorms;
        private int cachedTexRes;

        public float SourceLambda => sourceLambda;
        public float ColorRevealProgress => colorRevealProgress;
        public bool IsColorRevealed => colorRevealProgress >= 0.999f;
        public Color CurrentSpectralColor => Color.Lerp(new Color(0.86f, 0.88f, 0.92f, 1f), WavelengthToRgb(sourceLambda), colorRevealProgress);
        public Texture2D InterferenceTexture => interferenceTexture;

        /// <summary>
        /// Converts a visible light wavelength in nanometers (380..750 nm) into its
        /// corresponding monochromatic RGB color for optical interference rendering.
        /// </summary>
        public static Color WavelengthToRgb(float wavelengthNm)
        {
            float w = Mathf.Clamp(wavelengthNm, 380f, 750f);
            float r = 0f, g = 0f, b = 0f;

            if (w >= 380f && w < 440f)
            {
                r = -(w - 440f) / (440f - 380f);
                g = 0f;
                b = 1f;
            }
            else if (w >= 440f && w < 490f)
            {
                r = 0f;
                g = (w - 440f) / (490f - 440f);
                b = 1f;
            }
            else if (w >= 490f && w < 510f)
            {
                r = 0f;
                g = 1f;
                b = -(w - 510f) / (510f - 490f);
            }
            else if (w >= 510f && w < 580f)
            {
                r = (w - 510f) / (580f - 510f);
                g = 1f;
                b = 0f;
            }
            else if (w >= 580f && w < 645f)
            {
                r = 1f;
                g = -(w - 645f) / (645f - 580f);
                b = 0f;
            }
            else if (w >= 645f && w <= 750f)
            {
                r = 1f;
                g = 0f;
                b = 0f;
            }

            float factor = 1f;
            if (w >= 380f && w < 420f)
                factor = 0.35f + 0.65f * (w - 380f) / (420f - 380f);
            else if (w > 700f && w <= 750f)
                factor = 0.35f + 0.65f * (750f - w) / (750f - 700f);

            const float gamma = 0.80f;
            float rf = r > 0f ? Mathf.Pow(r * factor, gamma) : 0f;
            float gf = g > 0f ? Mathf.Pow(g * factor, gamma) : 0f;
            float bf = b > 0f ? Mathf.Pow(b * factor, gamma) : 0f;

            // Keep slight optical saturation floor so fringes remain crisp and luminous against dark background
            return new Color(
                Mathf.Clamp01(rf),
                Mathf.Clamp01(gf),
                Mathf.Clamp01(bf),
                1f);
        }

        public static string GetWavelengthColorNameVi(float wavelengthNm)
        {
            if (wavelengthNm < 435f) return "Tím";
            if (wavelengthNm < 495f) return "Xanh dương";
            if (wavelengthNm < 570f) return "Lục";
            if (wavelengthNm < 590f) return "Vàng";
            if (wavelengthNm < 620f) return "Cam";
            return "Đỏ";
        }

        private void Awake()
        {
            EnsureApparatus();
        }

        private void OnEnable()
        {
            EnsureApparatus();
        }

        public void SetCenterPosition(Vector2 pos)
        {
            transform.position = new Vector3(pos.x, pos.y, 2f);
        }

        public void Configure(float scale, float outer, float sLambda = 550f, float rLambda = 550f, bool startColored = false)
        {
            ringScale = scale;
            outerRadius = outer;
            sourceLambda = sLambda;
            referenceLambda = rLambda;
            colorRevealProgress = startColored ? 1f : 0f;
            if (interferenceTexture != null)
            {
                DestroyImmediate(interferenceTexture);
                interferenceTexture = null;
            }
            grayscalePixels = null;
            spectralPixels = null;
            workingPixels = null;
            pixelRNorms = null;
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                DestroyImmediate(meshFilter.sharedMesh);
                meshFilter.sharedMesh = null;
            }
            EnsureApparatus();
        }

        /// <summary>
        /// Updates the progressive color reveal animation (0 = black-and-white/grayscale film,
        /// 1 = full true monochromatic spectral color blooming radially from the center).
        /// </summary>
        public void SetColorRevealProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            if (interferenceTexture == null || grayscalePixels == null || spectralPixels == null)
            {
                colorRevealProgress = clamped;
                EnsureApparatus();
                return;
            }

            if (Mathf.Approximately(colorRevealProgress, clamped))
                return;

            colorRevealProgress = clamped;
            ApplyColorRevealToTexture();
        }

        public void SetColorRevealedImmediate(bool revealed)
        {
            SetColorRevealProgress(revealed ? 1f : 0f);
        }

        private void EnsureApparatus()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (apparatusMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                             ?? Shader.Find("Sprites/Default");
                apparatusMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.DontSave
                };
                meshRenderer.sharedMaterial = apparatusMaterial;
                meshRenderer.sortingOrder = -50; // Behind player and platforms, in front of background
            }

            BuildInterferenceTexture();
            BuildDiscMesh();
        }

        private void BuildInterferenceTexture()
        {
            if (interferenceTexture != null && grayscalePixels != null && spectralPixels != null)
                return;

            const int res = 256;
            cachedTexRes = res;
            if (interferenceTexture == null)
            {
                interferenceTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
            }

            float center = res * 0.5f;
            float maxR = center - 2f;
            int total = res * res;
            grayscalePixels = new Color32[total];
            spectralPixels = new Color32[total];
            workingPixels = new Color32[total];
            pixelRNorms = new float[total];

            Color spectralColor = WavelengthToRgb(sourceLambda);
            // Film black-and-white grayscale palette: luminous silver-white peaks & neutral gray fringes
            Color grayscaleFilmColor = new Color(0.90f, 0.92f, 0.95f, 1f);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int idx = y * res + x;
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float rNorm = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                    pixelRNorms[idx] = rNorm;

                    if (rNorm > 1f)
                    {
                        Color32 clear = new Color32(0, 0, 0, 0);
                        grayscalePixels[idx] = clear;
                        spectralPixels[idx] = clear;
                        continue;
                    }

                    // Keep ring geometry strictly locked to ringScale (only info and color change with wavelength)
                    float worldR = rNorm * outerRadius;
                    float order = (worldR / ringScale) * (worldR / ringScale);

                    // Reflected Newton rings: dark at integer order, bright halfway between
                    float intensity = (1f - Mathf.Cos(Mathf.PI * 2f * order)) * 0.5f;
                    // Slightly compress highlights so it looks optical rather than contour lines
                    intensity = Mathf.Pow(intensity, 0.72f);

                    float edgeFade = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(rNorm, 5f)), 0.22f);
                    float fringe = intensity * edgeFade;
                    float baseDark = 7f / 255f;
                    float coreHighlight = Mathf.Pow(fringe, 2.8f) * 0.16f;

                    // Off-center spherical glass reflection highlight
                    float highlightDx = dx - center * 0.24f;
                    float highlightDy = dy - center * 0.28f;
                    float highlightDist = Mathf.Sqrt(highlightDx * highlightDx + highlightDy * highlightDy) / (maxR * 0.8f);
                    float glassHighlight = Mathf.Clamp01(1f - highlightDist) * 0.08f;

                    // Concentric physical rim mount near the edge
                    float rim = (rNorm > 0.96f) ? 0.30f : (rNorm > 0.93f ? 0.12f : 0f);
                    float alpha = Mathf.Clamp01((1f - rNorm * 0.05f) * Mathf.Clamp01((1f - rNorm) / 0.02f));

                    // 1. Grayscale / black-and-white film pixel
                    float gVal = Mathf.Clamp01(baseDark + fringe * 0.86f * grayscaleFilmColor.r + coreHighlight + glassHighlight + rim * 0.82f);
                    float gValG = Mathf.Clamp01(baseDark + fringe * 0.86f * grayscaleFilmColor.g + coreHighlight + glassHighlight + rim * 0.84f);
                    float gValB = Mathf.Clamp01(baseDark + fringe * 0.86f * grayscaleFilmColor.b + coreHighlight + glassHighlight + rim * 0.86f);
                    grayscalePixels[idx] = new Color(gVal, gValG, gValB, alpha);

                    // 2. True monochromatic spectral pixel
                    float rCol = Mathf.Clamp01(baseDark + fringe * 0.88f * spectralColor.r + coreHighlight + glassHighlight + rim * (0.45f + 0.55f * spectralColor.r));
                    float gCol = Mathf.Clamp01(baseDark + fringe * 0.88f * spectralColor.g + coreHighlight + glassHighlight + rim * (0.45f + 0.55f * spectralColor.g));
                    float bCol = Mathf.Clamp01(baseDark + fringe * 0.88f * spectralColor.b + coreHighlight + glassHighlight + rim * (0.45f + 0.55f * spectralColor.b));
                    spectralPixels[idx] = new Color(rCol, gCol, bCol, alpha);
                }
            }

            ApplyColorRevealToTexture();

            if (apparatusMaterial != null)
                apparatusMaterial.mainTexture = interferenceTexture;
        }

        private void ApplyColorRevealToTexture()
        {
            if (interferenceTexture == null || grayscalePixels == null || spectralPixels == null)
                return;

            if (colorRevealProgress <= 0.001f)
            {
                interferenceTexture.SetPixels32(grayscalePixels);
                interferenceTexture.Apply(false);
                return;
            }

            if (colorRevealProgress >= 0.999f)
            {
                interferenceTexture.SetPixels32(spectralPixels);
                interferenceTexture.Apply(false);
                return;
            }

            // Smooth radial wave blooming outward from center (rNorm = 0 -> 1)
            const float waveSoftness = 0.32f;
            float waveFront = colorRevealProgress * (1f + waveSoftness);
            int total = grayscalePixels.Length;
            for (int i = 0; i < total; i++)
            {
                float rNorm = pixelRNorms[i];
                if (rNorm > 1f)
                {
                    workingPixels[i] = grayscalePixels[i];
                    continue;
                }
                float t = Mathf.Clamp01((waveFront - rNorm) / waveSoftness);
                // Smoothstep for organic optical bloom
                t = t * t * (3f - 2f * t);
                workingPixels[i] = Color32.Lerp(grayscalePixels[i], spectralPixels[i], t);
            }

            interferenceTexture.SetPixels32(workingPixels);
            interferenceTexture.Apply(false);
        }

        private void BuildDiscMesh()
        {
            if (meshFilter == null || meshFilter.sharedMesh != null)
                return;

            Mesh mesh = new Mesh
            {
                name = "NewtonApparatusDisc",
                hideFlags = HideFlags.DontSave
            };

            float size = outerRadius * 2f;
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-outerRadius, -outerRadius, 0f),
                new Vector3(outerRadius, -outerRadius, 0f),
                new Vector3(-outerRadius, outerRadius, 0f),
                new Vector3(outerRadius, outerRadius, 0f)
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (interferenceTexture != null)
                DestroyImmediate(interferenceTexture);
            if (apparatusMaterial != null)
                DestroyImmediate(apparatusMaterial);
            if (meshFilter != null && meshFilter.sharedMesh != null)
                DestroyImmediate(meshFilter.sharedMesh);
        }
    }
}
