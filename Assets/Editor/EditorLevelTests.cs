using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Color;
using Contrast.Data;
using Contrast.Level;
using Contrast.Player;
using UnityEditor;
using UnityEngine;

namespace Contrast.Tests
{
    /// <summary>
    /// Automated test runner executed directly inside the real Unity Editor environment.
    /// Tests trajectory mathematics, lifecycle, persistence, deletion, and rider carry.
    /// Logs detailed PASS / FAIL results directly to Unity Console and Logs/Editor.log.
    /// </summary>
    public static class EditorLevelTests
    {
        private static int passedCount = 0;
        private static int failedCount = 0;
        private static readonly List<string> testFailures = new List<string>();

        [InitializeOnLoadMethod]
        public static void RunAllEditorTests()
        {
            passedCount = 0;
            failedCount = 0;
            testFailures.Clear();

            Debug.Log("================================================================================");
            Debug.Log("[TEST_SUITE] STARTING CONTRAST LEVEL EDITOR & TRAJECTORY COMPREHENSIVE AUDIT");
            Debug.Log($"[TEST_SUITE] Unity Version: {Application.unityVersion} | Platform: {Application.platform}");
            Debug.Log("================================================================================");

            Test_LineEvaluation_Forward();
            Test_LineEvaluation_Reverse();
            Test_LineEvaluation_Rotations();
            Test_Line_SpatialBounds_Clipping();
            Test_CircleEvaluation_Full360();
            Test_ArcEvaluation_180PingPong();
            Test_Trajectory_ValidationRules();
            Test_Trajectory_Projection();
            Test_Trajectory_RuntimeSimulation_PingPong();
            Test_Trajectory_RuntimeSimulation_Loop();
            Test_Trajectory_RuntimeSimulation_Once();
            Test_Platform_Creation_And_Deletion_Registry();
            Test_RiderCarry_Displacement_Calculation();
            Test_LevelPersistence_RoundTrip();
            Test_LegacyJson_BackwardCompatibility();
            Test_TrajectoryShowcaseLevelFile();

            Debug.Log("================================================================================");
            Debug.Log($"[TEST_SUITE] COMPLETED: {passedCount} PASSED, {failedCount} FAILED, 0 BLOCKED");
            if (failedCount > 0)
            {
                Debug.LogError($"[TEST_SUITE] FAILURES DETECTED ({failedCount}):\n" + string.Join("\n", testFailures));
            }
            else
            {
                Debug.Log("[TEST_SUITE] ALL AUDIT AND ACCEPTANCE TESTS PASSED SUCCESSFULLY!");
            }
            Debug.Log("================================================================================");
        }

        private static void AssertTrue(bool condition, string testName, string details)
        {
            if (condition)
            {
                passedCount++;
                Debug.Log($"[TEST][PASS] {testName} - {details}");
            }
            else
            {
                failedCount++;
                string err = $"[TEST][FAIL] {testName} - {details}";
                testFailures.Add(err);
                Debug.LogError(err);
            }
        }

        private static void AssertApproximately(float expected, float actual, float tolerance, string testName, string details)
        {
            float diff = Mathf.Abs(expected - actual);
            if (diff <= tolerance)
            {
                passedCount++;
                Debug.Log($"[TEST][PASS] {testName} - {details} (Expected: {expected:F3}, Actual: {actual:F3}, Diff: {diff:E2})");
            }
            else
            {
                failedCount++;
                string err = $"[TEST][FAIL] {testName} - {details} (Expected: {expected:F3}, Actual: {actual:F3}, Diff: {diff:E2} > Tol: {tolerance:E2})";
                testFailures.Add(err);
                Debug.LogError(err);
            }
        }

        private static void AssertVectorApproximately(Vector2 expected, Vector2 actual, float tolerance, string testName, string details)
        {
            float dist = Vector2.Distance(expected, actual);
            if (dist <= tolerance)
            {
                passedCount++;
                Debug.Log($"[TEST][PASS] {testName} - {details} (Expected: {expected}, Actual: {actual}, Dist: {dist:E2})");
            }
            else
            {
                failedCount++;
                string err = $"[TEST][FAIL] {testName} - {details} (Expected: {expected}, Actual: {actual}, Dist: {dist:E2} > Tol: {tolerance:E2})";
                testFailures.Add(err);
                Debug.LogError(err);
            }
        }

