# PRE-CHANGE DEATH CONDITION AUDIT (`Reports/DeathY/PRE_CHANGE_DEATH_CONDITION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Base Commit:** `497c601d3f52095df712228c9e69a024d08ae23c`  
**Audit Status:** Read-only pre-change inspection completed **before** any source or JSON modification.

---

## 1. Executive Summary

Before any changes on `fix/deathy-newton-camera-x-collider`, player death in Unity is controlled by `PlayerController.Die()` (`Assets/Script/Contrast/Player/PlayerController.cs:677–693`), which is invoked exclusively from two vertical fall checks at the end of `PlayerController.ProcessUpdate(float deltaTime)` (`lines 411–423`):

1. **Dynamic Heuristic Free-World Void Death (`PlayerController.cs:411–418` + `HasPotentialLandingBelow()` at `lines 426–447`):**
   - When the player is airborne (`!isGrounded` and `!result.IsStuck`), `ProcessUpdate` computes `currentFeet = transform.position.y - playerSize.y * 0.5f`.
   - If `currentFeet < lastSupportedFeet - voidGraceUnits` (`voidGraceUnits = 3.75f`) AND `!HasPotentialLandingBelow()` (which loops through all active `ColorPlatform.All` and computes an optimistic parabolic horizontal reach `Mathf.Max(3.28f, moveSpeed * airborneTime) + playerSize.x`), `Die()` is triggered immediately—even if the player is still well above the bottom of the level or camera boundary.
2. **Dynamic Boundary/Platform-Derived `KillY` Check (`PlayerController.cs:421–423` + `LevelLoader.ComputeKillY()` at `LevelLoader.cs:621–653`):**
   - Checks `if (gm != null && transform.position.y < gm.KillY) Die();` where `GameManager.KillY` (`GameManager.cs:56`) forwards `levelLoader.KillY`.
   - `LevelLoader.ComputeKillY(LevelData data)` (`LevelLoader.cs:621–653`) dynamically derives `KillY` during `BuildRuntimeLevel` (`line 151`) from:
     1. `data.cameraBoundary`: `cameraBoundary.position.y - cameraBoundary.size.y * 0.5f - 1f` (if `cameraBoundary != null` and `size.y > 0.01f`).
     2. Otherwise, `lowestPlatformBottom - 10f` across `data.platforms`.
     3. Otherwise, fallback `-50f`.

Crucially, `LevelData` (`Assets/Script/Contrast/Data/LevelData.cs`) **does not currently define an explicit `deathY` field**, and vertical fall death is coupled to the dynamic `lastSupportedFeet` + `HasPotentialLandingBelow()` heuristic and `cameraBoundary` geometry rather than a single configurable fixed world-space `deathY` threshold.

---

## 2. Point-by-Point Answers to Section A1 Audit Questions

