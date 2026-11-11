# ROOT CAUSE INVESTIGATION REPORT: NEWTON RINGS PLAYER-FOLLOW CAMERA

**Date:** 2026-11-02  
**Component:** `Contrast.Camera.CameraFollow`, `Contrast.Level.EasyNewtonFinaleController`, `Contrast.Level.LevelLoader`  
**Target Levels:** L01, L02, L03  
**Status:** Root Cause Identified & Verified  

---

## 1. Executive Summary

During the final Newton Rings gameplay section of Levels L01, L02, and L03, players must navigate onto rotating orbital platforms ($K$ and $I$) around the optical center $O$ and press `X` to lock in reference fringes. Under the original HTML implementation, the camera framed the Newton Chamber center statically at high zoom. 

The project owner explicitly specified that for the Unity version:
> **"The camera must follow the player throughout active Newton gameplay."**

Investigation revealed that when the player reached the Newton section, the camera stopped following the player due to two combined root causes:
1. **Severe Camera Boundary Clamping:** The camera boundary clamp (`boundaryCenter` / `boundarySize`) in `LevelLoader.cs` and the level JSON files clamped maximum camera $X$ to $X \le 68.9$ (L01), $X \le 67.7$ (L02), and $X \le 68.3$ (L03). Because the Newton Chamber center $O$ is located at $X \approx 80-81$, and rail $K$ orbits out to $X \approx 89-91$, the camera stopped at the vestibule, causing the player to walk and ride completely off-screen.
2. **Absence of Dedicated Newton Follow State Architecture:** There was no explicit camera state distinguishing normal platforming, active Newton player tracking, and final result reveal. Camera overrides (`SetOverride`) completely detached target tracking to lerp towards a static position, with no mechanism to follow the player at an appropriate framing/zoom while maintaining continuous tracking during platform rotation, jumps, and $X$-marking.

---

## 2. Detailed 10-Point Technical Investigation

### Question 1: Which component controls the camera during normal platforming?
- **Component:** `Contrast.Camera.CameraFollow` on `Main Camera`.
- **Execution Point:** Called authoritatively by `GameManager.ProcessUpdate()` at line 158 of `Assets/Script/Contrast/Core/GameManager.cs` every frame during `GameState.Playing`.
- **Target:** Follows `player.transform.position` with boundary clamping when `hasBoundary` is true.

### Question 2: Which component controls the camera during Newton gameplay?
- **Component:** `CameraFollow` remains the sole component attached to `Main Camera`.
- **Behavior in Newton Stage:**
  - Before fix: `CameraFollow` remained in normal follow mode, but hit the boundary clamp at $X \approx 68.9$, freezing the camera in place while the player continued into the chamber.
  - Upon completion: `EasyNewtonFinaleController.StartRevealSequence()` called `cameraFollow.SetOverride(new Vector3(centerO.x, centerO.y, -10f), 9.6f, 3.2f)`.

### Question 3: Whether Newton initialization overrides the follow target?
- **Finding:** No. In `LevelLoader.cs` lines 340–348, `SetupNewtonFinale()` initializes `EasyNewtonFinaleController` with references to `player` and `camFollow`. The follow target remains assigned to `player.transform`. The tracking failure was not caused by target nullification or reassignment.

### Question 4: Whether the camera becomes fixed at the Newton center?
- **Finding:**
  - During the final reveal sequence, `StartRevealSequence()` intentionally called `SetOverride` to frame center $O$ at ortho size 9.6f.
  - During active gameplay prior to reveal, the camera appeared "fixed" near the entrance because `CameraFollow` clamped `desiredX` to $maxX - horzExtent$. Because the player was moving into $X \in [71.5, 90.7]$, the camera remained clamped at $X \approx 68.9$, giving the illusion of a frozen camera.

### Question 5: Whether camera boundaries prevent tracking? (PRIMARY PHYSICAL ROOT CAUSE)
- **Finding:** **YES. This was the direct blocker preventing camera tracking.**
  - **L01:**
    - Level JSON boundary: Center $X = 29.758$, Size $X = 96.203 \implies maxX = 77.8595$.
    - With orthographic size 5.0 and 16:9 aspect ratio ($horzExtent = 8.889$):
      $$\text{Max Allowed Camera } X = 77.8595 - 8.889 = 68.9705$$
    - Chamber center $O.x = 5135 / 64 = 80.234$.
    - Rail $K$ radius $r_K = 3.4375 \times \sqrt{6.5} \approx 8.764 \implies X_{orbit} \in [71.47, 88.99]$.
    - Platform $K$ starts at $X \approx 88.21$.
    - Result: The camera was halted ~11 units to the left of $O$, and ~20 units to the left of the player on $K$. The player drifted completely off the right edge of the screen ($68.97 + 8.89 = 77.86 < 88.21$).
  - **L02:**
    - JSON boundary $maxX = 76.591 \implies \text{Camera } X \le 67.70$.
    - Chamber center $O.x = 5110 / 64 = 79.844$. Rail $K$ reaches $X = 87.13$.
    - Result: Camera stopped at $X = 67.70$; player disappeared off-screen.
  - **L03:**
    - JSON boundary $maxX = 77.172 \implies \text{Camera } X \le 68.28$.
    - Chamber center $O.x = 5201 / 64 = 81.266$. Rail $K$ reaches $X = 90.68$.
    - Result: Camera stopped at $X = 68.28$; player disappeared off-screen.
  - In `LevelLoader.cs`, lines 298–302 previously attempted to expand `maxX`:
    `if (IsEasyLevel(data) && maxX < 78.125f) maxX = 78.125f;`
    This failed because:
    1. `IsEasyLevel` evaluated to `false` for L01, L02, L03 (as their file paths do not contain "easy" or "map2").
    2. Even if it ran, $78.125f$ was far too small, as the camera needed to reach $X \ge 90.7 + 8.9 = 99.6f$.
    3. `bPos.y` and `bSize.y` were not updated to cover the vertical extent of the orbit ($Y \in [-19.1, -1.5]$ in L01).

