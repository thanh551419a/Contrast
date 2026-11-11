# SEQUENTIAL PRESS-X COLLIDER VALIDATION TEST RESULTS (`Reports/NewtonX/X_SEQUENTIAL_TEST_RESULTS.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Test Harnesses Executed:**
1. Unity 6 (`6000.5.8f1`) Batchmode Suite (`EditorLevelTests.Test_NewtonX_SequentialColliderValidation_Suite`) — **620 / 620 PASSED**
2. Standalone `.NET 10.0` Suite (`ContrastTests.Program.Test_NewtonX_SequentialColliderValidation_Suite`) — **496 / 496 PASSED**

---

## Matrix Results (`X01`–`X22`)

| Case | Scenario | Expected | Actual Result | Status |
|---|---|---|---|---|
| **X01** | Initial state | Target 1 enabled (`BoxCollider2D.enabled = true`); Target 2 and Target 3 disabled | `CurrentTargetIndex == 0`, `Enabled = [true, false, false]` | **PASS** |
| **X02** | Touch Target 1 without pressing X | Target 1 is NOT marked | `IsTargetCompleted(0) == false`, `CurrentTargetIndex == 0` | **PASS** |
| **X03** | Hold X before entering Target 1, then walk into Target 1 | Target 1 is NOT marked (requires discrete `wasPressedThisFrame`) | Rejected; `CurrentTargetIndex == 0` | **PASS** |
| **X04** | Press X away from Target 1 | Target 1 is NOT marked (`NO_TARGET_1_COLLIDER_OVERLAP` / `NOT_ON_ORBIT_PLATFORM`) | Rejected; `CurrentTargetIndex == 0` | **PASS** |
| **X05** | Overlap Target 2 or Target 3 before Target 1 and press X | Neither Target 2 nor Target 3 is marked | Rejected; `IsTargetCompleted(1) == false`, `IsTargetCompleted(2) == false` | **PASS** |
| **X06** | **(Step C6.1)** Overlap Target 1 and press X once | Target 1 is marked & disabled; ONLY Target 2 becomes enabled; Target 3 stays disabled | `CurrentTargetIndex == 1`, `Completed = [true, false, false]`, `Enabled = [false, true, false]` | **PASS** |
| **X07** | Press X again on Target 1 after it is marked | No effect; Target 1 stays completed, state remains on Target 2 | `CurrentTargetIndex == 1`, `IsTargetCompleted(1) == false` | **PASS** |
| **X08** | Keep holding X from Target 1 and move to Target 2 | Target 2 is NOT marked until X is released and pressed again | `OnPlayerNewtonMarkInput` + `wasPressedThisFrame` prevents hold-marking | **PASS** |
| **X09** | Press X away from Target 2 (misaligned on Platform I) | Target 2 is NOT marked (`DoesPlayerOverlapTargetCollider(1) == false`) | Rejected (`NO_TARGET_2_COLLIDER_OVERLAP`); `CurrentTargetIndex == 1` | **PASS** |
| **X10** | Overlap Target 3 before Target 2 and press X | Target 3 is NOT marked | Rejected; `CurrentTargetIndex == 1`, `IsTargetCompleted(2) == false` | **PASS** |
| **X11** | **(Step C6.2)** Overlap Target 2 and press X once | Target 2 is marked & disabled; ONLY Target 3 becomes enabled | `CurrentTargetIndex == 2`, `Completed = [true, true, false]`, `Enabled = [false, false, true]` | **PASS** |
| **X12** | Press X again on Target 2 after it is marked | No effect; Target 2 stays completed, state remains on Target 3 | `CurrentTargetIndex == 2`, `IsTargetCompleted(2) == false` | **PASS** |
| **X13** | Keep holding X from Target 2 and move to Target 3 | Target 3 is NOT marked until X is released and pressed again | Rejected; `CurrentTargetIndex == 2` | **PASS** |
| **X14** | **(Step C6.3)** Overlap Target 3 and press X once | Target 3 is marked & disabled; Newton sequence completes (`StartRevealSequence`) | `CurrentTargetIndex == 3`, `Completed = [true, true, true]`, `Enabled = [false, false, false]`, `IsRevealActive == true` | **PASS** |
| **X15** | Press X after all 3 targets are completed | No duplicate completion, error, or state corruption | Safe no-op (`ALREADY_COMPLETED`) | **PASS** |
| **X16** | Die/respawn before any target marked | Target 1 enabled; Target 2 and Target 3 disabled | `CurrentTargetIndex == 0`, `Enabled = [true, false, false]` | **PASS** |
| **X17** | Die/respawn after Target 1 marked | Resets incomplete measurement cleanly to Stage 0 (`Target 1` enabled) | `ResetState()` restores `CurrentTargetIndex == 0`, `Enabled = [true, false, false]` | **PASS** |
| **X18** | Die/respawn after Target 2 marked | Resets incomplete measurement cleanly to Stage 0 (`Target 1` enabled) | `ResetState()` restores `CurrentTargetIndex == 0`, `Enabled = [true, false, false]` | **PASS** |
| **X19** | Level restart | Target 1 enabled; Target 2 and Target 3 disabled; all completed flags reset | Verified in `ResetState()` and `BuildRuntimeLevel()` | **PASS** |
| **X20** | L01 full sequence (`i=3, k=6`) | `Target 1 -> Target 2 -> Target 3` works via collider + PRESS X (`lambda = 550.0 nm`) | Verified in `Cross_D5_L01_FullSequenceCompletes550nm` | **PASS** |
| **X21** | L02 full sequence (`i=2, k=4`) | `Target 1 -> Target 2 -> Target 3` works via collider + PRESS X (`lambda = 550.0 nm`) | Verified in `Cross_D5_L02_FullSequenceCompletes550nm` | **PASS** |
| **X22** | L03 full sequence (`i=2, k=7`) | `Target 1 -> Target 2 -> Target 3` works via collider + PRESS X (`lambda = 550.0 nm`) | Verified in `Cross_D5_L03_FullSequenceCompletes550nm` | **PASS** |

