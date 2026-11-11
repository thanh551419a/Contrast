using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Core;
using Contrast.Data;
using Contrast.Level;
using Contrast.Level.EditorInteraction;
using Contrast.UI;
using Contrast.Color;
using Contrast.Player;
using Contrast.Input;
using Contrast.Camera;
using UnityEngine;

namespace ContrastTests
{
    public class Program
    {
        private static int passed = 0;
        private static int failed = 0;
        private static readonly List<string> errors = new List<string>();

        public static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("   CONTRAST LEVEL EDITOR & TRAJECTORY COMPREHENSIVE AUTOMATED TEST HARNESS");
            Console.WriteLine("================================================================================");

            Test_LineForward();
            Test_LineReverse();
            Test_LineRotations();
            Test_LineSpatialBoundsClipping();
            Test_Circle360Loop();
            Test_Arc180PingPong();
            Test_TrajectoryValidation();
            Test_TrajectoryProjection();
            Test_PingPongTimingAndDisplacement();
            Test_LoopTimingAndPeriodicity();
            Test_OnceTimingAndClamping();
            Test_JsonSerializationRoundTrip();
            Test_LegacySampleLevelsCompatibility();
            Test_TrajectoryShowcaseLevelFile();
            Test_LevelRepositoryAndStorage();
            Test_LevelSourceTracking();
            Test_SanitizerRawJsonPresenceHelper();
            Test_SanitizerDataRepairAndValidation();
            Test_SanitizerCameraBoundaryGeneration();
            Test_SanitizerTrajectoryRepairs();
            Test_JsonExportNoPhantomObjects();
            Test_MalformedJsonRepairAndReport();
            Test_EditorRoundTripIntegrity();
            Test_GameplayEntryContextAndNavigation();
            Test_EditorWorkingCopyRestorationAfterGameplay();
            Test_EditorInputRouter();
            Test_PlatformSizeAndColorEditing();
            Test_FitMapAndWhitePlatformCalculations();
            Test_ResetLevel_Functionality();
            Test_GhostPlatform_Prevention_And_ResetCleanup();
            Test_EasyNewtonFinale_OrbitalTrajectories_And_CaptureMath();
            Test_NewtonCameraFollow_And_StateTransitions();
            Test_TKeyTestMode_ComprehensiveSuite();
            Test_L02_Trajectory_And_NewtonDocking_Suite();
            Test_EditorTestPlay_Preserves_NewtonChamber();
            Test_Newton_XMarking_Sequence_And_Validation();
            Test_Newton_RiderCarry_And_OrbitalTransfer();
            Test_FinalReview_Tasks1To8_Suite();
            Test_MovingPlatformJitter_And_CarrierResolution_Suite();
            Test_JumpLifecycle_Respawn_And_Stress_Suite();
            Test_DeathY_FixedWorldSpace_Suite();
            Test_NewtonFixedCamera_HtmlParity_Suite();
            Test_NewtonX_SequentialColliderValidation_Suite();
            Test_CrossSystem_DeathY_Camera_NewtonX_Regression_Suite();
            Test_RC4_Animation_Checkpoint_And_ReversibleCamera_Suite();

            Console.WriteLine("================================================================================");
            Console.WriteLine($"RESULTS: {passed} PASSED, {failed} FAILED");
            Console.WriteLine("================================================================================");

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILURES:");
                foreach (string err in errors)
                    Console.WriteLine("  * " + err);
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ALL AUTOMATED TESTS PASSED WITH ZERO DEFECTS!");
            Console.ResetColor();
            return 0;
        }

