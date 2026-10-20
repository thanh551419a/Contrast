# Project Map: Contrast

> **System Purpose:** This document is the primary navigational entry point for engineers and autonomous AI agents working on the **Contrast** codebase. It maps the overall repository layout, defines the responsibilities of each subsystem, indexes key entry points, and provides task-oriented routing across the entire documentation graph.

---

## 1. System Identity and Nature of the Project

**Contrast** is a 2D monochrome/grayscale puzzle-platformer developed in **Unity 6000.5.8f1** using C#. The core gameplay mechanic revolves around contrast and color-shifting:
- Platforms possess grayscale values classified into logical colors: **White** ($0 \le v \le 84$), **Gray** ($85 \le v \le 169$), **Black** ($170 \le v \le 254$), and **Universal / Everything** ($v = 255$).
- The player shifts their own logical color. A platform blocks the player if and only if their logical colors differ or the platform is Universal ($v = 255$). If player and platform share the same standard color, the platform is permeable/transparent to the player.
- **Custom Non-Physics Engine:** Authoritative gameplay collision, movement, and level interaction bypass Unity's `Physics2D` engine entirely. The game uses a custom deterministic AABB pipeline (`Contrast.Player.Aabb`, `PlayerMovementResolver`, `PlayerCollisionResolver`, `PlayerOverlapResolver`).
- **Parametric Moving Platforms:** Platforms can be augmented with a `Trajectory` component supporting deterministic Line and Circle paths, spatial bounds clipping, and rider-carry momentum transfer.
- **In-Engine Runtime Level Editor:** A complete runtime level editor allows creators to spawn, translate, resize, configure trajectories, test-play, delete, and serialize levels to/from JSON.

---

## 2. Global Repository Layout

```text
Contrast/
├── My project/                          # Unity Project Root
│   ├── Assets/
│   │   ├── Animation/                   # 2D character sprite sheets
│   │   ├── Editor/                      # Unity Editor unit tests and auditors
│   │   │   ├── EditorLevelTests.cs      # 58-test batchmode verification harness
│   │   │   └── PlayerPlatformAuditor.cs # Startup scene geometry logger
│   │   ├── Levels/                      # NTFS Junction -> Y:\Contrast\Code\Levels
│   │   │   ├── ContrastLevel.json       # Authored level
│   │   │   ├── map2.json                # Reconstructed stage
│   │   │   ├── SampleLevel.json         # Default fallback level
│   │   │   └── TrajectoryShowcaseLevel.json # 10-trajectory comprehensive stage
│   │   ├── Scenes/
│   │   │   └── SampleScene.unity        # Main runtime scene
│   │   ├── Script/                      # NTFS Junction -> Y:\Contrast\Code\Script
│   │   │   └── Contrast/
│   │   │       ├── Camera/              # CameraFollow and view boundaries
│   │   │       ├── Color/               # LogicalColor enum and ColorClassifier
│   │   │       ├── Core/                # GameManager singleton and GameState enum
│   │   │       ├── Data/                # LevelData, TrajectoryData DTOs
│   │   │       ├── Level/               # ColorPlatform, LevelLoader, Trajectory, LevelEditorController
│   │   │       ├── Player/              # PlayerController, PlayerInput, Movement resolvers
│   │   │       └── UI/                  # MainMenuUI, GameControlUI, GameOverUI, LevelEditorUI
│   │   └── Settings/                    # URP, Volume profiles, Input Actions
│   ├── Packages/
│   │   └── manifest.json                # Package declarations (URP, Input System, uGUI)
│   ├── ProjectSettings/
│   │   └── ProjectVersion.txt           # Unity version 6000.5.8f1
│   ├── Tests/
│   │   └── ContrastTests/               # Standalone .NET Core test harness
│   │       ├── ContrastTests.csproj     # Targets net10.0
│   │       └── Program.cs               # 91 automated tests (0 failures)
│   └── Documentation/                   # Structured Markdown Documentation System
```

---

## 3. Subsystem Breakdown and Responsibilities

