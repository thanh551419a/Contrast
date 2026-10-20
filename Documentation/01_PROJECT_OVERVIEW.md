# Project Overview: Contrast

> **Target Audience:** Systems engineers, gameplay programmers, and AI technical agents seeking an exact, fact-based understanding of the game's mechanics, engine configuration, scope boundaries, and development constraints.

---

## 1. Project Purpose & Genre

**Contrast** is a 2D puzzle-platformer designed around chromatic duality and deterministic mechanics.

### Core Gameplay Concept:
1. **Grayscale Spectrum:** Platforms exist with grayscale brightness values from 0.0 to 255.0.
2. **Logical Color Categorization:**
   - **White:** $0.0 \le v \le 84.0$ (Center: 0.0)
   - **Gray:** $85.0 \le v \le 169.0$ (Center: 127.0)
   - **Black:** $170.0 \le v \le 254.0$ (Center: 212.0)
   - **Universal / Everything:** $v = 255.0$ (Center: 255.0)
3. **Collision Inversion Rule:**
   - A player possessing logical color $C_p$ can walk, jump, and stand on platforms whose logical color $C_{plat} \neq C_p$.
   - A platform possessing the **same** logical color as the player ($C_{plat} == C_p$) is non-colliding and transparent, allowing the player to fall or walk directly through it.
   - **Universal Platforms ($v = 255.0$):** Solid to ALL player colors at all times.
4. **Active Chromatic Manipulation:** The player can cycle their logical color at will via number keys (`1`, `2`, `3`) or arrow keys, strategically solidifying and permeating platforms to navigate complex spatial obstacles.

---

## 2. Technical Stack and Engine Environment

| Attribute | Value | Verification Status |
|---|---|:---:|
| **Engine Version** | **Unity 6000.5.8f1** (`6000.5.8f1 (5cb7df797b7d)`) | Confirmed (`ProjectSettings/ProjectVersion.txt`) |
| **Render Pipeline** | Universal Render Pipeline (URP 2D) `17.6.0` | Confirmed (`Packages/manifest.json`, `UniversalRP.asset`) |
| **Input Subsystem** | Unity Input System `1.20.0` (with legacy fallback in code) | Confirmed (`Packages/manifest.json`, `PlayerInput.cs`) |
| **UI Framework** | Unity uGUI (`com.unity.ugui 2.5.0`) created programmatically | Confirmed (`Packages/manifest.json`, `LevelEditorUI.cs`) |
| **Target Platforms** | Windows Standalone x86_64, Windows Editor | Confirmed (`EDIT_LEVEL_TEST_REPORT.md`, `unity_batch.log`) |
| **Scripting Backend** | Mono / .NET Standard 2.1 (Editor), .NET 10.0 (Standalone tests) | Confirmed (`ContrastTests.csproj`) |

---

## 3. Runtime Scope vs. Editor Scope

The codebase operates across two distinct runtime modes governed by [`Contrast.Core.GameState`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameState.cs):

### 3.1 Runtime Gameplay Scope (`GameState.Playing`, `GameOver`, `Won`)
- **Player Lifecycle:** Controlled via `PlayerController`. Responsive to A/D movement, Spacebar jumping, and color cycling.
- **Custom AABB Physics:** Completely custom movement resolution (`PlayerMovementResolver`, `PlayerCollisionResolver`, `PlayerOverlapResolver`).
- **Moving Platforms:** `TrajectorySystem` updates all active trajectories each frame before player motion and calculates dynamic rider momentum transfer.
- **Interactive Hazards & Goals:**
  - Fall-death threshold (`KillY`) dynamically calculated from level geometry or camera bounds.
  - Checkpoint rectangles (`LevelMarkerType.CheckPoint`) that update the active respawn anchor upon contact.
  - End goal rectangles (`LevelMarkerType.End`) that trigger victory (`GameState.Won`).
- **HUD & UI Overlays:** Minimalistic in-game HUD (`GameControlUI`) exposing **RESTART** (full reload) and **RETRY** (respawn at latest checkpoint), plus overlay screens (`GameOverUI`).

### 3.2 Runtime Level Editor Scope (`GameState.Editing`)
- **Active Subsystem:** Controlled via `LevelEditorController` and rendered via `LevelEditorUI`.
- **Viewport Camera:** Middle-mouse drag pans the editor camera smoothly across the level canvas without altering world coordinates of placed objects.
- **Interactive Manipulation:**
  - Click-selection of platforms and level markers with visual bounding box highlights.
  - Body dragging for spatial repositioning.
  - Four directional resize handles (Left, Right, Top, Bottom) for interactive AABB dimension adjustment.
  - Real-time trajectory handles (Origin, Endpoint, Radius, Arc limits, Spatial bounding edges).
