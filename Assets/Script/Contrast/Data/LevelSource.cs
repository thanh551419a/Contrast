using System;
using System.IO;

namespace Contrast.Data
{
    public enum LevelSourceType
    {
        FilePath,
        InMemoryJson,
        PackagedResource
    }

    /// <summary>
    /// Represents the origin of an active level (disk file, in-memory JSON snapshot, or built-in packaged resource).
    /// Used by Restart and the loading pipeline to deterministically reload the exact source.
    /// </summary>
    [Serializable]
    public sealed class LevelSource
    {
        public LevelSourceType SourceType { get; private set; }
        public string FilePath { get; private set; }
        public string ResourcePath { get; private set; }
        public string InMemoryJson { get; private set; }

        public static LevelSource FromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path cannot be null or empty.", nameof(path));

            return new LevelSource
            {
                SourceType = LevelSourceType.FilePath,
                FilePath = path,
                ResourcePath = null,
                InMemoryJson = null
            };
        }

        public static LevelSource FromResource(string resourcePath, string levelName = null)
        {
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentException("Resource path cannot be null or empty.", nameof(resourcePath));

            return new LevelSource
            {
                SourceType = LevelSourceType.PackagedResource,
                ResourcePath = resourcePath,
                FilePath = levelName ?? Path.GetFileName(resourcePath),
                InMemoryJson = null
            };
        }

        public static LevelSource FromMemory(string json)
        {
            return new LevelSource
            {
                SourceType = LevelSourceType.InMemoryJson,
                FilePath = null,
                ResourcePath = null,
                InMemoryJson = json ?? string.Empty
            };
        }

        public string LoadRawJson()
        {
            if (SourceType == LevelSourceType.PackagedResource)
            {
                string cleanPath = ResourcePath;
                if (!string.IsNullOrEmpty(cleanPath) && cleanPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanPath = cleanPath.Substring(0, cleanPath.Length - 5);
                }
                UnityEngine.TextAsset asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(cleanPath);
                if (asset == null)
                    throw new FileNotFoundException($"Packaged level resource not found: {ResourcePath} (attempted: {cleanPath})");

                return asset.text;
            }

            if (SourceType == LevelSourceType.FilePath)
            {
                if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                    throw new FileNotFoundException($"Level file not found: {FilePath}");

                return File.ReadAllText(FilePath);
            }

            return InMemoryJson ?? string.Empty;
        }

        public override string ToString()
        {
            return SourceType switch
            {
                LevelSourceType.PackagedResource => $"[PackagedResource] {ResourcePath}",
                LevelSourceType.FilePath => $"[FilePath] {FilePath}",
                _ => "[InMemoryJson]"
            };
        }
    }
}
