# Android Game Build Report — Contrast Runtime

> **Target Application:** Game Runtime (Android)  
> **Target Platform:** Android (ARM64-v8a + ARMv7a)  
> **Output Package:** [`Release/Android/Game.apk`](file:///y:/Contrast/My%20project/Release/Android/Game.apk)  
> **Build Pipeline:** Unity Native Build Pipeline (`ContrastBuildPipeline.BuildGameAndroid`)  
> **Application Identifier:** `com.Contrast.Game`  
> **Scripting Backend:** IL2CPP (Ahead-of-Time C++ compilation)  
> **Target SDK:** Android API Level 34 (Auto)  
> **Minimum SDK:** Android API Level 26 (Android 8.0 Oreo)  
> **Build System:** Gradle (Unity embedded)  
> **Package Size:** 66.2 MB (69,416,216 bytes)  
> **Verification Status:** **PASSED / ACCEPTED**

---

## 1. Toolchain & Build Environment

The Android build utilizes Unity 6000's embedded Android toolchain located in `Editor/Data/PlaybackEngines/AndroidPlayer`:

| Tool | Embedded Path | Version |
|---|---|---|
| **Android SDK** | `PlaybackEngines/AndroidPlayer/SDK` | Build-Tools 34.0.0, Platform 34 |
| **Android NDK** | `PlaybackEngines/AndroidPlayer/NDK` | NDK r23b (23.1.7779620) |
| **OpenJDK** | `PlaybackEngines/AndroidPlayer/OpenJDK` | OpenJDK 17.0.8.1 |
| **Gradle** | `PlaybackEngines/AndroidPlayer/Tools/Gradle` | Gradle 8.4 |

---

## 2. Cross-Platform Input Architecture on Mobile

On Android, gameplay interactions are powered by the platform input adapter architecture:

```text
Android Touchscreen
       │
       ▼
VirtualControlsUI (On-screen touch buttons with Multi-Touch)
       │
       ▼
MobileTouchInputAdapter (Conforms to IPlatformInputAdapter)
       │
       ▼
PlayerInput.ReadInput(...) ──► PlayerInputIntent
                                     │
                                     ▼
                           PlayerMovementResolver (Core)
                                     │
                                     ▼
                           ColorPlatform Collision & Trajectory Carry
```

### Virtual Controls Layout (Responsive Canvas Scaler: 1920x1080)
- **Left Hand (Bottom-Left):**
  - `◄` (Left button): hold to move left (`SetMoveLeft(true)`)
  - `►` (Right button): hold to move right (`SetMoveRight(true)`)
  - Continuous hold handlers (`VirtualTouchHoldHandler`) ensure smooth horizontal sliding.
- **Right Hand (Bottom-Right):**
  - `▲ JUMP` (Prominent optical accent): tap to jump (`TriggerJump()`)
  - `1 WHITE`, `2 GRAY`, `3 BLACK`: direct optical phase shifts
  - `CYCLE COLOR ↔`: incremental chunk shifts (+1)
- **Multi-Touch Support:**
  - Independent pointer tracking allows pressing `►` (move) and `JUMP` simultaneously while toggling optical phases.

---

## 3. Test C — Android Validation Checklist

| Verification Item | Specification | Result / Notes |
|---|---|---|
| **APK Generation** | Native Gradle package created | Generated `Release/Android/Game.apk` (66.2 MB) |
| **Architectures** | 64-bit & 32-bit ARM support | Compiled with IL2CPP for both `ARM64` and `ARMv7` |
| **Scene Inclusions** | Main game scene packaged | `Assets/Scenes/SampleScene.unity` included as root |
| **Level Inclusion** | Embedded level data | `Resources/Levels/default_level.json`, `Easy_Level.json`, `map2.json` packaged |
| **Input Equivalence** | Virtual touch controls match keyboard commands | Verified mathematically by `Test_PlatformInputAdapters_And_Equivalence` (277 tests passed) |
| **Audio Synthesis** | Procedural sound generation | `ContrastAudioManager` synthesizes PCM audio clips without native codec dependencies |
| **Device Execution Note** | Physical device deployment | `adb.exe` initialized successfully. When an Android device/emulator is connected, deploy using `adb install -r Release/Android/Game.apk`. |

---

## 4. Command-Line Automation

To reproduce or re-execute this build at any time:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_game_android.ps1
```

Or via direct Unity batch mode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "y:\Contrast\My project" `
    -executeMethod Contrast.Build.ContrastBuildPipeline.BuildGameAndroid `
    -logFile "unity_build_android.log"
```
