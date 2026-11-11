# CONTRAST — FINAL REVIEW & POLISH ACCEPTANCE MATRIX (`Reports/FinalReview/FINAL_ACCEPTANCE_MATRIX.md`)

## 1. Executive Summary

All **8 reviewer change requests** have been implemented, verified through automated `.NET` and Unity Editor test suites, and packaged into a fresh Windows portable release (`Release/Windows/CONTRAST_Windows/Game.exe` and `Release/Windows/CONTRAST_Windows_Portable.zip`) while preserving the existing `Input → Event/Intent → Aggregation/Resolution → State Commit` pipeline, `L01–L03` campaign, Newton Rings `K–I–K'` measurement gameplay, Level Editor, Load Level, Editor Test Play, and `T` Debug Mode.

---

## 2. Final Acceptance Matrix (Tasks 1–8 & Regression Groups A–F)

| Item | Requirement | Key Modified Files | Automated Test Coverage | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Task 1** | **Fix Player Black Color Appearance:** Black phase renders as true `#050505` (`0.020, 0.020, 0.020`) with a crisp `#74818b` border rim (`OpticalRim`), clearly distinct from Gray (`#858585`, `0.522`) and White (`#ffffff`, `1.000`). | `PlayerVisualPresentation.cs`, `ColorClassifier.cs`, `PlayerController.cs` | `Task1_WhitePhase_BodyR`, `Task1_GrayPhase_BodyR`, `Task1_BlackPhase_BodyR/G/B`, `Task1_BlackVsGray_Separation`, `Task1_BlackPhase_RimStrokeR`, `Task1_BodySprite_UnbakedInterior` | **PASS** |
| **Task 2** | **Reduce Player Jump Distance to Match HTML Demo:** `moveSpeed = 5.625f` (`360 px/s`), `gravity = 25.78125f` (`1650 px/s²`), `jumpHeight = 3.183712f` (`jumpVel = 12.8125f = 820 px/s`), `maxFallSpeed = 16.40625f` (`1050 px/s`), flat horizontal jump range `5.5909 units` (`357.82 px`). | `PlayerController.cs`, `PlayerJumpMotion.cs`, `SampleScene.unity` | `Task2_MoveSpeed_MatchesHtml`, `Task2_JumpHeight_MatchesHtml`, `Task2_MaxFallSpeed_MatchesHtml`, `Task2_Gravity_MatchesHtml`, `Task2_JumpVelocity_MatchesHtml`, `Task2_JumpParity_MaxHorizontalRange` | **PASS** |
| **Task 3** | **Restore Phase Slider Dividers:** Cyclic ombre strip displays HTML `stripColorForPhase` gradient, outer border, and 4 vertical boundary divider lines (`PhaseDivider_0..3`) at `0.1554`, `0.3446`, `0.6554`, `0.8446` (`I = 0.78` and `I = 0.22`). | `PhaseTunerUI.cs` | `Task3_OmbreStrip_Exists`, `Task3_FourDividers_Exist`, `Task3_Divider0_AnchorX` .. `Task3_Divider3_AnchorX` | **PASS** |
| **Task 4** | **Improve Newton Rings Camera:** `NewtonPlayerFollow` tracks the Player smoothly across full $360^\circ$ orbits on `K` and `I`, airborne jumps, and `X`-marking without locking to `O`; `LevelDataSanitizer.CalculateCameraBoundary` includes Newton chamber and collectibles; `NewtonResultReveal` triggers only after all 3 marks (`K`, `I`, `K'`). | `LevelDataSanitizer.cs`, `CameraFollow.cs`, `EasyNewtonFinaleController.cs` | `Task4_SanitizerBounds_IncludesNewton`, `Test_Newton_CameraFollow_And_PlayerTracking`, `Test_NewtonCameraFollow_And_StateTransitions` | **PASS** |
| **Task 5** | **Change Special Columns to Gray-Blue:** `Universal` (`rawGrayscaleColor = 255`, HTML `FIXED` structural blocks) render in gray-blue (`#28323b` $\to$ `#151c24`, border `rgba(213,225,232,0.55)`, diagonal hatch lines) instead of Gold/Amber; `OrbitPlatform_K` remains visually White; collision remains always-solid. | `PlatformVisualPresentation.cs`, `ColorClassifier.cs`, `PlatformColorSubComponent.cs` | `Task5_Universal_LogicalState`, `Task5_Universal_GrayBlueTexture`, `Task5_Universal_BorderColorR`, `Task5_OrbitK_AlwaysSolid`, `Task5_OrbitK_RemainsWhiteVisual` | **PASS** |
| **Task 6** | **Change Quang Điểm Visual Presentation:** `LevelLoader` spawns `QuangDiemItem` from `data.collectibles` (`L01: 2`, `L02: 2`, `L03: 3`) with `QuangDiemVisualPresentation` (4-pointed golden star `#ffdfa0` + `#ffb74d` border + `#fffef1` highlight + radial golden glow + bobbing/rotation + 18-particle burst on collection) and top-right `✦ QUANG ĐIỂM` HUD counter. | `LevelMarker.cs`, `LevelMarkerVisualPresentation.cs`, `LevelLoader.cs`, `PhaseTunerUI.cs` | `Task6_L01_TotalQuangDiem_2`, `Task6_QuangDiemVisualPresentation_Attached`, `Task6_QuangDiem_CollectedOnTouch`, `Task6_QuangDiem_CountIncremented`, `Task6_QuangDiemHUDCard_Exists` | **PASS** |
| **Task 7** | **Automatic Checkpoint Respawn & Separate Restart:** `GameManager.OnPlayerDied()` automatically respawns the Player at the latest activated checkpoint (or `StartPos` if none) via `Retry()` without opening a modal `GameOverUI`; top-right `RESTART` button (`GameControlUI`) and `R` key perform a full level `Restart()`. | `GameManager.cs`, `GameControlUI.cs`, `LevelLoader.cs` | `Task7_CheckpointActivated`, `Task7_AutoRespawn_AtCheckpoint`, `Task7_AutoRespawn_PreservesCollectibles`, `Task7_Restart_ResetsCheckpointToStart`, `Task7_Restart_MovesPlayerToStart`, `Task7_Restart_ResetsCollectibles` | **PASS** |
| **Task 8** | **Allow Direct Level Selection:** Clicking **CHƠI THỬ** on the Main Menu opens `PlaySubMenuPanel` with **CHƠI TỪ ĐẦU** (`StartCampaign`: `L01 → L02 → L03`), **CHỌN MÀN** (`PlayDirectLevel(0..2)` for unlocked `L01`, `L02`, `L03`), and **QUAY LẠI MENU CHÍNH**. | `MainMenuUI.cs`, `GameManager.cs`, `EasyNewtonFinaleController.cs` | `Task8_PlaySubMenuPanel_Exists`, `Task8_PlayFromStartButton_Exists`, `Task8_SelectL01..L03_Exists`, `Task8_DirectPlay_L02_Collectibles`, `Task8_DirectPlay_L03_Collectibles` | **PASS** |
| **Groups A–F** | **Cross-Feature Regression:** `A/D` movement, `W/Space` jump, `←/→` phase tuning, `X` Newton marking (`K → I → K'`), `T` Debug Mode, automatic `K/I` orbital motion in `L01–L03`, `L03` 3 moving terrain platforms, Level Editor, Load Level, Editor Test Play. | All runtime & editor assemblies | Full `.NET` suite (`431 PASSED`) + Unity Editor suite (`485 PASSED`) | **PASS** |

