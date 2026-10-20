# Contrast Codebase Architecture — Current State Analysis

> **Document Type:** Architectural Reverse Engineering & Codebase Diagnostic  
> **Target Project:** Contrast (Unity 2D Custom-Physics Platformer & Level Editor)  
> **Target Audience:** Software Architects, Lead Developers, Refactoring Council  
> **Status:** Current State Baseline (As-Is Codebase)  
> **Scope:** Analysis only — no source files modified.

---

# 1. Architectural Taxonomy & Concept Separation

To establish an objective architectural baseline, all components in the project are classified against nine core architectural roles:

1. **Data**: Pure state/information containers with no side effects or external system calls.
2. **Domain/Game Logic**: Game rules, polarity logic, win/loss rules, trajectory mathematics.
3. **System**: Stateless or registered processors operating on domain data according to rules.
4. **Controller**: Translators and coordinators of user input, actions, or editing intents.
5. **Manager**: Central orchestrators of global lifecycle, game states, and inter-system flow.
6. **View/UI**: Visual presentation, widget rendering, and user event capture.
7. **Persistence**: File I/O, schema definitions, serialization, deserialization, backward compatibility.
8. **Editor Tooling**: Edit-mode manipulation, handle gizmos, scene interaction, audit tools.
9. **Mathematical/Utility Logic**: Reusable geometry, clamping, projections, and clipping calculations.

---

# 2. File-by-File Documentation

The following master table documents every source file in the project, its architectural category, primary and secondary responsibilities, dependencies, callers, state owned, and architectural risk rating.

*Risk Ratings:*
- **Low**: Cleanly focused responsibility, low coupling.
- **Medium**: Carries secondary responsibilities or participates in static registries.
- **High**: Carries multiple conflicting responsibilities, acts as a bottleneck, or introduces severe coupling.

