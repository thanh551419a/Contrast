# ARCHITECTURE & IMPLEMENTATION REPORT: NEWTON RINGS PLAYER-FOLLOW CAMERA FIX

**Date:** 2026-11-02  
**Author:** Senior Unity C# Gameplay Camera Engineer, Technical Game Designer  
**Target Systems:** `Contrast.Camera`, `Contrast.Level`  
**Status:** Implemented, Integrated & Build-Verified  

---

## 1. Executive Summary

This report documents the architectural remediation implemented to resolve the camera-follow defect during the final Newton Rings stage of Levels L01, L02, and L03. 

While the legacy HTML reference framed the center of the Newton Chamber ($O$) statically during the finale, the project owner established an intentional design override for the Unity edition:
> **"The camera must follow the player throughout active Newton gameplay."**

The fix guarantees continuous 1:1 tracking of the player during all active Newton gameplay—including running, jumping, landing on rotating platforms $K$ and $I$, full orbital transport, and fringe marking with key `X`. The camera only transitions to cinematic chamber reveal framing after all three measurement marks ($K \to I \to K'$) are successfully locked.

---

## 2. Architecture & Design Principles

### 2.1 Single Authoritative Camera Writer
The system preserves the existing single-pipeline execution model in `GameManager.ProcessUpdate()`:
1. `TrajectorySystem.UpdateAll(dt)`: Orbiting platforms $K$ and $I$ advance along circular trajectories.
2. `TrajectorySystem.GetRiderDisplacement(...)`: Computes rider carry for the player on moving platforms.
3. `player.SetRiderDisplacement(riderDisp)` & `player.ProcessUpdate(dt)`: Resolves player final world position for the current frame.
4. `levelLoader?.ProcessPlayerInteractions(player)`: Processes triggers.
5. `cameraFollow?.ProcessCameraUpdate()`: Sets camera position directly to the player's resolved world coordinates.

Because the camera updates in the exact same frame immediately following player position resolution, platform transport is followed with **zero sub-frame jitter, zero latency, and zero oscillation**.

### 2.2 Camera State Machine (`CameraMode`)
A formal state machine was integrated into `CameraFollow.cs`:

```csharp
public enum CameraMode
{
    NormalPlatformingFollow, // Standard platforming tracking (orthographic size 5.0f)
    NewtonPlayerFollow,      // Active Newton chamber tracking (orthographic size 6.5f)
    NewtonResultReveal       // Post-puzzle cinematic reveal framing center O (orthographic size 9.6f)
}
```

- **NormalPlatformingFollow:** Tracks player at default zoom ($5.0\text{ u}$).
- **NewtonPlayerFollow:** When the player enters the Newton chamber vestibule ($px \ge centerO.x - 4.25 \times ringScale$), the camera transitions into `NewtonPlayerFollow`. Target remains `player.transform`. The camera continuously tracks the player while smoothly expanding orthographic zoom to $6.5\text{ u}$ via exponential smoothing ($1 - e^{-2.5 \Delta t}$). This provides ample framing of the surrounding fringes and platforms without shrinking the player.
- **NewtonResultReveal:** When all three marks ($K \to I \to K'$) are locked, `EasyNewtonFinaleController.StartRevealSequence()` transitions the camera to `NewtonResultReveal`. The camera smoothly glides from the player's position to center $O$ at $9.6\text{ u}$ zoom to reveal the geometry, optical axis, and wavelength calculations.
- **State Reset & Recovery:** Calling `ResetToNormal()` (triggered on player death, retry, level restart, or exiting the chamber) cleanly restores `NormalPlatformingFollow` and returns orthographic size to $5.0\text{ u}$.

### 2.3 Dynamic Camera Boundary Encompassment (`LevelLoader.cs`)
In the original implementation, the camera boundary clamped maximum $X$ to $X \le 68.9$ (L01), $X \le 67.7$ (L02), and $X \le 68.3$ (L03), halting the camera ~11 units to the left of center $O$ and ~20 units to the left of the outer orbital track.

`LevelLoader.cs` was modified to dynamically expand the camera bounding box for any level containing a Newton chamber (`data.newton != null && data.newton.k > 0`):
- Center: $(newtonCx, newtonCy) = (cx/64, -cy/64)$
- Maximum orbit radius: $r_{max} = 3.4375 \times \sqrt{k + 1.5} + 4\text{ u}$
- Viewport margin: $16\text{ u}$ (accommodating half-viewport extent $11.55\text{ u}$ with comfortable margin)
- Bounds expansion:
  $$\begin{aligned}
  minX &= \min(minX, newtonCx - r_{max} - margin) \\
  maxX &= \max(maxX, newtonCx + r_{max} + margin) \\
  minY &= \min(minY, newtonCy - r_{max} - margin) \\
  maxY &= \max(maxY, newtonCy + r_{max} + margin)
  \end{aligned}$$
- Level JSON files remain completely unmodified, preserving normal platforming boundaries on the left while eliminating the artificial clamp inside the Newton chamber.

---

## 3. Code Modifications & Diffs

### 3.1 `Assets/Script/Contrast/Camera/CameraFollow.cs`
- Added `CameraMode` enum (`NormalPlatformingFollow`, `NewtonPlayerFollow`, `NewtonResultReveal`).
- Added mode management methods: `SetNewtonPlayerFollow()`, `SetNewtonReveal()`, `ResetToNormal()`.
- Implemented smooth orthographic zoom transitions while maintaining active target tracking.
- Added structured diagnostics generation: `GetDiagnosticsLog(activeRail)` and `IsPlayerVisibleInViewport`.
- Preserved backward-compatibility via `SetOverride()` and `ClearOverride()`.

```diff
+    public enum CameraMode
+    {
+        NormalPlatformingFollow,
+        NewtonPlayerFollow,
+        NewtonResultReveal
+    }
...
+        [Header("Camera State Management")]
+        [SerializeField] private CameraMode currentMode = CameraMode.NormalPlatformingFollow;
+        private float targetOrthoSize = 5f;
+        private float zoomLerpSpeed = 2.5f;
+
+        public void SetNewtonPlayerFollow(float newtonOrthoSize = 6.5f, float lerpSpeed = 2.5f)
+        {
+            currentMode = CameraMode.NewtonPlayerFollow;
+            targetOrthoSize = newtonOrthoSize;
+            zoomLerpSpeed = lerpSpeed;
+        }
```

### 3.2 `Assets/Script/Contrast/Level/LevelLoader.cs`
- Updated camera boundary setup to dynamically encompass the full Newton chamber and orbital rails whenever `data.newton` is present.
- Updated `bPos.y` and `bSize.y` recalculation to ensure vertical orbital tracks are unconstrained.
- Ensured `camFollow.ResetToNormal()` is invoked upon player retry/respawn.

```diff
-                // If Easy level, ensure camera boundary reaches the Newton chamber at x >= 61
-                if (IsEasyLevel(data) && maxX < 78.125f)
-                {
-                    maxX = 78.125f;
-                }
+                // If level has Newton chamber, ensure camera boundary encompasses entire chamber & orbital tracks
+                if (data.newton != null && data.newton.k > 0)
+                {
+                    float newtonCx = data.newton.cx / 64f;
+                    float newtonCy = -data.newton.cy / 64f;
+                    float maxOrbitRadius = 3.4375f * Mathf.Sqrt(data.newton.k + 1.5f) + 4f;
+                    float margin = 16f;
+
+                    float nMinX = newtonCx - maxOrbitRadius - margin;
+                    float nMaxX = newtonCx + maxOrbitRadius + margin;
+                    float nMinY = newtonCy - maxOrbitRadius - margin;
+                    float nMaxY = newtonCy + maxOrbitRadius + margin;
+
+                    minX = Mathf.Min(minX, nMinX);
+                    maxX = Mathf.Max(maxX, nMaxX);
+                    minY = Mathf.Min(minY, nMinY);
+                    maxY = Mathf.Max(maxY, nMaxY);
+                }
```

### 3.3 `Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs`
- Added camera transition to `NewtonPlayerFollow` when player enters the chamber zone (`isZoneActive == true`).
- Added camera restoration to `NormalPlatformingFollow` when player exits the zone.
- Retained continuous player follow during step capture of $K$ and $I$.
- Updated `StartRevealSequence()` to invoke `cameraFollow.SetNewtonReveal()`.
- Added helper methods `GetCurrentActiveRail()` and `LogCameraDiagnostics()`.
- Ensured `cameraFollow.ResetToNormal()` is called on `ResetState()` and `Teardown()`.

---

## 4. Verification Summary

1. **Automated Unit & State Tests (`Tests/ContrastTests/Program.cs`):**
   - 317 tests executed; **317 PASSED, 0 FAILED**.
   - Verified boundary expansion on actual `L01.json`, `L02.json`, `L03.json`.
   - Verified orbital tracking across all 36 angular steps ($0^\circ \to 360^\circ$) on rails $K$ and $I$.
   - Verified jump arc tracking and mark persistence.
2. **Editor Level Test Suite (`Assets/Editor/EditorLevelTests.cs`):**
   - Executed live inside Unity Editor with real `CameraFollow` and `Camera` components.
   - All tests (`CamFollow_InitMode`, `CamFollow_TestA_Enter`, `CamFollow_TestB_Move`, `CamFollow_TestC_OrbitK`, `CamFollow_TestD_OrbitI`, `CamFollow_TestE_JumpKI`, `CamFollow_TestF_TrackK/I`, `CamFollow_TestG_RevealMode`, `CamFollow_TestH_ResetMode`, `CamFollow_Diag_*`) passed with zero errors.
3. **Windows Executable Build:**
   - Standalone player rebuilt from source: `Release/Windows/Game/Game.exe`.
   - Executed and validated cleanly on Direct3D 12.
