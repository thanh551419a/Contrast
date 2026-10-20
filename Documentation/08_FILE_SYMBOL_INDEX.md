# File & Symbol Index: Contrast

> **Target Audience:** Autonomous AI agents and developers seeking an exhaustive, symbol-level dictionary of all classes, structs, enums, interfaces, and primary methods across the codebase, complete with caller/callee cross-references.

---

## 1. Core & Game State Subsystem

### `Assets/Script/Contrast/Core/GameState.cs`
- **Namespace:** `Contrast.Core`
- **Enums:**
  - `GameState`: `MainMenu`, `Playing`, `Editing`, `GameOver`, `Won`.
- **Responsibility:** High-level state enumeration for global application lifecycle.
- **Related Files:** `GameManager.cs`.

### `Assets/Script/Contrast/Core/GameManager.cs`
- **Namespace:** `Contrast.Core`
- **Classes:**
  - `GameManager` (`MonoBehaviour`, sealed)
- **Important Properties & Fields:**
  - `Instance` (`static GameManager`, public getter, private setter)
  - `State` (`GameState`, public getter)
  - `KillY` (`float`, public getter, reads `levelLoader.KillY`)
  - `Player` (`PlayerController`, public getter)
  - `LevelLoader` (`LevelLoader`, public getter)
  - `CameraFollow` (`CameraFollow`, public getter)
- **Important Methods:**
  - `Awake()`: Singleton enforcement, auto-discovery or instantiation of UI/controller objects.
  - `Start()`: Initializes `GameState.MainMenu`.
  - `Update()`: Single authoritative gameplay loop. Advances `TrajectorySystem`, evaluates rider displacement, invokes `player.ProcessUpdate()`, evaluates interactions, and updates camera.
  - `StartGame()`: Enters play mode and triggers level loading.
  - `StartGameWithLevelData(LevelData data)`: Direct in-memory play mode bridge for Level Editor.
  - `OpenEditor()`: Enters edit mode, disables player, activates editor controller and UI.
  - `ReturnToMainMenu()`: Tears down editor/gameplay and returns to title menu.
  - `OnPlayerDied()`: Sets state to `GameOver` and triggers `GameOverUI`.
  - `OnPlayerWon()`: Sets state to `Won` and triggers `GameOverUI`.
  - `Restart()`: Reloads entire level from scratch via `LevelLoader`.
  - `Retry()`: Respawns player at latest checkpoint via `LevelLoader.RetryCurrentSpawn()`.
- **Callers:** `MainMenuUI`, `GameOverUI`, `GameControlUI`, `LevelEditorController`, `PlayerController`.
- **Callees:** `TrajectorySystem`, `PlayerController`, `LevelLoader`, `CameraFollow`, all UI controllers.

---

## 2. Color Classification Subsystem

### `Assets/Script/Contrast/Color/LogicalColor.cs`
- **Namespace:** `Contrast.Color`
- **Enums:**
  - `LogicalColor`: `White = 0`, `Gray = 1`, `Black = 2`, `Universal = 3`.
- **Responsibility:** Enumerates discrete platform and player color states.

### `Assets/Script/Contrast/Color/ColorClassifier.cs`
- **Namespace:** `Contrast.Color`
- **Classes:**
  - `ColorClassifier` (`static class`)
- **Important Constants:**
  - `WhiteMin = 0f`, `WhiteMax = 84f`, `GrayMin = 85f`, `GrayMax = 169f`, `BlackMin = 170f`, `BlackMax = 254f`, `UniversalValue = 255f`.
- **Important Methods:**
  - `Classify(float raw)`: Maps grayscale float to `LogicalColor`.
  - `DefaultValue(LogicalColor c)`: Returns representative value ($0, 127, 212, 255$).
  - `ToValue(LogicalColor c)`: Returns float value for shaders/sprites.
  - `GetPlatformVisualColor(float raw)`: Returns `UnityEngine.Color` tint (Universal = Gold/Amber `RGB(0.96, 0.78, 0.22)`).
- **Callers:** `ColorPlatform`, `PlayerController`, `LevelMarker`, `PlatformColorSubComponent`.

---

## 3. Data & Schema Models

### `Assets/Script/Contrast/Data/LevelData.cs`
- **Namespace:** `Contrast.Data`
- **Classes:**
  - `LevelData`: Root schema containing `startPos`, `checkPoint`, `end`, `spawnPoint`, `cameraBoundary`, `platforms`.
  - `StartPosData`: Player spawn `position`, `size`, `color`.
  - `CheckPointData`: Checkpoint trigger `position`, `size`, `color`.
  - `EndData`: Level end area `position`, `size`.
  - `SpawnPointData`: Legacy spawn point representation.
  - `ColorPlatformData`: Platform `position`, `size`, `rawGrayscaleColor`, optional `trajectory`.
  - `CameraBoundaryData`: View boundary `position`, `size`.
  - `Vector2Data`: Serializable 2D coordinate container with `ToVector2()`.