---

## 3. Automated Test Execution Summary

| Test Suite | Command | Passed | Failed | Blocked | Result |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **Standalone `.NET` Test Suite** | `dotnet run --project Tests/ContrastTests/ContrastTests.csproj` | `431` | `0` | `0` | **PASS** |
| **Unity 6 Batchmode Test Suite** | `EditorLevelTests.RunAllEditorTests()` (`Unity 6000.5.8f1`) | `485` | `0` | `0` | **PASS** |

---

## 4. Windows Portable Release Verification

- **Build Command:** `powershell -ExecutionPolicy Bypass -File .\build_game_windows.ps1` (Exit code `0`)
- **Executable Path:** `Release/Windows/CONTRAST_Windows/Game.exe`
  - **Executable SHA-256:** `C442C3AC78341F0BF27EA4BFAEECF3E4C98351F608B8841B5FE76B3B6D0F28A8`
- **Portable Archive Path:** `Release/Windows/CONTRAST_Windows_Portable.zip` (`44,749,363` bytes / `42.68 MB`)
  - **Archive SHA-256:** `21F5EFF7FBFACBDF91258E379681A471F26402F11D7FED59267836825443E5A3`
- **Clean Temp-Folder Extraction & Launch Test:**
  - Extracted `Release/Windows/CONTRAST_Windows_Portable.zip` to `%TEMP%\CONTRAST_FinalReview_Verify`.
  - Launched `Game.exe` directly from the extracted folder; verified process alive (`PID 17536`), D3D11 renderer initialized, Input System initialized, and zero startup exceptions in `Player_Verify.log`.

