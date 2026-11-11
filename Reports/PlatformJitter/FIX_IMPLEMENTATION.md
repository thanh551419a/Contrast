# CONTRAST — MOVING PLATFORM JITTER FIX IMPLEMENTATION

## 1. Architectural Overview of the Solution

All fixes strictly preserve the project's **Input → Event → Impact Aggregation / Resolution → End-of-Frame Commit** pipeline (`GameManager.Update()`) with **zero** Rigidbody physics changes, **zero** parallel `Update`/`LateUpdate`/`FixedUpdate` loops, and **zero** modifications to Level Editor tooling or HTML reference files.

### Target Frame Pipeline (Post-Fix)

```text
1. InputRouter.CaptureFrameInput()
2. TrajectorySystem.UpdateAll(dt)
     └── Each active Trajectory updates worldPos and records FrameDisplacement = ΔP
3. riderMove = (player.IsGrounded && !player.IsJumping && !player.IsTestModeActive)
               ? TrajectorySystem.GetRiderDisplacement(playerPos, size, color, player.GroundedPlatform, player.IsJumping)
               : Vector2.zero
4. player.StepMovement(dt, riderMove)
     ├── jumpMove = jumpMotion.Step(dt, jumpPressed, fastFall, isGrounded)
     └── movementResolver.Resolve(currentPosition, size, offset, inputMove, jumpMove, color, dt, riderMove)
           ├── Step 0 (Carrier Pre-Resolution):
           │     carrierCollision = collisionResolver.ResolveCarrier(currentPosition, size, offset, riderMove, color)
           │     carriedPosition  = currentPosition + carrierCollision.FinalMove
           ├── Step 1 (Post-Carry Overlap Evaluation):
           │     currentBounds = Aabb.FromCenter(carriedPosition + offset, size)
           │     overlap       = overlapResolver.Evaluate(currentBounds, color)
           ├── Step 2 (Player Self-Intent):
           │     selfIntent     = adjustedInput + jumpMove + pushMove
           │     combinedIntent = selfIntent + riderMove
           ├── Step 3 (Self-Intent Collision Resolution from Carried Position):
           │     collision = collisionResolver.Resolve(carriedPosition, size, offset, selfIntent, color, overlap.IsPushingOut)
           └── Step 4 (Single Final Displacement Vector):
                 finalMove = carrierCollision.FinalMove + collision.FinalMove
5. player.transform.position = currentPosition + finalMove   (Single End-of-Frame Commit)
```

---

## 2. Exact File & Method Changes

### 2.1. `Assets/Script/Contrast/Player/PlayerController.cs`
- Exposed `public bool IsJumping => jumpMotion != null && jumpMotion.IsJumping;` so `GameManager.Update()` and `TrajectorySystem.GetRiderDisplacement()` can detect active jump takeoff/ascent.

### 2.2. `Assets/Script/Contrast/Level/TrajectorySystem.cs`
- Added overload `GetRiderDisplacement(Vector2 playerCenter, Vector2 playerSize, LogicalColor playerColor, ColorPlatform groundedPlatform, bool isJumping)` while retaining the 3-parameter overload for backward compatibility:
  - Immediately returns `Vector2.zero` when `isJumping == true` so jumping detaches cleanly from the moving platform on the takeoff frame without carrying airborne platform velocity.
  - Prioritizes `groundedPlatform` (the platform confirmed as supporting the player on the previous frame) if it still satisfies horizontal overlap and vertical support tolerance (`gap <= groundEpsilon`), and otherwise selects the valid supporting moving platform with the smallest vertical gap.

### 2.3. `Assets/Script/Contrast/Core/GameManager.cs`
- Updated `GameManager.Update()` (lines 197–207) so `riderMove` is only queried when `!player.IsTestModeActive && player.IsGrounded && !player.IsJumping`, passing `player.GroundedPlatform` and `player.IsJumping`.

### 2.4. `Assets/Script/Contrast/Player/Movement/PlayerMovementResolver.cs`
- Updated `Resolve()` to pre-resolve `riderMove` via `collisionResolver.ResolveCarrier(...)` into `carrierMove` and `carriedPosition = currentPosition + carrierMove` **before** calling `overlapResolver.Evaluate(currentBounds, playerColor)`.
- Evaluated player self-movement (`selfIntent = adjustedInput + jumpMove + pushMove`) from `carriedPosition`, and returned `finalMove = carrierMove + collision.FinalMove` for the single end-of-frame commit in `PlayerController.StepMovement()`.

### 2.5. `Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs`
- Added `ResolveCarrier(Vector2 currentPosition, Vector2 playerSize, Vector2 playerOffset, Vector2 carrierMove, LogicalColor playerColor)`:
  - When `carrierMove.y > 0f` (rising platform), resolves **Y first, then X** so the rider is lifted onto the platform's new top surface before horizontal carrier motion is tested against platform side edges.
  - When `carrierMove.y <= 0f` (horizontal or descending platform), resolves **X first, then Y** while the rider is still at the previous top height before lowering Y.
- Added `GetPreviousPlatformMaxY(ColorPlatform platform, Aabb platformBounds)` to account for upward platform displacement (`traj.FrameDisplacement.y > 0f`) during swept top-surface contact:
  - In `ResolveHorizontal()`: skips horizontal blocking against a rising platform whose top surface swept across the player's feet (`current.Min.y >= prevPlatformMaxY - collisionEpsilon && current.Center.y > platformBounds.Max.y`).
  - In `ResolveVertical()`: allows downward landing (`moveY < 0f`) and zero-velocity support (`|moveY| <= collisionEpsilon`) to snap cleanly to `platformBounds.Max.y + playerSize.y * 0.5f + collisionEpsilon` whenever `current.Min.y >= prevPlatformMaxY - collisionEpsilon`.

### 2.6. `Assets/Script/Contrast/Player/Movement/PlayerOverlapResolver.cs`
- Added a top-surface contact guard in `Evaluate(Aabb playerBounds, LogicalColor playerColor)`:
  - Computes `effectiveTopY = platformBounds.Max.y - upwardPlatformDisp`.
  - If `playerBounds.Min.y >= effectiveTopY - 0.001f && playerBounds.Center.y > platformBounds.Max.y`, treats the contact as top-surface ground support rather than interior penetration (`continue`), while preserving `< 50%` interior overlap push-out and `>= 50%` hard `IsStuck` detection.