- **Responsibility:** Pure data transfer objects for JSON persistence.

### `Assets/Script/Contrast/Data/TrajectoryData.cs`
- **Namespace:** `Contrast.Data`
- **Enums:**
  - `TrajectoryCurveType`: `Line = 0`, `Circle = 1`.
  - `TrajectoryDomainMode`: `ParameterRange = 0`, `SpatialBounds = 1`.
  - `TrajectoryPlaybackMode`: `Loop = 0`, `PingPong = 1`, `Once = 2`.
- **Classes:**
  - `TrajectoryData`: DTO holding all parametric curve settings.
- **Important Methods:**
  - `CreateDefault(Vector2 platformPosition)`: Factory for initial stationary configuration.
  - `Clone()`: Creates deep copy to isolate editor mutations.

---

## 4. Player & Movement Subsystem

### `Assets/Script/Contrast/Player/PlayerState.cs`
- **Namespace:** `Contrast.Player`
- **Enums:**
  - `PlayerState`: `Grounded`, `Falling`, `Stuck`, `Dead`.

### `Assets/Script/Contrast/Player/PlayerController.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerController` (`MonoBehaviour`, sealed)
- **Important Methods:**
  - `ProcessUpdate(float deltaTime)`: Authoritative gameplay update called from `GameManager`. Reads input, delegates to movement resolvers, checks `KillY`.
  - `ApplyMove(Vector2 finalMove)`: The single normal gameplay method that writes to `transform.position`.
  - `SetRiderDisplacement(Vector2 disp)`: Receives momentum from `TrajectorySystem`.
  - `SetColor(LogicalColor color)`: Updates active player color and visual tint.
  - `StepColorChunk(int direction)`: Cycles player color forward or backward.
  - `Respawn(Vector2 pos, LogicalColor color)`: Repositions character on start/checkpoint.

### `Assets/Script/Contrast/Player/Input/PlayerInput.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerInput` (`MonoBehaviour`, sealed)
- **Structs:**
  - `PlayerInputIntent` (readonly struct)
- **Important Methods:**
  - `ReadInput(float deltaTime, float moveSpeed, bool isGrounded)`: Returns immutable `PlayerInputIntent`.

### `Assets/Script/Contrast/Player/Movement/MovementGeometry.cs`
- **Namespace:** `Contrast.Player`
- **Structs:**
  - `Aabb` (readonly struct): Axis-aligned bounding box defined by `Min` and `Max`. Exposes `Center`, `Size`, `FromCenter()`, `Overlaps()`, `OverlapArea()`.

### `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerJumpMotion` (`MonoBehaviour`, sealed)
- **Important Methods:**
  - `RequestJump()`: Computes initial vertical jump velocity $v_y = \sqrt{2 g h}$.
  - `GetMoveIntent(float deltaTime, bool isGrounded)`: Integrates gravity and clamps at `maxFallSpeed`.
  - `NotifyGrounded(bool grounded)`: Clears vertical velocity upon landing.

### `Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerOverlapResolver` (`MonoBehaviour`, sealed)
- **Structs:**
  - `OverlapResult` (readonly struct)
- **Important Methods:**
  - `Evaluate(Aabb playerBounds, LogicalColor playerColor)`: Calculates overlap percentage with blocking platforms; flags `IsStuck` if $\ge 50\%$, or computes shortest escape push vector.
  - `GetPushSpeed(Vector2 pushDirection, float currentVerticalSpeed)`: Ensures vertical push velocity beats downward fall speed.

### `Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerCollisionResolver` (`MonoBehaviour`, sealed)
- **Structs:**
  - `CollisionResult` (readonly struct)
- **Important Methods:**
  - `Resolve(...)`: Sequential X-then-Y predictive resolution against `ColorPlatform.All`.
  - `ResolveHorizontal()`, `ResolveVertical()`: Clamps candidate AABB against solid boundaries.
  - `HasGroundSupport()`: Verifies downward contact within `groundEpsilon`.

### `Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs`
- **Namespace:** `Contrast.Player`
- **Classes:**
  - `PlayerMovementResolver` (`MonoBehaviour`, sealed)
- **Structs:**
  - `MovementResult` (readonly struct)
- **Important Methods:**
  - `Resolve(...)`: Pure calculation pipeline combining inputs, jump intent, push-out intent, and rider displacement, yielding one final collision-resolved vector.

---

## 5. Level & Trajectory Subsystem

### `Assets/Script/Contrast/Level/ColorPlatform.cs`
- **Namespace:** `Contrast.Level`
- **Classes:**
  - `ColorPlatform` (`MonoBehaviour`, marked `[ExecuteAlways]`)
- **Important Static Fields:**
  - `All` (`List<ColorPlatform>`): Live registry of all platform instances.
- **Important Methods:**
  - `Initialize(...)`: Configures transform, color, size, and adds to `All`.
  - `SetEditorData(...)`: Live property mutation during editor dragging/resizing.
  - `GetAabb()`: Authoritative axis-aligned bounding box `Rect`.
  - `InitializeTrajectory(TrajectoryData data)`: Ensures `Trajectory` component is configured.
  - `OnDestroy()`: Removes `this` from `All` registry.

