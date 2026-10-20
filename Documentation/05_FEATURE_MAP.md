# Feature Map: Contrast

> **Target Audience:** Developers and AI agents seeking to locate the exact implementation of any gameplay, editor, or system feature, evaluate its impact radius, and follow established validation steps.

---

## Index of Major Features

1. [Color Shifting & Permeability Mechanics](#1-color-shifting--permeability-mechanics)
2. [Custom Non-Physics AABB Collision & Sliding](#2-custom-non-physics-aabb-collision--sliding)
3. [In-Wall Overlap Push-Out & Anti-Stuck System](#3-in-wall-overlap-push-out--anti-stuck-system)
4. [Jump Dynamics & Fall Limiting](#4-jump-dynamics--fall-limiting)
5. [Parametric Moving Platforms (Trajectory Engine)](#5-parametric-moving-platforms-trajectory-engine)
6. [Dynamic Rider-Carry Momentum Transfer](#6-dynamic-rider-carry-momentum-transfer)
7. [Checkpoints, Fall Death, and Spawn Retries](#7-checkpoints-fall-death-and-spawn-retries)
8. [Camera Tracking & Bounding Box Constraints](#8-camera-tracking--bounding-box-constraints)
9. [Runtime Level Editor: Selection, Moving & Resizing](#9-runtime-level-editor-selection-moving--resizing)
10. [Runtime Level Editor: Trajectory Authoring & Visual Handles](#10-runtime-level-editor-trajectory-authoring--visual-handles)
11. [Two-Step Safe Platform Deletion](#11-two-step-safe-platform-deletion)
12. [Direct In-Memory Test Play Bridge](#12-direct-in-memory-test-play-bridge)
13. [Level JSON Serialization (Import & Export)](#13-level-json-serialization-import--export)

---

## 1. Color Shifting & Permeability Mechanics

- **Responsibility:** Classifies platform grayscale values into discrete logical buckets and enforces transparency when player color matches platform color.
- **Entry Point:** [`PlayerController.SetColor()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L211) and [`PlayerController.StepColorChunk()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L181).
- **Key Files & Symbols:**
  - [`Assets/Script/Contrast/Color/LogicalColor.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs): `LogicalColor` enum (`White`, `Gray`, `Black`, `Universal`).
  - [`Assets/Script/Contrast/Color/ColorClassifier.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs): `Classify(float raw)`, `GetPlatformVisualColor(float raw)`.
  - [`Assets/Script/Contrast/Level/ColorPlatform.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs): `Logical`, `IsUniversal`.
- **Data Involved:** Grayscale `float` (0..255), `LogicalColor`.
- **Runtime Flow:** Player input requests color $\to$ `PlayerController.SetColor()` $\to$ `SpriteRenderer.color` updated $\to$ collision resolver checks `platform.Logical != playerColor`.
- **Editor Flow:** `PlatformColorSubComponent` presents dropdown, raw numeric field, and quick buttons to assign platform grayscale value.
- **Dependencies:** Consumed by `PlayerCollisionResolver`, `PlayerOverlapResolver`, `TrajectorySystem.GetRiderDisplacement`.
- **Validation:** Verify in `ContrastTests` and check that player passes through identical colors while resting securely on non-matching colors or Universal (255) platforms.

---

## 2. Custom Non-Physics AABB Collision & Sliding

- **Responsibility:** Resolves player movement deterministically against axis-aligned platform bounding boxes, supporting smooth sliding along horizontal and vertical surfaces without Unity physics jitter.
- **Entry Point:** [`PlayerMovementResolver.Resolve()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs#L29) $\to$ [`PlayerCollisionResolver.Resolve()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs#L31).
- **Key Files & Symbols:**
  - [`Assets/Script/Contrast/Player/Movement/MovementGeometry.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs): `Aabb` struct, `Overlaps()`, `OverlapArea()`.
  - [`Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs): `ResolveHorizontal()`, `ResolveVertical()`, `HasGroundSupport()`.
- **Runtime Flow:** Sequential X-axis resolution clamps candidate X $\to$ Y-axis resolution evaluates vertical motion $\to$ ground support check confirms `isGrounded`.
- **Impact Radius:** Central gameplay loop. Any changes affect jump availability, corner snagging, and wall sliding.
- **Validation:** Run automated tests `EditorLevelTests.RunAllEditorTests()`. Verify wall slide and corner hops.

---

## 3. In-Wall Overlap Push-Out & Anti-Stuck System

- **Responsibility:** Prevents the player from becoming permanently trapped inside solid geometry when color shifts or platform movement induces an unexpected overlap.
- **Entry Point:** [`PlayerOverlapResolver.Evaluate()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs#L59).
- **Key Files & Symbols:**
  - [`Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs): `stuckOverlapPercent` ($50\%$), `GetPushSpeed()`, `GetControlMultiplier()`.
- **Runtime Flow:** If $\text{overlap} \ge 50\%$, player enters `PlayerState.Stuck`. If $< 50\%$, calculates shortest escape vector and injects high-priority push velocity while damping user control by $80\%$.
- **Validation:** Spawn player overlapping a solid platform by $20\%$; verify player smoothly slides outward to safety.

---

## 4. Jump Dynamics & Fall Limiting

- **Responsibility:** Calculates discrete vertical jump velocity and applies constant gravity acceleration.
- **Entry Point:** [`PlayerJumpMotion.RequestJump()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs#L44) and [`GetMoveIntent()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs#L67).
- **Key Files & Symbols:**
  - [`Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs): `jumpHeight` ($3.0\,\text{m}$), `maxFallSpeed` ($16.0\,\text{m/s}$), `rb.gravityScale`.
- **Runtime Flow:** Grounded player presses Space $\to$ initial vertical velocity $v_y = \sqrt{2 g h}$ $\to$ each tick integrates gravity $v_y \mathrel{+}= g \cdot \Delta t$ down to $-16\,\text{m/s}$.
- **Validation:** Verify peak jump height matches $3.0\,\text{m} \pm 0.05\,\text{m}$.

---

## 5. Parametric Moving Platforms (Trajectory Engine)

- **Responsibility:** Evaluates deterministic parametric motion for platforms across Line and Circle curves with configurable playback modes (`PingPong`, `Loop`, `Once`).
- **Entry Point:** [`TrajectorySystem.UpdateAll()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs#L23) $\to$ [`Trajectory.ProcessUpdate()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L106).
- **Key Files & Symbols:**
  - [`Assets/Script/Contrast/Level/Trajectory.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs): `EvaluateWorldPoint()`, `ClipLineAgainstBounds()`, `ProjectPointOntoTrajectory()`.
  - [`Assets/Script/Contrast/Data/TrajectoryData.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs): Serialization DTO.
- **Kinematic Formulas:**
  - Line: $\mathbf{P}(t) = \mathbf{P}_0 + R(\theta) \cdot (u(t) \cdot \text{scaleX}, 0)^T$.
  - Circle: $\mathbf{P}(t) = \mathbf{C} + r \cdot (\cos\alpha(t), \sin\alpha(t))^T$.
- **Validation:** Standalone suite `ContrastTests` (tests `Test_LineForward`, `Test_LineReverse`, `Test_Circle360Loop`, `Test_SpatialBounds`).

---

## 6. Dynamic Rider-Carry Momentum Transfer

- **Responsibility:** Detects if the player's feet rest on a moving platform and transfers the platform's frame displacement to the player's movement pipeline.
- **Entry Point:** [`TrajectorySystem.GetRiderDisplacement()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs#L61).
- **Key Files & Symbols:**
  - [`TrajectorySystem.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs#L61-L114)
  - [`PlayerController.SetRiderDisplacement()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L43)
- **Runtime Flow:** Prior to player resolution, `TrajectorySystem` inspects the platform's pre-displacement top edge ($y_{\text{plat, max}} - \Delta y$). If player rests on it, passes $\Delta \mathbf{x}$ to `PlayerController`.
- **Validation:** Test `Test_RiderCarry_Displacement_Calculation` in `EditorLevelTests.cs`.

---

## 7. Checkpoints, Fall Death, and Spawn Retries

- **Responsibility:** Manages the player death plane, checkpoint updates, and immediate in-memory respawn without level rebuilding.
- **Entry Point:** [`LevelLoader.ProcessPlayerInteractions()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L294) and [`GameManager.Retry()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L202).
- **Key Files:** `LevelLoader.cs`, `LevelMarker.cs`, `GameManager.cs`.
- **Runtime Flow:** Touching a checkpoint stores its position/color in `LevelLoader` $\to$ Death or clicking Retry calls `TrajectorySystem.ResetAll()` and `player.Respawn()`.
- **Validation:** Die in `SampleScene`; press `R`; verify instant respawn at the latest activated checkpoint with all trajectories reset to phase 0.

---

## 8. Camera Tracking & Bounding Box Constraints

- **Responsibility:** Smoothly tracks the player while clamping the viewport within an authored rectangular boundary.
- **Entry Point:** [`CameraFollow.ProcessCameraUpdate()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs#L46).
- **Key Files:** `CameraFollow.cs`, `CameraBoundaryData`.
- **Validation:** Walk player toward the edge of `TrajectoryShowcaseLevel`; camera must stop at boundary edges without showing out-of-bounds space.

---

## 9. Runtime Level Editor: Selection, Moving & Resizing

- **Responsibility:** Provides full authoring capabilities in the running game, including camera panning, object hit-testing, body translation, and 4-handle AABB resizing.
- **Entry Point:** [`LevelEditorController.Update()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L363).
- **Key Files:** `LevelEditorController.cs`, `LevelEditorUI.cs`.
- **UI Guard:** Drag operations are cancelled if pointer enters any UI panel (`EventSystem.current.IsPointerOverGameObject()`).
- **Validation:** Click and drag platform in Edit Mode; verify position and size update live in Inspector.

---

## 10. Runtime Level Editor: Trajectory Authoring & Visual Handles

- **Responsibility:** Visualizes trajectory curves via `LineRenderer` and exposes interactive handles for origin, endpoints, radius, arc angles, and spatial bounds.
- **Entry Point:** [`LevelEditorController.RefreshHandles()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1055).
- **Key Files:** `LevelEditorController.cs`, `LevelEditorUI.cs`.
- **Handles Available:**
  - Yellow: Origin / Center.
  - Cyan: Line endpoint (scale & rotation).
  - Magenta: Circle radius.
  - Lime: Arc start/end angles.
  - Orange: Spatial bounds rectangle edges.
- **Validation:** Enable trajectory on a platform; drag Cyan handle; verify rotation and scale update in Inspector.

---

## 11. Two-Step Safe Platform Deletion

- **Responsibility:** Protects level designers from accidental platform loss via a 3-second two-step confirmation button and guarded keyboard shortcuts (`Delete`, `Backspace`).
- **Entry Point:** [`LevelEditorUI.OnDeleteButtonClicked()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs#L503) $\to$ [`LevelEditorController.DeleteSelectedPlatform()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1280).
- **Key Files:** `LevelEditorUI.cs`, `LevelEditorController.cs`.
- **Validation:** Click DELETE PLATFORM once (turns amber: "CONFIRM DELETE"); wait 3 seconds (reverts to red); click twice within 3s (deletes platform and deregisters from `ColorPlatform.All`).

---

## 12. Direct In-Memory Test Play Bridge

- **Responsibility:** Allows level designers to test newly authored platform timings instantly without exporting to disk and manually reloading.
- **Entry Point:** [`LevelEditorUI`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) **TEST PLAY** $\to$ [`LevelEditorController.RequestPlayLevel()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L242) $\to$ [`GameManager.StartGameWithLevelData()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L127).
- **Validation:** Create moving platform; click **TEST PLAY**; verify immediate gameplay transition with active trajectories.

---

## 13. Level JSON Serialization (Import & Export)

- **Responsibility:** Serializes live scene objects to JSON schema and parses external level files back into runtime objects.
- **Entry Point:** [`LevelEditorController.RequestExport()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L213) and [`RequestImport()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L158).
- **Key Files:** `LevelEditorController.cs`, `LevelLoader.cs`, `LevelData.cs`.
- **Coordinate Contract:** Authoritative platform position on export is evaluated at $t=0$ (or $t=1$ if reversed) via `Trajectory.GetInitialPosition()`.
- **Validation:** Round-trip export and import of `TrajectoryShowcaseLevel.json`; verify zero positional drift.
