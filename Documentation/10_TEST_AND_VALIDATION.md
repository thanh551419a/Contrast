# Test & Validation Guide: Contrast

> **Target Audience:** Quality assurance engineers, systems testers, and AI agents verifying code changes. This guide provides step-by-step procedures for running automated test suites, executing manual regression passes, and checking critical invariants across the Contrast codebase.

---

## 1. Automated Test Suites Overview

The Contrast codebase features two distinct automated test harnesses providing complete verification without requiring manual scene play:

| Test Harness | Execution Environment | Location / Command | Tests | Coverage Scope |
|---|---|---|:---:|---|
| **Standalone .NET Test Harness** | .NET CLI (`dotnet run`) | `Tests/ContrastTests/` | **91 Assertions** | Trajectory mathematics, timing, spatial clipping, circle/arc orbits, projection, JSON round-trips, schema backward compatibility. |
| **Unity Batchmode Test Suite** | Unity Editor Engine Runtime | `Assets/Editor/EditorLevelTests.cs` | **58 Assertions** | Real Unity MonoBehaviours, transform mutations, `ColorPlatform.All` registry lifecycle, rider-carry displacement, JSON persistence. |

---

## 2. How to Run Automated Tests

### 2.1 Running the Standalone .NET Test Suite
The standalone harness compiles and runs in under 3 seconds via the command line:

```powershell
# Execute from workspace root:
dotnet run --project "Tests/ContrastTests/ContrastTests.csproj"
```

**Expected Result:**
```text
================================================================================
RESULTS: 91 PASSED, 0 FAILED
================================================================================
ALL AUTOMATED TESTS PASSED WITH ZERO DEFECTS!
```
Exit code `0` indicates 100% pass.

### 2.2 Running the Unity Batchmode Test Suite
To execute the tests directly inside the real Unity 6000.5.8f1 engine runtime (headless):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "Y:\Contrast\My project" `
  -executeMethod Contrast.Tests.EditorLevelTests.RunAllEditorTests `
  -logFile "unity_batch.log"
```

**Verification:** Inspect `unity_batch.log`:
```text
[TEST_SUITE] COMPLETED: 58 PASSED, 0 FAILED, 0 BLOCKED
[TEST_SUITE] ALL AUDIT AND ACCEPTANCE TESTS PASSED SUCCESSFULLY!
```

---

## 3. Manual Functional Validation Checklist

When modifying UI, controls, or rendering, execute the following manual tests in `Assets/Scenes/SampleScene.unity`:

### 3.1 Gameplay & Collision Verification
- [ ] **Player Color Shift:**
  - Press `1`: Player turns White. Should walk on Gray, Black, and Universal platforms; falls through White platforms.
  - Press `2`: Player turns Gray. Should walk on White, Black, and Universal platforms; falls through Gray platforms.
  - Press `3`: Player turns Black. Should walk on White, Gray, and Universal platforms; falls through Black platforms.
  - Press Arrow Keys (`Left`/`Right`): Cycles sequentially through colors.
- [ ] **Movement & Jumping:**
  - Hold `A` / `D`: Player translates left/right at constant speed without slipping or accelerating.
  - Press `Space`: Jump height reaches exactly 3 units.
  - Corner hops: Jump against the edge of a solid platform; player slides smoothly over the corner without snagging.
  - Wall slide: Jump against a vertical wall; player descends smoothly without getting stuck horizontally.
- [ ] **Moving Platform Rider-Carry:**
  - Stand on a moving platform (horizontal Line or vertical Elevator in `TrajectoryShowcaseLevel.json`).
  - Verify player stays anchored to the top of the platform as it moves, without sliding off or oscillating.
  - Jump while riding: Player launches upward cleanly from their current world coordinate.
- [ ] **Checkpoints & Fall Death:**
  - Walk past a CheckPoint; verify console log `[CHECKPOINT] reached`.
  - Jump into the pit below `KillY`; verify Game Over screen appears.
  - Press `R` or click **RETRY**; player respawns immediately at the checkpoint with all platforms reset to start points.
- [ ] **Victory Flow:**
  - Touch the End goal marker; verify Game Over overlay switches to `"YOU WIN"` with a **RESTART** button.

### 3.2 Level Editor Verification
- [ ] **Mode Switching:**
  - From Main Menu, click **EDIT LEVEL**; title disappears, player is hidden, editor toolbar, inspector, and palette appear.
- [ ] **Camera Panning:**
  - Hold Middle Mouse Button over empty space and drag; camera pans smoothly.
  - Release MMB; camera stays at new position. Platform world positions do not change.
- [ ] **Platform Selection & Transformation:**
  - Left-click a platform; four white handles appear around it.
  - Drag the platform body; platform moves with cursor. If trajectory is enabled, origin moves in lockstep.
  - Drag Left handle; width expands to the left while right edge remains fixed.
- [ ] **Trajectory Authoring:**
  - In Inspector, check `Enabled` under Trajectory.
  - Click preset `Circle`; green circular preview line appears.
  - Drag the Magenta handle; circle radius expands interactively.
  - Check `Reverse Dir`; platform snaps to the ending point of the trajectory.
- [ ] **Two-Step Safe Deletion:**
  - With a platform selected, click **DELETE PLATFORM**; button turns amber ("CONFIRM DELETE (CLICK AGAIN)").
  - Wait 3 seconds without clicking; button reverts to red.
  - Click twice within 3 seconds; platform is deleted and removed from scene.
- [ ] **Instant Test Play:**
  - Click emerald-green **TEST PLAY** button in top toolbar.
  - Transitions directly to `Playing` state; moving platforms begin traversing their authored paths immediately.
- [ ] **File Export & Import:**
  - Click **EXPORT**; save JSON to disk.
  - Move/delete platforms in editor.
  - Click **IMPORT**; select the exported JSON; level rebuilds exactly as saved.

---

## 4. Known Fragile Areas to Watch During Modifications

1. **`Trajectory.ResetPhase()` vs. `reverseDirection`:**
   - Always initialize `phase = config.reverseDirection ? 1f : 0f` and `pingPongDir = config.reverseDirection ? -1 : 1`. Setting phase unconditionally to 0 causes a frame-1 direction bounce glitch.
2. **`LevelEditorController.BuildExportData()` Coordinate Contract:**
   - Must export `Trajectory.GetInitialPosition(trajData)`. Do NOT export `platform.Position` or `platform.AuthoredPosition` directly for moving platforms.
3. **`ColorPlatform.OnDestroy()` Registry Cleanup:**
   - Ensure `[ExecuteAlways]` remains on `ColorPlatform.cs`. Removing it causes stale entries in `ColorPlatform.All` during edit mode or test teardowns.
4. **Middle-Mouse Camera Pan Delta:**
   - Keep `panStartScreen` and `panStartCameraPosition` cached at mouse-down. Do not compute incremental frame-to-frame delta via re-projected world points, as rounding errors accumulate rapid drift.
