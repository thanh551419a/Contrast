using UnityEngine;

namespace Contrast.Data
{
    [System.Serializable]
    public class LevelData
    {
        // New level spawn definition.
        public StartPosData startPos;

        // One-shot checkpoint.
        public CheckPointData checkPoint;

        // End-game contact area.
        public EndData end;

        // Legacy compatibility with the previous JSON schema.
        // If startPos is missing, LevelLoader falls back to spawnPoint.
        public SpawnPointData spawnPoint;

        public CameraBoundaryData cameraBoundary;
        public ColorPlatformData[] platforms;
    }

    [System.Serializable]
    public class StartPosData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);
        public Vector2Data size = new Vector2Data(1f, 1f);
        public float color = 0f;
    }

    [System.Serializable]
    public class CheckPointData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);
        public Vector2Data size = new Vector2Data(1f, 1f);
        public float color = 0f;
    }

    [System.Serializable]
    public class EndData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);
        public Vector2Data size = new Vector2Data(1f, 1f);
    }

    [System.Serializable]
    public class SpawnPointData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);

        // Legacy field. The new schema uses "color".
        public float color = 0f;

        // Kept so older files containing initialPlayerColor remain readable.
        // Values are interpreted using the same 0..255 grayscale classifier.
        public float initialPlayerColor = -1f;
    }

    [System.Serializable]
    public class ColorPlatformData
    {
        public Vector2Data position          = new Vector2Data(0f, 0f);
        public Vector2Data size              = new Vector2Data(1f, 1f);
        public float       rawGrayscaleColor = 255f;
    }

    [System.Serializable]
    public class CameraBoundaryData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);
        public Vector2Data size     = new Vector2Data(20f, 10f);
    }

    [System.Serializable]
    public class Vector2Data
    {
        public float x;
        public float y;

        public Vector2Data() { }
        public Vector2Data(float x, float y) { this.x = x; this.y = y; }

        public Vector2 ToVector2() => new Vector2(x, y);
    }
}
