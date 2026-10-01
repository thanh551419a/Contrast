using UnityEngine;

namespace Contrast.Data
{
    [System.Serializable]
    public class LevelData
    {
        public SpawnPointData      spawnPoint;
        public CameraBoundaryData  cameraBoundary;
        public ColorPlatformData[] platforms;
    }

    [System.Serializable]
    public class SpawnPointData
    {
        public Vector2Data position = new Vector2Data(0f, 0f);
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
