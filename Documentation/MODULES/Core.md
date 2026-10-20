# Module: Core

> **Namespace:** `Contrast.Core`  
> **Subsystem:** Master Game State & Loop Coordination

---

## 1. Responsibility
The **Core** module is the central orchestrator of the entire Contrast game application. It manages the global state machine (`GameState`), initializes and coordinates all presentation and gameplay subsystems, and hosts the **single authoritative `Update()` tick** for all active gameplay mechanics.

---

## 2. Main Files & Classes

| File Path | Primary Classes / Enums | Responsibility |
|---|---|---|
| [`Assets/Script/Contrast/Core/GameManager.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs) | `GameManager` (`MonoBehaviour`, sealed) | Singleton coordinator (`Instance`), state transitions, single update pipeline. |
| [`Assets/Script/Contrast/Core/GameState.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameState.cs) | `GameState` (`enum`) | High-level application states: `MainMenu`, `Playing`, `Editing`, `GameOver`, `Won`. |

---

## 3. Entry Points

- **`GameManager.Awake()`**: Singleton instance assignment, dynamic fallback instantiation of UI and controller subsystems.
- **`GameManager.Start()`**: Bootstraps game into `GameState.MainMenu`.
- **`GameManager.Update()`**: Authoritative frame update pump when `State == GameState.Playing`.
- **`GameManager.StartGame()`**: Launches standard level gameplay.
- **`GameManager.StartGameWithLevelData(LevelData data)`**: Launches gameplay with in-memory level data from the Level Editor.
- **`GameManager.OpenEditor()`**: Switches state to `GameState.Editing`.

---

## 4. Data
- `State` (`GameState`): Current operational mode.
- `KillY` (`float`): Fall-death plane elevation, queried from `LevelLoader`.
- Cached references to `PlayerController`, `LevelLoader`, `CameraFollow`, and UI controllers.

---

## 5. Runtime Flow

```text
GameManager.Update() (State == GameState.Playing)
  │
  ├── 1. TrajectorySystem.UpdateAll(Time.deltaTime)
  ├── 2. TrajectorySystem.GetRiderDisplacement(...) -> player.SetRiderDisplacement(...)
  ├── 3. player.ProcessUpdate(Time.deltaTime)
  ├── 4. levelLoader.ProcessPlayerInteractions(player)
  └── 5. cameraFollow.ProcessCameraUpdate()
```

---

## 6. Editor Flow
- Calling `GameManager.OpenEditor()`:
  - Sets `State = GameState.Editing`.
  - Disables `player.gameObject`.
  - Calls `levelEditor.StartEditing()` and `levelEditorUI.Show(true)`.

---

## 7. Dependencies
- **Downstream Consumers:** `MainMenuUI`, `GameOverUI`, `GameControlUI`, `LevelEditorController`, `PlayerController`.
- **Upstream Dependencies:** Calls `TrajectorySystem`, `PlayerController`, `LevelLoader`, `CameraFollow`.

---

## 8. State Ownership
- Owns `GameManager.State`.
- Authoritative for switching between menu, gameplay, and editing modes.

---

## 9. Important Invariants
- `GameManager.Update()` is the **only** permitted update pump for gameplay physics and kinematics.
- Trajectory advancement must occur strictly before player movement resolution.

---

## 10. Known Problems & Technical Debt
- `SampleScene.unity` has a missing component on the `GameSystems` GameObject referencing deleted `Contrast.UI.UIManager`. (Benign; handled dynamically in code).

---

## 11. Related Modules & Documentation
- [Architecture](file:///y:/Contrast/My%20project/Documentation/02_ARCHITECTURE.md)
- [Runtime Flow](file:///y:/Contrast/My%20project/Documentation/03_RUNTIME_FLOW.md)
- [Invariants](file:///y:/Contrast/My%20project/Documentation/INVARIANTS.md)
- [Player Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Player.md)
- [LevelRuntime Module](file:///y:/Contrast/My%20project/Documentation/MODULES/LevelRuntime.md)