        // ── 1. Line Evaluation (Forward) ──────────────────────────
        private static void Test_LineEvaluation_Forward()
        {
            TrajectoryData cfg = TrajectoryData.CreateDefault(new Vector2(2f, 3f));
            cfg.enabled = true;
            cfg.scaleX = 6f;
            cfg.rotation = 0f;
            cfg.paramMin = 0f;
            cfg.paramMax = 1f;

            Vector2 p0 = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 pMid = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 p1 = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVectorApproximately(new Vector2(2f, 3f), p0, 1e-4f, "LineForward_Start", "Origin at phase 0");
            AssertVectorApproximately(new Vector2(5f, 3f), pMid, 1e-4f, "LineForward_Mid", "Midpoint at phase 0.5");
            AssertVectorApproximately(new Vector2(8f, 3f), p1, 1e-4f, "LineForward_End", "End at phase 1.0");
        }

        // ── 2. Line Evaluation (Reverse) ──────────────────────────
        private static void Test_LineEvaluation_Reverse()
        {
            TrajectoryData cfg = TrajectoryData.CreateDefault(new Vector2(2f, 3f));
            cfg.enabled = true;
            cfg.scaleX = 6f;
            cfg.rotation = 0f;
            cfg.reverseDirection = true;

            Vector2 initialPos = Trajectory.GetInitialPosition(cfg);
            AssertVectorApproximately(new Vector2(8f, 3f), initialPos, 1e-4f, "LineReverse_InitialPos", "Initial point when reverse=true is end (8, 3)");
        }

        // ── 3. Line Rotations ──────────────────────────────────────
        private static void Test_LineEvaluation_Rotations()
        {
            TrajectoryData cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.scaleX = 10f;

            // 90 degrees vertical
            cfg.rotation = 90f;
            Vector2 pVert = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVectorApproximately(new Vector2(0f, 10f), pVert, 1e-3f, "LineRotation_90deg", "Vertical line endpoint");

            // 180 degrees left
            cfg.rotation = 180f;
            Vector2 pLeft = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVectorApproximately(new Vector2(-10f, 0f), pLeft, 1e-3f, "LineRotation_180deg", "Horizontal left endpoint");

            // 45 degrees diagonal
            cfg.rotation = 45f;
            Vector2 pDiag = Trajectory.EvaluateWorldPoint(cfg, 1f);
            float expectedComp = 10f * Mathf.Cos(45f * Mathf.Deg2Rad);
            AssertVectorApproximately(new Vector2(expectedComp, expectedComp), pDiag, 1e-3f, "LineRotation_45deg", "Diagonal 45 deg endpoint");
        }

        // ── 4. Spatial Bounds Clipping ─────────────────────────────
        private static void Test_Line_SpatialBounds_Clipping()
        {
            TrajectoryData cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.SpatialBounds,
                originX = 0f,
                originY = 0f,
                rotation = 0f, // horizontal line along Y=0
                scaleX = 1f,
                boundsXMin = -5f,
                boundsXMax = 5f,
                boundsYMin = -3f,
                boundsYMax = 3f
            };

            Trajectory.ClipLineAgainstBounds(cfg, out float pMin, out float pMax);
            AssertApproximately(-5f, pMin, 1e-4f, "SpatialBounds_pMin", "Clipping left border at -5");
            AssertApproximately(5f, pMax, 1e-4f, "SpatialBounds_pMax", "Clipping right border at +5");

            Vector2 pStart = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 pEnd = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVectorApproximately(new Vector2(-5f, 0f), pStart, 1e-4f, "SpatialBounds_StartPoint", "Clipped start at (-5, 0)");
            AssertVectorApproximately(new Vector2(5f, 0f), pEnd, 1e-4f, "SpatialBounds_EndPoint", "Clipped end at (5, 0)");
        }

        // ── 5. Circle Evaluation (Full 360) ────────────────────────
        private static void Test_CircleEvaluation_Full360()
        {
            TrajectoryData cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 10f,
                originY = 5f,
                radius = 4f,
                paramMin = 0f,
                paramMax = 360f,
                duration = 4f
            };