### `Assets/Script/Contrast/Level/LevelMarker.cs`
- **Namespace:** `Contrast.Level`
- **Enums:**
  - `LevelMarkerType`: `StartPos`, `CheckPoint`, `End`.
- **Classes:**
  - `LevelMarker` (`MonoBehaviour`, sealed)
- **Important Static Fields:**
  - `All` (`List<LevelMarker>`): Live registry of level markers.
- **Important Methods:**
  - `Initialize(...)`: Sets marker type, position, bounds, and alpha visual.
  - `OnPlayerTouched()`: Consumes checkpoint/end trigger and destroys instance.

### `Assets/Script/Contrast/Level/LevelLoader.cs`
- **Namespace:** `Contrast.Level`
- **Classes:**
  - `LevelLoader` (`MonoBehaviour`)
- **Important Methods:**
  - `LoadAndInitializeLevel()`: Reads level JSON from asset/disk and invokes `BuildRuntimeLevel()`.
  - `BuildRuntimeLevel(LevelData data)`: Reconstructs platforms, markers, camera boundary, and spawns player.
  - `RetryCurrentSpawn()`: Fast respawn at active checkpoint without disk re-parsing.
  - `ProcessPlayerInteractions(PlayerController player)`: Evaluates contacts with `LevelMarker.All`.

### `Assets/Script/Contrast/Level/Trajectory.cs`
- **Namespace:** `Contrast.Level`
- **Classes:**
  - `Trajectory` (`MonoBehaviour`, sealed)
- **Important Methods:**
  - `ProcessUpdate(float deltaTime)`: Advances phase, evaluates curve, moves platform, and sets `FrameDisplacement`.
  - `EvaluateWorldPoint(TrajectoryData cfg, float normalizedPhase)`: Static pure math evaluator for Line and Circle points.
  - `GetInitialPosition(TrajectoryData cfg)`: Authoritative start point ($t=0$ or $1$ if reverse).
  - `ClipLineAgainstBounds(TrajectoryData cfg, out float pMin, out float pMax)`: Liang-Barsky line clipper.
  - `ProjectPointOntoTrajectory(TrajectoryData cfg, Vector2 point, out float normalizedT)`: Orthogonal cursor-to-curve projection.
  - `Validate(TrajectoryData cfg)`: Mathematical sanity validation.

### `Assets/Script/Contrast/Level/TrajectorySystem.cs`
- **Namespace:** `Contrast.Level`
- **Classes:**
  - `TrajectorySystem` (`static class`)
- **Important Methods:**
  - `UpdateAll(float deltaTime)`: Advances all active trajectories registered in `ColorPlatform.All`.
  - `ResetAll()`: Snaps all trajectories back to phase 0 (or 1 if reverse).
  - `GetRiderDisplacement(...)`: Evaluates whether the player stands on top of a moving platform.

---

## 6. Level Editor & UI Subsystem

### `Assets/Script/Contrast/Level/LevelEditorController.cs`
- **Namespace:** `Contrast.Level`
- **Classes:**
  - `LevelEditorController` (`MonoBehaviour`, sealed)
- **Important Methods:**
  - `StartEditing()`, `StopEditing()`: Editor state transitions.
  - `SelectPlatform(ColorPlatform p)`: Selects platform, updates UI, and shows gizmos.
  - `ApplySelectedPlatformFromUI(...)`: Ingests numerical inspector edits.
  - `DeleteSelectedPlatform()`: Safely destroys selected platform and deregisters from `ColorPlatform.All`.
  - `RequestExport()`, `RequestImport()`: File I/O serialization handlers.
  - `RequestPlayLevel()`: In-memory test-play bridge.
  - `RefreshHandles()`: Updates positions of 4 resize and trajectory visual handles.

### `Assets/Script/Contrast/UI/LevelEditorUI.cs`
- **Namespace:** `Contrast.UI`
- **Classes:**
  - `LevelEditorUI` (`MonoBehaviour`, sealed)
- **Important Methods:**
  - `BuildUI()`: Procedural generation of scrollable inspector, toolbar, help bar, and palette.
  - `SetSelectedPlatform(ColorPlatform p)`: Populates inspector fields.
  - `UpdateTrajectorySummary()`: Calculates live trajectory length, duration, and speed in m/s.
  - `OnDeleteButtonClicked()`: Two-step deletion logic with 3-second confirmation timeout.

### `Assets/Script/Contrast/UI/PlatformColorSubComponent.cs`
- **Namespace:** `Contrast.UI`
- **Classes:**
  - `PlatformColorSubComponent` (`sealed class`)
- **Important Methods:**
  - `BuildUI(...)`: Creates color dropdown, raw float field, visual swatch, and quick preset buttons.
  - `SetRawColor(float rawColor, bool notify)`: Synchronizes UI elements with platform color.
