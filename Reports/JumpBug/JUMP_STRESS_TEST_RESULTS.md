# CONTRAST — JUMP & RESPAWN STRESS TEST RESULTS (`JUMP_STRESS_TEST_RESULTS.md`)

## 1. Automated Verification Summary

Both the standalone `.NET 10` test harness (`Tests/ContrastTests/ContrastTests.csproj`) and the real Unity 6 (`6000.5.8f1`) Editor & Runtime batchmode test runner (`Assets/Editor/EditorLevelTests.cs` via `-executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameWindowsJumpFixRC2`) were executed and verified from retained logs (`Logs/unity_build_jumpfix_rc2.log` and `Reports/JumpBug/JUMP_FAILURE_TRACE.jsonl`).

| Test Suite | Command / Runner | Total Assertions | Passed | Failed | Blocked | Status |
|---|---|---:|---:|---:|---:|---|
| **Standalone `.NET` Suite** | `dotnet run --project "Tests/ContrastTests/ContrastTests.csproj"` | **449** | **449** | **0** | **0** | **PASS** |
| **Unity 6 Editor & Runtime Suite** | `Unity.exe -batchmode -nographics -quit -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameWindowsJumpFixRC2` | **532** | **532** | **0** | **0** | **PASS** |

---

## 2. Detailed Jump & Respawn Stress Test Matrix Results

| Test ID | Description | Iterations / Frames | Frame Rates Tested | Success Rate | Status |
|---|---|---:|---|---:|---|
| `JumpBug_SpawnImmediateGrounded` | Player spawned/respawned on platform establishes `IsGrounded = true` before Frame 1 `ReadInput()` | Immediate | All | **100%** | **PASS** |
| `JumpBug_C2_C5_HighFPS1000_TakeoffNotCanceled` | High-FPS (`1000 FPS`, `dt = 0.001s`) jump takeoff does not self-cancel via `HasGroundSupport()` (`y = 0.71279`, `vy = 12.787 u/s`) | 1000 Hz frame | `1000 FPS` (`dt = 0.001s`) | **100%** | **PASS** |
| `JumpBug_100RespawnCycles_R1ToR8_ZeroMissed` | 100 consecutive death/respawn cycles across all 8 timing cases (`R1`–`R8`: normal, held-key release/repress, airborne spawn buffer, Frame-1 immediate jump, mid-jump death, falling death, moving-platform death, post-phase-change death) | **100 cycles** | `60`, `144`, `1000 FPS` | **100 / 100 (100%)** | **PASS** |
| `JumpBug_1000GroundedJumps_ZeroMissed` | 1,000 valid grounded jump attempts (`500` `W`, `500` `Space`) | **1,000 jumps** | `60`, `144`, `240`, `500`, `1000 FPS` | **1,000 / 1,000 (100%)** | **PASS** |
| `JumpBug_100LandingBufferCoyoteCycles_ZeroMissed` | 100 consecutive landing-frame jumps, pre-landing buffered jumps (`0.12s` window), and post-ledge coyote-time jumps (`0.10s` window) | **100 sequences** | `60 FPS` | **100 / 100 (100%)** | **PASS** |
| `JumpBug_50MovingPlatformTakeoffs_ZeroMissed` | 50 takeoffs from active moving platforms (rising vertical `rot=90°`, descending `rot=-90°`, horizontal `rot=0°`, and circular Newton `K`/`I` orbits) | **50 takeoffs** | `60 FPS` | **50 / 50 (100%)** | **PASS** |
| `JumpBug_50TModeExitAndNewtonMarkJumps_ZeroMissed` | 50 `T`-mode exit (`SetTestMode(true) -> SetTestMode(false)`) + immediate jump sequences with alternating Newton `X` marking | **50 sequences** | `60 FPS` | **50 / 50 (100%)** | **PASS** |
| `JumpBug_CeilingBlocked_ZeroesUpwardVelocity` | Upward jump bonking into low overhead ceiling clamps upward vertical velocity `<= 0` (`vy = -0.430 u/s`) | 2 frames | `60 FPS` | **100%** | **PASS** |
| `JumpBug_TraceFileWritten` | Structured JSONL lifecycle diagnostics written to `Reports/JumpBug/JUMP_FAILURE_TRACE.jsonl` | **37 records** | `60..1000 FPS` | **100%** | **PASS** |

---

## 3. Release Candidate Isolation & Portable Build Verification

- **Git Branch:** `fix/intermittent-jump-respawn`
- **Previous Release Candidate Preserved Untouched:**
  - `Release/Windows/CONTRAST_Windows/Game.exe` (unchanged)
  - `Release/Windows/CONTRAST_Windows_Portable.zip` (`44,750,255` bytes, unchanged)
- **New Isolated Release Candidate Created & Verified:**
  - `Release/Windows/CONTRAST_JumpFix_RC2/Game.exe`
  - `Release/Windows/CONTRAST_JumpFix_RC2.zip` (`44,788,686` bytes)

