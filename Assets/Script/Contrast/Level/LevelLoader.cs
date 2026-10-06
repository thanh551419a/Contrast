using System.IO;
using Contrast.Camera;
using Contrast.Color;
using Contrast.Core;
using Contrast.Data;
using Contrast.Player;
using UnityEngine;

namespace Contrast.Level
{
    /// <summary>
    /// Reads JSON, creates runtime platforms and level interaction rectangles,
    /// configures the camera boundary, and owns the current retry spawn state.
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
        [SerializeField] private Transform levelObjectsRoot;

        public LevelData CurrentLevelData { get; private set; }
        public float KillY { get; private set; } = -10f;

        public Vector2 CurrentSpawnPosition { get; private set; }
        public LogicalColor CurrentSpawnColor { get; private set; } = LogicalColor.Black;

        public Transform PlatformsRoot => platformsRoot;
        public Transform LevelObjectsRoot => levelObjectsRoot;

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

            CurrentLevelData = data;

            Debug.Log(
                $"[FLOW][LEVEL_LOAD] platforms={data.platforms?.Length ?? 0} " +
                $"checkpoints={data.checkPoint?.Length ?? 0} " +
                $"hasEnd={data.end != null}");

            EnsureRoots();
            ClearRuntimeLevel();

            KillY = ComputeKillY(data);

            // --------------------------------------------------
            // Start position / current retry spawn.
            // --------------------------------------------------
            StartPosData startData = data.startPos;
            if (startData == null)
                startData = ConvertLegacySpawnPoint(data.spawnPoint);

            if (startData == null)
                startData = new StartPosData();

            CurrentSpawnPosition = startData.position != null
                ? startData.position.ToVector2()
                : Vector2.zero;

            CurrentSpawnColor =
                ColorClassifier.Classify(startData.color);

            CreateMarker(
                LevelMarkerType.StartPos,
                startData.position != null
                    ? startData.position.ToVector2()
                    : Vector2.zero,
                startData.size != null
                    ? startData.size.ToVector2()
                    : Vector2.one,
                startData.color);

            // --------------------------------------------------
            // Runtime platforms.
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
                    platform.Initialize(
                        pData.position.ToVector2(),
                        pData.size.ToVector2(),
                        pData.rawGrayscaleColor);

