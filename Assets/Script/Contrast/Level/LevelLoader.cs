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

        public Transform PlatformsRoot
        {
            get
            {
                EnsureRoots();
                return platformsRoot;
            }
        }

        public Transform LevelObjectsRoot
        {
            get
            {
                EnsureRoots();
                return levelObjectsRoot;
            }
        }

        private void Awake()
        {
            EnsureRoots();
        }

        public void LoadAndInitializeLevel()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentLevelSource != null)
            {
                GameManager.Instance.Restart();
                return;
            }

            // Ensure persistent levels directory has default level if empty
            LevelRepository.EnsureDefaultLevelFile();
            string[] files = LevelRepository.ListLevelFiles();
            string path = (files != null && files.Length > 0) ? files[0] : null;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Debug.Log($"[FLOW][LEVEL_FILE] load path={path}");
                try
                {
                    string json = LevelRepository.ReadAllText(path);
                    LevelData data = JsonUtility.FromJson<LevelData>(json);
                    data = LevelDataSanitizer.Sanitize(data, json, out var report);
                    if (report != null && report.Count > 0)
                    {
                        Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                        foreach (string item in report)
                            Debug.Log($"[LEVEL_FIX] {item}");
                    }
                    BuildRuntimeLevel(data);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LevelLoader] Error loading level from {path}: {ex.Message}");
                }
            }

            // Legacy fallback if no persistent file is available
            string legacyJson = LoadJsonString();
            if (!string.IsNullOrEmpty(legacyJson))
            {
                try
                {
                    LevelData data = JsonUtility.FromJson<LevelData>(legacyJson);
                    data = LevelDataSanitizer.Sanitize(data, legacyJson, out _);
                    BuildRuntimeLevel(data);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LevelLoader] Error parsing legacy JSON: {ex.Message}");
                }
            }

            CreateFallbackLevel();
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
                    var pres = platform.GetComponent<Contrast.Presentation.PlatformVisualPresentation>();
                    if (pres == null)
                        pres = platform.gameObject.AddComponent<Contrast.Presentation.PlatformVisualPresentation>();

                    platform.Initialize(
                        pData.position.ToVector2(),
                        pData.size.ToVector2(),
                        pData.rawGrayscaleColor);

                    pres.InitializeVisuals();

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

                    Contrast.Presentation.VisualPresentationManager.Instance?.NotifyCheckpointReached();
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

        public void EnsureRoots()
        {
            if (platformsRoot == null)
            {
                Transform existingChild = transform.Find("Platforms");
                if (existingChild != null)
                {
                    platformsRoot = existingChild;
                }
                else
                {
                    GameObject existing = GameObject.Find("Platforms");
                    if (existing != null)
                    {
                        platformsRoot = existing.transform;
                    }
                    else
                    {
                        GameObject platforms =
                            new GameObject("Platforms");
                        platforms.transform.SetParent(transform, false);
                        platformsRoot = platforms.transform;
                    }
                }
            }

            if (levelObjectsRoot == null)
            {
                Transform existingChild = transform.Find("LevelObjects");
                if (existingChild != null)
                {
                    levelObjectsRoot = existingChild;
                }
                else
                {
                    GameObject existing = GameObject.Find("LevelObjects");
                    if (existing != null)
                    {
                        levelObjectsRoot = existing.transform;
                    }
                    else
                    {
                        GameObject objects =
                            new GameObject("LevelObjects");
                        objects.transform.SetParent(transform, false);
                        levelObjectsRoot = objects.transform;
                    }
                }
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

        public void ClearLevel(bool preserveCameraBoundary = true)
        {
            CameraBoundaryData boundary = preserveCameraBoundary ? CurrentLevelData?.cameraBoundary : null;
            CurrentLevelData = new LevelData
            {
                platforms = new ColorPlatformData[0],
                startPos = null,
                checkPoint = null,
                end = null,
                spawnPoint = null,
                cameraBoundary = boundary
            };
            ClearRuntimeLevel();
        }

        public void ClearRuntimeLevel()
        {
            EnsureRoots();

            // Destroy all registered platforms first
            for (int i = ColorPlatform.All.Count - 1; i >= 0; i--)
            {
                ColorPlatform p = ColorPlatform.All[i];
                if (p != null && p.gameObject != null)
                    SafeDestroy(p.gameObject);
            }
            ColorPlatform.ClearRegistry();

            // Destroy all registered markers first
            for (int i = LevelMarker.All.Count - 1; i >= 0; i--)
            {
                LevelMarker m = LevelMarker.All[i];
                if (m != null && m.gameObject != null)
                    SafeDestroy(m.gameObject);
            }
            LevelMarker.ClearRegistry();

            // Destroy any remaining children under platformsRoot
            if (platformsRoot != null)
            {
                for (int i = platformsRoot.childCount - 1; i >= 0; i--)
                    SafeDestroy(platformsRoot.GetChild(i).gameObject);
            }

            // Destroy any remaining children under levelObjectsRoot
            if (levelObjectsRoot != null)
            {
                for (int i = levelObjectsRoot.childCount - 1; i >= 0; i--)
                    SafeDestroy(levelObjectsRoot.GetChild(i).gameObject);
            }

            // Sweep scene for any orphaned ColorPlatform or LevelMarker
            ColorPlatform[] strayPlatforms = FindObjectsByType<ColorPlatform>();
            for (int i = 0; i < strayPlatforms.Length; i++)
            {
                if (strayPlatforms[i] != null && strayPlatforms[i].gameObject != null)
                    SafeDestroy(strayPlatforms[i].gameObject);
            }

            LevelMarker[] strayMarkers = FindObjectsByType<LevelMarker>();
            for (int i = 0; i < strayMarkers.Length; i++)
            {
                if (strayMarkers[i] != null && strayMarkers[i].gameObject != null)
                    SafeDestroy(strayMarkers[i].gameObject);
            }

            // Clean up any redundant root containers named "Platforms" or "LevelObjects" in the scene
            GameObject scenePlatRoot = GameObject.Find("Platforms");
            if (scenePlatRoot != null && scenePlatRoot.transform != platformsRoot)
            {
                for (int i = scenePlatRoot.transform.childCount - 1; i >= 0; i--)
                    SafeDestroy(scenePlatRoot.transform.GetChild(i).gameObject);
                SafeDestroy(scenePlatRoot);
            }

            GameObject sceneObjRoot = GameObject.Find("LevelObjects");
            if (sceneObjRoot != null && sceneObjRoot.transform != levelObjectsRoot)
            {
                for (int i = sceneObjRoot.transform.childCount - 1; i >= 0; i--)
                    SafeDestroy(sceneObjRoot.transform.GetChild(i).gameObject);
                SafeDestroy(sceneObjRoot);
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(go);
                return;
            }
#endif
            Destroy(go);
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
