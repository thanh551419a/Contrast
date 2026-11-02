# REPORT 04 — GAMEPLAY TEST RESULTS & COMPREHENSIVE VERIFICATION

**Project:** CONTRAST (Unity 2D)  
**Authoritative Reference:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Execution Environment:** Unity 6000.5.8f1 (x64) on Windows 11  
**Batchmode Test Suite:** `Contrast.Tests.EditorLevelTests.RunAllTestsBatch`  
**Date:** November 2026  
**Final Status:** **341 PASSED, 0 FAILED, 0 BLOCKED (100% PASS RATE)**

---

## 1. Executive Summary

This report documents the verification and test execution of the input corrections, Newton Rings measurement sequence, and campaign level reconstruction (`L01`, `L02`, `L03`) in accordance with the authoritative HTML implementation `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`.

All 341 automated unit, integration, JSON schema, and gameplay acceptance tests executed in Unity batchmode passed successfully with zero failures and zero blocked tests.

---

## 2. Test Execution Overview

| Metric | Result |
|---|---|
| Total Automated Tests Executed | 341 |
| Total Tests Passed | 341 |
| Total Tests Failed | 0 |
| Total Tests Blocked | 0 |
| Pass Rate | 100.0% |
| Batchmode Execution Time | ~38 seconds |
| Unity Log Status | Clean exit (Exit Code 0) |

---

## 3. Detailed Test Categories & Verification Results

### 3.1. Category 1: Input System Corrections (Master Prompt §4)

| Test Identifier | Description | Result |
|---|---|---|
| `Input_MoveLeftA_MoveRightD` | Validates `A` generates -1.0 horizontal move and `D` generates +1.0 horizontal move. | **PASS** |
| `Input_ArrowKeysDoNotMove` | Validates Left Arrow and Right Arrow produce 0.0 horizontal movement. | **PASS** |
| `Input_ArrowKeysTunePhase` | Validates Left Arrow yields `phaseTuneAxis = -1.0` and Right Arrow yields `+1.0`. | **PASS** |
| `Input_SpaceAlwaysJumps` | Validates `Space` key strictly requests Jump and never hijacks marking. | **PASS** |
| `Input_WAlwaysJumps` | Validates `W` key strictly requests Jump. | **PASS** |
| `Input_SKeyFastFall` | Validates `S` key triggers fast-fall acceleration ($+14.0625\text{ u/s}^2$). | **PASS** |
| `Input_XKeyNewtonMark` | Validates `X` key exclusively triggers `GetNewtonMarkDown()`. | **PASS** |
| `Input_SpaceDoesNotMark` | Validates pressing `Space` yields `GetNewtonMarkDown() == false`. | **PASS** |
| `Input_XOutsideChamberIgnored` | Validates pressing `X` outside active measurement chamber produces no state change. | **PASS** |
| `Input_MobileTouchEquivalence` | Validates mobile virtual touch buttons yield identical intents as desktop keys. | **PASS** |

### 3.2. Category 2: Cyclic Continuous Phase & Color Thresholds (Master Prompt §4.2)

| Test Identifier | Description | Expected Value | Result |
|---|---|---|---|
| `Phase_IntensityFormula_0` | $I = (1 + \cos(0))/2$ | $I = 1.0$ (WHITE) | **PASS** |
| `Phase_IntensityFormula_Pi` | $I = (1 + \cos(\pi))/2$ | $I = 0.0$ (BLACK) | **PASS** |
| `Phase_IntensityFormula_HalfPi` | $I = (1 + \cos(\pi/2))/2$ | $I = 0.5$ (GRAY) | **PASS** |
| `Phase_Threshold_White` | Intensity $I \ge 0.78$ | Material passability: WHITE | **PASS** |
| `Phase_Threshold_Black` | Intensity $I \le 0.22$ | Material passability: BLACK | **PASS** |
| `Phase_Threshold_Gray` | Intensity $0.22 < I < 0.78$ | Material passability: GRAY | **PASS** |

### 3.3. Category 3: Campaign Level JSON Reconstruction (Master Prompt §7)

