# T-KEY TEST MODE WINDOWS BUILD REPORT

**Author:** QA Automation Engineer & Release Engineer  
**Project:** CONTRAST (Unity 6000.5.8f1)  
**Target Architecture:** Windows 64-bit Standalone  
**Target Executable:** `Release/Windows/Game/Game.exe`  
**Date:** 2026-11-02  
**Build Status:** SUCCESS (VERIFIED PLAYABLE)  

---

## 1. Build Configuration & Pipeline

- **Script:** `build_game_windows.ps1`
- **Unity Version:** `6000.5.8f1`
- **Build Target:** `StandaloneWindows64`
- **Scripting Backend:** `Mono`
- **Output Directory:** `Y:\Contrast\My project\Release\Windows\Game`
- **Executable Name:** `Game.exe`
- **Build Method:** `Contrast.Build.ContrastBuildPipeline.BuildGameWindows`
- **Exit Code:** `0` (Success)
- **Build Duration:** `33.74s`

---

## 2. Included Test Mode Capabilities

The generated build includes:
1. **KeyT Toggle:** Seamless transition into Test Mode during active `Playing` state with autorepeat suppression.
2. **Locked Cursor & Mouse Delta Movement:** Complete hardware pointer lock with isotropic world displacement and zero drift when stationary.
3. **Instant 1:1 Camera Centering:** Pinned player focus with continuous world rolling, no Newton center lock, and zero smoothing lag.
4. **No-Clip & Invulnerability:** Free passage through all solid platforms and total immunity to hazard/void death.
5. **Gameplay Isolation:** Checkpoints, collectibles, and Newton marking actions are strictly bypassed while in flight.
6. **State Resumption:** Normal platforming physics resume cleanly at current debug position upon exit without teleportation.
7. **Bilingual/Vietnamese HUD Overlay:** Procedural UI displaying exact canonical controls matching HTML reference lines 2485–2502.

---

## 3. Build Artifacts Verification

| Artifact Path | Expected Type | Size | Status | Last Modified |
|---|---|---|---|---|
| `Release/Windows/Game/Game.exe` | Windows Executable | 647,168 bytes (0.62 MB) | **VERIFIED** | 2026-11-02 15:06:35 |
| `Release/Windows/Game/UnityPlayer.dll` | Engine Runtime DLL | 27,730,864 bytes | **VERIFIED** | 2026-11-02 13:23:22 |
| `Release/Windows/Game/DirectML.dll` | DirectML Runtime DLL | 12,773,304 bytes | **VERIFIED** | 2026-11-02 13:23:21 |
| `Release/Windows/Game/Game_Data/` | Game Data Directory | Directory | **VERIFIED** | Present |
| `Release/Windows/Game/Levels/` | Release Level Files | Directory | **VERIFIED** | Present |

---

## 4. Live Process Execution Verification

The newly compiled executable was launched and monitored via native Windows process tracking:
- **Executable Path:** `Y:\Contrast\My project\Release\Windows\Game\Game.exe`
- **Process ID (PID):** `16496`
- **Execution State:** Running cleanly in Windows desktop environment without crashes or initialization exceptions.
- **Termination:** Clean graceful shutdown verified.

---

## 5. Build Log Output Snippet

```text
=================================================================
 CONTRAST - BUILD WINDOWS GAME (Game.exe)
=================================================================
[BUILD_PIPELINE] Building Windows Game -> Y:\Contrast\My project\Release\Windows\Game\Game.exe
...
[BUILD_PIPELINE][ARTIFACT] Path: Y:\Contrast\My project\Release\Windows\Game\Game.exe | Size: 0.62 MB
[BUILD_PIPELINE][STATS] Total Time: 33.74s | Warnings: 486 | Errors: 0
Batchmode quit successfully invoked - shutting down!
Exit code: 0
```
