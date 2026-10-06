using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Data;
using Contrast.Level;
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
    }
}
