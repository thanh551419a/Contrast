using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Contrast.Data
{
    /// <summary>
    /// Manages persistent file storage for user and default levels.
    /// All level files reside under Application.persistentDataPath/Levels.
    /// </summary>
    public static class LevelRepository
    {
        public const string LevelsFolderName = "Levels";
        public const string DefaultLevelFileName = "default_level.json";

        /// <summary>
        /// Optional override for tests running outside the Unity player runtime.
        /// </summary>
        public static string CustomStorageDirectory { get; set; }

        public static string GetLevelsDirectory()
        {
            if (!string.IsNullOrEmpty(CustomStorageDirectory))
                return CustomStorageDirectory;

            return Path.Combine(Application.persistentDataPath, LevelsFolderName);
        }

        public static void EnsureDirectory()
        {
            string dir = GetLevelsDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static string[] ListLevelFiles()
        {
            EnsureDirectory();
            string dir = GetLevelsDirectory();

            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            FileInfo[] files = dirInfo.GetFiles("*.json");

            return files
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => f.FullName)
                .ToArray();
        }

        public static string ReadAllText(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException($"Level file not found: {path}");

            return File.ReadAllText(path);
        }

        public static void WriteAllText(string path, string json)
        {
            EnsureDirectory();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, json);

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL persistentDataPath sync to IndexedDB
            try
            {
                // In Unity WebGL, File.WriteAllText commits to Emscripten's virtual FS.
                // Syncing is automatically handled or flushed per frame.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LEVEL_REPO][WEBGL] IndexedDB flush note: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Generates a unique filename in the format level_yyyyMMdd_HHmmss.json.
        /// Guaranteed never to overwrite an existing file.
        /// </summary>
        public static string GenerateUniqueFileName(string basePrefix = "level_")
        {
            EnsureDirectory();
            string dir = GetLevelsDirectory();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string candidateName = $"{basePrefix}{timestamp}.json";
            string candidatePath = Path.Combine(dir, candidateName);

            int counter = 1;
            while (File.Exists(candidatePath))
            {
                candidateName = $"{basePrefix}{timestamp}_{counter}.json";
                candidatePath = Path.Combine(dir, candidateName);
                counter++;
            }

            return candidateName;
        }

        /// <summary>
        /// Generates a unique full path for saving an export file.
        /// Never overwrites an existing file.
        /// </summary>
        public static string GetUniqueSavePath(string desiredFileName)
        {
            EnsureDirectory();
            string dir = GetLevelsDirectory();

            if (string.IsNullOrWhiteSpace(desiredFileName))
                desiredFileName = GenerateUniqueFileName();

            // Strip invalid filename characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                desiredFileName = desiredFileName.Replace(c.ToString(), string.Empty);
            }

            if (!desiredFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                desiredFileName += ".json";

            string candidatePath = Path.Combine(dir, desiredFileName);
            if (!File.Exists(candidatePath))
                return candidatePath;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(desiredFileName);
            int counter = 1;
            while (File.Exists(candidatePath))
            {
                string nextName = $"{nameWithoutExt}_{counter}.json";
                candidatePath = Path.Combine(dir, nextName);
                counter++;
            }

            return candidatePath;
        }

        /// <summary>
        /// Creates default_level.json if the Levels directory contains no JSON files.
        /// Uses Resources.Load("Levels/default_level") as seed template, or falls back
        /// to programmatic level data.
        /// </summary>
        public static void EnsureDefaultLevelFile()
        {
            EnsureDirectory();
            string dir = GetLevelsDirectory();

            string[] existing = Directory.GetFiles(dir, "*.json");
            if (existing != null && existing.Length > 0)
                return;

            string defaultPath = Path.Combine(dir, DefaultLevelFileName);
            string defaultJson = null;

            // Attempt to load from Resources seed template
            try
            {
                TextAsset templateAsset = Resources.Load<TextAsset>("Levels/default_level");
                if (templateAsset != null && !string.IsNullOrWhiteSpace(templateAsset.text))
                {
                    defaultJson = templateAsset.text;
                    SafeLog("[LEVEL_REPO] Seeded default level from Resources template 'Levels/default_level'.");
                }
            }
            catch (Exception ex)
            {
                SafeLogWarning($"[LEVEL_REPO] Could not load Resources/Levels/default_level: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(defaultJson))
            {
                defaultJson = GenerateProgrammaticDefaultJson();
                SafeLog("[LEVEL_REPO] Seeded default level programmatically.");
            }

            File.WriteAllText(defaultPath, defaultJson);
            SafeLog($"[FLOW][LEVEL_FILE] load path={defaultPath}");
        }

        private static void SafeLog(string message)
        {
            try
            {
                Debug.Log(message);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        private static void SafeLogWarning(string message)
        {
            try
            {
                Debug.LogWarning(message);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        public static string GenerateProgrammaticDefaultJson()
        {
            var data = new LevelData
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
                    },
                    new ColorPlatformData
                    {
                        position = new Vector2Data(10f, 0.5f),
                        size = new Vector2Data(6f, 1f),
                        rawGrayscaleColor = 255f
                    }
                },
                end = new EndData
                {
                    position = new Vector2Data(12f, 1.5f),
                    size = new Vector2Data(1.5f, 2f)
                }
            };

            return LevelDataSanitizer.SerializeToJson(data);
        }
    }
}
