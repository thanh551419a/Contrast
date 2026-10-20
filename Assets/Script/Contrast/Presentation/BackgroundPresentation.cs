namespace Contrast.Presentation
{
    using UnityEngine;
    using Color = UnityEngine.Color;
    /// <summary>
    /// Presentation-only component responsible for rendering the dark optical
    /// atmosphere, vertical background gradient, distant diffraction rings,
    /// and ambient floating optical dust motes matching demo_quangbai1.html.
    /// Does NOT affect any gameplay or collision logic.
    /// </summary>
    [ExecuteAlways]
    public class BackgroundPresentation : MonoBehaviour
    {
        private static BackgroundPresentation instance;
        public static BackgroundPresentation Instance => instance;

        [Header("Colors (from HTML reference)")]
        // Background gradient: #07111e -> #09101d -> #050812
        private static readonly Color TopColor = new Color(0.027f, 0.067f, 0.118f, 1f);
        private static readonly Color MidColor = new Color(0.035f, 0.063f, 0.114f, 1f);
        private static readonly Color BottomColor = new Color(0.020f, 0.031f, 0.071f, 1f);

        [Header("Screen Flash & Fade")]
        private float screenPulse = 0f;
        private float deathFade = 0f;

        private UnityEngine.Camera targetCam;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material gradientMaterial;
        private Texture2D gradientTex;

        // Ambient particles
        private struct DustMote
        {
            public Vector2 pos;
            public float radius;
            public float phase;
            public float speed;
        }

        private DustMote[] motes;
        private const int MoteCount = 45;

        private void Awake()
        {
            instance = this;
            EnsureComponents();
            InitMotes();
        }

        private void OnEnable()
        {
            instance = this;
            ConfigureCamera();
        }

        public void TriggerScreenPulse(float intensity = 0.4f)
        {
            screenPulse = Mathf.Max(screenPulse, intensity);
        }

        public void SetDeathFade(float fade)
        {
            deathFade = Mathf.Clamp01(fade);
        }

        private void EnsureComponents()
        {
            if (targetCam == null)
                targetCam = UnityEngine.Camera.main;

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

            if (gradientMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                             ?? Shader.Find("Sprites/Default");
                gradientMaterial = new Material(shader);
                gradientMaterial.hideFlags = HideFlags.DontSave;
                meshRenderer.sharedMaterial = gradientMaterial;
                meshRenderer.sortingOrder = -200; // Far behind all gameplay objects
            }

            BuildGradientTexture();
            BuildQuadMesh();
        }

        private void ConfigureCamera()
        {
            if (targetCam == null)
                targetCam = UnityEngine.Camera.main;

            if (targetCam != null)
            {
                // Match the HTML prototype base clear color #050812
                targetCam.clearFlags = CameraClearFlags.SolidColor;
                targetCam.backgroundColor = BottomColor;
            }
        }

        private void BuildGradientTexture()
        {
            if (gradientTex != null)
                return;

            gradientTex = new Texture2D(1, 256, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[256];
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f;
                Color col;
                if (t > 0.58f)
                {
                    float u = (t - 0.58f) / 0.42f;
                    col = Color.Lerp(MidColor, TopColor, u);
                }
                else
                {
                    float u = t / 0.58f;
                    col = Color.Lerp(BottomColor, MidColor, u);
                }
                pixels[y] = col;
            }

            gradientTex.SetPixels(pixels);
            gradientTex.Apply();

            if (gradientMaterial != null)
                gradientMaterial.mainTexture = gradientTex;
        }

        private void BuildQuadMesh()
        {
            if (meshFilter == null || meshFilter.sharedMesh != null)
                return;

            Mesh mesh = new Mesh
            {
                name = "BackgroundQuad",
                hideFlags = HideFlags.DontSave
            };

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f)
            };

            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
        }

        private void InitMotes()
        {
            motes = new DustMote[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                motes[i] = new DustMote
                {
                    pos = new Vector2(Random.Range(-30f, 60f), Random.Range(-10f, 20f)),
                    radius = Random.Range(0.04f, 0.12f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    speed = Random.Range(0.2f, 0.6f)
                };
            }
        }

        private void LateUpdate()
        {
            if (targetCam == null)
                targetCam = UnityEngine.Camera.main;

            if (targetCam == null)
                return;

            // Position quad to fill camera view exactly
            float height = targetCam.orthographic ? targetCam.orthographicSize * 2f : 10f;
            float width = height * targetCam.aspect;

            // Extra padding to avoid edge seams during camera motion
            transform.position = new Vector3(targetCam.transform.position.x, targetCam.transform.position.y, 10f);
            transform.localScale = new Vector3(width * 1.5f, height * 1.5f, 1f);

            // Screen pulse decay
            if (screenPulse > 0f)
            {
                screenPulse = Mathf.Max(0f, screenPulse - Time.deltaTime * 1.8f);
            }
        }

        private void OnDestroy()
        {
            if (gradientTex != null)
                DestroyImmediate(gradientTex);
            if (gradientMaterial != null)
                DestroyImmediate(gradientMaterial);
            if (meshFilter != null && meshFilter.sharedMesh != null)
                DestroyImmediate(meshFilter.sharedMesh);
        }
    }
}