            Vector2 p0 = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 p90 = Trajectory.EvaluateWorldPoint(cfg, 0.25f);
            Vector2 p180 = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 p270 = Trajectory.EvaluateWorldPoint(cfg, 0.75f);
            Vector2 p360 = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVectorApproximately(new Vector2(14f, 5f), p0, 1e-4f, "Circle_0deg", "Start at (14, 5)");
            AssertVectorApproximately(new Vector2(10f, 9f), p90, 1e-4f, "Circle_90deg", "Top at (10, 9)");
            AssertVectorApproximately(new Vector2(6f, 5f), p180, 1e-4f, "Circle_180deg", "Left at (6, 5)");
            AssertVectorApproximately(new Vector2(10f, 1f), p270, 1e-4f, "Circle_270deg", "Bottom at (10, 1)");
            AssertVectorApproximately(new Vector2(14f, 5f), p360, 1e-4f, "Circle_360deg", "Closed loop: phase 1.0 equals phase 0.0");
        }

        // ── 6. Arc Evaluation (180 PingPong) ───────────────────────
        private static void Test_ArcEvaluation_180PingPong()
        {
            TrajectoryData cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.PingPong,
                originX = 0f,
                originY = 0f,
                radius = 3f,
                paramMin = 0f,
                paramMax = 180f,
                duration = 4f
            };

            Vector2 start = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 peak = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 end = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVectorApproximately(new Vector2(3f, 0f), start, 1e-4f, "Arc_Start", "Right edge (3, 0)");
            AssertVectorApproximately(new Vector2(0f, 3f), peak, 1e-4f, "Arc_Peak", "Crest at (0, 3)");
            AssertVectorApproximately(new Vector2(-3f, 0f), end, 1e-4f, "Arc_End", "Left edge (-3, 0)");
        }

        // ── 7. Trajectory Validation Rules ─────────────────────────
        private static void Test_Trajectory_ValidationRules()
        {
            TrajectoryData cfg = new TrajectoryData { enabled = false };
            AssertTrue(Trajectory.Validate(cfg) == null, "Validation_Disabled", "Disabled trajectory is valid (stationary)");

            cfg.enabled = true;
            cfg.duration = -1f;
            AssertTrue(Trajectory.Validate(cfg) != null, "Validation_NegativeDuration", "Negative duration rejected");

            cfg.duration = 4f;
            cfg.curveType = TrajectoryCurveType.Line;
            cfg.scaleX = 0f;
            AssertTrue(Trajectory.Validate(cfg) != null, "Validation_ZeroLineScale", "Zero line scale rejected");

            cfg.scaleX = 5f;
            cfg.paramMin = 2f;
            cfg.paramMax = 2f;
            AssertTrue(Trajectory.Validate(cfg) != null, "Validation_EqualMinMax", "Equal min/max parameters rejected");

            cfg.paramMax = 5f;
            AssertTrue(Trajectory.Validate(cfg) == null, "Validation_ValidLine", "Correct line configuration accepted");

            cfg.curveType = TrajectoryCurveType.Circle;
            cfg.radius = -2f;
            AssertTrue(Trajectory.Validate(cfg) != null, "Validation_NegativeRadius", "Negative radius rejected");

            cfg.radius = 3f;
            AssertTrue(Trajectory.Validate(cfg) == null, "Validation_ValidCircle", "Correct circle configuration accepted");
        }

        // ── 8. Trajectory Projection ───────────────────────────────
        private static void Test_Trajectory_Projection()
        {
            TrajectoryData lineCfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            lineCfg.enabled = true;
            lineCfg.scaleX = 10f;
            lineCfg.rotation = 0f;

            // Point at (4, 5) projected onto line Y=0 should land at (4, 0) with t=0.4
            Vector2 onLine = Trajectory.ProjectPointOntoTrajectory(lineCfg, new Vector2(4f, 5f), out float tLine);
            AssertVectorApproximately(new Vector2(4f, 0f), onLine, 1e-4f, "Project_LinePoint", "Projected point on line");
            AssertApproximately(0.4f, tLine, 1e-4f, "Project_LinePhase", "Projected phase u=0.4");

            // Point outside circle projected onto circle circumference
            TrajectoryData circCfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                originX = 0f,
                originY = 0f,
                radius = 5f,
                paramMin = 0f,
                paramMax = 360f
            };
            Vector2 onCirc = Trajectory.ProjectPointOntoTrajectory(circCfg, new Vector2(10f, 0f), out float tCirc);
            AssertVectorApproximately(new Vector2(5f, 0f), onCirc, 1e-3f, "Project_CirclePoint", "Point outside circle projected to boundary");
            AssertApproximately(0f, tCirc, 1e-2f, "Project_CirclePhase", "Phase angle 0 deg");
        }

        // ── 9. Runtime Simulation: PingPong ────────────────────────
        private static void Test_Trajectory_RuntimeSimulation_PingPong()
        {
            GameObject go = new GameObject("TestPingPongPlatform");
            Trajectory traj = go.AddComponent<Trajectory>();

            TrajectoryData cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.playbackMode = TrajectoryPlaybackMode.PingPong;
            cfg.scaleX = 10f;
            cfg.duration = 4f; // PingPong out-and-back = 4s. One way = 2s. Speed = 5 u/s.
            traj.SetConfig(cfg);

            // Frame 0: snap to start (0, 0)
            AssertVectorApproximately(new Vector2(0f, 0f), go.transform.position, 1e-4f, "Sim_PingPong_Start", "Phase 0 at start");

            // Simulate 1.0s (halfway out)
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(5f, 0f), go.transform.position, 1e-3f, "Sim_PingPong_1s", "Pos at 1.0s (midway)");

            // Simulate another 1.0s (total 2.0s -> reaches end 10, 0)
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(10f, 0f), go.transform.position, 1e-3f, "Sim_PingPong_2s", "Pos at 2.0s (far endpoint)");

            // Simulate another 1.0s (total 3.0s -> moving back, pos 5, 0)
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(5f, 0f), go.transform.position, 1e-3f, "Sim_PingPong_3s", "Pos at 3.0s (returning)");

            // Simulate another 1.0s (total 4.0s -> returns to 0, 0)
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(0f, 0f), go.transform.position, 1e-3f, "Sim_PingPong_4s", "Pos at 4.0s (cycle completed)");

            UnityEngine.Object.DestroyImmediate(go);
        }

        // ── 10. Runtime Simulation: Loop ───────────────────────────
        private static void Test_Trajectory_RuntimeSimulation_Loop()
        {
            GameObject go = new GameObject("TestLoopPlatform");
            Trajectory traj = go.AddComponent<Trajectory>();

            TrajectoryData cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 0f,
                originY = 0f,
                radius = 2f,
                paramMin = 0f,
                paramMax = 360f,
                duration = 2f // 1 revolution every 2s
            };
            traj.SetConfig(cfg);

            // At t=0: (2, 0)
            AssertVectorApproximately(new Vector2(2f, 0f), go.transform.position, 1e-4f, "Sim_Loop_Start", "Circle Loop start at (2, 0)");

            // At t=0.5s (quarter turn 90 deg): (0, 2)
            traj.ProcessUpdate(0.5f);
            AssertVectorApproximately(new Vector2(0f, 2f), go.transform.position, 1e-3f, "Sim_Loop_Quarter", "Circle Loop at 0.5s is (0, 2)");

            // At t=1.0s (half turn 180 deg): (-2, 0)
            traj.ProcessUpdate(0.5f);
            AssertVectorApproximately(new Vector2(-2f, 0f), go.transform.position, 1e-3f, "Sim_Loop_Half", "Circle Loop at 1.0s is (-2, 0)");

            // At t=2.0s (full cycle): returns to (2, 0)
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(2f, 0f), go.transform.position, 1e-3f, "Sim_Loop_Cycle", "Circle Loop at 2.0s returned to (2, 0)");

            UnityEngine.Object.DestroyImmediate(go);
        }

        // ── 11. Runtime Simulation: Once ───────────────────────────
        private static void Test_Trajectory_RuntimeSimulation_Once()
        {
            GameObject go = new GameObject("TestOncePlatform");
            Trajectory traj = go.AddComponent<Trajectory>();

            TrajectoryData cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.playbackMode = TrajectoryPlaybackMode.Once;
            cfg.scaleX = 8f;
            cfg.duration = 2f;
            traj.SetConfig(cfg);

            traj.ProcessUpdate(2.0f);
            AssertVectorApproximately(new Vector2(8f, 0f), go.transform.position, 1e-3f, "Sim_Once_ReachedEnd", "Once reached end");

            // Further updates should remain clamped at the end
            traj.ProcessUpdate(1.0f);
            AssertVectorApproximately(new Vector2(8f, 0f), go.transform.position, 1e-3f, "Sim_Once_Clamped", "Once clamped at end after duration elapsed");

            UnityEngine.Object.DestroyImmediate(go);
        }

        // ── 12. Platform Creation, Selection & Registry Deletion ───
        private static void Test_Platform_Creation_And_Deletion_Registry()
        {
            int initialCount = ColorPlatform.All.Count;

            GameObject pGo = new GameObject("TestAuditedPlatform");
            ColorPlatform platform = pGo.AddComponent<ColorPlatform>();
            platform.Initialize(new Vector2(5f, 2f), new Vector2(4f, 1f), 255f);

            AssertTrue(ColorPlatform.All.Contains(platform), "Registry_Add", "Created platform registered in ColorPlatform.All");
            AssertTrue(ColorPlatform.All.Count == initialCount + 1, "Registry_CountIncrement", "Registry count incremented");

            // Delete object
            UnityEngine.Object.DestroyImmediate(pGo);
            AssertTrue(!ColorPlatform.All.Contains(platform), "Registry_RemoveOnDestroy", "Destroyed platform removed from ColorPlatform.All");
            AssertTrue(ColorPlatform.All.Count == initialCount, "Registry_CountRestored", "Registry count returned to original");
        }

        // ── 13. Rider Carry Displacement ───────────────────────────
        private static void Test_RiderCarry_Displacement_Calculation()
        {
            GameObject pGo = new GameObject("RiderTestPlatform");
            ColorPlatform platform = pGo.AddComponent<ColorPlatform>();
            platform.Initialize(new Vector2(0f, 0f), new Vector2(4f, 1f), 255f);

            TrajectoryData trajCfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            trajCfg.enabled = true;
            trajCfg.scaleX = 10f;
            trajCfg.duration = 4f;
            platform.InitializeTrajectory(trajCfg);

            // Simulate 1 frame of movement (deltaTime = 0.02s -> phaseStep = 0.01 -> disp = (0.1, 0))
            platform.GetTrajectory().ProcessUpdate(0.02f);
            Vector2 expectedDisp = platform.GetTrajectory().FrameDisplacement;

            // Player resting on top of platform (platform yMax = 0.5, player size = 1x1, player center = (0, 1.0))
            Vector2 playerCenter = new Vector2(0f, 1.0f);
            Vector2 playerSize = new Vector2(1f, 1f);

            Vector2 riderDisp = TrajectorySystem.GetRiderDisplacement(playerCenter, playerSize, LogicalColor.White);
            AssertVectorApproximately(expectedDisp, riderDisp, 1e-4f, "RiderCarry_Detected", "Grounded player inherits moving platform displacement");

            // Player floating in the air (y = 5.0) should receive zero displacement
            Vector2 airborneDisp = TrajectorySystem.GetRiderDisplacement(new Vector2(0f, 5f), playerSize, LogicalColor.White);
            AssertVectorApproximately(Vector2.zero, airborneDisp, 1e-4f, "RiderCarry_AirborneIgnored", "Airborne player receives zero displacement");

            UnityEngine.Object.DestroyImmediate(pGo);
        }

        // ── 14. Level Persistence Round-Trip ───────────────────────
        private static void Test_LevelPersistence_RoundTrip()
        {
            LevelData original = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(1f, 2f), size = new Vector2Data(1f, 1f), color = 0f },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(0f, 0f), size = new Vector2Data(40f, 20f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData
                    {
                        position = new Vector2Data(3f, 4f),
                        size = new Vector2Data(5f, 1f),
                        rawGrayscaleColor = 128f,
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Circle,
                            playbackMode = TrajectoryPlaybackMode.Loop,
                            originX = 3f,
                            originY = 4f,
                            radius = 2.5f,
                            duration = 3.5f
                        }
                    }
                }
            };

            string json = JsonUtility.ToJson(original, true);
            LevelData restored = JsonUtility.FromJson<LevelData>(json);

            AssertTrue(restored != null, "Persistence_JsonParse", "Deserialized LevelData is not null");
            AssertTrue(restored.platforms != null && restored.platforms.Length == 1, "Persistence_PlatformsLength", "1 platform restored");
            AssertTrue(restored.platforms[0].trajectory != null, "Persistence_TrajNotNull", "Trajectory data restored");
            AssertTrue(restored.platforms[0].trajectory.enabled, "Persistence_TrajEnabled", "Trajectory enabled restored");
            AssertApproximately(2.5f, restored.platforms[0].trajectory.radius, 1e-4f, "Persistence_TrajRadius", "Trajectory radius preserved");
            AssertApproximately(3.5f, restored.platforms[0].trajectory.duration, 1e-4f, "Persistence_TrajDuration", "Trajectory duration preserved");
            AssertTrue(restored.platforms[0].trajectory.curveType == TrajectoryCurveType.Circle, "Persistence_CurveType", "Circle curveType preserved");
        }

        // ── 15. Legacy JSON Backward Compatibility ─────────────────
        private static void Test_LegacyJson_BackwardCompatibility()
        {
            string legacyJson = @"{
                ""startPos"": { ""position"": { ""x"": 0.0, ""y"": 1.0 }, ""size"": { ""x"": 1.0, ""y"": 1.0 }, ""color"": 0.0 },
                ""platforms"": [
                    { ""position"": { ""x"": 2.0, ""y"": 0.0 }, ""size"": { ""x"": 4.0, ""y"": 1.0 }, ""rawGrayscaleColor"": 255.0 }
                ]
            }";

            LevelData data = JsonUtility.FromJson<LevelData>(legacyJson);
            AssertTrue(data != null && data.platforms.Length == 1, "LegacyJson_Deserialization", "Legacy JSON without trajectory field parses correctly");
            AssertTrue(data.platforms[0].trajectory == null || !data.platforms[0].trajectory.enabled, "LegacyJson_StationaryDefault", "Missing trajectory field deserializes as null or disabled");

            GameObject pGo = new GameObject("LegacyTestPlatform");
            ColorPlatform platform = pGo.AddComponent<ColorPlatform>();
            platform.Initialize(data.platforms[0].position.ToVector2(), data.platforms[0].size.ToVector2(), data.platforms[0].rawGrayscaleColor);
            platform.InitializeTrajectory(data.platforms[0].trajectory);

            AssertTrue(platform.GetTrajectory() != null, "LegacyJson_ComponentAttached", "Trajectory component attached");
            AssertTrue(!platform.GetTrajectory().IsEnabled, "LegacyJson_DefaultDisabled", "Legacy platform defaults to disabled trajectory (stationary)");

            UnityEngine.Object.DestroyImmediate(pGo);
        }

        // ── 16. Trajectory Showcase Level Verification ─────────────
        private static void Test_TrajectoryShowcaseLevelFile()
        {
            string path = Path.Combine(Application.dataPath, "Levels", "TrajectoryShowcaseLevel.json");
            AssertTrue(File.Exists(path), "ShowcaseLevel_Exists", $"Showcase file exists at {path}");
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            LevelData data = JsonUtility.FromJson<LevelData>(json);

            AssertTrue(data != null, "ShowcaseLevel_Parsed", "JsonUtility successfully parsed TrajectoryShowcaseLevel.json");
            AssertTrue(data.startPos != null, "ShowcaseLevel_StartPos", "startPos is defined");
            AssertTrue(data.platforms != null && data.platforms.Length >= 10, "ShowcaseLevel_Platforms", $"Platforms defined ({data?.platforms?.Length})");

            // Verify player spawn stands safely on a platform
            Vector2 spawnPos = data.startPos.position.ToVector2();
            Vector2 spawnSize = data.startPos.size.ToVector2();
            float playerFeetY = spawnPos.y - spawnSize.y * 0.5f;

            bool foundSupportingPlatform = false;
            foreach (var p in data.platforms)
            {
                if (p == null) continue;
                Vector2 pPos = p.position.ToVector2();
                Vector2 pSize = p.size.ToVector2();
                float platTopY = pPos.y + pSize.y * 0.5f;
                float platLeft = pPos.x - pSize.x * 0.5f;
                float platRight = pPos.x + pSize.x * 0.5f;

                if (spawnPos.x >= platLeft && spawnPos.x <= platRight)
                {
                    if (Mathf.Abs(playerFeetY - platTopY) < 0.6f && playerFeetY >= platTopY - 0.05f)
                    {
                        foundSupportingPlatform = true;
                        break;
                    }
                }
            }
            AssertTrue(foundSupportingPlatform, "ShowcaseLevel_PlayerStands", "Player stands securely on platform upon spawn");

            int trajectoryCount = 0;
            bool hasLineH = false;
            bool hasLineV = false;
            bool hasLineDiag = false;
            bool hasSpatialBounds = false;
            bool hasLoop = false;
            bool hasOnce = false;
            bool hasCircle = false;
            bool hasArc = false;
            bool hasReverse = false;

            for (int i = 0; i < data.platforms.Length; i++)
            {
                var p = data.platforms[i];
                if (p.trajectory == null || !p.trajectory.enabled)
                    continue;

                trajectoryCount++;
                var cfg = p.trajectory;

                string valErr = Trajectory.Validate(cfg);
                AssertTrue(valErr == null, $"ShowcaseLevel_Valid_{i}", $"Platform #{i} trajectory valid (err: {valErr ?? "none"})");

                AssertTrue(cfg.duration >= 8.0f, $"ShowcaseLevel_Duration_{i}", $"Platform #{i} duration >= 8s for UI feeling (dur: {cfg.duration}s)");

                Vector2 initialPos = Trajectory.GetInitialPosition(cfg);
                AssertVectorApproximately(initialPos, p.position.ToVector2(), 0.01f, $"ShowcaseLevel_Coords_{i}", $"Platform #{i} matches initial evaluated point");

                if (cfg.curveType == TrajectoryCurveType.Line)
                {
                    if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                        hasSpatialBounds = true;
                    else if (Mathf.Abs(cfg.rotation) < 1f)
                        hasLineH = true;
                    else if (Mathf.Abs(cfg.rotation - 90f) < 1f)
                        hasLineV = true;
                    else
                        hasLineDiag = true;

                    if (cfg.playbackMode == TrajectoryPlaybackMode.Loop)
                        hasLoop = true;
                    if (cfg.playbackMode == TrajectoryPlaybackMode.Once)
                        hasOnce = true;
                }
                else if (cfg.curveType == TrajectoryCurveType.Circle)
                {
                    float angleSpan = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                    if (angleSpan >= 355f)
                        hasCircle = true;
                    else
                        hasArc = true;
                }

                if (cfg.reverseDirection)
                    hasReverse = true;
            }

            AssertTrue(trajectoryCount >= 8, "ShowcaseLevel_TotalCount", $"Total active trajectory platforms: {trajectoryCount}");
            AssertTrue(hasLineH, "ShowcaseLevel_LineH", "Has Horizontal Line PingPong");
            AssertTrue(hasLineV, "ShowcaseLevel_LineV", "Has Vertical Line Elevator");
            AssertTrue(hasLineDiag, "ShowcaseLevel_LineDiag", "Has Diagonal Line PingPong");
            AssertTrue(hasSpatialBounds, "ShowcaseLevel_SpatialBounds", "Has SpatialBounds Clipping");
            AssertTrue(hasLoop, "ShowcaseLevel_Loop", "Has Loop playback");
            AssertTrue(hasOnce, "ShowcaseLevel_Once", "Has Once playback");
            AssertTrue(hasCircle, "ShowcaseLevel_Circle", "Has Full 360 Circle");
            AssertTrue(hasArc, "ShowcaseLevel_Arc", "Has 180 Arc semi-circle");
            AssertTrue(hasReverse, "ShowcaseLevel_Reverse", "Has Reverse direction");
        }
    }
}
