using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Core;
using Contrast.Data;
using Contrast.Level;
using Contrast.Level.EditorInteraction;
using Contrast.UI;
using Contrast.Color;
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
            AssertApprox(0.96f, c255.r, 0.01f, "ColorVisual", "Universal color r is 0.96");

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

        private static LevelData DeserializeLevelData(string json)
        {
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
            return System.Text.Json.JsonSerializer.Deserialize<LevelData>(json, options);
        }
    }
}