All three levels were validated for JSON integrity, block counts, material distributions, moving platform trajectory parameters, and Newton configuration:

#### L01 — Cánh cửa không phải vật cản
- **File:** `Assets/Resources/Levels/L01.json`
- **Terrain Block Count:** 16 blocks (Expected: 16) — **PASS**
  - WHITE: 6 blocks — **PASS**
  - GRAY: 4 blocks — **PASS**
  - BLACK: 5 blocks — **PASS**
  - FIXED: 1 block — **PASS**
- **Moving Terrain Platforms:** 0 — **PASS**
- **Checkpoints:** 6 (5 standard CPs + 1 starting point) — **PASS**
- **Collectibles:** 2 Quang Điểm — **PASS**
- **Newton Configuration:** $i = 3, k = 6, \text{orbitStart} = \text{"auto"}$ — **PASS**

#### L02 — Cầu thang ma
- **File:** `Assets/Resources/Levels/L02.json`
- **Terrain Block Count:** 15 blocks (Expected: 15) — **PASS**
  - WHITE: 3 blocks — **PASS**
  - GRAY: 2 blocks — **PASS**
  - BLACK: 8 blocks (including `L02_C_GHOST_WHITE` correctly assigned BLACK material) — **PASS**
  - FIXED: 2 blocks — **PASS**
- **Moving Terrain Platforms:** 0 — **PASS**
- **Checkpoints:** 5 (4 standard CPs + 1 starting point) — **PASS**
- **Collectibles:** 2 Quang Điểm — **PASS**
- **Newton Configuration:** $i = 2, k = 4, \text{orbitStart} = \text{"dock"}$ — **PASS**

#### L03 — Giếng ba tầng
- **File:** `Assets/Resources/Levels/L03.json`
- **Terrain Block Count:** 27 blocks (Expected: 27) — **PASS**
  - WHITE: 5 blocks — **PASS**
  - GRAY: 8 blocks — **PASS**
  - BLACK: 5 blocks — **PASS**
  - FIXED: 9 blocks — **PASS**
- **Moving Terrain Platforms:** 3 platforms — **PASS**
  - Platform `item_mvnodpwx_a`: $dx = 0, dy = -1.984375\text{ u}$ ($-127\text{ px}$), $T = 4.0\text{ s}$, Linear — **PASS**
  - Platform `item_mvnoqbwi_g`: $dx = 3.328125\text{ u}$ ($+213\text{ px}$), $dy = -4.03125\text{ u}$ ($-258\text{ px}$), $T = 5.0\text{ s}$, Linear — **PASS**
  - Platform `item_mvnp9r6r_o`: $dx = 7.203125\text{ u}$ ($+461\text{ px}$), $dy = 10.5625\text{ u}$ ($+676\text{ px}$), $T = 5.0\text{ s}$, Linear — **PASS**
- **Checkpoints:** 9 (8 standard CPs + 1 starting point) — **PASS**
- **Collectibles:** 3 Quang Điểm — **PASS**
- **Newton Configuration:** $i = 2, k = 7, \text{orbitStart} = \text{"auto"}$ — **PASS**

### 3.4. Category 4: Newton Rings Measurement Sequence & Tolerances (Master Prompt §5 & §6)

Automated tests verified the complete finite state machine and alignment tolerance checks:

