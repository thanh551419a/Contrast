# PRE-CHANGE NEWTON X-MARKING & VALIDATION AUDIT (`Reports/NewtonX/PRE_CHANGE_MARKING_VALIDATION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Base Commit:** `497c601d3f52095df712228c9e69a024d08ae23c`  
**Audit Status:** Read-only pre-change inspection completed **before** any code modification.

---

## 1. Executive Summary

Before any changes on `fix/deathy-newton-camera-x-collider`, Newton X-marking is handled by `EasyNewtonFinaleController.cs` (`Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs`). Inspection of the exact source code reveals **three critical findings**:

1. **Duplicate Same-Frame X-Trigger Bug (`EasyNewtonFinaleController.cs:190–191` AND `895–898`):**
   - In `EasyNewtonFinaleController.Initialize()` (`lines 190–191`), the controller subscribes `AttemptCapture` to `player.OnNewtonMarkRequested`:
     ```csharp
     player.OnNewtonMarkRequested -= AttemptCapture;
     player.OnNewtonMarkRequested += AttemptCapture;
     ```
   - In `PlayerController.ProcessUpdate()` (`PlayerController.cs:260–264`), when `input.NewtonMarkRequested` is `true` (`X` key pressed this frame), `PlayerController` sets `NewtonMarkRequested = true` and synchronously invokes `OnNewtonMarkRequested?.Invoke()`, which calls **`AttemptCapture()` for the first time**.
   - Then, in the **very same frame**, Unity runs `EasyNewtonFinaleController.Update()` (`lines 872–919`), which checks `if (IsNewtonMarkPressedThisFrame()) AttemptCapture();` (`lines 895–898`), where `IsNewtonMarkPressedThisFrame()` (`lines 957–969`) returns `true` if `player.NewtonMarkRequested` is `true` OR `kb.xKey.wasPressedThisFrame` is `true`!
   - Consequently, **a single physical press of `X` invokes `AttemptCapture()` TWICE in the same frame**!
2. **Distance-Only Validation Instead of Target Collider Overlap (`EasyNewtonFinaleController.cs:971–1106`):**
   - Currently, `AttemptCapture()` does **not** instantiate or evaluate collider/AABB hitboxes for `Target 1 (K)`, `Target 2 (I)`, or `Target 3 (K')`:
     - **Target 1 (`CaptureK`, `lines 992–1034`):** Checks only `bool onK = (player.IsGrounded && player.GroundedPlatform == platformK) || IsPlayerStandingOn(platformK);` — there is no Target 1 collider object or `InteractionEnabled` state.
     - **Target 2 (`CaptureI`, `lines 1036–1068`):** Checks `onI` and Euclidean distance between platform center `curPos` and `targetI`: `float dist = Vector2.Distance(curPos, targetI); if (dist <= toleranceI)`.
     - **Target 3 (`CaptureKp`, `lines 1070–1104`):** Checks `onK` and Euclidean distance between platform center `curPos` and `targetKp`: `float dist = Vector2.Distance(curPos, targetKp); if (dist <= toleranceKp)`.
3. **Target Visual Objects Have No Colliders or Sequential Enable/Disable State (`EasyNewtonFinaleController.cs:402–439, 772–870`):**
   - `guideNodeI` and `guideNodeKp` (`lines 433–437`) and `markers[0..2]` (`lines 422–431`) are purely visual `SpriteRenderer` GameObjects without `BoxCollider2D` or `NewtonTargetPoint` state components.

---

## 2. Point-by-Point Answers to All 16 Audit Questions (Section C1)

