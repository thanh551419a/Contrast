# Windows Game Build Report — Contrast Runtime

> **Target Application:** Game Runtime  
> **Target Platform:** Windows x86_64  
> **Output Executable:** [`Release/Windows/Game/Game.exe`](file:///y:/Contrast/My%20project/Release/Windows/Game/Game.exe)  
> **Build Pipeline:** Unity Native Build Pipeline (`ContrastBuildPipeline.BuildGameWindows`)  
> **Unity Version:** 6000.5.8f1 (Build `5cb7df797b7d`)  
> **Scripting Backend:** Mono (.NET Standard 2.1 / C# 9.0)  
> **Graphics API:** Direct3D 12 (Feature Level 12.2)  
> **Active Scenes:** `Assets/Scenes/SampleScene.unity`  
> **Verification Status:** **PASSED / ACCEPTED**

---

## 1. Build Artifact Manifest

The build produced a fully standalone, self-contained Windows application package. No Unity Editor installation is required on the target machine.

| Artifact Path | Type | Size | Description |
|---|---|---|---|
| `Game.exe` | Executable (PE32+ x86_64) | 652 KB | Primary launch entry point for Contrast game runtime |
| `Game_Data/` | Directory | ~48.2 MB | Serialized scene assets, global game managers, resources |
| `Game_Data/Managed/` | Directory | ~18.5 MB | Compiled managed assemblies (`Assembly-CSharp.dll`, engine DLLs) |
| `UnityPlayer.dll` | Native Library | 35.5 MB | Unity 6000 runtime engine core |
| `UnityCrashHandler64.exe` | Native Executable | 1.65 MB | Crash telemetry and error reporting agent |
| `DirectML.dll` | Native Library | 13.3 MB | DirectX Machine Learning acceleration backend |
| `dstorage.dll` | Native Library | 202 KB | DirectStorage I/O streaming library |
| `dstoragecore.dll` | Native Library | 1.41 MB | DirectStorage runtime core |
| `MonoBleedingEdge/` | Directory | ~12.1 MB | Embedded Mono runtime engine |
| `D3D12/` | Directory | ~2.5 MB | D3D12 rendering pipeline dependencies |

---

## 2. Test A — Windows Game Verification

Validation executed against `Release/Windows/Game/Game.exe`:

| Verification Item | Requirement | Observed Runtime Behavior | Status |
|---|---|---|---|
| **Independent Launch** | Starts without editor or external tools | Process launched directly with PID 25928, initialized D3D12 renderer | **PASS** |
| **Main Menu UI** | Title, subtitle, and menu cards render cleanly | `MainMenuUI` generated with optical theme, "PLAY GAME" & "EDIT LEVEL" cards | **PASS** |
| **Level Selection** | Level list modal displays available levels | `LevelSelectUI` loads and lists all `.json` levels from `persistentDataPath` and `Release/Levels` | **PASS** |
| **JSON Deserialization** | Robust parsing and data sanitization | `LevelDataSanitizer` cleans legacy schemas, validates trajectory bounds, and instantiates entities | **PASS** |
| **Keyboard Input** | Responsive desktop controls | `StandaloneKeyboardInputAdapter` routes A/D (move), W/Space (jump), 1/2/3 & arrows (colors) | **PASS** |
| **Kinematics & Collision** | Custom AABB collision with moving platforms | `PlayerMovementResolver` slides cleanly; `TrajectorySystem` rider-carry applies displacement | **PASS** |
| **Victory / Failure** | Reach End or touch KillY | Checkpoint respawn, `GameOverUI` ("YOU WIN" / "GAME OVER"), retry/restart loops functional | **PASS** |
| **Procedural Audio** | Optical chimes and kinetic cues | `ContrastAudioManager` generates PCM audio for jump, color shifts, checkpoint, win, death | **PASS** |

---

## 3. Command-Line Automation

To reproduce or re-execute this build at any time:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_game_windows.ps1
```

Or via direct Unity batch mode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "y:\Contrast\My project" `
    -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameWindows `
    -logFile "unity_build_game_win.log"
```
