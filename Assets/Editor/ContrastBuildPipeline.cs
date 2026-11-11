using System;
using System.IO;
using Contrast.Data;
using Contrast.Level;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Contrast.Build
{
    /// <summary>
    /// Automated native build pipeline for Contrast.
    /// Builds:
    /// 1. Windows Game Runtime: Release/Windows/Game/Game.exe
    /// 2. Windows Standalone Level Editor: Release/Windows/LevelEditor/LevelEditor.exe
    /// 3. Android Game Runtime: Release/Android/Game.apk
    /// </summary>
    public static class ContrastBuildPipeline
    {
        public const string GameScenePath = "Assets/Scenes/SampleScene.unity";
        public const string EditorScenePath = "Assets/Scenes/LevelEditorScene.unity";

        public static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static readonly string ReleaseRoot = Path.Combine(ProjectRoot, "Release");
        public static readonly string WindowsContrastWindowsDir = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_Windows");
        public static readonly string WindowsPortableZipPath = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_Windows_Portable.zip");
        public static readonly string WindowsJumpFixRC2Dir = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_JumpFix_RC2");
        public static readonly string WindowsJumpFixRC2ZipPath = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_JumpFix_RC2.zip");
        public static readonly string WindowsDeathYNewtonXRC3Dir = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_DeathY_NewtonX_RC3");
        public static readonly string WindowsDeathYNewtonXRC3ZipPath = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_DeathY_NewtonX_RC3.zip");
        public static readonly string WindowsAnimationCameraRC4Dir = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_Animation_CameraV2_RC4");
        public static readonly string WindowsAnimationCameraRC4ZipPath = Path.Combine(ReleaseRoot, "Windows", "CONTRAST_Animation_CameraV2_RC4.zip");
        public static readonly string WindowsGameDir = Path.Combine(ReleaseRoot, "Windows", "Game");
        public static readonly string WindowsReleaseWindowDir = Path.Combine(ReleaseRoot, "window");
        public static readonly string WindowsEditorDir = Path.Combine(ReleaseRoot, "Windows", "LevelEditor");
        public static readonly string AndroidDir = Path.Combine(ReleaseRoot, "Android");
        public static readonly string LevelsDir = Path.Combine(ReleaseRoot, "Levels");
        public static readonly string ReportsDir = Path.Combine(ReleaseRoot, "Reports");

        [MenuItem("Contrast/Build/Build All Executables")]
        public static void BuildAll()
        {
            Debug.Log("================================================================================");
            Debug.Log("[BUILD_PIPELINE] STARTING FULL RELEASE BUILD MATRIX");
            Debug.Log("================================================================================");

            EnsureDirectories();
            EnsureLevelEditorScene();
            PopulateReleaseLevels();

            bool gameWinOk = BuildGameWindows();
            bool editorWinOk = BuildLevelEditorWindows();
            bool gameAndroidOk = BuildGameAndroid();

            Debug.Log("================================================================================");
            Debug.Log($"[BUILD_PIPELINE] SUMMARY: GameWindows={gameWinOk}, EditorWindows={editorWinOk}, Android={gameAndroidOk}");
            Debug.Log("================================================================================");
        }

        [MenuItem("Contrast/Build/1. Build Game (Windows x64)")]
        public static bool BuildGameWindows()
        {
            EnsureDirectories();
            PopulateReleaseLevels();
            Contrast.Tests.EditorLevelTests.RunAllEditorTests();

            if (Directory.Exists(WindowsContrastWindowsDir))
            {
                try
                {
                    Directory.Delete(WindowsContrastWindowsDir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning cleaning {WindowsContrastWindowsDir}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(WindowsContrastWindowsDir);

            string exePath = Path.Combine(WindowsContrastWindowsDir, "Game.exe");
            Debug.Log($"[BUILD_PIPELINE] Building Windows Game -> {exePath}");

            PlayerSettings.productName = "Contrast";
            PlayerSettings.companyName = "ContrastTeam";

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            bool success = EvaluateBuildReport(report, "Windows Game", exePath);
            if (success)
            {
                GenerateReadme(WindowsContrastWindowsDir);
                try
                {
                    string destLevels = Path.Combine(WindowsContrastWindowsDir, "Levels");
                    Directory.CreateDirectory(destLevels);
                    CopyDirectoryRecursive(LevelsDir, destLevels);
                    CopyDirectoryRecursive(WindowsContrastWindowsDir, WindowsGameDir);
                    CopyDirectoryRecursive(WindowsContrastWindowsDir, WindowsReleaseWindowDir);
                    Debug.Log($"[BUILD_PIPELINE] Mirrored Windows Game release -> {WindowsGameDir} and {WindowsReleaseWindowDir}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning mirroring to legacy folders: {ex.Message}");
                }

                CreatePortableZip(WindowsContrastWindowsDir, WindowsPortableZipPath);
            }
            return success;
        }

        [MenuItem("Contrast/Build/4. Build JumpFix RC2 (Windows x64)")]
        public static bool BuildGameWindowsJumpFixRC2()
        {
            EnsureDirectories();
            PopulateReleaseLevels();
            Contrast.Tests.EditorLevelTests.RunAllEditorTests();

            if (Directory.Exists(WindowsJumpFixRC2Dir))
            {
                try
                {
                    Directory.Delete(WindowsJumpFixRC2Dir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning cleaning {WindowsJumpFixRC2Dir}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(WindowsJumpFixRC2Dir);

            string exePath = Path.Combine(WindowsJumpFixRC2Dir, "Game.exe");
            Debug.Log($"[BUILD_PIPELINE] Building Windows JumpFix RC2 -> {exePath}");

            PlayerSettings.productName = "Contrast";
            PlayerSettings.companyName = "ContrastTeam";

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            bool success = EvaluateBuildReport(report, "Windows Game JumpFix RC2", exePath);
            if (success)
            {
                GenerateReadme(WindowsJumpFixRC2Dir);
                try
                {
                    string destLevels = Path.Combine(WindowsJumpFixRC2Dir, "Levels");
                    Directory.CreateDirectory(destLevels);
                    CopyDirectoryRecursive(LevelsDir, destLevels);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning copying levels to JumpFix RC2: {ex.Message}");
                }

                CreatePortableZip(WindowsJumpFixRC2Dir, WindowsJumpFixRC2ZipPath);
            }
            return success;
        }

        [MenuItem("Contrast/Build/1c. Build Game DeathY NewtonX RC3 (Windows x64 Isolated)")]
        public static bool BuildGameWindowsDeathYNewtonXRC3()
        {
            EnsureDirectories();
            PopulateReleaseLevels();
            Contrast.Tests.EditorLevelTests.RunAllEditorTests();

            if (Directory.Exists(WindowsDeathYNewtonXRC3Dir))
            {
                try
                {
                    Directory.Delete(WindowsDeathYNewtonXRC3Dir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning cleaning {WindowsDeathYNewtonXRC3Dir}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(WindowsDeathYNewtonXRC3Dir);

            string exePath = Path.Combine(WindowsDeathYNewtonXRC3Dir, "Game.exe");
            Debug.Log($"[BUILD_PIPELINE] Building Windows Game DeathY NewtonX RC3 -> {exePath}");

            PlayerSettings.productName = "Contrast";
            PlayerSettings.companyName = "ContrastTeam";

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            bool success = EvaluateBuildReport(report, "Windows Game DeathY NewtonX RC3", exePath);
            if (success)
            {
                try
                {
                    string destLevels = Path.Combine(WindowsDeathYNewtonXRC3Dir, "Levels");
                    Directory.CreateDirectory(destLevels);
                    CopyDirectoryRecursive(LevelsDir, destLevels);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning copying levels to DeathY NewtonX RC3: {ex.Message}");
                }

                CreatePortableZip(WindowsDeathYNewtonXRC3Dir, WindowsDeathYNewtonXRC3ZipPath);
            }
            return success;
        }

        [MenuItem("Contrast/Build/1d. Build Game Animation Camera RC4 (Windows x64)")]
        public static bool BuildGameWindowsAnimationCameraRC4()
        {
            EnsureDirectories();
            PopulateReleaseLevels();
            Contrast.Editor.SilhouetteMotionImporter.EnsureAssetsNow();

            // Run full Editor verification suite (including RC4 Animation, Checkpoint & Reversible Camera suites) before building
            Contrast.Tests.EditorLevelTests.RunAllEditorTests();

            if (Directory.Exists(WindowsAnimationCameraRC4Dir))
            {
                try
                {
                    Directory.Delete(WindowsAnimationCameraRC4Dir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning cleaning {WindowsAnimationCameraRC4Dir}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(WindowsAnimationCameraRC4Dir);

            string exePath = Path.Combine(WindowsAnimationCameraRC4Dir, "Game.exe");
            Debug.Log($"[BUILD_PIPELINE] Building Windows Game Animation Camera RC4 -> {exePath}");

            PlayerSettings.productName = "Contrast";
            PlayerSettings.companyName = "ContrastTeam";

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            bool success = EvaluateBuildReport(report, "Windows Game Animation Camera RC4", exePath);
            if (success)
            {
                try
                {
                    string destLevels = Path.Combine(WindowsAnimationCameraRC4Dir, "Levels");
                    Directory.CreateDirectory(destLevels);
                    CopyDirectoryRecursive(LevelsDir, destLevels);
                    GenerateReadme(WindowsAnimationCameraRC4Dir);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BUILD_PIPELINE] Warning copying levels to Animation Camera RC4: {ex.Message}");
                }

                CreatePortableZip(WindowsAnimationCameraRC4Dir, WindowsAnimationCameraRC4ZipPath);
            }
            return success;
        }

        [MenuItem("Contrast/Build/2. Build Level Editor (Windows x64)")]
        public static bool BuildLevelEditorWindows()
        {
            EnsureDirectories();
            EnsureLevelEditorScene();

            string exePath = Path.Combine(WindowsEditorDir, "LevelEditor.exe");
            Debug.Log($"[BUILD_PIPELINE] Building Windows Standalone Level Editor -> {exePath}");

            PlayerSettings.productName = "ContrastLevelEditor";
            PlayerSettings.companyName = "ContrastTeam";

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { EditorScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            return EvaluateBuildReport(report, "Windows Standalone Level Editor", exePath);
        }

        [MenuItem("Contrast/Build/3. Build Game (Android APK)")]
        public static bool BuildGameAndroid()
        {
            EnsureDirectories();
            ConfigureAndroidEnvironment();

            string apkPath = Path.Combine(AndroidDir, "Game.apk");
            Debug.Log($"[BUILD_PIPELINE] Building Android Game APK -> {apkPath}");

            PlayerSettings.productName = "Contrast";
            PlayerSettings.companyName = "ContrastTeam";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.Contrast.Game");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(opt);
            return EvaluateBuildReport(report, "Android Game APK", apkPath);
        }

        [MenuItem("Contrast/Build/Ensure Level Editor Scene")]
        public static void EnsureLevelEditorScene()
        {
            if (File.Exists(EditorScenePath))
            {
                Debug.Log($"[BUILD_PIPELINE] Level Editor scene already exists at {EditorScenePath}");
                return;
            }

            Debug.Log($"[BUILD_PIPELINE] Generating standalone Level Editor scene from {GameScenePath}...");

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            // 1. Remove Player GameObject from editor scene
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
            }

            // 2. Adjust GameSystems GameObject into standalone EditorSystems
            GameObject gameSystemsGo = GameObject.Find("GameSystems");
            if (gameSystemsGo != null)
            {
                gameSystemsGo.name = "EditorSystems";

                var gm = gameSystemsGo.GetComponent<Contrast.Core.GameManager>();
                if (gm != null)
                {
                    UnityEngine.Object.DestroyImmediate(gm);
                }

                var bootstrapper = gameSystemsGo.GetComponent<StandaloneLevelEditorBootstrapper>();
                if (bootstrapper == null)
                {
                    bootstrapper = gameSystemsGo.AddComponent<StandaloneLevelEditorBootstrapper>();
                }
            }

            // 3. Save as dedicated standalone LevelEditorScene.unity
            bool saved = EditorSceneManager.SaveScene(scene, EditorScenePath);
            Debug.Log($"[BUILD_PIPELINE] Saved LevelEditorScene: {saved} -> {EditorScenePath}");

            // Register scenes in EditorBuildSettings
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(EditorScenePath, true)
            };

            // Re-open original GameScene
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        public static void PopulateReleaseLevels()
        {
            Directory.CreateDirectory(LevelsDir);

            // Copy official campaign levels L01, L02, L03
            CopyLevelIfFound("Assets/Resources/Levels/L01.json", Path.Combine(LevelsDir, "L01.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L01.json", Path.Combine(LevelsDir, "Level_01.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L02.json", Path.Combine(LevelsDir, "L02.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L02.json", Path.Combine(LevelsDir, "Level_02.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L03.json", Path.Combine(LevelsDir, "L03.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L03.json", Path.Combine(LevelsDir, "Level_03.json"));

            // Also mirror directly adjacent to Game executable for standalone distribution
            string gameLevelsDir = Path.Combine(WindowsGameDir, "Levels");
            Directory.CreateDirectory(gameLevelsDir);
            CopyLevelIfFound("Assets/Resources/Levels/L01.json", Path.Combine(gameLevelsDir, "L01.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L02.json", Path.Combine(gameLevelsDir, "L02.json"));
            CopyLevelIfFound("Assets/Resources/Levels/L03.json", Path.Combine(gameLevelsDir, "L03.json"));

            // Maintain legacy prototypes for backwards compatibility
            CopyLevelIfFound("Assets/Resources/Levels/default_level.json", Path.Combine(LevelsDir, "default_level.json"));
            CopyLevelIfFound("Assets/Resources/Levels/Easy_Level.json", Path.Combine(LevelsDir, "Easy_Level.json"));
            CopyLevelIfFound("Assets/Resources/Levels/map2.json", Path.Combine(LevelsDir, "map2.json"));
            CopyLevelIfFound("Assets/Script/Contrast/Level/SampleLevel.CheckpointEnd.json", Path.Combine(LevelsDir, "SampleLevel.CheckpointEnd.json"));
            CopyLevelIfFound("Assets/Script/Contrast/Level/SampleLevel.Easy.HTMLReconstructed.json", Path.Combine(LevelsDir, "SampleLevel.Easy.HTMLReconstructed.json"));

            Debug.Log($"[BUILD_PIPELINE] Populated Release/Levels directory: {LevelsDir}");
        }

        private static void CopyLevelIfFound(string sourceRelPath, string destPath)
        {
            string fullSource = Path.Combine(ProjectRoot, sourceRelPath);
            if (File.Exists(fullSource))
            {
                File.Copy(fullSource, destPath, true);
            }
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(WindowsContrastWindowsDir);
            Directory.CreateDirectory(WindowsGameDir);
            Directory.CreateDirectory(WindowsReleaseWindowDir);
            Directory.CreateDirectory(WindowsEditorDir);
            Directory.CreateDirectory(AndroidDir);
            Directory.CreateDirectory(LevelsDir);
            Directory.CreateDirectory(ReportsDir);
        }

        public static void GenerateReadme(string targetDir)
        {
            string readmePath = Path.Combine(targetDir, "README.txt");
            string content =
@"================================================================================
                    CONTRAST — PHIÊN BẢN PORTABLE WINDOWS
             Optical Interference Puzzle Game · Unity Standalone
================================================================================

1. CÁCH GIẢI NÉN VÀ KHỞI ĐỘNG (HOW TO EXTRACT & LAUNCH)
--------------------------------------------------------------------------------
- Giải nén toàn bộ tệp ""CONTRAST_Windows_Portable.zip"" vào bất kỳ thư mục nào
  có quyền ghi (ví dụ: Desktop, D:\Games\Contrast, v.v.).
- Mở thư mục vừa giải nén ""CONTRAST_Windows"".
- Nhấp đúp vào tệp ""Game.exe"" để khởi chạy trò chơi.
- Game hoàn toàn Portable: KHÔNG yêu cầu cài đặt Unity, Unity Hub hay .NET SDK.
- Game KHÔNG cần kết nối Internet và KHÔNG cần tải thêm bất kỳ file màn chơi nào.

2. BẮT ĐẦU CHIẾN DỊCH (HOW TO START CAMPAIGN)
--------------------------------------------------------------------------------
- Tại Menu chính (Main Menu), nhấp chọn thẻ ""CHƠI THỬ"" hoặc nhấn phím [1] trên bàn phím.
- Game sẽ ngay lập tức tải và bắt đầu Màn 1 (L01 — Cánh cửa không phải vật cản).
- Tất cả 3 màn chơi (L01, L02, L03) đã được nhúng sẵn trong bộ cài đặt của trò chơi.

3. TIẾN TRÌNH CHIẾN DỊCH TỰ ĐỘNG (CAMPAIGN PROGRESSION)
--------------------------------------------------------------------------------
- Hoàn thành L01 -> Tự động chuyển tiếp vào L02 (Cầu thang ma).
- Hoàn thành L02 -> Tự động chuyển tiếp vào L03 (Giếng ba tầng).
- Hoàn thành L03 (đo đạc thành công vân giao thoa Newton K-I-K') -> Màn hình Chúc mừng Hoàn thành Chiến dịch (Campaign Complete).
- Tại màn hình kết thúc, bạn có thể chọn ""PLAY AGAIN"" (Chơi lại từ đầu) hoặc ""MAIN MENU"".

4. HỆ THỐNG PHÍM ĐIỀU KHIỂN (CONTROLS)
--------------------------------------------------------------------------------
[Di chuyển & Nhảy]
- A / D hoặc Mũi tên Trái / Phải : Di chuyển sang trái / phải
- W / Space / Mũi tên Lên       : Nhảy (Jump)
- S / Mũi tên Xuống              : Rơi nhanh (Fast Fall)

[Điều chỉnh Pha Màu & Tương tác]
- J / K hoặc Lăn chuột           : Dịch pha liên tục (White <-> Gray <-> Black)
- Phím 1, 2, 3                   : Chuyển đổi nhanh trực tiếp sang màu White / Gray / Black
- X                              : Đánh dấu điểm đo giao thoa Newton (tại phòng quang học)

[Menu & Hệ thống]
- ESC                            : Mở Menu tạm dừng (In-Game Menu) / Quay lại Menu chính
- R                              : Tái khởi động màn chơi (Restart)
- T                              : Chế độ thử nghiệm tự do (T-Mode Test Flight)

5. TẢI FILE NGOÀI & THIẾT KẾ MÀN CHƠI (LOAD LEVEL & EDIT LEVEL)
--------------------------------------------------------------------------------
- Thẻ 2 ""LOAD LEVEL"" (Phím 2): Tải các file JSON tùy chỉnh được lưu trong thư mục người dùng.
- Thẻ 3 ""EDIT LEVEL"" (Phím 3): Mở trình thiết kế màn chơi trực quan và xuất file JSON.
================================================================================
";
            File.WriteAllText(readmePath, content, System.Text.Encoding.UTF8);
            Debug.Log($"[BUILD_PIPELINE] Created README.txt at {readmePath}");
        }

        public static void CreatePortableZip(string sourceDir, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { }
            }

            try
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                long zipSize = new FileInfo(zipPath).Length;
                Debug.Log($"[BUILD_PIPELINE] Successfully packaged portable ZIP: {zipPath} ({zipSize / (1024 * 1024.0):F2} MB)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BUILD_PIPELINE] Failed to create ZIP via ZipFile: {ex.Message}");
            }
        }

        public static void BuildPortableReleaseBatch()
        {
            bool ok = BuildGameWindows();
            if (ok && File.Exists(WindowsPortableZipPath))
            {
                Debug.Log("[BUILD_PIPELINE] Batch portable release build PASSED.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[BUILD_PIPELINE] Batch portable release build FAILED.");
                EditorApplication.Exit(1);
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }

        private static void ConfigureAndroidEnvironment()
        {
            string unityEditorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            string playbackEngines = Path.Combine(unityEditorDir, "Data", "PlaybackEngines", "AndroidPlayer");

            string sdkPath = Path.Combine(playbackEngines, "SDK");
            string ndkPath = Path.Combine(playbackEngines, "NDK");
            string jdkPath = Path.Combine(playbackEngines, "OpenJDK");

            if (Directory.Exists(sdkPath))
            {
                EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
                EditorPrefs.SetBool("SdkUseEmbedded", true);
            }
            if (Directory.Exists(ndkPath))
            {
                EditorPrefs.SetString("AndroidNdkRootR23b", ndkPath);
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                EditorPrefs.SetBool("NdkUseEmbedded", true);
            }
            if (Directory.Exists(jdkPath))
            {
                EditorPrefs.SetString("JdkPath", jdkPath);
                EditorPrefs.SetBool("JdkUseEmbedded", true);
            }

            Debug.Log($"[BUILD_PIPELINE] Configured Android SDK: {sdkPath}, NDK: {ndkPath}, JDK: {jdkPath}");
        }

        private static bool EvaluateBuildReport(BuildReport report, string targetName, string expectedArtifact)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                long size = File.Exists(expectedArtifact) ? new FileInfo(expectedArtifact).Length : 0;
                Debug.Log($"[BUILD_PIPELINE][SUCCESS] {targetName} built successfully!");
                Debug.Log($"[BUILD_PIPELINE][ARTIFACT] Path: {expectedArtifact} | Size: {size / (1024 * 1024.0):F2} MB");
                Debug.Log($"[BUILD_PIPELINE][STATS] Total Time: {report.summary.totalTime.TotalSeconds:F2}s | Warnings: {report.summary.totalWarnings} | Errors: {report.summary.totalErrors}");
                return true;
            }
            else
            {
                Debug.LogError($"[BUILD_PIPELINE][FAIL] {targetName} build FAILED with result {report.summary.result}!");
                foreach (BuildStep step in report.steps)
                {
                    foreach (BuildStepMessage msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError($"[BUILD_PIPELINE][ERROR] [{step.name}] {msg.content}");
                        }
                    }
                }
                return false;
            }
        }
    }
}
