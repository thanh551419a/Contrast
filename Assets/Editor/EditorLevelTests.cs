using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Color;
using Contrast.Data;
using Contrast.Input;
using Contrast.Level;
using Contrast.Level.EditorInteraction;
using Contrast.Player;
using Contrast.Presentation;
using Contrast.UI;
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
            Test_VisualPresentation_Integration();
            Test_EditorInputRouter();
            Test_TrajectoryInteractionState_Preview_Commit_Rollback();
            Test_TrajectoryHandleVisualizer_HitTesting();
            Test_TrajectoryEditController_LockstepAndSnapping();
            Test_Platform_SizeAndColor_Editing();
            Test_FitMap_And_LevelBounds_Calculations();
            Test_ResetLevel_Functionality();
            Test_GhostPlatform_TestPlay_Lifecycle_And_Reset();
            Test_EasyNewtonFinale_Integration();
            Test_PlatformInputAdapters_And_Equivalence();
            Test_StandaloneLevelEditor_Lifecycle();
            Test_CampaignLevels_JSON_Validation();
            Test_Newton_Marking_Sequence_And_Tolerances();

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

        public static void RunAllTestsBatch()
        {
            RunAllEditorTests();
            if (failedCount > 0)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
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

        private static void Test_VisualPresentation_Integration()
        {
            // 1. Verify BackgroundPresentation sets camera clear background correctly
            GameObject camGo = new GameObject("TestCam");
            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";

            GameObject bgGo = new GameObject("TestBg");
            var bg = bgGo.AddComponent<Contrast.Presentation.BackgroundPresentation>();
            AssertTrue(bg != null, "Visual_Background_Creation", "BackgroundPresentation initialized");
            AssertTrue(cam.clearFlags == CameraClearFlags.SolidColor, "Visual_Background_ClearFlags", "Camera clear flags set to SolidColor");
            AssertApproximately(0.02f, cam.backgroundColor.r, 0.015f, "Visual_Background_Color_R", "Background color red component");
            AssertApproximately(0.031f, cam.backgroundColor.g, 0.015f, "Visual_Background_Color_G", "Background color green component");
            AssertApproximately(0.071f, cam.backgroundColor.b, 0.015f, "Visual_Background_Color_B", "Background color blue component");

            // 2. Verify PlatformVisualPresentation generates optical styles without modifying AABB
            GameObject platGo = new GameObject("TestPlatform");
            var plat = platGo.AddComponent<ColorPlatform>();
            plat.Initialize(new Vector2(5f, 2f), new Vector2(4f, 1f), 255f);
            var platPres = platGo.AddComponent<Contrast.Presentation.PlatformVisualPresentation>();
            platPres.InitializeVisuals();

            AssertTrue(platPres != null, "Visual_Platform_Creation", "PlatformVisualPresentation created");
            Rect aabb = plat.GetAabb();
            AssertApproximately(4f, aabb.width, 0.01f, "Visual_Platform_AABB_Width_Preserved", "Platform AABB width untouched");
            AssertApproximately(1f, aabb.height, 0.01f, "Visual_Platform_AABB_Height_Preserved", "Platform AABB height untouched");
            AssertApproximately(5f, aabb.center.x, 0.01f, "Visual_Platform_AABB_CenterX_Preserved", "Platform AABB center X untouched");
            AssertApproximately(2f, aabb.center.y, 0.01f, "Visual_Platform_AABB_CenterY_Preserved", "Platform AABB center Y untouched");

            // 3. Verify PlayerVisualPresentation creates optical children without altering PlayerSize
            GameObject playerGo = new GameObject("TestPlayer");
            var pc = playerGo.AddComponent<PlayerController>();
            var pPres = playerGo.AddComponent<Contrast.Presentation.PlayerVisualPresentation>();
            pPres.InitializeVisuals();
            AssertTrue(pPres != null, "Visual_Player_Creation", "PlayerVisualPresentation created");
            Transform visualRoot = playerGo.transform.Find("VisualPresentationRoot");
            AssertTrue(visualRoot != null, "Visual_Player_Root_Hierarchy", "VisualPresentationRoot exists as child");
            AssertTrue(visualRoot.Find("OpticalBody") != null, "Visual_Player_OpticalBody_Child", "OpticalBody child exists");
            AssertTrue(visualRoot.Find("GlowHalo") != null, "Visual_Player_GlowHalo_Child", "GlowHalo child exists");
            AssertTrue(visualRoot.Find("CoreLight") != null, "Visual_Player_CoreLight_Child", "CoreLight child exists");

            // 4. Verify NewtonRingsPresentation initializes without exceptions
            GameObject newtonGo = new GameObject("TestNewton");
            var newton = newtonGo.AddComponent<Contrast.Presentation.NewtonRingsPresentation>();
            AssertTrue(newton != null, "Visual_Newton_Creation", "NewtonRingsPresentation created");

            // Cleanup test objects
            UnityEngine.Object.DestroyImmediate(playerGo);
            UnityEngine.Object.DestroyImmediate(platGo);
            UnityEngine.Object.DestroyImmediate(bgGo);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(newtonGo);
        }

        private static void Test_EditorInputRouter()
        {
            var router = new EditorInputRouter();
            AssertTrue(!router.HasPointerCapture, "InputRouter_InitialCapture", "No capture initially");

            object ownerA = "ownerA";
            object ownerB = "ownerB";

            AssertTrue(router.CapturePointer(ownerA), "InputRouter_CaptureA", "Owner A acquires capture");
            AssertTrue(router.HasPointerCapture, "InputRouter_HasCapture", "Has capture after acquire");
            AssertTrue(router.IsOwner(ownerA), "InputRouter_IsOwnerA", "IsOwner matches owner A");
            AssertTrue(!router.IsOwner(ownerB), "InputRouter_NotOwnerB", "IsOwner returns false for owner B");

            // Owner B cannot steal capture while owner A holds it
            AssertTrue(!router.CapturePointer(ownerB), "InputRouter_CannotSteal", "Owner B cannot steal capture from owner A");

            router.ReleasePointer(ownerA);
            AssertTrue(!router.HasPointerCapture, "InputRouter_Released", "Capture released successfully");

            // Can now be acquired by owner B
            AssertTrue(router.CapturePointer(ownerB), "InputRouter_CaptureB", "Owner B acquires capture after release");
            router.ForceReleasePointer();
            AssertTrue(!router.HasPointerCapture, "InputRouter_ForceRelease", "ForceRelease clears capture");
        }

        private static void Test_TrajectoryInteractionState_Preview_Commit_Rollback()
        {
            GameObject go = new GameObject("TestPlat_InteractionState");
            var plat = go.AddComponent<ColorPlatform>();
            plat.Initialize(new Vector2(10f, 5f), new Vector2(4f, 1f), 255f);

            var cfg = TrajectoryData.CreateDefault(new Vector2(10f, 5f));
            cfg.enabled = true;
            cfg.curveType = TrajectoryCurveType.Line;
            cfg.scaleX = 6f;
            cfg.rotation = 0f;
            plat.InitializeTrajectory(cfg);

            var state = new TrajectoryInteractionState();
            AssertTrue(!state.IsDragging, "InteractionState_Init", "Initially idle");

            // 1. Begin drag on LineEnd handle
            Vector2 handleWorld = new Vector2(16f, 5f);
            AssertTrue(state.BeginDrag(TrajectoryHandleKind.LineEnd, handleWorld, plat), "InteractionState_BeginDrag", "Begin drag succeeded");
            AssertTrue(state.IsDragging, "InteractionState_Dragging", "Phase is dragging");
            AssertTrue(state.ActiveHandle == TrajectoryHandleKind.LineEnd, "InteractionState_ActiveHandle", "Active handle is LineEnd");

            // 2. Drag pointer to (10, 13) -> vertical line of length 8 (scaleX=8, rotation=90)
            Vector2 dragWorld = new Vector2(10f, 13f);
            AssertTrue(state.CalculatePreview(dragWorld, out Vector2 previewPlatformPos), "InteractionState_CalcPreview", "CalculatePreview returned true");
            AssertApproximately(8f, state.WorkingPreviewConfig.scaleX, 0.01f, "InteractionState_PreviewScale", "Preview scaleX is 8");
            AssertApproximately(90f, state.WorkingPreviewConfig.rotation, 0.1f, "InteractionState_PreviewRot", "Preview rotation is 90 deg");

            // 3. Rollback: should restore initial state
            TrajectoryData rolledBack = state.Rollback(out Vector2 restoredPos);
            AssertTrue(!state.IsDragging, "InteractionState_Rollback_Idle", "Phase returns to idle after rollback");
            AssertApproximately(6f, rolledBack.scaleX, 0.01f, "InteractionState_Rollback_ScaleRestored", "ScaleX restored to 6");
            AssertApproximately(0f, rolledBack.rotation, 0.01f, "InteractionState_Rollback_RotRestored", "Rotation restored to 0");
            AssertApproximately(10f, restoredPos.x, 0.01f, "InteractionState_Rollback_PosRestoredX", "Platform X restored to 10");
            AssertApproximately(5f, restoredPos.y, 0.01f, "InteractionState_Rollback_PosRestoredY", "Platform Y restored to 5");

            // 4. Begin drag again and Commit
            state.BeginDrag(TrajectoryHandleKind.LineEnd, handleWorld, plat);
            state.CalculatePreview(dragWorld, out _);
            TrajectoryData committed = state.Commit(out Vector2 finalPos);
            AssertTrue(!state.IsDragging, "InteractionState_Commit_Idle", "Phase returns to idle after commit");
            AssertApproximately(8f, committed.scaleX, 0.01f, "InteractionState_Commit_ScaleCommitted", "Committed scaleX is 8");
            AssertApproximately(90f, committed.rotation, 0.1f, "InteractionState_Commit_RotCommitted", "Committed rotation is 90 deg");
            AssertApproximately(8f, plat.GetTrajectory().Config.scaleX, 0.01f, "InteractionState_Authoritative_Scale", "Platform trajectory config updated");

            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void Test_TrajectoryHandleVisualizer_HitTesting()
        {
            var visualizer = new TrajectoryHandleVisualizer(0.16f, 0.35f);

            // 1. Line mode
            var lineCfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.ParameterRange,
                originX = 5f,
                originY = 2f,
                scaleX = 10f,
                rotation = 0f,
                paramMin = 0f,
                paramMax = 1f,
                duration = 4f
            };

            // Hit near origin (5, 2)
            AssertTrue(visualizer.HitTest(new Vector2(5.1f, 2.05f), lineCfg) == TrajectoryHandleKind.Origin,
                "HitTest_Line_Origin", "Origin handle hit detected");

            // Hit near endpoint (15, 2)
            AssertTrue(visualizer.HitTest(new Vector2(14.95f, 2.1f), lineCfg) == TrajectoryHandleKind.LineEnd,
                "HitTest_Line_LineEnd", "LineEnd handle hit detected");

            // Miss
            AssertTrue(visualizer.HitTest(new Vector2(10f, 10f), lineCfg) == TrajectoryHandleKind.None,
                "HitTest_Line_Miss", "Miss returns None");

            // 2. Circle mode
            var circleCfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                originX = 20f,
                originY = 10f,
                radius = 5f,
                paramMin = 0f,
                paramMax = 360f,
                duration = 6f
            };

            // Hit center (20, 10)
            AssertTrue(visualizer.HitTest(new Vector2(20.1f, 9.95f), circleCfg) == TrajectoryHandleKind.CircleCenter,
                "HitTest_Circle_Center", "Circle center handle hit detected");

            // Hit radius handle (20 + 5, 10) = (25, 10)
            AssertTrue(visualizer.HitTest(new Vector2(25.05f, 10.1f), circleCfg) == TrajectoryHandleKind.CircleRadius,
                "HitTest_Circle_Radius", "Circle radius handle hit detected");

            // 3. Spatial bounds line
            var boundsCfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.SpatialBounds,
                originX = 0f,
                originY = 0f,
                scaleX = 10f,
                rotation = 0f,
                boundsXMin = -8f,
                boundsXMax = 8f,
                boundsYMin = -4f,
                boundsYMax = 4f,
                duration = 4f
            };

            // Hit MinX handle at (-8, 0)
            AssertTrue(visualizer.HitTest(new Vector2(-8.1f, 0.05f), boundsCfg) == TrajectoryHandleKind.BoundsMinX,
                "HitTest_Bounds_MinX", "SpatialBounds MinX handle hit detected");
            // Hit MaxX handle at (8, 0)
            AssertTrue(visualizer.HitTest(new Vector2(7.95f, -0.05f), boundsCfg) == TrajectoryHandleKind.BoundsMaxX,
                "HitTest_Bounds_MaxX", "SpatialBounds MaxX handle hit detected");
        }

        private static void Test_TrajectoryEditController_LockstepAndSnapping()
        {
            var editCtrl = new TrajectoryEditController(0.16f, 0.35f);

            GameObject go = new GameObject("TestPlat_EditCtrl");
            var plat = go.AddComponent<ColorPlatform>();
            plat.Initialize(new Vector2(10f, 5f), new Vector2(4f, 1f), 255f);

            var cfg = TrajectoryData.CreateDefault(new Vector2(10f, 5f));
            cfg.enabled = true;
            cfg.curveType = TrajectoryCurveType.Line;
            cfg.domainMode = TrajectoryDomainMode.SpatialBounds;
            cfg.originX = 10f;
            cfg.originY = 5f;
            cfg.boundsXMin = 5f;
            cfg.boundsXMax = 15f;
            cfg.boundsYMin = 2f;
            cfg.boundsYMax = 8f;
            plat.InitializeTrajectory(cfg);

            editCtrl.SetTarget(plat, null);

            // Test lockstep offset
            Vector2 initialOrigin = new Vector2(10f, 5f);
            Vector2 initialBoundsMin = new Vector2(5f, 2f);
            Vector2 initialBoundsMax = new Vector2(15f, 8f);
            Vector2 delta = new Vector2(3f, -2f);

            editCtrl.ApplyLockstepOffset(initialOrigin, initialBoundsMin, initialBoundsMax, delta, null);

            var activeCfg = plat.GetTrajectory().Config;
            AssertApproximately(13f, activeCfg.originX, 0.001f, "EditCtrl_Lockstep_OriginX", "Origin X translated lockstep");
            AssertApproximately(3f, activeCfg.originY, 0.001f, "EditCtrl_Lockstep_OriginY", "Origin Y translated lockstep");
            AssertApproximately(8f, activeCfg.boundsXMin, 0.001f, "EditCtrl_Lockstep_BoundsMinX", "Bounds MinX translated lockstep");
            AssertApproximately(18f, activeCfg.boundsXMax, 0.001f, "EditCtrl_Lockstep_BoundsMaxX", "Bounds MaxX translated lockstep");
            AssertApproximately(0f, activeCfg.boundsYMin, 0.001f, "EditCtrl_Lockstep_BoundsMinY", "Bounds MinY translated lockstep");
            AssertApproximately(6f, activeCfg.boundsYMax, 0.001f, "EditCtrl_Lockstep_BoundsMaxY", "Bounds MaxY translated lockstep");

            editCtrl.ClearTarget(null);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void Test_Platform_SizeAndColor_Editing()
        {
            GameObject go = new GameObject("TestPlat_SizeColor");
            var plat = go.AddComponent<ColorPlatform>();
            plat.Initialize(new Vector2(0f, 0f), new Vector2(4f, 1f), 255f);

            var pres = go.AddComponent<PlatformVisualPresentation>();
            pres.InitializeVisuals();

            // Find surface renderer and border renderer
            Transform visualRoot = go.transform.Find("PlatformVisualPresentationRoot");
            AssertTrue(visualRoot != null, "SizeColor_RootExists", "Visual root exists");
            SpriteRenderer surfaceSr = visualRoot.Find("Surface")?.GetComponent<SpriteRenderer>();
            SpriteRenderer borderSr = visualRoot.Find("Border")?.GetComponent<SpriteRenderer>();

            AssertTrue(surfaceSr != null, "SizeColor_SurfaceExists", "Surface renderer exists");
            AssertApproximately(4f, surfaceSr.transform.localScale.x, 0.01f, "SizeColor_InitScaleX", "Initial scale X is 4");
            AssertApproximately(1f, surfaceSr.transform.localScale.y, 0.01f, "SizeColor_InitScaleY", "Initial scale Y is 1");

            // 1. Change size via SetEditorData
            plat.SetEditorData(new Vector2(0f, 0f), new Vector2(8f, 3f), 255f);
            AssertApproximately(8f, plat.Size.x, 0.01f, "SizeColor_PlatSizeX", "Platform Size X updated");
            AssertApproximately(3f, plat.Size.y, 0.01f, "SizeColor_PlatSizeY", "Platform Size Y updated");

            // Verify visual presentation updated to 8x3 with 9-slice borders
            AssertApproximately(8f, surfaceSr.transform.localScale.x, 0.01f, "SizeColor_VisualScaleX", "Visual surface scale X updated to 8");
            AssertApproximately(3f, surfaceSr.transform.localScale.y, 0.01f, "SizeColor_VisualScaleY", "Visual surface scale Y updated to 3");
            AssertApproximately(8f, borderSr.size.x, 0.01f, "SizeColor_BorderSizeX", "Visual border size X updated to 8");
            AssertApproximately(3f, borderSr.size.y, 0.01f, "SizeColor_BorderSizeY", "Visual border size Y updated to 3");

            // 2. Change color to White (0) via SetEditorData
            plat.SetEditorData(new Vector2(0f, 0f), new Vector2(8f, 3f), 0f);
            AssertApproximately(0f, plat.RawGrayscaleColor, 0.01f, "SizeColor_PlatColorRaw", "Platform color updated to 0 (White)");
            AssertTrue(plat.Logical == LogicalColor.White, "SizeColor_PlatLogical", "Platform logical color is White");

            // Verify visual presentation updated to high-contrast White border (crisp slate outline)
            UnityEngine.Color whiteBorder = new UnityEngine.Color(0.18f, 0.25f, 0.35f, 0.95f);
            AssertApproximately(whiteBorder.r, borderSr.color.r, 0.02f, "SizeColor_VisualBorderColorR", "Visual border color R matches White high-contrast border");
            AssertApproximately(whiteBorder.a, borderSr.color.a, 0.02f, "SizeColor_VisualBorderColorA", "Visual border color A matches White high-contrast border");

            // 3. Test through LevelEditorController.ApplySelectedPlatformFromUI
            GameObject ctrlGo = new GameObject("TestCtrl_SizeColor");
            var ctrl = ctrlGo.AddComponent<LevelEditorController>();
            ctrl.SelectPlatform(plat);

            ctrl.ApplySelectedPlatformFromUI(new Vector2(2f, 1f), new Vector2(12f, 4f), 127f); // Gray, 12x4 at (2, 1)

            AssertApproximately(12f, plat.Size.x, 0.01f, "SizeColor_CtrlSizeX", "Platform Size X updated via controller");
            AssertApproximately(4f, plat.Size.y, 0.01f, "SizeColor_CtrlSizeY", "Platform Size Y updated via controller");
            AssertApproximately(12f, surfaceSr.transform.localScale.x, 0.01f, "SizeColor_CtrlVisualScaleX", "Visual scale X updated via controller");
            AssertApproximately(4f, surfaceSr.transform.localScale.y, 0.01f, "SizeColor_CtrlVisualScaleY", "Visual scale Y updated via controller");
            AssertApproximately(12f, borderSr.size.x, 0.01f, "SizeColor_CtrlBorderSizeX", "Visual border size X updated via controller");
            AssertApproximately(4f, borderSr.size.y, 0.01f, "SizeColor_CtrlBorderSizeY", "Visual border size Y updated via controller");
            AssertTrue(plat.Logical == LogicalColor.Gray, "SizeColor_CtrlLogical", "Platform logical color is Gray via controller");

            UnityEngine.Color grayBorder = new UnityEngine.Color(0.769f, 0.800f, 0.835f, 0.50f);
            AssertApproximately(grayBorder.r, borderSr.color.r, 0.02f, "SizeColor_CtrlVisualBorderR", "Visual border color R matches Gray");

            UnityEngine.Object.DestroyImmediate(ctrlGo);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void Test_FitMap_And_LevelBounds_Calculations()
        {
            // Clean slate for testing bounds
            ColorPlatform.ClearRegistry();
            LevelMarker.All.Clear();

            // Create temporary camera
            GameObject camGo = new GameObject("TestFitMap_Cam");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            GameObject ctrlGo = new GameObject("TestFitMap_Ctrl");
            var ctrl = ctrlGo.AddComponent<LevelEditorController>();

            // 1. Degenerate / Empty level fallback bounds
            Bounds emptyBounds = ctrl.CalculateLevelBounds();
            AssertApproximately(0f, emptyBounds.center.x, 0.01f, "FitMap_EmptyBoundsCenterX", "Empty bounds centered at X=0");
            AssertApproximately(0f, emptyBounds.center.y, 0.01f, "FitMap_EmptyBoundsCenterY", "Empty bounds centered at Y=0");
            AssertApproximately(20f, emptyBounds.size.x, 0.01f, "FitMap_EmptyBoundsSizeX", "Empty bounds fallback width 20");
            AssertApproximately(10f, emptyBounds.size.y, 0.01f, "FitMap_EmptyBoundsSizeY", "Empty bounds fallback height 10");

            // 2. Multi-object level bounds (including negative coordinates and platforms of various sizes)
            GameObject p1Go = new GameObject("P1");
            var p1 = p1Go.AddComponent<ColorPlatform>();
            p1.Initialize(new Vector2(-20f, -5f), new Vector2(6f, 2f), 0f); // White platform at (-20, -5)

            GameObject p2Go = new GameObject("P2");
            var p2 = p2Go.AddComponent<ColorPlatform>();
            p2.Initialize(new Vector2(40f, 15f), new Vector2(10f, 2f), 212f); // Black platform at (40, 15)

            // StartPos marker at (-10, 0)
            GameObject markerGo = new GameObject("StartPos");
            var marker = markerGo.AddComponent<LevelMarker>();
            marker.Initialize(LevelMarkerType.StartPos, new Vector2(-10f, 0f), new Vector2(1f, 1f), 0f);

            Bounds multiBounds = ctrl.CalculateLevelBounds();
            // X span: (-20 - 3) = -23 to (40 + 5) = 45 -> width 68, center 11
            // Y span: (-5 - 1) = -6 to (15 + 1) = 16 -> height 22, center 5
            AssertApproximately(11f, multiBounds.center.x, 0.1f, "FitMap_MultiBoundsCenterX", "Multi bounds center X correct");
            AssertApproximately(5f, multiBounds.center.y, 0.1f, "FitMap_MultiBoundsCenterY", "Multi bounds center Y correct");
            AssertApproximately(68f, multiBounds.size.x, 0.1f, "FitMap_MultiBoundsSizeX", "Multi bounds size X correct");
            AssertApproximately(22f, multiBounds.size.y, 0.1f, "FitMap_MultiBoundsSizeY", "Multi bounds size Y correct");

            // 3. Trajectory extent expansion
            // Add trajectory to P2 moving from (40, 15) to (70, 25)
            var traj = p2.GetTrajectory();
            var trajData = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.ParameterRange,
                originX = 40f,
                originY = 15f,
                rotation = 0f,
                scaleX = 30f,
                paramMin = 0f,
                paramMax = 1f,
                duration = 4f
            };
            p2.InitializeTrajectory(trajData);

            Bounds trajBounds = ctrl.CalculateLevelBounds();
            // Trajectory extends to X=70 -> total X span -23 to 70 = 93
            AssertTrue(trajBounds.size.x >= 92f, "FitMap_TrajBoundsExpanded", "Trajectory motion expands bounds");

            // 4. Test FitMap execution
            ctrl.StartEditing();
            ctrl.FitMap();

            // Verify orthographic size enlarged to fit bounds comfortably
            AssertTrue(cam.orthographicSize > 15f, "FitMap_CamOrthoSizeExpanded", "Camera orthographic size fits level content");

            // Verify camera position centered on bounds (accounting for UI panel margins)
            float camXBefore = cam.transform.position.x;
            float camYBefore = cam.transform.position.y;
            float orthoBefore = cam.orthographicSize;

            // Repeated FitMap call should be idempotent and stable
            ctrl.FitMap();
            AssertApproximately(camXBefore, cam.transform.position.x, 0.001f, "FitMap_IdempotentX", "FitMap repeated call preserves X");
            AssertApproximately(camYBefore, cam.transform.position.y, 0.001f, "FitMap_IdempotentY", "FitMap repeated call preserves Y");
            AssertApproximately(orthoBefore, cam.orthographicSize, 0.001f, "FitMap_IdempotentOrtho", "FitMap repeated call preserves OrthoSize");

            // 5. White platform visibility aid and selection box verification
            ctrl.SelectPlatform(p1);
            AssertTrue(ctrl.SelectedPlatform == p1, "FitMap_SelectWhitePlat", "White platform selected successfully");
            AssertTrue(p1.Logical == LogicalColor.White, "FitMap_WhiteColorSemantic", "White platform preserves White semantic");

            ctrl.StopEditing();

            // Cleanup
            UnityEngine.Object.DestroyImmediate(p1Go);
            UnityEngine.Object.DestroyImmediate(p2Go);
            UnityEngine.Object.DestroyImmediate(markerGo);
            UnityEngine.Object.DestroyImmediate(ctrlGo);
            UnityEngine.Object.DestroyImmediate(camGo);
            ColorPlatform.ClearRegistry();
            LevelMarker.All.Clear();
        }

        // ── 24. Reset Level Functionality ─────────────────────────
        private static void Test_ResetLevel_Functionality()
        {
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            GameObject loaderGo = new GameObject("LevelLoader");
            LevelLoader loader = loaderGo.AddComponent<LevelLoader>();

            GameObject ctrlGo = new GameObject("LevelEditorController");
            LevelEditorController ctrl = ctrlGo.AddComponent<LevelEditorController>();

            // Setup platforms root
            GameObject platRoot = new GameObject("Platforms");
            GameObject p1Go = new GameObject("Platform_1");
            p1Go.transform.SetParent(platRoot.transform);
            ColorPlatform p1 = p1Go.AddComponent<ColorPlatform>();
            p1.Initialize(new Vector2(0f, 0f), new Vector2(4f, 1f), 255f);

            GameObject p2Go = new GameObject("Platform_2");
            p2Go.transform.SetParent(platRoot.transform);
            ColorPlatform p2 = p2Go.AddComponent<ColorPlatform>();
            p2.Initialize(new Vector2(10f, 5f), new Vector2(3f, 1f), 0f);

            // Setup level markers root
            GameObject markerRoot = new GameObject("LevelObjects");
            GameObject startGo = new GameObject("StartPos");
            startGo.transform.SetParent(markerRoot.transform);
            LevelMarker startMarker = startGo.AddComponent<LevelMarker>();
            startMarker.Initialize(LevelMarkerType.StartPos, new Vector2(0f, 2f), Vector2.one, 0f);

            ctrl.StartEditing();
            AssertTrue(ColorPlatform.All.Count >= 2, "ResetLevel_InitPlatforms", "Platforms registered before reset");
            AssertTrue(LevelMarker.All.Count >= 1, "ResetLevel_InitMarkers", "Markers registered before reset");

            // Select a platform
            ctrl.SelectPlatform(p1);
            AssertTrue(ctrl.SelectedPlatform == p1, "ResetLevel_SelectionBefore", "Platform selected before reset");

            // Save camera state before reset
            Vector3 camPosBefore = cam.transform.position;
            float orthoBefore = cam.orthographicSize;

            // Execute Reset
            ctrl.ResetLevel();

            // Verify GameObjects and registries cleared
            AssertTrue(ColorPlatform.All.Count == 0, "ResetLevel_PlatformsCleared", "All platforms cleared from registry");
            AssertTrue(LevelMarker.All.Count == 0, "ResetLevel_MarkersCleared", "All markers cleared from registry");
            AssertTrue(ctrl.SelectedPlatform == null, "ResetLevel_SelectionCleared", "Selection cleared after reset");

            // Verify camera view was NOT reset (preserved as required)
            AssertApproximately(camPosBefore.x, cam.transform.position.x, 0.001f, "ResetLevel_CamXPreserved", "Camera X preserved after reset");
            AssertApproximately(camPosBefore.y, cam.transform.position.y, 0.001f, "ResetLevel_CamYPreserved", "Camera Y preserved after reset");
            AssertApproximately(orthoBefore, cam.orthographicSize, 0.001f, "ResetLevel_OrthoPreserved", "Camera zoom preserved after reset");

            // Verify exported data has 0 platforms and no resurrected markers
            LevelData exportData = ctrl.BuildExportData();
            AssertTrue(exportData != null, "ResetLevel_ExportNonNull", "Export LevelData non-null");
            AssertTrue(exportData.platforms != null && exportData.platforms.Length == 0, "ResetLevel_ExportPlatformsEmpty", "Exported platforms array is empty");
            AssertTrue(exportData.checkPoint == null || exportData.checkPoint.Length == 0, "ResetLevel_ExportCheckpointsEmpty", "Exported checkpoints empty");
            AssertTrue(exportData.end == null, "ResetLevel_ExportEndNull", "Exported End marker is null");

            // Verify second reset on empty level (idempotent / safe)
            ctrl.ResetLevel();
            AssertTrue(ColorPlatform.All.Count == 0, "ResetLevel_EmptyLevelSafe", "Second reset on empty level safe");

            ctrl.StopEditing();

            // Cleanup
            UnityEngine.Object.DestroyImmediate(platRoot);
            UnityEngine.Object.DestroyImmediate(markerRoot);
            UnityEngine.Object.DestroyImmediate(ctrlGo);
            UnityEngine.Object.DestroyImmediate(loaderGo);
            UnityEngine.Object.DestroyImmediate(camGo);
            ColorPlatform.ClearRegistry();
            LevelMarker.All.Clear();
        }

        private static void Test_GhostPlatform_TestPlay_Lifecycle_And_Reset()
        {
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();

            GameObject loaderGo = new GameObject("LevelLoader");
            LevelLoader loader = loaderGo.AddComponent<LevelLoader>();

            GameObject ctrlGo = new GameObject("LevelEditorController");
            LevelEditorController ctrl = ctrlGo.AddComponent<LevelEditorController>();

            // 1. Verify roots are initialized early and unified
            AssertTrue(loader.PlatformsRoot != null, "GhostTest_LoaderPlatformsRootNonNull", "LevelLoader.PlatformsRoot is non-null before play");
            AssertTrue(loader.LevelObjectsRoot != null, "GhostTest_LoaderLevelObjectsRootNonNull", "LevelLoader.LevelObjectsRoot is non-null before play");

            ctrl.StartEditing();

            // 2. Authored level in Edit mode with 1 static and 1 moving platform
            LevelData authoredData = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(10f, 0f), size = new Vector2Data(30f, 15f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, 0f), size = new Vector2Data(4f, 1f), rawGrayscaleColor = 255f },
                    new ColorPlatformData
                    {
                        position = new Vector2Data(10f, 2f),
                        size = new Vector2Data(3f, 1f),
                        rawGrayscaleColor = 0f,
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Line,
                            playbackMode = TrajectoryPlaybackMode.PingPong,
                            originX = 10f,
                            originY = 2f,
                            scaleX = 8f,
                            duration = 8f
                        }
                    }
                }
            };

            loader.BuildRuntimeLevel(authoredData);

            int editSceneCount = UnityEngine.Object.FindObjectsByType<ColorPlatform>().Length;
            AssertTrue(ColorPlatform.All.Count == 2, "GhostTest_EditPlatformCount", "2 platforms registered in edit mode");
            AssertTrue(editSceneCount == 2, "GhostTest_EditSceneObjectCount", "Exactly 2 platform GameObjects in scene in edit mode");

            // 3. Transition into Test Play
            // Export working copy and build runtime level (simulating Test Play entry)
            LevelData testPlayData = ctrl.BuildExportData();
            loader.BuildRuntimeLevel(testPlayData);

            int testPlaySceneCount = UnityEngine.Object.FindObjectsByType<ColorPlatform>().Length;
            AssertTrue(ColorPlatform.All.Count == 2, "GhostTest_TestPlayPlatformCount", "Exactly 2 platforms registered during Test Play");
            AssertTrue(testPlaySceneCount == 2, "GhostTest_NoGhostObjectsCreated", "No ghost or duplicate platform GameObjects exist in scene on Test Play start");

            // 4. Advance trajectory movement
            TrajectorySystem.UpdateAll(1.0f); // 1s into 4s PingPong => phase 0.25 => X moves to 10 + 8 * 0.25 = 12
            ColorPlatform movingPlat = null;
            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                if (ColorPlatform.All[i].GetTrajectory() != null && ColorPlatform.All[i].GetTrajectory().Config != null && ColorPlatform.All[i].GetTrajectory().Config.enabled)
                    movingPlat = ColorPlatform.All[i];
            }
            AssertTrue(movingPlat != null, "GhostTest_MovingPlatformFound", "Moving platform exists");
            AssertApproximately(12f, movingPlat.transform.position.x, 0.05f, "GhostTest_PlatformMoved", "Moving platform advanced along trajectory to x=12");

            // Verify scene objects count remains 2 (no ghosts at original x=10)
            int movingSceneCount = UnityEngine.Object.FindObjectsByType<ColorPlatform>().Length;
            AssertTrue(movingSceneCount == 2, "GhostTest_NoStationaryGhostAtOrigin", "Scene platform count strictly equals 2 during movement (no stationary ghosts at origin)");

            // 5. Test Reset clears all platforms, markers, and ghost candidates
            ctrl.ResetLevel();
            int resetPlatCount = UnityEngine.Object.FindObjectsByType<ColorPlatform>().Length;
            int resetMarkerCount = UnityEngine.Object.FindObjectsByType<LevelMarker>().Length;
            AssertTrue(ColorPlatform.All.Count == 0, "GhostTest_ResetRegistryEmpty", "ColorPlatform.All is empty after Reset");
            AssertTrue(resetPlatCount == 0, "GhostTest_ResetScenePlatformsEmpty", "Zero ColorPlatform GameObjects remain in scene after Reset");
            AssertTrue(resetMarkerCount == 0, "GhostTest_ResetSceneMarkersEmpty", "Zero LevelMarker GameObjects remain in scene after Reset");

            // 6. Test Position Restoration after stopping Test Play
            // Re-load authored level, move it, then test return to Edit mode
            loader.BuildRuntimeLevel(authoredData);
            LevelData beforeTestPlay = ctrl.BuildExportData();
            ctrl.SetWorkingCopy(beforeTestPlay);
            loader.BuildRuntimeLevel(beforeTestPlay);
            TrajectorySystem.UpdateAll(4.0f); // advance to turnaround point x=18 (4s = half cycle of 8s PingPong)

            // Stop Test Play and return to Edit (rebuilds from working copy)
            ctrl.StartEditing();
            ColorPlatform restoredPlat = null;
            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                if (ColorPlatform.All[i].GetTrajectory() != null && ColorPlatform.All[i].GetTrajectory().Config != null && ColorPlatform.All[i].GetTrajectory().Config.enabled)
                    restoredPlat = ColorPlatform.All[i];
            }
            AssertTrue(restoredPlat != null, "GhostTest_RestoredPlatFound", "Platform found after returning to Edit");
            AssertApproximately(10f, restoredPlat.transform.position.x, 0.001f, "GhostTest_RestoredPositionX", "Platform position restored to authored edit position (10, 2)");
            AssertApproximately(2f, restoredPlat.transform.position.y, 0.001f, "GhostTest_RestoredPositionY", "Platform position Y matches authored edit position");

            ctrl.StopEditing();

            // Cleanup
            UnityEngine.Object.DestroyImmediate(ctrlGo);
            UnityEngine.Object.DestroyImmediate(loaderGo);
            UnityEngine.Object.DestroyImmediate(camGo);
            ColorPlatform.ClearRegistry();
            LevelMarker.All.Clear();
        }

        private static void Test_EasyNewtonFinale_Integration()
        {
            Debug.Log("[TEST] --- Running Test_EasyNewtonFinale_Integration ---");

            // 1. Input mapping test
            PlayerInput.IsNewtonFinaleActive = false;
            AssertTrue(PlayerInput.IsJumpInputPressed(false, true), "FinaleInput_NormalSpaceJump", "Space triggers jump outside finale");
            AssertTrue(PlayerInput.IsJumpInputPressed(true, false), "FinaleInput_NormalWJump", "W triggers jump outside finale");
            AssertTrue(PlayerInput.IsJumpInputPressed(true, true), "FinaleInput_NormalBothJump", "Both keys trigger jump outside finale");

            PlayerInput.IsNewtonFinaleActive = true;
            AssertTrue(PlayerInput.IsJumpInputPressed(false, true), "FinaleInput_FinaleSpaceJumps", "Space ALWAYS triggers jump even inside finale");
            AssertTrue(PlayerInput.IsJumpInputPressed(true, false), "FinaleInput_FinaleWJump", "W triggers jump inside finale");
            AssertTrue(PlayerInput.IsJumpInputPressed(true, true), "FinaleInput_FinaleBothJump", "W still jumps even if space pressed inside finale");
            PlayerInput.IsNewtonFinaleActive = false;

            // 2. Easy Level Detection
            LevelData easyLevel = new LevelData { platforms = new[] { new ColorPlatformData { position = new Vector2Data(45.625f, 0.4375f) } } };
            LevelData hardLevel = new LevelData { platforms = new[] { new ColorPlatformData { position = new Vector2Data(10f, 10f) } } };
            AssertTrue(LevelLoader.IsEasyLevel(easyLevel), "FinaleLevel_EasyDetected", "IsEasyLevel returns true for Easy_Level");
            AssertTrue(!LevelLoader.IsEasyLevel(hardLevel), "FinaleLevel_HardExcluded", "IsEasyLevel returns false for Hard_Level");

            // 3. Mathematical Geometry Verification
            float ringScale = 3.4375f; // 220 px / 64
            float rK = ringScale * Mathf.Sqrt(4f); // 6.875 units
            float rI = ringScale * Mathf.Sqrt(2f); // ~4.86134 units
            AssertApproximately(6.875f, rK, 0.001f, "FinaleMath_RadiusK", "Platform K radius is exactly 6.875 units (440 px)");
            AssertApproximately(4.86134f, rI, 0.001f, "FinaleMath_RadiusI", "Platform I radius is exactly 4.86134 units (311.127 px)");

            Vector2 centerO = new Vector2(61.0f, 0.625f);
            float testAngle = 60f * Mathf.Deg2Rad;
            Vector2 u = new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle)).normalized;
            Vector2 ptK = centerO + u * rK;
            Vector2 ptI = centerO - u * rI;
            Vector2 ptKp = centerO - u * rK;

            float distB_units = Vector2.Distance(ptK, ptI);
            float distb_units = Vector2.Distance(ptI, ptKp);
            AssertApproximately(rK + rI, distB_units, 0.001f, "FinaleMath_DistB", "Measured B equals rK + rI");
            AssertApproximately(rK - rI, distb_units, 0.001f, "FinaleMath_Distb", "Measured b equals rK - rI");

            float lambdaRefMm = 550f * 1e-6f;
            float R_mm = 855f;
            float physicalR1Mm = Mathf.Sqrt(lambdaRefMm * R_mm);
            float mmPerUnit = physicalR1Mm / ringScale;

            float distB_mm = distB_units * mmPerUnit;
            float distb_mm = distb_units * mmPerUnit;
            int orderDelta = Mathf.Abs(4 - 2);
            float denom = orderDelta * R_mm;
            float lambdaOutMm = (distB_mm * distb_mm) / denom;
            float lambdaOutNm = lambdaOutMm * 1e6f;
            AssertApproximately(550f, lambdaOutNm, 0.05f, "FinaleMath_CalculatedWavelength", "Physical formula calculates exactly 550.0 nm wavelength");

            // 4. Runtime Controller Lifecycle & Platform Registry
            ColorPlatform.ClearRegistry();
            GameObject finaleGo = new GameObject("TestEasyNewtonFinale");
            EasyNewtonFinaleController ctrl = finaleGo.AddComponent<EasyNewtonFinaleController>();
            ctrl.Initialize(null, null);

            AssertTrue(EasyNewtonFinaleController.Instance != null, "FinaleLifecycle_InstanceRegistered", "Singleton instance is registered");
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureK, "FinaleLifecycle_InitialStep", "Initial step is CaptureK");
            AssertTrue(!ctrl.HasK && !ctrl.HasI && !ctrl.HasKp, "FinaleLifecycle_NoInitialCaptures", "No points captured initially");

            // Check platforms registered in ColorPlatform.All
            bool foundK = false;
            bool foundI = false;
            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                var p = ColorPlatform.All[i];
                if (p.gameObject.name.Contains("OrbitPlatform_K_White"))
                {
                    foundK = true;
                    AssertApproximately(255f, p.RawGrayscaleColor, 0.01f, "FinalePlat_WhiteColor", "Platform K has white color 255");
                    var traj = p.GetTrajectory();
                    AssertTrue(traj != null && traj.Config != null && traj.Config.curveType == TrajectoryCurveType.Circle, "FinalePlat_WhiteTrajectoryCircle", "Platform K has Circle trajectory");
                    AssertApproximately(6.875f, traj.Config.radius, 0.001f, "FinalePlat_WhiteTrajectoryRadius", "Platform K radius is 6.875");
                    AssertApproximately(10.8f, traj.Config.duration, 0.001f, "FinalePlat_WhiteTrajectoryPeriod", "Platform K period is 10.8s");
                }
                else if (p.gameObject.name.Contains("OrbitPlatform_I_Gray"))
                {
                    foundI = true;
                    AssertApproximately(128f, p.RawGrayscaleColor, 0.01f, "FinalePlat_GrayColor", "Platform I has gray color 128");
                    var traj = p.GetTrajectory();
                    AssertTrue(traj != null && traj.Config != null && traj.Config.curveType == TrajectoryCurveType.Circle, "FinalePlat_GrayTrajectoryCircle", "Platform I has Circle trajectory");
                    AssertApproximately(4.86134f, traj.Config.radius, 0.01f, "FinalePlat_GrayTrajectoryRadius", "Platform I radius is 4.86134");
                    AssertApproximately(8.6f, traj.Config.duration, 0.001f, "FinalePlat_GrayTrajectoryPeriod", "Platform I period is 8.6s");
                }
            }
            AssertTrue(foundK, "FinalePlat_WhitePlatformRegistered", "OrbitPlatform_K_White registered in ColorPlatform.All");
            AssertTrue(foundI, "FinalePlat_GrayPlatformRegistered", "OrbitPlatform_I_Gray registered in ColorPlatform.All");

            // 5. Test Teardown
            ctrl.Teardown();
            UnityEngine.Object.DestroyImmediate(finaleGo);
            AssertTrue(EasyNewtonFinaleController.Instance == null, "FinaleLifecycle_InstanceCleared", "Singleton instance cleared after teardown");
            AssertTrue(ColorPlatform.All.Count == 0, "FinaleLifecycle_RegistryClean", "ColorPlatform.All is empty after teardown");
        }

        private static void Test_PlatformInputAdapters_And_Equivalence()
        {
            Debug.Log("[TEST_CASE] Starting Test_PlatformInputAdapters_And_Equivalence");

            // 1. Test StandaloneKeyboardInputAdapter contract
            StandaloneKeyboardInputAdapter kb = new StandaloneKeyboardInputAdapter();
            AssertApproximately(0f, kb.GetHorizontalAxis(), 0.001f, "KeyboardAdapter_NeutralAxis", "Default keyboard axis is 0");
            AssertTrue(!kb.GetJumpDown(), "KeyboardAdapter_NeutralJump", "Default keyboard jump is false");
            AssertTrue(kb.GetColorChunkStep() == 0, "KeyboardAdapter_NeutralColorStep", "Default color step is 0");
            AssertTrue(!kb.TryGetDirectColor(out _), "KeyboardAdapter_NeutralDirectColor", "Default direct color is false");

            // 2. Test MobileTouchInputAdapter contract
            MobileTouchInputAdapter touch = new MobileTouchInputAdapter();
            AssertApproximately(0f, touch.GetHorizontalAxis(), 0.001f, "TouchAdapter_InitAxis", "Initial touch axis is 0");

            touch.SetMoveLeft(true);
            AssertApproximately(-1f, touch.GetHorizontalAxis(), 0.001f, "TouchAdapter_MoveLeft", "Touch move left produces -1");

            touch.SetMoveLeft(false);
            touch.SetMoveRight(true);
            AssertApproximately(1f, touch.GetHorizontalAxis(), 0.001f, "TouchAdapter_MoveRight", "Touch move right produces +1");
            touch.SetMoveRight(false);

            touch.TriggerJump();
            AssertTrue(touch.GetJumpDown(), "TouchAdapter_JumpTriggered", "TriggerJump produces GetJumpDown == true");
            touch.ResetFrameState();
            AssertTrue(!touch.GetJumpDown(), "TouchAdapter_JumpReset", "ResetFrameState resets jump to false");

            touch.TriggerColorChunkStep(1);
            AssertTrue(touch.GetColorChunkStep() == 1, "TouchAdapter_ColorStepTriggered", "TriggerColorChunkStep(1) produces 1");
            touch.ResetFrameState();
            AssertTrue(touch.GetColorChunkStep() == 0, "TouchAdapter_ColorStepReset", "ResetFrameState resets color step to 0");

            touch.TriggerDirectColor(LogicalColor.Black);
            AssertTrue(touch.TryGetDirectColor(out LogicalColor directCol) && directCol == LogicalColor.Black, "TouchAdapter_DirectColor", "TriggerDirectColor(Black) produces Black");
            touch.ResetFrameState();
            AssertTrue(!touch.TryGetDirectColor(out _), "TouchAdapter_DirectColorReset", "ResetFrameState resets direct color");

            // 3. Test Cross-Platform Input Equivalence (Test D)
            // Execute equivalent command sequences through desktop adapter and mobile adapter:
            GameObject inputGo = new GameObject("TestPlayerInputHost");
            PlayerInput playerInput = inputGo.AddComponent<PlayerInput>();

            float deltaTime = 0.0166667f;
            float moveSpeed = 6f;

            // Scenario A: Move Right Intent
            MockControlledInputAdapter mockDesktop = new MockControlledInputAdapter { Axis = 1f, Jump = false };
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopMoveIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);

            touch.SetMoveRight(true);
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileMoveIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);
            touch.SetMoveRight(false);

            AssertApproximately(desktopMoveIntent.Move.x, mobileMoveIntent.Move.x, 0.0001f, "InputEquivalence_MoveX", "Desktop Move.x matches Mobile Move.x exactly");
            AssertApproximately(desktopMoveIntent.Move.y, mobileMoveIntent.Move.y, 0.0001f, "InputEquivalence_MoveY", "Desktop Move.y matches Mobile Move.y exactly");

            // Scenario B: Jump when Grounded
            mockDesktop.Axis = 0f;
            mockDesktop.Jump = true;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopJumpGrounded = playerInput.ReadInput(deltaTime, moveSpeed, true);

            touch.TriggerJump();
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileJumpGrounded = playerInput.ReadInput(deltaTime, moveSpeed, true);

            AssertTrue(desktopJumpGrounded.JumpRequested == mobileJumpGrounded.JumpRequested && mobileJumpGrounded.JumpRequested,
                "InputEquivalence_JumpGrounded", "Desktop and Mobile both request jump when grounded");

            // Scenario C: Jump when Airborne (should NOT trigger jump intent)
            mockDesktop.Jump = true;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopJumpAirborne = playerInput.ReadInput(deltaTime, moveSpeed, false);

            touch.TriggerJump();
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileJumpAirborne = playerInput.ReadInput(deltaTime, moveSpeed, false);

            AssertTrue(!desktopJumpAirborne.JumpRequested && !mobileJumpAirborne.JumpRequested,
                "InputEquivalence_JumpAirborne", "Desktop and Mobile both reject jump when airborne");

            // Scenario D: Color Shift to Gray
            mockDesktop.Jump = false;
            mockDesktop.HasDirectColor = true;
            mockDesktop.DirectColor = LogicalColor.Gray;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopColorIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);

            touch.TriggerDirectColor(LogicalColor.Gray);
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileColorIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);

            AssertTrue(desktopColorIntent.HasColorChange == mobileColorIntent.HasColorChange && mobileColorIntent.HasColorChange,
                "InputEquivalence_ColorChangeFlag", "Desktop and Mobile both signal HasColorChange");
            AssertTrue(desktopColorIntent.RequestedColor == mobileColorIntent.RequestedColor && mobileColorIntent.RequestedColor == LogicalColor.Gray,
                "InputEquivalence_ColorValue", "Desktop and Mobile both request Gray color");

            // Scenario E: Fast Fall (S key / mobile touch)
            mockDesktop.HasDirectColor = false;
            mockDesktop.FastFall = true;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopFastFall = playerInput.ReadInput(deltaTime, moveSpeed, false);

            touch.SetFastFall(true);
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileFastFall = playerInput.ReadInput(deltaTime, moveSpeed, false);
            touch.SetFastFall(false);

            AssertTrue(desktopFastFall.FastFall && mobileFastFall.FastFall,
                "InputEquivalence_FastFall", "Desktop and Mobile both signal FastFall");

            // Scenario F: Continuous Phase Tuning (Left/Right Arrow / mobile phase touch)
            mockDesktop.FastFall = false;
            mockDesktop.PhaseTuneAxis = 1f;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopPhaseIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);

            touch.SetPhaseTuneRight(true);
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobilePhaseIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);
            touch.SetPhaseTuneRight(false);

            AssertApproximately(desktopPhaseIntent.PhaseTuneDelta, mobilePhaseIntent.PhaseTuneDelta, 0.0001f,
                "InputEquivalence_PhaseTuneDelta", "Desktop and Mobile PhaseTuneDelta match");

            // Scenario G: Newton Marking Request (X key / mobile mark button)
            mockDesktop.PhaseTuneAxis = 0f;
            mockDesktop.NewtonMark = true;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent desktopNewtonMark = playerInput.ReadInput(deltaTime, moveSpeed, true);

            touch.TriggerNewtonMark();
            playerInput.SetCustomAdapter(touch);
            PlayerInputIntent mobileNewtonMark = playerInput.ReadInput(deltaTime, moveSpeed, true);

            AssertTrue(desktopNewtonMark.NewtonMarkRequested && mobileNewtonMark.NewtonMarkRequested,
                "InputEquivalence_NewtonMark", "Desktop and Mobile both signal NewtonMarkRequested on X / Mark button");

            // Space key must NEVER trigger NewtonMarkRequested
            mockDesktop.NewtonMark = false;
            mockDesktop.Jump = true;
            playerInput.SetCustomAdapter(mockDesktop);
            PlayerInputIntent spaceIntent = playerInput.ReadInput(deltaTime, moveSpeed, true);
            AssertTrue(!spaceIntent.NewtonMarkRequested, "Space_Never_Triggers_NewtonMark", "Space key strictly jumps and never triggers NewtonMark");

            UnityEngine.Object.DestroyImmediate(inputGo);
        }

        private static void Test_StandaloneLevelEditor_Lifecycle()
        {
            Debug.Log("[TEST_CASE] Starting Test_StandaloneLevelEditor_Lifecycle");

            // 1. Directory & Persistence resolution
            string dir = LevelRepository.GetLevelsDirectory();
            AssertTrue(!string.IsNullOrEmpty(dir), "LevelRepo_DirectoryResolved", "Levels directory resolved properly");

            LevelRepository.EnsureDefaultLevelFile();
            string[] files = LevelRepository.ListLevelFiles();
            AssertTrue(files != null && files.Length > 0, "LevelRepo_FilesDiscovered", "ListLevelFiles finds seeded default levels");

            string uniqueName = LevelRepository.GenerateUniqueFileName("test_prefix_");
            AssertTrue(uniqueName.StartsWith("test_prefix_") && uniqueName.EndsWith(".json"), "LevelRepo_UniqueNameFormat", "Unique filename has proper prefix and extension");

            // 2. Standalone Bootstrapper lifecycle
            GameObject editorRoot = new GameObject("TestStandaloneEditorRoot");
            StandaloneLevelEditorBootstrapper bootstrapper = editorRoot.AddComponent<StandaloneLevelEditorBootstrapper>();
            LevelLoader loader = editorRoot.AddComponent<LevelLoader>();
            LevelEditorController ctrl = editorRoot.AddComponent<LevelEditorController>();
            LevelEditorUI ui = editorRoot.AddComponent<LevelEditorUI>();

            AssertTrue(bootstrapper != null, "StandaloneEditor_BootstrapperCreated", "Bootstrapper component created");
            AssertTrue(loader != null && ctrl != null && ui != null, "StandaloneEditor_ComponentsPresent", "Editor infrastructure present without GameManager");

            // 3. Test Level Export & Import Interoperability
            string programmaticJson = LevelRepository.GenerateProgrammaticDefaultJson();
            AssertTrue(!string.IsNullOrEmpty(programmaticJson), "LevelRepo_ProgrammaticJsonValid", "Generated programmatic default JSON is valid");

            LevelData parsed = JsonUtility.FromJson<LevelData>(programmaticJson);
            AssertTrue(parsed != null && parsed.platforms != null && parsed.platforms.Length > 0, "LevelRepo_JsonParsesProperly", "LevelData deserializes successfully");

            UnityEngine.Object.DestroyImmediate(editorRoot);
        }

        private static void Test_CampaignLevels_JSON_Validation()
        {
            Debug.Log("[TEST_CASE] Starting Test_CampaignLevels_JSON_Validation");

            string[] levels = new[] { "L01", "L02", "L03" };
            foreach (string lvl in levels)
            {
                string path = Path.Combine(Application.dataPath, "Resources", "Levels", $"{lvl}.json");
                AssertTrue(File.Exists(path), $"{lvl}_File_Exists", $"Resource file {lvl}.json exists on disk");

                string json = File.ReadAllText(path);
                AssertTrue(!string.IsNullOrEmpty(json), $"{lvl}_JSON_NotEmpty", $"{lvl}.json has content");

                LevelData data = JsonUtility.FromJson<LevelData>(json);
                AssertTrue(data != null, $"{lvl}_JSON_Parses", $"{lvl}.json deserializes to LevelData");

                LevelData sanitized = LevelDataSanitizer.Sanitize(data, json, out List<string> report);
                AssertTrue(sanitized != null, $"{lvl}_Sanitizes_Successfully", $"{lvl} sanitization succeeds");

                if (lvl == "L01")
                {
                    AssertTrue(sanitized.platforms != null && sanitized.platforms.Length == 16, "L01_BlockCount_16", $"L01 has 16 terrain blocks (actual: {sanitized.platforms?.Length})");
                    int w = 0, g = 0, b = 0, f = 0;
                    foreach (var p in sanitized.platforms)
                    {
                        if (p.rawGrayscaleColor >= 254.5f) f++;
                        else if (p.rawGrayscaleColor <= 84.5f) w++;
                        else if (p.rawGrayscaleColor <= 169.5f) g++;
                        else b++;
                    }
                    AssertTrue(w == 6, "L01_WhiteBlocks_6", $"L01 White blocks == 6 (actual: {w})");
                    AssertTrue(g == 4, "L01_GrayBlocks_4", $"L01 Gray blocks == 4 (actual: {g})");
                    AssertTrue(b == 5, "L01_BlackBlocks_5", $"L01 Black blocks == 5 (actual: {b})");
                    AssertTrue(f == 1, "L01_FixedBlocks_1", $"L01 Fixed blocks == 1 (actual: {f})");

                    AssertTrue(sanitized.checkPoint != null && sanitized.checkPoint.Length == 5, "L01_Checkpoints_5", "L01 has 5 checkpoints (+ startPos = 6)");
                    AssertTrue(sanitized.collectibles != null && sanitized.collectibles.Length == 2, "L01_Collectibles_2", "L01 has 2 collectibles");
                    AssertTrue(sanitized.newton != null && sanitized.newton.i == 3 && sanitized.newton.k == 6, "L01_Newton_Config", "L01 Newton i=3, k=6");
                    AssertTrue(sanitized.newton.orbitStart == "auto", "L01_Newton_OrbitStart_Auto", "L01 orbitStart == auto");
                }
                else if (lvl == "L02")
                {
                    AssertTrue(sanitized.platforms != null && sanitized.platforms.Length == 15, "L02_BlockCount_15", $"L02 has 15 terrain blocks (actual: {sanitized.platforms?.Length})");
                    int w = 0, g = 0, b = 0, f = 0;
                    foreach (var p in sanitized.platforms)
                    {
                        if (p.rawGrayscaleColor >= 254.5f) f++;
                        else if (p.rawGrayscaleColor <= 84.5f) w++;
                        else if (p.rawGrayscaleColor <= 169.5f) g++;
                        else b++;
                    }
                    AssertTrue(w == 3, "L02_WhiteBlocks_3", $"L02 White blocks == 3 (actual: {w})");
                    AssertTrue(g == 2, "L02_GrayBlocks_2", $"L02 Gray blocks == 2 (actual: {g})");
                    AssertTrue(b == 8, "L02_BlackBlocks_8", $"L02 Black blocks == 8 (actual: {b})");
                    AssertTrue(f == 2, "L02_FixedBlocks_2", $"L02 Fixed blocks == 2 (actual: {f})");

                    AssertTrue(sanitized.checkPoint != null && sanitized.checkPoint.Length == 4, "L02_Checkpoints_4", "L02 has 4 checkpoints (+ startPos = 5)");
                    AssertTrue(sanitized.collectibles != null && sanitized.collectibles.Length == 2, "L02_Collectibles_2", "L02 has 2 collectibles");
                    AssertTrue(sanitized.newton != null && sanitized.newton.i == 2 && sanitized.newton.k == 4, "L02_Newton_Config", "L02 Newton i=2, k=4");
                    AssertTrue(sanitized.newton.orbitStart == "dock", "L02_Newton_OrbitStart_Dock", "L02 orbitStart == dock");
                }
                else if (lvl == "L03")
                {
                    AssertTrue(sanitized.platforms != null && sanitized.platforms.Length == 27, "L03_BlockCount_27", $"L03 has 27 terrain blocks (actual: {sanitized.platforms?.Length})");
                    int w = 0, g = 0, b = 0, f = 0;
                    int movingCount = 0;
                    foreach (var p in sanitized.platforms)
                    {
                        if (p.rawGrayscaleColor >= 254.5f) f++;
                        else if (p.rawGrayscaleColor <= 84.5f) w++;
                        else if (p.rawGrayscaleColor <= 169.5f) g++;
                        else b++;

                        if (p.trajectory != null && p.trajectory.enabled)
                            movingCount++;
                    }
                    AssertTrue(w == 5, "L03_WhiteBlocks_5", $"L03 White blocks == 5 (actual: {w})");
                    AssertTrue(g == 8, "L03_GrayBlocks_8", $"L03 Gray blocks == 8 (actual: {g})");
                    AssertTrue(b == 5, "L03_BlackBlocks_5", $"L03 Black blocks == 5 (actual: {b})");
                    AssertTrue(f == 9, "L03_FixedBlocks_9", $"L03 Fixed blocks == 9 (actual: {f})");
                    AssertTrue(movingCount == 3, "L03_MovingPlatforms_3", $"L03 moving platforms == 3 (actual: {movingCount})");

                    AssertTrue(sanitized.checkPoint != null && sanitized.checkPoint.Length == 8, "L03_Checkpoints_8", "L03 has 8 checkpoints (+ startPos = 9)");
                    AssertTrue(sanitized.collectibles != null && sanitized.collectibles.Length == 3, "L03_Collectibles_3", "L03 has 3 collectibles");
                    AssertTrue(sanitized.newton != null && sanitized.newton.i == 2 && sanitized.newton.k == 7, "L03_Newton_Config", "L03 Newton i=2, k=7");
                    AssertTrue(sanitized.newton.orbitStart == "auto", "L03_Newton_OrbitStart_Auto", "L03 orbitStart == auto");
                }
            }
        }

        private static void Test_Newton_Marking_Sequence_And_Tolerances()
        {
            Debug.Log("[TEST_CASE] Starting Test_Newton_Marking_Sequence_And_Tolerances");

            GameObject newtonGo = new GameObject("TestNewtonSequenceController");
            EasyNewtonFinaleController ctrl = newtonGo.AddComponent<EasyNewtonFinaleController>();

            GameObject playerGo = new GameObject("TestPlayer");
            PlayerController player = playerGo.AddComponent<PlayerController>();

            NewtonChamberData chamberData = new NewtonChamberData
            {
                cx = 5135f,
                cy = 659f,
                i = 3,
                k = 6,
                orbitStart = "auto",
                rails = new NewtonRailsConfigData
                {
                    K = new NewtonRailData { width = 136f, period = 10.8f, angleDeg = 0f, direction = 1 },
                    I = new NewtonRailData { width = 126f, period = 8.6f, angleDeg = 0f, direction = 1 }
                }
            };

            ctrl.Initialize(player, null, chamberData);

            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureK, "Newton_InitialStep_WaitingForK", "Initial measurement step is CaptureK");
            AssertTrue(ctrl.MarkerCount == 0, "Newton_InitialMarkers_Zero", "No markers locked initially");

            // 1. Pressing mark when NOT grounded should fail
            player.SetGroundedForTest(false, null);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureK, "Newton_MarkUngrounded_Fails", "Mark attempt when ungrounded remains in CaptureK");
            AssertTrue(ctrl.MarkerCount == 0, "Newton_MarkerCount_StillZero", "Marker count still 0 after invalid attempt");

            // 2. Put player on rail I (wrong rail for step 1)
            player.SetGroundedForTest(true, ctrl.PlatformI);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureK, "Newton_MarkWrongPlatform_Fails", "Mark attempt on rail I for Step K remains in CaptureK");
            AssertTrue(ctrl.MarkerCount == 0, "Newton_MarkerCount_StillZero2", "Marker count still 0");

            // 3. Put player on rail K and mark
            player.SetGroundedForTest(true, ctrl.PlatformK);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureI, "Newton_StepAdvancesTo_CaptureI", "Marking on K establishes reference axis and advances to CaptureI");
            AssertTrue(ctrl.MarkerCount == 1, "Newton_MarkerK_Locked", "Marker K successfully locked");
            AssertTrue(ctrl.HasK, "Newton_HasK_True", "HasK flag is true");

            // 4. Mark on rail I when misaligned (> toleranceI = 0.75 units = 48 px)
            float rI = ctrl.GetRadiusForOrder(3);
            Vector2 misalignedPosI = ctrl.CenterO + new Vector2(ctrl.AxisU.y, -ctrl.AxisU.x) * rI;
            ctrl.PlatformI.transform.position = new Vector3(misalignedPosI.x, misalignedPosI.y, 0f);
            player.SetGroundedForTest(true, ctrl.PlatformI);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureI, "Newton_MisalignedI_Rejected", "Misaligned mark on I is rejected within 48px tolerance");
            AssertTrue(ctrl.MarkerCount == 1, "Newton_MarkerI_NotLockedYet", "Marker count remains 1");

            // 5. Mark on rail I when aligned (within 48px tolerance)
            Vector2 alignedPosI = ctrl.CenterO - ctrl.AxisU * rI;
            ctrl.PlatformI.transform.position = new Vector3(alignedPosI.x, alignedPosI.y, 0f);
            player.SetGroundedForTest(true, ctrl.PlatformI);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureKp, "Newton_AlignedI_Accepted", "Aligned mark on I advances to CaptureKp");
            AssertTrue(ctrl.MarkerCount == 2, "Newton_MarkerI_Locked", "Marker I successfully locked");
            AssertTrue(ctrl.HasI, "Newton_HasI_True", "HasI flag is true");

            // 6. Mark on rail K for K' when misaligned (> 54 px = 0.84375 units)
            float rK = ctrl.GetRadiusForOrder(6);
            Vector2 misalignedPosK = ctrl.CenterO + ctrl.AxisU * rK;
            ctrl.PlatformK.transform.position = new Vector3(misalignedPosK.x, misalignedPosK.y, 0f);
            player.SetGroundedForTest(true, ctrl.PlatformK);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.CaptureKp, "Newton_MisalignedKp_Rejected", "Misaligned mark on K' is rejected within 54px tolerance");
            AssertTrue(ctrl.MarkerCount == 2, "Newton_MarkerKp_NotLockedYet", "Marker count remains 2");

            // 7. Mark on rail K for K' when aligned (within 54px tolerance)
            Vector2 alignedPosKp = ctrl.CenterO - ctrl.AxisU * rK;
            ctrl.PlatformK.transform.position = new Vector3(alignedPosKp.x, alignedPosKp.y, 0f);
            player.SetGroundedForTest(true, ctrl.PlatformK);
            ctrl.AttemptCapture();
            AssertTrue(ctrl.CurrentStep == NewtonFinaleStep.Reveal, "Newton_AlignedKp_Accepted", "Aligned mark on K' advances to Reveal");
            AssertTrue(ctrl.MarkerCount == 3, "Newton_MarkerKp_Locked", "Marker K' successfully locked (3 total markers)");
            AssertTrue(ctrl.HasKp, "Newton_HasKp_True", "HasKp flag is true");
            AssertTrue(ctrl.IsRevealActive, "Newton_IsRevealActive_True", "IsRevealActive is true");

            // Clean up
            ctrl.Teardown();
            UnityEngine.Object.DestroyImmediate(newtonGo);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        private sealed class MockControlledInputAdapter : IPlatformInputAdapter
        {
            public float Axis;
            public bool Jump;
            public bool FastFall;
            public float PhaseTuneAxis;
            public bool NewtonMark;
            public int ColorStep;
            public bool HasDirectColor;
            public LogicalColor DirectColor;

            public float GetHorizontalAxis() => Axis;
            public bool GetJumpDown() => Jump;
            public bool GetFastFall() => FastFall;
            public float GetPhaseTuneAxis() => PhaseTuneAxis;
            public bool GetNewtonMarkDown() => NewtonMark;
            public int GetColorChunkStep() => ColorStep;
            public bool TryGetDirectColor(out LogicalColor color)
            {
                color = DirectColor;
                return HasDirectColor;
            }
            public void ResetFrameState()
            {
                Jump = false;
                FastFall = false;
                PhaseTuneAxis = 0f;
                NewtonMark = false;
                ColorStep = 0;
                HasDirectColor = false;
            }
        }
    }
}