| # | Audit Question | Pre-Change Source Evidence & Answer |
|---|---|---|
| **1** | Where is X key input read? | 1. `StandaloneKeyboardInputAdapter.GetNewtonMarkDown()` (`Assets/Script/Contrast/Player/Input/StandaloneKeyboardInputAdapter.cs:66–77`) reads `Keyboard.current.xKey.wasPressedThisFrame` (or `Input.GetKeyDown(KeyCode.X)`).<br>2. `PlayerInput.ReadInput()` (`Assets/Script/Contrast/Player/PlayerInput.cs:91`) stores it in `PlayerInputIntent.NewtonMarkRequested`.<br>3. `PlayerController.ProcessUpdate()` (`PlayerController.cs:260–264`) sets `player.NewtonMarkRequested = true` and fires `OnNewtonMarkRequested`.<br>4. `EasyNewtonFinaleController.IsNewtonMarkPressedThisFrame()` (`EasyNewtonFinaleController.cs:957–969`) ALSO reads `player.NewtonMarkRequested` and `Keyboard.current.xKey.wasPressedThisFrame` directly! |
| **2** | Does X use `wasPressedThisFrame` / `GetKeyDown` (press) or `isPressed` / `GetKey` (hold)? | Both `StandaloneKeyboardInputAdapter.GetNewtonMarkDown()` (`line 71`) and `EasyNewtonFinaleController.IsNewtonMarkPressedThisFrame()` (`line 965`) use `wasPressedThisFrame` / `GetKeyDown(KeyCode.X)`. However, because both `OnNewtonMarkRequested` and `EasyNewtonFinaleController.Update()` fire on the same frame, a single press calls `AttemptCapture()` twice in one frame. |
| **3** | Can holding X across frames trigger multiple marks? | Holding `X` across subsequent frames does not set `wasPressedThisFrame`, **but** on the frame `X` is pressed, `AttemptCapture()` is called **twice** (`OnNewtonMarkRequested` + `EasyNewtonFinaleController.Update()`). If Target 1 and Target 2 were ever overlapping or satisfied in the same frame, a single press would advance two steps at once. |
| **4** | How are the three Newton targets represented in scene/data/runtime? | In `EasyNewtonFinaleController.cs`:<br>• State enum `NewtonFinaleStep { CaptureK, CaptureI, CaptureKp, Reveal, Completed }` (`lines 15–22`).<br>• Vectors `pointK`, `pointI`, `pointKp`, `targetI`, `targetKp` (`lines 56–61`).<br>• Visual GameObjects `markers[0..2]` (`Marker_0`, `Marker_1`, `Marker_2` at `lines 422–431`) and `guideNodeI` (`GuideTarget_I`), `guideNodeKp` (`GuideTarget_Kp` at `lines 433–437`). None of them have `Collider2D` components. |
| **5** | Do the three targets currently have `Collider2D` components? | **No.** Only `platformK` and `platformI` have `BoxCollider2D` components (`CreateOrbitPlatform` at `line 346`). The targets (`K`, `I`, `K'`) are represented only as `Vector2` coordinates (`targetI`, `targetKp`) and visual `SpriteRenderer` GameObjects (`guideNodeI`, `guideNodeKp`, `markers[0..2]`). |
| **6** | How does the code currently decide whether pressing X is valid? | In `EasyNewtonFinaleController.AttemptCapture()` (`lines 971–1106`):<br>• **Step 0 (`CaptureK`):** Requires `onK` (`player.IsGrounded && player.GroundedPlatform == platformK` or `IsPlayerStandingOn(platformK)`).<br>• **Step 1 (`CaptureI`):** Requires `onI` AND `Vector2.Distance(platformI.position, targetI) <= toleranceI` (`0.75 u`).<br>• **Step 2 (`CaptureKp`):** Requires `onK` AND `Vector2.Distance(platformK.position, targetKp) <= toleranceKp` (`0.84375 u`). |
| **7** | Is validation currently based on collider overlap, distance threshold, platform index, angle tolerance, trigger state, or has it been bypassed? | Currently based on **platform standing check (`onK` / `onI`) + platform-center Euclidean distance threshold (`Vector2.Distance(curPos, target) <= tolerance`)**, **not** Player-to-Target collider/AABB overlap. |
| **8** | How is the target order (`1 -> 2 -> 3`) currently tracked? | Via `private NewtonFinaleStep currentStep = NewtonFinaleStep.CaptureK;` (`line 52`) and booleans `hasK`, `hasI`, `hasKp` (`lines 53–55`) inside a `switch (currentStep)` statement in `AttemptCapture()` (`lines 990–1105`). |
| **9** | Can target 2 or target 3 be marked before target 1? | Not across separate frames because `switch (currentStep)` starts at `CaptureK`, **except** `Target 2` and `Target 3` do not have explicit target collider objects with `InteractionEnabled = false` flags that can be inspected or tested independently. |
| **10** | Can a single X press mark more than one target in the same frame? | Because `AttemptCapture()` is invoked **twice** per `X` press (once via `player.OnNewtonMarkRequested` and once via `EasyNewtonFinaleController.Update()`), `AttemptCapture()` actually executes Step N on the first call and immediately evaluates Step N+1 on the second call in the very same frame! (It only fails Step N+1 in normal play because Step 0 requires `onK` and Step 1 requires `onI`). |
| **11** | Can a completed target be marked again? | Once `currentStep` advances past a target, the `switch (currentStep)` no longer branches to the completed step, and once `currentStep >= NewtonFinaleStep.Reveal`, `AttemptCapture()` returns early at `line 976`. |
| **12** | Are all three target visuals visible when needed, or does disabling interaction hide visuals? | `guideNodeI` and `guideNodeKp` are kept active during `CaptureI` and `CaptureKp` (`UpdateDynamicVisuals` at `lines 796–797`), while `markers[0..2]` become visible as each point is locked. Visual visibility and interaction collider state must remain cleanly separated so all target visuals/guides remain visible as designed while only the current target's collider has `InteractionEnabled == true`. |
| **13** | What happens to target state when the Player dies and respawns? | `OnPlayerRespawned()` (`lines 238–266`) checks if the player respawned outside the Newton zone (`distToO > 15.8f`); if so and `currentStep != Completed`, it calls `ResetMeasurementState()` (`lines 599–635`), resetting `currentStep = CaptureK`, `hasK = hasI = hasKp = false`, and hiding locked markers. |
| **14** | What happens to target state when the level is restarted? | `LevelLoader.BuildRuntimeLevel()` tears down and recreates `EasyNewtonFinaleController` (or calls `ResetState()`), resetting `currentStep = CaptureK` and `hasK = hasI = hasKp = false`. |
| **15** | How does X-marking behave in L01, L02, and L03? | All three built-in levels (`L01`, `L02`, `L03`) use `EasyNewtonFinaleController` with `orbitStart = "auto"` and level-specific `(cx, cy, i, k, R, lambda, rails.K, rails.I)`:<br>• **L01:** `i = 3, k = 6`<br>• **L02:** `i = 2, k = 4`<br>• **L03:** `i = 2, k = 7` |
| **16** | How does HTML prototype validate X-marking in L01, L02, and L03? | In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`handleNewtonMeasurePress()` at `lines 1188–1242`):<br>• Triggered **only** on discrete `KeyX` keydown (`!e.repeat` at `lines 262–268`).<br>• Step 0 (`!newton.hasK`): Requires player standing on Rail K (`onK`). Captures `pointK`, sets `axisAngle`, and computes `targetI` and `targetKp`.<br>• Step 1 (`!newton.hasI`): Requires player on Rail I (`onI`) overlapping the Target I window (`dist <= toleranceI = 48 px = 0.75 u`).<br>• Step 2 (`!newton.hasKp`): Requires player on Rail K (`onK`) overlapping the Target K' window (`dist <= toleranceKp = 54 px = 0.84375 u`). |