| # | Audit Question | Pre-Change Source Evidence & Answer |
|---|---|---|
| **1** | Which file(s), class(es), and method(s) trigger Player death? | `Assets/Script/Contrast/Player/PlayerController.cs` — class `PlayerController`, method `ProcessUpdate(float deltaTime)` (`lines 411–423`) calls private method `Die()` (`lines 677–693`), which notifies `GameManager.Instance.OnPlayerDied()` (`Assets/Script/Contrast/Core/GameManager.cs:406–446`). |
| **2** | List every death cause currently present in the project. | Only **two vertical fall conditions** call `Die()`:<br>1. **Dynamic heuristic void-fall check** (`PlayerController.cs:411–418`): `currentFeet < lastSupportedFeet - voidGraceUnits && !HasPotentialLandingBelow()`.<br>2. **Level `KillY` threshold check** (`PlayerController.cs:421–423`): `transform.position.y < gm.KillY`.<br>*(Note: ≥50% solid overlap sets `State = PlayerState.Stuck` at `PlayerController.cs:391–396` and stops motion, but does not call `Die()`. There are no hazard spikes or enemy colliders.)* |
| **3** | How is vertical fall death currently detected? | Dual check at the end of `PlayerController.ProcessUpdate`:<br>• Check A (`lines 411–418`): Compares player feet (`transform.position.y - playerSize.y * 0.5f`) against `lastSupportedFeet - 3.75f` and scans `ColorPlatform.All` via `HasPotentialLandingBelow()`.<br>• Check B (`lines 421–423`): Compares player center (`transform.position.y`) against `GameManager.Instance.KillY`. |
| **4** | Is it based on a fixed world Y, camera bottom, platform bounds, trigger collider, timeout, velocity threshold, or a combination? | A **combination** of:<br>1. Relative drop distance from last grounded feet (`lastSupportedFeet - 3.75f`) combined with a dynamic all-platform parabolic reach scan (`HasPotentialLandingBelow()`), and<br>2. A level-derived `KillY` computed from `cameraBoundary` bottom (`minY - 1f`) or lowest platform bottom (`lowestBottom - 10f`). |
| **5** | Is `deathY` already present in `LevelData`, level JSON, `LevelLoader`, `GameManager`, `PlayerController`, or the HTML prototype? | • **HTML Prototype (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`):** Yes — `WORLD = { w: 5600, h: 1200, deathY: 1080 }` (`line 67`) and `level.world.deathY` in `__CE_LEVELS__` (`lines 91, 134, 179`, set to `null` for `AUTO` mode in `setupEditorEasyLevel` at `lines 370–378`).<br>• **Unity C# / JSON (`LevelData.cs`, `L01.json`–`L03.json`, `LevelLoader.cs`, `GameManager.cs`, `PlayerController.cs`):** `deathY` is **not** present in `LevelData.cs` or `L01..L03.json`; `LevelLoader` and `GameManager` expose `KillY` (computed dynamically in `ComputeKillY`). |
| **6** | If `deathY` or an equivalent value exists, what are its current values in L01, L02, L03, and custom/editor levels? | Before changes, `LevelLoader.ComputeKillY(LevelData)` (`LevelLoader.cs:621–653`) computes `KillY` from `data.cameraBoundary` (before Newton boundary expansion at `line 344`):<br>• **L01:** `cameraBoundary.pos.y = -12.664`, `size.y = 22.172` $\rightarrow$ `KillY = -12.664 - 11.086 - 1.0 = -24.75`<br>• **L02:** `cameraBoundary.pos.y = -7.937`, `size.y = 28.625` $\rightarrow$ `KillY = -7.937 - 14.3125 - 1.0 = -23.2495`<br>• **L03:** `cameraBoundary.pos.y = -21.328`, `size.y = 53.594` $\rightarrow$ `KillY = -21.328 - 26.797 - 1.0 = -49.125`<br>• **Custom/Editor levels without `cameraBoundary`:** `lowestPlatformBottom - 10f` (or `-50f` if no platforms).<br>However, in practice, the heuristic `HasPotentialLandingBelow()` check (`PlayerController.cs:411–418`) fires **much earlier** (`3.75` units below the last platform) whenever no platform lies inside its cone below the player. |
| **7** | Does the current death condition depend on camera position, camera bounds, or camera zoom? | Indirectly yes: `ComputeKillY` uses `data.cameraBoundary` (`cameraBoundary.position.y - cameraBoundary.size.y * 0.5f - 1f`). If `cameraBoundary` is modified or if `BuildRuntimeLevel` is called again after `cameraBoundary` was mutated in-place by Newton expansion (`LevelLoader.cs:367–370`), `ComputeKillY` changes. |
| **8** | Does changing camera zoom or framing affect when the Player dies? | Not directly from `Camera.orthographicSize`, **except** if `BuildRuntimeLevel` re-runs on an in-memory `LevelData` whose `cameraBoundary` was expanded by Newton chamber initialization (`LevelLoader.cs:367–370`). |
| **9** | In which phase of the frame lifecycle is the death check evaluated? | Inside `GameManager.Update()` (`GameManager.cs:270`) $\rightarrow$ `player.ProcessUpdate(Time.deltaTime)` (`PlayerController.cs:175`), after movement and collision resolution (`movementResolver.Resolve` and `ApplyMove` at `lines 353–381`) and after grounded state update (`lines 388–408`), at `lines 411–423`. |
| **10** | How does death interact with `GameManager` and automatic checkpoint respawn? | `PlayerController.Die()` (`lines 677–693`) sets `State = PlayerState.Dead`, clears `isGrounded`, `GroundedPlatform`, `riderDisplacement`, `jumpBufferTimer`, `coyoteTimer`, resets `jumpMotion`, plays death audio/visuals, and calls `GameManager.Instance.OnPlayerDied()` (`GameManager.cs:406–446`), which starts `AutoRespawnRoutine(0.18f)` (`lines 448–462`) $\rightarrow$ `RespawnAtLastCheckpoint()` (`lines 464–502`). |
| **11** | How does death interact with grounded state, jump motion, rider displacement, and Newton state? | `Die()` resets `isGrounded = false`, `GroundedPlatform = null`, `riderDisplacement = Vector2.zero`, `jumpBufferTimer = 0f`, `coyoteTimer = 0f`, and `jumpMotion.ResetMotion()`. Upon `RespawnAtLastCheckpoint()`, `EasyNewtonFinaleController.Instance?.OnPlayerRespawned()` (`EasyNewtonFinaleController.cs:238–266`) resets incomplete Newton measurement state if the player respawned outside the Newton zone. |
| **12** | Are there any duplicate or competing fall-death checks in different scripts? | Both fall-death checks live in `PlayerController.ProcessUpdate` (`lines 411–418` and `lines 421–423`), and they **compete**:<br>• Check 1 (`lastSupportedFeet - voidGraceUnits` + `!HasPotentialLandingBelow()`) is a dynamic heuristic that can prematurely kill the player during large drops or orbital jumps when no platform is inside its estimated horizontal cone.<br>• Check 2 (`transform.position.y < gm.KillY`) is the level floor threshold, which is often never reached because Check 1 kills the player first. |

---

## 3. Level Geometry & Lowest Playable Y Coordinates (L01, L02, L03)

Using the project's exact coordinate conversion (`UnityX = HtmlX / 64`, `UnityY = -HtmlY / 64`):

| Level | Lowest Static/Moving Platform Bottom (`UnityY`) | Newton Center `cy` (`UnityY`) | Newton Outer Orbit `r_K` (`u`) | Lowest Newton Orbit Bottom (`UnityY`) | Pre-Change `cameraBoundary` Bottom (`UnityY`) | Recommended Fixed `deathY` (`UnityY`) |
|---|---|---|---|---|---|---|
| **L01** | Platform #5 (`y = -17.1797, h = 1.1406`) $\rightarrow$ **`-17.7500`** | `-10.2969` (`659 px`) | `8.4201` (`k=6`) | **`-18.8889`** | `-23.7500` | **`-24.75`** |
| **L02** | Platform #0 (`y = -15.4688, h = 1.5625`) $\rightarrow$ **`-16.2500`** | `-8.2500` (`528 px`) | `6.8750` (`k=4`) | **`-15.2969`** | `-22.2495` | **`-23.25`** |
| **L03** | Platform #25 (`y = -41.7656, h = 0.7188`) $\rightarrow$ **`-42.1250`** | `-32.8125` (`2100 px`) | `9.0948` (`k=7`) | **`-42.0792`** | `-48.1250` | **`-49.125`** |

---

## 4. Identified Architectural Issues to Resolve in Task A

1. **Remove the Dynamic Heuristic Fall Check (`HasPotentialLandingBelow` / `lastSupportedFeet - voidGraceUnits`):**
   - Section A2 explicitly requires: *"Do NOT compute vertical fall death from: current camera bottom, camera orthographic size, Newton camera zoom state, screen resolution, aspect ratio, dynamic viewport bounds."* and *"Vertical fall death must be determined by a fixed world-space coordinate `deathY`."*
   - Removing the `lastSupportedFeet - voidGraceUnits` + `HasPotentialLandingBelow()` check eliminates premature mid-air deaths during long drops and Newton orbital platform jumps, leaving a single authoritative `playerWorldY < deathY` comparison.
2. **Add Configurable `deathY` to `LevelData` and Level JSONs (`L01`–`L03`):**
   - Add `public float deathY = float.NaN;` (or explicit `hasDeathY` / `deathY` sanitization) to `LevelData` and `LevelDataSanitizer`, populate `deathY` in `L01.json` (`-24.75`), `L02.json` (`-23.25`), and `L03.json` (`-49.125`), and have `LevelLoader.ComputeKillY(LevelData)` use `data.deathY` directly when finite, with a single deterministic fallback when loading legacy/custom JSON without `deathY`.
   - Furthermore, ensure `LevelLoader.ComputeKillY` is evaluated and stored in a fixed runtime property (`DeathY` / `KillY`) **before** any Newton camera boundary expansion and never changes when camera mode or zoom changes.

