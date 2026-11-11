# CONTRAST — MOVING PLATFORM JITTER ROOT CAUSE ANALYSIS

## 1. Executive Summary

When the **Player** stands on, walks along, or lands on a moving platform (`ColorPlatform` driven by `Trajectory` and `TrajectorySystem`), intermittent vertical popping, horizontal micro-stutters, sudden sideways kicks near platform edges, and `Grounded` $\leftrightarrow$ `Falling` state oscillation occur whenever the supporting platform has an upward velocity component ($\Delta P_y > \epsilon_{\text{col}} = 0.0001$) or sweeps across the Player's feet.

Inspection of the runtime movement and collision pipeline (`GameManager.cs`, `TrajectorySystem.cs`, `PlayerController.cs`, `PlayerMovementResolver.cs`, `PlayerOverlapResolver.cs`, `PlayerCollisionResolver.cs`, `PlayerVisualPresentation.cs`, and `CameraFollow.cs`) identified **three coupled root causes**:

1. **Root Cause 1 (Primary Jitter Driver) — Pre-Carry Overlap Evaluation Against Post-Move Platform Geometry (`PlayerMovementResolver.cs` + `PlayerOverlapResolver.cs`):**
   - `GameManager.Update()` calls `TrajectorySystem.UpdateAll(Time.deltaTime)` **before** `player.StepMovement(Time.deltaTime, riderMove)`.
   - `TrajectorySystem.UpdateAll()` immediately mutates `platform.transform.position` to its new Frame $N$ position $\mathbf{P}(t + \Delta t) = \mathbf{P}(t) + \Delta \mathbf{P}$.
   - Inside `PlayerMovementResolver.Resolve()`, `currentBounds` was constructed from `currentPosition` (**Frame $N-1$ player position, BEFORE `riderMove` $\Delta \mathbf{P}$ is applied**) and passed to `overlapResolver.Evaluate(currentBounds, playerColor)`, which queries `platform.GetAabb()` (**Frame $N$ platform position, AFTER $\Delta \mathbf{P}$ is applied**).
   - Whenever the platform moves upward by $\Delta P_y > 0.0001$, the platform's new top surface `platformBounds.Max.y` is now above the player's pre-carry feet `currentBounds.Min.y` by $\Delta P_y - 0.0001$.
   - `PlayerOverlapResolver.Evaluate()` falsely detects interior AABB penetration (`overlapArea > 0`, `IsPushingOut = true`), triggering **four simultaneous jitter artifacts**:
     1. **80% Horizontal Walk Speed Stall:** `overlapResolver.GetControlMultiplier(true)` returns `0.20f` (`overlapSpeedReductionPercent = 80f`), cutting player horizontal walk input `adjustedInput` by 80% on every overlap frame.
     2. **Uncommanded Vertical Pop (`+2.0 * dt` on top of `riderMove.y`):** Away from platform edges, `bestPushDirection = Vector2.up`, adding `pushMove = (0, +2.0 * dt)` (`+0.0333` units at 60 FPS) **in addition to** `riderMove.y = +\Delta P_y`. This launches the player's feet to `platformBounds.Max.y + 0.0334 > groundEpsilon (0.02)`.
     3. **Uncommanded Horizontal Kick Near Platform Edges (`±2.0 * dt`):** In `PlayerOverlapResolver.cs:118-133`, `left = |center.x - platformBounds.Min.x|` and `right = |platformBounds.Max.x - center.x|` are compared against `up = |platformBounds.Max.y - center.y|` (`~0.20` units). Whenever the player stands within `0.20` units of the left or right platform edge on a rising platform, `left < up` (or `right < up`), switching `bestPushDirection` to `Vector2.left` or `Vector2.right` and kicking the player sideways (often opposite to platform motion).
     4. **3-Frame `Grounded` $\leftrightarrow$ `Falling` Oscillation & Camera/Visual Shaking:** Because `pushMove.y` launches the player to `verticalGap = 0.0334 > groundEpsilon (0.02)`, `HasGroundSupport()` returns `false` and `IsGrounded` flips to `false` (`PlayerState.Falling`). On the next frame, `PlayerVisualPresentation.UpdateSquashAndStretch()` stretches the sprite to `(0.82, 1.25, 1)`, `GetRiderDisplacement` is skipped because `IsGrounded == false`, gravity pulls the player back down onto the rising platform (`IsGrounded = true`, landing squash `(1.22, 0.78, 1)`), and `CameraFollow` (which tracks `player.transform.position` 1:1) shakes vertically every 3 frames.