---

## 3. Design of Sequential PRESS-X Collider Validation (`Task C`)

To satisfy every requirement of Sections C2–C7 while preserving 100% compatibility with existing gameplay and automated tests:

1. **Single-Event Press-X Consumption (Eliminate Duplicate Same-Frame Call):**
   - Track a monotonically increasing `newtonMarkEventSequence` / `lastProcessedMarkFrame` and remove the duplicate trigger so that a discrete `X` key-down (`wasPressedThisFrame` / `OnNewtonMarkRequested` / direct test `AttemptCapture()`) is evaluated **at most once per press event**, never on hold, and never twice in the same frame.
2. **Three Explicit Target Colliders (`Target 1: K`, `Target 2: I`, `Target 3: K'`):**
   - Represent each of the 3 targets with a dedicated runtime target object & `BoxCollider2D` (`isTrigger = true`) + world-space `Aabb` hitbox:
     - **Target 1 (`Index 0 — K`):** Attached to / tracking the active capture zone above `PlatformK` (center at `platformK.position + (0, platformHeight * 0.5 + playerHeight * 0.5)`, size `(platformK.width + toleranceK * 2, playerHeight + 1.2f)`), so that when the Player is standing on / overlapping `PlatformK`, `PlayerCollider` (`GetPlayerAabb()`) overlaps `Target1Collider` (`GetTargetAabb(0)`), and when the Player is off `PlatformK` (or on `PlatformI` or airborne away from `PlatformK`), `PlayerCollider` does **not** overlap `Target1Collider`.
     - **Target 2 (`Index 1 — I`):** Positioned at `targetI = centerO - AxisU * rI` (with vertical extent covering a player standing on `PlatformI` when `PlatformI` is within the `toleranceI = 0.75 u` capture window of `targetI`), so `PlayerCollider` overlaps `Target2Collider` if and only if the Player is on `PlatformI` and within the Target 2 collider bounds at `targetI`!
     - **Target 3 (`Index 2 — K'`):** Positioned at `targetKp = centerO - AxisU * rK` (with vertical extent covering a player standing on `PlatformK` when `PlatformK` is within the `toleranceKp = 0.84375 u` capture window of `targetKp`), so `PlayerCollider` overlaps `Target3Collider` if and only if the Player is on `PlatformK` and within the Target 3 collider bounds at `targetKp`!
