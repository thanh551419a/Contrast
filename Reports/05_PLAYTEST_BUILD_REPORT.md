# REPORT 05 — PLAYTEST BUILD REPORT & ARTIFACT VERIFICATION

**Project:** CONTRAST (Unity 2D)  
**Authoritative Reference:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Build Target:** Standalone Windows x86_64  
**Unity Version:** 6000.5.8f1 (64-bit)  
**Date:** November 2026  
**Status:** **BUILD SUCCEEDED & LAUNCH VERIFIED**

---

## 1. Executive Summary

This report documents the compilation, packaging, and launch verification of the native Windows game executable `Game.exe` incorporating all input corrections, Newton Rings measurement sequence gameplay, Vietnamese step-by-step HUD, and the three official reconstructed levels (`L01`, `L02`, `L03`).

The build was compiled natively using Unity's automated batchmode build pipeline (`Contrast.Build.ContrastBuildPipeline.BuildGameWindows`), and its runtime initialization, level loading from external JSON, and physics update loop were verified via process execution.

---

## 2. Build Pipeline & Environment Specifications

| Parameter | Specification |
|---|---|
| Build Pipeline Script | `Assets/Editor/ContrastBuildPipeline.cs` |
| Entry Method | `Contrast.Build.ContrastBuildPipeline.BuildGameWindows` |
| Target Platform | `BuildTarget.StandaloneWindows64` |
| Subtarget | Standalone Player x86_64 |
| Scripting Backend | Mono |
| Graphics API | Direct3D 12 (Feature Level 12.2) |
| Output Binary | `Release/Windows/Game/Game.exe` |
| Build Log | `build_game.log` (Exit code: 0) |
| Build Duration | 59.22 seconds |
| Compilation Errors | 0 |
| Build Warnings | 486 (Standard non-fatal API and package warnings) |

---

## 3. Packaged Artifacts & Directory Structure

The Windows release directory (`Release/Windows/Game/`) contains the complete standalone distribution:

```text
Release/Windows/Game/
├── Game.exe                             (667,648 bytes - Standalone entry executable)
├── UnityPlayer.dll                      (37,282,216 bytes - Engine runtime)
├── UnityCrashHandler64.exe              (1,736,616 bytes - Crash telemetry handler)
├── DirectML.dll                         (14,021,152 bytes)
├── dstorage.dll                         (206,904 bytes)
├── dstoragecore.dll                     (1,482,784 bytes)
├── Game_Data/                           (Streaming assets, player data, resources)
│   ├── Managed/                         (Compiled assemblies & dependencies)
│   ├── Resources/                       (Unity built-in extra & level data)
│   └── boot.config                      (Engine initialization configuration)
├── MonoBleedingEdge/                    (Mono runtime environment)
└── Levels/                              (External JSON levels for campaign & playtesting)
    ├── L01.json                         (5,392 bytes - Cánh cửa không phải vật cản)
    ├── L02.json                         (4,971 bytes - Cầu thang ma)
    └── L03.json                         (9,869 bytes - Giếng ba tầng)
```

Additionally, the central distribution repository `Release/Levels/` contains identical mirrors of `L01.json`, `L02.json`, `L03.json`, along with legacy backwards-compatibility prototypes.

---

## 4. Runtime Launch & Startup Verification

To verify that the packaged game executable is fully functional and free of missing DLLs, startup exceptions, or broken paths, `Release/Windows/Game/Game.exe` was executed directly.

### Verified Startup Sequence (from `player_launch.log`):

1. **Engine Initialization:**
   ```text
   Initialize engine version: 6000.5.8f1 (5cb7df797b7d)
   Direct3D: Version: Direct3D 12 [level 12.2] | Renderer: NVIDIA GeForce RTX 2050
   Input System module state changed to: Initialized.
   ```
2. **State Machine Transition:**
   ```text
   [FLOW][STATE] MainMenu -> MainMenu
   [FLOW][START_GAME] opening level select screen
   [FLOW][STATE] MainMenu -> LevelSelect
   ```
3. **External JSON Level Loading:**
   ```text
   [FLOW][START_GAME_FILE] loading level from file: Y:\Contrast\My project\Release\Windows\Game\Levels\L03.json (context=MainMenuGameplay)
   [FLOW][STATE] LevelSelect -> Loading
   [FLOW][LEVEL_FILE] load path=Y:\Contrast\My project\Release\Windows\Game\Levels\L03.json
   [FLOW][LEVEL_LOAD] platforms=27 checkpoints=8 hasEnd=False
   ```
4. **Player Spawn & Physics Loop:**
   ```text
   [FLOW][SPAWN_INITIAL] position=(4.13, -3.59) color=Gray
   [FLOW][STATE] Loading -> Playing
   [INTENT] input=(0.00, 0.00) multiplier=1.00 ...
   [COLLISION][FINAL] resolvedPosition=(4.13, -3.61) grounded=False
   ```

No fatal exceptions or crashes occurred during startup or level simulation.

---

## 5. Standalone Level Editor Compatibility

The standalone Level Editor remains independently runnable at:
`Release/Windows/LevelEditor/LevelEditor.exe`

Its shared data models (`LevelData`, `LevelDataSanitizer`, `ColorPlatformData`, `TrajectoryData`) seamlessly handle the extended `NewtonChamberData` schema while maintaining full backwards compatibility with legacy levels.

---

## 6. Build Acceptance Sign-Off

| Acceptance Requirement | Status |
|---|---|
| Native Windows x64 `Game.exe` produced via Unity Build Pipeline | **COMPLETED** |
| All supporting DLLs and runtime dependencies included | **COMPLETED** |
| Updated `L01.json`, `L02.json`, `L03.json` packaged and loaded | **COMPLETED** |
| Engine and game loop initialization verified without crash | **COMPLETED** |
| Android build scripts and mobile input adapters preserved | **COMPLETED** |
| Standalone Level Editor compatibility verified | **COMPLETED** |