- **Palette Spawning:** Right-hand palette allows dragging new platforms, start markers, checkpoints, and end goals into the map.
- **Two-Step Platform Deletion:** Safe deletion workflow with 3-second confirmation timeout to prevent accidental asset destruction.
- **Serialization & File I/O:** In-editor JSON import (via native OS file picker) and export to formatted level files.
- **Instant Test Play Bridge:** **TEST PLAY** button launches gameplay directly with in-memory `LevelData` without file saving.

---

## 4. Special Project Characteristics & Constraints

### 4.1 Custom Collision Over Unity Physics2D (Confirmed)
> [!IMPORTANT]
> The project does **NOT** use Unity's built-in 2D physics engine (`BoxCollider2D`, `CircleCollider2D`, `Physics2D.Raycast`, etc.) for gameplay collision.
- Platforms do not require active physics colliders.
- `ColorPlatform.GetAabb()` returns the authoritative axis-aligned bounding box `Rect`.
- `PlayerController` uses an attached `Rigidbody2D`, but its configuration sets `simulated = false`. It is utilized strictly as a data container for gravity acceleration and vertical velocity.
- Collision resolution is deterministic, discrete, and purely mathematical.

### 4.2 Non-Rotated Platform Convention (Confirmed)
- A platform GameObject is **never rotated** in world space (`transform.rotation == Quaternion.identity`).
- Even when a platform follows a diagonal or rotated trajectory (e.g., $45^\circ$), the rotation angle applies exclusively to the trajectory evaluation mathematics. The platform body remains strictly axis-aligned.

### 4.3 Programmatic UI Construction (Confirmed)
- The user interface is not pre-authored as prefab assets or Unity scene objects.
- All canvases, buttons, input fields, toggles, scrollbars, and dropdowns are dynamically constructed via C# scripts using `UnityEngine.UI` (`LevelEditorUI.cs`, `MainMenuUI.cs`, `GameOverui.cs`, `GameControlUI.cs`).
- This guarantees zero missing asset GUID errors across differing development environments.

### 4.4 Junction Architecture (Confirmed)
- `Assets/Script` and `Assets/Levels` are NTFS Junction directory symlinks pointing to external repository paths (`Y:\Contrast\Code\Script` and `Y:\Contrast\Code\Levels`).
- File search tools must account for directory reparse points.

---

## 5. Main Scenes and Assets Inventory

| Scene / Asset | File Path | Status / Purpose |
|---|---|---|
| **SampleScene** | `Assets/Scenes/SampleScene.unity` | Authoritative runtime scene containing `Player`, `Main Camera`, `Global Light 2D`, and `GameSystems`. |
| **URP2DSceneTemplate** | `Assets/Settings/Scenes/URP2DSceneTemplate.unity` | Engine default 2D template. |
| **_Recovery/0.unity** | `Assets/_Recovery/0.unity` | Stale auto-save recovery scene from an earlier iteration. |
| **TrajectoryShowcaseLevel** | `Assets/Levels/TrajectoryShowcaseLevel.json` | 15-platform level showcasing all 10 trajectory configurations. |
| **SampleLevel** | `Assets/Levels/SampleLevel.json` | Baseline sample level referenced by `LevelLoader`. |
| **ContrastLevel** | `Assets/Levels/ContrastLevel.json` | Authored stage used for regression tests. |

---

## 6. Project Evidence Classification

- **Confirmed Facts:**
  - Unity version `6000.5.8f1` verified via project settings.
  - Custom AABB collision logic confirmed in `MovementGeometry.cs` and `PlayerCollisionResolver.cs`.
  - Kinematic formulas and test results verified via `Tests/ContrastTests/Program.cs` (91/91 passed) and `Assets/Editor/EditorLevelTests.cs` (58/58 passed).
  - Trajectory coordinate export contract confirmed in `LevelEditorController.BuildExportData()`.
- **Inferred Facts:**
  - `Contrast.UI.UIManager` in `SampleScene.unity` is a legacy reference from an earlier UI architecture before `MainMenuUI` and `LevelEditorUI` were split into standalone scripts.
- **Unknown / Unconfirmed:**
  - Production build targets beyond Windows Standalone x86_64 are not defined in the current workspace.
