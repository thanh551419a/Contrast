using System;
using System.IO;

namespace Contrast.Data
{
    public enum LevelSourceType
    {
        FilePath,
        InMemoryJson
    }

    /// <summary>
    /// Represents the origin of an active level (either a disk file or in-memory JSON snapshot).
    /// Used by Restart and the loading pipeline to deterministically reload the exact source.
    /// </summary>
    [Serializable]
    public sealed class LevelSource
    {
        public LevelSourceType SourceType { get; private set; }
        public string FilePath { get; private set; }
        public string InMemoryJson { get; private set; }

        public static LevelSource FromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path cannot be null or empty.", nameof(path));

            return new LevelSource
            {
                SourceType = LevelSourceType.FilePath,
                FilePath = path,
                InMemoryJson = null
            };
        }

        public static LevelSource FromMemory(string json)
        {
            return new LevelSource
            {
                SourceType = LevelSourceType.InMemoryJson,
                FilePath = null,
                InMemoryJson = json ?? string.Empty
            };
        }

        public string LoadRawJson()
        {
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
            return SourceType == LevelSourceType.FilePath
                ? $"[FilePath] {FilePath}"
                : "[InMemoryJson]";
        }
    }
}
