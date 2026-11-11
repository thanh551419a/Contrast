# CONTRAST — JUMP FAILURE ROOT CAUSE ANALYSIS (`JUMP_ROOT_CAUSE.md`)

## Executive Summary

Investigation of the CONTRAST Unity runtime (`PlayerController`, `PlayerInput`, `StandaloneKeyboardInputAdapter`, `PlayerJumpMotion`, `PlayerMovementResolver`, `PlayerCollisionResolver`, `GameControlUI`, `VirtualControlsUI`, and campaign levels `L01`–`L03`) identified **four distinct, interacting root causes** spanning candidate classes **`C1`**, **`C2`**, **`C3`**, **`C4`**, and **`C5`**. Together, these defects explain both:
1. **Why Jump intermittently failed during normal gameplay in standalone Windows builds and on moving platforms (`C1`, `C2`, `C5`)**, and
2. **Why Jump consistently or intermittently failed right after player death, automatic checkpoint respawn, landing, or `T`-mode exit (`C3`, `C4`)**.

---

## Root Cause 1 (`C2` & `C5`) — Same-Frame Jump Cancellation via Unconditional `HasGroundSupport()` + `NotifyGrounded(true)` on Takeoff Frame

### Exact File, Method, and Line Reference (Pre-Fix)
- `Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs` — `Resolve()` (line 195):
  ```csharp
  result.IsGrounded = isLanding || HasGroundSupport(finalBounds, playerColor, out groundPlatform);
  ```
- `Assets/Script/Contrast/Player/PlayerController.cs` — `ProcessUpdate()` (lines 305–310):
  ```csharp
  isGrounded = moveResult.IsGrounded;
  GroundedPlatform = moveResult.GroundedPlatform;
  if (isGrounded)
  {
      jumpMotion.NotifyGrounded(true);
  }
  ```
- `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs` — `NotifyGrounded()` (lines 89–96):
  ```csharp
  if (isGrounded)
  {
      jumping = false;
      SetVerticalVelocity(0f);
  }
  ```

### Mathematical & Runtime Proof
1. When the player presses `W` or `Space`, `PlayerJumpMotion.RequestJump(12.8125f)` sets `jumping = true` and `vy = 12.8125 u/s` (`820 px/s`).
2. In the **same frame**, `UpdateVerticalPhysics(dt)` applies gravity (`25.78125 u/s²`) and computes first-frame upward displacement:
   $$\Delta y = (12.8125 - 25.78125 \cdot \Delta t) \cdot \Delta t$$
3. Next, `PlayerCollisionResolver.Resolve()` moves the player AABB upward by $\Delta y$ (`intentMove.y > 0`) and then **unconditionally** calls `HasGroundSupport(finalBounds, ...)` using `groundEpsilon = 0.02f` (`1.28 px`).
4. Whenever `finalBounds.Min.y - platform.Max.y <= 0.02f` at the end of the takeoff frame:
   - **In Standalone `Game.exe` at High Frame Rates:** `Game.exe` runs with default VSync/uncapped desktop frame timing. At $\Delta t \le 0.00156\text{ s}$ ($\ge 640\text{ FPS}$, or $\ge 280\text{ FPS}$ when resting with up to `0.001f` contact penetration), $\Delta y \le 0.02\text{ u}$.
   - **On Rising Moving Platforms (`C5`):** When jumping off a rising platform or when upward displacement is partially clipped, the post-step vertical clearance above the platform is $\le 0.02\text{ u}$.
5. Because `HasGroundSupport()` returned `true` while `intentMove.y > 0`, `result.IsGrounded` was set to `true` on the takeoff frame itself.
6. At the end of `PlayerController.ProcessUpdate()`, `if (isGrounded) jumpMotion.NotifyGrounded(true)` executed **`jumping = false; SetVerticalVelocity(0f);`**, **destroying the jump on the exact frame it started**.

---

## Root Cause 2 (`C3` & `C4`) — `ResetState()` Forcing `isGrounded = false` Before Frame-1 `ReadInput()` + Missing HTML Jump Buffer (`0.12s`) & Coyote Time (`0.10s`)

### Exact File, Method, and Line Reference (Pre-Fix)
- `Assets/Script/Contrast/Player/PlayerController.cs` — `Respawn()` & `ResetState()` (lines 408–428):
  ```csharp
  public void Respawn(Vector3 position)
  {
      transform.position = position;
      ResetState(); // <-- Sets isGrounded = false; GroundedPlatform = null;
  }
  ```