                    // Initialize trajectory (disabled by default for old data).
                    platform.InitializeTrajectory(pData.trajectory);
                }
            }

            // --------------------------------------------------
            // Checkpoints.
            // Multiple checkpoints are supported. The latest checkpoint
            // reached by the player updates CurrentSpawnPosition/Color.
            // --------------------------------------------------
            if (data.checkPoint != null)
            {
                for (int i = 0; i < data.checkPoint.Length; i++)
                {
                    CheckPointData checkPointData = data.checkPoint[i];

                    if (checkPointData == null ||
                        checkPointData.position == null ||
                        checkPointData.size == null)
                    {
                        continue;
                    }

                    CreateMarker(
                        LevelMarkerType.CheckPoint,
                        checkPointData.position.ToVector2(),
                        checkPointData.size.ToVector2(),
                        checkPointData.color);
                }
            }

            // --------------------------------------------------
            // End-game rectangle.
            // --------------------------------------------------
            if (data.end != null &&
                data.end.position != null &&
                data.end.size != null)
            {
                CreateMarker(
                    LevelMarkerType.End,
                    data.end.position.ToVector2(),
                    data.end.size.ToVector2());
            }

            // --------------------------------------------------
            // Camera boundary.
            // --------------------------------------------------
            CameraFollow camFollow =
                FindAnyObjectByType<CameraFollow>();

            if (camFollow != null && data.cameraBoundary != null)
            {
                camFollow.SetBoundary(
                    data.cameraBoundary.position != null
                        ? data.cameraBoundary.position.ToVector2()
                        : Vector2.zero,
                    data.cameraBoundary.size != null
                        ? data.cameraBoundary.size.ToVector2()
                        : Vector2.zero);
            }

            // --------------------------------------------------
            // Initial player state = StartPos position + color.
            // --------------------------------------------------
            PlayerController player =
                GameManager.Instance != null
                    ? GameManager.Instance.Player
                    : FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                Debug.Log(
                    $"[FLOW][SPAWN_INITIAL] position={CurrentSpawnPosition} " +
                    $"color={CurrentSpawnColor}");

                player.Respawn(
                    CurrentSpawnPosition,
                    CurrentSpawnColor);

                if (camFollow != null)
                    camFollow.Target = player.transform;
            }
        }

        /// <summary>
        /// Retry does NOT rebuild the level.
        /// It only uses the current checkpoint-derived spawn state.
        /// </summary>
        public void RetryCurrentSpawn()
        {
            PlayerController player =
                GameManager.Instance != null
                    ? GameManager.Instance.Player
                    : FindAnyObjectByType<PlayerController>();

            if (player == null)
                return;

            Debug.Log(
                $"[FLOW][SPAWN_RETRY] position={CurrentSpawnPosition} " +
                $"color={CurrentSpawnColor}");

            // Deterministically reset all moving platforms back to their initial phase
            TrajectorySystem.ResetAll();

            player.Respawn(
                CurrentSpawnPosition,
                CurrentSpawnColor);

            CameraFollow camFollow =
                FindAnyObjectByType<CameraFollow>();

            if (camFollow != null)
                camFollow.Target = player.transform;
        }

        /// <summary>
        /// Called once after Player movement. This is the authoritative contact
        /// point for checkpoint/end interactions.
        /// </summary>
        public void ProcessPlayerInteractions(PlayerController player)
        {
            if (player == null)
                return;

            Aabb playerBounds = Aabb.FromCenter(
                new Vector2(
                    player.transform.position.x,
                    player.transform.position.y) + player.PlayerOffset,
                new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(player.PlayerSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(player.PlayerSize.y))));

            for (int i = LevelMarker.All.Count - 1; i >= 0; i--)
            {
                LevelMarker marker = LevelMarker.All[i];

                if (marker == null || marker.IsConsumed)
                    continue;

                if (!marker.Overlaps(playerBounds))
                    continue;

                if (marker.Type == LevelMarkerType.CheckPoint)
                {
                    CurrentSpawnPosition = marker.Position;
                    CurrentSpawnColor = marker.LogicalColor;

                    Debug.Log(
                        $"[CHECKPOINT] reached position={CurrentSpawnPosition} " +
                        $"color={CurrentSpawnColor} " +
                        $"rawColor={marker.RawColor:F2}");

                    marker.OnPlayerTouched();
                    continue;
                }

                if (marker.Type == LevelMarkerType.End)
                {
                    Debug.Log(
                        $"[END] Player touched End at {marker.Position}.");

                    marker.OnPlayerTouched();
                    GameManager.Instance?.OnPlayerWon();
                    return;
                }
            }
        }


        private void CreateMarker(
            LevelMarkerType type,
            Vector2 position,
            Vector2 size,
            float color = 0f)
        {
            GameObject markerObject =
                new GameObject(type.ToString());

            markerObject.transform.SetParent(
                levelObjectsRoot,
                false);

            LevelMarker marker =
                markerObject.AddComponent<LevelMarker>();

            marker.Initialize(
                type,
                position,
                size,
                color);
        }

        private static StartPosData ConvertLegacySpawnPoint(
            SpawnPointData legacy)
        {
            if (legacy == null)
                return null;

            float color = legacy.initialPlayerColor >= 0f
                ? legacy.initialPlayerColor
                : legacy.color;

            return new StartPosData
            {
                position = legacy.position,
                size = new Vector2Data(1f, 1f),
                color = color
            };
        }

        private void EnsureRoots()
        {
            if (platformsRoot == null)
            {
                GameObject platforms =
                    new GameObject("Platforms");
                platforms.transform.SetParent(transform, false);
                platformsRoot = platforms.transform;
            }

            if (levelObjectsRoot == null)
            {
                GameObject objects =
                    new GameObject("LevelObjects");
                objects.transform.SetParent(transform, false);
                levelObjectsRoot = objects.transform;
            }
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
            ColorPlatform.ClearRegistry();
            LevelMarker.ClearRegistry();

            if (platformsRoot != null)
            {
                for (int i = platformsRoot.childCount - 1; i >= 0; i--)
                    Destroy(platformsRoot.GetChild(i).gameObject);
            }

            if (levelObjectsRoot != null)
            {
                for (int i = levelObjectsRoot.childCount - 1; i >= 0; i--)
                    Destroy(levelObjectsRoot.GetChild(i).gameObject);
            }
        }

        private void CreateFallbackLevel()
        {
            CurrentLevelData = new LevelData
            {
                startPos = new StartPosData
                {
                    position = new Vector2Data(0f, 1f),
                    size = new Vector2Data(1f, 1f),
                    color = 0f
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