| File | Category | Primary Responsibility | Secondary Responsibilities | Dependencies | Called By | State Owned | Risk |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| [GameState.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameState.cs) | Data | Defines game lifecycle states enum | None | None | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | None (Enum) | **Low** |
| [GameManager.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | Manager | Central orchestrator for game state, UI visibility, and update loop | Direct restart input polling, rider query routing, fallback component instantiation | [CameraFollow](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs), UI classes | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), UI buttons | Singleton `Instance`, `State` | **High** |
| [LogicalColor.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | Data | Defines gameplay color polarity enum | None | None | Ubiquitous across Player, Level, UI, Data | None (Enum) | **Low** |
| [ColorClassifier.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs) | Domain / Math / View | Maps raw float (0..255) to `LogicalColor` | Provides presentation RGBA colors (`GetPlatformVisualColor`), type string names | UnityEngine.Color | [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [PlatformColorSubComponent](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs) | None (static utility) | **Medium** |
| [PlayerState.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerState.cs) | Data | Defines player locomotion state enum | None | None | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | None (Enum) | **Low** |
| [LevelData.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs) | Data / Persistence | Serializable JSON data contracts for level definitions | Legacy schema fallback fields (`SpawnPointData`) | None | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | Schema definition | **Low** |
| [TrajectoryData.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs) | Data / Persistence | Serializable trajectory parameters and domain enums | Default factory (`CreateDefault`), deep clone (`Clone`) | None | [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | Trajectory configuration | **Low** |
| [PlayerInput.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs) | Controller / Input | Hardware input polling (Keyboard / InputSystem) | Produces immutable `PlayerInputIntent` | UnityEngine.InputSystem | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | Debug log flag | **Low** |
| [MovementGeometry.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs) | Math / Utility | Immutable `Aabb` struct and intersection calculations | Center offset transformations, overlap area | UnityEngine.Vector2, Mathf | [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs), [PlayerOverlapResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs), [PlayerCollisionResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs), [LevelMarker](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs) | None (readonly struct) | **Low** |
| [PlayerJumpMotion.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs) | Domain / Simulation | Vertical ballistic trajectory, jump impulse, gravity integration | Disables Rigidbody2D simulation, uses rb only as state container | Rigidbody2D, Physics2D.gravity | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | `jumping`, velocity state | **Medium** |
| [PlayerOverlapResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs) | System / Domain | Evaluates current AABB penetrations against blocking platforms | Calculates escape vector, push-out velocity, stuck condition | [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs), [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | Overlap tuning parameters | **Medium** |
| [PlayerCollisionResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs) | System / Domain | Predictive swept-AABB collision resolution (X then Y axes) | Ground contact detection, escape penetration bypassing | [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs), [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | Collision and ground epsilons | **Medium** |
| [PlayerMovementResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | Controller / System | Coordinates overlap evaluation, intent composition, and collision prediction | Computes composite intent vector (`input + jump + push + rider`) | [PlayerOverlapResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs), [PlayerCollisionResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs) | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | Cached component references | **Low** |
| [PlayerController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | Controller / Domain / View | Top-level player facade; applies final transform movement | Direct SpriteRenderer management, color step logic, death polling against `KillY` | [PlayerInput](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs), [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs), [PlayerJumpMotion](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs), [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | Player position, `State`, `CurrentColor`, `riderDisplacement` | **High** |
| [CameraFollow.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs) | View / Controller | Orthographic camera tracking restricted by bounding rectangle | Handles orthographic and perspective aspect calculations | UnityEngine.Camera | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | `target`, `boundaryCenter`, `boundarySize` | **Low** |
| [ColorPlatform.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | Domain / View / Global State | Represents physical platform AABB and color polarity | Global static registry (`All`), procedural sprite/texture generation, lazy Trajectory creation, dual runtime/editor setup | [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs), [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs) | Ubiquitous across Movement, Resolvers, Trajectory, Editor, Loader | Static `All`, `size`, `rawGrayscaleColor`, `Logical`, `AuthoredPosition` | **High** |
| [LevelMarker.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs) | Domain / View / Global State | Authoritative AABB volume for StartPos, CheckPoint, and End | Global static registry (`All`), self-destruction on contact, visual tinting | [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs) | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | Static `All`, `markerType`, `size`, `rawColor`, `consumed` | **Medium** |
| [Trajectory.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs) | Domain / Math / View | Evaluates parametric curve paths (Line, Circle) and bounds clipping | Frame displacement calculation, point projection, gizmo drawing, validation | [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs) | [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | `phase`, `pingPongDir`, `FrameDisplacement`, `previousWorldPosition` | **Medium** |
| [TrajectorySystem.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) | System | Orchestrates frame updates across all active platform trajectories | Queries rider-carry displacement for grounded players | [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | None (static coordinator) | **Medium** |
| [LevelLoader.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | Manager / Persistence / Gameplay | Reads level JSON, instantiates runtime hierarchies, computes `KillY` | Owns checkpoint retry state, runs checkpoint/end player interaction checks, triggers game win | [LevelData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [LevelMarker](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs), [CameraFollow](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs), [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | `CurrentLevelData`, `CurrentSpawnPosition`, `CurrentSpawnColor`, `KillY` | **High** |
| [LevelEditorController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | Controller / Editor / Persistence | In-game level editor interaction, selection, and handle manipulation | Direct JSON import/export, file dialogs, line renderers for trajectories, camera panning, direct platform spawning/deletion | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [LevelMarker](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs), [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs), [LevelEditorPaletteItem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorPaletteItem.cs) | Active selections, drag states, handle GameObjects, panning state | **High** |
| [LevelEditorPaletteItem.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorPaletteItem.cs) | View / UI | Captures pointer drag on palette items | Forwards events to `LevelEditorController` | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | Unity EventSystem | Drag configuration | **Low** |
| [PlatformColorSubComponent.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs) | View / Controller | Dedicated UI widget group for editing platform color/type | Quick-preset color buttons, hex/swatch preview, dropdown and raw numeric synchronization | [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs), [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) | `currentRawColor`, UI widget references | **Low** |
| [LevelEditorUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) | View / UI | Displays inspector, palette, trajectory settings, and status | Procedural widget layout engine, domain data construction (`BuildTrajectoryDataFromUI`), 2-step delete confirmation timer | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [PlatformColorSubComponent](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | Procedural UI hierarchy, delete confirmation timer, input field cache | **High** |
| [MainMenuUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/MainMenuUI.cs) | View / UI | Procedural main menu canvas with Play and Edit buttons | Direct calls to `GameManager.Instance` | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | Panel GameObject reference | **Low** |
| [GameControlUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameControlUI.cs) | View / UI | Procedural in-game overlay with Restart and Retry buttons | Direct calls to `GameManager.Instance` | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | Panel GameObject reference | **Low** |
| [GameOverui.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameOverui.cs) | View / UI | Procedural Game Over / Win overlay with Restart / Retry buttons | Direct calls to `GameManager.Instance` | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | Title text, retry button reference | **Low** |
| [PlayerPlatformAuditor.cs](file:///y:/Contrast/My%20project/Assets/Editor/PlayerPlatformAuditor.cs) | Editor Tooling | Scene inspection and audit script executing on load | Logs transform, collider, and sprite properties to console | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | Unity Editor (`[InitializeOnLoadMethod]`) | None (static tool) | **Low** |
| [EditorLevelTests.cs](file:///y:/Contrast/My%20project/Assets/Editor/EditorLevelTests.cs) | Tests | Automated unit/integration test runner running in Editor | Validates trajectory math, clipping, rider-carry, and persistence | [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs), [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) | Unity Editor (`[InitializeOnLoadMethod]`) | Test count counters | **Low** |
| [Tests/ContrastTests/Program.cs](file:///y:/Contrast/My%20project/Tests/ContrastTests/Program.cs) | Tests | Standalone C# console application test harness | Headless validation of curves, domains, clipping, and JSON serialization | [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs), [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs) | CLI / Standalone Runner | Pass/Fail counts | **Low** |

---

# 3. Responsibility Heatmap

This matrix visually highlights files containing excessive architectural roles. High counts (≥ 4) indicate significant architectural tension and refactoring pressure.

| File | Data | Domain Logic | System Logic | Controller | Manager | UI/View | Persistence | Editor | Math | Total Roles |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| [GameState.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameState.cs) | ✓ | | | | | | | | | 1 |
| [GameManager.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | | | | **6** |
| [LogicalColor.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | ✓ | | | | | | | | | 1 |
| [ColorClassifier.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs) | | ✓ | | | | ✓ | | | ✓ | 3 |
| [PlayerState.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerState.cs) | ✓ | | | | | | | | | 1 |
| [LevelData.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs) | ✓ | | | | | | ✓ | | | 2 |
| [TrajectoryData.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs) | ✓ | | | | | | ✓ | | | 2 |
| [PlayerInput.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs) | ✓ | | | ✓ | | | | | | 2 |
| [MovementGeometry.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs) | ✓ | | | | | | | | ✓ | 2 |
| [PlayerJumpMotion.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs) | ✓ | ✓ | ✓ | | | | | | | 3 |
| [PlayerOverlapResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs) | | ✓ | ✓ | | | | | | | 2 |
| [PlayerCollisionResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs) | | ✓ | ✓ | | | | | | | 2 |
| [PlayerMovementResolver.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | ✓ | | ✓ | ✓ | | | | | | 3 |
| [PlayerController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | ✓ | ✓ | | ✓ | | ✓ | | | | **4** |
| [CameraFollow.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs) | | | | ✓ | | ✓ | | | ✓ | 3 |
| [ColorPlatform.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | ✓ | ✓ | ✓ | | | ✓ | | ✓ | | **5** |
| [LevelMarker.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs) | ✓ | ✓ | ✓ | | | ✓ | | ✓ | | **5** |
| [Trajectory.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs) | ✓ | ✓ | ✓ | | | ✓ | | ✓ | ✓ | **6** |
| [TrajectorySystem.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) | | ✓ | ✓ | | | | | | ✓ | 3 |
| [LevelLoader.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | ✓ | ✓ | ✓ | | ✓ | | ✓ | | | **5** |
| [LevelEditorController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | **9** |
| [LevelEditorPaletteItem.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorPaletteItem.cs) | | | | | | ✓ | | | | 1 |
| [PlatformColorSubComponent.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs) | | | | ✓ | | ✓ | | | | 2 |
| [LevelEditorUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) | ✓ | | | ✓ | | ✓ | ✓ | ✓ | | **5** |
| [MainMenuUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/MainMenuUI.cs) | | | | | | ✓ | | | | 1 |
| [GameControlUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameControlUI.cs) | | | | | | ✓ | | | | 1 |
| [GameOverui.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameOverui.cs) | | | | | | ✓ | | | | 1 |

---

# 4. Detailed Analysis for Important Files

---

## GameManager.cs

### Current responsibility
Coordinates global application state transitions (`MainMenu`, `Playing`, `Editing`, `GameOver`, `Won`), activates/deactivates corresponding UI canvases, drives the single runtime gameplay simulation loop in `Update()`, and provides a global access point via a static singleton.

### State owned
- `Instance`: Static singleton reference.
- `State`: Current lifecycle enum (`GameState`).
- Direct references to `PlayerController`, `LevelLoader`, `CameraFollow`, `LevelEditorController`, and four UI canvas components (`GameOverUI`, `GameControlUI`, `MainMenuUI`, `LevelEditorUI`).

### Responsibilities mixed into this file
1. **Application Lifecycle Orchestration**: Sets active UI panels based on state changes.
2. **Simulation Clock & Update Orchestration**: Explicitly ticks `TrajectorySystem`, queries rider displacement, updates `PlayerController`, triggers marker checks in `LevelLoader`, and updates `CameraFollow`.
3. **Hardware Input Interception**: Directly queries keyboard devices for `R` or `Enter` keys to trigger restart.
4. **Fallback Dependency Instantiation**: Searches for missing scene objects in `Awake()` and dynamically attaches components to newly created GameObjects if missing.

### Dependencies
- [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs)
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)
- [CameraFollow](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)
- [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs)
- [GameOverUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameOverui.cs), [GameControlUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameControlUI.cs), [MainMenuUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/MainMenuUI.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs)

### Dependents
- [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) (queries `KillY`, calls `OnPlayerDied`)
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) (calls `OnPlayerWon`, queries `Player`)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) (calls `StartGameWithLevelData`)
- [MainMenuUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/MainMenuUI.cs) (calls `StartGame`, `OpenEditor`)
- [GameControlUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameControlUI.cs) (calls `Restart`, `Retry`)
- [GameOverUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameOverui.cs) (calls `Restart`, `Retry`)

### Architectural concerns
- Tightly coupled to every subsystem in the game; any change to UI or controller types impacts `GameManager`.
- Hosts low-level simulation sequencing that would be better owned by a dedicated gameplay simulation loop or state machine.

### Evidence from code
- `Awake()` ([lines 36-72](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L36-L72)): Fallback instantiation of UI and controllers via `new GameObject(...).AddComponent<...>()`.
- `Update()` ([lines 80-111](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L80-L111)): Hardcoded execution sequence for trajectory advancement, rider carry, player movement, marker collision, and camera follow.
- `RestartKeyPressed()` ([lines 233-242](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L233-L242)): Direct hardware input polling embedded in core lifecycle manager.

---

## LevelEditorController.cs

### Current responsibility
Manages interactive editing of levels in the game: coordinates 14 distinct interactive handle types for resizing and trajectory authoring, camera panning, object selection, placement of items from the palette, and level file import/export.

### State owned
- Active editing status (`editing`, `paletteDragging`, `mapPointerDragging`, `mapPanning`).
- Selection state (`selectionKind`, `selectedPlatform`, `selectedMarker`, `activeHandle`).
- Drag anchors (`dragStartWorld`, `dragStartPosition`, `dragStartSize`, trajectory origin/scale/rotation/radius anchors).
- Interactive visual handles (`handleRoot`, dictionary of 14 handle GameObjects).
- Interactive curve rendering (`trajectoryLineRenderer`, `boundsLineRenderer`).

### Responsibilities mixed into this file
1. **Editor Interaction & Camera Navigation**: Translates pointer input to world coordinates and pans `Camera.main` directly.
2. **Handle Rendering & Manipulation**: Instantiates 14 separate handle GameObjects and calculates parametric delta updates for 14 different handle kinds.
3. **Data Model Construction**: Gathers active platforms and markers in [BuildExportData()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L843-L924) to create a `LevelData` structure.
4. **File Persistence & Dialogs**: Directly opens OS file dialogs via `EditorUtility.OpenFilePanel` / `SaveFilePanel` and performs file I/O using `System.IO.File`.
5. **Runtime Scene Management**: Spawns and deletes `ColorPlatform` and `LevelMarker` GameObjects in the scene hierarchy.
6. **Data Validation**: Implements [ValidateImportedData()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L826-L841).
7. **Simulation Mode Switching**: Initiates test play by invoking `GameManager.Instance.StartGameWithLevelData()`.

### Dependencies
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)
- [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs)
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [LevelMarker](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs)
- [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs)
- [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs)
- [LevelData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs)
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs)

### Dependents
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) (starts/stops editor)
- [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) (two-way binding for property adjustments)
- [LevelEditorPaletteItem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorPaletteItem.cs) (notifies palette drag)

### Architectural concerns
- Massive class (1,246 lines) with 8 distinct architectural roles.
- Direct use of `UnityEditor.EditorUtility` requires conditional compilation `#if UNITY_EDITOR` and causes divergent behavior in standalone builds.
- Tightly couples UI, persistence, geometry mutation, rendering, and scene graph operations into one place.

### Evidence from code
- `RequestImport()` / `RequestExport()` ([lines 158-240](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L158-L240)): Uses `EditorUtility.OpenFilePanel` and `System.IO.File` directly.
- `ContinueMapPan()` ([lines 704-729](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L704-L729)): Directly mutates camera transform position.
- `BuildExportData()` ([lines 843-924](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L843-L924)): Traverses static registries `ColorPlatform.All` and `LevelMarker.All` to assemble serialization models.
- `RefreshHandles()` ([lines 1055-1167](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1055-L1167)): Manages positioning and visibility of 14 distinct interactive handle GameObjects.

---

## LevelEditorUI.cs

### Current responsibility
Builds and manages the entire runtime user interface for the level editor: inspector panel, palette, trajectory settings, status text, and deletion confirmation.

### State Owned
- Over 40 UI widget references (inputFields, dropdowns, toggles, text labels).
- Two-step delete button state: `pendingDeleteConfirm`, `deleteConfirmExpiry`.
- Event suppression flags: `suppressFields`, `suppressTraj`.
- Current platform reference: `selectedPlatform`.

### Responsibilities mixed into this file
1. **Procedural UI Construction**: Creates all Canvases, RectTransforms, Buttons, and Layout elements purely in code (over 600 lines of procedural UI code).
2. **Domain Object Construction**: In `BuildTrajectoryDataFromUI()`, parses numeric strings and constructs domain models (`TrajectoryData`).
3. **Dynamic Layout Repacking Engine**: In `RepositionTrajectoryRows()`, recalculates pixel offsets to reposition UI rows when curve parameters change.
4. **Preset Domain Configuration**: Encodes hardcoded trajectory presets (`ApplyPresetLineH`, `ApplyPresetLineV`, `ApplyPresetCircle`, `ApplyPresetArc180`).

### Dependencies
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)
- [PlatformColorSubComponent](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs)
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs)
- [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs)

### Dependents
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) (shows/hides editor UI)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) (sends selection data and status updates)

### Architectural concerns
- Massive class (1,232 lines) resulting from absence of Unity UI prefabs.
- Parses and instantiates domain models within the presentation layer.

### Evidence from code
- `BuildUI()` & `BuildInspector()` ([lines 201-479](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs#L201-L479)): Imperatively builds hundreds of UI components from scratch.
- `RepositionTrajectoryRows()` ([lines 546-620](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs#L546-L620)): Custom pixel-offset layout computation.
- `BuildTrajectoryDataFromUI()` ([lines 883-916](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs#L883-L916)): Instantiates and populates `TrajectoryData` from raw text input fields.

---

## LevelLoader.cs

### Current responsibility
Reads level definitions from JSON or fallback assets, instantiates platform and marker GameObject hierarchies, calculates the camera boundary and kill-depth (`KillY`), and manages checkpoint retry coordinates.

### State owned
- `CurrentLevelData`: Deserialized level contract.
- `CurrentSpawnPosition` and `CurrentSpawnColor`: Current active spawn coordinates and logical polarity.
- `KillY`: Vertical threshold for player death.
- Transform roots: `platformsRoot`, `levelObjectsRoot`.

### Responsibilities mixed into this file
1. **Multi-Source File I/O**: Searches across 4 filesystem paths and serialized TextAssets in [LoadJsonString()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L65-L85).
2. **Schema Migration**: Converts obsolete legacy `SpawnPointData` to modern `StartPosData` in [ConvertLegacySpawnPoint()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L367-L383).
3. **Fallback Level Construction**: Programmatically creates fallback level data if file loading fails ([CreateFallbackLevel()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L456-L484)).
4. **Gameplay Progression & Win Detection**: Evaluates player AABB contact with markers every frame in [ProcessPlayerInteractions()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L294-L342) and triggers `GameManager.Instance.OnPlayerWon()`.
5. **Camera Boundary Binding**: Directly searches for `CameraFollow` and sets boundary dimensions.

### Dependencies
- [LevelData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs)
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [LevelMarker](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs)
- [CameraFollow](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Camera/CameraFollow.cs)
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs)
- [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs)

### Dependents
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) (triggers level load, retry, and interactions)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) (triggers runtime build on import and test play)

### Architectural concerns
- Mixes file I/O, legacy schema migration, scene spawning, and runtime gameplay goal evaluation in one class.

### Evidence from code
- `LoadJsonString()` ([lines 65-85](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L65-L85)): Probes 4 filesystem paths directly via `System.IO.File`.
- `BuildRuntimeLevel()` ([lines 87-256](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L87-L256)): Destroys existing scene hierarchy, creates GameObjects, initializes trajectories, and spawns the player.
- `ProcessPlayerInteractions()` ([lines 294-342](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L294-L342)): Gameplay loop evaluating player AABB contact against `LevelMarker.All` and invoking `GameManager.Instance.OnPlayerWon()`.

---

## ColorPlatform.cs

### Current responsibility
Represents an individual platform in the scene, maintaining its spatial AABB and color polarity, and hosting its trajectory component.

### State owned
- `All`: Public static list of all active platforms.
- Geometry: `size`, `colliderPadding`, `colliderOffset`, `AuthoredPosition`.
- Polarity: `rawGrayscaleColor`, `Logical`.
- Component: `Trajectory trajectory`.
- Procedural rendering: `static Sprite cachedSprite`, child `SpriteRenderer`.

### Responsibilities mixed into this file
1. **Collision Geometry Storage**: Generates authoritative axis-aligned bounds via `GetAabb()`.
2. **Procedural Rendering**: Creates a 2x2 Texture2D, creates a Sprite, spawns a child GameObject named `"Visual"`, and applies colors.
3. **Global Static Registration**: Automatically registers itself to `ColorPlatform.All` on initialization and unregisters on destruction.
4. **Trajectory Lifecycle**: Lazily instantiates and configures the `Trajectory` component.
5. **Dual Initialization**: Maintains distinct `Initialize()` (runtime) and `SetEditorData()` (editor) methods.
6. **Editor Execution**: Runs in edit mode via `[ExecuteAlways]`.

### Dependencies
- [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs)
- [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs)
- [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs)

### Dependents
- [PlayerOverlapResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs) (iterates `ColorPlatform.All`)
- [PlayerCollisionResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs) (iterates `ColorPlatform.All`)
- [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) (iterates `ColorPlatform.All`)
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) (instantiates platforms, clears registry)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) (manipulates platforms, reads `All` for export)

### Architectural concerns
- Public static registry `ColorPlatform.All` acts as a global mutable bottleneck for movement, collision, trajectories, and serialization.
- Mixes physics bounding box data with procedural sprite generation and editor lifecycle hooks.

### Evidence from code
- `All` declaration & `OnDestroy()` ([lines 42, 54-57](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L42-L57)): Global static registry management.
- `GetDefaultSprite()` ([lines 194-223](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L194-L223)): Procedural creation of Texture2D and Sprite.
- `InitializeTrajectory()` ([lines 72-83](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L72-L83)): Component lifecycle management for Trajectory.

---

## PlayerController.cs

### Current responsibility
Coordinates the player character: drives input reading, motion resolution, applies transform displacement, updates visual color, and checks for kill plane deaths.

### State owned
- Locomotion state: `State` (`PlayerState`), `isGrounded`.
- Polarity state: `CurrentColor` (`LogicalColor`), `currentColorValue`.
- Transform mutation: `riderDisplacement`.
- Geometry: `playerSize`, `playerOffset`.

### Responsibilities mixed into this file
1. **Locomotion Facade**: Invokes `PlayerInput`, `PlayerJumpMotion`, and `PlayerMovementResolver` in `ProcessUpdate()`.
2. **Transform Mutation**: Only component that mutates player transform position during gameplay (`ApplyMove()`).
3. **Color Polarity Cycling**: Cycles player color chunks (White -> Gray -> Black -> White) in `StepColorChunk()`.
4. **Visual Presentation**: Directly mutates `SpriteRenderer.color`.
5. **Death Boundary Evaluation**: Checks current `transform.position.y` against `GameManager.Instance.KillY`.

### Dependencies
- [PlayerInput](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs)
- [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs)
- [PlayerJumpMotion](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs)
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs)
- [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs)
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)

### Dependents
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) (calls `ProcessUpdate`, sets rider displacement)
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) (calls `Respawn`, checks marker interaction)

### Architectural concerns
- Tightly couples movement pipeline coordination with direct SpriteRenderer visual mutation and GameManager kill boundary checks.

### Evidence from code
- `ProcessUpdate()` ([lines 69-157](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L69-L157)): Coordinates input, jump, movement resolver, position application, state updates, and kill check.
- `ApplyMove()` ([lines 162-179](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L162-L179)): Mutates `transform.position`.
- `ApplyColorVisual()` ([lines 279-286](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L279-L286)): Directly sets `sr.color`.

---

## PlayerMovementResolver.cs

### Current responsibility
Executes the top-level player movement pipeline: queries current penetration from `PlayerOverlapResolver`, combines intent vectors (`input + jump + push + rider`), queries predictive collision from `PlayerCollisionResolver`, and outputs an immutable `MovementResult`.

### State owned
- References to `PlayerOverlapResolver` and `PlayerCollisionResolver`.

### Responsibilities mixed into this file
- Pure orchestration of the movement calculation. It does not mutate transforms or directly query platforms.

### Dependencies
- [PlayerOverlapResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs)
- [PlayerCollisionResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs)
- [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs)

### Dependents
- [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) (invokes `Resolve()`)

### Architectural concerns
- Well-separated pipeline component; serves as a model of single-responsibility orchestration.

### Evidence from code
- `Resolve()` ([lines 29-147](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs#L29-L147)): Coordinates overlap evaluation, intent composition, and predictive resolution.

---

## PlayerCollisionResolver.cs

### Current responsibility
Executes predictive swept-AABB collision resolution along the horizontal axis, then along the vertical axis, preserving tangential sliding and ground contact status.

### State owned
- Tuning parameters: `collisionEpsilon`, `groundEpsilon`.

### Responsibilities mixed into this file
1. **Swept AABB Collision Resolution**: Calculates adjusted X and Y positions.
2. **Ground Support Detection**: Evaluates whether the player's bottom edge is resting on a solid platform.
3. **Escape Permeability Handling**: Allows penetration escape movements without snapping back into currently overlapped platforms.

### Dependencies
- [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs)
- [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs)

### Dependents
- [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs)

### Architectural concerns
- Iterates directly over `ColorPlatform.All` without spatial partitioning or collision world abstractions.

### Evidence from code
- `Resolve()` ([lines 31-103](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs#L31-L103)): Coordinates horizontal and vertical resolution.
- `ResolveHorizontal()` ([lines 105-193](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs#L105-L193)): Iterates `ColorPlatform.All` for horizontal blocking.
- `ResolveVertical()` ([lines 195-284](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs#L195-L284)): Iterates `ColorPlatform.All` for vertical blocking.
- `HasGroundSupport()` ([lines 286-314](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs#L286-L314)): Iterates `ColorPlatform.All` to detect ground contact.

---

## PlayerOverlapResolver.cs

### Current responsibility
Evaluates existing penetrations between the player's current AABB and blocking platforms in `ColorPlatform.All`. Determines if the player is stuck (overlap ≥ 50%) or computes an outward escape direction and push speed.

### State owned
- `stuckOverlapPercent`, `overlapPushSpeed`, `verticalPushSpeed`, `verticalPushSpeedMargin`, `overlapSpeedReductionPercent`.

### Responsibilities mixed into this file
1. **Penetration Evaluation**: Computes percentage area overlap against all platforms.
2. **Escape Direction Selection**: Determines the minimal distance edge to escape penetration.
3. **Push Velocity Calculation**: Guarantees vertical escape velocity exceeds gravity.

### Dependencies
- [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs)
- [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs)

### Dependents
- [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs)

### Architectural concerns
- Iterates directly over `ColorPlatform.All` every frame.

### Evidence from code
- `Evaluate()` ([lines 59-165](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs#L59-L165)): Traverses `ColorPlatform.All`, calculates overlap areas, and chooses escape direction.
- `GetPushSpeed()` ([lines 44-57](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs#L44-L57)): Adjusts vertical push speed to override fall velocity.

---

## Trajectory.cs

### Current responsibility
Evaluates parametric trajectory curve positions (Line and Circle), handles Liang-Barsky spatial bounds clipping, advances playback phases (`PingPong`, `Loop`, `Once`), calculates per-frame displacements for rider-carry, and renders Scene-view gizmos.

### State owned
- Serialized configuration: `TrajectoryData config`.
- Runtime simulation state: `phase`, `pingPongDir`, `previousWorldPosition`, `FrameDisplacement`.
- Validation error message: `validationError`.

### Responsibilities mixed into this file
1. **Mathematical Curve Evaluation**: Static pure functions for Line and Circle evaluation ([EvaluateWorldPoint()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L210-L228)).
2. **Spatial Bounds Clipping**: Liang-Barsky line clipping ([ClipLineAgainstBounds()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L283-L324)).
3. **Runtime Simulation**: Progresses `phase`, calculates bounces, and computes `FrameDisplacement`.
4. **Point Projection**: Projects world coordinates onto curves in [ProjectPointOntoTrajectory()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L371-L462).
5. **Data Validation**: Houses [Validate()](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L469-L491).
6. **Scene Visualization**: Generates preview line arrays and draws Scene Gizmos.

### Dependencies
- [TrajectoryData](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/TrajectoryData.cs)

### Dependents
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) (owns component instance)
- [TrajectorySystem](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs) (ticks `ProcessUpdate`)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) (calls projection and validation)
- [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) (calls validation)

### Architectural concerns
- Mixes pure mathematical geometry, runtime simulation state, and editor visualizer logic into a single 581-line file.

### Evidence from code
- `ProcessUpdate()` ([lines 106-202](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L106-L202)): Advances phase and mutates transform position.
- `EvaluateLine()` & `EvaluateCircle()` ([lines 232-258, 353-365](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L232-L258)): Pure curve evaluation functions.
- `OnDrawGizmosSelected()` ([lines 557-579](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L557-L579)): In-editor Gizmo drawing.

---

## TrajectorySystem.cs

### Current responsibility
Coordinates all active platform trajectories: iterates `ColorPlatform.All` once per gameplay frame to tick motion and evaluates whether a grounded player should receive rider displacement.

### State owned
- None (pure static coordinator).

### Responsibilities mixed into this file
1. **System Ticking Coordinator**: Calls `ProcessUpdate` on all platform trajectories.
2. **Rider-Carry Physics Calculation**: Queries prior-frame platform geometry to determine if player's feet rested on a moving platform.

### Dependencies
- [ColorPlatform.All](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs)
- [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs)

### Dependents
- [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) (calls `UpdateAll` and `GetRiderDisplacement`)
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) (calls `ResetAll`)

### Architectural concerns
- Tightly bound to `ColorPlatform.All`.

### Evidence from code
- `UpdateAll()` ([lines 23-36](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs#L23-L36)): Traverses `ColorPlatform.All`.
- `GetRiderDisplacement()` ([lines 61-113](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/TrajectorySystem.cs#L61-L113)): Computes rider displacement from prior platform geometry.

---

## PlatformColorSubComponent.cs

### Current responsibility
Encapsulates UI widgets for selecting and displaying platform color (type dropdown, raw numeric field, quick-preset buttons, preview swatch).

### State owned
- `currentRawColor`, widget references (`typeDropdown`, `rawValueField`, `swatchImage`, `swatchLabel`, buttons).

### Responsibilities mixed into this file
- Cleanly focused UI sub-component; maintains two-way synchronization between dropdown types, raw numeric inputs, and visual swatches.

### Dependencies
- [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs)
- [LogicalColor](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs)

### Dependents
- [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs)

### Architectural concerns
- Contains hardcoded color values and names that mirror `ColorClassifier`.

### Evidence from code
- `BuildUI()` ([lines 37-147](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs#L37-L147)): Assembles color inspector rows.
- `SetRawColor()` ([lines 195-214](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs#L195-L214)): Synchronizes widgets and triggers callbacks.

---

## LevelMarker.cs

### Current responsibility
Represents spatial volumes for StartPos, CheckPoint, and End contacts.

### State owned
- `All`: Public static list of active markers.
- `markerType`, `size`, `rawColor`, `consumed`.
- Child `SpriteRenderer`.

### Responsibilities mixed into this file
1. **Trigger Geometry Storage**: Authoritative AABB bounds.
2. **Visual Presentation**: Color tinting and transparency setup.
3. **Global Static Registration**: Registers to `LevelMarker.All`.
4. **Self-Destruction on Contact**: CheckPoints and End markers destroy themselves upon being touched.

### Dependencies
- [ColorClassifier](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs)
- [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [MovementGeometry](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs)

### Dependents
- [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)
- [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)

### Architectural concerns
- Global static list `All` creates hidden coupling with `LevelLoader` and `LevelEditorController`.

### Evidence from code
- `All` declaration & `OnPlayerTouched()` ([lines 26, 122-135](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs#L26-L135)): Self-removal from static list and self-destruction.

---

# 5. Real Execution Control Flow

---

## Runtime Flow

```text
Input (Keyboard / InputSystem)
│
▼
PlayerInput.ReadInput() -> produces PlayerInputIntent
│
▼
GameManager.Update() (only when State == Playing)
│
├── 1. TrajectorySystem.UpdateAll(Time.deltaTime)
│      └── For each platform in ColorPlatform.All:
│             Trajectory.ProcessUpdate() -> updates Transform -> stores FrameDisplacement
│
├── 2. TrajectorySystem.GetRiderDisplacement(...)
│      └── Checks if player was grounded on moving platform in previous frame
│
├── 3. player.SetRiderDisplacement(riderDisp)
│
├── 4. player.ProcessUpdate(Time.deltaTime)
│      ├── Color step or direct color change applied
│      ├── jumpMotion.RequestJump() (if requested)
│      ├── jumpMotion.GetMoveIntent() -> jumpIntent
│      │
│      ├── movementResolver.Resolve(...)
│      │      ├── overlapResolver.Evaluate() -> scans ColorPlatform.All -> stuck or pushDirection
│      │      ├── combinedIntent = input + jump + push + rider
│      │      └── collisionResolver.Resolve(combinedIntent)
│      │            ├── ResolveHorizontal vs ColorPlatform.All
│      │            ├── ResolveVertical vs ColorPlatform.All
│      │            └── HasGroundSupport vs ColorPlatform.All
│      │
│      ├── player.ApplyMove(finalMove) -> mutates transform.position
│      └── Kill check: if (pos.y < GameManager.Instance.KillY) player.Die()
│
├── 5. levelLoader.ProcessPlayerInteractions(player)
│      └── Scans LevelMarker.All:
│             ├── CheckPoint -> updates CurrentSpawnPosition & CurrentSpawnColor
│             └── End -> GameManager.Instance.OnPlayerWon()
│
└── 6. cameraFollow.ProcessCameraUpdate()
       └── Snaps camera to player, clamped within boundary
```

---

## Level Editor Flow

```text
Editor Input (Mouse / Drag / Keyboard)
│
▼
LevelEditorController.Update()
│
├── Middle Mouse Drag -> BeginMapPan() / ContinueMapPan() -> Camera.main.transform moved
│
├── Left Mouse Down -> HitHandle() / HitPlatform() / HitMarker()
│     ├── If Handle -> mapPointerDragging = true; activeHandle = handle
│     ├── If Platform -> SelectPlatform(platform); mapPointerDragging = true
│     └── If Marker -> SelectMarker(marker); mapPointerDragging = true
│
├── Left Mouse Drag:
│     ├── If activeHandle != None -> updates platform size, position, or trajectory parameters
│     └── If activeHandle == None -> drags platform or marker body (shifts trajectory origin)
│
├── Palette Drag:
│     LevelEditorPaletteItem.OnPointerDown() -> BeginPaletteDrag()
│     LevelEditorPaletteItem.OnPointerUp()   -> EndPaletteDrag() -> CreateFromPalette()
│
├── Inspector Edit:
│     LevelEditorUI input field edit -> LevelEditorUI.ApplyFields()
│     └── LevelEditorController.ApplySelectedPlatformFromUI(...)
│
├── Trajectory Edit:
│     LevelEditorUI dropdown/toggle edit -> LevelEditorUI.ApplyTrajectory()
│     └── LevelEditorUI.BuildTrajectoryDataFromUI() -> constructs TrajectoryData
│     └── LevelEditorController.ApplySelectedTrajectoryFromUI(data)
│
└── Export / Import:
      Export -> LevelEditorController.BuildExportData() -> JsonUtility.ToJson -> File.WriteAllText
      Import -> LevelEditorController.RequestImport() -> File.ReadAllText -> JsonUtility.FromJson
             -> LevelLoader.BuildRuntimeLevel(data)
```

---

## Test Play Flow

```text
LevelEditorUI ("TEST PLAY" clicked)
│
▼
LevelEditorController.RequestPlayLevel()
│
├── 1. BuildExportData() -> creates fresh LevelData from active scene platforms/markers
├── 2. ClearSelection()
└── 3. GameManager.Instance.StartGameWithLevelData(exportData)
       │
       ├── a. levelEditor.StopEditing()
       ├── b. levelEditorUI.Show(false)
       ├── c. player.gameObject.SetActive(true)
       ├── d. SetState(GameState.Playing)
       └── e. levelLoader.BuildRuntimeLevel(exportData)
              ├── ClearRuntimeLevel() (destroys old GameObjects, clears registries)
              ├── Instantiates ColorPlatform instances from exportData
              ├── Instantiates LevelMarker instances from exportData
              ├── Configures CameraFollow boundary
              └── Respawns player at StartPosData position and color
```

---

# 6. Data Ownership Analysis

| Data Type | Who Creates It? | Who Owns It? | Who Reads It? | Who Modifies It? | Who Serializes It? | Who Reconstructs It? |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `LevelData` | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) / JSON | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | `LevelEditorController` via JsonUtility | `LevelLoader` & `LevelEditorController` via JsonUtility |
| `ColorPlatformData` | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) / JSON | `LevelData.platforms[]` | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) | JsonUtility | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) |
| `TrajectoryData` | [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) / Defaults | `ColorPlatformData`, [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs) | [Trajectory](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) | [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs), [LevelEditorUI](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs) | JsonUtility | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs), [LevelEditorController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs) |
| Player State | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs), [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | Not serialized | Reset on respawn |
| Checkpoint Spawn | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) (on contact) | Embedded in LevelData | Reconstructed on level build |
| Game State | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | All UI classes, Update loop | [GameManager](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | Not serialized | Default to MainMenu |
| `PlayerInputIntent` | [PlayerInput](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs) | Stack/Local | [PlayerController](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs), [PlayerMovementResolver](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | Immutable struct | Not serialized | Created each frame |
| Runtime Platform List | [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) | Static `ColorPlatform.All` | Movement Resolvers, TrajectorySystem, Editor | [ColorPlatform](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs), [LevelLoader](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | Extracted on Export | Cleared on Level Clear |

---

# 7. Trace Dependencies

```text
GameManager (Singleton)
  ├── depends on → PlayerController
  ├── depends on → LevelLoader
  ├── depends on → CameraFollow
  ├── depends on → LevelEditorController
  ├── depends on → TrajectorySystem
  ├── depends on → GameOverUI, GameControlUI, MainMenuUI, LevelEditorUI
  └── modifies → GameState

LevelEditorController
  ├── depends on → LevelLoader
  ├── depends on → LevelEditorUI
  ├── depends on → ColorPlatform
  ├── depends on → LevelMarker
  ├── depends on → Trajectory
  ├── depends on → GameManager
  ├── modifies → Camera.main.transform.position
  ├── modifies → ColorPlatform instances & TrajectoryData
  └── modifies → LevelData (Export)

LevelEditorUI
  ├── depends on → LevelEditorController
  ├── depends on → PlatformColorSubComponent
  ├── depends on → ColorPlatform
  ├── depends on → TrajectoryData
  └── modifies → TrajectoryData (from UI inputs)

LevelLoader
  ├── depends on → LevelData
  ├── depends on → ColorPlatform
  ├── depends on → LevelMarker
  ├── depends on → CameraFollow
  ├── depends on → GameManager
  ├── modifies → ColorPlatform.All & LevelMarker.All (clears & instantiates)
  └── modifies → CurrentSpawnPosition & CurrentSpawnColor

PlayerController
  ├── depends on → PlayerInput
  ├── depends on → PlayerMovementResolver
  ├── depends on → PlayerJumpMotion
  ├── depends on → GameManager
  ├── depends on → ColorClassifier
  └── modifies → Player transform.position, PlayerState, CurrentColor

PlayerMovementResolver
  ├── depends on → PlayerOverlapResolver
  ├── depends on → PlayerCollisionResolver
  └── modifies → MovementResult (immutable return)

PlayerCollisionResolver
  ├── depends on → ColorPlatform.All
  ├── depends on → MovementGeometry
  └── modifies → CollisionResult (immutable return)

PlayerOverlapResolver
  ├── depends on → ColorPlatform.All
  ├── depends on → MovementGeometry
  └── modifies → OverlapResult (immutable return)

TrajectorySystem
  ├── depends on → ColorPlatform.All
  ├── depends on → Trajectory
  └── modifies → Trajectory state (via ProcessUpdate)
```

### Dependency Findings:
- **Central Bottleneck**: `GameManager.cs` sits in the center of all runtime systems.
- **Global Data Coupling**: `ColorPlatform.All` couples the player movement resolution directly to the concrete `ColorPlatform` MonoBehaviour instances.
- **Two-Way Coupling**: `LevelEditorController` and `LevelEditorUI` call each other bidirectionally.

---

# 8. Potential God Classes

---

### LevelEditorController.cs (1,246 lines)

**Evidence:**
- Houses 14 distinct interactive handle manipulation routines.
- Captures screen input, calculates world coordinates, and directly moves the main camera.
- Implements disk I/O, file dialogs, and JSON serialization.
- Assembles `LevelData` domain models.
- Spawns and deletes runtime GameObjects in the scene.
- Transitions into test play mode via `GameManager`.

**Responsibilities currently contained:**
1. Editor user interaction and drag coordination.
2. Scene camera navigation.
3. Handle visualization and geometry computation.
4. Trajectory curve math synchronization.
5. Level data export and JSON serialization.
6. Level data import, validation, and file dialogs.
7. Runtime scene hierarchy management.
8. Test play transition coordination.

**Why this is potentially problematic:**
Violates Single Responsibility Principle. Any change to editor interaction, serialization schema, handle visuals, camera controls, or level testing requires modifying this single file.

---

### LevelEditorUI.cs (1,232 lines)

**Evidence:**
- Imperatively constructs dozens of UI widgets without prefabs.
- Maintains a dynamic vertical layout engine with pixel-coordinate calculations.
- Implements a timed two-step confirmation state machine for deletion.
- Assembles and parses domain data (`TrajectoryData`) from text inputs.

**Responsibilities currently contained:**
1. Procedural UI generation.
2. Dynamic widget repositioning and layout packing.
3. Domain data parsing and construction (`TrajectoryData`).
4. Hardcoded trajectory preset configurations.
5. Deletion confirmation timing state machine.

**Why this is potentially problematic:**
Conflates visual presentation with layout calculation, domain parsing, and editor interaction flow.

---

### LevelLoader.cs (486 lines)

**Evidence:**
- Implements file search across 4 filesystem directories.
- Handles JSON schema migrations for legacy data.
- Instantiates GameObject hierarchies and configures camera boundaries.
- Owns runtime checkpoint spawn coordinates and colors.
- Monitors player-marker collisions and triggers game-winning events.
- Constructs fallback level data programmatically.

**Responsibilities currently contained:**
1. Filesystem access and JSON deserialization.
2. Schema migration.
3. Scene instantiation and hierarchy cleanup.
4. Checkpoint progression tracking.
5. Gameplay win-condition detection.
6. Fallback data authoring.

**Why this is potentially problematic:**
Mixes data persistence, scene spawning, player progression, and win-condition gameplay logic.

---

# 9. Coupling Analysis

1. **Strongest Dependencies**:
   - `GameManager` tightly couples UI, editor, loader, player, and camera.
   - `LevelEditorController` and `LevelEditorUI` form a tight two-way dependency pair.
2. **Central Managers**:
   - `GameManager` acts as the single central hub for application lifecycle and runtime simulation updates.
3. **Classes with Excessive Knowledge**:
   - `LevelEditorController` knows about handles, file I/O, JSON, line renderers, cameras, platforms, and game manager states.
   - `LevelLoader` knows about filesystem candidate paths, JSON schemas, camera boundaries, player respawning, checkpoints, and game-win events.
4. **Systems That Cannot Be Tested Independently**:
   - `PlayerCollisionResolver` and `PlayerOverlapResolver` cannot run without populating the static list `ColorPlatform.All`.
   - `TrajectorySystem` requires active `ColorPlatform` GameObjects in `ColorPlatform.All`.
5. **UI Classes Containing Gameplay / Domain Logic**:
   - `LevelEditorUI.BuildTrajectoryDataFromUI()` directly parses strings and constructs `TrajectoryData`.
   - `PlatformColorSubComponent` knows the exact color classification thresholds and presets.
6. **Gameplay Classes Depending on UI / Presentation**:
   - `PlayerController` directly manipulates `SpriteRenderer.color`.
   - `ColorPlatform` programmatically builds its own visual child GameObject and generates procedural textures.
   - `ColorClassifier` returns `UnityEngine.Color` values alongside logical polarity classification.
7. **Editor Logic Leaking into Runtime**:
   - `ColorPlatform` is marked `[ExecuteAlways]` and maintains `SetEditorData()` alongside `Initialize()`.
   - `LevelMarker` maintains `SetEditorData()` and `SetEditorPosition()`.
   - `Trajectory` contains `GetPreviewPoints()` and Gizmo drawing logic alongside runtime simulation code.
8. **Serialization Logic Leaking into Gameplay**:
   - `LevelLoader` handles filesystem search and schema migration while simultaneously evaluating checkpoint touches during gameplay.
9. **Global / Static State Creating Hidden Dependencies**:
   - `ColorPlatform.All` and `LevelMarker.All` provide global mutable state accessed across disparate systems.

---

# 10. Candidate Boundaries

```text
┌─────────────────────────────────────────────────────────────────┐
│                           CORE / APP                            │
│  GameState.cs                     GameManager.cs (Coordinator)  │
└───────────────────────────────┬─────────────────────────────────┘
                                │
        ┌───────────────────────┼────────────────────────┐
        ▼                       ▼                        ▼
┌───────────────┐       ┌───────────────┐       ┌─────────────────┐
│     INPUT     │       │   SIMULATION  │       │  PRESENTATION   │
│ PlayerInput   │       │   (Movement)  │       │ MainMenuUI      │
└───────┬───────┘       │ MovementGeom  │       │ GameControlUI   │
        │               │ JumpMotion    │       │ GameOverUI      │
        ▼               │ OverlapResolv │       │ CameraFollow    │
┌───────────────┐       │ CollisionRes  │       └─────────────────┘
│ PLAYER FACADE │       │ MovementRes   │
│ PlayerControl │       └───────▲───────┘
└───────┬───────┘               │
        │                       │ queries geometry
        ▼                       │
┌───────────────────────────────┴─────────────────────────────────┐
│                      ENVIRONMENT / LEVEL                        │
│  LogicalColor.cs      ColorClassifier.cs      ColorPlatform.cs  │
│  LevelMarker.cs       Trajectory.cs           TrajectorySystem  │
└───────────────────────────────▲─────────────────────────────────┘
                                │ instantiates / manages
        ┌───────────────────────┴────────────────────────┐
        ▼                                                ▼
┌───────────────────────────────┐       ┌─────────────────────────┐
│     PERSISTENCE & DATA        │       │      LEVEL EDITOR       │
│  LevelData.cs                 │       │  LevelEditorController  │
│  TrajectoryData.cs            │       │  LevelEditorUI          │
│  LevelLoader.cs               │       │  LevelEditorPaletteItem │
└───────────────────────────────┘       │  PlatformColorSubComp   │
                                        └─────────────────────────┘
```

### Boundary Analysis:
- **Files that cleanly belong**: `PlayerInput` (Input), `MovementGeometry` (Simulation), `LevelData` (Persistence), `MainMenuUI` (Presentation).
- **Files that cross multiple boundaries**:
  - `ColorPlatform` crosses Domain, Simulation, Presentation, and Global Registry.
  - `LevelLoader` crosses Persistence, Scene Spawning, and Gameplay Progression.
  - `LevelEditorController` crosses Editor Tooling, Persistence, Interaction, and Presentation.
  - `LevelEditorUI` crosses Presentation and Domain Model Assembly.
- **Unclear boundaries**: The boundary between Level Loading and Gameplay Goal Evaluation is blurred within `LevelLoader`. The boundary between Collision World and Scene Hierarchy is blurred within `ColorPlatform.All`.

---

# 11. Current Architecture Diagram

```text
                               +------------------------+
                               |      GameManager       | (Singleton)
                               |  Master Update & State |
                               +-----------+------------+
                                           |
                 +-------------------------+-------------------------+
                 |                         |                         |
                 v                         v                         v
       +-------------------+     +-------------------+     +-------------------+
       |    LevelLoader    |     |  PlayerController |     |   UI Orchestrator  |
       |  Loads JSON &     |     |  Locomotion Root  |     | MainMenu / Over / |
       |  Spawns Scene     |     +---------+---------+     | GameControl       |
       +---------+---------+               |               +-------------------+
                 |                         |
                 | spawns                  | drives
                 v                         v
       +-------------------+     +-------------------+
       |   ColorPlatform   |     | MovementResolver  |
       |  & LevelMarker    |     +---------+---------+
       +---------+---------+               |
                 |                         | delegates to
                 | registers into          v
                 v               +-------------------+     +-------------------+
    +=========================+  | OverlapResolver   |     | CollisionResolver |
    |   ColorPlatform.All     |<-+ Evaluates escape  |     | Swept AABB        |
    |   LevelMarker.All       |<-+ direction & stuck |<----+ resolution (X & Y)|
    +=========================+  +-------------------+     +-------------------+
                 ^                         ^
                 | reads                   | reads
                 |                         |
       +---------+---------+               |
       | TrajectorySystem  |---------------+ (Rider displacement)
       | Ticks Trajectory  |
       +---------+---------+
                 ^
                 | uses
       +---------+---------+
       |   Trajectory      |
       | Evaluates curves  |
       +-------------------+
                 ^
                 | manipulated by
       +---------+---------+              two-way binding          +-------------------+
       | LevelEditorContr. |<=====================================>|   LevelEditorUI   |
       | Handles, Pan,     |                                       | Procedural canvas |
       | Export/Import I/O |                                       | & Inspector GUI   |
       +-------------------+                                       +-------------------+
```

---

# 12. Critical Architectural Findings

### HIGH SEVERITY

1. **Global Mutable Registries (`ColorPlatform.All` & `LevelMarker.All`) Drive Physics & Serialization**
   - *What the problem is*: The custom collision engine, trajectory system, and level exporter query public static lists rather than an abstracted level container or spatial index.
   - *Where it exists*: `ColorPlatform.cs` ([line 42](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L42)), `LevelMarker.cs` ([line 26](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelMarker.cs#L26)).
   - *Why it matters*: Static lists introduce hidden state across scene transitions, complicate automated unit testing, and risk memory leaks or stale null entries if objects are destroyed without clean deregistration.
   - *Triggering future change*: Adding object pooling, multi-room level streaming, or running automated simulation tests in parallel.

2. **Monolithic God Controllers in Level Editor (`LevelEditorController` & `LevelEditorUI`)**
   - *What the problem is*: Over 2,400 lines of complex editing logic, file I/O, procedural UI layout, handle math, and camera panning are packed into two interdependent classes.
   - *Where it exists*: `LevelEditorController.cs`, `LevelEditorUI.cs`.
   - *Why it matters*: Extreme coupling makes extending editor features hazardous and difficult to debug.
   - *Triggering future change*: Adding an Undo/Redo command pattern, multi-selection, or supporting touch/mobile editing.

3. **Gameplay / Win Condition Evaluated Inside `LevelLoader`**
   - *What the problem is*: `LevelLoader` evaluates player contact against End and CheckPoint markers every frame and directly calls `GameManager.Instance.OnPlayerWon()`.
   - *Where it exists*: `LevelLoader.cs` ([lines 294-342](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs#L294-L342)).
   - *Why it matters*: Conflates scene loading and file persistence with runtime gameplay trigger evaluation.
   - *Triggering future change*: Introducing multiple win conditions, cutscenes, or level-streaming triggers.

---

### MEDIUM SEVERITY

4. **Procedural UI Construction (Lack of Prefabs)**
   - *What the problem is*: All UI canvases, panels, input fields, scroll rects, and dropdowns are created via imperative C# code.
   - *Where it exists*: `LevelEditorUI.cs`, `MainMenuUI.cs`, `GameControlUI.cs`, `GameOverui.cs`.
   - *Why it matters*: Bloats code size by thousands of lines of boilerplate, prevents designers from modifying UI visually in the Unity Editor, and complicates localization/styling.
   - *Triggering future change*: UI redesign, theme changes, or localization.

5. **`ColorClassifier` Mixing Pure Domain Logic with Presentation Assets**
   - *What the problem is*: `ColorClassifier` defines both domain classification thresholds (0..84, 85..169, 170..254, 255) and visual presentation RGBA colors.
   - *Where it exists*: `ColorClassifier.cs` ([lines 48-68](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs#L48-L68)).
   - *Why it matters*: Visual palette adjustments bleed into domain classification logic.
   - *Triggering future change*: Introducing custom color themes or colorblind accessibility modes.

6. **Input Polling Scattered Across Multiple Layers**
   - *What the problem is*: Input is polled in `PlayerInput.cs` (gameplay), `GameManager.cs` (restart hotkey), and `LevelEditorController.cs` (map panning, delete shortcut).
   - *Where it exists*: `PlayerInput.cs`, `GameManager.cs`, `LevelEditorController.cs`.
   - *Why it matters*: Lack of a unified input coordinator leads to input leakage (e.g., typing in an input field could trigger game controls if not guarded).
   - *Triggering future change*: Supporting gamepads, remappable keybindings, or mobile controls.

---

### LOW SEVERITY

7. **Bifurcated Serialization Logic in `LevelEditorController` vs `LevelLoader`**
   - *What the problem is*: `LevelEditorController` contains its own validation and JSON reading logic, while `LevelLoader` also parses JSON and has legacy fallback logic.
   - *Where it exists*: `LevelEditorController.cs`, `LevelLoader.cs`.
   - *Why it matters*: Duplicated JSON parsing and path handling code risks divergence when schema updates occur.
   - *Triggering future change*: Updating the level JSON schema.

8. **Naming Inconsistencies**
   - *What the problem is*: `GameOverui.cs` uses a lowercase `ui` in the filename while the class is `GameOverUI`.
   - *Where it exists*: `Assets/Script/Contrast/UI/GameOverui.cs`.
   - *Why it matters*: Can cause case-sensitivity issues on cross-platform file systems (Linux CI/CD build agents).
   - *Triggering future change*: Building on Linux or strict case-sensitive platforms.

---

# 13. Architecture Discussion Preparation

## Files that should be examined first
1. **[LevelEditorController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)**: Holds the greatest concentration of mixed responsibilities (I/O, UI, handles, camera, data model assembly).
2. **[LevelEditorUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs)**: Heaviest view component; prime candidate for conversion to Unity UI Prefabs and separation of data parsing.
3. **[ColorPlatform.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)**: Heart of the gameplay environment; decoupling its static registry and visual presentation from its AABB collision data is critical for modularity.
4. **[LevelLoader.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)**: Core persistence coordinator; needs win-condition and player-interaction evaluation extracted.

## Most overloaded files
1. **[LevelEditorController.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)** (8 architectural roles identified)
2. **[LevelEditorUI.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/UI/LevelEditorUI.cs)** (5 architectural roles identified)
3. **[ColorPlatform.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)** (5 architectural roles identified)
4. **[LevelLoader.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)** (5 architectural roles identified)
5. **[GameManager.cs](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs)** (6 architectural roles identified)

## Most important dependency relationships
1. **`PlayerMovementResolver` / `PlayerCollisionResolver` -> `ColorPlatform.All`**: Direct dependency on concrete platform GameObjects for simulation queries.
2. **`GameManager` <-> All Subsystems**: Bidirectional coupling between singleton manager and its managed components.
3. **`LevelEditorController` <-> `LevelEditorUI`**: Tight, mutually dependent UI-Controller coupling.
4. **`LevelLoader` -> `LevelMarker.All` -> `GameManager`**: Gameplay win evaluation leaking into the persistence loader.

## Main architectural questions
1. **Collision World Abstraction**: Should `ColorPlatform.All` be replaced with a dedicated `ICollisionWorld` or spatial query service (e.g., Quadtree or spatial hash grid) to decouple collision resolution from MonoBehaviour instances?
2. **UI Architecture Strategy**: Should procedural code-based UI generation be replaced with standard Unity UI Prefabs or UI Toolkit?
3. **Level Editor Decomposition**: How should `LevelEditorController` be partitioned? Recommended split candidates:
   - `LevelPersistenceService` (File I/O, Save/Load, JSON serialization)
   - `EditorHandleManager` (Transform/Trajectory gizmos and handle interaction)
   - `EditorCameraController` (Camera panning and zooming)
   - `LevelEditorOrchestrator` (Selection state and high-level commands)
4. **Extracting Gameplay Triggers from LevelLoader**: Should checkpoint progression and goal contact evaluation move into a dedicated `LevelProgressSystem` or `ObjectiveTracker` rather than living inside `LevelLoader`?
5. **Unified Input Architecture**: Should all input (Player locomotion, editor navigation, hotkeys) be routed through a single input manager or Input Actions asset?

## Recommended order for future architectural analysis
1. **Spatial / Platform Query**: Introduce a clean spatial query abstraction (`IPlatformQueryService`) to eliminate direct access to `ColorPlatform.All` from movement resolvers and trajectory systems.
2. **Decouple Level Persistence & Gameplay Rules from `LevelLoader`**: Extract `LevelPersistenceService` (pure I/O and serialization) and `LevelGoalTriggerSystem` (win/checkpoint detection) out of `LevelLoader`.
3. **Refactor Level Editor into Command / Service Pattern**: Extract file I/O, camera panning, and handle rendering out of `LevelEditorController`. Introduce a Command pattern for editor actions (enabling Undo/Redo).
4. **Modernize UI Layer**: Replace procedural C# canvas generation with modular Prefabs or UI Toolkit, separating view binding from domain parsing.
5. **Refine GameManager**: Narrow `GameManager` to high-level state machine transitions and event dispatching, eliminating direct dependencies on concrete UI views.
