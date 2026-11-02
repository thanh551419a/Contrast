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
        public static readonly string WindowsGameDir = Path.Combine(ReleaseRoot, "Windows", "Game");
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
            string exePath = Path.Combine(WindowsGameDir, "Game.exe");
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
            return EvaluateBuildReport(report, "Windows Game", exePath);
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
            Directory.CreateDirectory(WindowsGameDir);
            Directory.CreateDirectory(WindowsEditorDir);
            Directory.CreateDirectory(AndroidDir);
            Directory.CreateDirectory(LevelsDir);
            Directory.CreateDirectory(ReportsDir);
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
