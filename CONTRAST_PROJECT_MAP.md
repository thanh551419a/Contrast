# Contrast — Project Map for AI-Assisted Changes

> **Purpose:** Give another AI a grounded map of the supplied project scripts before it modifies the runtime level editor. This document describes the files present in `Script.zip`; it does not claim to describe Unity scenes, prefabs, packages, or Project Settings that were not included in the archive.
>
> **Reviewed scope:** `Script/Contrast/**` C# scripts, the level-editor setup note, and sample level JSON files from the supplied `Script.zip`.

## 1. Project overview

**Contrast** is a Unity 2D grayscale side-scroller with a runtime level editor. The editor places, selects, moves, resizes, imports, and exports platforms and level markers. Level geometry and player-platform collision are primarily implemented with custom AABB code; do not assume Unity `Collider2D`/`Physics2D` is the authoritative gameplay collision system.

The requested feature work is to extend the existing platform data/runtime/editor with a reusable `Trajectory` component for moving platforms, serialize its configuration in level JSON, expose simple editing controls/handles in the existing editor, and add a way to delete the selected platform.

### Existing design facts that must be preserved

- `GameManager` coordinates game state and owns the gameplay update path. While playing, it calls player movement, level-marker interactions, and camera update in a defined order.
- `LevelLoader` parses `LevelData`, clears the previous runtime map, creates platforms/markers, configures the camera boundary, and initializes the player's spawn state.
- `ColorPlatform` stores platform geometry/color, registers instances in the static `ColorPlatform.All` list, and exposes `GetAabb()` for gameplay/editor geometry.
- `PlayerMovementResolver`, `PlayerCollisionResolver`, and `PlayerOverlapResolver` perform movement/collision using platform AABBs and `ColorPlatform.All`. Moving platforms must remain visible to this system.
- `LevelEditorController` handles editor state, map input, platform/marker selection, drag/resize, palette placement, JSON import/export, and selection handles.
- `LevelEditorUI` creates the editor UI in code using `UnityEngine.UI`; it is not built from a scene-authored panel layout in the supplied files.
- Existing level JSON that has no trajectory field must continue to load as stationary platforms.
- Do not rotate a platform GameObject merely to rotate its path. Platform gameplay geometry is axis-aligned, and path orientation should be evaluated mathematically while the platform itself retains its existing visual/geometry conventions.

## 2. Runtime flow and dependency map

```text
MainMenuUI
  ├─ PLAY GAME ───────────> GameManager.StartGame()
  │                           └─ LevelLoader.LoadAndInitializeLevel()
  │                                ├─ Load JSON -> LevelData
  │                                ├─ BuildRuntimeLevel()
  │                                │    ├─ PlatformsRoot / ColorPlatform instances
  │                                │    ├─ LevelObjectsRoot / LevelMarker instances
  │                                │    ├─ camera boundary
  │                                │    └─ initial player spawn/color
  │                                └─ CurrentLevelData
  │
  └─ EDIT LEVEL ──────────> GameManager.OpenEditor()
                              ├─ LevelEditorController.StartEditing()
                              └─ LevelEditorUI.Show(true)
                                   ├─ top bar: IMPORT / EXPORT / BACK
                                   ├─ left Inspector: selected platform fields
                                   └─ right Palette: PLATFORM / START POS /
                                                     CHECKPOINT / END

GameManager.Update() while Playing
  ├─ PlayerController.ProcessUpdate(deltaTime)
  │    └─ input + jump intent
  │         -> PlayerMovementResolver.Resolve(...)
  │              ├─ PlayerOverlapResolver
  │              └─ PlayerCollisionResolver
  │                    └─ ColorPlatform.All + ColorPlatform.GetAabb()
  ├─ LevelLoader.ProcessPlayerInteractions(player)
  └─ CameraFollow.ProcessCameraUpdate()
```

**Integration implication:** If trajectory motion is added, decide explicitly where it advances in the existing gameplay update order. It must advance only during the intended gameplay state (not while the level editor is open, in the main menu, or after game over). Move platforms before the player collision query for that frame so AABBs are current. Verify whether a player standing on a platform should be carried by platform displacement; ordinary platform-game behavior normally expects this, but the current code does not expose an obvious persistent “ground platform” reference in the supplied map.

## 3. File-by-file map

Paths below are relative to the archive root and should be matched to their actual paths in the target Unity project before edits.

### Core and game state

| File | Responsibility | Relevant relationships / cautions |
|---|---|---|
| `Script/Contrast/Core/GameManager.cs` | Singleton coordinator for menu, playing, editing, won/game-over state. Creates/finds UI/editor/level dependencies. Owns the gameplay `Update()` path. | Calls `PlayerController.ProcessUpdate`, then `LevelLoader.ProcessPlayerInteractions`, then camera update. Integrate trajectory progression deliberately here or through a clearly called system; avoid a second independent gameplay update pipeline. |
| `Script/Contrast/Core/GameState.cs` | Enum for overall game flow states. | Check existing state values before adding state-dependent behavior; trajectory motion should not run in Edit/MainMenu/GameOver/Won unless explicitly designed. |

