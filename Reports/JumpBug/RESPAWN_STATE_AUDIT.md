# CONTRAST — DEATH, CHECKPOINT & RESPAWN STATE AUDIT (`RESPAWN_STATE_AUDIT.md`)

## 1. Complete Death & Respawn Call Chain

1. **Death Triggers (`PlayerController.ProcessUpdate`):**
   - **Void Fall:** `transform.position.y < voidThresholdY` (`PlayerController.cs:238-242`) $\rightarrow$ calls `Die()` and immediately `return;`s so no post-death movement/collision runs in the same frame.
   - **Hazard Touch:** `moveResult.TouchedHazard` (`PlayerController.cs:297-301`) $\rightarrow$ calls `Die()` and `return;`s.
   - **Stuck / Crush ($\ge 50\%$ Interior Overlap):** `moveResult.IsStuck` (`PlayerController.cs:289-294`) $\rightarrow$ calls `Die()` and `return;`s.
2. **Death Handler (`PlayerController.Die` $\rightarrow$ `GameManager.HandlePlayerDead`):**
   - `PlayerController.Die()` invokes `OnPlayerDead?.Invoke()`.
   - `GameManager.HandlePlayerDead()` (`GameManager.cs:402-409`) increments `deathCount` and immediately calls `RespawnPlayer()` in the **same frame** (no coroutine delay, no `GameState` change away from `Playing`).
3. **Respawn Execution (`GameManager.RespawnPlayer` $\rightarrow$ `PlayerController.Respawn`):**
   - Determines target spawn position: `hasActiveCheckpoint ? activeCheckpointPos : levelRoot.SpawnPoint` (`GameManager.cs:430`).
   - Resets ghost platforms via `LevelLoader.ResetGhostPlatformsForRetry(levelRoot)`.
   - Calls `activePlayer.Respawn(spawnPos)` (`PlayerController.cs:408-420`).
   - Calls `cameraFollow.SnapToTarget()`.

---

## 2. State Variable Audit Across `Die()` $\rightarrow$ `Respawn()` $\rightarrow$ `ResetState()`

| State Variable / Component | Owner | Pre-Fix Value After `Respawn()` | Post-Fix Value After `Respawn()` | Impact on Post-Respawn Jump |
|---|---|---|---|---|
| `transform.position` | `Transform` | `position` | `position` + `Physics2D.SyncTransforms()` | Collider AABB is immediately synchronized in Box2D before ground check. |
| `rb.linearVelocity` | `Rigidbody2D` | `(0, 0)` (via `jumpMotion.ResetState()`) | `(0, 0)` (with `EnsureRigidbody()` guarantee) | Clean zero vertical velocity on spawn. |
| `jumpMotion.IsJumping` (`jumping`) | `PlayerJumpMotion` | `false` | `false` | Ready for next jump. |
| `isGrounded` | `PlayerController` | **`false` (DEFECT)** | **`true` if spawn AABB rests on valid platform** (via `RefreshGroundedStateImmediate()`) | Fixes Frame-1 post-respawn jump failure (`Case R1`, `Case R4`). |
| `GroundedPlatform` | `PlayerController` | **`null` (DEFECT)** | **Supporting `ColorPlatform`** (via `RefreshGroundedStateImmediate()`) | Immediate carrier reference if spawning on a platform. |
| `coyoteTimer` | `PlayerController` | *(Did not exist)* | `0.10f` if grounded on spawn, else `0f` | Matches HTML `coyote = 0.10s`. |
| `jumpBufferTimer` | `PlayerController` | *(Did not exist)* | `0f` on reset; set to `0.12f` on any `JumpPressed` | Matches HTML `jumpBuffer = 0.12s`; enables jump even when spawning slightly above ground (`L03`). |
| `isTestMode` | `PlayerController` | Preserved (`false` in normal play) | Preserved; exiting `SetTestMode(false)` calls `RefreshGroundedStateImmediate()` | Prevents lost jump on Frame 1 after exiting `T` mode. |
| `prevWPressed`, `prevSpacePressed` | `StandaloneKeyboardInputAdapter` | *(Did not exist)* | Updated every `ResetFrameState()` | Tracks true key state across frames and level/respawn transitions. |
| `EventSystem.current.currentSelectedGameObject` | Unity UI `EventSystem` | **Could remain on `RETRY`/`RESTART` button (DEFECT)** | **`null` (`Navigation.Mode.None` on all HUD buttons)** | Prevents `Space` from re-clicking HUD `RETRY`/`RESTART` buttons (`C1`). |

---

## 3. Campaign Spawn & Checkpoint Coordinate Audit (`L01.json`, `L02.json`, `L03.json`)

