# CHECKPOINT IMPLEMENTATION REPORT (L02 PRE-NEWTON CHECKPOINT & INVISIBLE CHECKPOINTS)

## 1. Pre-Newton Platform Identification in `L02.json`

By inspecting `Assets/Resources/Levels/L02.json` (`platforms` array and `newton` chamber definition):
- **Newton Chamber Center (`O`)**: `cx = 5110.0 px` (`79.84375 units`), `cy = 528.0 px` (`-8.25 units`), `k = 4`, `i = 2`.
- **Outer Orbit `K` Left Edge**: `79.84375 - 3.4375 * sqrt(4) = 72.96875 units`.
- **Platform Immediately Before Newton Ring (Index `13`)**:
  ```json
  {
    "position": {
      "x": 67.617188,
      "y": -9.640625
    },
    "size": {
      "x": 1.953125,
      "y": 0.8125
    },
    "rawGrayscaleColor": 255.0
  }
  ```
  - **World-Space AABB**:
    - `minX = 67.617188 - 1.953125 * 0.5 = 66.6406255`
    - `maxX = 67.617188 + 1.953125 * 0.5 = 68.5937505`
    - `minY = -9.640625 - 0.8125 * 0.5 = -10.046875`
    - `maxY (top surface) = -9.640625 + 0.8125 * 0.5 = -9.234375`
  - **Surface Color**: `255.0` (Universal Fixed platform — solid for all Player optical phases: White, Gray, and Black).

---

## 2. New `L02` Pre-Newton Checkpoint Specification (Task C)

Added as the 5th entry (`index 4`) in `checkPoint` inside `Assets/Resources/Levels/L02.json` (and synchronized to `Release/Levels/L02.json` and `Release/Levels/Level_02.json`):
```json
{
  "position": {
    "x": 67.617188,
    "y": -8.734375
  },
  "size": {
    "x": 1.5625,
    "y": 1.640625
  },
  "color": 127.0
}
```

### Exact Coordinates & Respawn Verification
- **Checkpoint Center (`position`)**: `(67.617188, -8.734375)`
- **Checkpoint Trigger Size (`size`)**: `(1.5625, 1.640625)` (`x ∈ [66.835938, 68.398438]`, `y ∈ [-9.5546875, -7.9140625]`)
- **Saved Respawn Location (`LevelLoader.CurrentSpawnPosition`)**: `(67.617188, -8.734375)`
- **Saved Respawn Color (`LevelLoader.CurrentSpawnColor`)**: `LogicalColor.Gray` (`127.0`)
- **Player Grounded Contact Upon Respawn**:
  With `playerSize = (0.59375, 1.0)`, the respawned Player's feet are at `y = -8.734375 - 0.500000 = -9.234375`, which rests **flush (`0.000000` gap)** on the top surface (`maxY = -9.234375`) of Universal platform `13`. `PlayerController.RefreshGroundedStateImmediate()` immediately establishes `IsGrounded = true` on respawn.
- **Separation from Newton Target Hitboxes**:
  Checkpoint right edge is `68.398438`, while Newton outer ring `K` left edge is `72.96875` (`> 4.57` units separation).
- **Decoupled from Camera Control**:
  Checkpoint activation only updates `CurrentSpawnPosition` and `CurrentSpawnColor` in `LevelLoader.ProcessPlayerInteractions`. It does **not** trigger or control the Newton camera transition.

---

## 3. Invisible Checkpoints During Gameplay (Task D)

1. **`LevelMarker.Initialize` (`Assets/Script/Contrast/Level/LevelMarker.cs`)**:
   - When `type == LevelMarkerType.CheckPoint`, sets `spriteRenderer.enabled = false`.
   - The `CheckPoint` `GameObject` itself remains active (`gameObject.activeSelf == true`) until touched/consumed, so `LevelMarker.Overlaps(Aabb playerBounds)` continues to evaluate AABB contact normally.
   - `OnDrawGizmosSelected()` remains intact for Editor-only inspection.
2. **`LevelMarkerVisualPresentation` (`Assets/Script/Contrast/Presentation/LevelMarkerVisualPresentation.cs`)**:
   - When `marker.Type == LevelMarkerType.CheckPoint`, `HideCheckpointRenderers()` disables `rawSpriteRenderer`, `baseRenderer`, `glowRenderer`, `beamRenderer`, and all child `SpriteRenderer` components.
   - Applies uniformly to all checkpoints across `L01` (5 checkpoints), `L02` (5 checkpoints), and `L03` (8 checkpoints).