### Level data, platform objects and loading

| File | Responsibility | Relevant relationships / cautions |
|---|---|---|
| `Script/Contrast/Data/LevelData.cs` | Serializable JSON model: `LevelData`, `StartPosData`, `CheckPointData`, `EndData`, legacy `SpawnPointData`, `ColorPlatformData`, `CameraBoundaryData`, `Vector2Data`. | `ColorPlatformData` currently contains `position`, `size`, `rawGrayscaleColor` only. Add an optional serializable trajectory data field here or in a small adjacent data file. Missing trajectory data must mean “stationary” for backward compatibility. |
| `Script/Contrast/Level/ColorPlatform.cs` | Runtime platform component; initializes visuals and geometry, exposes `Position`, `Size`, `RawGrayscaleColor`, `GetAabb()`, and static registry `ColorPlatform.All`. | `Initialize()` sets authored position/size/color and ensures registration. `GetAabb()` is the geometry contract consumed by editor selection and custom gameplay collision. Keep the platform AABB axis-aligned. There is no visible `OnDestroy()` registry cleanup in this supplied version; deletion should remove from the registry and prevent stale/null entries. |
| `Script/Contrast/Level/LevelLoader.cs` | Reads JSON, creates runtime level instances, clears old objects/registries, configures camera boundary and current spawn, processes checkpoint/end interaction, retries spawn. | `BuildRuntimeLevel()` instantiates or creates `ColorPlatform`, then calls `Initialize()`. Add trajectory initialization here after platform data is available. `ClearRuntimeLevel()` clears registries and destroys children. Maintain `CurrentLevelData` and old JSON compatibility. |
| `Script/Contrast/Level/LevelMarker.cs` | Represents StartPos, CheckPoint, End; keeps static `LevelMarker.All`, size/color/type, AABB and one-shot interaction behavior. | Keep platform deletion separate from marker deletion. Current JSON allows one StartPos, one End and multiple checkpoints. |
| `Script/Contrast/Level/LEVEL_EDITOR_SETUP.md` | Existing concise notes on editor flow, controls, JSON import/export and UI-panel drag cancellation. | Update this file only if editor interaction instructions change materially; it is useful but does not replace source inspection. |
| `Script/Contrast/Level/SampleLevel.CheckpointEnd.json` | Example schema with start, checkpoint, end, camera boundary, and empty platform list. | Existing example does not have trajectory data. |
| `Script/Contrast/Level/SampleLevel.CheckpointEnd.Test.json` | Additional checkpoint/end test level. | Inspect before changing schema or test assumptions. |
| `Script/Contrast/Level/SampleLevel.Easy.HTMLReconstructed.json` | Reconstructed Easy level data containing many platforms. | Use as a backwards-compatibility import/export test: all legacy static platforms must remain static and preserve geometry/color. |

### Runtime level editor

| File | Responsibility | Relevant relationships / cautions |
|---|---|---|
| `Script/Contrast/Level/LevelEditorController.cs` | Owns edit mode, palette drag/drop, import/export, selection, map pan, platform body dragging, four resize handles, marker dragging, hit-testing, and `BuildExportData()`. | `CreateFromPalette()` creates platforms/markers. `SelectPlatform()` updates the UI. `ApplySelectedPlatformFromUI()` updates platform data. `BuildExportData()` iterates `ColorPlatform.All` and currently writes position/size/color. Add/delete/trajectory edits must update selection, handles, registry, and export data consistently. Middle mouse pans the camera; pointer over UI blocks/cancels canvas drag. |
| `Script/Contrast/UI/LevelEditorUI.cs` | Dynamically creates Canvas, top toolbar, left Inspector, right Palette, input fields, buttons, status text, and an EventSystem if needed. | Left Inspector currently edits Position X/Y, Size X/Y and grayscale Color. Top bar has Import, Export, Back. Right Palette creates Platform, Start Pos, Checkpoint, End. It currently has no delete button or trajectory controls. New controls must fit the generated UI; avoid absolute-position overflow by using a scrollable/sectioned Inspector if needed. |
| `Script/Contrast/UI/LevelEditorPaletteItem.cs` | Pointer-down/up handlers for dragging palette entries to the map. | Forwards to `LevelEditorController.BeginPaletteDrag()` and `EndPaletteDrag()`. Do not break drag/drop or let palette pointer events accidentally begin platform movement. |

### Player, collision and geometry

