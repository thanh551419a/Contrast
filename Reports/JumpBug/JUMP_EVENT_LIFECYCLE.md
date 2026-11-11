# CONTRAST — JUMP EVENT LIFECYCLE & PIPELINE AUDIT (`JUMP_EVENT_LIFECYCLE.md`)

## 1. End-to-End Jump Event Lifecycle (Post-Fix Verified Pipeline)

The CONTRAST Unity runtime strictly enforces the single-authority frame pipeline defined in `docs/architecture/ARCHITECTURE_CONTRACT.md`:
`Input (Frame-Local) → Event / Intent → Object Impact Aggregation & Resolution → End-of-Frame State Commit`.

```
[OS / Unity Input System (Keyboard.current / VirtualControlsUI)]
        │
        ▼  Phase 0: GameManager.Update() -> Ensure playing state & active PlayerController
        │
        ▼  Phase 1: TrajectorySystem.UpdateAll(dt)
        │           Advances all active moving platforms; records platform.FrameDisplacement
        │
        ▼  Phase 2: PlayerController.ProcessUpdate(dt) — Support & Timer Maintenance
        │           1. If (!isGrounded && !IsJumping): RefreshGroundedStateImmediate()
        │              (Establishes immediate support on Frame 1 after spawn/respawn/T-mode exit)
        │           2. Update coyoteTimer:
        │              - If (isGrounded && !IsJumping) -> coyoteTimer = 0.10s (HtmlCoyoteTimeDuration)
        │              - Else -> coyoteTimer = max(0, coyoteTimer - dt)
        │           3. Update jumpBufferTimer:
        │              - jumpBufferTimer = max(0, jumpBufferTimer - dt)
        │
        ▼  Phase 3: PlayerInput.ReadInput(dt, moveSpeed, isGrounded)
        │           - Queries IPlatformInputAdapter.GetJumpDown() (W or Space rising edge)
        │           - Produces frame-local PlayerInputIntent:
        │               * intent.JumpPressed   = jumpPressed (raw rising-edge press)
        │               * intent.JumpRequested = jumpPressed && isGrounded
        │           - Calls adapter.ResetFrameState() at end of ReadInput
        │
        ▼  Phase 4: Jump Buffer & Coyote-Time Eligibility Evaluation (PlayerController)
        │           - If (intent.JumpPressed):
        │               * ++totalJumpRequests; lastJumpRequestId = totalJumpRequests
        │               * jumpBufferTimer = 0.12s (HtmlJumpBufferDuration)
        │           - Determine canJumpNow = !IsJumping && (isGrounded || coyoteTimer > 0f)
        │           - If (jumpBufferTimer > 0f && canJumpNow):
        │               * ExecuteJumpImmediate():
        │                   - jumpBufferTimer = 0f; coyoteTimer = 0f
        │                   - isGrounded = false; GroundedPlatform = null
        │                   - jumpMotion.RequestJump(jumpForce = 12.8125f)
        │                   - ++totalJumpsExecuted; lastJumpRejectionReason = "None"
        │           - Else if (intent.JumpPressed):
        │               * Record diagnostic reason ("Buffered_AlreadyJumping" / "Buffered_AirborneNoCoyote")
        │
        ▼  Phase 5: Vertical Physics & Carrier Displacement Aggregation
        │           - jumpMotion.UpdateVerticalPhysics(dt, isGrounded, fastFallRequested, maxFallSpeed)
        │             (Applies gravity = 25.78125 u/s², produces verticalDisp = vy * dt)
        │           - carrierPlatform = (!isGrounded || IsJumping) ? null : GroundedPlatform
        │           - riderMove = TrajectorySystem.GetRiderDisplacement(..., carrierPlatform, IsJumping)
        │
        ▼  Phase 6: Collision & Overlap Resolution (PlayerMovementResolver + PlayerCollisionResolver)
        │           - Separates riderMove (already collision-checked against carrier) from selfIntentMove
        │           - Resolves horizontal & vertical sweeps against active opposite-color platforms
        │           - Rising-platform takeoff clearance: if (moveY > 0f && predictedBottom >= platTop - eps)
        │             do not treat departing floor underneath as a ceiling
        │           - Guarded Ground Support Check:
        │             result.IsGrounded = isLanding ||
        │                 (intentMove.y <= collisionEpsilon && HasGroundSupport(finalBounds, ...))
        │             (Prevents active upward jump frames from being falsely snapped back to grounded!)
        │
        ▼  Phase 7: End-of-Frame State Commit (PlayerController)
        │           - transform.position += FinalMove
        │           - Upward ceiling bonk check: if (IsJumping && intentMove.y > eps && FinalMove.y <= eps)
        │             jumpMotion.NotifyCeilingBlocked() (clamps vy = 0, matching HTML line 795)
        │           - Update isGrounded = moveResult.IsGrounded; GroundedPlatform = moveResult.GroundedPlatform
        │           - If (isGrounded):
        │               * coyoteTimer = 0.10s
        │               * jumpMotion.NotifyGrounded(true)
        │               * If (jumpBufferTimer > 0f): ExecuteJumpImmediate() (Buffered landing jump!)
```

