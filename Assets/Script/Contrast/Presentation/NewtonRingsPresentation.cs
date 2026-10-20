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

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material apparatusMaterial;
        private Texture2D interferenceTexture;

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
            if (interferenceTexture != null)
                return;

            const int res = 512;
            interferenceTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            float center = res * 0.5f;
            float maxR = center - 2f;
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float rNorm = Mathf.Sqrt(dx * dx + dy * dy) / maxR;

                    if (rNorm > 1f)
                    {
                        pixels[y * res + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    // Continuous wave interference math from demo_quangbai1.html
                    float worldR = rNorm * outerRadius;
                    float order = (worldR / ringScale) * (worldR / ringScale) * (referenceLambda / sourceLambda);

                    // Reflected Newton rings: dark at integer order, bright halfway between
                    float intensity = (1f - Mathf.Cos(Mathf.PI * 2f * order)) * 0.5f;
                    // Slightly compress highlights so it looks optical rather than white contour lines
                    intensity = Mathf.Pow(intensity, 0.72f);

                    float edgeFade = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(rNorm, 5f)), 0.22f);
                    float v = (7f + intensity * 224f * edgeFade) / 255f;

                    // Off-center spherical glass reflection highlight
                    float highlightDx = dx - center * 0.24f;
                    float highlightDy = dy - center * 0.28f;
                    float highlightDist = Mathf.Sqrt(highlightDx * highlightDx + highlightDy * highlightDy) / (maxR * 0.8f);
                    float glassHighlight = Mathf.Clamp01(1f - highlightDist) * 0.12f;

                    // Concentric physical rim mount near the edge
                    float rim = (rNorm > 0.96f) ? 0.35f : (rNorm > 0.93f ? 0.15f : 0f);

                    float rCol = Mathf.Clamp01(v + glassHighlight + rim);
                    float gCol = Mathf.Clamp01(v + glassHighlight + rim);
                    float bCol = Mathf.Clamp01(v * 1.02f + glassHighlight + rim);
                    float alpha = Mathf.Clamp01((1f - rNorm * 0.05f) * Mathf.Clamp01((1f - rNorm) / 0.02f));

                    pixels[y * res + x] = new Color(rCol, gCol, bCol, alpha);
                }
            }

            interferenceTexture.SetPixels(pixels);
            interferenceTexture.Apply();

            if (apparatusMaterial != null)
                apparatusMaterial.mainTexture = interferenceTexture;
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
