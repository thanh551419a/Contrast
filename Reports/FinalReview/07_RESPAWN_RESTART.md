# 07 — AUTOMATIC CHECKPOINT RESPAWN & SEPARATE RESTART (`Reports/FinalReview/07_RESPAWN_RESTART.md`)

## 1. Reviewer Requirement

- **Automatic Checkpoint Respawn on Death:**
  - When the Player dies (falls out of bounds or hits a hazard), do **not** show a blocking `THẤT BẠI` (`GameOverUI`) modal requiring a button click.
  - Instead, immediately and automatically respawn the Player at the latest activated checkpoint (or the level's initial `StartPos` if no checkpoint has been activated yet).
- **Separate Full Level Restart Button:**
  - Provide a visible corner **Restart** button (`RESTART (R)`) and `R` keyboard shortcut during active gameplay that resets the entire level from its initial state (`StartPos`, uncollected Quang Điểm, initial trajectories, reset Newton sequence).

---

## 2. Implementation (`GameManager.cs`, `GameControlUI.cs`, `LevelLoader.cs`)

1. **Automatic Respawn on Death (`Assets/Script/Contrast/Core/GameManager.cs`):**
   - Updated `GameManager.OnPlayerDied()`:
     ```csharp
     public void OnPlayerDied()
     {
         if (State != GameState.Playing)
             return;

         Debug.Log("[GameManager] Player died -> automatic respawn at active checkpoint.");
         Retry();
     }
     ```
   - `Retry()` keeps `State = GameState.Playing`, calls `player.Initialize(levelLoader.ActiveRespawnPosition, levelLoader.ActiveRespawnColor)`, resets any in-progress Newton chamber sequence via `EasyNewtonFinaleController.ActiveInstance.ResetSequenceToStart()`, and snaps the camera cleanly via `cameraFollow.SnapToTarget()`.
   - Previously collected Quang Điểm and the activated checkpoint remain preserved across checkpoint respawns.
2. **Full Level Restart (`GameManager.Restart()` + `GameControlUI.cs`):**
   - `GameControlUI` renders the top-right `RESTART` button (`(-118f, -20f)`) during `GameState.Playing` and wires it to `gameManager.Restart()`.
   - `GameManager.Update()` now also accepts `Keyboard.current.rKey.wasPressedThisFrame` during `GameState.Playing` (when no UI input field is focused) to trigger `Restart()`.
   - `Restart()` reloads the current level via `LoadActiveGameplayLevel()`, which calls `LevelLoader.ClearRuntimeLevel()` and `BuildRuntimeLevel()`, resetting:
     - `ActiveRespawnPosition` back to `StartPos`,
     - All checkpoints back to unactivated state,
     - All Quang Điểm collectibles back to `0 / total`,
     - All moving platform trajectories and Newton orbital platforms `K` and `I` back to initial phase,
     - The Newton `K -> I -> K'` measurement sequence back to `WaitingForK1`.

