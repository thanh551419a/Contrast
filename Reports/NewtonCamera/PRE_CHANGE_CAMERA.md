# PRE-CHANGE NEWTON CAMERA AUDIT REPORT

## 1. Current Camera Architecture (`CameraFollow.cs`)

### 1.1 Single Authoritative Camera Component
- **File**: `Assets/Script/Contrast/Camera/CameraFollow.cs`
- **Execution Order**: `GameManager.Update()` calls `cameraFollow.ProcessCameraUpdate()` once per frame after `TrajectorySystem.ProcessUpdate`, `PlayerController.ProcessUpdate`, and `LevelLoader.ProcessPlayerInteractions`.
- **Camera Modes (`CameraMode` enum)**:
  - `NormalPlatformingFollow`: Standard gameplay camera tracking `target.position` (`PlayerController.transform.position`), clamped inside `cameraBoundary` (`boundaryCenter`, `boundarySize`), with `normalOrthoSize = 5.0f`.
  - `NewtonFixed`: Fixed world-space camera at `newtonFixedPosition` with `newtonFixedOrthoSize`.
  - `NewtonPlayerFollow`: Legacy player-follow zoom mode.
  - `NewtonResultReveal`: Post-capture reveal framing (`Lerp` toward `revealPosition` and `revealOrthoSize = 9.6f`).
  - `TestModeFreeMove`: Editor Test Mode 1:1 mouse-driven camera.

---

## 2. Current Newton Camera Trigger & Transition Behavior (`EasyNewtonFinaleController.cs`)

### 2.1 Current Trigger Condition
In `EasyNewtonFinaleController.Update()` (`Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs:1054–1061`):
```csharp
float px = player.transform.position.x;
float chamberZoneThreshold = centerO.x - ringScale * 4.25f;
bool inZone = px >= chamberZoneThreshold;
if (inZone != isZoneActive)
{
    SetNewtonZoneActive(inZone);
}
```
And in `SetNewtonZoneActive(bool active)` (lines `993–1029`):
- When `active == true`: Calls `cameraFollow.SetNewtonFixed(fixedCenter, orthoSize, 3.2f)`.
- In `CameraFollow.SetNewtonFixed` (lines `92–106`):
  ```csharp
  transform.position = newtonFixedPosition;
  if (cam != null && cam.orthographic)
  {
      cam.orthographicSize = orthoSize;
  }
  ```
- When `active == false`: Calls `cameraFollow.ResetToNormal(5.0f, 3.0f)`.

### 2.2 Identified Deficiencies Relative to Task E Requirements
1. **Trigger Threshold Location**:
   - Currently uses `chamberZoneThreshold = centerO.x - ringScale * 4.25f` (`79.84375 - 14.609375 = 65.234375` in `L02`), which is an arbitrary offset from `centerO.x` rather than the **left world-space AABB boundary (`platformBounds.min.x`) of the platform immediately before the Newton Ring**.
   - In `L02.json`, the platform immediately before the Newton Ring is platform index `13`:
     - `position = (67.617188, -9.640625)`
     - `size = (1.953125, 0.8125)`
     - Left boundary: `thresholdX = 67.617188 - 1.953125 * 0.5 = 66.6406255` (`4265 px / 64`).
   - In `L01.json`, the platform immediately before the Newton Ring is platform index `14`:
     - `position = (68.414062, -12.625)`
     - `size = (2.890625, 1.21875)`
     - Left boundary: `thresholdX = 68.414062 - 2.890625 * 0.5 = 66.9687495` (`4286 px / 64`).
   - In `L03.json`, the platform immediately before the Newton Ring is platform index `21`:
     - `position = (65.75, -33.296875)`
     - `size = (6.84375, 0.96875)`
     - Left boundary: `thresholdX = 65.75 - 6.84375 * 0.5 = 62.328125` (`3989 px / 64`).
2. **Instant Snap Instead of Smooth Reversible Interpolation**:
   - `SetNewtonFixed` currently assigns `transform.position = newtonFixedPosition` and `cam.orthographicSize = orthoSize` immediately in a single frame, causing an abrupt visual jump.
   - `ResetToNormal` immediately switches `currentMode = CameraMode.NormalPlatformingFollow`, which snaps `transform.position` back to the Player's clamped follow position on the very next `ProcessCameraUpdate()`, while `orthographicSize` lerps separately.
3. **Lack of Mid-Transition Reversibility**:
   - Because `NormalPlatformingFollow` overwrites `transform.position` directly without interpolating from the current blended camera position, crossing the threshold right-to-left mid-transition causes a positional snap back to the Player rather than smoothly reversing from the exact in-flight camera position and orthographic size.
4. **Lack of Explicit `minScale` / `maxScale` Clamping**:
   - `CameraFollow` does not currently enforce explicit `minScale` (`5.0f`) and `maxScale` (`newtonWideOrthographicSize`) bounds with outward velocity clamping on every frame.

---

## 3. Target Design for Bidirectional Reversible Newton Camera (Task E)

1. **Authoritative Threshold (`thresholdX`)**:
   - Computed from the pre-Newton platform's world AABB left edge: `thresholdX = preNewtonPlatformBounds.min.x`.
   - Comparison rules:
     - `playerWorldX > thresholdX` $\rightarrow$ `TargetCameraMode = NewtonWide` (`DesiredCameraPosition = newtonCameraCenter`, `DesiredOrthographicSize = maxScale`).
     - `playerWorldX < thresholdX` $\rightarrow$ `TargetCameraMode = FollowPlayer` (`DesiredCameraPosition = CalculateCurrentPlayerFollowTarget()`, `DesiredOrthographicSize = minScale`).
     - `playerWorldX == thresholdX` $\rightarrow$ Preserve current `TargetCameraMode` (no oscillation on exact boundary equality).
2. **Simultaneous Reversible Damping (`SmoothDamp`)**:
   - Maintain persistent `positionVelocity` (`Vector3`) and `scaleVelocity` (`float`) inside `CameraFollow`.
   - Every frame during normal/Newton gameplay, update `transform.position` and `cam.orthographicSize` from their **actual current values** toward `DesiredCameraPosition` and `DesiredOrthographicSize`.
   - When the Player reverses across `thresholdX` mid-transition, only the target (`DesiredCameraPosition`, `DesiredOrthographicSize`) changes; interpolation continues smoothly from the current in-flight state without resetting or snapping.
3. **Strict Scale Bounds (`minScale` and `maxScale`)**:
   - `minScale = 5.0f` (`normalOrthoSize`).
   - `maxScale = newtonFixedOrthoSize` (`8.60f` in L02, `10.25f` in L01, `11.01f` in L03).
   - Clamp `cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minScale, maxScale)` every frame and zero outward `scaleVelocity` at the limits.