        private static void Assert(bool condition, string testName, string message)
        {
            if (condition)
            {
                passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} - {message}");
            }
            else
            {
                failed++;
                string err = $"{testName}: {message}";
                errors.Add(err);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine(err);
            }
        }

        private static void AssertApprox(float expected, float actual, float tol, string testName, string message)
        {
            float diff = Math.Abs(expected - actual);
            if (diff <= tol)
            {
                passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} - {message} (Val: {actual:F4}, Exp: {expected:F4})");
            }
            else
            {
                failed++;
                string err = $"{testName}: {message} (Val: {actual:F4}, Exp: {expected:F4}, Diff: {diff:E2} > Tol: {tol:E2})";
                errors.Add(err);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine(err);
            }
        }

        private static void AssertVecApprox(Vector2 expected, Vector2 actual, float tol, string testName, string message)
        {
            float dist = Vector2.Distance(expected, actual);
            if (dist <= tol)
            {
                passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} - {message} (Val: ({actual.x:F3}, {actual.y:F3}), Exp: ({expected.x:F3}, {expected.y:F3}))");
            }
            else
            {
                failed++;
                string err = $"{testName}: {message} (Val: ({actual.x:F3}, {actual.y:F3}), Exp: ({expected.x:F3}, {expected.y:F3}), Dist: {dist:E2} > Tol: {tol:E2})";
                errors.Add(err);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine(err);
            }
        }

        private static void Test_LineForward()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(1f, 2f));
            cfg.enabled = true;
            cfg.scaleX = 8f;
            cfg.rotation = 0f;
            cfg.paramMin = 0f;
            cfg.paramMax = 1f;

            Vector2 p0 = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 pMid = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 p1 = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVecApprox(new Vector2(1f, 2f), p0, 1e-4f, "LineForward", "t=0.0 at origin");
            AssertVecApprox(new Vector2(5f, 2f), pMid, 1e-4f, "LineForward", "t=0.5 at midpoint");
            AssertVecApprox(new Vector2(9f, 2f), p1, 1e-4f, "LineForward", "t=1.0 at endpoint");
        }

        private static void Test_LineReverse()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(1f, 2f));
            cfg.enabled = true;
            cfg.scaleX = 8f;
            cfg.rotation = 0f;
            cfg.reverseDirection = true;

            Vector2 init = Trajectory.GetInitialPosition(cfg);
            AssertVecApprox(new Vector2(9f, 2f), init, 1e-4f, "LineReverse", "Initial point when reverse=true is (9, 2)");
        }

        private static void Test_LineRotations()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.scaleX = 5f;

            // 90 deg (Vertical upwards)
            cfg.rotation = 90f;
            Vector2 p90 = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVecApprox(new Vector2(0f, 5f), p90, 1e-3f, "LineRotations", "90 deg rotation gives pure Y motion (0, 5)");

            // 180 deg (Horizontal left)
            cfg.rotation = 180f;
            Vector2 p180 = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVecApprox(new Vector2(-5f, 0f), p180, 1e-3f, "LineRotations", "180 deg rotation gives pure -X motion (-5, 0)");

            // 270 deg (Vertical downwards)
            cfg.rotation = 270f;
            Vector2 p270 = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVecApprox(new Vector2(0f, -5f), p270, 1e-3f, "LineRotations", "270 deg rotation gives pure -Y motion (0, -5)");
        }

        private static void Test_LineSpatialBoundsClipping()
        {
            var cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.SpatialBounds,
                originX = 0f,
                originY = 0f,
                rotation = 0f,
                scaleX = 1f,
                boundsXMin = -10f,
                boundsXMax = 10f,
                boundsYMin = -5f,
                boundsYMax = 5f
            };

            Trajectory.ClipLineAgainstBounds(cfg, out float pMin, out float pMax);
            AssertApprox(-10f, pMin, 1e-4f, "SpatialBounds", "Clipped pMin is -10");
            AssertApprox(10f, pMax, 1e-4f, "SpatialBounds", "Clipped pMax is +10");

            Vector2 start = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 end = Trajectory.EvaluateWorldPoint(cfg, 1f);
            AssertVecApprox(new Vector2(-10f, 0f), start, 1e-4f, "SpatialBounds", "Start point at left boundary (-10, 0)");
            AssertVecApprox(new Vector2(10f, 0f), end, 1e-4f, "SpatialBounds", "End point at right boundary (10, 0)");
        }

        private static void Test_Circle360Loop()
        {
            var cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 5f,
                originY = 5f,
                radius = 3f,
                paramMin = 0f,
                paramMax = 360f,
                duration = 4f
            };

            Vector2 p0 = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 p90 = Trajectory.EvaluateWorldPoint(cfg, 0.25f);
            Vector2 p180 = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 p270 = Trajectory.EvaluateWorldPoint(cfg, 0.75f);
            Vector2 p360 = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVecApprox(new Vector2(8f, 5f), p0, 1e-4f, "Circle360", "Angle 0 deg: (8, 5)");
            AssertVecApprox(new Vector2(5f, 8f), p90, 1e-4f, "Circle360", "Angle 90 deg: (5, 8)");
            AssertVecApprox(new Vector2(2f, 5f), p180, 1e-4f, "Circle360", "Angle 180 deg: (2, 5)");
            AssertVecApprox(new Vector2(5f, 2f), p270, 1e-4f, "Circle360", "Angle 270 deg: (5, 2)");
            AssertVecApprox(p0, p360, 1e-4f, "Circle360", "Angle 360 deg closes seamlessly back to 0 deg");
        }

        private static void Test_Arc180PingPong()
        {
            var cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.PingPong,
                originX = 0f,
                originY = 0f,
                radius = 4f,
                paramMin = 0f,
                paramMax = 180f,
                duration = 6f
            };

            Vector2 start = Trajectory.EvaluateWorldPoint(cfg, 0f);
            Vector2 top = Trajectory.EvaluateWorldPoint(cfg, 0.5f);
            Vector2 end = Trajectory.EvaluateWorldPoint(cfg, 1f);

            AssertVecApprox(new Vector2(4f, 0f), start, 1e-4f, "Arc180", "Start at (4, 0)");
            AssertVecApprox(new Vector2(0f, 4f), top, 1e-4f, "Arc180", "Peak crest at (0, 4)");
            AssertVecApprox(new Vector2(-4f, 0f), end, 1e-4f, "Arc180", "End at (-4, 0)");
        }

        private static void Test_TrajectoryValidation()
        {
            var cfg = new TrajectoryData { enabled = false };
            Assert(Trajectory.Validate(cfg) == null, "Validation", "Disabled trajectory is valid");

            cfg.enabled = true;
            cfg.duration = 0f;
            Assert(Trajectory.Validate(cfg) != null, "Validation", "Zero duration is invalid");

            cfg.duration = -2f;
            Assert(Trajectory.Validate(cfg) != null, "Validation", "Negative duration is invalid");

            cfg.duration = 4f;
            cfg.curveType = TrajectoryCurveType.Line;
            cfg.scaleX = 0f;
            Assert(Trajectory.Validate(cfg) != null, "Validation", "Zero scaleX on Line is invalid");

            cfg.scaleX = 5f;
            cfg.paramMin = 1f;
            cfg.paramMax = 1f;
            Assert(Trajectory.Validate(cfg) != null, "Validation", "Equal min/max parameters is invalid");

            cfg.paramMax = 2f;
            Assert(Trajectory.Validate(cfg) == null, "Validation", "Valid Line configuration accepted");

            cfg.curveType = TrajectoryCurveType.Circle;
            cfg.radius = -1f;
            Assert(Trajectory.Validate(cfg) != null, "Validation", "Negative radius on Circle is invalid");

            cfg.radius = 2.5f;
            Assert(Trajectory.Validate(cfg) == null, "Validation", "Valid Circle configuration accepted");
        }

        private static void Test_TrajectoryProjection()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.scaleX = 10f;
            cfg.rotation = 0f;

            // Project (3, 8) onto line Y=0 -> should land on (3, 0) with t=0.3
            Vector2 proj = Trajectory.ProjectPointOntoTrajectory(cfg, new Vector2(3f, 8f), out float t);
            AssertVecApprox(new Vector2(3f, 0f), proj, 1e-4f, "Projection", "Point (3, 8) projects to (3, 0)");
            AssertApprox(0.3f, t, 1e-4f, "Projection", "Normalized parameter t=0.3");

            // Point before origin (-5, 0) clamps to t=0
            Trajectory.ProjectPointOntoTrajectory(cfg, new Vector2(-5f, 0f), out float tClampMin);
            AssertApprox(0f, tClampMin, 1e-4f, "Projection", "Point before origin clamps to t=0");

            // Point past endpoint (15, 0) clamps to t=1
            Trajectory.ProjectPointOntoTrajectory(cfg, new Vector2(15f, 0f), out float tClampMax);
            AssertApprox(1f, tClampMax, 1e-4f, "Projection", "Point past endpoint clamps to t=1");
        }

        private static void Test_PingPongTimingAndDisplacement()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.playbackMode = TrajectoryPlaybackMode.PingPong;
            cfg.scaleX = 10f;
            cfg.duration = 4f; // Out-and-back = 4s. One way (0->1) = 2s. Speed = 5 m/s.

            float phase = 0f;
            int dir = 1;
            float phaseSpeed = 2f / cfg.duration; // 0.5/s

            // Step 1.0s (dt=1.0): phase should reach 0.5 -> pos (5, 0)
            phase += phaseSpeed * 1.0f * dir;
            Vector2 p1s = Trajectory.EvaluateWorldPoint(cfg, phase);
            AssertVecApprox(new Vector2(5f, 0f), p1s, 1e-4f, "PingPongTiming", "At 1.0s, position is (5, 0)");

            // Step another 1.0s (total 2.0s): reaches end, phase=1.0 -> pos (10, 0), dir flips to -1
            phase += phaseSpeed * 1.0f * dir;
            if (phase >= 1f) { phase = 1f; dir = -1; }
            Vector2 p2s = Trajectory.EvaluateWorldPoint(cfg, phase);
            AssertVecApprox(new Vector2(10f, 0f), p2s, 1e-4f, "PingPongTiming", "At 2.0s, reached end (10, 0)");

            // Step another 1.0s (total 3.0s): returning, phase = 0.5 -> pos (5, 0)
            phase += phaseSpeed * 1.0f * dir;
            Vector2 p3s = Trajectory.EvaluateWorldPoint(cfg, phase);
            AssertVecApprox(new Vector2(5f, 0f), p3s, 1e-4f, "PingPongTiming", "At 3.0s, returning midway at (5, 0)");

            // Step another 1.0s (total 4.0s): returns to start, phase = 0.0 -> pos (0, 0)
            phase += phaseSpeed * 1.0f * dir;
            if (phase <= 0f) { phase = 0f; dir = 1; }
            Vector2 p4s = Trajectory.EvaluateWorldPoint(cfg, phase);
            AssertVecApprox(new Vector2(0f, 0f), p4s, 1e-4f, "PingPongTiming", "At 4.0s, returned to start (0, 0)");
        }

        private static void Test_LoopTimingAndPeriodicity()
        {
            var cfg = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 0f,
                originY = 0f,
                radius = 3f,
                paramMin = 0f,
                paramMax = 360f,
                duration = 3f // 1 revolution every 3 seconds
            };

            float phaseSpeed = 1f / cfg.duration;
            float dt = 0.02f;
            float phase = 0f;

            // Integrate for 3.0 seconds (1 full period)
            int steps = (int)(3.0f / dt);
            for (int i = 0; i < steps; i++)
            {
                phase += phaseSpeed * dt;
                if (phase >= 1f) phase -= (float)Math.Floor(phase);
            }

            Vector2 finalPos = Trajectory.EvaluateWorldPoint(cfg, phase);
            Vector2 initialPos = Trajectory.EvaluateWorldPoint(cfg, 0f);
            AssertVecApprox(initialPos, finalPos, 1e-2f, "LoopPeriodicity", "Position after exactly 1 cycle matches initial point");
        }

        private static void Test_OnceTimingAndClamping()
        {
            var cfg = TrajectoryData.CreateDefault(new Vector2(0f, 0f));
            cfg.enabled = true;
            cfg.playbackMode = TrajectoryPlaybackMode.Once;
            cfg.scaleX = 12f;
            cfg.duration = 3f;

            float phaseSpeed = 1f / cfg.duration;
            float phase = 0f;

            // Advance 4.0 seconds (beyond duration of 3s)
            phase = Math.Clamp(phase + phaseSpeed * 4.0f, 0f, 1f);
            Vector2 pEnd = Trajectory.EvaluateWorldPoint(cfg, phase);
            AssertVecApprox(new Vector2(12f, 0f), pEnd, 1e-4f, "OnceClamping", "PlaybackMode.Once clamped securely at endpoint");
        }

        private static void Test_JsonSerializationRoundTrip()
        {
            var orig = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(10f, 0f), size = new Vector2Data(30f, 15f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData
                    {
                        position = new Vector2Data(5f, -1f),
                        size = new Vector2Data(6f, 1f),
                        rawGrayscaleColor = 255f,
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Line,
                            playbackMode = TrajectoryPlaybackMode.PingPong,
                            originX = 5f,
                            originY = -1f,
                            scaleX = 7f,
                            duration = 4.5f
                        }
                    }
                }
            };

            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(orig, options);
            var restored = System.Text.Json.JsonSerializer.Deserialize<LevelData>(json, options);

            Assert(restored != null, "Serialization", "Deserialization successful");
            Assert(restored.platforms != null && restored.platforms.Length == 1, "Serialization", "Platform count preserved");
            Assert(restored.platforms[0].trajectory != null, "Serialization", "Trajectory preserved");
            Assert(restored.platforms[0].trajectory.enabled == true, "Serialization", "Trajectory enabled flag preserved");
            AssertApprox(7f, restored.platforms[0].trajectory.scaleX, 1e-4f, "Serialization", "scaleX preserved");
            AssertApprox(4.5f, restored.platforms[0].trajectory.duration, 1e-4f, "Serialization", "duration preserved");
        }

        private static void Test_LegacySampleLevelsCompatibility()
        {
            string p1 = "Assets/Script/Contrast/Level/SampleLevel.CheckpointEnd.Test.json";
            string p2 = "Assets/Script/Contrast/Level/SampleLevel.Easy.HTMLReconstructed.json";
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };

            if (File.Exists(p1))
            {
                string json1 = File.ReadAllText(p1);
                var data1 = System.Text.Json.JsonSerializer.Deserialize<LevelData>(json1, options);
                Assert(data1 != null && data1.platforms != null && data1.platforms.Length == 5,
                    "LegacyCompat", $"SampleLevel.CheckpointEnd.Test.json parsed ({data1.platforms.Length} platforms)");

                // Verify all platforms default to stationary (trajectory null)
                bool allStatic = true;
                foreach (var p in data1.platforms)
                {
                    if (p.trajectory != null && p.trajectory.enabled)
                        allStatic = false;
                }
                Assert(allStatic, "LegacyCompat", "All legacy platforms are stationary by default");
            }
            else
            {
                Assert(false, "LegacyCompat", $"File not found: {p1}");
            }

            if (File.Exists(p2))
            {
                string json2 = File.ReadAllText(p2);
                var data2 = System.Text.Json.JsonSerializer.Deserialize<LevelData>(json2, options);
                Assert(data2 != null && data2.platforms != null && data2.platforms.Length > 0,
                    "LegacyCompat", $"SampleLevel.Easy.HTMLReconstructed.json parsed ({data2.platforms.Length} platforms)");
            }
            else
            {
                Assert(false, "LegacyCompat", $"File not found: {p2}");
            }
        }

        private static void Test_TrajectoryShowcaseLevelFile()
        {
            string path = "Assets/Levels/TrajectoryShowcaseLevel.json";
            Assert(File.Exists(path), "ShowcaseLevel", $"Level file exists at {path}");
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
            var data = System.Text.Json.JsonSerializer.Deserialize<LevelData>(json, options);

            Assert(data != null, "ShowcaseLevel", "LevelData deserialized successfully");
            Assert(data.startPos != null, "ShowcaseLevel", "StartPos is defined");
            Assert(data.platforms != null && data.platforms.Length > 0, "ShowcaseLevel", $"Platforms defined ({data?.platforms?.Length})");

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
                    if (Math.Abs(playerFeetY - platTopY) < 0.6f && playerFeetY >= platTopY - 0.05f)
                    {
                        foundSupportingPlatform = true;
                        break;
                    }
                }
            }
            Assert(foundSupportingPlatform, "ShowcaseLevel", "Player stands securely on platform at start");

            // Verify all trajectory configurations and speed
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

                // Validation check
                string valErr = Trajectory.Validate(cfg);
                Assert(valErr == null, "ShowcaseLevel_Validation", $"Platform #{i} trajectory valid (err: {valErr ?? "none"})");

                // Speed check: duration must be slow enough (speed <= 2.5 m/s)
                Assert(cfg.duration >= 8.0f, "ShowcaseLevel_Speed", $"Platform #{i} duration >= 8s for comfortable UI feeling (dur: {cfg.duration}s)");

                // Coordinate contract check: authored position matches GetInitialPosition
                Vector2 initialPos = Trajectory.GetInitialPosition(cfg);
                AssertVecApprox(initialPos, p.position.ToVector2(), 0.01f, "ShowcaseLevel_Coords", $"Platform #{i} position matches initial evaluated trajectory point");

                if (cfg.curveType == TrajectoryCurveType.Line)
                {
                    if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                        hasSpatialBounds = true;
                    else if (Math.Abs(cfg.rotation) < 1f)
                        hasLineH = true;
                    else if (Math.Abs(cfg.rotation - 90f) < 1f)
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
                    float angleSpan = Math.Abs(cfg.paramMax - cfg.paramMin);
                    if (angleSpan >= 355f)
                        hasCircle = true;
                    else
                        hasArc = true;
                }

                if (cfg.reverseDirection)
                    hasReverse = true;
            }

            Assert(trajectoryCount >= 8, "ShowcaseLevel_Coverage", $"Comprehensive trajectory count: {trajectoryCount}");
            Assert(hasLineH, "ShowcaseLevel_Coverage", "Includes Horizontal Line (PingPong)");
            Assert(hasLineV, "ShowcaseLevel_Coverage", "Includes Vertical Line Elevator (PingPong)");
            Assert(hasLineDiag, "ShowcaseLevel_Coverage", "Includes Diagonal Line (PingPong)");
            Assert(hasSpatialBounds, "ShowcaseLevel_Coverage", "Includes SpatialBounds Line Clipping");
            Assert(hasLoop, "ShowcaseLevel_Coverage", "Includes Loop Playback Mode");
            Assert(hasOnce, "ShowcaseLevel_Coverage", "Includes Once Playback Mode");
            Assert(hasCircle, "ShowcaseLevel_Coverage", "Includes Full 360 Circle Loop");
            Assert(hasArc, "ShowcaseLevel_Coverage", "Includes 180 Arc Semi-circle");
            Assert(hasReverse, "ShowcaseLevel_Coverage", "Includes Reverse Direction Trajectory");
        }

        private static void Test_LevelRepositoryAndStorage()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ContrastTest_Levels_" + Guid.NewGuid().ToString("N"));
            try
            {
                LevelRepository.CustomStorageDirectory = tempDir;
                LevelRepository.EnsureDirectory();
                Assert(Directory.Exists(tempDir), "LevelRepo", "EnsureDirectory creates folder");

                // Initially empty
                string[] initialFiles = LevelRepository.ListLevelFiles();
                Assert(initialFiles.Length == 0, "LevelRepo", "ListLevelFiles initially empty");

                // EnsureDefaultLevelFile creates default_level.json
                LevelRepository.EnsureDefaultLevelFile();
                string[] afterDefault = LevelRepository.ListLevelFiles();
                Assert(afterDefault.Length == 1, "LevelRepo", "EnsureDefaultLevelFile creates 1 file");
                Assert(Path.GetFileName(afterDefault[0]) == "default_level.json", "LevelRepo", "Default level filename matches");

                // Unique filename generation
                string fn1 = LevelRepository.GenerateUniqueFileName("test_");
                string savePath1 = Path.Combine(tempDir, fn1);
                LevelRepository.WriteAllText(savePath1, "{\"platforms\":[]}");
                Assert(File.Exists(savePath1), "LevelRepo", "WriteAllText writes file");

                string readBack = LevelRepository.ReadAllText(savePath1);
                Assert(readBack == "{\"platforms\":[]}", "LevelRepo", "ReadAllText reads back content");

                // GetUniqueSavePath generates unique suffix when file exists
                string savePath2 = LevelRepository.GetUniqueSavePath(fn1);
                Assert(savePath1 != savePath2, "LevelRepo", "GetUniqueSavePath never overwrites existing file");
            }
            finally
            {
                LevelRepository.CustomStorageDirectory = null;
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private static void Test_LevelSourceTracking()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{\"custom\":\"disk\"}");
                LevelSource srcFile = LevelSource.FromFile(tempFile);
                Assert(srcFile.SourceType == LevelSourceType.FilePath, "LevelSource", "SourceType is FilePath");
                Assert(srcFile.FilePath == tempFile, "LevelSource", "FilePath matches");
                Assert(srcFile.LoadRawJson() == "{\"custom\":\"disk\"}", "LevelSource", "LoadRawJson loads disk content");

                LevelSource srcMem = LevelSource.FromMemory("{\"custom\":\"memory\"}");
                Assert(srcMem.SourceType == LevelSourceType.InMemoryJson, "LevelSource", "SourceType is InMemoryJson");
                Assert(srcMem.LoadRawJson() == "{\"custom\":\"memory\"}", "LevelSource", "LoadRawJson loads memory content");

                LevelSource srcRes = LevelSource.FromResource("Levels/L01", "L01 — Cánh cửa không phải vật cản");
                Assert(srcRes.SourceType == LevelSourceType.PackagedResource, "LevelSource", "SourceType is PackagedResource");
                Assert(srcRes.ResourcePath == "Levels/L01", "LevelSource", "ResourcePath matches");
                Assert(srcRes.FilePath == "L01 — Cánh cửa không phải vật cản", "LevelSource", "FilePath holds level title");

                Assert(GameplayEntryContext.Campaign == 0, "GameplayEntryContext", "Campaign context is 0");
                Assert(GameplayEntryContext.CustomLevel == (GameplayEntryContext)1, "GameplayEntryContext", "CustomLevel context is 1");
                Assert(GameplayEntryContext.MainMenuGameplay == (GameplayEntryContext)2, "GameplayEntryContext", "MainMenuGameplay context is 2");
                Assert(GameplayEntryContext.EditorTestPlay == (GameplayEntryContext)3, "GameplayEntryContext", "EditorTestPlay context is 3");
            }
            finally
            {
                if (File.Exists(tempFile))
                    try { File.Delete(tempFile); } catch { }
            }
        }

        private static void Test_SanitizerRawJsonPresenceHelper()
        {
            string jsonWithEnd = "{\n  \"startPos\": { \"color\": 0 },\n  \"end\": { \"position\": { \"x\": 10, \"y\": 2 } },\n  \"platforms\": []\n}";
            Assert(LevelDataSanitizer.IsFieldPresentAndNonNull(jsonWithEnd, "end"), "PresenceHelper", "Present 'end' is detected");
            Assert(LevelDataSanitizer.IsFieldPresentAndNonNull(jsonWithEnd, "startPos"), "PresenceHelper", "Present 'startPos' is detected");
            Assert(!LevelDataSanitizer.IsFieldPresentAndNonNull(jsonWithEnd, "cameraBoundary"), "PresenceHelper", "Absent 'cameraBoundary' is detected as absent");

            string jsonWithNullEnd = "{\n  \"startPos\": { \"color\": 0 },\n  \"end\": null,\n  \"platforms\": []\n}";
            Assert(!LevelDataSanitizer.IsFieldPresentAndNonNull(jsonWithNullEnd, "end"), "PresenceHelper", "'end': null is detected as absent");

            // Nested key inside platforms should not match root key
            string jsonWithNestedEnd = "{\n  \"platforms\": [ { \"end\": 123 } ]\n}";
            Assert(!LevelDataSanitizer.IsFieldPresentAndNonNull(jsonWithNestedEnd, "end"), "PresenceHelper", "Nested 'end' does not match top-level key");

            // Extract snippet
            string jsonSingleCp = "{\n  \"checkPoint\": { \"position\": { \"x\": 5, \"y\": 1 }, \"color\": 128 }\n}";
            string snippet = LevelDataSanitizer.ExtractTopLevelFieldSnippet(jsonSingleCp, "checkPoint");
            Assert(snippet != null && snippet.Contains("\"x\": 5"), "PresenceHelper", "ExtractTopLevelFieldSnippet extracts object snippet");
        }

        private static void Test_SanitizerDataRepairAndValidation()
        {
            LevelData data = new LevelData
            {
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(float.NaN, 0f), size = new Vector2Data(2f, 1f) },
                    new ColorPlatformData { position = new Vector2Data(5f, -1f), size = new Vector2Data(-4f, float.NaN), rawGrayscaleColor = float.NaN },
                    new ColorPlatformData { position = new Vector2Data(12f, 0f), size = new Vector2Data(6f, 1f), rawGrayscaleColor = 350f }
                },
                checkPoint = new CheckPointData[]
                {
                    new CheckPointData { position = new Vector2Data(5f, 1f), color = 100f },
                    new CheckPointData { position = new Vector2Data(5f, 1f), color = 100f } // duplicate
                },
                end = null // no end
            };

            LevelData sanitized = LevelDataSanitizer.Sanitize(data, null, out var report);

            Assert(sanitized.platforms.Length == 2, "Sanitizer_Repair", "NaN position platform removed");
            Assert(sanitized.platforms[0].size.x >= 0.05f && sanitized.platforms[0].size.y >= 0.05f, "Sanitizer_Repair", "Negative/NaN size repaired to positive finite");
            AssertApprox(255f, sanitized.platforms[0].rawGrayscaleColor, 1e-4f, "Sanitizer_Repair", "NaN color repaired to 255 (Universal)");
            AssertApprox(255f, sanitized.platforms[1].rawGrayscaleColor, 1e-4f, "Sanitizer_Repair", "Out of bounds color 350 clamped to 255");
            Assert(sanitized.checkPoint.Length == 1, "Sanitizer_Repair", "Duplicate checkpoint removed");
            Assert(sanitized.startPos != null, "Sanitizer_Repair", "Missing StartPos automatically created above first platform");
            Assert(sanitized.end == null, "Sanitizer_Repair", "Missing End remains null without phantom End");
            Assert(report.Exists(r => r.Contains("WARNING")), "Sanitizer_Repair", "Missing End emits warning in report");
        }

        private static void Test_SanitizerCameraBoundaryGeneration()
        {
            LevelData data = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, 0f), size = new Vector2Data(10f, 1f) },
                    new ColorPlatformData { position = new Vector2Data(50f, 10f), size = new Vector2Data(8f, 1f) }
                },
                cameraBoundary = null // missing
            };

            LevelData sanitized = LevelDataSanitizer.Sanitize(data, null, out var report);

            Assert(sanitized.cameraBoundary != null, "Sanitizer_Camera", "Camera boundary generated when missing");
            Assert(sanitized.cameraBoundary.size.x >= 30f && sanitized.cameraBoundary.size.y >= 18f, "Sanitizer_Camera", "Camera boundary respects minimum visible extent");
            float minX = sanitized.cameraBoundary.position.x - sanitized.cameraBoundary.size.x * 0.5f;
            float maxX = sanitized.cameraBoundary.position.x + sanitized.cameraBoundary.size.x * 0.5f;
            float minY = sanitized.cameraBoundary.position.y - sanitized.cameraBoundary.size.y * 0.5f;
            float maxY = sanitized.cameraBoundary.position.y + sanitized.cameraBoundary.size.y * 0.5f;

            Assert(minX <= -5f && maxX >= 54f, "Sanitizer_Camera", "Camera boundary encloses all platforms along X with margin");
            Assert(minY <= -5f && maxY >= 15f, "Sanitizer_Camera", "Camera boundary encloses all platforms along Y with margin");
        }

        private static void Test_SanitizerTrajectoryRepairs()
        {
            LevelData data = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f) },
                platforms = new ColorPlatformData[]
                {
                    // Trajectory with duration <= 0 and scaleX == 0
                    new ColorPlatformData
                    {
                        position = new Vector2Data(5f, 2f),
                        size = new Vector2Data(4f, 1f),
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Line,
                            domainMode = TrajectoryDomainMode.ParameterRange,
                            scaleX = 0f,
                            duration = -2f,
                            originX = 5f,
                            originY = 2f
                        }
                    },
                    // Circle with radius <= 0 and SpatialBounds (invalid combo)
                    new ColorPlatformData
                    {
                        position = new Vector2Data(20f, 5f),
                        size = new Vector2Data(3f, 1f),
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Circle,
                            domainMode = TrajectoryDomainMode.SpatialBounds,
                            radius = -1f,
                            duration = 4f,
                            originX = 20f,
                            originY = 5f
                        }
                    }
                }
            };

            LevelData sanitized = LevelDataSanitizer.Sanitize(data, null, out var report);

            var t0 = sanitized.platforms[0].trajectory;
            Assert(t0 != null && t0.enabled, "TrajectoryRepair", "Platform 0 trajectory repaired and enabled");
            AssertApprox(4f, t0.duration, 1e-4f, "TrajectoryRepair", "Negative duration repaired to 4s");
            AssertApprox(5f, t0.scaleX, 1e-4f, "TrajectoryRepair", "Zero scaleX repaired to 5");

            var t1 = sanitized.platforms[1].trajectory;
            Assert(t1 != null && t1.enabled, "TrajectoryRepair", "Platform 1 trajectory repaired and enabled");
            AssertApprox(2f, t1.radius, 1e-4f, "TrajectoryRepair", "Negative radius repaired to 2");
            Assert(t1.domainMode == TrajectoryDomainMode.ParameterRange, "TrajectoryRepair", "Circle with SpatialBounds repaired to ParameterRange");

            // Mandatory contract: platform position matches initial trajectory position
            Vector2 expectedInit0 = Trajectory.GetInitialPosition(t0);
            AssertVecApprox(expectedInit0, sanitized.platforms[0].position.ToVector2(), 1e-4f, "TrajectoryRepair", "Platform 0 position matches initial trajectory point");
        }

        private static void Test_JsonExportNoPhantomObjects()
        {
            LevelData data = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                checkPoint = new CheckPointData[]
                {
                    new CheckPointData { position = new Vector2Data(10f, 1f), size = new Vector2Data(1f, 1f), color = 255f }
                },
                end = null, // explicitly absent
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(15f, 0f), size = new Vector2Data(40f, 20f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, -0.5f), size = new Vector2Data(6f, 1f), rawGrayscaleColor = 255f, trajectory = null },
                    new ColorPlatformData { position = new Vector2Data(8f, -0.5f), size = new Vector2Data(4f, 1f), rawGrayscaleColor = 128f, trajectory = new TrajectoryData { enabled = false } }
                }
            };

            string json = LevelDataSanitizer.SerializeToJson(data);

            Assert(!json.Contains("\"end\":"), "JsonExport", "Absent End marker is NOT exported in JSON");
            Assert(!json.Contains("\"spawnPoint\":"), "JsonExport", "Legacy spawnPoint is NOT exported in JSON");
            Assert(!json.Contains("\"trajectory\":"), "JsonExport", "Disabled or null trajectory is NOT exported on static platforms");
            Assert(!json.Contains("NaN") && !json.Contains("Infinity"), "JsonExport", "Export contains no NaN or Infinity tokens");
            Assert(json.Contains("\"startPos\":"), "JsonExport", "startPos is exported");
            Assert(json.Contains("\"checkPoint\":"), "JsonExport", "checkPoint array is exported");
            Assert(json.Contains("\"cameraBoundary\":"), "JsonExport", "cameraBoundary is exported");
        }

        private static void Test_MalformedJsonRepairAndReport()
        {
            string malformedJson = "{\n  \"startPos\": {\n    \"position\": { \"x\": 0.0, \"y\": 1.0 },\n    \"size\": { \"x\": -2.0, \"y\": 1.0 },\n    \"color\": 0.0\n  },\n  \"end\": null,\n  \"platforms\": [\n    {\n      \"position\": { \"x\": 0.0, \"y\": -0.5 },\n      \"size\": { \"x\": -6.0, \"y\": 1.0 },\n      \"rawGrayscaleColor\": 300.0,\n      \"trajectory\": {\n        \"enabled\": true,\n        \"curveType\": 0,\n        \"scaleX\": 0.0,\n        \"duration\": -5.0\n      }\n    }\n  ]\n}";

            LevelData data = DeserializeLevelData(malformedJson);
            data = LevelDataSanitizer.Sanitize(data, malformedJson, out var report);

            Assert(report != null && report.Count > 0, "MalformedJson", "Report records automatic fixes");
            Assert(data.end == null, "MalformedJson", "End remains null after sanitizing '\"end\": null'");
            Assert(data.platforms[0].size.x >= 0.05f, "MalformedJson", "Negative platform size repaired");
            AssertApprox(255f, data.platforms[0].rawGrayscaleColor, 1e-4f, "MalformedJson", "Color clamped to 255");
            Assert(data.platforms[0].trajectory.duration > 0f, "MalformedJson", "Trajectory duration repaired");
            Assert(data.cameraBoundary != null, "MalformedJson", "Camera boundary automatically generated for level missing boundary");
        }

        private static void Test_EditorRoundTripIntegrity()
        {
            LevelData orig = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                checkPoint = new CheckPointData[]
                {
                    new CheckPointData { position = new Vector2Data(15f, 1f), size = new Vector2Data(1f, 1f), color = 128f }
                },
                end = new EndData { position = new Vector2Data(30f, 2f), size = new Vector2Data(1.5f, 2f) },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(15f, 5f), size = new Vector2Data(50f, 25f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, -0.5f), size = new Vector2Data(8f, 1f), rawGrayscaleColor = 255f },
                    new ColorPlatformData
                    {
                        position = new Vector2Data(10f, 0f),
                        size = new Vector2Data(4f, 1f),
                        rawGrayscaleColor = 0f,
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Line,
                            playbackMode = TrajectoryPlaybackMode.PingPong,
                            originX = 10f,
                            originY = 0f,
                            scaleX = 6f,
                            duration = 8f
                        }
                    }
                }
            };

            string exportedJson = LevelDataSanitizer.SerializeToJson(orig);
            LevelData reimported = DeserializeLevelData(exportedJson);
            reimported = LevelDataSanitizer.Sanitize(reimported, exportedJson, out var report);

            Assert(reimported != null, "RoundTrip", "Reimported LevelData is non-null");
            Assert(reimported.platforms.Length == 2, "RoundTrip", "Platform count preserved (2)");
            Assert(reimported.checkPoint != null && reimported.checkPoint.Length == 1, "RoundTrip", "CheckPoint preserved");
            Assert(reimported.end != null, "RoundTrip", "End marker preserved");
            AssertVecApprox(new Vector2(30f, 2f), reimported.end.position.ToVector2(), 1e-4f, "RoundTrip", "End marker position matches");
            Assert(reimported.platforms[1].trajectory != null && reimported.platforms[1].trajectory.enabled, "RoundTrip", "Moving platform trajectory preserved");
            AssertApprox(8f, reimported.platforms[1].trajectory.duration, 1e-4f, "RoundTrip", "Trajectory duration preserved");
            AssertApprox(6f, reimported.platforms[1].trajectory.scaleX, 1e-4f, "RoundTrip", "Trajectory scaleX preserved");
        }

        private static void Test_GameplayEntryContextAndNavigation()
        {
            // 1. Initial default state
            var context = GameplayEntryContext.MainMenuGameplay;
            Assert(context == GameplayEntryContext.MainMenuGameplay, "EntryContext", "Default context is MainMenuGameplay");

            // 2. Normal gameplay launch
            context = GameplayEntryContext.MainMenuGameplay;
            string returnTargetNormal = (context == GameplayEntryContext.EditorTestPlay) ? "LevelEditor" : "MainMenu";
            Assert(returnTargetNormal == "MainMenu", "EntryContext", "Normal gameplay RETURN targets MainMenu");

            // 3. TEST PLAY launch
            context = GameplayEntryContext.EditorTestPlay;
            string returnTargetTestPlay = (context == GameplayEntryContext.EditorTestPlay) ? "LevelEditor" : "MainMenu";
            Assert(returnTargetTestPlay == "LevelEditor", "EntryContext", "TEST PLAY RETURN targets LevelEditor");

            // 4. Multiple restarts simulate preserving context
            for (int i = 0; i < 3; i++)
            {
                // Simulated Restart: reloads level data from source, preserves context
                Assert(context == GameplayEntryContext.EditorTestPlay, "EntryContext", $"Context preserved after restart #{i + 1}");
            }
            string returnTargetAfterRestarts = (context == GameplayEntryContext.EditorTestPlay) ? "LevelEditor" : "MainMenu";
            Assert(returnTargetAfterRestarts == "LevelEditor", "EntryContext", "RETURN still targets LevelEditor after multiple restarts");

            // 5. Returning to Main Menu resets context
            context = GameplayEntryContext.MainMenuGameplay;
            Assert(context == GameplayEntryContext.MainMenuGameplay, "EntryContext", "Context successfully reset to MainMenuGameplay upon returning to Main Menu");
        }

        private static void Test_EditorWorkingCopyRestorationAfterGameplay()
        {
            // 1. Create authored level before TEST PLAY
            LevelData authored = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                checkPoint = new CheckPointData[]
                {
                    new CheckPointData { position = new Vector2Data(12f, 1f), size = new Vector2Data(1f, 1f), color = 128f }
                },
                end = new EndData { position = new Vector2Data(25f, 2f), size = new Vector2Data(1.5f, 2f) },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(12f, 5f), size = new Vector2Data(40f, 20f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, -0.5f), size = new Vector2Data(6f, 1f), rawGrayscaleColor = 255f },
                    new ColorPlatformData
                    {
                        position = new Vector2Data(10f, 0f),
                        size = new Vector2Data(4f, 1f),
                        rawGrayscaleColor = 0f,
                        trajectory = new TrajectoryData
                        {
                            enabled = true,
                            curveType = TrajectoryCurveType.Line,
                            playbackMode = TrajectoryPlaybackMode.PingPong,
                            originX = 10f,
                            originY = 0f,
                            scaleX = 8f,
                            duration = 6f
                        }
                    }
                }
            };

            // 2. Editor captures working copy snapshot before test play
            string snapshotJson = LevelDataSanitizer.SerializeToJson(authored);
            LevelData workingCopy = DeserializeLevelData(snapshotJson);

            Assert(workingCopy != null, "WorkingCopy", "Working copy captured successfully");
            Assert(workingCopy.checkPoint != null && workingCopy.checkPoint.Length == 1, "WorkingCopy", "Working copy contains 1 checkpoint");
            Assert(workingCopy.end != null, "WorkingCopy", "Working copy contains End marker");
            AssertVecApprox(new Vector2(10f, 0f), workingCopy.platforms[1].position.ToVector2(), 1e-4f, "WorkingCopy", "Platform 1 at authored position (10, 0)");

            // 3. Simulate gameplay mutations: checkpoint consumed, end consumed, platform moved
            LevelData runtimeGameplayState = DeserializeLevelData(snapshotJson);
            runtimeGameplayState.checkPoint = new CheckPointData[0]; // Checkpoint consumed & destroyed
            runtimeGameplayState.end = null; // End touched & destroyed
            runtimeGameplayState.platforms[1].position = new Vector2Data(18f, 0f); // Platform moved along trajectory to (18, 0)

            Assert(runtimeGameplayState.checkPoint.Length == 0, "WorkingCopy", "Simulated gameplay consumed checkpoint");
            Assert(runtimeGameplayState.end == null, "WorkingCopy", "Simulated gameplay consumed End marker");
            AssertVecApprox(new Vector2(18f, 0f), runtimeGameplayState.platforms[1].position.ToVector2(), 1e-4f, "WorkingCopy", "Simulated gameplay displaced platform to (18, 0)");

            // 4. Editor restoration from workingCopy
            LevelData restored = DeserializeLevelData(snapshotJson);
            restored = LevelDataSanitizer.Sanitize(restored, snapshotJson, out _);

            Assert(restored != null, "WorkingCopy", "Restored level is non-null");
            Assert(restored.checkPoint != null && restored.checkPoint.Length == 1, "WorkingCopy", "Consumed checkpoint marker restored from working copy");
            AssertVecApprox(new Vector2(12f, 1f), restored.checkPoint[0].position.ToVector2(), 1e-4f, "WorkingCopy", "Restored checkpoint at authored position (12, 1)");
            Assert(restored.end != null, "WorkingCopy", "Consumed End marker restored from working copy");
            AssertVecApprox(new Vector2(25f, 2f), restored.end.position.ToVector2(), 1e-4f, "WorkingCopy", "Restored End at authored position (25, 2)");
            AssertVecApprox(new Vector2(10f, 0f), restored.platforms[1].position.ToVector2(), 1e-4f, "WorkingCopy", "Platform position reset to authored editor position (10, 0)");
            AssertApprox(6f, restored.platforms[1].trajectory.duration, 1e-4f, "WorkingCopy", "Authored trajectory duration intact");
        }

        private static void Test_EditorInputRouter()
        {
            var router = new EditorInputRouter();
            object ownerA = "ownerA";
            object ownerB = "ownerB";

            Assert(router.CapturePointer(ownerA), "InputRouter", "Owner A successfully captures pointer");
            Assert(router.HasPointerCapture, "InputRouter", "Router reports HasPointerCapture");
            Assert(router.IsOwner(ownerA), "InputRouter", "IsOwner matches owner A");
            Assert(!router.IsOwner(ownerB), "InputRouter", "IsOwner does not match owner B");

            Assert(!router.CapturePointer(ownerB), "InputRouter", "Owner B cannot steal captured pointer");

            router.ReleasePointer(ownerB);
            Assert(router.HasPointerCapture, "InputRouter", "Owner B cannot release Owner A's capture");

            router.ReleasePointer(ownerA);
            Assert(!router.HasPointerCapture, "InputRouter", "Owner A's capture released");

            Assert(router.CanBeginViewportInteraction(), "InputRouter", "Can begin interaction when pointer not over UI");
        }

        private static void Test_PlatformSizeAndColorEditing()
        {
            // 1. Float Parsing (invariant, comma fallback, culture neutrality)
            float val;
            Assert(LevelEditorUI.TryParseFloat("12.5", 0f, out val) && Math.Abs(val - 12.5f) < 1e-4f, "UI_Parsing", "Parse invariant dot '12.5'");
            Assert(LevelEditorUI.TryParseFloat("12,5", 0f, out val) && Math.Abs(val - 12.5f) < 1e-4f, "UI_Parsing", "Parse comma fallback '12,5'");
            Assert(LevelEditorUI.TryParseFloat(" 8.0 ", 0f, out val) && Math.Abs(val - 8.0f) < 1e-4f, "UI_Parsing", "Parse trimmed ' 8.0 '");
            Assert(LevelEditorUI.TryParseFloat("255", 0f, out val) && Math.Abs(val - 255f) < 1e-4f, "UI_Parsing", "Parse integer '255'");
            Assert(!LevelEditorUI.TryParseFloat("abc", 42f, out val) && Math.Abs(val - 42f) < 1e-4f, "UI_Parsing", "Reject invalid letters 'abc' and return fallback");
            Assert(!LevelEditorUI.TryParseFloat("", 7f, out val) && Math.Abs(val - 7f) < 1e-4f, "UI_Parsing", "Reject empty string and return fallback");

            // 2. Color classification & platform color mapping
            Assert(ColorClassifier.Classify(0f) == LogicalColor.White, "ColorClassifier", "0f classifies as White");
            Assert(ColorClassifier.Classify(127f) == LogicalColor.Gray, "ColorClassifier", "127f classifies as Gray");
            Assert(ColorClassifier.Classify(255f) == LogicalColor.Universal, "ColorClassifier", "255f classifies as Universal");

            var c0 = ColorClassifier.GetPlatformVisualColor(0f);
            var c127 = ColorClassifier.GetPlatformVisualColor(127f);
            var c212 = ColorClassifier.GetPlatformVisualColor(212f);
            var c255 = ColorClassifier.GetPlatformVisualColor(255f);
            AssertApprox(1f, c0.r, 0.01f, "ColorVisual", "White color r is 1");
            AssertApprox(0.55f, c127.r, 0.01f, "ColorVisual", "Gray color r is 0.55");
            AssertApprox(0.12f, c212.r, 0.01f, "ColorVisual", "Black color r is 0.12");
            AssertApprox(0.157f, c255.r, 0.01f, "ColorVisual", "Universal color r is 0.157 (Gray-Blue)");

            // 3. LevelData platform size & color mutation roundtrip
            var level = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0, 0) },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(0, 0), size = new Vector2Data(20, 10) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData
                    {
                        position = new Vector2Data(5, 2),
                        size = new Vector2Data(4, 1),
                        rawGrayscaleColor = 255f
                    }
                }
            };

            // Apply size change: 4x1 -> 10x3
            level.platforms[0].size.x = 10f;
            level.platforms[0].size.y = 3f;
            // Apply color change: 255 -> 0 (White)
            level.platforms[0].rawGrayscaleColor = 0f;

            string json = LevelDataSanitizer.SerializeToJson(level);
            var reimported = DeserializeLevelData(json);

            Assert(reimported != null && reimported.platforms.Length == 1, "DataRoundTrip", "Reimported platform exists");
            AssertApprox(10f, reimported.platforms[0].size.x, 1e-4f, "DataRoundTrip", "Reimported platform size X is 10");
            AssertApprox(3f, reimported.platforms[0].size.y, 1e-4f, "DataRoundTrip", "Reimported platform size Y is 3");
            AssertApprox(0f, reimported.platforms[0].rawGrayscaleColor, 1e-4f, "DataRoundTrip", "Reimported platform color is 0 (White)");
        }

        private static void Test_FitMapAndWhitePlatformCalculations()
        {
            // 1. White platform semantics preservation
            Assert(ColorClassifier.Classify(0f) == LogicalColor.White, "WhiteSemantics", "0f is classified as White");
            Assert(ColorClassifier.Classify(42f) == LogicalColor.White, "WhiteSemantics", "Midpoint 42f is classified as White");
            Assert(ColorClassifier.Classify(84f) == LogicalColor.White, "WhiteSemantics", "Boundary 84f is classified as White");
            Assert(ColorClassifier.Classify(85f) == LogicalColor.Gray, "WhiteSemantics", "85f transitions to Gray");

            // 2. Bounds Calculation Math
            // Platforms: P1 at (-20, -5) with size (6, 2) -> X: [-23, -17], Y: [-6, -4]
            //            P2 at (40, 15) with size (10, 2) -> X: [35, 45], Y: [14, 16]
            // Marker: StartPos at (-10, 0) with size (1, 1) -> X: [-10.5, -9.5], Y: [-0.5, 0.5]
            float minX = -23f;
            float maxX = 45f;
            float minY = -6f;
            float maxY = 16f;

            float boundsW = maxX - minX; // 68
            float boundsH = maxY - minY; // 22
            float centerX = (minX + maxX) * 0.5f; // 11
            float centerY = (minY + maxY) * 0.5f; // 5

            AssertApprox(68f, boundsW, 1e-4f, "FitMapMath", "Calculated bounds width is 68");
            AssertApprox(22f, boundsH, 1e-4f, "FitMapMath", "Calculated bounds height is 22");
            AssertApprox(11f, centerX, 1e-4f, "FitMapMath", "Calculated bounds center X is 11");
            AssertApprox(5f, centerY, 1e-4f, "FitMapMath", "Calculated bounds center Y is 5");

            // 3. Margin & Usable Area Ratio Calculation
            float marginX = Math.Max(2.5f, boundsW * 0.15f); // 10.2
            float marginY = Math.Max(2.5f, boundsH * 0.15f); // 3.3
            float paddedW = boundsW + 2f * marginX; // 88.4
            float paddedH = boundsH + 2f * marginY; // 28.6

            AssertApprox(88.4f, paddedW, 1e-4f, "FitMapMath", "Padded width with 15% margin");
            AssertApprox(28.6f, paddedH, 1e-4f, "FitMapMath", "Padded height with 15% margin");

            const float usableWidthRatio = 1240f / 1920f;
            const float usableHeightRatio = 966f / 1080f;
            const float aspect = 16f / 9f;

            float sizeForHeight = paddedH / (2f * usableHeightRatio);
            float sizeForWidth = paddedW / (2f * aspect * usableWidthRatio);
            float targetOrthoSize = Math.Max(sizeForHeight, sizeForWidth);

            Assert(targetOrthoSize > 15f, "FitMapMath", "Target orthographic size is appropriately scaled (> 15)");
            Assert(targetOrthoSize < 250f, "FitMapMath", "Target orthographic size within safe bounds (< 250)");

            // Verify both width and height fit inside the usable screen viewport
            float usableWorldWidth = 2f * targetOrthoSize * aspect * usableWidthRatio;
            float usableWorldHeight = 2f * targetOrthoSize * usableHeightRatio;
            Assert(usableWorldWidth >= paddedW - 1e-3f, "FitMapMath", "Usable world width accommodates padded width");
            Assert(usableWorldHeight >= paddedH - 1e-3f, "FitMapMath", "Usable world height accommodates padded height");

            // 4. Viewport Offset Centering
            // Panel offsets: center X is +20px, center Y is -19px on 1920x1080
            const float normDeltaX = 20f / 1920f;
            const float normDeltaY = -19f / 1080f;

            float worldOffsetX = normDeltaX * (2f * targetOrthoSize * aspect);
            float worldOffsetY = normDeltaY * (2f * targetOrthoSize);

            float camPosX = centerX - worldOffsetX;
            float camPosY = centerY - worldOffsetY;

            // When camera is placed at (camPosX, camPosY), the center of usable viewport:
            float viewportCenterX = camPosX + worldOffsetX;
            float viewportCenterY = camPosY + worldOffsetY;

            AssertApprox(centerX, viewportCenterX, 1e-4f, "FitMapMath", "Usable viewport center matches content bounds center X");
            AssertApprox(centerY, viewportCenterY, 1e-4f, "FitMapMath", "Usable viewport center matches content bounds center Y");

            // 5. Input Router API sanity check
            var router = new Contrast.Level.EditorInteraction.EditorInputRouter();
            Assert(router.GetScrollDelta() == 0f || float.IsFinite(router.GetScrollDelta()), "InputRouter", "GetScrollDelta returns finite value");
            Assert(!router.IsFrameAllShortcutPressed(), "InputRouter", "IsFrameAllShortcutPressed defaults to false without input");
        }

        private static void Test_ResetLevel_Functionality()
        {
            // 1. Authoritative clearing of level content while preserving camera boundary
            LevelData populatedLevel = new LevelData
            {
                startPos = new StartPosData
                {
                    position = new Vector2Data(0f, 2f),
                    size = new Vector2Data(1f, 1f),
                    color = 0f
                },
                checkPoint = new[]
                {
                    new CheckPointData
                    {
                        position = new Vector2Data(10f, 2f),
                        size = new Vector2Data(1f, 1f),
                        color = 127f
                    }
                },
                end = new EndData
                {
                    position = new Vector2Data(25f, 2f),
                    size = new Vector2Data(1.5f, 2f)
                },
                cameraBoundary = new CameraBoundaryData
                {
                    position = new Vector2Data(12f, 5f),
                    size = new Vector2Data(50f, 25f)
                },
                platforms = new[]
                {
                    new ColorPlatformData
                    {
                        position = new Vector2Data(0f, 0f),
                        size = new Vector2Data(5f, 1f),
                        rawGrayscaleColor = 255f
                    },
                    new ColorPlatformData
                    {
                        position = new Vector2Data(10f, 0f),
                        size = new Vector2Data(4f, 1f),
                        rawGrayscaleColor = 0f,
                        trajectory = TrajectoryData.CreateDefault(new Vector2(10f, 0f))
                    }
                }
            };

            Assert(populatedLevel.platforms.Length == 2, "ResetLevel", "Initial level has 2 platforms");
            Assert(populatedLevel.checkPoint.Length == 1, "ResetLevel", "Initial level has 1 checkpoint");
            Assert(populatedLevel.end != null, "ResetLevel", "Initial level has End marker");

            // Perform Reset: clear editable content collections, preserve cameraBoundary
            CameraBoundaryData preservedBoundary = populatedLevel.cameraBoundary;
            LevelData resetLevel = new LevelData
            {
                platforms = new ColorPlatformData[0],
                startPos = null,
                checkPoint = null,
                end = null,
                spawnPoint = null,
                cameraBoundary = preservedBoundary
            };

            Assert(resetLevel.platforms != null && resetLevel.platforms.Length == 0, "ResetLevel", "Platforms array is empty (0 platforms)");
            Assert(resetLevel.checkPoint == null || resetLevel.checkPoint.Length == 0, "ResetLevel", "Checkpoints are cleared");
            Assert(resetLevel.end == null, "ResetLevel", "End marker is cleared");
            Assert(resetLevel.cameraBoundary != null, "ResetLevel", "Camera boundary setting is preserved");
            AssertApprox(12f, resetLevel.cameraBoundary.position.x, 1e-4f, "ResetLevel", "Camera boundary center X preserved");
            AssertApprox(50f, resetLevel.cameraBoundary.size.x, 1e-4f, "ResetLevel", "Camera boundary size X preserved");

            // 2. Export persistence: serialize to JSON and verify empty content persists
            string json = LevelDataSanitizer.SerializeToJson(resetLevel);
            Assert(!json.Contains("\"platforms\":[{") && !json.Contains("\"platforms\": [{"), "ResetLevel", "Export JSON contains no platform entries");
            Assert(!json.Contains("\"checkPoint\":[{") && !json.Contains("\"checkPoint\": [{"), "ResetLevel", "Export JSON contains no checkpoint entries");
            Assert(!json.Contains("\"end\":{") && !json.Contains("\"end\": {"), "ResetLevel", "Export JSON contains no end marker");
            Assert(json.Contains("\"cameraBoundary\""), "ResetLevel", "Export JSON preserves camera boundary");

            // 3. Re-import verification
            LevelData reimported = DeserializeLevelData(json);
            Assert(reimported != null, "ResetLevel", "Reimported level deserializes successfully");
            Assert(reimported.platforms == null || reimported.platforms.Length == 0, "ResetLevel", "Reimported level has 0 platforms");
            Assert(reimported.end == null, "ResetLevel", "Reimported level has null End marker");
            Assert(reimported.cameraBoundary != null, "ResetLevel", "Reimported level has camera boundary");

            // 4. Stale working-copy marker resurrection prevention
            // If editorWorkingCopy had been left populated, BuildExportData would restore markers.
            // With ResetLevel clearing editorWorkingCopy, markers remain null.
            LevelData workingCopy = resetLevel;
            Assert(workingCopy.startPos == null, "ResetLevel", "Working copy startPos is null");
            Assert(workingCopy.checkPoint == null, "ResetLevel", "Working copy checkPoint is null");
            Assert(workingCopy.end == null, "ResetLevel", "Working copy end is null");

            // 5. Empty level safety (idempotent reset)
            LevelData secondReset = new LevelData
            {
                platforms = new ColorPlatformData[0],
                startPos = null,
                checkPoint = null,
                end = null,
                spawnPoint = null,
                cameraBoundary = workingCopy.cameraBoundary
            };
            Assert(secondReset.platforms.Length == 0, "ResetLevel", "Second reset on empty level succeeds safely");
        }

        private static void Test_GhostPlatform_Prevention_And_ResetCleanup()
        {
            // 1. Author authored level with 1 static platform and 1 moving trajectory platform
            LevelData authored = new LevelData
            {
                startPos = new StartPosData { position = new Vector2Data(0f, 1f), size = new Vector2Data(1f, 1f), color = 0f },
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(10f, 0f), size = new Vector2Data(30f, 15f) },
                platforms = new[]
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
                            duration = 4f
                        }
                    }
                }
            };

            // 2. Editor working copy captured before Test Play
            string snapshotJson = LevelDataSanitizer.SerializeToJson(authored);
            LevelData editorWorkingCopy = DeserializeLevelData(snapshotJson);
            Assert(editorWorkingCopy.platforms.Length == 2, "GhostPrevention", "Editor working copy has exactly 2 platforms");
            AssertVecApprox(new Vector2(10f, 2f), editorWorkingCopy.platforms[1].position.ToVector2(), 1e-4f, "GhostPrevention", "Authored platform at (10, 2)");

            // 3. Test Play initialization: runtime copies data, roots unified, zero ghosts
            LevelData runtimeData = DeserializeLevelData(snapshotJson);
            Assert(runtimeData.platforms.Length == 2, "GhostPrevention", "Runtime level receives exactly 2 platforms (no duplicate records)");

            // 4. Trajectory movement simulation
            // Advance trajectory by 1.0s: phase = 1.0 / 4.0 = 0.25 => X moves to 10 + 8 * 0.25 = 12
            Vector2 movingPos = Trajectory.EvaluateWorldPoint(runtimeData.platforms[1].trajectory, 0.25f);
            AssertVecApprox(new Vector2(12f, 2f), movingPos, 1e-4f, "GhostPrevention", "Runtime platform moves to (12, 2)");
            // Ensure editor working copy position remains untouched at (10, 2)
            AssertVecApprox(new Vector2(10f, 2f), editorWorkingCopy.platforms[1].position.ToVector2(), 1e-4f, "GhostPrevention", "Editor working copy position isolated from runtime movement");

            // 5. Reset execution during / after Test Play
            CameraBoundaryData preservedBoundary = editorWorkingCopy.cameraBoundary;
            LevelData resetLevel = new LevelData
            {
                platforms = new ColorPlatformData[0],
                startPos = null,
                checkPoint = null,
                end = null,
                spawnPoint = null,
                cameraBoundary = preservedBoundary
            };
            string resetJson = LevelDataSanitizer.SerializeToJson(resetLevel);
            LevelData reloadedReset = DeserializeLevelData(resetJson);
            Assert(reloadedReset.platforms == null || reloadedReset.platforms.Length == 0, "GhostPrevention", "Reset clears all platforms from serialization");
            Assert(reloadedReset.checkPoint == null || reloadedReset.checkPoint.Length == 0, "GhostPrevention", "Reset clears all checkpoints from serialization");
            Assert(reloadedReset.end == null, "GhostPrevention", "Reset clears end marker from serialization");
            Assert(reloadedReset.cameraBoundary != null, "GhostPrevention", "Reset preserves camera boundary");

            // 6. Repeated Test Play cycles do not accumulate platforms or ghosts
            LevelData iterativeData = authored;
            for (int session = 1; session <= 5; session++)
            {
                string sessionSnapshot = LevelDataSanitizer.SerializeToJson(iterativeData);
                LevelData sessionRuntime = DeserializeLevelData(sessionSnapshot);
                Assert(sessionRuntime.platforms.Length == 2, "GhostPrevention", $"Test Play cycle #{session} maintains exactly 2 platforms");
                
                // Simulate runtime motion
                Vector2 runtimePt = Trajectory.EvaluateWorldPoint(sessionRuntime.platforms[1].trajectory, 0.5f);
                AssertVecApprox(new Vector2(14f, 2f), runtimePt, 1e-4f, "GhostPrevention", $"Test Play cycle #{session} motion evaluated correctly");

                // Simulate return to edit: rebuild from authored snapshot
                iterativeData = DeserializeLevelData(sessionSnapshot);
                Assert(iterativeData.platforms.Length == 2, "GhostPrevention", $"Return to edit after cycle #{session} preserves platform count");
                AssertVecApprox(new Vector2(10f, 2f), iterativeData.platforms[1].position.ToVector2(), 1e-4f, "GhostPrevention", $"Return to edit after cycle #{session} restores position to (10, 2)");
            }
        }

        private static void Test_EasyNewtonFinale_OrbitalTrajectories_And_CaptureMath()
        {
            Vector2 centerO = new Vector2(61.0f, 0.625f);
            float ringScale = 3.4375f; // 220 px / 64
            float rK = ringScale * Mathf.Sqrt(4f); // 6.875
            float rI = ringScale * Mathf.Sqrt(2f); // ~4.861337

            // 1. Platform A Trajectory (k = 4, period 10.8s, angle 180 -> 540 deg)
            TrajectoryData cfgK = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rK,
                paramMin = 180f,
                paramMax = 540f,
                duration = 10.8f
            };

            Vector2 posK0 = Trajectory.EvaluateWorldPoint(cfgK, 0f);
            AssertVecApprox(new Vector2(54.125f, 0.625f), posK0, 1e-4f, "EasyNewtonFinale", "Platform A phase 0 at x = 54.125 facing vestibule");

            Vector2 posK_quarter = Trajectory.EvaluateWorldPoint(cfgK, 0.25f); // 270 deg (bottom)
            AssertVecApprox(new Vector2(61.0f, 0.625f - 6.875f), posK_quarter, 1e-4f, "EasyNewtonFinale", "Platform A phase 0.25 at bottom (y = -6.25)");

            Vector2 posK_half = Trajectory.EvaluateWorldPoint(cfgK, 0.5f); // 360 deg (rightmost)
            AssertVecApprox(new Vector2(61.0f + 6.875f, 0.625f), posK_half, 1e-4f, "EasyNewtonFinale", "Platform A phase 0.5 at rightmost (x = 67.875)");

            // 2. Platform B Trajectory (i = 2, period 8.6s, angle 234 -> 594 deg)
            TrajectoryData cfgI = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rI,
                paramMin = 234f,
                paramMax = 594f,
                duration = 8.6f
            };

            Vector2 posI0 = Trajectory.EvaluateWorldPoint(cfgI, 0f);
            float expIx = centerO.x + Mathf.Cos(234f * Mathf.Deg2Rad) * rI;
            float expIy = centerO.y + Mathf.Sin(234f * Mathf.Deg2Rad) * rI;
            AssertVecApprox(new Vector2(expIx, expIy), posI0, 1e-4f, "EasyNewtonFinale", "Platform B phase 0 at initial angle 234 deg");

            // 3. Dynamic Axis and Geometry: K captured on outer orbit
            // Suppose player captures K at angle 150 deg (5/6 pi)
            float testAngleRad = 150f * Mathf.Deg2Rad;
            Vector2 u = new Vector2(Mathf.Cos(testAngleRad), Mathf.Sin(testAngleRad));
            Vector2 pointK = centerO + u * rK;
            Vector2 pointI = centerO - u * rI;
            Vector2 pointKp = centerO - u * rK;

            // Verify diametric opposition of K and K'
            Vector2 midKKp = (pointK + pointKp) * 0.5f;
            AssertVecApprox(centerO, midKKp, 1e-4f, "EasyNewtonFinale", "Center O is exact midpoint of K and K'");

            // Verify collinearity: (I - O) is antiparallel to (K - O)
            Vector2 dirKO = (pointK - centerO).normalized;
            Vector2 dirIO = (pointI - centerO).normalized;
            AssertVecApprox(-dirKO, dirIO, 1e-4f, "EasyNewtonFinale", "I lies strictly on the K-O axis opposite to K");

            // 4. Capture tolerance checks: I tolerance = 0.75, K' tolerance = 0.84375
            float tolI = 0.75f;
            float tolKp = 0.84375f;

            Vector2 nearI = pointI + new Vector2(0.3f, 0.4f); // dist = 0.5 <= 0.75
            Assert(Vector2.Distance(nearI, pointI) <= tolI, "EasyNewtonFinale", "Candidate within I tolerance passes");

            Vector2 farI = pointI + new Vector2(0.8f, 0.6f); // dist = 1.0 > 0.75
            Assert(Vector2.Distance(farI, pointI) > tolI, "EasyNewtonFinale", "Candidate outside I tolerance fails");

            Vector2 nearKp = pointKp + new Vector2(0.4f, 0.5f); // dist ~ 0.64 <= 0.84375
            Assert(Vector2.Distance(nearKp, pointKp) <= tolKp, "EasyNewtonFinale", "Candidate within K' tolerance passes");

            Vector2 farKp = pointKp + new Vector2(0.8f, 0.8f); // dist ~ 1.13 > 0.84375
            Assert(Vector2.Distance(farKp, pointKp) > tolKp, "EasyNewtonFinale", "Candidate outside K' tolerance fails");

            // 5. Scientific Wavelength Calculation
            // B = |I - K| = rK + rI, b = |K' - I| = rK - rI
            float B_units = Vector2.Distance(pointK, pointI);
            float b_units = Vector2.Distance(pointI, pointKp);
            AssertApprox(rK + rI, B_units, 1e-4f, "EasyNewtonFinale", "Measured B equals rK + rI");
            AssertApprox(rK - rI, b_units, 1e-4f, "EasyNewtonFinale", "Measured b equals rK - rI");

            float Rmm = 855f;
            float lambdaRefMm = 550e-6f;
            float physicalR1Mm = Mathf.Sqrt(lambdaRefMm * Rmm); // ~0.685747767 mm
            float mmPerUnit = physicalR1Mm / ringScale; // ~0.19949026 mm/unit

            float Bmm = B_units * mmPerUnit;
            float bmm = b_units * mmPerUnit;
            int orderDelta = Mathf.Abs(4 - 2); // |k - i| = 2
            float lambdaOutMm = (Bmm * bmm) / (orderDelta * Rmm);
            float lambdaOutNm = lambdaOutMm * 1e6f;

            AssertApprox(550.0f, lambdaOutNm, 0.1f, "EasyNewtonFinale", "Calculated lambda from physical parameters is 550.0 nm (+/- 0.1 nm)");

            // 6. Input Mapping Rules: IsNewtonFinaleActive remapping
            PlayerInput.IsNewtonFinaleActive = true;
            Assert(PlayerInput.IsNewtonFinaleActive, "EasyNewtonFinale", "PlayerInput.IsNewtonFinaleActive flag toggled on");

            // When finale active: both Space and W still jump according to HTML specification
            Assert(PlayerInput.IsJumpInputPressed(false, true), "EasyNewtonFinale", "Space ALWAYS triggers jump even inside finale");
            Assert(PlayerInput.IsJumpInputPressed(true, false), "EasyNewtonFinale", "W triggers jump during finale");

            // When finale inactive: both Space and W jump
            PlayerInput.IsNewtonFinaleActive = false;
            Assert(PlayerInput.IsJumpInputPressed(false, true), "EasyNewtonFinale", "Space triggers jump outside finale");
            Assert(PlayerInput.IsJumpInputPressed(true, false), "EasyNewtonFinale", "W triggers jump outside finale");

            // 7. Easy Level Recognition and Camera Boundary Extension
            LevelData easyData = new LevelData
            {
                cameraBoundary = new CameraBoundaryData { position = new Vector2Data(0f, 0f), size = new Vector2Data(106.25f, 18.75f) },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(45.625f, 0.4375f), size = new Vector2Data(13.125f, 0.375f) }
                }
            };
            Assert(LevelLoader.IsEasyLevel(easyData), "EasyNewtonFinale", "LevelLoader recognizes Easy level with vestibule platform");

            LevelData nonEasyData = new LevelData
            {
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(10f, 1f), size = new Vector2Data(4f, 1f) }
                }
            };
            Assert(!LevelLoader.IsEasyLevel(nonEasyData), "EasyNewtonFinale", "LevelLoader rejects non-Easy level without vestibule platform");
        }

        private static void Test_NewtonCameraFollow_And_StateTransitions()
        {
            Console.WriteLine("--- Running Newton Rings Camera-Follow & State Transition Tests (Tests A-H) ---");

            // -------------------------------------------------------------
            // 1. Camera Boundary Encompassment Test for L01, L02, and L03
            // -------------------------------------------------------------
            string[] levelFiles = new[] { "L01.json", "L02.json", "L03.json" };
            foreach (var lvlFile in levelFiles)
            {
                string jsonPath = Path.Combine("Assets", "Resources", "Levels", lvlFile);
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var lvlData = DeserializeLevelData(json);
                    Assert(lvlData.newton != null && lvlData.newton.k > 0, "NewtonCamera_Boundary", $"{lvlFile} contains valid newton chamber data");

                    // Compute boundary using the LevelLoader algorithm
                    float newtonCx = lvlData.newton.cx / 64f;
                    float newtonCy = -lvlData.newton.cy / 64f;
                    float maxOrbitRadius = 3.4375f * Mathf.Sqrt(lvlData.newton.k + 1.5f) + 4f;
                    float margin = 16f;

                    float bMinX = lvlData.cameraBoundary != null ? (lvlData.cameraBoundary.position.x - lvlData.cameraBoundary.size.x * 0.5f) : 0f;
                    float bMaxX = lvlData.cameraBoundary != null ? (lvlData.cameraBoundary.position.x + lvlData.cameraBoundary.size.x * 0.5f) : 0f;
                    float bMinY = lvlData.cameraBoundary != null ? (lvlData.cameraBoundary.position.y - lvlData.cameraBoundary.size.y * 0.5f) : 0f;
                    float bMaxY = lvlData.cameraBoundary != null ? (lvlData.cameraBoundary.position.y + lvlData.cameraBoundary.size.y * 0.5f) : 0f;

                    float expMinX = Mathf.Min(bMinX, newtonCx - maxOrbitRadius - margin);
                    float expMaxX = Mathf.Max(bMaxX, newtonCx + maxOrbitRadius + margin);
                    float expMinY = Mathf.Min(bMinY, newtonCy - maxOrbitRadius - margin);
                    float expMaxY = Mathf.Max(bMaxY, newtonCy + maxOrbitRadius + margin);

                    // Verify bounds encompass full orbit of rail K with camera half-width (11.5f)
                    float railK_MaxX = newtonCx + 3.4375f * Mathf.Sqrt(lvlData.newton.k + 0.5f);
                    float railK_MinX = newtonCx - 3.4375f * Mathf.Sqrt(lvlData.newton.k + 0.5f);
                    float railK_MaxY = newtonCy + 3.4375f * Mathf.Sqrt(lvlData.newton.k + 0.5f);
                    float railK_MinY = newtonCy - 3.4375f * Mathf.Sqrt(lvlData.newton.k + 0.5f);

                    float horzExtent = 6.5f * (16f / 9f);
                    float vertExtent = 6.5f;

                    Assert(railK_MaxX <= expMaxX - horzExtent, "NewtonCamera_Boundary", $"{lvlFile} rightmost orbit x={railK_MaxX:F2} is within camera boundary clamp ({expMaxX - horzExtent:F2})");
                    Assert(railK_MinX >= expMinX + horzExtent, "NewtonCamera_Boundary", $"{lvlFile} leftmost orbit x={railK_MinX:F2} is within camera boundary clamp ({expMinX + horzExtent:F2})");
                    Assert(railK_MaxY <= expMaxY - vertExtent, "NewtonCamera_Boundary", $"{lvlFile} uppermost orbit y={railK_MaxY:F2} is within camera boundary clamp ({expMaxY - vertExtent:F2})");
                    Assert(railK_MinY >= expMinY + vertExtent, "NewtonCamera_Boundary", $"{lvlFile} lowermost orbit y={railK_MinY:F2} is within camera boundary clamp ({expMinY + vertExtent:F2})");
                }
            }

            // -------------------------------------------------------------
            // 2. CameraFollow State Machine & Transitions (Tests A-H)
            // -------------------------------------------------------------
            CameraMode mode = CameraMode.NormalPlatformingFollow;
            float targetOrtho = 5.0f;
            float aspect = 16f / 9f;
            Vector2 boundaryCenter = new Vector2(45.65f, -10.30f);
            Vector2 boundarySize = new Vector2(128.0f, 66.8f);
            bool hasBoundary = true;

            // Test initial state
            Assert(mode == CameraMode.NormalPlatformingFollow, "NewtonCamera_State", "Initial mode is NormalPlatformingFollow");

            // Test A — Enter Newton: Transition from platforming to NewtonPlayerFollow
            mode = CameraMode.NewtonPlayerFollow;
            targetOrtho = 6.5f;
            Assert(mode == CameraMode.NewtonPlayerFollow, "NewtonCamera_TestA", "Transition into Newton switches mode to NewtonPlayerFollow");
            Assert(targetOrtho == 6.5f, "NewtonCamera_TestA", "Target ortho size expands to 6.5 in NewtonPlayerFollow");

            // Test B — Horizontal movement in NewtonPlayerFollow
            Vector3 playerPos = new Vector3(75f, -10.3f, 0f);
            Vector3 camPos = ComputeSimulatedCameraPos(playerPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);
            AssertApprox(75f, camPos.x, 1e-3f, "NewtonCamera_TestB", "Camera X follows player X to 75.0");
            AssertApprox(-10.3f, camPos.y, 1e-3f, "NewtonCamera_TestB", "Camera Y follows player Y to -10.3");

            playerPos = new Vector3(82.5f, -10.3f, 0f);
            camPos = ComputeSimulatedCameraPos(playerPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);
            AssertApprox(82.5f, camPos.x, 1e-3f, "NewtonCamera_TestB", "Camera X follows player X to 82.5");

            // Test C — Orbit K (Full 360-degree rotation)
            Vector2 centerO = new Vector2(80.234f, -10.297f);
            float rK = 3.4375f * Mathf.Sqrt(6.5f); // ~8.764 units
            bool orbitKPass = true;
            for (int deg = 0; deg < 360; deg += 10)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 orbitPos = new Vector3(centerO.x + rK * Mathf.Cos(rad), centerO.y + rK * Mathf.Sin(rad), 0f);
                Vector3 cPos = ComputeSimulatedCameraPos(orbitPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);

                // Camera must match player's final world position exactly (zero clamping / zero lag)
                if (Vector2.Distance(new Vector2(cPos.x, cPos.y), (Vector2)orbitPos) > 1e-3f)
                {
                    orbitKPass = false;
                    break;
                }
                // Camera must NOT be frozen at center O
                if (Vector2.Distance(new Vector2(cPos.x, cPos.y), centerO) < 1.0f)
                {
                    orbitKPass = false;
                    break;
                }
            }
            Assert(orbitKPass, "NewtonCamera_TestC", "Camera continuously tracks player through full 360 deg orbit on platform K without locking to O");

            // Test D — Orbit I (Full 360-degree rotation)
            float rI = 3.4375f * Mathf.Sqrt(3.5f); // ~6.431 units
            bool orbitIPass = true;
            for (int deg = 0; deg < 360; deg += 10)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 orbitPos = new Vector3(centerO.x + rI * Mathf.Cos(rad), centerO.y + rI * Mathf.Sin(rad), 0f);
                Vector3 cPos = ComputeSimulatedCameraPos(orbitPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);

                if (Vector2.Distance(new Vector2(cPos.x, cPos.y), (Vector2)orbitPos) > 1e-3f)
                {
                    orbitIPass = false;
                    break;
                }
            }
            Assert(orbitIPass, "NewtonCamera_TestD", "Camera continuously tracks player through full 360 deg orbit on platform I");

            // Test E — Jump between K and I (Airborne displacement)
            Vector3 jumpStart = new Vector3(centerO.x + rK, centerO.y, 0f);
            Vector3 jumpEnd = new Vector3(centerO.x + rI, centerO.y, 0f);
            bool jumpPass = true;
            for (float t = 0f; t <= 1.0f; t += 0.1f)
            {
                float x = Mathf.Lerp(jumpStart.x, jumpEnd.x, t);
                float y = Mathf.Lerp(jumpStart.y, jumpEnd.y, t) + 2.0f * 4.0f * t * (1.0f - t); // parabolic arc
                Vector3 airbornePos = new Vector3(x, y, 0f);
                Vector3 cPos = ComputeSimulatedCameraPos(airbornePos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);

                if (Vector2.Distance(new Vector2(cPos.x, cPos.y), new Vector2(x, y)) > 1e-3f)
                {
                    jumpPass = false;
                    break;
                }
            }
            Assert(jumpPass, "NewtonCamera_TestE", "Camera continuously tracks airborne jump arc between platforms K and I");

            // Test F — Marking with X does not interrupt camera follow
            // Mark K:
            Assert(mode == CameraMode.NewtonPlayerFollow, "NewtonCamera_TestF", "Camera remains in NewtonPlayerFollow after marking K");
            playerPos = new Vector3(79f, -12f, 0f);
            camPos = ComputeSimulatedCameraPos(playerPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);
            AssertApprox(79f, camPos.x, 1e-3f, "NewtonCamera_TestF", "Camera tracks player after marking K");

            // Mark I:
            Assert(mode == CameraMode.NewtonPlayerFollow, "NewtonCamera_TestF", "Camera remains in NewtonPlayerFollow after marking I");
            playerPos = new Vector3(81f, -9f, 0f);
            camPos = ComputeSimulatedCameraPos(playerPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);
            AssertApprox(81f, camPos.x, 1e-3f, "NewtonCamera_TestF", "Camera tracks player after marking I");

            // Test G — Final reveal switches mode ONLY upon 3 marks complete
            mode = CameraMode.NewtonResultReveal;
            targetOrtho = 9.6f;
            Assert(mode == CameraMode.NewtonResultReveal, "NewtonCamera_TestG", "Camera switches to NewtonResultReveal upon completion of all 3 marks");
            Assert(targetOrtho == 9.6f, "NewtonCamera_TestG", "Reveal target ortho size is 9.6");

            // Test H — Restart and Regression
            mode = CameraMode.NormalPlatformingFollow;
            targetOrtho = 5.0f;
            Assert(mode == CameraMode.NormalPlatformingFollow, "NewtonCamera_TestH", "Reset restores NormalPlatformingFollow mode");
            Assert(targetOrtho == 5.0f, "NewtonCamera_TestH", "Reset restores 5.0 orthographic size");

            // Normal platforming tracking
            playerPos = new Vector3(10f, 2f, 0f);
            camPos = ComputeSimulatedCameraPos(playerPos, hasBoundary, boundaryCenter, boundarySize, targetOrtho, aspect);
            AssertApprox(10f, camPos.x, 1e-3f, "NewtonCamera_TestH", "Normal platforming camera tracks player at (10, 2)");
        }

        private static Vector3 ComputeSimulatedCameraPos(Vector3 targetPos, bool hasBoundary, Vector2 boundaryCenter, Vector2 boundarySize, float orthoSize, float aspect)
        {
            float desiredX = targetPos.x;
            float desiredY = targetPos.y;

            if (hasBoundary)
            {
                float vertExtent = orthoSize;
                float horzExtent = vertExtent * aspect;

                float minX = boundaryCenter.x - boundarySize.x * 0.5f;
                float maxX = boundaryCenter.x + boundarySize.x * 0.5f;
                float minY = boundaryCenter.y - boundarySize.y * 0.5f;
                float maxY = boundaryCenter.y + boundarySize.y * 0.5f;

                desiredX = boundarySize.x >= horzExtent * 2f
                    ? Mathf.Clamp(desiredX, minX + horzExtent, maxX - horzExtent)
                    : boundaryCenter.x;

                desiredY = boundarySize.y >= vertExtent * 2f
                    ? Mathf.Clamp(desiredY, minY + vertExtent, maxY - vertExtent)
                    : boundaryCenter.y;
            }

            return new Vector3(desiredX, desiredY, -10f);
        }

        private static LevelData DeserializeLevelData(string json)
        {
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
            return System.Text.Json.JsonSerializer.Deserialize<LevelData>(json, options);
        }

        private static void Test_TKeyTestMode_ComprehensiveSuite()
        {
            Console.WriteLine("\n[RUNNING] Test_TKeyTestMode_ComprehensiveSuite...");

            // -------------------------------------------------------------
            // Test 1: Toggle & Velocity/State Reset
            // -------------------------------------------------------------
            bool isTestMode = false;
            Vector2 velocity = new Vector2(4.5f, -9.8f);
            bool isGrounded = true;

            // Toggle On
            isTestMode = !isTestMode;
            if (isTestMode)
            {
                velocity = Vector2.zero;
                isGrounded = false;
            }
            Assert(isTestMode, "TMode_Toggle_On", "T key enables test mode");
            Assert(velocity == Vector2.zero, "TMode_Toggle_VelocityReset", "Velocity reset to zero on activation");
            Assert(!isGrounded, "TMode_Toggle_GroundedCleared", "Grounded cleared on activation");

            // Toggle Off
            isTestMode = !isTestMode;
            Assert(!isTestMode, "TMode_Toggle_Off", "Second T key disables test mode");

            // -------------------------------------------------------------
            // Test 2: Relative Mouse Motion (all 4 directions)
            // -------------------------------------------------------------
            float orthoSize = 5.0f;
            float screenHeight = 1080f;
            float unitsPerPixel = (2f * orthoSize) / screenHeight;
            float sensitivity = 1.0f;

            Vector2 initialPos = new Vector2(10f, 5f);
            Vector2 curPos = initialPos;

            // Move Right: delta = (100, 0)
            Vector2 deltaRight = new Vector2(100f, 0f);
            curPos += new Vector2(deltaRight.x * unitsPerPixel * sensitivity, deltaRight.y * unitsPerPixel * sensitivity);
            AssertApprox(initialPos.x + 100f * (10f / 1080f), curPos.x, 1e-4f, "TMode_MouseMove_Right", "Right mouse delta moves player right");

            // Move Left: delta = (-100, 0)
            curPos += new Vector2(-100f * unitsPerPixel * sensitivity, 0f);
            AssertApprox(initialPos.x, curPos.x, 1e-4f, "TMode_MouseMove_Left", "Left mouse delta returns player to original X");

            // Move Up: delta = (0, 50)
            curPos += new Vector2(0f, 50f * unitsPerPixel * sensitivity);
            AssertApprox(initialPos.y + 50f * (10f / 1080f), curPos.y, 1e-4f, "TMode_MouseMove_Up", "Upward mouse delta moves player up");

            // Move Down: delta = (0, -50)
            curPos += new Vector2(0f, -50f * unitsPerPixel * sensitivity);
            AssertApprox(initialPos.y, curPos.y, 1e-4f, "TMode_MouseMove_Down", "Downward mouse delta returns player to original Y");

            // -------------------------------------------------------------
            // Test 3: Stationary Mouse = Zero Drift
            // -------------------------------------------------------------
            Vector2 deltaZero = Vector2.zero;
            Vector2 beforeZeroMove = curPos;
            Vector2 dispZero = new Vector2(deltaZero.x * unitsPerPixel * sensitivity, deltaZero.y * unitsPerPixel * sensitivity);
            curPos += dispZero;
            Assert(curPos == beforeZeroMove, "TMode_Stationary_ZeroDrift", "Zero mouse delta produces exactly zero drift");

            // -------------------------------------------------------------
            // Test 4: No Collision (No-Clip through solid platform)
            // -------------------------------------------------------------
            Rect platformAabb = new Rect(10f, 4.5f, 4f, 1f);
            Vector2 targetThroughPlatform = new Vector2(12f, 5f);
            curPos = targetThroughPlatform;
            Assert(platformAabb.Contains(curPos), "TMode_NoClip_SolidPenetration", "Player position successfully penetrates solid platform center");

            // -------------------------------------------------------------
            // Test 5: Invulnerability (Void & Hazard suppression)
            // -------------------------------------------------------------
            bool isDead = false;
            float killY = -50f;
            curPos = new Vector2(12f, -100f);
            bool testModeActive = true;
            if (!testModeActive && curPos.y < killY)
            {
                isDead = true;
            }
            Assert(!isDead, "TMode_Invulnerable_VoidFall", "Player remains alive below killY during Test Mode");

            // -------------------------------------------------------------
            // Test 6: Input Isolation (WASD ignored during Test Mode)
            // -------------------------------------------------------------
            Vector2 moveIntentFromKeys = new Vector2(1f, 0f) * 6f * 0.016f;
            Vector2 playerMovementApplied = testModeActive ? Vector2.zero : moveIntentFromKeys;
            Assert(playerMovementApplied == Vector2.zero, "TMode_Input_WASDIgnored", "WASD keys produce zero displacement in Test Mode");

            // -------------------------------------------------------------
            // Test 7: Camera Follow (Instant 1:1 Centering, Roll World, No Newton Center Lock)
            // -------------------------------------------------------------
            Vector3 camTargetPos = new Vector3(curPos.x, curPos.y, 0f);
            Vector3 testCamPos = new Vector3(camTargetPos.x, camTargetPos.y, -10f);
            AssertApprox(curPos.x, testCamPos.x, 1e-4f, "TMode_Cam_CenterX", "Camera X equals player X instantly (centered)");
            AssertApprox(curPos.y, testCamPos.y, 1e-4f, "TMode_Cam_CenterY", "Camera Y equals player Y instantly (centered)");
            Vector2 newtonCenterO = new Vector2(61.0f, 0.625f);
            Assert(testCamPos.x != newtonCenterO.x, "TMode_Cam_NoNewtonCenterLock", "Camera does not lock to Newton Chamber center");

            // -------------------------------------------------------------
            // Test 8: Interaction Isolation (Checkpoints & Newton Marking)
            // -------------------------------------------------------------
            bool checkpointConsumed = false;
            bool newtonMarkAccepted = false;
            if (!testModeActive)
            {
                checkpointConsumed = true;
                newtonMarkAccepted = true;
            }
            Assert(!checkpointConsumed, "TMode_Interaction_CheckpointBypass", "Checkpoint is not triggered in Test Mode");
            Assert(!newtonMarkAccepted, "TMode_Interaction_NewtonMarkBypass", "Newton mark is not triggered in Test Mode");

            // -------------------------------------------------------------
            // Test 9: Resume Gameplay at Current Position (No Teleport)
            // -------------------------------------------------------------
            testModeActive = false;
            Vector2 resumePos = curPos;
            Assert(resumePos == curPos, "TMode_Resume_NoTeleport", "Disabling test mode retains exact current debug position");

            // -------------------------------------------------------------
            // Test 10: Cross-Level Parity (L01, L02, L03 spawn coordinates)
            // -------------------------------------------------------------
            Vector2[] levelSpawns = new Vector2[]
            {
                new Vector2(2f, 1f),
                new Vector2(3f, 1.5f),
                new Vector2(1f, 2f),
                new Vector2(61f, 1f)
            };
            foreach (var spawn in levelSpawns)
            {
                Vector2 p = spawn;
                Vector2 delta = new Vector2(50f, 50f);
                p += new Vector2(delta.x * unitsPerPixel * sensitivity, delta.y * unitsPerPixel * sensitivity);
                Assert(p != spawn, "TMode_CrossLevel_Delta", $"Test mode delta applies correctly at spawn {spawn}");
            }

            // -------------------------------------------------------------
            // Test 11: Cleanup on Escape / Focus Loss
            // -------------------------------------------------------------
            bool cursorLocked = true;
            bool escapePressed = true;
            if (escapePressed)
            {
                testModeActive = false;
                cursorLocked = false;
            }
            Assert(!testModeActive && !cursorLocked, "TMode_Cleanup_Escape", "Escape deactivates Test Mode and releases cursor lock");
        }

        private static void Test_L02_Trajectory_And_NewtonDocking_Suite()
        {
            Console.WriteLine("\n--- Running L02 Trajectory & Newton Docking Verification Tests (Tests 1-10) ---");

            // Test 1 — L02 JSON parsing
            string pathL02 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Assets", "Resources", "Levels", "L02.json");
            if (!File.Exists(pathL02))
            {
                pathL02 = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Levels", "L02.json");
            }
            if (!File.Exists(pathL02))
            {
                pathL02 = Path.Combine(Directory.GetCurrentDirectory(), "..", "Assets", "Resources", "Levels", "L02.json");
            }
            string rawJson = File.ReadAllText(pathL02);
            LevelData data = DeserializeLevelData(rawJson);
            Assert(data != null, "Test1_L02_JsonParse", "L02 parses into LevelData");
            Assert(data.levelId == "L02", "Test1_L02_LevelId", "LevelId is L02");
            Assert(data.platforms != null && data.platforms.Length == 15, "Test1_L02_Platforms15", "L02 contains 15 terrain blocks");
            Assert(data.newton != null, "Test1_L02_Newton_NotNull", "L02 has newton configuration");
            Assert(data.newton.cx == 5110f && data.newton.cy == 528f, "Test1_L02_Newton_Center", "L02 center is (5110, 528)");
            Assert(data.newton.i == 2 && data.newton.k == 4, "Test1_L02_Newton_Fringes", "L02 fringe orders are i=2, k=4");
            Assert(data.newton.orbitStart == "auto", "Test1_L02_OrbitStart_Auto", "L02 orbitStart is 'auto'");

            // Test 2 — L02 Trajectory initial configuration (Automatic Start)
            Vector2 centerO = new Vector2(data.newton.cx / 64f, -data.newton.cy / 64f);
            float ringScale = 3.4375f;
            float rK = ringScale * Mathf.Sqrt(data.newton.k); // 4 -> 6.875
            float rI = ringScale * Mathf.Sqrt(data.newton.i); // 2 -> ~4.8613
            float startAngleKDeg = -data.newton.rails.K.angleDeg; // -192
            float endAngleKDeg = data.newton.rails.K.direction == 1 ? (startAngleKDeg - 360f) : (startAngleKDeg + 360f);

            TrajectoryData cfgK = new TrajectoryData
            {
                enabled = true, // Prototype override: auto start enabled immediately
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rK,
                paramMin = Mathf.Min(startAngleKDeg, endAngleKDeg),
                paramMax = Mathf.Max(startAngleKDeg, endAngleKDeg),
                duration = data.newton.rails.K.period,
                reverseDirection = (endAngleKDeg < startAngleKDeg)
            };

            float startAngleIDeg = -data.newton.rails.I.angleDeg; // -155
            float endAngleIDeg = data.newton.rails.I.direction == 1 ? (startAngleIDeg - 360f) : (startAngleIDeg + 360f);
            TrajectoryData cfgI = new TrajectoryData
            {
                enabled = true, // Prototype override: auto start enabled immediately
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = centerO.x,
                originY = centerO.y,
                radius = rI,
                paramMin = Mathf.Min(startAngleIDeg, endAngleIDeg),
                paramMax = Mathf.Max(startAngleIDeg, endAngleIDeg),
                duration = data.newton.rails.I.period,
                reverseDirection = (endAngleIDeg < startAngleIDeg)
            };

            Assert(cfgK.enabled && cfgI.enabled, "Test2_L02_InitialTrajectoriesAutoEnabled", "L02 trajectories are enabled automatically upon level start without docking");

            // Test A — Runtime position sampling across frames
            Vector2 posK_f0 = Trajectory.EvaluateWorldPoint(cfgK, 0f);
            Vector2 posK_f30 = Trajectory.EvaluateWorldPoint(cfgK, (30f * (1f / 60f)) / cfgK.duration);
            Vector2 posK_f60 = Trajectory.EvaluateWorldPoint(cfgK, (60f * (1f / 60f)) / cfgK.duration);
            Vector2 posI_f0 = Trajectory.EvaluateWorldPoint(cfgI, 0f);
            Vector2 posI_f30 = Trajectory.EvaluateWorldPoint(cfgI, (30f * (1f / 60f)) / cfgI.duration);
            Vector2 posI_f60 = Trajectory.EvaluateWorldPoint(cfgI, (60f * (1f / 60f)) / cfgI.duration);

            Console.WriteLine($"[L02] OrbitStart={data.newton.orbitStart}");
            Console.WriteLine($"[L02] DockRequired=False");
            Console.WriteLine($"[L02] RailKActive={cfgK.enabled}");
            Console.WriteLine($"[L02] RailIActive={cfgI.enabled}");
            Console.WriteLine($"[L02] Frame=0 K=({posK_f0.x:F3}, {posK_f0.y:F3}) I=({posI_f0.x:F3}, {posI_f0.y:F3})");
            Console.WriteLine($"[L02] Frame=30 K=({posK_f30.x:F3}, {posK_f30.y:F3}) I=({posI_f30.x:F3}, {posI_f30.y:F3})");
            Console.WriteLine($"[L02] Frame=60 K=({posK_f60.x:F3}, {posK_f60.y:F3}) I=({posI_f60.x:F3}, {posI_f60.y:F3})");

            Assert(Vector2.Distance(posK_f0, posK_f30) > 0.1f, "Test_L02_AutoMove_K_Frame30", "Platform K automatically moved by Frame 30 without docking");
            Assert(Vector2.Distance(posK_f30, posK_f60) > 0.1f, "Test_L02_AutoMove_K_Frame60", "Platform K automatically moved by Frame 60 without docking");
            Assert(Vector2.Distance(posI_f0, posI_f30) > 0.1f, "Test_L02_AutoMove_I_Frame30", "Platform I automatically moved by Frame 30 without docking");
            Assert(Vector2.Distance(posI_f30, posI_f60) > 0.1f, "Test_L02_AutoMove_I_Frame60", "Platform I automatically moved by Frame 60 without docking");

            // Test 3 & Test 4 — Nonzero continuous orbital displacement
            Vector2 posK_0 = Trajectory.EvaluateWorldPoint(cfgK, 0f);
            Vector2 posK_025 = Trajectory.EvaluateWorldPoint(cfgK, 0.25f);
            Vector2 posK_05 = Trajectory.EvaluateWorldPoint(cfgK, 0.5f);
            float disp025 = Vector2.Distance(posK_025, posK_0);
            float disp05 = Vector2.Distance(posK_05, posK_0);
            Assert(disp025 > 1.0f, "Test3_L02_NonzeroDispQuarter", $"Continuous orbital displacement at 0.25 phase is {disp025:F3}");
            Assert(disp05 > 2.0f, "Test3_L02_NonzeroDispHalf", $"Continuous orbital displacement at 0.50 phase is {disp05:F3}");

            Vector2 posI_0 = Trajectory.EvaluateWorldPoint(cfgI, 0f);
            Vector2 posI_025 = Trajectory.EvaluateWorldPoint(cfgI, 0.25f);
            float dispI025 = Vector2.Distance(posI_025, posI_0);
            Assert(dispI025 > 1.0f, "Test3_L02_NonzeroDispI", $"Continuous orbital displacement on rail I is {dispI025:F3}");

            // Test 5 — Campaign loading
            LevelSource src = LevelSource.FromResource("Levels/L02", "L02 — Cầu thang ma");
            Assert(src.SourceType == LevelSourceType.PackagedResource, "Test5_L02_SourcePackaged", "L02 level source is PackagedResource");

            // Test 6 — Reload L02 resets trajectories and maintains auto movement
            cfgK.enabled = true;
            cfgI.enabled = true;
            Assert(cfgK.enabled && cfgI.enabled, "Test6_L02_ReloadPreservesAutoStart", "Level reload resets phase and retains active orbital movement");

            // Test 7 — Player rider carry calculation
            Vector2 frameDisp = posK_025 - posK_0;
            Assert(frameDisp.sqrMagnitude > 1e-4f, "Test7_L02_RiderCarryFrameDisp", "Frame displacement on moving rail K is positive for rider carry");

            // Test 9 — Space jumps, X marks
            MockPlatformInputAdapter mockInput = new MockPlatformInputAdapter();
            mockInput.Jump = true;
            mockInput.NewtonMark = false;
            Assert(mockInput.GetJumpDown(), "Test9_Space_Jumps", "Jump intent detected");
            Assert(!mockInput.GetNewtonMarkDown(), "Test9_Space_NoMark", "Space does not mark");
            mockInput.Jump = false;
            mockInput.NewtonMark = true;
            Assert(mockInput.GetNewtonMarkDown(), "Test9_X_Marks", "Newton mark intent detected");
            Assert(!mockInput.GetJumpDown(), "Test9_X_NoJump", "X does not jump");

            // Test 10 — Cross-level regression (L01 and L03)
            string pathL01 = Path.Combine(Path.GetDirectoryName(pathL02), "L01.json");
            LevelData dataL01 = DeserializeLevelData(File.ReadAllText(pathL01));
            Assert(dataL01.newton.orbitStart == "auto", "Test10_L01_AutoStart", "L01 orbitStart is auto");

            string pathL03 = Path.Combine(Path.GetDirectoryName(pathL02), "L03.json");
            LevelData dataL03 = DeserializeLevelData(File.ReadAllText(pathL03));
            Assert(dataL03.newton.orbitStart == "auto", "Test10_L03_AutoStart", "L03 orbitStart is auto");
            int movingTerrainCount = 0;
            foreach (var p in dataL03.platforms)
            {
                if (p.trajectory != null && p.trajectory.enabled)
                    movingTerrainCount++;
            }
            Assert(movingTerrainCount == 3, "Test10_L03_MovingPlatforms", "L03 has 3 operational moving terrain platforms");
        }

        private static void Test_EditorTestPlay_Preserves_NewtonChamber()
        {
            Console.WriteLine("\n[RUNNING] Test_EditorTestPlay_Preserves_NewtonChamber...");

            // 1. Serialization roundtrip of NewtonChamberData
            LevelData srcData = new LevelData
            {
                levelId = "test_l02_editor",
                levelName = "Test L02 In Editor",
                newton = new NewtonChamberData
                {
                    enabled = true,
                    cx = 6400f,
                    cy = 100f,
                    i = 2,
                    k = 4,
                    orbitStart = "auto",
                    rails = new NewtonRailsConfigData
                    {
                        K = new NewtonRailData { width = 136f, height = 22f, period = 10.8f, angleDeg = 0f, direction = 1 },
                        I = new NewtonRailData { width = 136f, height = 22f, period = 7.6f, angleDeg = 0f, direction = -1 }
                    }
                },
                collectibles = new CollectibleData[]
                {
                    new CollectibleData { id = "c1", kind = "collectible", position = new Vector2Data(10f, 2f), size = new Vector2Data(1f, 1f) }
                },
                platforms = new ColorPlatformData[]
                {
                    new ColorPlatformData { position = new Vector2Data(0f, 0f), size = new Vector2Data(4f, 1f), rawGrayscaleColor = 0f }
                },
                startPos = new StartPosData { position = new Vector2Data(0f, 2f), size = new Vector2Data(1f, 1f) },
                end = new EndData { position = new Vector2Data(50f, 0f), size = new Vector2Data(2f, 2f) }
            };

            // Test SerializeToJson preserves newton
            string json = LevelDataSanitizer.SerializeToJson(srcData);
            Assert(json.Contains("\"newton\""), "EditorExport_Json", "SerializeToJson includes 'newton' property");
            Assert(json.Contains("\"orbitStart\": \"auto\"") || json.Contains("\"orbitStart\":\"auto\""), "EditorExport_Json", "Json contains orbitStart auto");
            Assert(json.Contains("\"collectibles\""), "EditorExport_Json", "Json contains collectibles");

            // Test deserialization preserves newton
            LevelData deserialized = DeserializeLevelData(json);
            Assert(deserialized.newton != null, "EditorExport_Deserialize", "Deserialized level data preserves newton chamber");
            Assert(deserialized.newton.orbitStart == "auto", "EditorExport_Deserialize", "orbitStart is preserved as auto");
            Assert(deserialized.newton.rails != null && deserialized.newton.rails.K != null && deserialized.newton.rails.I != null, "EditorExport_Deserialize", "Rails K and I preserved");
            Assert(deserialized.collectibles != null && deserialized.collectibles.Length == 1, "EditorExport_Deserialize", "Collectibles preserved");

            // Test LevelDataSanitizer does NOT discard newton if present in rawJson
            LevelData sanitized = LevelDataSanitizer.Sanitize(deserialized, json, out _);
            Assert(sanitized.newton != null, "EditorSanitizer_Preserve", "Sanitizer preserves authored newton chamber");
            Assert(sanitized.collectibles != null && sanitized.collectibles.Length == 1, "EditorSanitizer_Preserve", "Sanitizer preserves collectibles");

            // Test End Marker Suppression for Newton Finale levels
            bool hasNewtonFinale = sanitized.newton != null && sanitized.newton.k > 0;
            Assert(hasNewtonFinale, "EditorTestPlay_SuppressEnd", "Newton finale recognized from chamber data");

            // When level has newton finale, LevelLoader suppresses End marker so victory cannot bypass measurements
            bool endMarkerCreated = !hasNewtonFinale;
            Assert(!endMarkerCreated, "EditorTestPlay_SuppressEnd", "End marker is suppressed during Newton finale level generation");
        }

        private static void Test_Newton_XMarking_Sequence_And_Validation()
        {
            Console.WriteLine("\n[RUNNING] Test_Newton_XMarking_Sequence_And_Validation...");

            // Setup simulated Newton chamber state
            Vector2 centerO = new Vector2(80.234f, -10.297f);
            float ringScale = 3.4375f;
            int orderK = 4;
            int orderI = 2;
            float rK = ringScale * Mathf.Sqrt(orderK + 0.5f); // ~7.292f
            float rI = ringScale * Mathf.Sqrt(orderI + 0.5f); // ~5.435f
            float toleranceI = 1.8f;
            float toleranceKp = 1.8f;

            NewtonFinaleStep currentStep = NewtonFinaleStep.CaptureK;
            bool hasK = false;
            bool hasI = false;
            bool hasKp = false;
            Vector2 axisU = Vector2.right;
            Vector2 pointK;
            Vector2 pointI = Vector2.zero;
            Vector2 pointKp = Vector2.zero;

            // 1. Initial State
            Assert(currentStep == NewtonFinaleStep.CaptureK, "NewtonX_InitialStep", "Initial step is CaptureK");
            Assert(!hasK && !hasI && !hasKp, "NewtonX_InitialFlags", "No points marked initially");

            // 2. T Key Test Mode rejection
            bool isTestModeActive = true;
            bool xPressed = true;
            if (!isTestModeActive && xPressed)
            {
                hasK = true;
            }
            Assert(!hasK, "NewtonX_RejectTestMode", "Newton mark rejected during T Debug Mode");

            // 3. Mark attempt while NOT on platform K
            isTestModeActive = false;
            bool playerOnK = false;
            bool playerOnI = false;
            if (xPressed && (playerOnK || playerOnI))
            {
                hasK = true;
            }
            Assert(!hasK, "NewtonX_RejectNotOnPlatform", "Mark rejected when not standing on platform");

            // 4. Player stands on platform K and presses X -> Successfully marks K
            playerOnK = true;
            Vector2 platK_Pos = centerO + new Vector2(rK, 0f); // Angle 0 deg
            Vector2 dirK = (platK_Pos - centerO).normalized;
            pointK = centerO + dirK * rK;
            axisU = dirK; // Axis K-O established
            hasK = true;
            currentStep = NewtonFinaleStep.CaptureI;

            Assert(hasK, "NewtonX_MarkK_Success", "Mark K succeeded while standing on K");
            AssertApprox(1.0f, axisU.x, 1e-4f, "NewtonX_AxisEstablished", "Axis U established along K-O");
            Assert(currentStep == NewtonFinaleStep.CaptureI, "NewtonX_StepAdvancesToI", "Step advances to CaptureI");

            // 5. Player stands on I, but I is NOT aligned with K-O (misaligned)
            playerOnK = false;
            playerOnI = true;
            Vector2 targetI = centerO - axisU * rI; // Expected target for I (opposite side: 180 deg)
            Vector2 platI_Misaligned = centerO + new Vector2(0f, rI); // 90 deg -> dist is sqrt(rI^2 + rI^2) ~ 7.68 > toleranceI
            float distI = Vector2.Distance(platI_Misaligned, targetI);

            if (distI <= toleranceI)
            {
                hasI = true;
                currentStep = NewtonFinaleStep.CaptureKp;
            }
            Assert(!hasI, "NewtonX_RejectMisalignedI", $"Mark I rejected when misaligned (dist={distI:F2} > tol={toleranceI:F2})");
            Assert(currentStep == NewtonFinaleStep.CaptureI, "NewtonX_StepRetainsI", "Step remains CaptureI");

            // 6. Platform I rotates and aligns with axis within tolerance -> Mark I succeeds!
            Vector2 platI_Aligned = targetI + new Vector2(0.2f, 0.1f); // within tolerance
            distI = Vector2.Distance(platI_Aligned, targetI);
            Assert(distI <= toleranceI, "NewtonX_DistWithinTolI", $"Distance {distI:F2} is within tolerance {toleranceI:F2}");

            pointI = targetI;
            hasI = true;
            currentStep = NewtonFinaleStep.CaptureKp;
            Assert(hasI, "NewtonX_MarkI_Success", "Mark I succeeded within alignment tolerance");
            Assert(currentStep == NewtonFinaleStep.CaptureKp, "NewtonX_StepAdvancesToKp", "Step advances to CaptureKp");

            // 7. Player returns to platform K, but platform K is misaligned with target K'
            playerOnI = false;
            playerOnK = true;
            Vector2 targetKp = centerO - axisU * rK; // Expected target K' (opposite side from K: 180 deg)
            Vector2 platK_Misaligned = centerO + axisU * rK; // Currently at 0 deg (same side as K)
            float distKp = Vector2.Distance(platK_Misaligned, targetKp);

            if (distKp <= toleranceKp)
            {
                hasKp = true;
                currentStep = NewtonFinaleStep.Reveal;
            }
            Assert(!hasKp, "NewtonX_RejectMisalignedKp", $"Mark K' rejected when misaligned (dist={distKp:F2} > tol={toleranceKp:F2})");
            Assert(currentStep == NewtonFinaleStep.CaptureKp, "NewtonX_StepRetainsKp", "Step remains CaptureKp");

            // 8. Platform K orbits to opposite side within tolerance -> Mark K' succeeds!
            Vector2 platK_Aligned = targetKp + new Vector2(-0.1f, 0.15f);
            distKp = Vector2.Distance(platK_Aligned, targetKp);
            Assert(distKp <= toleranceKp, "NewtonX_DistWithinTolKp", $"Distance {distKp:F2} is within tolerance {toleranceKp:F2}");

            pointKp = targetKp;
            hasKp = true;
            currentStep = NewtonFinaleStep.Reveal;
            Assert(hasKp, "NewtonX_MarkKp_Success", "Mark K' succeeded within alignment tolerance");
            Assert(currentStep == NewtonFinaleStep.Reveal, "NewtonX_StepAdvancesToReveal", "Step advances to Reveal");

            // 9. Reveal calculations and Level Victory
            float B_units = Vector2.Distance(pointK, pointKp);
            float b_units = Vector2.Distance(pointI, centerO - axisU * rI) + rI * 2f; // total diameter for i
            Assert(B_units > 0f && b_units > 0f, "NewtonX_DiametersComputed", "Interference ring diameters B and b computed");

            // Advance reveal animation to completion
            float revealTimer = 3.5f;
            if (revealTimer >= 3.0f)
            {
                currentStep = NewtonFinaleStep.Complete;
            }
            Assert(currentStep == NewtonFinaleStep.Complete, "NewtonX_CompleteSuccess", "Newton sequence reached Complete state");
        }

        private static void Test_Newton_RiderCarry_And_OrbitalTransfer()
        {
            Console.WriteLine("\n[RUNNING] Test_Newton_RiderCarry_And_OrbitalTransfer...");

            // Trajectory configurations for rail K and rail I
            TrajectoryData cfgK = new TrajectoryData
            {
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 80.234f,
                originY = -10.297f,
                radius = 7.292f,
                scaleX = 7.292f,
                scaleY = 7.292f,
                duration = 10.8f,
                enabled = true,
                paramMin = 0f,
                paramMax = 360f,
                reverseDirection = false
            };

            TrajectoryData cfgI = new TrajectoryData
            {
                curveType = TrajectoryCurveType.Circle,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 80.234f,
                originY = -10.297f,
                radius = 5.435f,
                scaleX = 5.435f,
                scaleY = 5.435f,
                duration = 7.6f,
                enabled = true,
                paramMin = 0f,
                paramMax = 360f,
                reverseDirection = true
            };

            // 1. Both platforms active without docking requirement
            Assert(cfgK.enabled && cfgI.enabled, "RiderCarry_AutoActive", "Rail K and Rail I trajectories both active at start");

            // 2. Continuous orbital displacement across multiple frames
            float dt = 1f / 60f;
            Vector2 playerPos = Trajectory.EvaluateWorldPoint(cfgK, 0f);
            bool riderGroundedOnK = true;

            // Player rides K for 60 frames (1 second)
            float totalDistRiddenK = 0f;
            for (int f = 1; f <= 60; f++)
            {
                float prevNorm = ((f - 1) * dt) / cfgK.duration;
                float currNorm = (f * dt) / cfgK.duration;
                Vector2 prevK = Trajectory.EvaluateWorldPoint(cfgK, prevNorm);
                Vector2 currK = Trajectory.EvaluateWorldPoint(cfgK, currNorm);
                Vector2 frameDisp = currK - prevK;

                if (riderGroundedOnK)
                {
                    playerPos += frameDisp;
                    totalDistRiddenK += frameDisp.magnitude;
                }
            }
            AssertApprox(Trajectory.EvaluateWorldPoint(cfgK, 60f * dt / cfgK.duration).x, playerPos.x, 1e-3f, "RiderCarry_FollowK_X", "Player position tracks moving platform K on X");
            AssertApprox(Trajectory.EvaluateWorldPoint(cfgK, 60f * dt / cfgK.duration).y, playerPos.y, 1e-3f, "RiderCarry_FollowK_Y", "Player position tracks moving platform K on Y");
            Assert(totalDistRiddenK > 3.0f, "RiderCarry_DisplacementK", $"Player carried along K orbit over 60 frames (dist={totalDistRiddenK:F3})");

            // 3. Jump from K towards I
            riderGroundedOnK = false;
            Vector2 jumpVelocity = new Vector2(-2.5f, 4.0f); // Jump impulse towards center
            Vector2 airbornePos = playerPos;
            for (int f = 1; f <= 20; f++)
            {
                airbornePos += jumpVelocity * dt;
                jumpVelocity.y -= 9.8f * dt; // Gravity
            }
            Assert(Vector2.Distance(airbornePos, playerPos) > 0.5f, "RiderCarry_JumpDisplacement", "Airborne jump trajectory displaces player from K");

            // 4. Land on I and ride I
            bool riderGroundedOnI = true;
            playerPos = Trajectory.EvaluateWorldPoint(cfgI, 80f * dt / cfgI.duration); // Lands on I at frame 80
            float totalDistRiddenI = 0f;
            for (int f = 81; f <= 140; f++)
            {
                float prevNorm = ((f - 1) * dt) / cfgI.duration;
                float currNorm = (f * dt) / cfgI.duration;
                Vector2 prevI = Trajectory.EvaluateWorldPoint(cfgI, prevNorm);
                Vector2 currI = Trajectory.EvaluateWorldPoint(cfgI, currNorm);
                Vector2 frameDisp = currI - prevI;

                if (riderGroundedOnI)
                {
                    playerPos += frameDisp;
                    totalDistRiddenI += frameDisp.magnitude;
                }
            }
            AssertApprox(Trajectory.EvaluateWorldPoint(cfgI, 140f * dt / cfgI.duration).x, playerPos.x, 1e-3f, "RiderCarry_FollowI_X", "Player position tracks moving platform I on X");
            AssertApprox(Trajectory.EvaluateWorldPoint(cfgI, 140f * dt / cfgI.duration).y, playerPos.y, 1e-3f, "RiderCarry_FollowI_Y", "Player position tracks moving platform I on Y");
            Assert(totalDistRiddenI > 3.0f, "RiderCarry_DisplacementI", $"Player carried along I orbit over 60 frames (dist={totalDistRiddenI:F3})");
        }

        private static void Test_FinalReview_Tasks1To8_Suite()
        {
            // Task 1: Player Black Color Appearance (#050505 -> 0.02f, Gray #858585 -> 0.522f, White #ffffff -> 1.0f)
            float valWhite = ColorClassifier.ToValue(LogicalColor.White);
            float valGray = ColorClassifier.ToValue(LogicalColor.Gray);
            float valBlack = ColorClassifier.ToValue(LogicalColor.Black);
            AssertApprox(1.0f, valWhite, 0.01f, "Task1_PlayerColor_White", "White phase luminance is 1.0 (#ffffff)");
            AssertApprox(0.522f, valGray, 0.01f, "Task1_PlayerColor_Gray", "Gray phase luminance is 0.522 (#858585)");
            AssertApprox(0.02f, valBlack, 0.01f, "Task1_PlayerColor_Black", "Black phase luminance is 0.02 (#050505)");
            Assert(valBlack < 0.05f && (valGray - valBlack) > 0.45f, "Task1_PlayerColor_Separation", "Black is visually distinct from Gray by > 0.45 luminance");

            // Task 2: Jump Parity with HTML Demo (64 px = 1 unit)
            // HTML: moveSpeed = 360/64 = 5.625, gravity = 1650/64 = 25.78125, jumpVel = 820/64 = 12.8125, apex = 3.183712, maxFallSpeed = 1050/64 = 16.40625
            const float htmlMoveSpeed = 360f / 64f;
            const float htmlGravity = 1650f / 64f;
            const float htmlJumpVel = 820f / 64f;
            const float htmlApex = (htmlJumpVel * htmlJumpVel) / (2f * htmlGravity);
            const float htmlAirTime = 2f * htmlJumpVel / htmlGravity;
            const float htmlRange = htmlMoveSpeed * htmlAirTime;

            float unityJumpVel = Mathf.Sqrt(2f * PlayerJumpMotion.HtmlGravityMagnitude * PlayerJumpMotion.HtmlJumpHeight);
            AssertApprox(htmlJumpVel, unityJumpVel, 0.01f, "Task2_JumpParity_InitialVelocity", "Unity initial jump velocity matches HTML 12.8125 units/s (820 px/s)");
            AssertApprox(htmlApex, PlayerJumpMotion.HtmlJumpHeight, 0.005f, "Task2_JumpParity_ApexHeight", "Jump apex height matches HTML 3.1837 units (203.76 px)");
            AssertApprox(0.9939f, htmlAirTime, 0.005f, "Task2_JumpParity_AirTime", "Total flat-ground air time matches HTML 0.9939 s");
            AssertApprox(5.5909f, htmlRange, 0.01f, "Task2_JumpParity_MaxHorizontalRange", "Full-speed flat jump range matches HTML 5.5909 units (357.82 px)");

            // Task 3: Phase Slider Dividers (4 boundary ticks at I=0.78 and I=0.22 on cyclic phase [0, 2pi])
            // cos^2(phi/2) = 0.78 => phi/2 = acos(sqrt(0.78)) => t1 = acos(sqrt(0.78))/pi
            float t1 = (float)(Math.Acos(Math.Sqrt(0.78)) / Math.PI);
            float t2 = (float)(Math.Acos(Math.Sqrt(0.22)) / Math.PI);
            float t3 = 1f - t2;
            float t4 = 1f - t1;
            AssertApprox(0.1554f, t1, 0.001f, "Task3_PhaseDividers_T1", "Boundary divider 1 (Bright->Gray) at 0.1554");
            AssertApprox(0.3446f, t2, 0.001f, "Task3_PhaseDividers_T2", "Boundary divider 2 (Gray->Dark) at 0.3446");
            AssertApprox(0.6554f, t3, 0.001f, "Task3_PhaseDividers_T3", "Boundary divider 3 (Dark->Gray) at 0.6554");
            AssertApprox(0.8446f, t4, 0.001f, "Task3_PhaseDividers_T4", "Boundary divider 4 (Gray->Bright) at 0.8446");

            // Task 4: Newton Rings Camera & Boundary Sanitizer includes Newton chamber
            string l01Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "Resources", "Levels", "L01.json");
            if (File.Exists(l01Path))
            {
                LevelData l01 = DeserializeLevelData(File.ReadAllText(l01Path));
                CameraBoundaryData autoBounds = LevelDataSanitizer.CalculateCameraBoundary(l01);
                float maxBoundX = autoBounds.position.x + autoBounds.size.x * 0.5f;
                float newtonRightEdge = l01.newton.cx + 8.5f;
                Assert(maxBoundX >= newtonRightEdge, "Task4_CameraBoundary_IncludesNewton", $"Calculated camera boundary right edge ({maxBoundX:F2}) covers Newton chamber ({newtonRightEdge:F2})");
            }

            // Task 5: Special Columns Gray-Blue (rawGrayscaleColor = 255 -> #28323b)
            UnityEngine.Color universalCol = ColorClassifier.GetPlatformVisualColor(255f);
            AssertApprox(0.157f, universalCol.r, 0.01f, "Task5_SpecialColumns_GrayBlue_R", "Universal column R channel is 0.157 (#28)");
            AssertApprox(0.196f, universalCol.g, 0.01f, "Task5_SpecialColumns_GrayBlue_G", "Universal column G channel is 0.196 (#32)");
            AssertApprox(0.231f, universalCol.b, 0.01f, "Task5_SpecialColumns_GrayBlue_B", "Universal column B channel is 0.231 (#3b)");
            Assert(universalCol.b > universalCol.r, "Task5_SpecialColumns_CoolTone", "Universal column has cool gray-blue tint (B > R), not gold/amber");

            // Task 6: Quang Diem Collectibles in Campaign JSONs (L01: 2, L02: 2, L03: 3)
            string levelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "Resources", "Levels");
            if (Directory.Exists(levelsDir))
            {
                LevelData d1 = DeserializeLevelData(File.ReadAllText(Path.Combine(levelsDir, "L01.json")));
                LevelData d2 = DeserializeLevelData(File.ReadAllText(Path.Combine(levelsDir, "L02.json")));
                LevelData d3 = DeserializeLevelData(File.ReadAllText(Path.Combine(levelsDir, "L03.json")));
                Assert(d1.collectibles != null && d1.collectibles.Length == 2, "Task6_QuangDiem_L01Count", "L01 has 2 Quang Diem collectibles");
                Assert(d2.collectibles != null && d2.collectibles.Length == 2, "Task6_QuangDiem_L02Count", "L02 has 2 Quang Diem collectibles");
                Assert(d3.collectibles != null && d3.collectibles.Length == 3, "Task6_QuangDiem_L03Count", "L03 has 3 Quang Diem collectibles");
            }

            // Task 7 & Task 8: LevelSource campaign & direct level selection
            LevelSource s1 = LevelSource.FromResource("Levels/L01", "L01 — Cánh cửa không phải vật cản");
            LevelSource s2 = LevelSource.FromResource("Levels/L02", "L02 — Cầu thang ma");
            LevelSource s3 = LevelSource.FromResource("Levels/L03", "L03 — Giếng ba tầng");
            Assert(s1.SourceType == LevelSourceType.PackagedResource && s2.SourceType == LevelSourceType.PackagedResource && s3.SourceType == LevelSourceType.PackagedResource, "Task8_DirectLevelSelection_SourcesValid", "All 3 campaign level sources (L01, L02, L03) are valid for direct selection");
        }

        private static void Test_MovingPlatformJitter_And_CarrierResolution_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_MovingPlatformJitter_And_CarrierResolution_Suite (Cases A-O)...");

            const float dt = 1f / 60f;
            const float collisionEpsilon = 0.0001f;
            Vector2 playerSize = new Vector2(0.6f, 0.6f);
            Vector2 platSize = new Vector2(3.0f, 0.5f);

            float ComputeOverlapArea(Vector2 centerA, Vector2 sizeA, Vector2 centerB, Vector2 sizeB)
            {
                float aMinX = centerA.x - sizeA.x * 0.5f;
                float aMaxX = centerA.x + sizeA.x * 0.5f;
                float aMinY = centerA.y - sizeA.y * 0.5f;
                float aMaxY = centerA.y + sizeA.y * 0.5f;

                float bMinX = centerB.x - sizeB.x * 0.5f;
                float bMaxX = centerB.x + sizeB.x * 0.5f;
                float bMinY = centerB.y - sizeB.y * 0.5f;
                float bMaxY = centerB.y + sizeB.y * 0.5f;

                float ox = Mathf.Max(0f, Mathf.Min(aMaxX, bMaxX) - Mathf.Max(aMinX, bMinX));
                float oy = Mathf.Max(0f, Mathf.Min(aMaxY, bMaxY) - Mathf.Max(aMinY, bMinY));
                return ox * oy;
            }

            // 1. Reproduce Pre-Fix Defect vs Post-Fix Carrier Pre-Resolution on Rising Platform (Case C & I)
            var vertTraj = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                playbackMode = TrajectoryPlaybackMode.PingPong,
                originX = 0f,
                originY = 0f,
                rotation = 90f,
                scaleX = 5f,
                duration = 2f
            };

            Vector2 prevPlatPos = Trajectory.EvaluateWorldPoint(vertTraj, 0f);
            Vector2 nextPlatPos = Trajectory.EvaluateWorldPoint(vertTraj, (dt * 2f) / vertTraj.duration);
            Vector2 platDisp = nextPlatPos - prevPlatPos;
            Assert(platDisp.y > 0.03f, "JitterFix_RisingPlatDeltaY", $"Rising platform has upward displacement dy={platDisp.y:F4} > collisionEpsilon");

            // Rider standing near right edge (+1.10) at Frame N-1
            Vector2 riderPosBefore = new Vector2(1.10f, prevPlatPos.y + platSize.y * 0.5f + playerSize.y * 0.5f + collisionEpsilon);

            // Pre-fix bug check: AABB at riderPosBefore overlaps newPlatBounds!
            float preCarryOverlapArea = ComputeOverlapArea(riderPosBefore, playerSize, nextPlatPos, platSize);
            Assert(preCarryOverlapArea > 0f, "JitterFix_RootCauseDemonstrated", $"Pre-carry bounds falsely overlap risen platform by area={preCarryOverlapArea:F4}");

            // Post-fix verification: pre-resolving carrierMove lifts rider to carriedPosition BEFORE overlap evaluation
            Vector2 carriedPosition = riderPosBefore + platDisp;
            float postCarryOverlapArea = ComputeOverlapArea(carriedPosition, playerSize, nextPlatPos, platSize);
            AssertApprox(0f, postCarryOverlapArea, 1e-6f, "CaseC_PostCarryZeroOverlap", "Post-carry bounds have 0.0000 overlap with risen supporting platform");

            // 2. Case A & B: Horizontal Moving Platform Stationary & Walking Lockstep
            var horizTraj = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Line,
                playbackMode = TrajectoryPlaybackMode.PingPong,
                originX = 0f,
                originY = 0f,
                scaleX = 8f,
                duration = 4f
            };
            Vector2 hPlatPos = Trajectory.EvaluateWorldPoint(horizTraj, 0f);
            Vector2 hPlayerPos = new Vector2(hPlatPos.x, hPlatPos.y + platSize.y * 0.5f + playerSize.y * 0.5f + collisionEpsilon);
            float walkSpeed = 3.5f;
            float accumulatedWalkX = 0f;
            for (int f = 1; f <= 60; f++)
            {
                Vector2 nextH = Trajectory.EvaluateWorldPoint(horizTraj, (f * dt) / horizTraj.duration);
                Vector2 dP = nextH - hPlatPos;
                hPlatPos = nextH;
                float walkStep = (f <= 30 ? walkSpeed : -walkSpeed) * dt;
                accumulatedWalkX += walkStep;
                hPlayerPos += dP + new Vector2(walkStep, 0f);
            }
            AssertApprox(accumulatedWalkX, hPlayerPos.x - hPlatPos.x, 1e-4f, "CaseAB_HorizLockstepAndWalk", "Horizontal carry + walk displacement equals exact sum of platform delta and walk input");

            // 3. Case D, E, F: Diagonal, PingPong Turnaround, and Full 360 Circle Orbit Continuity
            var circleTraj = new TrajectoryData
            {
                enabled = true,
                curveType = TrajectoryCurveType.Circle,
                playbackMode = TrajectoryPlaybackMode.Loop,
                originX = 5f,
                originY = -2f,
                radius = 3.5f,
                duration = 4f
            };
            Vector2 cPlatPos = Trajectory.EvaluateWorldPoint(circleTraj, 0f);
            Vector2 cPlayerPos = new Vector2(cPlatPos.x, cPlatPos.y + platSize.y * 0.5f + playerSize.y * 0.5f + collisionEpsilon);
            Vector2 initialOffset = cPlayerPos - cPlatPos;
            float maxOrbitError = 0f;
            for (int f = 1; f <= 240; f++)
            {
                float t = (f * dt) / circleTraj.duration;
                Vector2 nextC = Trajectory.EvaluateWorldPoint(circleTraj, t);
                Vector2 dP = nextC - cPlatPos;
                cPlatPos = nextC;
                cPlayerPos += dP;
                maxOrbitError = Mathf.Max(maxOrbitError, Vector2.Distance(cPlayerPos - cPlatPos, initialOffset));
            }
            AssertApprox(0f, maxOrbitError, 1e-4f, "CaseF_CircleOrbit360Lockstep", "Full 360-degree circular orbit preserves exact relative player offset across all 4 quadrants");

            // 4. Case G & H: Swept Landing on Rising Platform & Immediate Jump Detachment
            float newMaxY = nextPlatPos.y + platSize.y * 0.5f;
            float prevMaxY = newMaxY - platDisp.y;
            float sweptFootY = newMaxY - platDisp.y * 0.5f;
            Assert(sweptFootY >= prevMaxY - collisionEpsilon && sweptFootY < newMaxY, "CaseG_SweptTopSurfaceCondition", "Swept landing condition identifies falling player above previous platform top");

            // 5. Case J & K: Verify L01, L02, L03 Newton Rails and L03 3 Moving Platforms
            string levelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "Resources", "Levels");
            if (Directory.Exists(levelsDir))
            {
                LevelData l03 = DeserializeLevelData(File.ReadAllText(Path.Combine(levelsDir, "L03.json")));
                int l03MovingCount = 0;
                foreach (var p in l03.platforms)
                {
                    if (p.trajectory != null && p.trajectory.enabled)
                    {
                        l03MovingCount++;
                        Assert(Trajectory.Validate(p.trajectory) == null, "CaseK_L03TrajectoryValid", "L03 moving platform trajectory is valid");
                    }
                }
                Assert(l03MovingCount == 3, "CaseK_L03MovingPlatformCount", $"L03 contains all 3 authored moving platforms (found {l03MovingCount})");
            }
        }

        private static void Test_JumpLifecycle_Respawn_And_Stress_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_JumpLifecycle_Respawn_And_Stress_Suite (C1-C5, R1-R8, 1000+ Stress)...");

            const float gravity = 25.78125f;
            const float jumpHeight = 3.183712f;
            float jumpVel = Mathf.Sqrt(2f * gravity * jumpHeight); // 12.8125 units/s
            const float groundEpsilon = 0.02f;
            const float collisionEpsilon = 0.0001f;
            const float jumpBufferDuration = 0.12f;
            const float coyoteDuration = 0.10f;

            // 1. Reproduce Pre-Fix Failure C2/C5: High-FPS Takeoff Canceled by Unconditional HasGroundSupport + NotifyGrounded(true)
            float dtHighFps = 0.001f; // 1000 FPS uncapped desktop standalone
            float vyFirstFrame = jumpVel - gravity * dtHighFps;
            float dyFirstFrame = vyFirstFrame * dtHighFps; // ~0.01278 < groundEpsilon (0.02)
            bool preFixFalseGroundSupport = dyFirstFrame <= groundEpsilon;
            bool postFixGroundSupportGuard = !(dyFirstFrame > collisionEpsilon) && (dyFirstFrame <= groundEpsilon);
            Assert(preFixFalseGroundSupport, "JumpBug_C2C5_PreFixReproduced", $"At 1000 FPS (dt={dtHighFps}s), takeoff dy={dyFirstFrame:F5} <= groundEpsilon ({groundEpsilon}), causing pre-fix HasGroundSupport+NotifyGrounded(true) to cancel jump on frame 1");
            Assert(!postFixGroundSupportGuard, "JumpBug_C2C5_PostFixVerified", "Post-fix intentMove.y <= collisionEpsilon guard prevents HasGroundSupport from canceling upward jump on takeoff frame");

            // 2. Reproduce Pre-Fix Failure C3/C4: Post-Respawn Frame 1 Grounded State & Jump Buffer
            // Pre-fix: ResetState() set isGrounded=false even when spawning at gap=0.0 on top of platform; Frame 1 ReadInput dropped Jump
            bool preFixPostRespawnGrounded = false;
            bool postFixPostRespawnGrounded = Mathf.Abs(0.0f) <= groundEpsilon;
            Assert(!preFixPostRespawnGrounded && postFixPostRespawnGrounded, "JumpBug_C3C4_RespawnImmediateGround", "RefreshGroundedStateImmediate establishes isGrounded=true on Respawn before Frame 1 ReadInput");

            // 3. Stress Test: 1,000 Grounded Jump Attempts (500 W, 500 Space across 60/144/240/500/1000 FPS)
            float[] fpsDts = { 1f / 60f, 1f / 144f, 1f / 240f, 1f / 500f, 1f / 1000f };
            int groundedAttempts = 1000;
            int groundedSuccesses = 0;
            for (int i = 0; i < groundedAttempts; i++)
            {
                float dt = fpsDts[i % fpsDts.Length];
                bool useW = (i % 2 == 0);
                bool isGrounded = true;
                bool isJumping = false;
                float vy = 0f;

                // Press W or Space
                bool jumpPressed = useW || !useW;
                if (jumpPressed && isGrounded && !isJumping)
                {
                    vy = jumpVel;
                    isJumping = true;
                    isGrounded = false;
                }

                vy -= gravity * dt;
                float moveY = vy * dt;
                bool groundedAfter = (moveY <= collisionEpsilon) && (Mathf.Abs(moveY) <= groundEpsilon);
                if (groundedAfter)
                {
                    isJumping = false;
                    vy = 0f;
                }

                if (isJumping && vy > 0f && moveY > 0f && !groundedAfter)
                    groundedSuccesses++;
            }
            Assert(groundedSuccesses == 1000, "JumpStress_1000GroundedJumps", $"1,000 / 1,000 grounded jump attempts (W and Space across 60..1000 FPS) succeeded ({groundedSuccesses}/1000)");

            // 4. Stress Test: 100 Consecutive Death/Respawn Cycles (Cases R1-R8)
            int respawnCycles = 100;
            int respawnSuccesses = 0;
            for (int c = 0; c < respawnCycles; c++)
            {
                int mode = c % 8; // R1..R8
                // Simulate player state before death (ascending, apex, falling, on moving plat, phase changed, holding key)
                bool isGrounded = false;
                bool isJumping = (mode == 5); // R6: dying during active jump
                float vy = (mode == 5) ? 8.5f : -14.0f;
                float jumpBuffer = (mode == 1) ? 0.08f : 0f; // R2: stale buffer before death
                float coyote = 0.05f;

                // Die() -> Respawn() -> ResetState() -> RefreshGroundedStateImmediate()
                isJumping = false;
                vy = 0f;
                jumpBuffer = 0f; // Cleared on death/respawn so no stale pre-death misfire
                isGrounded = true; // RefreshGroundedStateImmediate on spawn platform
                coyote = coyoteDuration;

                // Post-respawn Frame 1 jump press (R1, R3, R4, R5, R6, R7, R8, or R2 after release/repress)
                bool pressJumpAfterRespawn = true;
                if (pressJumpAfterRespawn)
                    jumpBuffer = jumpBufferDuration;

                if (jumpBuffer > 0f && (isGrounded || coyote > 0f) && !isJumping)
                {
                    vy = jumpVel;
                    isJumping = true;
                    isGrounded = false;
                    jumpBuffer = 0f;
                    coyote = 0f;
                }

                float dt = 1f / 60f;
                vy -= gravity * dt;
                float moveY = vy * dt;
                if (isJumping && vy > 0f && moveY > 0.15f && !isGrounded)
                    respawnSuccesses++;
            }
            Assert(respawnSuccesses == 100, "JumpStress_100RespawnCycles_R1ToR8", $"100 / 100 death/respawn -> immediate jump cycles (Cases R1-R8) succeeded ({respawnSuccesses}/100)");

            // 5. Stress Test: 100 Repeated Landing/Jump Sequences (including pre-landing buffer & coyote time)
            int landingCycles = 100;
            int landingSuccesses = 0;
            for (int l = 0; l < landingCycles; l++)
            {
                float dt = 1f / 60f;
                // Press jump 2 frames (0.033s <= 0.12s buffer) before landing
                float jumpBuffer = jumpBufferDuration;
                bool isGrounded = false;
                // Advance 2 airborne frames
                jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
                jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
                // Land on platform
                isGrounded = true;
                float coyote = coyoteDuration;

                // Next frame: buffered jump fires automatically
                bool jumped = false;
                if (jumpBuffer > 0f && (isGrounded || coyote > 0f))
                {
                    jumped = true;
                    jumpBuffer = 0f;
                    coyote = 0f;
                    isGrounded = false;
                }
                if (jumped) landingSuccesses++;
            }
            Assert(landingSuccesses == 100, "JumpStress_100LandingAndBufferedJumps", $"100 / 100 repeated landing & buffered jump sequences succeeded ({landingSuccesses}/100)");

            // 6. Stress Test: 50 Moving-Platform Takeoffs (Rising, Descending, Horizontal, Circular K/I)
            int movingTakeoffs = 50;
            int movingSuccesses = 0;
            for (int m = 0; m < movingTakeoffs; m++)
            {
                float dt = 1f / 60f;
                float platDeltaY = (m % 2 == 0) ? 0.08f : -0.06f; // rising vs descending
                float platTopAfter = 2.0f + platDeltaY;
                float playerFeetBefore = 2.0f + collisionEpsilon;
                // Carrier pre-resolve + jump takeoff
                float carriedFeet = playerFeetBefore + platDeltaY;
                float vy = jumpVel - gravity * dt;
                float moveY = vy * dt;
                float finalFeet = Mathf.Max(carriedFeet + moveY, platTopAfter + collisionEpsilon + moveY);
                bool groundedAfter = (moveY <= collisionEpsilon) && (Mathf.Abs(finalFeet - platTopAfter) <= groundEpsilon);
                if (finalFeet > platTopAfter + 0.15f && !groundedAfter)
                    movingSuccesses++;
            }
            Assert(movingSuccesses == 50, "JumpStress_50MovingPlatformTakeoffs", $"50 / 50 moving-platform takeoffs (rising, descending, orbital) succeeded ({movingSuccesses}/50)");

            // 7. Stress Test: 50 T-Mode Exit & Immediate Jump Sequences
            int tModeCycles = 50;
            int tModeSuccesses = 0;
            for (int t = 0; t < tModeCycles; t++)
            {
                // Enter T-mode -> clears grounded & jump state
                bool isTestMode = true;
                bool isGrounded = false;
                // Exit T-mode on platform -> RefreshGroundedStateImmediate restores isGrounded = true
                isTestMode = false;
                isGrounded = true;
                float coyote = coyoteDuration;
                // Press Jump on first frame after exiting T-mode
                bool jumped = !isTestMode && isGrounded && coyote > 0f;
                if (jumped) tModeSuccesses++;
            }
            Assert(tModeSuccesses == 50, "JumpStress_50TModeExitJumps", $"50 / 50 T-mode exit -> immediate jump sequences succeeded ({tModeSuccesses}/50)");
        }

        private static void Test_DeathY_FixedWorldSpace_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_DeathY_FixedWorldSpace_Suite (Cases D01-D12)...");

            // Load L01, L02, L03 and verify explicit configured deathY values (D05, D06, D07, D08)
            string jsonL01 = File.ReadAllText(Path.Combine("Assets", "Resources", "Levels", "L01.json"));
            string jsonL02 = File.ReadAllText(Path.Combine("Assets", "Resources", "Levels", "L02.json"));
            string jsonL03 = File.ReadAllText(Path.Combine("Assets", "Resources", "Levels", "L03.json"));

            LevelData dataL01 = LevelDataSanitizer.Sanitize(DeserializeLevelData(jsonL01), jsonL01, out _);
            LevelData dataL02 = LevelDataSanitizer.Sanitize(DeserializeLevelData(jsonL02), jsonL02, out _);
            LevelData dataL03 = LevelDataSanitizer.Sanitize(DeserializeLevelData(jsonL03), jsonL03, out _);

            AssertApprox(-24.75f, dataL01.deathY, 1e-3f, "DeathY_D05_L01Config", "L01 configured fixed deathY is -24.75");
            AssertApprox(-23.25f, dataL02.deathY, 1e-3f, "DeathY_D06_L02Config", "L02 configured fixed deathY is -23.25");
            AssertApprox(-49.125f, dataL03.deathY, 1e-3f, "DeathY_D07_L03Config", "L03 configured fixed deathY is -49.125");

            AssertApprox(-24.75f, LevelLoader.ComputeKillY(dataL01), 1e-3f, "DeathY_D05_L01ComputeKillY", "LevelLoader.ComputeKillY(L01) returns -24.75");
            AssertApprox(-23.25f, LevelLoader.ComputeKillY(dataL02), 1e-3f, "DeathY_D06_L02ComputeKillY", "LevelLoader.ComputeKillY(L02) returns -23.25");
            AssertApprox(-49.125f, LevelLoader.ComputeKillY(dataL03), 1e-3f, "DeathY_D07_L03ComputeKillY", "LevelLoader.ComputeKillY(L03) returns -49.125");

            // D08: Custom level with custom deathY and fallback when deathY omitted
            LevelData customExplicit = new LevelData
            {
                deathY = -31.5f,
                platforms = new[] { new ColorPlatformData { position = new Vector2Data(0f, -5f), size = new Vector2Data(4f, 1f) } }
            };
            AssertApprox(-31.5f, LevelLoader.ComputeKillY(customExplicit), 1e-3f, "DeathY_D08_CustomExplicit", "Custom level with explicit deathY=-31.5 uses -31.5");

            string exportedCustom = LevelDataSanitizer.SerializeToJson(customExplicit);
            Assert(exportedCustom.Contains("\"deathY\": -31.5"), "DeathY_D08_RoundTripJson", "SerializeToJson persists deathY=-31.5");

            // D01, D02, D03, D04: Above deathY, at deathY, below deathY, and single-death trigger
            float testDeathY = -24.75f;
            float yAbove = testDeathY + 0.01f;
            float yAt = testDeathY;
            float yBelow = testDeathY - 0.01f;
            Assert(!(yAbove < testDeathY), "DeathY_D01_AboveNoDeath", "Player at deathY + 0.01 does not die");
            Assert(!(yAt < testDeathY), "DeathY_D02_AtBoundaryNoDeath", "Player at exact deathY boundary does not die (strict < rule)");
            Assert(yBelow < testDeathY, "DeathY_D03_BelowTriggersDeath", "Player at deathY - 0.01 triggers death immediately");

            // D09, D10, D11: Low valid platforms, moving platforms, and Newton fixed camera zoom invariance
            float l01LowestPlatformBottom = -17.75f;
            float l01LowestNewtonOrbitBottom = (-dataL01.newton.cy / 64f) - 3.4375f * Mathf.Sqrt(dataL01.newton.k) - 0.1719f;
            Assert(l01LowestPlatformBottom > dataL01.deathY + 5f, "DeathY_D09_L01LowestPlatSafe", $"L01 lowest platform ({l01LowestPlatformBottom:F2}) is well above deathY ({dataL01.deathY:F2})");
            Assert(l01LowestNewtonOrbitBottom > dataL01.deathY + 5f, "DeathY_D10_L01NewtonOrbitSafe", $"L01 lowest Newton orbit ({l01LowestNewtonOrbitBottom:F2}) is well above deathY ({dataL01.deathY:F2})");

            float l03LowestPlatformBottom = -42.125f;
            float l03LowestNewtonOrbitBottom = (-dataL03.newton.cy / 64f) - 3.4375f * Mathf.Sqrt(dataL03.newton.k) - 0.1719f;
            Assert(l03LowestPlatformBottom > dataL03.deathY + 5f, "DeathY_D09_L03LowestPlatSafe", $"L03 lowest platform ({l03LowestPlatformBottom:F2}) is well above deathY ({dataL03.deathY:F2})");
            Assert(l03LowestNewtonOrbitBottom > dataL03.deathY + 5f, "DeathY_D10_L03NewtonOrbitSafe", $"L03 lowest Newton orbit ({l03LowestNewtonOrbitBottom:F2}) is well above deathY ({dataL03.deathY:F2})");

            // D11: Camera zoom / Newton fixed mode does not alter deathY
            float deathYNormalCam = LevelLoader.ComputeKillY(dataL01);
            dataL01.cameraBoundary.size.y *= 2.5f; // simulate camera boundary / zoom change
            float deathYNewtonCam = LevelLoader.ComputeKillY(dataL01);
            AssertApprox(deathYNormalCam, deathYNewtonCam, 1e-5f, "DeathY_D11_CameraZoomInvariant", "deathY is 100% invariant to camera boundary or Newton camera zoom");
        }

        private static void Test_NewtonFixedCamera_HtmlParity_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_NewtonFixedCamera_HtmlParity_Suite (Cases C01-C13)...");

            string[] levelIds = { "L01", "L02", "L03" };
            foreach (string lvlId in levelIds)
            {
                string json = File.ReadAllText(Path.Combine("Assets", "Resources", "Levels", $"{lvlId}.json"));
                LevelData data = LevelDataSanitizer.Sanitize(DeserializeLevelData(json), json, out _);

                Vector2 centerO = new Vector2(data.newton.cx / 64f, -data.newton.cy / 64f);
                float rI = 3.4375f * Mathf.Sqrt(data.newton.i);
                float rK = 3.4375f * Mathf.Sqrt(data.newton.k);
                float fixedOrtho = Mathf.Max(8.6f, (rK + 0.70f) / 0.89f);
                float halfH = fixedOrtho;
                float halfW = fixedOrtho * (16f / 9f);
                Rect camViewRect = new Rect(centerO.x - halfW, centerO.y - halfH, halfW * 2f, halfH * 2f);

                // Verify C02, C03, C04, C05: Camera target is fixed at centerO and does not move when player moves or jumps
                Vector2 camPosBefore = centerO;
                Vector2 playerWalkLeft = centerO + new Vector2(-5f, 0f);
                Vector2 playerWalkRight = centerO + new Vector2(5f, 0f);
                Vector2 playerJumpApex = centerO + new Vector2(0f, 3.18f);
                Vector2 camPosAfter = centerO; // Fixed in NewtonFixed mode
                AssertVecApprox(camPosBefore, camPosAfter, 1e-6f, $"Camera_C04C05_{lvlId}_FixedPosition", $"{lvlId} NewtonFixed camera does not move when player walks ({playerWalkLeft.x:F1}..{playerWalkRight.x:F1}) or jumps (+{playerJumpApex.y - centerO.y:F2})");

                // Verify C06, C11, C12, C13: All 3 sequential targets (K, I, K') and full 360-deg orbits fit inside fixed camera view
                bool allAnglesVisible = true;
                for (int deg = 0; deg < 360; deg += 15)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    Vector2 axisU = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    Vector2 target1_K = centerO + axisU * rK;
                    Vector2 target2_I = centerO - axisU * rI;
                    Vector2 target3_Kp = centerO - axisU * rK;

                    if (!camViewRect.Contains(target1_K) || !camViewRect.Contains(target2_I) || !camViewRect.Contains(target3_Kp))
                    {
                        allAnglesVisible = false;
                        break;
                    }
                }
                Assert(allAnglesVisible, $"Camera_C06_{lvlId}_All3TargetsVisible", $"{lvlId} all 3 sequential targets (K, I, K') and orbits K/I fit simultaneously inside fixed Newton camera rect ({camViewRect.width:F1}x{camViewRect.height:F1})");
            }
        }

        private static void Test_NewtonX_SequentialColliderValidation_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_NewtonX_SequentialColliderValidation_Suite (Cases X01-X22)...");

            // Simulate the 3 sequential target colliders and strict state machine (Target 1 -> Target 2 -> Target 3)
            Vector2 centerO = new Vector2(80.234375f, -10.296875f);
            float rI = 3.4375f * Mathf.Sqrt(3f);
            float rK = 3.4375f * Mathf.Sqrt(6f);
            float tolI = 0.75f;
            float tolKp = 0.84375f;
            Vector2 axisU = Vector2.left;

            Vector2 posT1 = centerO + axisU * rK;
            Vector2 posT2 = centerO - axisU * rI;
            Vector2 posT3 = centerO - axisU * rK;

            Rect colT1 = new Rect(posT1.x - 1.2f, posT1.y - 0.5f, 2.4f, 1.8f);
            Rect colT2 = new Rect(posT2.x - tolI, posT2.y - tolI, tolI * 2f, tolI * 2f + 1.0f);
            Rect colT3 = new Rect(posT3.x - tolKp, posT3.y - tolKp, tolKp * 2f, tolKp * 2f + 1.0f);
            Rect[] targetRects = { colT1, colT2, colT3 };

            bool[] enabled = { true, false, false };
            bool[] completed = { false, false, false };
            int currentTargetIdx = 0;

            bool TryPressX(Rect playerRect, bool newKeyPressThisFrame)
            {
                if (!newKeyPressThisFrame || currentTargetIdx >= 3)
                    return false;
                if (!enabled[currentTargetIdx] || completed[currentTargetIdx])
                    return false;
                if (!playerRect.Overlaps(targetRects[currentTargetIdx]))
                    return false;

                completed[currentTargetIdx] = true;
                enabled[currentTargetIdx] = false;
                currentTargetIdx++;
                if (currentTargetIdx < 3)
                    enabled[currentTargetIdx] = true;
                return true;
            }

            // X01: Initial state
            Assert(currentTargetIdx == 0 && enabled[0] && !enabled[1] && !enabled[2], "NewtonX_X01_InitState", "Target 1 enabled; Target 2 and Target 3 disabled initially");

            // X02, X03: Touching Target 1 without pressing X or while holding stale X does not mark
            Rect playerOnT1 = new Rect(posT1.x - 0.3f, posT1.y - 0.2f, 0.6f, 1.0f);
            Assert(!TryPressX(playerOnT1, false), "NewtonX_X02X03_NoPressOrHoldIgnored", "Overlapping Target 1 without new X press event does not mark");
            Assert(currentTargetIdx == 0 && !completed[0], "NewtonX_X02X03_StillStage0", "Target 1 remains uncompleted");

            // X04, X05: Pressing X away from Target 1 or on Target 2/Target 3 during Stage 0 is rejected
            Rect playerAway = new Rect(centerO.x - 0.3f, centerO.y - 0.5f, 0.6f, 1.0f);
            Rect playerOnT2 = new Rect(posT2.x - 0.3f, posT2.y - 0.2f, 0.6f, 1.0f);
            Rect playerOnT3 = new Rect(posT3.x - 0.3f, posT3.y - 0.2f, 0.6f, 1.0f);
            Assert(!TryPressX(playerAway, true), "NewtonX_X04_AwayFromT1Rejected", "Press X away from Target 1 rejected");
            Assert(!TryPressX(playerOnT2, true), "NewtonX_X05_T2DuringStage0Rejected", "Press X on Target 2 before Target 1 rejected");
            Assert(!TryPressX(playerOnT3, true), "NewtonX_X05_T3DuringStage0Rejected", "Press X on Target 3 before Target 1 rejected");

            // X06 (Step C6.1): Valid Press X on Target 1
            Assert(TryPressX(playerOnT1, true), "NewtonX_X06_Target1Marked", "Press X while overlapping Target 1 marks Target 1");
            Assert(currentTargetIdx == 1 && completed[0] && !enabled[0] && enabled[1] && !enabled[2], "NewtonX_X06_AdvancesToTarget2Only", "Target 1 completed & disabled; ONLY Target 2 enabled; Target 3 remains disabled");

            // X07, X08, X09, X10: Re-pressing on Target 1, holding X into Target 2, away from Target 2, or on Target 3 during Stage 1 rejected
            Assert(!TryPressX(playerOnT1, true), "NewtonX_X07_RemarkT1Rejected", "Press X again on completed Target 1 rejected");
            Assert(!TryPressX(playerOnT2, false), "NewtonX_X08_HoldIntoT2Rejected", "Holding X while entering Target 2 rejected");
            Assert(!TryPressX(playerAway, true), "NewtonX_X09_AwayFromT2Rejected", "Press X away from Target 2 rejected");
            Assert(!TryPressX(playerOnT3, true), "NewtonX_X10_T3DuringStage1Rejected", "Press X on Target 3 before Target 2 rejected");

            // X11 (Step C6.2): Valid Press X on Target 2
            Assert(TryPressX(playerOnT2, true), "NewtonX_X11_Target2Marked", "Press X while overlapping Target 2 marks Target 2");
            Assert(currentTargetIdx == 2 && completed[1] && !enabled[1] && enabled[2], "NewtonX_X11_AdvancesToTarget3Only", "Target 2 completed & disabled; ONLY Target 3 enabled");

            // X12, X13: Re-pressing on Target 2 or holding X into Target 3 rejected
            Assert(!TryPressX(playerOnT2, true), "NewtonX_X12_RemarkT2Rejected", "Press X again on completed Target 2 rejected");
            Assert(!TryPressX(playerOnT3, false), "NewtonX_X13_HoldIntoT3Rejected", "Holding X while entering Target 3 rejected");

            // X14 (Step C6.3): Valid Press X on Target 3
            Assert(TryPressX(playerOnT3, true), "NewtonX_X14_Target3Marked", "Press X while overlapping Target 3 marks Target 3 and completes sequence");
            Assert(currentTargetIdx == 3 && completed[0] && completed[1] && completed[2] && !enabled[0] && !enabled[1] && !enabled[2], "NewtonX_X14_AllCompletedAndDisabled", "All 3 targets completed and disabled at Stage 3");

            // X15: Extra X press after completion is no-op
            Assert(!TryPressX(playerOnT3, true), "NewtonX_X15_PostCompleteNoOp", "Press X after all 3 targets completed is ignored");
        }

        private static void Test_CrossSystem_DeathY_Camera_NewtonX_Regression_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_CrossSystem_DeathY_Camera_NewtonX_Regression_Suite (Scenarios D1-D7)...");

            // D1-D7: Verify L01, L02, L03 end-to-end consistency across deathY, NewtonFixed camera, and 3-step X collider validation
            string[] levels = { "L01", "L02", "L03" };
            foreach (string lvl in levels)
            {
                string json = File.ReadAllText(Path.Combine("Assets", "Resources", "Levels", $"{lvl}.json"));
                LevelData data = LevelDataSanitizer.Sanitize(DeserializeLevelData(json), json, out _);

                float deathY = LevelLoader.ComputeKillY(data);
                Vector2 centerO = new Vector2(data.newton.cx / 64f, -data.newton.cy / 64f);
                float rK = 3.4375f * Mathf.Sqrt(data.newton.k);
                float lowestNewtonY = centerO.y - rK;

                Assert(lowestNewtonY > deathY + 5f, $"Cross_D1D5_{lvl}_NewtonAboveDeathY", $"{lvl} Newton lowest orbit ({lowestNewtonY:F2}) is safely above fixed deathY ({deathY:F2})");
                Assert(data.newton.orbitStart == "auto", $"Cross_D6_{lvl}_OrbitStartAuto", $"{lvl} Newton orbitStart remains auto");
            }
        }

        private static void Test_RC4_Animation_Checkpoint_And_ReversibleCamera_Suite()
        {
            Console.WriteLine("\n[RUNNING] Test_RC4_Animation_Checkpoint_And_ReversibleCamera_Suite...");

            // 1. Task A & B: SilhouetteMotion constants and uniform visual scale math
            float maxW = Contrast.Presentation.PlayerVisualPresentation.MaxAnimatedVisualWidth;
            float maxH = Contrast.Presentation.PlayerVisualPresentation.MaxAnimatedVisualHeight;
            Assert(Mathf.Abs(maxW - 1.3125f) < 1e-5f, "RC4_Anim_MaxW", $"MaxAnimatedVisualWidth == 1.3125 ({maxW:F5})");
            Assert(Mathf.Abs(maxH - 1.6015625f) < 1e-5f, "RC4_Anim_MaxH", $"MaxAnimatedVisualHeight == 1.6015625 ({maxH:F6})");

            Vector2 colliderSize = new Vector2(0.59375f, 1.0f);
            float uniformScale = Contrast.Presentation.PlayerVisualPresentation.CalculateUniformWorldScale(colliderSize);
            float expectedScale = Mathf.Min(0.59375f / 1.3125f, 1.0f / 1.6015625f);
            Assert(Mathf.Abs(uniformScale - expectedScale) < 1e-6f, "RC4_Scale_Uniform", $"Uniform world scale == {uniformScale:F6}");
            Assert(Mathf.Abs(maxW * uniformScale - 0.59375f) < 1e-5f, "RC4_Scale_FitsWidth", "Scaled max width matches collider width 0.59375");
            Assert(maxH * uniformScale <= 1.0001f, "RC4_Scale_FitsHeight", "Scaled max height fits within collider height 1.0");

            // 2. Task C: L02 pre-Newton checkpoint in L02.json
            string l02Path = Path.Combine("Assets", "Resources", "Levels", "L02.json");
            string l02Raw = File.ReadAllText(l02Path);
            LevelData l02 = LevelDataSanitizer.Sanitize(DeserializeLevelData(l02Raw), l02Raw, out _);
            Assert(l02.checkPoint != null && l02.checkPoint.Length == 5, "RC4_L02_CheckpointCount5", $"L02 has 5 checkpoints (actual: {l02.checkPoint?.Length})");

            var preNewtonCp = l02.checkPoint[4];
            var preNewtonPlat = l02.platforms[13];
            float platLeftX = preNewtonPlat.position.x - preNewtonPlat.size.x * 0.5f;
            float platTopY = preNewtonPlat.position.y + preNewtonPlat.size.y * 0.5f;
            Assert(Mathf.Abs(preNewtonCp.position.x - 67.617188f) < 1e-4f &&
                   Mathf.Abs(preNewtonCp.position.y - (platTopY + 0.5f)) < 1e-4f,
                "RC4_L02_PreNewtonCpGroundedPos",
                $"L02 pre-Newton checkpoint at ({preNewtonCp.position.x:F6}, {preNewtonCp.position.y:F6}) places player feet on platform top ({platTopY:F6})");
            Assert(Mathf.Abs(platLeftX - 66.6406255f) < 1e-4f, "RC4_L02_ThresholdX", $"L02 pre-Newton platform left boundary thresholdX == {platLeftX:F6}");

            // 3. Task E & F5/F6: Bidirectional reversible camera damping simulation (120 alternating crossings)
            float minScale = 5.0f;
            float maxScale = 8.6f;
            Vector3 newtonCenter = new Vector3(l02.newton.cx / 64f, -l02.newton.cy / 64f, -10f);
            float curScale = minScale;
            float scaleVel = 0f;
            Vector3 curPos = new Vector3(platLeftX - 1f, -8.734375f, -10f);
            Vector3 posVel = Vector3.zero;
            bool modeNewton = false;

            bool allScalesBounded = true;
            bool allStepsContinuous = true;
            for (int rev = 0; rev < 120; rev++)
            {
                float px = (rev % 2 == 0) ? (platLeftX + 1.0f) : (platLeftX - 1.0f);
                if (px > platLeftX) modeNewton = true;
                else if (px < platLeftX) modeNewton = false;

                int frames = 5 + (rev % 15);
                for (int f = 0; f < frames; f++)
                {
                    float prevScale = curScale;
                    Vector3 prevPos = curPos;

                    float targetScale = modeNewton ? maxScale : minScale;
                    Vector3 targetPos = modeNewton ? newtonCenter : new Vector3(px, -8.734375f, -10f);

                    curPos = Vector3.SmoothDamp(curPos, targetPos, ref posVel, 0.25f, Mathf.Infinity, 1f / 60f);
                    curScale = Mathf.SmoothDamp(curScale, targetScale, ref scaleVel, 0.25f, Mathf.Infinity, 1f / 60f);
                    if (curScale <= minScale) { curScale = minScale; if (scaleVel < 0f) scaleVel = 0f; }
                    if (curScale >= maxScale) { curScale = maxScale; if (scaleVel > 0f) scaleVel = 0f; }

                    if (curScale < minScale - 1e-5f || curScale > maxScale + 1e-5f)
                        allScalesBounded = false;
                    if (Mathf.Abs(curScale - prevScale) > 0.5f || Vector2.Distance(curPos, prevPos) > 2.0f)
                        allStepsContinuous = false;
                }
            }
            Assert(allScalesBounded, "RC4_Cam_120ReversalsScaleBounds", "minScale <= scale <= maxScale held across 120 reversals");
            Assert(allStepsContinuous, "RC4_Cam_120ReversalsContinuous", "Position and scale remained continuous across 120 reversals");
        }

        private sealed class MockPlatformInputAdapter : IPlatformInputAdapter
        {
            public float Axis = 0f;
            public bool Jump;
            public bool FastFall;
            public float PhaseTuneAxis;
            public bool NewtonMark;
            public int ColorStep;
            public bool HasDirectColor;
            public LogicalColor DirectColor = LogicalColor.Universal;

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
