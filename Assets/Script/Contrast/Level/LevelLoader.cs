using System.IO;
using Contrast.Camera;
using Contrast.Core;
using Contrast.Data;
using Contrast.Player;
using UnityEngine;

namespace Contrast.Level
{
    /// <summary>
    /// Reads JSON, creates runtime ColorPlatforms, configures the camera boundary,
    /// and moves the Player transform to the spawn position.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [Header("Level Source")]
        [Tooltip("Kéo file .json vào đây (ưu tiên dùng cách này).")]
        [SerializeField] private TextAsset levelJsonAsset;
        [Tooltip("Dự phòng nếu không gán TextAsset.")]
        [SerializeField] private string levelJsonFilePath = "Assets/Levels/SampleLevel.json";
        [SerializeField] private ColorPlatform platformPrefab;

        [Header("Hierarchy")]
        [SerializeField] private Transform platformsRoot;

        public LevelData CurrentLevelData { get; private set; }
        public float KillY { get; private set; } = -10f;

        public void LoadAndInitializeLevel()
        {
            string json = LoadJsonString();

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning(
                    "[LevelLoader] Không tìm thấy JSON — dùng level dự phòng.");
                CreateFallbackLevel();
                return;
            }

            try
            {
                CurrentLevelData =
                    JsonUtility.FromJson<LevelData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[LevelLoader] Lỗi parse JSON: {ex.Message}");
                CreateFallbackLevel();
                return;
            }

            BuildRuntimeLevel(CurrentLevelData);
        }

        private string LoadJsonString()
        {
            if (levelJsonAsset != null)
                return levelJsonAsset.text;

            string[] candidates =
            {
                levelJsonFilePath,
                Path.Combine(Application.dataPath, "..", levelJsonFilePath),
                Path.Combine(Application.dataPath, levelJsonFilePath),
                Path.Combine(Application.streamingAssetsPath, levelJsonFilePath)
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }

            return null;
        }

        public void BuildRuntimeLevel(LevelData data)
        {
            if (data == null)
                return;

            EnsurePlatformsRoot();
            ClearRuntimeLevel();

            KillY = ComputeKillY(data);

            // --------------------------------------------------
            // Spawn runtime platforms.
            // --------------------------------------------------
            if (data.platforms != null)
            {
                for (int i = 0; i < data.platforms.Length; i++)
                {
                    ColorPlatformData pData = data.platforms[i];

                    if (pData == null ||
                        pData.position == null ||
                        pData.size == null)
                    {
                        continue;
                    }

                    ColorPlatform platform;

                    if (platformPrefab != null)
                    {
                        platform = Instantiate(
                            platformPrefab,
                            platformsRoot);
                    }
                    else
                    {
                        GameObject platformObject =
                            new GameObject($"ColorPlatform_{i}");

                        platformObject.transform.SetParent(
                            platformsRoot,
                            false);

                        platform =
                            platformObject.AddComponent<ColorPlatform>();
                    }

                    platform.name = $"ColorPlatform_{i}";

                    // ColorPlatform.Initialize() stores the custom gameplay AABB.
                    // Unity Physics2D is not used for player collision.
                    platform.Initialize(
                        pData.position.ToVector2(),
                        pData.size.ToVector2(),
                        pData.rawGrayscaleColor);
                }
            }

            // --------------------------------------------------
            // Camera boundary.
            // --------------------------------------------------
            CameraFollow camFollow =
                FindAnyObjectByType<CameraFollow>();

            if (camFollow != null && data.cameraBoundary != null)
            {
                camFollow.SetBoundary(
                    data.cameraBoundary.position.ToVector2(),
                    data.cameraBoundary.size.ToVector2());
            }

            // --------------------------------------------------
            // Player spawn.
            // --------------------------------------------------
            PlayerController player =
                GameManager.Instance != null
                    ? GameManager.Instance.Player
                    : FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                if (data.spawnPoint != null &&
                    data.spawnPoint.position != null)
                {
                    player.SetSpawnPosition(
                        data.spawnPoint.position.ToVector2());
                }

                player.ResetState();

                if (camFollow != null)
                    camFollow.Target = player.transform;
            }
        }

        private void EnsurePlatformsRoot()
        {
            if (platformsRoot != null)
                return;

            GameObject go = new GameObject("Platforms");
            go.transform.SetParent(transform, false);
            platformsRoot = go.transform;
        }

        private static float ComputeKillY(LevelData data)
        {
            if (data.cameraBoundary != null &&
                data.cameraBoundary.position != null &&
                data.cameraBoundary.size != null)
            {
                return data.cameraBoundary.position.y
                       - data.cameraBoundary.size.y * 0.5f
                       - 1f;
            }

            if (data.platforms != null && data.platforms.Length > 0)
            {
                float lowest = float.MaxValue;

                foreach (ColorPlatformData p in data.platforms)
                {
                    if (p != null &&
                        p.position != null &&
                        p.size != null)
                    {
                        lowest = Mathf.Min(
                            lowest,
                            p.position.y - p.size.y * 0.5f);
                    }
                }

                if (lowest != float.MaxValue)
                    return lowest - 10f;
            }

            return -50f;
        }

        private void ClearRuntimeLevel()
        {
            // Destroy is deferred by Unity. Clear the registry immediately so
            // collision logic cannot see old platform references during restart.
            ColorPlatform.ClearRegistry();

            if (platformsRoot == null)
                return;

            for (int i = platformsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(platformsRoot.GetChild(i).gameObject);
            }
        }

        private void CreateFallbackLevel()
        {
            CurrentLevelData = new LevelData
            {
                spawnPoint = new SpawnPointData
                {
                    position = new Vector2Data(0f, 1f)
                },
                cameraBoundary = new CameraBoundaryData
                {
                    position = new Vector2Data(15f, 0f),
                    size = new Vector2Data(40f, 20f)
                },
                platforms = new[]
                {
                    new ColorPlatformData
                    {
                        position = new Vector2Data(0f, -0.5f),
                        size = new Vector2Data(8f, 1f),
                        rawGrayscaleColor = 255f
                    }
                }
            };

            BuildRuntimeLevel(CurrentLevelData);
        }
    }
}
