# Module: Movement

> **Namespace:** `Contrast.Player`  
> **Subsystem:** Custom Mathematical AABB Collision & Kinematics Pipeline

---

## 1. Responsibility
The **Movement** module implements the entire authoritative physics and collision engine of Contrast. It executes without using Unity's `Physics2D` solver. It resolves horizontal/vertical surface sliding, detects ground support, manages vertical jump/gravity integration, and calculates anti-stuck escape push-outs.

---

## 2. Main Files & Classes

| File Path | Primary Types | Responsibility |
|---|---|---|
| [`Assets/Script/Contrast/Player/Movement/MovementGeometry.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/MovementGeometry.cs) | `Aabb` (`internal readonly struct`) | Geometric AABB representation, overlap tests, area calculations. |
| [`Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs) | `PlayerJumpMotion` (`MonoBehaviour`, sealed) | Jump impulse generation, gravity acceleration, fall velocity clamping. |
| [`Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs) | `PlayerOverlapResolver` (`MonoBehaviour`, sealed), `OverlapResult` (`struct`) | Ejection vector calculation for trapped entities; 50% stuck threshold. |
| [`Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs) | `PlayerCollisionResolver` (`MonoBehaviour`, sealed), `CollisionResult` (`struct`) | Predictive sequential X-then-Y AABB clamping against solid platforms. |
| [`Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs) | `PlayerMovementResolver` (`MonoBehaviour`, sealed), `MovementResult` (`struct`) | Top-level pure functional pipeline combining all motion intents. |

---

## 3. Entry Points

- **`PlayerMovementResolver.Resolve(...)`**: Main pipeline entry invoked once per frame by `PlayerController.ProcessUpdate()`.
- **`PlayerJumpMotion.RequestJump()`**: Triggers jump impulse when grounded.
- **`PlayerJumpMotion.GetMoveIntent(deltaTime, isGrounded)`**: Produces vertical gravity/jump delta.
- **`PlayerCollisionResolver.Resolve(...)`**: Executes predictive clamping against `ColorPlatform.All`.
- **`PlayerOverlapResolver.Evaluate(...)`**: Inspects static overlap state before candidate motion.

---

## 4. Data
- `collisionEpsilon` ($0.0001\,\text{m}$): Skin offset to prevent numerical edge catching.
- `groundEpsilon` ($0.02\,\text{m}$): Downward tolerance for ground support detection.
- `stuckOverlapPercent` ($50.0\%$): Overlap percentage beyond which player is declared stuck.
- `overlapSpeedReductionPercent` ($80.0\%$): Control damping during ejection.
- `verticalPushSpeedMargin` ($2.0\,\text{m/s}$): Excess speed ensuring vertical push-out beats fall velocity.

---

## 5. Runtime Flow

```text
PlayerMovementResolver.Resolve()
  │
  ├── 1. OverlapResolver.Evaluate(currentBounds, playerColor)
  │        ├─ If overlap >= 50% -> Return IsStuck = true (Zero Move)
  │        └─ If overlap > 0 -> Select nearest escape edge (pushMove)
  │
  ├── 2. Combine Intents:
  │        v_combined = (inputMove * controlMultiplier) + jumpMove + pushMove + riderMove
  │
  └── 3. CollisionResolver.Resolve(..., v_combined, allowEscape)
           ├── ResolveHorizontal() -> Clamps candidate X against solid platforms
           ├── ResolveVertical() -> Clamps candidate Y from resolved X
           └── HasGroundSupport() -> Checks for platform top contact
```

---

## 6. Dependencies
- **Upstream Caller:** `PlayerController.ProcessUpdate()`.
- **Downstream Entities:** Iterates over `ColorPlatform.All` via `platform.GetAabb()` and `platform.Logical`.

---

## 7. State Ownership
- Does **not** own world positions. Operates as an immutable, pure calculation pipeline.
- `PlayerJumpMotion` owns internal vertical velocity state via `Rigidbody2D.linearVelocity` (unsimulated).

---

## 8. Important Invariants
- Resolvers must NEVER mutate `Transform` directly; return displacement structs only.
- Platform geometry queries must use `ColorPlatform.GetAabb()` which returns axis-aligned `Rect`.
- Overlapped platforms must not cancel escape push-out movement (`allowCurrentOverlapEscape`).

---

## 9. Known Problems
- High framerate drops could theoretical cause discrete tunneling if movement intent exceeds platform thickness. (Mitigated by standard 50–60Hz updates).

---

## 10. Related Modules & Documentation
- [Player Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Player.md)
- [Color Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Color.md)
- [Trajectory Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Trajectory.md)
- [Invariants](file:///y:/Contrast/My%20project/Documentation/INVARIANTS.md)
