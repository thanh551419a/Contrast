# CONTRAST — MOVING PLATFORM JITTER REGRESSION & TEST MATRIX RESULTS

## 1. Automated Test Execution Summary

| Test Suite | Command / Runner | Total Assertions | Passed | Failed | Blocked | Status |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Unity 6 Batchmode Suite (`EditorLevelTests`)** | `Unity.exe -batchmode -quit -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameWindows` | **523** | **523** | **0** | **0** | **PASS** |
| **Standalone `.NET 10` Suite (`ContrastTests`)** | `dotnet run --project "Tests/ContrastTests/ContrastTests.csproj"` | **441** | **441** | **0** | **0** | **PASS** |

---

## 2. Mandatory Test Matrix (Cases A–O)

| Case ID | Scenario | Verification Method & Measured Outcome | Status |
| :---: | :--- | :--- | :---: |
| **A** | **Stationary on moving platform** (`Line` horizontal, vertical, diagonal, `Circle`) | 60 frames stationary on moving platform (`CaseAB_HorizPlat`). Relative offset drift $= 0.0000\text{ units}$, `IsGrounded == true` on 60/60 frames (`0` false Falling toggles), `IsPushingOut == false` (`0` overlap push-outs). | **PASS** |
| **B** | **Walking along horizontal moving platform** (`A` / `D` with and against platform motion) | 60 frames walking right (`+3.0 u/s`) and left (`-3.0 u/s`) while platform moves horizontally. Net displacement matches $\sum (\Delta \mathbf{P} + \Delta \mathbf{M}_{\text{self}})$ within $< 10^{-4}\text{ units}$; `controlMultiplier == 1.00f` on 60/60 frames. | **PASS** |
| **C** | **Walking / standing on vertical moving platform** (upward $\Delta P_y > 0$ and downward $\Delta P_y < 0$) | 180 frames across upward and downward cycles (`CaseCEI_VertPlat`). `IsGrounded == true` on 180/180 frames, `IsPushingOut == false` on 180/180 frames, `overlapPercent == 0.000%`. | **PASS** |
| **D** | **Diagonal moving platform** ($\Delta P_x \neq 0, \Delta P_y \neq 0$, `rotation = 45°`) | 120 frames on 45° diagonal `PingPong` platform (`CaseD_DiagPlat`). `ResolveCarrier` lifts Y before X when $\Delta P_y > 0$, producing `0` ground drops and `0` horizontal self-blocks (`BlockedX == false`). | **PASS** |
| **E** | **PingPong turnaround** ($t \to 1.0 \to t$ endpoint reversal) | Inspected frames 56–65 across top turnaround (`y = 5.00000`) on `CaseCEI_VertPlat`. Relative offset remains locked at `(1.10000, 0.55010)` with `0` ground drops and `0` position spikes. | **PASS** |
| **F** | **Circle / Orbital trajectory** ($360^\circ$ full revolution) | 240 frames (full 4.0 s revolution, `radius = 3.0`, `0°..360°`) on `CaseF_CirclePlat`. 240/240 frames grounded, `0` false push-outs, relative offset locked at `(0.00000, 0.55010)` across all 4 quadrants. | **PASS** |
| **G** | **Landing on moving platform** (including rising platform swept top-surface) | Airborne falling player (`moveY = -0.15`) lands on rising platform (`FrameDisplacement.y = +0.10`) whose top surface swept past `current.Min.y`. Snaps cleanly to `platformBounds.Max.y + 0.3001` on contact frame 1 with `IsGrounded == true, IsPushingOut == false`. | **PASS** |
| **H** | **Jumping away from moving platform** | `jumpMotion.RequestJump()` sets `IsJumping = true`; `TrajectorySystem.GetRiderDisplacement(..., isJumping: true)` immediately returns `(0, 0)`, detaching cleanly on the jump takeoff frame without sticky airborne carry. | **PASS** |
| **I** | **Platform edge behavior** | Player standing at `x = +1.10` (`0.10` units inside right edge of `3.0`-wide rising platform `CaseCEI_VertPlat`) experiences `0.0000` horizontal kick (`maxEdgeHorizKick = 0.0000`), eliminating the legacy sideways edge ejection. | **PASS** |
| **J** | **Newton orbital platforms `K` and `I` (`L01`, `L02`, `L03`)** | Loaded `Levels/L01`, `Levels/L02`, and `Levels/L03`; verified automatic orbital activation (`orbitStart = "auto"`), 90-frame continuous grounded rider carry on `K` and `I` (`0` ground drops), orbital transfer jump `K -> I`, and `X`-key marking sequence (`K -> I -> K' -> Complete`). | **PASS** |
| **K** | **Level 3 (`L03`) authored moving platforms** | Loaded `Levels/L03` and tested all 3 authored moving platforms (`#12`, `#17`, `#24`, including vertical and horizontal `PingPong` platforms) for 90 frames each: `0` ground drops, `0` false push-outs. | **PASS** |
| **L** | **Camera stability while riding moving platform** | 60 frames riding rising platform `CaseL_CamPlat`: player/camera vertical target advances monotonically (`0` sign reversals, `0` vertical shake oscillations). | **PASS** |
| **M** | **Color collision & 50% stuck overlap rules** | Verified same-color platform is pass-through (`GetRiderDisplacement == (0, 0)`), contrasting/Universal (`255`) platforms block and carry, `25%` interior overlap triggers `IsPushingOut = true`, and `83.33%` (`>= 50%`) interior overlap triggers `IsStuck = true`. | **PASS** |
| **N** | **`T` Debug Mode non-interference** | Verified `GameManager.Update` skips `GetRiderDisplacement` while `player.IsTestModeActive == true`, and normal collision/platform carry resumes cleanly when `T` Debug Mode is toggled off. | **PASS** |
| **O** | **Level restart and transition** | Verified `ResetPhase()` and `LevelLoader` level transitions zero `FrameDisplacement` cleanly (`(0, 0)`) with no stale displacement spikes. | **PASS** |

