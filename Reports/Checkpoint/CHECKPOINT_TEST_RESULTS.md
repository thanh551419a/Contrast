# Checkpoint & Invisible Marker Gameplay Verification (`CHECKPOINT_TEST_RESULTS.md`)

## 1. Automated Verification Summary

All checkpoint placement, invisible gameplay rendering, activation, death/respawn restoration, and post-respawn jump tests passed with **0 failures** in both the Unity 6 EditMode/PlayMode test runner (`Assets/Editor/EditorLevelTests.cs`) and the standalone `.NET 10.0` test runner (`Tests/ContrastTests/Program.cs`).

---

## 2. Test Results Matrix (`F3` & `Task C/D`)

| Test ID | Level / Target | Assertion | Result |
| --- | --- | --- | --- |
| `Cp_F3_01_L01_Count` | `L01` | Spawns all `5` authored checkpoints (`LevelMarkerType.CheckPoint`) | **PASS** |
| `Cp_F3_02_L01_AllInvisibleDuringGameplay` | `L01` | Every checkpoint `GameObject` is active (`activeInHierarchy == true`), while every checkpoint `SpriteRenderer` (`root` + `LevelMarkerVisualPresentationRoot`) has `enabled == false` | **PASS** |
| `Cp_F3_01_L02_Count` | `L02` | Spawns all `5` authored checkpoints (including new pre-Newton checkpoint at index `4`) | **PASS** |
| `Cp_F3_02_L02_AllInvisibleDuringGameplay` | `L02` | Every `L02` checkpoint `GameObject` is active while all checkpoint `SpriteRenderer`s have `enabled == false` | **PASS** |
| `Cp_F3_01_L03_Count` | `L03` | Spawns all `8` authored checkpoints | **PASS** |
| `Cp_F3_02_L03_AllInvisibleDuringGameplay` | `L03` | Every `L03` checkpoint `GameObject` is active while all checkpoint `SpriteRenderer`s have `enabled == false` | **PASS** |
| `Cp_F3_03_L02PreNewtonCheckpointActivates` | `L02` (`checkPoint[4]`) | Player walking onto `(67.617188, -8.734375)` activates the pre-Newton checkpoint via AABB overlap and updates `LevelLoader.CurrentSpawnPosition` to `(67.617188, -8.734375)` | **PASS** |
| `Cp_F3_04_L02PreNewtonCheckpointColor` | `L02` (`checkPoint[4]`) | Pre-Newton checkpoint saves `LogicalColor.Gray` (`color = 127.0`) | **PASS** |
| `Cp_F3_05_DiesBelowL02DeathY` | `L02` | Falling below `deathY = -23.25` transitions `PlayerController.State` to `PlayerState.Dead` | **PASS** |
| `Cp_F3_06_RespawnsAtNewL02Checkpoint` | `L02` | `LevelLoader.RetryCurrentSpawn()` restores Player to `(67.617188, -8.734375)` instead of level start `(-13.125, -4.359375)` | **PASS** |
| `Cp_F3_07_GroundedOnPreNewtonPlatformAfterRespawn` | `L02` | Respawned Player feet (`y = -9.234375`) rest flush on pre-Newton platform `13` top (`yMax = -9.234375`), immediately establishing `IsGrounded == true` | **PASS** |
| `Cp_F3_08_JumpWorksImmediatelyAfterL02CheckpointRespawn` | `L02` | Pressing Jump (`W`/`Space`) on Frame 1 after respawning at the new `L02` pre-Newton checkpoint executes a full jump (`vy = 12.8125`) | **PASS** |
| `RC4_L02_CheckpointCount5` | `.NET 10.0` Harness | `Assets/Resources/Levels/L02.json` sanitizes cleanly with `checkPoint.Length == 5` | **PASS** |
| `RC4_L02_PreNewtonCpGroundedPos` | `.NET 10.0` Harness | `checkPoint[4]` at `(67.617188, -8.734375)` places Player feet on `platforms[13]` top `(-9.234375)` | **PASS** |
| `Task7_CheckpointExists` / `Task7_AutoRespawn_AtCheckpoint` | `L01` Regression | Existing checkpoint activation, collectible preservation on respawn, and full level restart reset remain 100% functional | **PASS** |