| Subsystem / Module | Directory / Key Source Files | Primary Responsibility | Documentation Link |
|---|---|---|---|
| **Core State Coordinator** | `Assets/Script/Contrast/Core/` | Singleton state machine (`GameManager`), `GameState` transitions, single authoritative `Update()` tick. | [Core Module](MODULES/Core.md) |
| **Player Controller** | `Assets/Script/Contrast/Player/PlayerController.cs`, `PlayerState.cs` | Player lifecycle, state reporting (`Grounded`, `Falling`, `Stuck`, `Dead`), dispatching input to movement resolvers. | [Player Module](MODULES/Player.md) |
| **Custom Movement & Collision** | `Assets/Script/Contrast/Player/Movement/` | Mathematical AABB predictive collision, escape push-out, ground support detection, rider displacement. | [Movement Module](MODULES/Movement.md) |
| **Input Handling** | `Assets/Script/Contrast/Player/Input/PlayerInput.cs` | Dual-backend input polling (Unity Input System + Legacy Input fallback), color cycling intents. | [Player Module](MODULES/Player.md) |
| **Color Classification** | `Assets/Script/Contrast/Color/` | Maps 0..255 grayscale values to `LogicalColor` (`White`, `Gray`, `Black`, `Universal`) and UI palette colors. | [Color Module](MODULES/Color.md) |
| **Level Data Model** | `Assets/Script/Contrast/Data/` | Pure serializable DTOs (`LevelData`, `ColorPlatformData`, `TrajectoryData`) for JSON persistence. | [LevelData Module](MODULES/LevelData.md) |
| **Runtime Level Geometry** | `Assets/Script/Contrast/Level/` (`ColorPlatform`, `LevelLoader`, `LevelMarker`) | Instantiates runtime platforms, registers instances in `ColorPlatform.All`, manages spawn/checkpoints. | [LevelRuntime Module](MODULES/LevelRuntime.md) |
| **Trajectory Kinematics** | `Assets/Script/Contrast/Level/Trajectory.cs`, `TrajectorySystem.cs` | Evaluates deterministic parametric curves (`Line`, `Circle`), Liang-Barsky spatial bounds clipping, rider-carry. | [Trajectory Module](MODULES/Trajectory.md) |
| **Runtime Level Editor** | `Assets/Script/Contrast/Level/LevelEditorController.cs` | Editor camera panning, platform/marker raycast selection, gizmo handle dragging, object deletion. | [LevelEditor Module](MODULES/LevelEditor.md) |
| **Editor UI & Palette** | `Assets/Script/Contrast/UI/LevelEditorUI.cs`, `PlatformColorSubComponent.cs` | Programmatic uGUI scrollable inspector, dynamic trajectory panel, 2-step deletion confirmation, palette dragging. | [EditorUI Module](MODULES/EditorUI.md) |
| **Game UI Overlays** | `Assets/Script/Contrast/UI/MainMenuUI.cs`, `GameControlUI.cs`, `GameOverui.cs` | Start menu, in-game Restart/Retry HUD, Game Over and Victory screens. | [GameUI Module](MODULES/GameUI.md) |
| **Camera Controller** | `Assets/Script/Contrast/Camera/CameraFollow.cs` | Smoothly tracks player target, clamped within rectangular `CameraBoundaryData`. | [Camera Module](MODULES/Camera.md) |

---

## 4. Key Architectural Entry Points