2. **Root Cause 2 — Axis-Order Self-Blocking During Rising Diagonal/Circular Carry (`PlayerCollisionResolver.cs`):**
   - `PlayerCollisionResolver.Resolve()` always resolved X first (`ResolveHorizontal`), then Y (`ResolveVertical`) from `currentPosition`.
   - When `riderMove` had both horizontal and upward components ($\Delta P_x \neq 0, \Delta P_y > 0$, such as the rising quadrants of Newton circular rails `K` and `I` in `L01–L03`), `ResolveHorizontal` ran while `current.Min.y` was still $\Delta P_y$ below the supporting platform's new `Max.y`. If the player stood near the trailing edge or walked onto a rising platform, `ResolveHorizontal` could collide against the supporting platform's side before `ResolveVertical` lifted the player by $\Delta P_y$.

3. **Root Cause 3 — Swept Top-Surface Rejection When Landing on a Rising Platform (`PlayerCollisionResolver.cs`) & Sticky Jump Carrier (`TrajectorySystem.cs`):**
   - When an airborne player fell (`moveY < 0`) onto a rising platform (`traj.FrameDisplacement.y > 0`), `TrajectorySystem.UpdateAll()` could sweep `platformBounds.Max.y` upward past `current.Min.y` (`current.Min.y < platformBounds.Max.y`) even though the player's feet were above the platform's previous top surface (`current.Min.y >= prevMaxY - collisionEpsilon`).
   - `PlayerCollisionResolver.ResolveVertical()` checked `if (current.Min.y >= platformBounds.Max.y - collisionEpsilon)` and skipped the downward landing (`continue`), causing the falling player to sink into the rising platform and bob upward via overlap push-out.
   - Conversely, when jumping off a moving platform, `TrajectorySystem.GetRiderDisplacement()` still added one frame of carrier displacement (`riderMove`) on the jump takeoff frame because `player.IsGrounded` was still `true` before `Resolve()` ran.

---

## 2. Affected Gameplay Entities & Platform Types

| Category | Affected Entities / Platforms | Why Affected |
| :--- | :--- | :--- |
| **Gameplay Object** | `PlayerController` (`Player` entity + `PlayerVisualPresentation` + `CameraFollow`) | Only dynamic rider using `PlayerMovementResolver` / `PlayerOverlapResolver` / `PlayerCollisionResolver`. |
| **Newton Orbital Platforms (`L01`, `L02`, `L03`)** | `Newton_Rail_K` and `Newton_Rail_I` (`Circle`, `Loop`, `rawGrayscaleColor = 255`) | During the rising half of every circular orbit ($\Delta P_y > 0$), pre-carry overlap evaluation triggered false push-outs, edge kicks, and `Grounded`/`Falling` oscillation. |
| **L03 Authored Moving Platforms** | Platform `#3` (`Line`, vertical `PingPong`, `rotation = 90°`), Platform `#11` (`Line`, horizontal `PingPong`), Platform `#16` (`Line`, horizontal `PingPong`) | Platform `#3` moves vertically ($\Delta P_y > 0$ on every upward half-cycle); Platforms `#11` and `#16` exhibited sticky jump carry on takeoff and swept-landing edge cases. |
| **Custom / Showcase Moving Platforms** | Any `Line` (vertical/diagonal) or `Circle` (`Loop` / `PingPong`) platform in `TrajectoryShowcaseLevel.json` or Level Editor levels | Any trajectory with $\Delta P_y > 0.0001$ per frame reproduced the exact false overlap and state oscillation. |

