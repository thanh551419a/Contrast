using System;

namespace Contrast.Core
{
    /// <summary>
    /// Global Singleton configuration containing the master <see cref="enableTest"/> flag.
    /// When <c>enableTest == true</c>, test/debug shortcuts (such as T free-move/no-clip,
    /// 1/2/3 direct color change, Up/Down step color chunk, and 1/2/3 menu quick-select)
    /// work normally. When <c>enableTest == false</c>, all test shortcuts are disabled.
    /// </summary>
    [Serializable]
    public sealed class Config
    {
        private static Config instance;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Config();
                }
                return instance;
            }
        }

        /// <summary>
        /// Master flag to enable or disable all test/debug shortcuts across the game.
        /// Set to <c>true</c> for testing/debugging; set to <c>false</c> to disable all test keys.
        /// </summary>
        public bool enableTest = true;

        /// <summary>
        /// Session-only completion flags for the 3 campaign levels (L01=0, L02=1, L03=2).
        /// A level is marked completed only after the player finishes the Newton measurement
        /// AND answers the wavelength color question correctly.
        /// Resets when the application restarts.
        /// </summary>
        private readonly bool[] completedLevels = new bool[3];

        public bool IsLevelCompleted(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= completedLevels.Length)
                return false;
            return completedLevels[levelIndex];
        }

        public void SetLevelCompleted(int levelIndex, bool completed = true)
        {
            if (levelIndex < 0 || levelIndex >= completedLevels.Length)
                return;
            completedLevels[levelIndex] = completed;
        }

        public void MarkLevelCompletedByWavelength(float wavelengthNm)
        {
            int idx = GetLevelIndexForWavelength(wavelengthNm);
            if (idx >= 0)
                SetLevelCompleted(idx, true);
        }

        public static int GetLevelIndexForWavelength(float wavelengthNm)
        {
            if (Math.Abs(wavelengthNm - 550f) < 30f) return 0; // L01: 550 nm (Lục)
            if (Math.Abs(wavelengthNm - 650f) < 30f) return 1; // L02: 650 nm (Đỏ)
            if (Math.Abs(wavelengthNm - 470f) < 30f) return 2; // L03: 470 nm (Lam)
            return -1;
        }

        public void ResetCompletedLevels()
        {
            for (int i = 0; i < completedLevels.Length; i++)
                completedLevels[i] = false;
        }
    }
}
