# Module: Player

> **Namespace:** `Contrast.Player`  
> **Subsystem:** Player Entity, Input Processing & Lifecycle

---

## 1. Responsibility
The **Player** module defines the primary playable character. It encapsulates input reading, color cycle state, death/respawn triggers, and serves as the single authority for mutating the character's `transform.position` during gameplay.

---

## 2. Main Files & Classes

| File Path | Primary Classes / Structs / Enums | Responsibility |
|---|---|---|
| [`Assets/Script/Contrast/Player/PlayerController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | `PlayerController` (`MonoBehaviour`, sealed) | Player entity entry point, state coordination, `ApplyMove()` execution. |
| [`Assets/Script/Contrast/Player/PlayerState.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerState.cs) | `PlayerState` (`enum`) | Operational state: `Grounded`, `Falling`, `Stuck`, `Dead`. |
| [`Assets/Script/Contrast/Player/Input/PlayerInput.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Input/PlayerInput.cs) | `PlayerInput` (`MonoBehaviour`, sealed), `PlayerInputIntent` (`readonly struct`) | Polling keyboard input across Input System and Legacy backends. |

---

## 3. Entry Points

- **`PlayerController.ProcessUpdate(float deltaTime)`**: Called exclusively by `GameManager.Update()`.
- **`PlayerController.ApplyMove(Vector2 finalMove)`**: The single point of normal gameplay transform translation.
- **`PlayerController.SetRiderDisplacement(Vector2 disp)`**: Receives moving platform momentum from `GameManager`.
- **`PlayerController.Respawn(Vector2 pos, LogicalColor color)`**: Positions and recolors player upon start/checkpoint retry.
- **`PlayerController.SetColor(LogicalColor color)`**: Sets active logical color and updates sprite visual.
- **`PlayerController.StepColorChunk(int direction)`**: Advances or reverses color cycling.

---

## 4. Data
- `State` (`PlayerState`): Current movement/life state.
- `CurrentColor` (`LogicalColor`): Active logical color (`White`, `Gray`, `Black`).
- `CurrentColorValue` (`float`): Raw grayscale brightness (0..254).
- `PlayerSize` (`Vector2`): AABB dimensions ($1 \times 1$ by default).
- `PlayerOffset` (`Vector2`): Center offset relative to transform position.
- `riderDisplacement` (`Vector2`): Transient platform delta injected each tick.

---

## 5. Runtime Flow

```text
GameManager.Update()
  ↓
PlayerController.SetRiderDisplacement(disp)
  ↓
PlayerController.ProcessUpdate(deltaTime)
  ├── 1. PlayerInput.ReadInput(...) -> PlayerInputIntent
  ├── 2. StepColorChunk() / SetColor() if input requested
  ├── 3. jumpMotion.RequestJump() if JumpRequested
  ├── 4. jumpMotion.GetMoveIntent(...) -> jumpIntent
  ├── 5. movementResolver.Resolve(..., input.Move, jumpIntent, riderDisp) -> MovementResult
  ├── 6. ApplyMove(result.FinalMove)
  └── 7. Die() if transform.position.y < KillY
```

---

## 6. Editor Flow
- Disabled during edit mode (`player.gameObject.SetActive(false)`).
- Re-activated when exiting editor or launching **TEST PLAY**.

---

## 7. Dependencies
- **Upstream Caller:** `GameManager.Update()`, `LevelLoader.BuildRuntimeLevel()`.
- **Downstream Collaborators:** `PlayerMovementResolver`, `PlayerJumpMotion`, `PlayerInput`, `SpriteRenderer`.

---

## 8. State Ownership
- Owns `PlayerController.transform.position`, `State`, `CurrentColor`, and `CurrentColorValue`.

---

## 9. Important Invariants
- `ApplyMove()` is the **only** normal method allowed to mutate `transform.position`.
- `Rigidbody2D` must remain unsimulated (`simulated = false`).

---

## 10. Known Problems
- In `SampleScene.unity`, `Player` scale is set to $(0.3, 0.3, 1)$ with `playerSize` matching the $(1, 1)$ default. AABB collision geometry is determined by `playerSize`.

---

## 11. Related Modules & Documentation
- [Movement Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Movement.md)
- [Color Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Color.md)
- [Runtime Flow](file:///y:/Contrast/My%20project/Documentation/03_RUNTIME_FLOW.md)
- [Invariants](file:///y:/Contrast/My%20project/Documentation/INVARIANTS.md)
