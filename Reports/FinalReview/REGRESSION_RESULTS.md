# Comprehensive Regression & Runtime Verification Results (`REGRESSION_RESULTS.md`)

## 1. Executive Summary

Both automated test suites executed cleanly against the compiled Unity 6 project with **zero failures**:

1. **Unity 6 Batchmode Test Suite (`Assets/Editor/EditorLevelTests.cs` via `RunAllEditorTests`):**
   - **Total Test Suites / Cases:** `686 PASSED, 0 FAILED, 0 BLOCKED` (`1,372` individual `[TEST][PASS]` assertions, `0` `[TEST][FAIL]`).
   - **Log Evidence:** `Logs/unity_rc4_build.log` (line `312440`: `[TEST_SUITE] COMPLETED: 686 PASSED, 0 FAILED, 0 BLOCKED`).
2. **Standalone `.NET 10.0` Automated Test Harness (`Tests/ContrastTests/ContrastTests.csproj`):**
   - **Total Assertions:** `506 PASSED, 0 FAILED`.

---

## 2. Full Suite Breakdown (`F1`–`F8` & Prior Regression Suites)

| Suite Name | Scope | Status |
| --- | --- | --- |
| `Test_AnimationCheckpointReversibleCamera_RC4_Suite` | Tasks A–H: `SilhouetteMotion` 213-frame animation states (`Idle`, `Run`, `Jump`, `Fall`, `Landing`, `Death`, `Respawn`), `applyRootMotion == false`, uniform visual scaling (`0.452381`) & feet alignment, `L01`/`L02`/`L03` invisible checkpoints, new `L02` pre-Newton checkpoint at `(67.617188, -8.734375)`, bidirectional reversible Newton camera (`CAM01`–`CAM05`), 120-reversal stress suite, `CAMERA_REVERSAL_TRACE.csv`, and `L02` end-to-end PRESS-X completion | **PASS** (`0 FAILED`) |
| `Test_DeathY_FixedWorldSpace_Suite` | Fixed world-space `deathY` (`L01 = -24.75`, `L02 = -23.25`, `L03 = -49.125`), camera zoom invariance, high-height survival, respawn recovery | **PASS** (`0 FAILED`) |
| `Test_NewtonFixedCamera_HtmlParity_Suite` | HTML-derived Newton camera framing across `L01`, `L02`, and `L03`, simultaneous visibility of all 3 targets (`K`, `I`, `K'`) | **PASS** (`0 FAILED`) |
| `Test_NewtonX_SequentialColliderValidation_Suite` | Strictly sequential 3-target PRESS-X collider validation (`Target 1 -> Target 2 -> Target 3`), rising-edge `GetKeyDown(KeyCode.X)`, wrong-order rejection | **PASS** (`0 FAILED`) |
| `Test_CrossSystem_DeathY_Camera_NewtonX_Regression_Suite` | End-to-end `L01`, `L02`, `L03`: Enter Newton -> Mark Target 1 -> Fall below `deathY` -> Respawn & Reset -> Re-enter -> Mark 1, 2, 3 -> 550nm reveal | **PASS** (`0 FAILED`) |
| `Test_IntermittentJump_And_RespawnLifecycle_Audit_Suite` | 1,000 grounded jumps across 60–1000 FPS, 100 death/respawn jump cycles (`R1`–`R8`), 100 landing/coyote/buffered jump cycles | **PASS** (`0 FAILED`) |
| `Test_MovingPlatformJitter_And_CarrierResolution_Suite` | Moving platform rider carry, edge stability, ascending/descending/circular carrier displacement without jitter | **PASS** (`0 FAILED`) |
| `Test_FinalReview_Tasks1To8_Suite` | Black/Gray/White optical phase rendering, HTML jump physics parity, phase slider dividers, universal gray-blue columns, Quang Điểm collectibles, direct level selection | **PASS** (`0 FAILED`) |
| `Test_L02_Trajectory_And_NewtonDocking_Suite` | Automatic `orbitStart = "auto"` orbital platform trajectories (`K` and `I`) across `L01`, `L02`, `L03` | **PASS** (`0 FAILED`) |
| `EditorLevelTests` (Core Level & Editor Suites) | Level schema validation, coordinates, trajectories, Level Editor import/export, sanitizer invariants | **PASS** (`0 FAILED`) |
| `ContrastTests.csproj` (`.NET 10.0` Harness) | `506` assertions covering trajectories, JSON levels, `deathY`, Newton camera, sequential PRESS-X, and RC4 animation/checkpoint/camera invariants | **PASS** (`506 PASSED, 0 FAILED`) |