---

## 2. Exact Answers to Section 2 Lifecycle Questions

| # | Question | Verified Code Answer |
|---|---|---|
| **Q1** | Where is `W` / `Space` read from Unity input? | `StandaloneKeyboardInputAdapter.GetJumpDown()` (`Assets/Script/Contrast/Input/StandaloneKeyboardInputAdapter.cs`), using `Keyboard.current.wKey` and `Keyboard.current.spaceKey` (`wasPressedThisFrame` OR rising-edge transition `!prevPressed && isPressed`), and `MobileTouchInputAdapter.GetJumpDown()` for touch UI. |
| **Q2** | Is it read using edge-triggered `GetKeyDown`, held-state `GetKey`, Unity Input System callbacks, or a custom adapter? | Read via custom `IPlatformInputAdapter` (`StandaloneKeyboardInputAdapter`) polling Unity Input System's `Keyboard.current` once per frame during `PlayerInput.ReadInput()`, combining `wasPressedThisFrame` with adapter-tracked rising-edge state (`prevWPressed`, `prevSpacePressed`) updated in `ResetFrameState()`. |
| **Q3** | Which object calls `ReadInput()` and in which Unity lifecycle method (`Update`, `FixedUpdate`, `LateUpdate`)? | `GameManager.Update()` calls `activePlayer.ProcessUpdate(dt)`, which calls `inputReader.ReadInput(dt, moveSpeed, isGrounded)`. Everything executes deterministically inside `Update` (never `FixedUpdate` or `LateUpdate`). |
| **Q4** | Can `ReadInput()` be called zero times or multiple times in a single rendered frame? | Zero times only when `GameManager.CurrentState != GameState.Playing` or `EasyNewtonFinaleController.IsInputFrozen` (post-victory cutscene) or `isTestMode == true` (`T` free-fly mode). Exactly once per rendered frame during active gameplay. |
| **Q5** | Where is `JumpRequested` created, stored, consumed, and cleared? | Created in `PlayerInput.ReadInput()` as a value-type `PlayerInputIntent` (`JumpPressed` and `JumpRequested`), returned on the stack to `PlayerController.ProcessUpdate()`, buffered into `jumpBufferTimer` (`0.12s`), and consumed via `ExecuteJumpImmediate()`. |
| **Q6** | What happens if the player presses Jump 1–6 frames before touching the ground? | `intent.JumpPressed` arms `jumpBufferTimer = 0.12f` (`~7.2` frames at 60 FPS, mirrors HTML `jumpBuffer = 0.12s`). The moment `moveResult.IsGrounded` becomes `true` on touchdown, `PlayerController` consumes `jumpBufferTimer` and calls `ExecuteJumpImmediate()` on that landing frame. |
| **Q7** | What happens if the player presses Jump on the exact frame of respawn? | `PlayerController.Respawn()` calls `RefreshGroundedStateImmediate()`, which queries `collisionResolver.TryGetGroundSupport()` at the spawn AABB before Frame 1's `ReadInput()`. Thus `isGrounded` is already `true` on Frame 1 (and even for slightly elevated spawns like `L03`, `jumpBufferTimer = 0.12s` catches touchdown). |
| **Q8** | What happens if the player holds `W` or `Space` across death/respawn, then releases and presses again? | Holding `W`/`Space` across death/respawn keeps `prevWPressed`/`prevSpacePressed == true` (no unwanted auto-jump while held); releasing sets `prev*Pressed = false`, and pressing again triggers a clean rising edge (`GetJumpDown() == true`) and executes the jump immediately. |
| **Q9** | What happens if `PlayerJumpMotion.jumping` is `true` when a new jump is requested? | `PlayerController` gates jump execution with `!jumpMotion.IsJumping` (storing any early press in `jumpBufferTimer = 0.12s` until landing), and `PlayerJumpMotion.RequestJump()` no longer silently returns early if invoked directly—it cleanly sets `jumping = true` and `SetVerticalVelocity(jumpForce)`. |
| **Q10** | Under what exact conditions does `PlayerJumpMotion.NotifyGrounded(true)` reset `jumping = false`? | Only at the end of `PlayerController.ProcessUpdate()` when `moveResult.IsGrounded == true`. Crucially, `PlayerCollisionResolver.Resolve()` now only evaluates `HasGroundSupport()` when `intentMove.y <= collisionEpsilon`, so an ascending jump (`intentMove.y > collisionEpsilon`) never falsely triggers `NotifyGrounded(true)` on the takeoff frame. |