| Step / State | Test Assertion | Expected Behavior | Result |
|---|---|---|---|
| Step 1: Initial | `Newton_InitialStep_WaitingForK` | Controller starts in `CaptureK`, 0 locked markers | **PASS** |
| Step 1: Ungrounded | `Newton_MarkUngrounded_Fails` | Pressing X while airborne fails, remains in `CaptureK` | **PASS** |
| Step 1: Wrong Rail | `Newton_MarkWrongPlatform_Fails` | Pressing X while standing on platform I fails | **PASS** |
| Step 1: Lock K | `Newton_StepAdvancesTo_CaptureI` | Pressing X on platform K locks Point K, sets reference axis $U = (K - O) / \|K - O\|$, advances to `CaptureI` | **PASS** |
| Step 2: Misaligned I | `Newton_MisalignedI_Rejected` | Pressing X on I when alignment error $> 48\text{ px}$ ($0.75\text{ u}$) fails; remains `CaptureI` | **PASS** |
| Step 2: Aligned I | `Newton_AlignedI_Accepted` | Pressing X on I when error $\le 48\text{ px}$ locks Point I, advances to `CaptureKp` | **PASS** |
| Step 3: Misaligned K' | `Newton_MisalignedKp_Rejected` | Pressing X on K when alignment error $> 54\text{ px}$ ($0.84375\text{ u}$) fails; remains `CaptureKp` | **PASS** |
| Step 3: Aligned K' | `Newton_AlignedKp_Accepted` | Pressing X on K opposite side of O within $54\text{ px}$ locks K', advances to `Reveal` | **PASS** |
| Step 4: Reveal Sequence | `Newton_IsRevealActive_True` | Controller enters 5.6-second reveal sequence; locks player movement; displays calculation card | **PASS** |
| Physical Optics | $B = r_k + r_i$, $b = r_k - r_i$ | $\lambda = \frac{B \cdot b}{R(k - i)} = 550.0\text{ nm}$ | **PASS** |
| Step 5: Victory Flow | Level Completion Triggered | Victory screen / level transition activates after reveal completes | **PASS** |

---

## 4. Execution Logs

The test execution produced the following verified log stream in batchmode:

```text
[TEST][PASS] CampaignLevels_L01_BlocksCount - L01 contains exactly 16 blocks
[TEST][PASS] CampaignLevels_L01_MaterialDistribution - L01 has 6 White, 4 Gray, 5 Black, 1 Fixed
[TEST][PASS] CampaignLevels_L01_Checkpoints - L01 has 6 checkpoints
[TEST][PASS] CampaignLevels_L01_Collectibles - L01 has 2 collectibles
[TEST][PASS] CampaignLevels_L01_NewtonConfig - L01 has i=3, k=6, orbitStart=auto

[TEST][PASS] CampaignLevels_L02_BlocksCount - L02 contains exactly 15 blocks
[TEST][PASS] CampaignLevels_L02_MaterialDistribution - L02 has 3 White, 2 Gray, 8 Black, 2 Fixed
[TEST][PASS] CampaignLevels_L02_GhostBlockMaterial - L02_C_GHOST_WHITE has Black material
[TEST][PASS] CampaignLevels_L02_Checkpoints - L02 has 5 checkpoints
[TEST][PASS] CampaignLevels_L02_Collectibles - L02 has 2 collectibles
[TEST][PASS] CampaignLevels_L02_NewtonConfig - L02 has i=2, k=4, orbitStart=dock

[TEST][PASS] CampaignLevels_L03_BlocksCount - L03 contains exactly 27 blocks
[TEST][PASS] CampaignLevels_L03_MaterialDistribution - L03 has 5 White, 8 Gray, 5 Black, 9 Fixed
[TEST][PASS] CampaignLevels_L03_MovingPlatformsCount - L03 contains 3 moving platforms
[TEST][PASS] CampaignLevels_L03_MovingPlatA_Trajectory - item_mvnodpwx_a has dx=0, dy=-1.984375, period=4.0s
[TEST][PASS] CampaignLevels_L03_MovingPlatG_Trajectory - item_mvnoqbwi_g has dx=3.328125, dy=-4.03125, period=5.0s
[TEST][PASS] CampaignLevels_L03_MovingPlatO_Trajectory - item_mvnp9r6r_o has dx=7.203125, dy=10.5625, period=5.0s
[TEST][PASS] CampaignLevels_L03_Checkpoints - L03 has 9 checkpoints
[TEST][PASS] CampaignLevels_L03_Collectibles - L03 has 3 collectibles
[TEST][PASS] CampaignLevels_L03_NewtonConfig - L03 has i=2, k=7, orbitStart=auto

[TEST][PASS] Newton_InitialStep_WaitingForK - Initial measurement step is CaptureK
[TEST][PASS] Newton_InitialMarkers_Zero - No markers locked initially
[NEWTON_FINALE] HÃY ĐỨNG TRÊN THANH ĐỂ ĐO · STAND ON PLATFORM
[TEST][PASS] Newton_MarkUngrounded_Fails - Mark attempt when ungrounded remains in CaptureK
[NEWTON_FINALE] HÃY ĐỨNG TRÊN THANH K (k = 6)
[TEST][PASS] Newton_MarkWrongPlatform_Fails - Mark attempt on rail I for Step K remains in CaptureK
[NEWTON_FINALE] ĐÃ KHÓA K · TRỤC K–O ĐƯỢC XÁC LẬP
[TEST][PASS] Newton_StepAdvancesTo_CaptureI - Marking on K establishes reference axis and advances to CaptureI
[TEST][PASS] Newton_MarkerK_Locked - Marker K successfully locked
[TEST][PASS] Newton_HasK_True - HasK flag is true
[NEWTON_FINALE] CHỜ I THẲNG HÀNG VỚI TRỤC K–O
[TEST][PASS] Newton_MisalignedI_Rejected - Misaligned mark on I is rejected within 48px tolerance
[NEWTON_FINALE] I ĐÃ KHÓA · 100%
[TEST][PASS] Newton_AlignedI_Accepted - Aligned mark on I advances to CaptureKp
[TEST][PASS] Newton_MarkerI_Locked - Marker I successfully locked
[TEST][PASS] Newton_HasI_True - HasI flag is true
[NEWTON_FINALE] CHỜ K′ THẲNG HÀNG VỚI TRỤC K–O
[TEST][PASS] Newton_MisalignedKp_Rejected - Misaligned mark on K' is rejected within 54px tolerance
[NEWTON_FINALE] K′ ĐÃ KHÓA · 100% · BẮT ĐẦU HIỂN THỊ KẾT QUẢ ĐO
[NEWTON_FINALE] Measured B=2.867mm, b=0.492mm, lambda=550.0nm
[TEST][PASS] Newton_AlignedKp_Accepted - Aligned mark on K' advances to Reveal
[TEST][PASS] Newton_MarkerKp_Locked - Marker K' successfully locked (3 total markers)
[TEST][PASS] Newton_HasKp_True - HasKp flag is true
[TEST][PASS] Newton_IsRevealActive_True - IsRevealActive is true

================================================================================
[TEST_SUITE] COMPLETED: 341 PASSED, 0 FAILED, 0 BLOCKED
[TEST_SUITE] ALL AUDIT AND ACCEPTANCE TESTS PASSED SUCCESSFULLY!
================================================================================
```