### 4.1 Runtime Entry Point
- **Root Scene:** [`Assets/Scenes/SampleScene.unity`](file:///y:/Contrast/My%20project/Assets/Scenes/SampleScene.unity)
- **Coordinator:** [`Contrast.Core.GameManager`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) attached to GameObject `GameSystems`.
- **Boot Sequence:**
  1. `GameManager.Awake()` discovers or instantiates all UI and controller subsystems.
  2. `GameManager.Start()` transitions to `GameState.MainMenu` via `SetState(GameState.MainMenu)`.
  3. Clicking **PLAY GAME** triggers `GameManager.StartGame()`, which invokes `LevelLoader.LoadAndInitializeLevel()`.
  4. Clicking **EDIT LEVEL** triggers `GameManager.OpenEditor()`, which invokes `LevelEditorController.StartEditing()`.

### 4.2 Gameplay Update Loop
The single gameplay update pump resides exclusively in [`GameManager.Update()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L80-L111):
```text
GameManager.Update() (only when State == GameState.Playing)
  │
  ├── 1. TrajectorySystem.UpdateAll(Time.deltaTime)          [Advance platform AABBs]
  │        └─ Trajectory.ProcessUpdate(dt) for each platform
  │
  ├── 2. TrajectorySystem.GetRiderDisplacement(...)          [Calculate rider momentum]
  │        └─ player.SetRiderDisplacement(riderDisp)
  │
  ├── 3. player.ProcessUpdate(Time.deltaTime)                [Predictive collision & move]
  │        ├─ PlayerInput.ReadInput(...)
  │        ├─ PlayerMovementResolver.Resolve(...)
  │        │    ├─ PlayerOverlapResolver.Evaluate(...)       [Push-out / Stuck check]
  │        │    └─ PlayerCollisionResolver.Resolve(...)      [Predictive AABB slide]
  │        └─ player.ApplyMove(result.FinalMove)             [Centralized transform write]
  │
  ├── 4. levelLoader.ProcessPlayerInteractions(player)       [Checkpoint & End contacts]
  │
  └── 5. cameraFollow.ProcessCameraUpdate()                  [Boundary-clamped follow]
```

---

## 5. Task → Documentation Routing

Autonomous agents and developers should consult the following table to route directly to relevant documentation for their assigned task:

| Task Type | Primary Documents to Read | Secondary Modules |
|---|---|---|
| **Understand overall system** | [00_PROJECT_MAP.md](00_PROJECT_MAP.md), [01_PROJECT_OVERVIEW.md](01_PROJECT_OVERVIEW.md), [02_ARCHITECTURE.md](02_ARCHITECTURE.md) | [INVARIANTS.md](INVARIANTS.md) |
| **Modify Player physics / jump / collision** | [03_RUNTIME_FLOW.md](03_RUNTIME_FLOW.md), [02_ARCHITECTURE.md](02_ARCHITECTURE.md) | [Player Module](MODULES/Player.md), [Movement Module](MODULES/Movement.md) |
| **Modify Moving Platforms / Trajectories** | [02_ARCHITECTURE.md](02_ARCHITECTURE.md), [04_DATA_MODEL.md](04_DATA_MODEL.md) | [Trajectory Module](MODULES/Trajectory.md), [LevelRuntime Module](MODULES/LevelRuntime.md) |
| **Modify Level Editor Viewport / Gizmos** | [07_EDITOR_ARCHITECTURE.md](07_EDITOR_ARCHITECTURE.md), [LEVEL_EDITOR_DEVELOPER_GUIDE.md](LEVEL_EDITOR_DEVELOPER_GUIDE.md) | [LevelEditor Module](MODULES/LevelEditor.md) |
| **Modify Level Editor UI / Panels** | [07_EDITOR_ARCHITECTURE.md](07_EDITOR_ARCHITECTURE.md), [LEVEL_EDITOR_DEVELOPER_GUIDE.md](LEVEL_EDITOR_DEVELOPER_GUIDE.md) | [EditorUI Module](MODULES/EditorUI.md) |
| **Modify Level JSON Schema / Persistence** | [04_DATA_MODEL.md](04_DATA_MODEL.md), [13_EXTENSION_GUIDE.md](13_EXTENSION_GUIDE.md) | [LevelData Module](MODULES/LevelData.md) |
| **Add a New Platform Property or Curve** | [13_EXTENSION_GUIDE.md](13_EXTENSION_GUIDE.md), [INVARIANTS.md](INVARIANTS.md) | [Trajectory Module](MODULES/Trajectory.md), [LevelEditor Module](MODULES/LevelEditor.md) |
| **Investigate Bugs or Strange Behavior** | [11_KNOWN_ISSUES.md](11_KNOWN_ISSUES.md), [INVARIANTS.md](INVARIANTS.md) | [09_DEPENDENCY_GRAPH.md](09_DEPENDENCY_GRAPH.md) |
| **Execute Tests & Validate Changes** | [10_TEST_AND_VALIDATION.md](10_TEST_AND_VALIDATION.md) | [15_DOCUMENTATION_WORK_LOG.md](15_DOCUMENTATION_WORK_LOG.md) |
| **Check Scene Wiring & Hierarchies** | [06_SCENE_AND_HIERARCHY.md](06_SCENE_AND_HIERARCHY.md) | [MODULES/Core.md](MODULES/Core.md) |
| **Lookup Class / Symbol References** | [08_FILE_SYMBOL_INDEX.md](08_FILE_SYMBOL_INDEX.md) | [09_DEPENDENCY_GRAPH.md](09_DEPENDENCY_GRAPH.md) |

---

## 6. Feature → Module Routing

| Feature | Primary Module Document | Associated Files |
|---|---|---|
| **Color Shifting & Filtering** | [Color Module](MODULES/Color.md) | `ColorClassifier.cs`, `LogicalColor.cs` |
| **AABB Collision Slide** | [Movement Module](MODULES/Movement.md) | `PlayerCollisionResolver.cs`, `MovementGeometry.cs` |
| **In-Wall Overlap Push-Out** | [Movement Module](MODULES/Movement.md) | `PlayerOverlapResolver.cs` |
| **Jump & Gravity Kinematics** | [Movement Module](MODULES/Movement.md) | `PlayerJumpMotion.cs` |
| **Moving Platforms (Line / Circle)** | [Trajectory Module](MODULES/Trajectory.md) | `Trajectory.cs`, `TrajectoryData.cs` |
| **Rider Momentum Carry** | [Trajectory Module](MODULES/Trajectory.md) | `TrajectorySystem.cs`, `PlayerController.cs` |
| **Checkpoints & Spawn Retry** | [LevelRuntime Module](MODULES/LevelRuntime.md) | `LevelLoader.cs`, `LevelMarker.cs` |
| **Editor Object Selection & Drag** | [LevelEditor Module](MODULES/LevelEditor.md) | `LevelEditorController.cs` |
| **Interactive Trajectory Handles** | [LevelEditor Module](MODULES/LevelEditor.md) | `LevelEditorController.cs`, `LevelEditorUI.cs` |
| **Two-Step Platform Deletion** | [EditorUI Module](MODULES/EditorUI.md) | `LevelEditorUI.cs`, `LevelEditorController.cs` |
| **Instant Test Play Bridge** | [Core Module](MODULES/Core.md) | `GameManager.cs`, `LevelEditorController.cs` |
| **JSON Export & Import** | [LevelData Module](MODULES/LevelData.md) | `LevelData.cs`, `LevelEditorController.cs`, `LevelLoader.cs` |