| File | Responsibility | Relevant relationships / cautions |
|---|---|---|
| `Script/Contrast/Player/PlayerController.cs` | Player's single `ProcessUpdate()` entry, reads input/jump intent, asks movement resolver for final movement, applies position, updates grounded/state/color/death. | `GameManager` calls it. It uses a custom geometry pipeline; do not let trajectory work create a separate player movement path. |
| `Script/Contrast/Player/Input/PlayerInput.cs` | Converts current input into movement/color/jump intents. | Supports conditional input backends through compile symbols; unrelated to trajectory unless platform carry requires movement input interaction. |
| `Script/Contrast/Player/Movement/MovementGeometry.cs` | Defines custom `Aabb` and operations such as center/size/overlap. | Shared geometry primitive for player and markers; platform `GetAabb()` is adapted into this representation by resolver code. |
| `Script/Contrast/Player/Movement/PlayerMovementResolver.cs` | Combines input, jump and overlap-push intents, delegates collision, returns one final movement result. | Does not itself change player transform. Do not bypass its result contract. |
| `Script/Contrast/Player/Movement/PlayerCollisionResolver.cs` | Predictive AABB collision with platforms, horizontal then vertical resolution and ground support checks. | Iterates `ColorPlatform.All` and calls platform AABBs. Must continue to treat moving platforms as solid according to existing logical-color collision rules. Check moving-platform carry/relative motion before claiming platform gameplay is complete. |
| `Script/Contrast/Player/Movement/PlayerOverlapResolver.cs` | Checks overlap with color platforms and decides push-out/control reduction. | Also reads `ColorPlatform.All` and current AABBs; moving platform positions must be current before this resolver executes. |
| `Script/Contrast/Player/Movement/PlayerJumpMotion.cs` | Computes jump/vertical movement intent and grounded response. | Player position changes remain centralized in `PlayerController.ApplyMove()`. |
| `Script/Contrast/Player/PlayerState.cs` | Enum describing player state. | Check before modifying state behavior. |

### Camera, color and UI

| File | Responsibility | Relevant relationships / cautions |
|---|---|---|
| `Script/Contrast/Camera/CameraFollow.cs` | Follows a target and clamps camera position to a level boundary; exposes boundary setters. | Editor middle-mouse panning changes camera transform directly and must not change platform/path world coordinates. |
| `Script/Contrast/Color/LogicalColor.cs` | `Black`, `Gray`, `White` enum. | Logical color controls whether the platform blocks a player. |
| `Script/Contrast/Color/ColorClassifier.cs` | Maps raw grayscale values to logical color and visual value. | Platform color input is currently raw grayscale in the 0–255 range. Avoid changing this in trajectory work. |
| `Script/Contrast/UI/MainMenuUI.cs` | Builds runtime main-menu UI and invokes GameManager actions. | Opens gameplay or editor through GameManager. |
| `Script/Contrast/UI/GameControlUI.cs` | Builds gameplay controls including restart/retry. | Uses GameManager/LevelLoader flows. |
| `Script/Contrast/UI/GameOverui.cs` | File name is lower-case `ui`, class is `GameOverUI`; displays win/game-over and retry/restart controls. | Keep the existing class/file identity unless fixing it is required. Avoid unrelated rename churn. |

## 4. Existing editor interaction contract

- `GameManager.OpenEditor()` sets state to Editing, disables the player and starts editor UI/controller.
- `LevelEditorController.Update()` processes mouse input only while editing.
- Left-click on a platform selects it; dragging the body moves it; four handles resize its AABB-based rectangle.
- Left-click/drag on a marker selects and moves the marker body.
- Middle mouse over the map pans the camera; it must not mutate platform/map coordinates.
- `IsPointerOverUi()` prevents a click on a UI panel from beginning a map operation. If an active map drag crosses onto UI, the drag is cancelled. Preserve these guards when adding controls or trajectory handles.
- The palette uses pointer down/up and only creates an object when released over the map rather than the UI.
- Import calls `LevelLoader.BuildRuntimeLevel(data)` after validation, which clears/rebuilds the current map.
- Export creates a `LevelData`, preserving non-editor fields such as camera boundary from `CurrentLevelData`, then regenerates platform and marker collections from the live registries.

## 5. Current serialized platform shape

Current platform JSON objects look like:

```json
{
  "position": { "x": 2.0, "y": 1.0 },
  "size": { "x": 3.0, "y": 0.5 },
  "rawGrayscaleColor": 255.0
}
```

A trajectory extension should be optional and versionable, for example:

```json
{
  "position": { "x": 2.0, "y": 1.0 },
  "size": { "x": 3.0, "y": 0.5 },
  "rawGrayscaleColor": 255.0,
  "trajectory": {
    "enabled": false,
    "curveType": "Line",
    "domainMode": "ParameterRange",
    "playbackMode": "PingPong"
  }
}
```

