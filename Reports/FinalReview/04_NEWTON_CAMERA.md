# 04 — NEWTON RINGS CAMERA TRACKING (`Reports/FinalReview/04_NEWTON_CAMERA.md`)

## 1. Camera Behavior Requirements & HTML Reference

- During active gameplay in the Newton Rings chamber (`L01`, `L02`, `L03`), the camera must follow the Player smoothly across:
  - Chamber entry from the main platforming path,
  - Jumping onto and riding orbital platforms `K` and `I` across full $360^\circ$ orbits (top, bottom, left, right),
  - Airborne jumps between `K` and `I`,
  - Pressing `X` to lock measurements `K`, `I`, and `K'`.
- The camera must **not** lock onto a static chamber overview while the Player is still maneuvering and marking (`K`, `I`, `K'`), and must only transition to `NewtonResultReveal` once all 3 measurements (`K`, `I`, `K'`) are locked.

---

## 2. Root Cause & Fixes (`LevelDataSanitizer.cs`, `CameraFollow.cs`, `EasyNewtonFinaleController.cs`)

1. **`LevelDataSanitizer.CalculateCameraBoundary(LevelData data)` Clamping:**
   - Previously, `CalculateCameraBoundary` only iterated over `data.platforms`, `data.startPos`, `data.checkPoint`, and `data.end`, ignoring `data.newton` and `data.collectibles`.
   - When a level's camera boundary was auto-calculated or repaired (such as during Editor Test Play or level sanitization), the right/top/bottom edges of `cameraBoundary` stopped at the last terrain platform and clamped the camera before the Newton chamber!
   - **Fix:** Updated `LevelDataSanitizer.CalculateCameraBoundary` to expand the bounding box to include `data.newton.center ± 8.5f` (full Newton ring radius + orbital padding) and `data.newton.goalPortal`, as well as all `data.collectibles`.
2. **Smooth Player Tracking in `NewtonPlayerFollow` (`CameraFollow.cs` & `EasyNewtonFinaleController.cs`):**
   - `EasyNewtonFinaleController.UpdateCameraFocus()` enters `CameraFollow.CameraMode.NewtonPlayerFollow` (orthographic size `6.5f`, look-ahead bias `0.15f` toward chamber center `O` clamped to `1.1f` units) as soon as the Player enters the Newton chamber (`px >= activationTriggerX`), keeping the Player comfortably centered throughout full $360^\circ$ orbits on `K` and `I`.
   - Only when `currentStep == SequenceStep.Completed` (all 3 marks `K`, `I`, `K'` recorded) does `EasyNewtonFinaleController` call `targetCamera.EnterNewtonResultReveal(chamberCenter, 9.6f)`.
   - On level restart (`ResetSequenceToStart()` / `Teardown()`), `targetCamera.ExitNewtonModes()` immediately restores `NormalGameplay` camera tracking (`orthoSize = 5f`).

