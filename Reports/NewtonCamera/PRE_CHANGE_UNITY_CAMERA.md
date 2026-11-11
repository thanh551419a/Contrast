# PRE-CHANGE UNITY CAMERA AUDIT (`Reports/NewtonCamera/PRE_CHANGE_UNITY_CAMERA.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Base Commit:** `497c601d3f52095df712228c9e69a024d08ae23c`  
**Audit Status:** Read-only inspection of current Unity camera implementation **before** any code changes.

---

## 1. Inspected Unity Files

1. `Assets/Script/Contrast/Camera/CameraFollow.cs` (`lines 1–249`)
2. `Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs` (`lines 252–264, 599–635, 921–955, 1115–1128`)
3. `Assets/Script/Contrast/Level/LevelLoader.cs` (`lines 341–381`)

---

## 2. Current `CameraFollow.cs` Behavior (`Pre-Change`)

### 2.1 Camera Modes (`CameraFollow.cs:5–11`)
```csharp
public enum CameraMode
{
    NormalPlatformingFollow,
    NewtonPlayerFollow,
    NewtonResultReveal,
    TestModeFreeMove
}
```

### 2.2 Normal Platforming Follow (`NormalPlatformingFollow`)
- Default `normalOrthoSize = 5f` (`VisibleWorldHeight = 10.0 u`, `VisibleWorldWidth = 17.778 u` at 16:9).
- In `ProcessCameraUpdate()` (`lines 125–221`):
  - Snaps camera X/Y directly to `target.position.x` and `target.position.y` (`desiredX = target.position.x; desiredY = target.position.y;`).
  - Clamps `(desiredX, desiredY)` to `boundaryCenter ± boundarySize * 0.5f` minus `(horzExtent, vertExtent)`.
  - Smoothly interpolates `cam.orthographicSize` toward `targetOrthoSize` (`5.0f`) at `orthoLerpSpeed` (`3.0f`).

### 2.3 Pre-Change Newton Marking Camera (`NewtonPlayerFollow`)
- Triggered in `EasyNewtonFinaleController.UpdateCameraControl()` (`lines 921–955`) when the Player is within `distanceToO < 15.8f` of `centerO`:
  ```csharp
  if (!cameraOverridden)
  {
      cameraOverridden = true;
      cameraFollow.SetNewtonPlayerFollow(6.5f, 2.5f);
  }
  ```
- **How `NewtonPlayerFollow` currently behaves (`CameraFollow.cs:62–72, 165–210`):**
  - Sets `currentMode = CameraMode.NewtonPlayerFollow`, `targetOrthoSize = 6.5f`, and **`hasOverride = false`**.
  - Because `hasOverride == false`, `ProcessCameraUpdate()` (`lines 165–183`) sets:
    ```csharp
    desiredX = target.position.x;
    desiredY = target.position.y;
    ```
    and then at `lines 207–210` snaps the camera position directly to `(desiredX, desiredY, -10f)` every frame!
- **Why this violates the HTML prototype and Task B requirements:**
  1. **Camera is NOT fixed in world space during Newton X-marking:** Instead of locking to a fixed Newton chamber framing as in HTML (`lines 1785–1790`), `NewtonPlayerFollow` continuously chases `player.transform.position` around the circular orbits K and I (`desiredX = target.position.x; desiredY = target.position.y;`).
  2. **All 3 targets (`K`, `I`, `K'`) are NOT simultaneously visible:** At `orthoSize = 6.5f` (`half-height = 6.5 u`) centered on the Player, when the Player is at the top or bottom of orbit K (`r_K = 8.42 u` in L01 or `9.09 u` in L03), the opposite side of the ring (`K'`, `2 * r_K = 16.84 u` to `18.19 u` away!) is completely off-screen.
  3. **Overview vs. Marking Framing:** In `EasyNewtonFinaleController`, the full chamber reveal uses `SetNewtonReveal(..., 9.6f, 3.2f)`, whereas during active Newton X-marking the camera was following the player instead of locking to a fixed zoomed world-space view of the Newton interaction region.

---

## 3. Summary of Required Changes for Task B

1. Introduce a dedicated fixed world-space Newton marking camera mode (`CameraMode.NewtonFixed` / updated Newton framing in `CameraFollow`) that locks the camera's world-space target `(fixedX, fixedY)` and orthographic size derived from the HTML prototype's `newtonViewLayout(cfg)` and the active level's Newton geometry.
2. While in Newton X-marking mode:
   - The camera position is **fixed in world space** (does **not** follow `player.transform.position` when the player runs, jumps, or rides orbits K and I, and does **not** shift when pressing `X` on Target 1 or Target 2).
   - All 3 sequential target points (`K`, `I`, `K'`) and both orbiting platforms `K` and `I` remain inside the visible camera rect (`IsWorldPointVisible`).
3. Exiting the Newton zone, respawning at a checkpoint outside the Newton zone, or restarting/reloading the level cleanly restores `CameraMode.NormalPlatformingFollow` (`orthoSize = 5.0f`).

