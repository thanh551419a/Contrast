# BIDIRECTIONAL REVERSIBLE NEWTON CAMERA TRANSITION IMPLEMENTATION REPORT

## 1. Architectural Ownership (`CameraFollow.cs`)

- **Single Authoritative Camera Owner**: `Assets/Script/Contrast/Camera/CameraFollow.cs` is the sole component that writes `camera.transform.position` and `camera.orthographicSize`.
- **Pipeline Phase**: Executed once per frame at the end of `GameManager.Update()` via `cameraFollow.ProcessCameraUpdate(deltaTime)` after all platform trajectories, Player movement/collision, and checkpoint interactions have completed.
- **No Competing Camera Controllers**: `EasyNewtonFinaleController` registers the pre-Newton platform reference, fallback left boundary `thresholdX`, and HTML-derived Newton wide framing (`fixedCenter`, `maxScale`) via `cameraFollow.ConfigureNewtonTransition(...)`. It does **not** teleport or directly mutate the camera during gameplay.

---

## 2. Coordinate-Driven Threshold (`platformBounds.min.x`)

In `EasyNewtonFinaleController.FindPreNewtonPlatform` and `CameraFollow.PlatformLeftBoundaryX`:
- The platform immediately before the Newton Ring section is identified from `ColorPlatform.All` as the terrain platform with the largest `Position.x` strictly to the left of the outer Newton ring `K` (`centerO.x - r_K`).
- Its live authoritative world-space AABB left edge is queried each frame:
  ```csharp
  float thresholdX = preNewtonPlatform.GetAabb().xMin;
  ```
- **Per-Level Authoritative `thresholdX` Values**:
  - **`L01`**: Platform `14` (`center = (68.414062, -12.625)`, `size = (2.890625, 1.21875)`) $\rightarrow$ `thresholdX = 66.9687495`
  - **`L02`**: Platform `13` (`center = (67.617188, -9.640625)`, `size = (1.953125, 0.8125)`) $\rightarrow$ `thresholdX = 66.6406255`
  - **`L03`**: Platform `21` (`center = (65.75, -33.296875)`, `size = (6.84375, 0.96875)`) $\rightarrow$ `thresholdX = 62.3281250`

### Threshold Comparison Rules
- **`playerWorldX > thresholdX`**:
  - `TargetCameraMode = CameraMode.NewtonFixed` (`NewtonWide`)
  - `DesiredCameraPosition = new Vector3(newtonFixedPosition.x, newtonFixedPosition.y, z)`
  - `DesiredOrthographicSize = maxScale`
- **`playerWorldX < thresholdX`**:
  - `TargetCameraMode = CameraMode.NormalPlatformingFollow` (`FollowPlayer`)
  - `DesiredCameraPosition = CalculateCurrentPlayerFollowTarget()` (computed from the Player's **live** authoritative position on that frame)
  - `DesiredOrthographicSize = minScale`
- **`playerWorldX == thresholdX`**:
  - Preserves the previous `TargetCameraMode` without triggering unnecessary switching.

---

## 3. Simultaneous Damped Interpolation & Mid-Transition Reversibility

In `CameraFollow.UpdateBidirectionalNewtonCamera(float deltaTime)`:
- **Smoothing Method**: `Vector3.SmoothDamp` for `transform.position` and `Mathf.SmoothDamp` for `cam.orthographicSize`.
- **Configurable Smoothing Parameters**:
  - `positionSmoothTime = 0.25f` seconds
  - `scaleSmoothTime = 0.25f` seconds
- **Persistent State**:
  - `positionVelocity` (`Vector3`)
  - `scaleVelocity` (`float`)
- **Reversibility from Actual Current Camera State**:
  - Every frame reads the actual current `transform.position` and `cam.orthographicSize` as the starting point for `SmoothDamp`.
  - When the Player reverses across `thresholdX` during an in-flight zoom-out or zoom-in, only `DesiredCameraPosition` and `DesiredOrthographicSize` change. The camera never resets to `minScale` or `maxScale` and never teleports between centers.
- **Progressive Live Player Coupling on Return**:
  - While returning toward `FollowPlayer` (`targetCameraMode == CameraMode.NormalPlatformingFollow`), the camera progressively couples to the Player's live frame-to-frame displacement (`followCoupling = 1f - (currentScale - minScale) / (maxScale - minScale)`) on non-reversal frames so that even when the Player is walking, running, or jumping, the camera converges smoothly to `0.0000` distance from `CalculateCurrentPlayerFollowTarget()` with **zero final-frame position snap**.

