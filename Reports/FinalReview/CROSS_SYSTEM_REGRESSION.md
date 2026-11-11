# CROSS-SYSTEM REGRESSION REPORT (`Reports/FinalReview/CROSS_SYSTEM_REGRESSION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Test Harnesses Executed:**
- Unity 6 (`6000.5.8f1`) Batchmode Suite (`Assets/Editor/EditorLevelTests.cs`) — **620 PASSED, 0 FAILED, 0 BLOCKED**
- Standalone `.NET 10.0` Suite (`Tests/ContrastTests/ContrastTests.csproj`) — **496 PASSED, 0 FAILED**

---

## 1. Cross-System Scenarios (`D1`–`D7`)

| Scenario | Description | Verification Evidence | Status |
|---|---|---|---|
| **D1** | **Newton Camera + `deathY`:** Enter Newton fixed-camera mode (`CameraMode.NewtonFixed`), fall below `deathY`, verify death triggers at fixed world `deathY`, respawn restores camera and player state. | Verified across L01 (`deathY = -24.75`), L02 (`deathY = -23.25`), and L03 (`deathY = -49.125`) in `Test_CrossSystem_DeathY_Camera_NewtonX_Regression_Suite`. | **PASS** |
| **D2** | **Newton Camera + Sequential X + Death/Respawn:** Enter Newton fixed-camera mode, mark Target 1 (`K`), fall below `deathY`, respawn at checkpoint, re-enter Newton mode, verify camera and target states reset cleanly to `CurrentTargetIndex = 0` (`Target 1` enabled, `Target 2/3` disabled). | Verified in `Cross_D2_L01`, `Cross_D2_L02`, and `Cross_D2_L03`: `CurrentTargetIndex` resets to `0`, `CameraMode` restores `NormalPlatformingFollow` outside zone and re-locks to `NewtonFixed` upon re-entry, and full `1 -> 2 -> 3` completion succeeds on retry. | **PASS** |
| **D3** | **Moving Platform + Target Collider + PRESS X:** Stand on moving orbital platform (`PlatformK` / `PlatformI`), ride into target collider overlap (`Target 1`, `Target 2`, `Target 3`), press `X` once when overlapping, verify mark succeeds without platform jitter or jump regression. | Verified in `Test_NewtonX_SequentialColliderValidation_Suite` and `Test_MovingPlatformJitter_And_CarrierResolution_Suite` (`0.0000` overlap/jitter error). | **PASS** |
| **D4** | **Jump + Respawn + X Input Isolation:** Jump (`W` / `Space`) after respawn, press `X` while jumping away from targets, land on target and press `X`, jump again after marking. Verify `X` never consumes jump input and Jump never triggers `X`. | Verified in `Test_PlatformInputAdapters_And_Equivalence` (`Space_Never_Triggers_NewtonMark`), `Test_L02_Trajectory_And_NewtonDocking_Suite` (`Test9_Space_Jumps`, `Test9_X_MarksNewton`), and `JumpBug_50TModeExitAndNewtonMarkJumps_ZeroMissed` (`50/50`). | **PASS** |
| **D5** | **Built-in Levels `L01`, `L02`, `L03` End-to-End:** Normal platforming, color switching (`Black` / `Gray` / `White`), collectibles (`Quang Điểm` `2 + 2 + 3 = 7`), checkpoints, `deathY` fall & respawn, Newton fixed camera (`NewtonFixed`), and sequential `Target 1 -> Target 2 -> Target 3` `PRESS X` completion (`550.0 nm`). | Verified in `Test_CampaignLevels_JSON_Validation`, `Test_FinalReview_Tasks1To8_Suite`, and `Cross_D5_L01/L02/L03_FullSequenceCompletes550nm`. | **PASS** |
| **D6** | **Level Editor & Custom Level Regression:** Editor launch, object selection, moving platform trajectories, save/load JSON with `"deathY"`, test play, and return to editor. | Verified in `Test_StandaloneLevelEditor_Lifecycle`, `Test_LevelPersistence_RoundTrip`, `Test_GhostPlatform_TestPlay_Lifecycle_And_Reset`, and `DeathY_D08_RoundTripJson`. | **PASS** |
| **D7** | **1,300+ Jump & Carrier Stress Regression:** 1,000 grounded jumps (`60..1000 FPS`), 100 death/respawn cycles (`R1`–`R8`), 100 landing/buffered jumps, 50 moving-platform takeoffs, 50 T-mode exit jumps. | `1,300 / 1,300` (`100%`) passed with zero missed jumps in both `.NET` and Unity 6. | **PASS** |

