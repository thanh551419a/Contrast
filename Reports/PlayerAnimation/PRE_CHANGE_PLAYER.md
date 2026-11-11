# PRE-CHANGE PLAYER & COLLIDER AUDIT REPORT

## 1. Current Player Hierarchy & Components

### 1.1 Root Player GameObject (`PlayerController`)
- **Script**: `Assets/Script/Contrast/Player/PlayerController.cs`
- **Required Sibling Components**:
  - `PlayerInput` (`Assets/Script/Contrast/Player/Input/PlayerInput.cs`)
  - `PlayerMovementResolver` (`Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs`)
  - `PlayerCollisionResolver` (`Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs`)
  - `PlayerOverlapResolver` (`Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs`)
  - `PlayerJumpMotion` (`Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`)
  - `Rigidbody2D` (configured with `simulated = false` / zero gravity scale; all gameplay movement is resolved deterministically by `PlayerMovementResolver` and committed via `PlayerController.ApplyMove`).
  - `SpriteRenderer` (`sr` on root `Player` GameObject, disabled by `PlayerVisualPresentation.InitializeVisuals()`).
  - `PlayerVisualPresentation` (`Assets/Script/Contrast/Presentation/PlayerVisualPresentation.cs`).

### 1.2 Child Visual Hierarchy (`PlayerVisualPresentation`)
- `VisualPresentationRoot` (`Transform`, localPosition = `playerOffset = (0, 0, 0)`):
  - `GlowHalo` (`SpriteRenderer`, sortingOrder = `9`, procedural radial glow sprite)
  - `OpticalBody` (`SpriteRenderer`, sortingOrder = `10`, procedural rounded rectangle sprite)
  - `OpticalRim` (`SpriteRenderer`, sortingOrder = `11`, procedural border stroke sprite)
  - `CoreLight` (`SpriteRenderer`, sortingOrder = `12`, procedural core dot sprite)

---

## 2. Authoritative Gameplay Collider Measurements

### 2.1 Collider Type & Geometry Source
CONTRAST uses a deterministic custom world-space AABB (`Contrast.Player.Aabb`) rather than Unity Physics2D colliders for Player-vs-Platform and Player-vs-Marker collisions.
- **Configured in Level JSON (`L01.json`, `L02.json`, `L03.json`)**:
  ```json
  "startPos": {
    "size": {
      "x": 0.59375,
      "y": 1.0
    }
  }
  ```
  This corresponds directly to the HTML reference player hitbox (`38 px × 64 px` at `64 px/unit` $\rightarrow$ `width = 38 / 64 = 0.59375 units`, `height = 64 / 64 = 1.0 unit`).
- **Applied at Level Load (`LevelLoader.BuildRuntimeLevel`, line 390)**:
  ```csharp
  player.SetRuntimeGeometry(startData.size != null ? startData.size.ToVector2() : Vector2.one);
  ```

### 2.2 Exact Pre-Change Collider Metrics
| Metric | Value | Source / Formula |
| :--- | :--- | :--- |
| **Collider Type** | Custom World-Space `Aabb` | `Aabb.FromCenter((Vector2)transform.position + playerOffset, playerSize)` |
| **`playerSize` (World Width × Height)** | `(0.59375, 1.00000)` units | `L01.json`, `L02.json`, `L03.json` (`38 × 64 px / 64`) |
| **`playerOffset` (World Offset)** | `(0.00000, 0.00000)` units | `PlayerController.playerOffset` |
| **Root `transform.localScale`** | `(0.30000, 0.30000, 1.00000)` | `PlayerController.Awake()` line 186 |
| **World-Space Collider Center** | `(transform.position.x, transform.position.y)` | `transform.position + (Vector3)playerOffset` |
| **World-Space Collider Bounds (`Min`..`Max`)** | `[x - 0.296875, x + 0.296875] × [y - 0.50, y + 0.50]` | `width = 0.59375 u`, `height = 1.0 u` |
| **Ground-Contact Feet Y** | `transform.position.y - 0.50000` units | `transform.position.y - playerSize.y * 0.5f` |

---

## 3. Pre-Change Visual Representation & Phase Colors

In `PlayerVisualPresentation.cs`:
- **White Phase (`LogicalColor.White`)**:
  - `BrightBody = #ffffff` (`(1.0, 1.0, 1.0, 1.0)`)
  - `BrightRim = #ffffff` (`(1.0, 1.0, 1.0, 1.0)`)
  - `BrightHalo = rgba(255, 255, 255, 0.38)`
- **Gray Phase (`LogicalColor.Gray`)**:
  - `GrayBody = #858585` (`(0.522, 0.522, 0.522, 1.0)`)
  - `GrayRim = #858585` (`(0.522, 0.522, 0.522, 1.0)`)
- **Black Phase (`LogicalColor.Black`)**:
  - `DarkBody = #050505` (`(0.020, 0.020, 0.020, 1.0)`)
  - `DarkStrokeRim = #74818b` (`(0.455, 0.506, 0.545, 1.0)`) — provides contrast outline against dark backgrounds.

### 3.1 Existing Automated Test Expectations on Player Visuals
In `Assets/Editor/EditorLevelTests.cs` (`Test_FinalReview_Tasks1To8_Suite`, lines `2168–2196`):
- Tests inspect `pcGo.transform.Find("VisualPresentationRoot/OpticalBody")` and `pcGo.transform.Find("VisualPresentationRoot/OpticalRim")` as `SpriteRenderer` components and verify their phase colors (`White`, `Gray`, `Black` body + `#74818b` rim).
- Therefore, when replacing the static procedural rectangle with the animated `SilhouetteMotion` character in `PlayerVisualPresentation`, driving the animated character sprite on `OpticalBody` (and a slightly dilated rim/outline silhouette on `OpticalRim` for dark-phase contrast) with an `Animator` (`applyRootMotion = false`) and `MotionLibrary` frame sampler will simultaneously:
  1. Render the animated `SilhouetteMotion` character for `Idle`, `Run`, `Jump`, `Fall`, `Landing`, `Dead`, and `Respawn`.
  2. Guarantee high-contrast legibility in all three optical phases (`White`, `Gray`, `Black` with `#74818b` rim).
  3. Preserve 100% compatibility with existing visual presentation regression tests.

