# SEQUENTIAL PRESS-X COLLIDER VALIDATION IMPLEMENTATION (`Reports/NewtonX/X_COLLIDER_IMPLEMENTATION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Status:** Implemented sequentially (`Step C6.1 Target 1` $\rightarrow$ `Step C6.2 Target 2` $\rightarrow$ `Step C6.3 Target 3`) and verified against `X01`–`X22`.

---

## 1. Root Causes Fixed from Pre-Change Audit

1. **Eliminated Duplicate Same-Frame `AttemptCapture()` Invocation:**
   - Previously, `EasyNewtonFinaleController` subscribed `player.OnNewtonMarkRequested += AttemptCapture` (`line 191`) AND called `if (IsNewtonMarkPressedThisFrame()) AttemptCapture();` inside `EasyNewtonFinaleController.Update()` (`line 895`). Because `PlayerController.ProcessUpdate()` and `EasyNewtonFinaleController.Update()` execute in the same frame, a single press of `X` called `AttemptCapture()` **twice in the same frame**.
   - Fixed by routing live frame input through `OnPlayerNewtonMarkInput()`, which records `lastInputMarkFrame = Time.frameCount` and guarantees that a physical `X` key-down is evaluated **at most once per frame**, while holding `X` across frames is ignored (`wasPressedThisFrame` / `GetKeyDown`).
2. **Replaced Distance-Only Checks with Explicit Target Colliders & Strict Sequential Enablement:**
   - Added three dedicated runtime target collider GameObjects (`NewtonTargetCollider_1_K`, `NewtonTargetCollider_2_I`, `NewtonTargetCollider_3_Kp`) with `BoxCollider2D` (`isTrigger = true`) under `apparatusRoot` in `EasyNewtonFinaleController.BuildTargetColliders()`.
   - Added explicit state arrays `targetInteractionEnabled[0..2]` and `targetCompleted[0..2]` synchronized by `SyncTargetStatesAndColliders()`.

---

## 2. Sequential State Machine (`Target 1 -> Target 2 -> Target 3`)

At every point during gameplay, `CurrentTargetIndex` (`0`, `1`, `2`, or `3` when complete) governs which single target is eligible for `PRESS X` interaction:

| Stage | `CurrentStep` | `CurrentTargetIndex` | `Target 1 (K)` State | `Target 2 (I)` State | `Target 3 (K')` State |
|---|---|---|---|---|---|
| **Initial (Stage 0)** | `CaptureK` | `0` | `Enabled = true`, `Completed = false`, `Collider.enabled = true` | `Enabled = false`, `Completed = false`, `Collider.enabled = false` | `Enabled = false`, `Completed = false`, `Collider.enabled = false` |
| **After Target 1 Marked (Stage 1)** | `CaptureI` | `1` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` | `Enabled = true`, `Completed = false`, `Collider.enabled = true` | `Enabled = false`, `Completed = false`, `Collider.enabled = false` |
| **After Target 2 Marked (Stage 2)** | `CaptureKp` | `2` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` | `Enabled = true`, `Completed = false`, `Collider.enabled = true` |
| **After Target 3 Marked (Stage 3)** | `Reveal` / `Complete` | `3` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` | `Enabled = false`, `Completed = true`, `Collider.enabled = false` |

---

## 3. Collider Validation & Visual Separation

1. **Player-to-Target Collider Overlap (`DoesPlayerOverlapTargetCollider`):**
   - Computes the Player's world-space AABB (`GetPlayerColliderWorldRect()`) and the active target's `BoxCollider2D` world-space AABB (`GetTargetColliderWorldRect(targetIndex)`).
   - Returns `true` if and only if:
     - `targetInteractionEnabled[targetIndex] == true`
     - `targetCompleted[targetIndex] == false`
     - `targetColliders[targetIndex].enabled == true`
     - `playerRect.Overlaps(targetRect) == true`
2. **Visuals Decoupled from `InteractionEnabled`:**
   - Once Target 1 (`K`) establishes the measurement axis `K–O`, both `targetICrosshair` (`I`) and `targetKpCrosshair` (`K'`) are rendered visually on the rings until each is captured (`!HasI` and `!HasKp`), while `targetInteractionEnabled` and `BoxCollider2D.enabled` strictly gate interaction to `Target 2 (I)` first and `Target 3 (K')` second.
3. **Structured Diagnostic Logging:**
   - Every `X` press attempt logs:
     - `[NEWTON_INPUT] frame=<n> xPressedThisFrame=True xHeld=False eventId=X_xxxx`
     - `[NEWTON_COLLISION] frame=<n> currentTarget=<1|2|3> playerBounds=<rect> targetBounds=<rect> overlapping=<bool>`
     - `[NEWTON_MARKING] frame=<n> eventId=X_xxxx target=<1|2|3> accepted=<bool> reason=<reason> nextTarget=<2|3|COMPLETE|UNCHANGED>`