- `Assets/Script/Contrast/Player/Input/PlayerInput.cs` — `ReadInput()` (line 53):
  ```csharp
  bool jumpPressed = adapter.GetJumpDown();
  intent.JumpRequested = jumpPressed && isGrounded;
  ```

### Mathematical & Runtime Proof
1. **Phase Ordering Trap on Respawn (`Case R4`), Landing, and `T`-Mode Exit:**
   - In `PlayerController.ProcessUpdate(dt)`, **Phase 1 (`inputReader.ReadInput(dt, moveSpeed, isGrounded)`) runs BEFORE Phase 3 (`movementResolver.Resolve(...)`)**.
   - When `Respawn(spawnPos)` ran, `ResetState()` forced `isGrounded = false`. Even though 19 of 20 spawn/checkpoint positions in `L01`–`L03` place the player directly on the platform top (`gap = 0.0000`), `isGrounded` remained `false` during Frame 1's `ReadInput()` and only became `true` at the end of Frame 1 after `Resolve()` finished.
   - Consequently, any jump press on Frame 1 after respawn (`Case R4`) or on the frame of exiting `T`-mode was **100% ignored**.
2. **Missing HTML Jump Buffer (`0.12s`) and Coyote Time (`0.10s`):**
   - The authoritative HTML reference (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`, lines 601–602, 628–636) implements:
     ```javascript
     if (jumpPressed) player.jumpBuffer = 0.12;
     if (player.onGround) player.coyote = 0.10;
     if (player.jumpBuffer > 0 && player.coyote > 0) { player.vy = -player.jumpVel; ... }
     ```
   - Pre-fix Unity had **zero** jump buffer and **zero** coyote time (`intent.JumpRequested = jumpPressed && isGrounded`).
   - Whenever a player pressed `W` or `Space` `1–7` frames (`<= 0.12s`) before landing from a jump/fall, or while settling after respawn (`Case R3`, or `L03 startPos`), or `1–6` frames (`<= 0.10s`) after walking off a platform edge, the edge-triggered `wasPressedThisFrame` event was consumed and discarded forever.

---

## Root Cause 3 (`C1`) — Unity UI `EventSystem` Button Focus Hijacking `Space` After Clicking HUD Buttons

### Exact File, Method, and Line Reference (Pre-Fix)
- `Assets/Script/Contrast/UI/GameControlUI.cs` — `CreateTopBarButton()` (lines 180–191) and `CreateMenuButton()` (lines 257–266).
- `Assets/Script/Contrast/UI/VirtualControlsUI.cs` — `CreateActionButton()` (lines 222–231).

### Runtime Proof
- All HUD buttons (`MENU`, `RETRY`, `RESTART`) were created with Unity's default `Navigation.Mode.Automatic`.
- Whenever a player clicked `RETRY` (checkpoint respawn) or `RESTART` with the mouse, Unity's `EventSystem.current` kept that `Button` as `currentSelectedGameObject`.
- In Unity's Input System UI module, `Space` is bound to `Submit`. Pressing `Space` to jump subsequently triggered `OnRetryClicked()` or `OnRestartClicked()` on the focused HUD button instead of pure gameplay jumping.

---

## Root Cause 4 (`C2` & `C5`) — Sub-Epsilon Vertical Displacement Zeroing & Missing Ceiling Bonk Velocity Clamp

### Exact File, Method, and Line Reference (Pre-Fix)
- `Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs` — `ResolveVertical()` (lines 303–305):
  ```csharp
  if (Mathf.Abs(moveY) <= collisionEpsilon) return false;
  ```
  When `snapY` had been initialized to `currentBounds.Center.y`, returning early when `|moveY| <= 0.001f` caused `snapY` to discard `predictedBounds.Center.y`, zeroing out vertical motion near the jump apex or at very high frame rates (`> 1000 FPS`).
- `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`:
  - `RequestJump()` had a silent `if (jumping) return;` guard and did not call `EnsureRigidbody()` if invoked before `Awake()`.
  - Hitting a low ceiling while ascending stopped `FinalMove.y` for 1 frame in `PlayerCollisionResolver`, but `PlayerJumpMotion` kept `rb.linearVelocity.y > 0` instead of clamping upward velocity to `0` (`NotifyCeilingBlocked()`, matching HTML line 795: `if (player.vy < 0) player.vy = 0`).

