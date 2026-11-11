# FIXED WORLD-SPACE `deathY` IMPLEMENTATION (`Reports/DeathY/DEATHY_IMPLEMENTATION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Status:** Implemented & verified against `D01`–`D12`.

---

## 1. Summary of Changes

Vertical fall death has been refactored from a dual dynamic/heuristic check (`HasPotentialLandingBelow()` + `cameraBoundary`-derived `KillY`) into a single, configurable, fixed world-space `deathY` threshold (`transform.position.y < effectiveDeathY`).

### Modified Files & Methods

| File | Method / Symbol | Change Description |
|---|---|---|
| `Assets/Script/Contrast/Data/LevelData.cs` | `LevelData.deathY` | Added `public float deathY = float.NaN;` to `LevelData` so every level JSON or runtime level can explicitly configure its world-space vertical fall death threshold. |
| `Assets/Script/Contrast/Data/LevelDataSanitizer.cs` | `Sanitize()`, `CalculateDefaultDeathY()`, `SerializeToJson()` | Validates `data.deathY` during level sanitization; if a legacy/custom JSON omits `"deathY"`, assigns a single deterministic fallback via `CalculateDefaultDeathY(data)` and persists `"deathY"` cleanly in `SerializeToJson()`. |
| `Assets/Resources/Levels/L01.json` | `"deathY": -24.75` | Configured explicit fixed world-space `deathY = -24.75` (safely below lowest platform `-17.75` and lowest Newton orbit `-18.89`). |
| `Assets/Resources/Levels/L02.json` | `"deathY": -23.25` | Configured explicit fixed world-space `deathY = -23.25` (safely below lowest platform `-16.25` and lowest Newton orbit `-15.30`). |
| `Assets/Resources/Levels/L03.json` | `"deathY": -49.125` | Configured explicit fixed world-space `deathY = -49.125` (safely below lowest platform `-42.125` and lowest Newton orbit `-42.08`). |
| `Assets/Script/Contrast/Level/LevelLoader.cs` | `DeathY`, `ComputeKillY(LevelData)`, `BuildRuntimeLevel()` | Changed `ComputeKillY` to return `data.deathY` directly when finite (falling back to `LevelDataSanitizer.CalculateDefaultDeathY(data)` when omitted), exposed `public float DeathY => KillY;`, and passed `player.SetLevelDeathY(KillY)` during level initialization. |
| `Assets/Script/Contrast/Core/GameManager.cs` | `DeathY` | Exposed `public float DeathY => levelLoader != null ? levelLoader.DeathY : -50f;`. |
| `Assets/Script/Contrast/Player/PlayerController.cs` | `ProcessUpdate()`, `GetEffectiveDeathY()` | Removed the dynamic `lastSupportedFeet - voidGraceUnits` and `HasPotentialLandingBelow()` heuristic void-fall check. Replaced vertical fall death with a single authoritative fixed world-space check: `if (!float.IsNaN(effectiveDeathY) && transform.position.y < effectiveDeathY) Die();`. |

---

## 2. Architectural Invariants Preserved

1. **Single Authoritative Vertical Fall Death Rule:**
   - Evaluated once per frame at the end of `PlayerController.ProcessUpdate(float deltaTime)` (`lines 441–449`).
   - Uses strict comparison `transform.position.y < effectiveDeathY`.
2. **100% Camera & Viewport Independence:**
   - `effectiveDeathY` does **not** depend on `Camera.main.transform.position`, `Camera.main.orthographicSize`, `CameraMode.NewtonFixed`, screen resolution, or aspect ratio.
   - `ComputeKillY(data)` is evaluated from `data.deathY` **before** any Newton camera boundary expansion in `LevelLoader.BuildRuntimeLevel()`.
3. **Respawn & Lifecycle Integration:**
   - Crossing `deathY` invokes `PlayerController.Die()` once (`State == PlayerState.Dead` guards against re-entry), triggering `GameManager.Instance.OnPlayerDied()` $\rightarrow$ `Retry()` $\rightarrow$ `LevelLoader.RetryCurrentSpawn()`, which restores the Player at the latest checkpoint with clean grounded/jump state and resets incomplete Newton measurement state.

