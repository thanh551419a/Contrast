# Windows Standalone Build & Release Verification (`WINDOWS_BUILD_VERIFICATION.md`)

## 1. Build Execution Summary

The Windows 64-bit standalone release candidate **RC4** (`CONTRAST_Animation_CameraV2_RC4`) was built and verified via Unity 6 (`6000.5.8f1`) batchmode pipeline (`Contrast.Build.ContrastBuildPipeline.BuildGameWindowsAnimationCameraRC4`):

- **Build Command:**
  ```powershell
  "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "y:\Contrast\My project" -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameWindowsAnimationCameraRC4 -logFile "y:\Contrast\My project\Logs\unity_rc4_build.log"
  ```
- **Exit Code:** `0` (`BuildResult.Succeeded`)
- **Total Build Time:** `31.00s`

---

## 2. Release Artifact Inventory & SHA-256 Hashes

| Artifact | Path | Size | SHA-256 Hash | Status |
| --- | --- | --- | --- | --- |
| **RC4 Portable ZIP (New)** | `Release/Windows/CONTRAST_Animation_CameraV2_RC4.zip` | `46,041,716` bytes (`43.91 MB`) | `A751A4C84FBA00EC2E625E8D6A5338125A96053CD445E61BB48FDBDA3990A091` | **VERIFIED** |
| **RC4 Standalone Executable (New)** | `Release/Windows/CONTRAST_Animation_CameraV2_RC4/Game.exe` | `647,168` bytes (`0.62 MB`) | `C442C3AC78341F0BF27EA4BFAEECF3E4C98351F608B8841B5FE76B3B6D0F28A8` | **VERIFIED** |
| **RC3 Portable ZIP (Preserved)** | `Release/Windows/CONTRAST_DeathY_NewtonX_RC3.zip` | `44,790,215` bytes (`42.72 MB`) | `D1456ABF660769953CC81C1B38FD2D700E93B2E0C56392A084D512556591AF07` | **UNTOUCHED** (`2026-11-05 00:29:58`) |
| **RC2 Portable ZIP (Preserved)** | `Release/Windows/CONTRAST_JumpFix_RC2.zip` | `44,788,686` bytes (`42.71 MB`) | `AF2591001FA597C24726E5175904798265B9D15FFD7018E039059C338AEBC891` | **UNTOUCHED** (`2026-11-04 23:49:42`) |
| **RC1 Portable ZIP (Preserved)** | `Release/Windows/CONTRAST_Windows_Portable.zip` | `44,750,255` bytes (`42.68 MB`) | `7513A895DD9A1F455257ED20DD0251DB25116AF0FD6BC3C28DB687CC35D146A3` | **UNTOUCHED** (`2026-11-04 18:31:18`) |

---

## 3. Standalone Portable Archive Smoke Test

1. Extracted `Release/Windows/CONTRAST_Animation_CameraV2_RC4.zip` into an isolated temporary directory (`scratch/verify_rc4_portable`).
2. Verified package contents:
   - `Game.exe`
   - `UnityPlayer.dll`
   - `Game_Data/` (containing compiled assemblies, `SilhouetteMotion` sprite sheets & animation assets in `Resources`, and `L01`/`L02`/`L03` level data)
   - `MonoBleedingEdge/`
   - `Levels/` (`L01.json`, `L02.json`, `L03.json`, `Level_01.json`, `Level_02.json`, `Level_03.json`, `SampleLevel.*.json`)
   - `README.txt`
3. Launched `Game.exe` (`PID = 27020`) from the extracted folder:
   - Verified `Responding == True` and `HasExited == False` after startup initialization.
   - Cleanly terminated the process and removed temporary extraction directory.