### Question 6: Whether camera updates stop during Newton-specific states?
- **Finding:** No. `GameManager.ProcessUpdate()` continuously calls `cameraFollow.ProcessCameraUpdate()` every frame. The method was running, but clamping prevented any transform position change.

### Question 7: Whether multiple systems compete to write the camera transform?
- **Finding:** No. Grep analysis confirmed that `CameraFollow` is the sole writer to `Camera.transform.position`. There are no competing update loops, physics camera scripts, or LateUpdate conflicts.

### Question 8: Whether movement from the K/I platforms is reflected in camera updates?
- **Finding:** Yes. The frame pipeline execution order in `GameManager.cs` is strictly sequential:
  1. `TrajectorySystem.UpdateAll(Time.deltaTime)` advances platform trajectories.
  2. `TrajectorySystem.GetRiderDisplacement(...)` computes rider-carry delta.
  3. `player.SetRiderDisplacement(riderDisp)` and `player.ProcessUpdate(Time.deltaTime)` resolves final player world position.
  4. `levelLoader?.ProcessPlayerInteractions(player)` checks triggers.
  5. `cameraFollow?.ProcessCameraUpdate()` updates camera position from player transform.
  Because the camera updates in the exact same frame immediately following the player's final position resolution, platform transport is fully reflected with zero latency and zero jitter.

### Question 9: Whether camera follow runs before or after the player's final position is resolved?
- **Finding:** It runs **after** the player's final position is resolved (step 5 following step 3 in `GameManager.ProcessUpdate()`).

### Question 10: Whether reveal/complete transitions incorrectly activate before marking is complete?
- **Finding:** In `EasyNewtonFinaleController.cs`, `StartRevealSequence()` was correctly guarded and called only after Step 3 ($K'$) succeeded (line 1040). However, the lack of an explicit `NewtonPlayerFollow` state meant the system did not adapt the framing or zoom when entering the chamber, and there was no clean state-machine contract between normal follow, Newton active follow, and final reveal.

---

## 3. Required Remediation Plan

1. **Camera Boundary Dynamic Adaptation (`LevelLoader.cs`):**
   - Automatically expand the level camera boundary whenever `data.newton != null && data.newton.k > 0` to encompass the Newton chamber center $(cx, cy)$, the maximum orbital radius $r_K$, and a generous viewport margin (at least 16 units) along both $X$ and $Y$.
   - Preserves normal platforming boundaries on the left while eliminating the artificial clamp on the right.

2. **Explicit Camera State Architecture (`CameraFollow.cs`):**
   - Introduce `enum CameraMode { NormalPlatformingFollow, NewtonPlayerFollow, NewtonResultReveal }`.
   - In `NormalPlatformingFollow`: Track player with standard orthographic size (5.0f).
   - In `NewtonPlayerFollow`: Continuously track player with comfortable Newton framing (orthographic size 6.5f), smoothly lerping the zoom when entering/exiting the chamber without any position jump.
   - In `NewtonResultReveal`: Smoothly lerp camera position to center $O$ at reveal zoom (9.6f) only after all 3 marks are locked.
   - Support `ResetToNormal()` for clean resets on player death, retry, or level exit.

3. **Newton Finale Controller State Integration (`EasyNewtonFinaleController.cs`):**
   - Switch camera to `NewtonPlayerFollow` when player enters the chamber zone (`isZoneActive == true`).
   - Restore camera to `NormalPlatformingFollow` if player moves back out of the zone.
   - Maintain continuous player follow during $X$ marking of $K$ and $I$.
   - Switch to `NewtonResultReveal` exclusively when $K'$ is successfully locked in `StartRevealSequence()`.
   - Restore `NormalPlatformingFollow` in `ResetState()` and `Teardown()`.

4. **Runtime Diagnostics Logging:**
   - Provide structured runtime diagnostics matching the contract specification:
     `[CAMERA] State=... Target=... PlayerWorld=... CameraWorld=... PlayerScreen=... OrthographicSize=... ActiveRail=... PlayerVisible=...`
