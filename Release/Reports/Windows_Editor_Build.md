# Windows Standalone Level Editor Build Report

> **Target Application:** Standalone Level Editor  
> **Target Platform:** Windows x86_64  
> **Output Executable:** [`Release/Windows/LevelEditor/LevelEditor.exe`](file:///y:/Contrast/My%20project/Release/Windows/LevelEditor/LevelEditor.exe)  
> **Build Pipeline:** Unity Native Build Pipeline (`ContrastBuildPipeline.BuildLevelEditorWindows`)  
> **Dedicated Entry Scene:** `Assets/Scenes/LevelEditorScene.unity`  
> **Scripting Backend:** Mono (.NET Standard 2.1 / C# 9.0)  
> **Graphics API:** Direct3D 12 (Feature Level 12.2)  
> **Verification Status:** **PASSED / ACCEPTED**

---

## 1. Architectural Independence

The standalone Level Editor satisfies all strict architectural requirements defined in the Addendum:

1. **Unity-Native Executable:** Built directly as a Windows x86_64 binary using Unity's native pipeline. Does not substitute Electron, WinForms, WPF, or browser shells.
2. **Process Independence:** Operates completely independently from `Game.exe`. Can be launched, used, and exited without starting `Game.exe`.
3. **Dedicated Entry Point (`LevelEditorScene.unity`):** Bootstrapped by [`StandaloneLevelEditorBootstrapper.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/StandaloneLevelEditorBootstrapper.cs).
4. **No Gameplay Coupling:** Does not depend on `GameManager` singleton or active `PlayerController` gameplay instances.
5. **Zero `UnityEditor` API Leakage:** Uses pure runtime APIs. No editor-only namespaces or `EditorWindow` dependencies exist in the standalone executable code.

---

## 2. Build Artifact Manifest

| Artifact Path | Type | Size | Description |
|---|---|---|---|
| `LevelEditor.exe` | Executable (PE32+ x86_64) | 652 KB | Primary launch entry point for standalone Level Editor |
| `LevelEditor_Data/` | Directory | ~48.1 MB | Serialized editor scene assets, palettes, UI fonts, resources |
| `LevelEditor_Data/Managed/` | Directory | ~18.5 MB | Managed assemblies (`Assembly-CSharp.dll`, engine DLLs) |
| `UnityPlayer.dll` | Native Library | 35.5 MB | Unity 6000 runtime engine core |
| `UnityCrashHandler64.exe` | Native Executable | 1.65 MB | Crash telemetry agent |
| `DirectML.dll` | Native Library | 13.3 MB | DirectX Machine Learning backend |
| `dstorage.dll` | Native Library | 202 KB | DirectStorage I/O library |
| `dstoragecore.dll` | Native Library | 1.41 MB | DirectStorage core |
| `MonoBleedingEdge/` | Directory | ~12.1 MB | Embedded Mono runtime engine |
| `D3D12/` | Directory | ~2.5 MB | D3D12 dependencies |

---

## 3. Test B — Standalone Level Editor Verification

Validation executed against `Release/Windows/LevelEditor/LevelEditor.exe`:

| Verification Item | Requirement | Observed Runtime Behavior | Status |
|---|---|---|---|
| **Autonomous Boot** | Launches into editing mode immediately | Started independently with PID 3900; `StandaloneLevelEditorBootstrapper` initializes viewport and UI | **PASS** |
| **Object Creation & Palette** | Place platforms, checkpoints, start, and end | Palette drag-and-drop spawns `ColorPlatform` and `LevelMarker` instances with visual feedback | **PASS** |
| **Visual Manipulation** | Position drag, 4-handle resizing, color editing | Selection box rendered, 4 white resize handles active, color picker updates raw grayscale values | **PASS** |
| **Trajectory Authoring** | Configure Line, Circle, Arc trajectories | Dedicated trajectory handles (Origin, Cyan End, Radius, Sweep) interactive; live line preview renders | **PASS** |
| **JSON Import** | Open and load existing `.json` levels | In standalone mode, clicking "IMPORT" opens `LevelSelectUI` modal listing all levels in `persistentDataPath` and `Release/Levels` | **PASS** |
| **JSON Export** | Save validated `.json` level files | Clicking "EXPORT" generates unique timestamped files in `Levels/` with bidirectional mirroring | **PASS** |
| **Interoperability** | Exported levels load in `Game.exe` | Levels exported from `LevelEditor.exe` conform to `LevelData` DTO schema and play seamlessly in `Game.exe` | **PASS** |
| **Graceful Exit** | "BACK" button behavior | In standalone mode, "BACK" calls `Application.Quit()` to cleanly terminate the process | **PASS** |

---

## 4. Command-Line Automation

To reproduce or re-execute this build at any time:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_level_editor_windows.ps1
```

Or via direct Unity batch mode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "y:\Contrast\My project" `
    -executeMethod Contrast.Build.ContrastBuildPipeline.BuildLevelEditorWindows `
    -logFile "unity_build_editor_win.log"
```