Using player collider half-height `0.45` (`size = 0.5 x 0.9`), every authored spawn and checkpoint in `Assets/Resources/Levels/L01.json`, `L02.json`, and `L03.json` was audited against underlying platform top surfaces (`plat_top = plat.y + plat.height * 0.5`):

| Level | Spawn / Checkpoint ID | Authored `(x, y)` | Player Bottom (`y - 0.45`) | Supporting Platform Top (`plat_top`) | Vertical Gap (`bottom - plat_top`) | Grounded Status on Spawn |
|---|---|---|---|---|---|---|
| **L01** | `startPos` | `(-9.5000, -5.8000)` | `-6.2500` | `-6.2500` | `0.0000` | Grounded immediately (`gap <= 0.02`) |
| **L01** | `cp_0` .. `cp_4` (5 CPs) | Various | `plat_top` | `plat_top` | `0.0000` | Grounded immediately (`gap <= 0.02`) |
| **L02** | `startPos` | `(-9.5000, -5.8000)` | `-6.2500` | `-6.2500` | `0.0000` | Grounded immediately (`gap <= 0.02`) |
| **L02** | `cp_0` .. `cp_4` (5 CPs) | Various | `plat_top` | `plat_top` | `0.0000` | Grounded immediately (`gap <= 0.02`) |
| **L03** | `startPos` | `(-9.5000, -5.5000)` | `-5.9500` | `-6.7000` | **`+0.7500` (Airborne)** | Falls `0.75u` (~14 frames at 60 FPS); buffered jump (`0.12s`) + immediate landing support |
| **L03** | `cp_0` .. `cp_6` (7 CPs) | Various | `plat_top` | `plat_top` | `0.0000` | Grounded immediately (`gap <= 0.02`) |

---

## 4. Verification of Required Respawn Timing Matrix (`Cases R1–R8`)

| Case | Scenario Description | Pre-Fix Behavior | Post-Fix Behavior | Automated Test Result |
|---|---|---|---|---|
| **R1** | Normal death (void or hazard), wait until grounded, press `W` or `Space` | Intermittent failure at high FPS (`>= 280-640 FPS`) due to same-frame ground snap (`C2`/`C5`), or if `RETRY` had UI focus (`C1`). | Immediate takeoff (`vy = 12.8125 u/s`, `isGrounded = false`). | **PASS (100%)** |
| **R2** | Hold `W` or `Space` while dying, keep holding across respawn, release, then press again | Could fail if `Space` triggered focused UI button or high-FPS snap occurred. | Held key does not falsely fire; release + repress fires rising edge and jumps cleanly. | **PASS (100%)** |
| **R3** | Press `W` or `Space` while still slightly airborne during spawn settle (`0 < gap <= 0.18u`) | **100% FAILED** — `JumpRequested = jumpPressed && isGrounded` discarded the press because `isGrounded` was `false` and no jump buffer existed. | `jumpBufferTimer = 0.12s` stores the press and auto-executes jump on the exact touchdown frame. | **PASS (100%)** |
| **R4** | Press `W` or `Space` on the exact first frame after respawn (`gap = 0.0`) | **100% FAILED** — `ResetState()` set `isGrounded = false`, and `ReadInput()` ran in Phase 1 before `movementResolver.Resolve()` set `isGrounded = true` in Phase 3. | `RefreshGroundedStateImmediate()` sets `isGrounded = true` inside `Respawn()`, so Frame-1 jump succeeds 100% of the time. | **PASS (100%)** |
| **R5** | Die during an active jump (ascending or at apex), respawn, then press Jump | Failed on Frame 1 (`R4`) or at high FPS (`C2`/`C5`). | `ResetState()` clears prior jump state and `RefreshGroundedStateImmediate()` restores ground support immediately. | **PASS (100%)** |
| **R6** | Die while falling off a platform edge, respawn, then press Jump | Failed on Frame 1 (`R4`) or at high FPS (`C2`/`C5`). | Clean state reset + immediate ground support + coyote/buffer timers. | **PASS (100%)** |
| **R7** | Die while standing on or jumping from a moving platform, respawn, then press Jump | Failed on rising moving platforms when `intentMove.y` was within `groundEpsilon` (`0.02u`) of the moving platform top (`C5`). | Rising-platform clearance + `intentMove.y <= collisionEpsilon` ground check guard ensures 100% takeoff. | **PASS (100%)** |
| **R8** | Die right after changing color/phase (`Q`/`E`, `1`/`2`/`3`, or slider), respawn, then press Jump | Phase change or respawn left `isGrounded` stale for 1 frame. | Both `Respawn()` and phase-change handlers call `RefreshGroundedStateImmediate()`. | **PASS (100%)** |

