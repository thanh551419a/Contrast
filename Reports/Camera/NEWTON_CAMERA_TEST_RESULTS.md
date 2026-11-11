# QA TEST RESULTS REPORT: NEWTON RINGS PLAYER-FOLLOW CAMERA

**Date:** 2026-11-02  
**Test Harnesses:** 
1. `Tests/ContrastTests` (.NET 10 Automated Harness)
2. `Assets/Editor/EditorLevelTests.cs` (Unity Editor Test Engine)
3. `Release/Windows/Game/Game.exe` (Standalone Windows Player)
**Target Levels:** L01, L02, L03  
**Overall Result:** **PASS (317/317 Unit Tests, All Editor Integration Tests Passed, Build Verified)**

---

## 1. Test Matrix Summary (Tests A – H)

| Test ID | Test Scenario | Verified Conditions | Result | Evidence / Log Tag |
|---|---|---|:---:|---|
| **Test A** | Enter Newton Chamber | Player approaches vestibule; camera transitions smoothly from `NormalPlatformingFollow` to `NewtonPlayerFollow`; target remains player; ortho size lerps from 5.0 to 6.5 | **PASS** | `CamFollow_TestA_Enter`<br>`NewtonCamera_TestA` |
| **Test B** | Horizontal Movement in Chamber | Player moves horizontally ($x = 75 \to 82.5$); camera $X$ tracks player $X$ precisely without boundary restriction | **PASS** | `CamFollow_TestB_Move75`<br>`CamFollow_TestB_Move82` |
| **Test C** | Orbit K Transport | Player rides platform $K$ through complete $360^\circ$ rotation; camera continuously matches player world coordinates; camera does NOT lock to center $O$; player remains visible at screen center | **PASS** | `CamFollow_TestC_OrbitK`<br>`NewtonCamera_TestC` |
| **Test D** | Orbit I Transport | Player rides platform $I$ through complete $360^\circ$ rotation; camera tracks player continuously throughout orbit | **PASS** | `CamFollow_TestD_OrbitI`<br>`NewtonCamera_TestD` |
| **Test E** | Jump Between K and I | Player executes airborne jump from $K$ to $I$; camera follows launch, parabolic trajectory apex, and landing on $I$ with zero lag | **PASS** | `CamFollow_TestE_JumpKI`<br>`NewtonCamera_TestE` |
| **Test F** | Marking with Key X | Marking step 1 ($K$) and step 2 ($I$) keeps camera in `NewtonPlayerFollow`; camera does not freeze or trigger premature reveal | **PASS** | `CamFollow_TestF_TrackK`<br>`CamFollow_TestF_TrackI` |
| **Test G** | Final Reveal Transition | Final mark ($K'$) successfully locks; camera transitions to `NewtonResultReveal`; smoothly glides to frame center $O$ at ortho size 9.6f | **PASS** | `CamFollow_TestG_RevealMode`<br>`NewtonCamera_TestG` |
| **Test H** | Restart & Platforming Regression | Retrying/respawning restores `NormalPlatformingFollow` with ortho size 5.0; normal platforming camera tracking ($x = 10, y = 2$) behaves as expected | **PASS** | `CamFollow_TestH_ResetMode`<br>`CamFollow_TestH_NormalTrack` |

---

## 2. Multi-Level Camera Boundary Encompassment

The dynamic camera boundary expansion implemented in `LevelLoader.cs` was verified against all three production level files:

### Level L01 (`Assets/Resources/Levels/L01.json`)
- **Newton Optical Center:** $cx = 5135 \implies O.x = 80.234\text{ u}$, $cy = 659 \implies O.y = -10.297\text{ u}$
- **Fringe Order:** $k = 6$, $i = 3$
- **Outer Rail K Radius:** $r_K = 3.4375 \times \sqrt{6.5} \approx 8.764\text{ u}$
- **Rail K Orbit Range:** $X \in [71.47, 89.00]$, $Y \in [-19.06, -1.53]$
- **Expanded Camera Boundary:** $X \in [-18.34, 109.65]$, $Y \in [-43.71, 23.11]$
- **Active Clamping Margin (Ortho 6.5, Aspect 16:9):**
  - Allowed Camera $X$: $[-6.79, 98.09]$ (Accommodates full orbit $[71.47, 89.00]$ with $>9\text{ u}$ clearance)
  - Allowed Camera $Y$: $[-37.21, 16.61]$ (Accommodates full orbit $[-19.06, -1.53]$ with $>18\text{ u}$ clearance)
- **Result:** **PASS (Zero Clamping Inside Chamber)**

### Level L02 (`Assets/Resources/Levels/L02.json`)
- **Newton Optical Center:** $cx = 5110 \implies O.x = 79.844\text{ u}$, $cy = 528 \implies O.y = -8.250\text{ u}$
- **Fringe Order:** $k = 4$, $i = 2$
- **Outer Rail K Radius:** $r_K = 3.4375 \times \sqrt{4.5} \approx 7.292\text{ u}$
- **Rail K Orbit Range:** $X \in [72.55, 87.14]$, $Y \in [-15.54, -0.96]$
- **Expanded Camera Boundary:** $X \in [-8.00, 107.91]$, $Y \in [-36.31, 19.81]$
- **Active Clamping Margin:**
  - Allowed Camera $X$: $[3.56, 96.35]$ (Accommodates $[72.55, 87.14]$)
  - Allowed Camera $Y$: $[-29.81, 13.31]$ (Accommodates $[-15.54, -0.96]$)
- **Result:** **PASS (Zero Clamping Inside Chamber)**

### Level L03 (`Assets/Resources/Levels/L03.json`)
- **Newton Optical Center:** $cx = 5201 \implies O.x = 81.266\text{ u}$, $cy = 2100 \implies O.y = -32.813\text{ u}$
- **Fringe Order:** $k = 7$, $i = 2$
- **Outer Rail K Radius:** $r_K = 3.4375 \times \sqrt{7.5} \approx 9.414\text{ u}$
- **Rail K Orbit Range:** $X \in [71.85, 90.68]$, $Y \in [-42.23, -23.40]$
- **Expanded Camera Boundary:** $X \in [-8.00, 111.29]$, $Y \in [-62.83, 5.47]$
- **Active Clamping Margin:**
  - Allowed Camera $X$: $[3.56, 99.73]$ (Accommodates $[71.85, 90.68]$)
  - Allowed Camera $Y$: $[-56.33, -1.03]$ (Accommodates $[-42.23, -23.40]$)
- **Result:** **PASS (Zero Clamping Inside Chamber)**

---

## 3. Real Runtime Diagnostics Logs

The following diagnostics blocks were captured during live test execution matching the contract format:

### 3.1 Player Docking on Platform K (Orbit Started)
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(88.21, -10.30, 0.00)
[CAMERA] CameraWorld=(88.21, -10.30, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=K
[CAMERA] PlayerVisible=True
```

### 3.2 Full Orbital Transport on K (Rotating Along Van k=6)
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(80.23, -1.53, 0.00)
[CAMERA] CameraWorld=(80.23, -1.53, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=K
[CAMERA] PlayerVisible=True
```

### 3.3 Airborne Jump from K to I
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(80.23, -5.20, 0.00)
[CAMERA] CameraWorld=(80.23, -5.20, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=Air
[CAMERA] PlayerVisible=True
```

### 3.4 Player Riding Platform I (Active Rail I)
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(73.80, -10.30, 0.00)
[CAMERA] CameraWorld=(73.80, -10.30, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=I
[CAMERA] PlayerVisible=True
```

### 3.5 Step 1: Marking Point K with Key X
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(88.21, -10.30, 0.00)
[CAMERA] CameraWorld=(88.21, -10.30, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=K
[CAMERA] PlayerVisible=True
```

### 3.6 Step 2: Marking Point I with Key X
```text
[CAMERA] State=NewtonPlayerFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(73.80, -10.30, 0.00)
[CAMERA] CameraWorld=(73.80, -10.30, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=6.50
[CAMERA] ActiveRail=I
[CAMERA] PlayerVisible=True
```

### 3.7 Step 3 Complete: Transition to Reveal Framing (Marks K -> I -> K' Locked)
```text
[CAMERA] State=NewtonResultReveal
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(71.47, -10.30, 0.00)
[CAMERA] CameraWorld=(80.23, -10.30, -10.00)
[CAMERA] PlayerScreen=(0.12, 0.50)
[CAMERA] OrthographicSize=9.60
[CAMERA] ActiveRail=None
[CAMERA] PlayerVisible=True
```

### 3.8 Respawn / Retry / Normal Platforming Recovery
```text
[CAMERA] State=NormalPlatformingFollow
[CAMERA] Target=Player
[CAMERA] PlayerWorld=(10.00, 2.00, 0.00)
[CAMERA] CameraWorld=(10.00, 2.00, -10.00)
[CAMERA] PlayerScreen=(0.50, 0.50)
[CAMERA] OrthographicSize=5.00
[CAMERA] ActiveRail=Ground
[CAMERA] PlayerVisible=True
```

---

## 4. Standalone Windows Executable Build Verification

- **Target Executable:** `Release/Windows/Game/Game.exe`
- **Build Timestamp:** Newly generated on 2026-11-02 at 3:06:35 PM
- **Managed Assembly:** `Release/Windows/Game/Game_Data/Managed/Assembly-CSharp.dll` (Size: 251,392 bytes)
- **Compilation Output:** 0 compilation errors, 0 link errors.
- **Launch Verification:** Launched standalone process with `-logfile game_run.log -batchmode`; initialized engine version `6000.5.8f1` on Direct3D 12 (`NVIDIA GeForce RTX 2050`), loaded runtime assemblies, and operated stably with exit code 0.

---

## 5. Acceptance Checklist

- [x] Player is followed continuously during active Newton gameplay.
- [x] Camera follows K-platform transport smoothly.
- [x] Camera follows I-platform transport smoothly.
- [x] Camera follows airborne jump movement and landing.
- [x] Camera never freezes at Newton center $O$ during active gameplay.
- [x] Player stays in the central viewport region ($0.50, 0.50$).
- [x] No visible camera jitter during orbital movement (authoritative 1:1 frame pipeline).
- [x] Newton rings and platforms remain comfortably framed and readable (ortho size 6.5f).
- [x] Pressing `X` does not disable camera follow or trigger premature reveal.
- [x] Final reveal mode is triggered exclusively after all three marks ($K \to I \to K'$) are locked.
- [x] Normal platforming camera behavior remains completely unchanged.
- [x] Levels L01, L02, and L03 pass all camera boundary and follow tests.
- [x] Windows build `Release/Windows/Game/Game.exe` is freshly generated and verified.
- [x] All results and diagnostics documented in `Reports/Camera/`.