3. **Strict Sequential State Machine (`CurrentTargetIndex = 0 -> 1 -> 2 -> 3`):**
   - **Initial State (`CurrentTargetIndex = 0`):**
     - `Target[0].InteractionEnabled = true`, `Target[0].Completed = false`
     - `Target[1].InteractionEnabled = false`, `Target[1].Completed = false`
     - `Target[2].InteractionEnabled = false`, `Target[2].Completed = false`
   - **After Valid Mark on Target 1 (`CurrentTargetIndex = 1`):**
     - `Target[0].Completed = true`, `Target[0].InteractionEnabled = false`
     - `Target[1].InteractionEnabled = true`, `Target[1].Completed = false`
     - `Target[2].InteractionEnabled = false`, `Target[2].Completed = false`
   - **After Valid Mark on Target 2 (`CurrentTargetIndex = 2`):**
     - `Target[0].Completed = true`, `Target[0].InteractionEnabled = false`
     - `Target[1].Completed = true`, `Target[1].InteractionEnabled = false`
     - `Target[2].InteractionEnabled = true`, `Target[2].Completed = false`
   - **After Valid Mark on Target 3 (`CurrentTargetIndex = 3`):**
     - `Target[0..2].Completed = true`, `Target[0..2].InteractionEnabled = false`
     - Triggers `StartRevealSequence()`.
4. **Visuals Decoupled from `InteractionEnabled`:**
   - All target guide visuals (`guideNodeI`, `guideNodeKp`, `markers[0..2]`, rings) remain controlled by their visual renderers, while `InteractionEnabled` and `BoxCollider2D.enabled` strictly gate which single target can be marked at the current stage.