This is an **illustrative shape only**, not a claim that Unity `JsonUtility` serializes enum strings by default. Confirm actual serialization choices in the implementation. Keep schema fields serializable by Unity; do not rely on unsupported `Dictionary` serialization or unguarded polymorphic object graphs. A missing/null trajectory must default to stationary behavior.

## 6. Trajectory feature target

### Architecture principle

One component named `Trajectory` can be attached to a `ColorPlatform`, but curve shape, domain and playback remain separate concepts:

- **Curve definition:** e.g. line or circle/ellipse; future options may include Bezier/spline/composite curves.
- **Transform:** origin/offset, rotation, scale. These should use a clearly documented local/world coordinate convention.
- **Domain:** `Unbounded`, `ParameterRange`, or `SpatialBounds` as appropriate. A line can be rotated and clipped against X/Y bounds. A circle is parameterized by angle; a full circle and an arc use the same curve definition with different angle intervals.
- **Playback:** `Loop`, `PingPong`, `Once`; finite `Duration` or unbounded `Speed` as appropriate.
- **Progress:** use a bounded normalized phase internally instead of a global parameter that increases forever. `Loop` wraps phase; `PingPong` maps forward phase to a forward-and-back parameter. Do not assume `Bounded` automatically means `Periodic`; validate continuity at loop seams.

### First supported configurations

1. **Line:** a normalized local line such as `C(u) = (u, 0)`, transformed by origin/rotation/scale. Support a finite parameter interval and, where reasonable, clipping against an axis-aligned X/Y rectangle. Use PingPong by default for finite motion.
2. **Circle/full loop:** a normalized circle `C(theta) = (cos(theta), sin(theta))`, scaled to the required radius, with a full angular interval and Loop playback by default.
3. **Arc/half-circle:** reuse the circle evaluator and restrict the angular interval (e.g. 180 degrees). Use PingPong by default for an open arc, so it reaches the end and returns along the same arc without teleporting.

Horizontal and vertical movement are configurations of a line, not separate curve types. A half-circle is not a separate equation type; it is a circle domain/preset. Keep curve-specific scalar parameters to 0–3 where practical; variable numbers of Bezier/spline handles must be represented as collections of handles/nodes rather than many fixed fields.

### Coordinate and lifecycle constraints

- Define an explicit contract between serialized `ColorPlatformData.position` (authored/start position), trajectory origin/center, and the initial evaluated point. Starting playback must not teleport a platform unexpectedly. If editing center/radius/angular limits changes the start point, update the authored start position intentionally or clearly preserve the authored point; document and test the selected rule.
- Runtime movement must update the same Transform/Position consumed by `ColorPlatform.GetAabb()` and existing collision resolvers.
- Do not rotate the platform visual/AABB to orient the trajectory. Apply the rotation to the trajectory equation.
- Editor preview/handles must edit serialized configuration, not spawn a second hidden gameplay object or have a separate movement formula from runtime.
- Only advance trajectories during Playing. When a level is rebuilt/restarted/imported, reset phase and base transform deterministically.
- Test whether the player should inherit moving-platform displacement when standing on it. If implementing rider carry, integrate minimally with the existing custom AABB pipeline; do not introduce `Physics2D` collision as a substitute.

## 7. Platform deletion target

Deletion must apply only to a selected `ColorPlatform`, not `LevelMarker` objects.

Expected behavior:

1. Add a visible `DELETE PLATFORM` / `Delete Selected Platform` control to the left Inspector. It should be disabled or unavailable when no platform is selected.
2. Deleting removes/deregisters the object from `ColorPlatform.All`, destroys its GameObject, clears selected references, hides resize/trajectory handles, clears inspector fields, and updates the status label.
3. A later EXPORT must not include a deleted platform; there must be no stale/destroyed registry entries or null-reference exceptions.
4. Do not delete a marker if one is selected. Do not delete when the user is typing in a UI input field. An optional Delete/Backspace keyboard shortcut is acceptable only if guarded against UI text editing and documented.
5. Keep import/rebuild/clear behavior reliable after deletion.

## 8. Scope boundaries and unknowns

The supplied archive contains scripts and sample JSON, but not `.unity` scenes, prefabs, `Packages/manifest.json`, ProjectSettings, input settings, or the complete Unity project. Therefore, this map does **not** certify actual scene hierarchy assignments, Unity version, compile status, or runtime behavior. The implementation agent must inspect the full target project before editing and must not invent scene/prefab references.

Avoid unrelated refactors to player input, grayscale classification, menu/game-over behavior, camera following, or the existing custom collision pipeline. Prefer small, testable changes with explicit logs only where useful. Update the editor setup note if the controls change.
