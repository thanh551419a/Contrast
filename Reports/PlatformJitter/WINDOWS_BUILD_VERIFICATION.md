# CONTRAST — WINDOWS PORTABLE BUILD & VERIFICATION REPORT

## 1. Build Execution Summary

- **Unity Editor Version:** `6000.5.8f1 (5cb7df797b7d)`
- **Build Command:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\build_game_windows.ps1
  ```
- **Entry Method:** `Contrast.Build.ContrastBuildPipeline.BuildGameWindows`
- **Build Log:** `y:\Contrast\My project\unity_build_game_win.log`
- **Exit Code:** `0` (`BUILD SUCCEEDED`)

---

## 2. Generated Release Artifacts & Integrity Hashes

| Artifact | Path | Size (Bytes) | Size (MB) | SHA-256 Checksum |
| :--- | :--- | :---: | :---: | :--- |
| **Windows Executable** | `Release/Windows/CONTRAST_Windows/Game.exe` | `647,168` | `0.62 MB` | `C442C3AC78341F0BF27EA4BFAEECF3E4C98351F608B8841B5FE76B3B6D0F28A8` |
| **Portable ZIP Archive** | `Release/Windows/CONTRAST_Windows_Portable.zip` | `44,750,255` | `42.68 MB` | `7513A895DD9A1F455257ED20DD0251DB25116AF0FD6BC3C28DB687CC35D146A3` |

---

## 3. Clean Extraction & Startup Smoke Verification

1. **Extraction Test:**
   - Extracted `Release/Windows/CONTRAST_Windows_Portable.zip` into an isolated temporary folder (`%TEMP%\CONTRAST_Portable_Verify_250742320`).
   - Verified presence of `Game.exe`, `UnityPlayer.dll`, `MonoBleedingEdge/`, and `Game_Data/` (including packaged `Resources/Levels/L01`, `L02`, `L03`).
2. **Startup Smoke Test:**
   - Launched extracted `Game.exe -logfile smoke_player.log`.
   - Verified process remained alive and running cleanly (`SMOKE_RUNNING_AT_4S=True`), initialized Input System, Direct3D 11 renderer, and Mono runtime assemblies with `0` startup exceptions.
   - Terminated test process and cleaned up temporary extraction directory.

