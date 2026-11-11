# `deathY` TEST RESULTS (`Reports/DeathY/DEATHY_TEST_RESULTS.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Test Harnesses Executed:**
1. Unity 6 (`6000.5.8f1`) Batchmode Suite (`EditorLevelTests.Test_DeathY_FixedWorldSpace_Suite`) — **620 / 620 PASSED**
2. Standalone `.NET 10.0` Suite (`ContrastTests.Program.Test_DeathY_FixedWorldSpace_Suite`) — **496 / 496 PASSED**

---

## Matrix Results (`D01`–`D12`)

| Case | Scenario | Expected | Actual Result | Status |
|---|---|---|---|---|
| **D01** | Player above `deathY` (`deathY + 0.50` / `+0.01`, zero platforms below) | Does not die (`State != Dead`) | `State == Falling`, no premature void-grace death | **PASS** |
| **D02** | Player at exact boundary (`playerY == deathY`) | Strict `<` comparison: does not die at boundary | `!(deathY < deathY)` verified in `.NET` and Unity | **PASS** |
| **D03** | Player crosses below `deathY` (`deathY - 0.01` / `-0.05`) | Dies immediately (`State == Dead`) | `State == PlayerState.Dead` on crossing frame | **PASS** |
| **D04** | Player stays below `deathY` across subsequent frames | Death triggers once; no duplicate respawn loop | `if (State == PlayerState.Dead) return;` + `isAutoRespawning` guard | **PASS** |
| **D05** | Fall death in `L01` (`deathY = -24.75`) | Uses L01 `deathY = -24.75` and respawns at latest checkpoint | `dataL01.deathY == -24.75`, `ComputeKillY == -24.75` | **PASS** |
| **D06** | Fall death in `L02` (`deathY = -23.25`) | Uses L02 `deathY = -23.25` and respawns at latest checkpoint | `dataL02.deathY == -23.25`, `ComputeKillY == -23.25` | **PASS** |
| **D07** | Fall death in `L03` (`deathY = -49.125`) | Uses L03 `deathY = -49.125` and respawns at latest checkpoint | `dataL03.deathY == -49.125`, `ComputeKillY == -49.125` | **PASS** |
| **D08** | Custom level with custom `deathY` (`-31.5` / `-19.5`) | Uses custom `deathY` value and round-trips through JSON | `ComputeKillY == -31.5`, JSON contains `"deathY": -31.5` | **PASS** |
| **D09** | Normal jump/drop to low valid platform (`L01 = -17.75`, `L02 = -16.25`, `L03 = -42.125`) | Does not falsely trigger death (`> 6.9u` margin above `deathY`) | Lowest platforms have `> 6.9u` clearance above `deathY` | **PASS** |
| **D10** | Riding lowest moving/Newton orbital platform (`L01 = -18.89`, `L02 = -15.30`, `L03 = -42.08`) | Does not falsely trigger death (`> 5.8u` margin above `deathY`) | Lowest orbital troughs have `> 5.8u` clearance above `deathY` | **PASS** |
| **D11** | Newton fixed zoomed camera active (`CameraMode.NewtonFixed`) | `deathY` still uses world Y, completely invariant to camera zoom/bounds | `pc.EffectiveDeathY` unchanged (`delta = 0.00000`) | **PASS** |
| **D12** | Respawn after `deathY` death | Normal movement, `IsGrounded == true`, and first-frame jump work | `State == Grounded`, immediate jump `vy = 12.38 u/s` succeeds | **PASS** |