---

## 3. Exact Execution Order & Mathematical Proof of the Defect

### 3.1. Legacy Frame Execution Pipeline (`GameManager.Update`)

```text
1. InputRouter.CaptureFrameInput()
2. TrajectorySystem.UpdateAll(dt)
     └── platform.transform.position = P(t + dt) = P(t) + ΔP
3. riderMove = TrajectorySystem.GetRiderDisplacement(X(t), size, color)
4. player.StepMovement(dt, riderMove)
     ├── jumpMove = jumpMotion.Step(dt, jumpPressed, fastFall, isGrounded)
     └── movementResolver.Resolve(X(t), size, offset, inputMove, jumpMove, color, dt, riderMove)
           ├── currentBounds = Aabb.FromCenter(X(t), size)   <-- PRE-CARRY PLAYER (Frame N-1)
           ├── overlap = overlapResolver.Evaluate(currentBounds, color)
           │     └── platformBounds = platform.GetAabb()     <-- POST-MOVE PLATFORM (Frame N)
           ├── combinedIntent = adjustedInput + jumpMove + pushMove + riderMove
           └── collision = collisionResolver.Resolve(X(t), size, offset, combinedIntent, color, overlap.IsPushingOut)
5. player.transform.position = X(t) + collision.FinalMove
```

### 3.2. Numerical Demonstration (60 FPS, $\Delta t = 0.01667\text{ s}$)

Consider a vertical or orbital moving platform of size $(3.0, 0.5)$ rising by $\Delta P_y = +0.045\text{ units}$ on Frame $N$:

- **End of Frame $N-1$:**
  - Platform top: $P_{\max, y}(t) = 0.2500$
  - Player bottom (`Min.y`): $X_{\min, y}(t) = 0.2500 + \epsilon_{\text{col}} = 0.2501$
  - Player center (`Center.y`): $X_{c, y}(t) = 0.5501$ (`playerSize = (0.6, 0.6)`)
- **Start of Frame $N$ (`TrajectorySystem.UpdateAll`):**
  - Platform moves upward by $\Delta P_y = +0.0450 \implies P_{\max, y}(t + \Delta t) = 0.2950$
- **Step 1 of `PlayerMovementResolver.Resolve` (Legacy):**
  - `currentBounds.Min.y` is still $0.2501$, while `platformBounds.Max.y` is already $0.2950$.
  - Vertical overlap depth:
    $$\delta_y = P_{\max, y}(t + \Delta t) - X_{\min, y}(t) = 0.2950 - 0.2501 = 0.0449\text{ units} > 0$$
  - Overlap percentage:
    $$\text{OverlapPercent} = \frac{0.6 \times 0.0449}{0.6 \times 0.6} \times 100\% = 7.483\% > 0\%$$
  - `overlap.IsPushingOut` becomes `true`!
  - **If player is near platform center ($x = 0$):**
    - `bestPushDirection = Vector2.up`, `pushMove.y = +2.0 * (1/60) = +0.03333`.
    - `combinedIntent.y = riderMove.y + pushMove.y = +0.0450 + 0.03333 = +0.07833` (**double upward displacement: carrier + false push-out!**).
    - After Frame $N$, player bottom is at $0.2501 + 0.07833 = 0.32843$, so vertical gap above `platformBounds.Max.y` ($0.2950$) is $0.03343 > \text{groundEpsilon } (0.02)$.
    - `IsGrounded` flips to `false` (`PlayerState.Falling`)!
  - **If player is within $0.20\text{ units}$ of the right platform edge (e.g. $x = 1.35$, where `platformBounds.Max.x = 1.50`):**
    - `right = |1.50 - 1.35| = 0.15 < up = |0.2950 - 0.5501| = 0.2551`.
    - `bestPushDirection = Vector2.right`, injecting an uncommanded horizontal kick `pushMove.x = +0.03333` per frame!