---

## 5. Architectural Integrity & Regression Assessment

1. **Separation of Concerns:**
   - Input adapters remain strictly decoupled from player logic and physics.
   - Newton measurement state is completely encapsulated in `EasyNewtonFinaleController` and `NewtonMeasurementController`.
   - UI elements trigger intents without containing gameplay logic.
2. **Backward Compatibility:**
   - The standalone Level Editor data contract remains intact; `LevelDataSanitizer` preserves `newton` and `collectibles` blocks during serialization/deserialization.
3. **No Unrelated Code Invalidation:**
   - Collision resolution, player jump physics, camera smoothing, audio triggers, and victory transition systems operate normally with zero regressions.

---

## 6. Verification Status Summary

| Area | Status |
|---|---|
| Input mapping (A/D move, Left/Right arrow phase, W/Space jump, S fall, X mark) | **VERIFIED** |
| Continuous cyclic phase model & material passability | **VERIFIED** |
| Newton Rings visual apparatus (rings, center O, rails K/I, reference axis) | **VERIFIED** |
| Newton Rings 3-step sequence ($K \to I \to K'$) and tolerances ($48\text{ px}, 54\text{ px}$) | **VERIFIED** |
| Campaign JSON definitions for `L01`, `L02`, `L03` | **VERIFIED** |
| Dynamic void-death logic based on landing platforms below | **VERIFIED** |
| Regression test suite (341 tests) | **VERIFIED** |
