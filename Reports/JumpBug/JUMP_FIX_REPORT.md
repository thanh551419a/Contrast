# CONTRAST — JUMP FIX IMPLEMENTATION REPORT (`JUMP_FIX_REPORT.md`)

## 1. Summary of Minimal, Architecture-Compliant Fixes

All changes strictly preserve the single-authority `Input → Event / Intent → Object Impact Aggregation & Resolution → End-of-Frame Commit` architecture defined in `docs/architecture/ARCHITECTURE_CONTRACT.md`. No parallel `Update` loops, physics steps, or state managers were introduced.

| File Modified | Methods / Spans Updated | Purpose of Change |
|---|---|---|
| `Assets/Script/Contrast/Input/StandaloneKeyboardInputAdapter.cs` | `GetJumpDown()`, `ResetFrameState()` | Combines `wasPressedThisFrame` with explicit rising-edge state (`prevWPressed`, `prevSpacePressed`) so `W`/`Space` presses across frame boundaries or state transitions are never missed while held keys never auto-repeat. |
| `Assets/Script/Contrast/Player/Input/PlayerInput.cs` | `PlayerInputIntent`, `ReadInput()` | Added `JumpPressed` (raw edge-triggered jump press independent of `isGrounded`) while preserving `JumpRequested = jumpPressed && isGrounded` for backward compatibility. |
| `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs` | `EnsureRigidbody()`, `RequestJump()`, `NotifyCeilingBlocked()`, `CurrentVerticalVelocity` | Added lazy `Rigidbody2D` initialization, removed silent `if (jumping) return;` early exit in `RequestJump()`, exposed `CurrentVerticalVelocity`, and added `NotifyCeilingBlocked()` to clamp upward velocity to `0` on ceiling impact (HTML parity). |
| `Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs` | `Resolve()`, `ResolveHorizontal()`, `ResolveVertical()`, `TryGetGroundSupport()` | Guarded `HasGroundSupport()` in `Resolve()` with `intentMove.y <= collisionEpsilon` so upward jump frames (`intentMove.y > collisionEpsilon`) never falsely snap back to grounded (`C2`/`C5`); added rising-platform takeoff clearance in `ResolveVertical()`; preserved sub-epsilon vertical displacement (`snapY = predictedBounds.Center.y`); exposed `TryGetGroundSupport()`. |
| `Assets/Script/Contrast/Player/PlayerController.cs` | `ProcessUpdate()`, `ExecuteJumpImmediate()`, `RefreshGroundedStateImmediate()`, `Respawn()`, `ResetState()`, `SetTestMode()`, `SetGroundedForTest()` | Implemented HTML-parity `HtmlJumpBufferDuration = 0.12f` and `HtmlCoyoteTimeDuration = 0.10f`; added `RefreshGroundedStateImmediate()` on spawn/respawn/phase-change/`T`-mode exit so `isGrounded` is valid before Frame 1's `ReadInput()`; added ceiling bonk velocity reset and bounded diagnostic counters. |
| `Assets/Script/Contrast/UI/GameControlUI.cs` | `CreateTopBarButton()`, `CreateMenuButton()`, `ClearEventSystemSelection()` | Set `Navigation.Mode.None` on all HUD/pause buttons and cleared `EventSystem.current.SetSelectedGameObject(null)` on button clicks so `Space` never triggers UI buttons during gameplay (`C1`). |
| `Assets/Script/Contrast/UI/VirtualControlsUI.cs` | `CreateActionButton()` | Set `Navigation.Mode.None` on virtual control buttons so keyboard `Space` cannot activate UI buttons. |
| `Assets/Editor/ContrastBuildPipeline.cs` | `BuildGameWindowsJumpFixRC2()` | Added isolated release candidate build target outputting to `Release/Windows/CONTRAST_JumpFix_RC2/Game.exe` and `Release/Windows/CONTRAST_JumpFix_RC2.zip` without touching existing release files. |
| `Assets/Editor/EditorLevelTests.cs` & `Tests/ContrastTests/Program.cs` | `Test_JumpLifecycle_Respawn_And_Stress_Suite()` | Added deterministic unit, respawn (`R1`–`R8`), high-FPS (`60..1000 FPS`), moving-platform (`50` takeoffs), `T`-mode (`50` cycles), and `1,000`-jump stress tests plus JSONL trace generation. |

---

## 2. How Each Root Cause (`C1`–`C5` & `R1`–`R8`) Was Resolved

1. **`C2` & `C5` (Takeoff-Frame Ground Snap Cancellation at High FPS & on Moving Platforms):**
   - In `PlayerCollisionResolver.Resolve()`, `HasGroundSupport()` is now only evaluated when `intentMove.y <= collisionEpsilon`:
     ```csharp
     bool canCheckGroundSupport = intentMove.y <= collisionEpsilon;
     result.IsGrounded = isLanding || (canCheckGroundSupport && HasGroundSupport(finalBounds, playerColor, out groundPlatform));
     ```
   - In `PlayerCollisionResolver.ResolveVertical()`, when `moveY > 0f` and `predictedBounds.Min.y >= platBounds.Max.y - groundEpsilon`, a platform underneath the player is never treated as an overhead ceiling during rising-platform takeoff.
2. **`C3` & `C4` (`Respawn()` Frame-1 Grounded Lag + Jump Buffer & Coyote Time):**
   - `PlayerController.RefreshGroundedStateImmediate()` calls `Physics2D.SyncTransforms()` and `collisionResolver.TryGetGroundSupport()` immediately inside `Respawn()`, `Initialize()`, `SetTestMode(false)`, and at the start of `ProcessUpdate()` whenever `!isGrounded && !IsJumping`.
   - `PlayerController.ProcessUpdate()` maintains `jumpBufferTimer` (`0.12s`) and `coyoteTimer` (`0.10s`), matching `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` lines 601–636. If a jump press arrives while airborne within `0.12s` of landing, `ExecuteJumpImmediate()` fires automatically on the landing frame.
3. **`C1` (UI Button Focus Stealing `Space`):**
   - Setting `button.navigation = new Navigation { mode = Navigation.Mode.None }` and clearing `EventSystem` selection guarantees `Space` is exclusively handled by gameplay input.

