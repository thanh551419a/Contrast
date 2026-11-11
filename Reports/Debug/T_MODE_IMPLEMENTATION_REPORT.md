# T-KEY TEST MODE IMPLEMENTATION REPORT

**Author:** Senior Unity C# Gameplay Engineer & Senior JavaScript Reverse Engineer  
**Project:** CONTRAST (Unity 6000.5.8f1)  
**Date:** 2026-11-02  
**Status:** IMPLEMENTATION COMPLETE & VERIFIED  

---

## 1. Overview & Objectives

The goal of this task is to faithfully reverse-engineer the original HTML implementation of the `T`-key Test Mode (`TEST_MOVE`) from `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` and port its complete behavior into the Unity CONTRAST engine without designing a new debug system from scratch or modifying normal gameplay constants, level JSON files, or collision physics.

The port reproduces:
1. One-key toggle on `KeyT` during active gameplay with autorepeat suppression.
2. Hardware cursor lock (`CursorLockMode.Locked`, `Cursor.visible = false`) on activation, released on `Escape` or re-toggling.
3. Raw relative mouse movement conversion to viewport world displacement with zero drift when stationary.
4. Instant 1:1 camera centering tracking the player without smoothing lag or Newton center lock, rolling the world underneath.
5. Complete bypass of player gravity, normal movement forces, terrain collision/overlap (No-Clip), crush checks, platform rider carry, hazards, and void death.
6. Complete isolation of checkpoints, collectibles, and Newton marking actions during Test Mode navigation.
7. Seamless resumption of standard gameplay at the current debug position upon deactivation.
8. Faithful Vietnamese HUD overlay matching the canonical HTML control hints.

---

## 2. Modified Files & Exact Changes

### 2.1. `Assets/Script/Contrast/Camera/CameraFollow.cs`
- **Enum Extension:** Added `CameraMode.TestModeFreeMove`.
- **Mode Switching:** Added `public void SetTestModeFreeMove(bool enabled)` which caches `previousMode` and activates `CameraMode.TestModeFreeMove` on `true`, restoring `previousMode` on `false`.
- **Centering Pipeline (`ProcessCameraUpdate`):**
  ```csharp
  if (currentMode == CameraMode.TestModeFreeMove)
  {
      if (target != null)
      {
          transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
          if (cam != null && cam.orthographic)
          {
              cam.orthographicSize = normalOrthoSize;
          }
      }
      return;
  }
  ```
  This guarantees instant centering without smoothing delay, boundary clamping, or Newton Chamber center locking.

### 2.2. `Assets/Script/Contrast/Player/PlayerController.cs`
- **Properties & Injections:**
  - `public bool IsTestModeActive { get; private set; }`
  - `public static System.Func<Vector2> CustomMouseDeltaReader { get; set; }`
- **State Control (`SetTestMode`):**
  - Clears velocity, temporary grounded state, grounded platform, and platform rider displacement.
  - Updates cursor lock state: `Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;` (and unlocks on false).
- **Movement Pipeline (`ProcessUpdate`):**
  - When `IsTestModeActive`:
    - Preserves phase tuning via Arrow keys and color selection (HTML lines 1549–1552).
    - Reads relative mouse delta via `ReadMouseDelta()`.
    - Computes world displacement scaled by `unitsPerPixel = (2f * cam.orthographicSize) / Screen.height`.
    - Displaces `transform.position` directly.
    - Zeroes velocity, jump motion, and grounded state.
    - Returns immediately, skipping collision resolution, overlap push-out, and void fall checks.
- **Death Suppression (`Die`):**
  - Added `if (IsTestModeActive) return;` at the very start of `Die()`.

### 2.3. `Assets/Script/Contrast/Core/GameManager.cs`
- **HUD Reference:** Added `[SerializeField] private TestModeUI testModeUI;` instantiated in `Awake()`.
- **Controller Method (`SetTestMode`):**
  - Synchronizes `player.SetTestMode(enabled)`, `cameraFollow.SetTestModeFreeMove(enabled)`, and `testModeUI.Show(enabled)`.
- **Input & State Pipeline (`Update`):**
  - In `GameState.Playing`:
    - Checks `player.IsTestModeActive && EscapeKeyPressed()`: deactivates Test Mode without opening pause menu.
    - Checks `TKeyPressed()`: toggles `SetTestMode(!player.IsTestModeActive)`.
    - Passes `riderDisp = Vector2.zero` when `player.IsTestModeActive`.
- **Lifecycle & Focus Safety:**
  - `SetState()` deactivates Test Mode if state leaves `Playing`.
  - `OnApplicationFocus(bool hasFocus)` deactivates Test Mode if window loses focus.

### 2.4. `Assets/Script/Contrast/Level/LevelLoader.cs`
- In `ProcessPlayerInteractions(PlayerController player)`:
  - Guarded with `if (player == null || player.IsTestModeActive) return;` to prevent checkpoints or level victory from triggering during free flight.

### 2.5. `Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs`
- In `IsNewtonMarkPressedThisFrame()`:
  - Returns `false` if `player != null && player.IsTestModeActive`.
- In `AttemptCapture()`:
  - Returns immediately if `player != null && player.IsTestModeActive`.

### 2.6. `Assets/Script/Contrast/UI/TestModeUI.cs` (New)
- Created runtime procedural Canvas UI with exact canonical Vietnamese styling:
  1. `CHẾ ĐỘ THỬ — DI CHUYỂN TỰ DO` (Cyan bold header)
  2. `CHUỘT — DI CHUYỂN NHÂN VẬT` (White text)
  3. `VẬT LÝ — TẮT` (Light gray text)
  4. `VA CHẠM — TẮT` (Light gray text)
  5. `T — THOÁT CHẾ ĐỘ THỬ` (Amber bold text)
  6. `ESC — GIẢI PHÓNG CHUỘT` (Muted italic text)
- Shows and hides dynamically in response to `SetTestMode(bool)`.

---

## 3. Mathematical Parity & Resolution Independence

In the HTML reference:
$$dx = \text{movementX} \times \left(\frac{1920}{\text{clientWidth}}\right) \times \frac{1}{\text{zoom}} \times \text{sensitivity}$$
In Unity orthographic projection:
$$U_{\text{pixel}} = \frac{2 \times \text{cam.orthographicSize}}{\text{Screen.height}}$$
$$\vec{D}_{\text{world}} = \begin{pmatrix} \Delta x \times U_{\text{pixel}} \times \text{sensitivity} \\ \Delta y \times U_{\text{pixel}} \times \text{sensitivity} \end{pmatrix}$$

Because $\text{Screen.width} / \text{Screen.height} = \text{cam.aspect}$, horizontal and vertical scales are perfectly isotropic:
- $100$ pixels of mouse movement right displaces the player by exactly the same fraction of the screen width/height as $100$ pixels in the HTML reference.
- Moving the mouse by $0$ pixels produces exactly zero world displacement.

---

## 4. Architectural Invariants Preserved

1. **Single Update Loop:** All gameplay operations remain inside `GameManager.Update()` $\rightarrow$ `PlayerController.ProcessUpdate(dt)`. No secondary updates were introduced.
2. **Zero Level File Alterations:** Level JSON files (`L01.json`, `L02.json`, `L03.json`) were untouched.
3. **Zero Physics Constants Altered:** Gravity, jump velocity, move speed, and collision boxes for normal gameplay remain completely unmodified.
4. **Clean Scope:** Changes are strictly confined to the debug flight mechanics.
