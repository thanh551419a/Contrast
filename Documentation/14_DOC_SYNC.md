# Documentation Synchronization & Freshness Log: Contrast

> **Purpose:** Tracks documentation currency, source snapshots, covered areas, and flags any drift between documentation and live source code.

---

## 1. Documentation Metadata

| Attribute | Value |
|---|---|
| **Initial Creation Date** | October 7, 2026 |
| **Last Audit & Update Date** | October 7, 2026 |
| **Active Git Commit Hash** | `d71f2fd580a5e54f56a138612201937077980b8f` (`refs/heads/main`) |
| **Unity Engine Version** | Unity 6000.5.8f1 |
| **Target Runtime Framework** | .NET Standard 2.1 (Mono), .NET 10.0 (Tests) |
| **Overall Documentation Status** | **CURRENT** |

---

## 2. Analyzed Source Areas

The following subsystems have been fully inspected, verified against active source code, and comprehensively documented:

1. **Core Orchestration (`Contrast.Core`):** `GameManager.cs`, `GameState.cs`.
2. **Color Classification (`Contrast.Color`):** `ColorClassifier.cs`, `LogicalColor.cs`.
3. **Data Models (`Contrast.Data`):** `LevelData.cs`, `TrajectoryData.cs`.
4. **Platform & Marker Entities (`Contrast.Level`):** `ColorPlatform.cs`, `LevelMarker.cs`, `LevelLoader.cs`.
5. **Kinematics Engine (`Contrast.Level`):** `Trajectory.cs`, `TrajectorySystem.cs`.
6. **Level Editor Controllers (`Contrast.Level`):** `LevelEditorController.cs`.
7. **Player & Movement Pipeline (`Contrast.Player`):** `PlayerController.cs`, `PlayerInput.cs`, `PlayerState.cs`, `MovementGeometry.cs`, `PlayerJumpMotion.cs`, `PlayerOverlapResolver.cs`, `PlayerCollisionResolver.cs`, `PlayerMovementResolver.cs`.
8. **UI Systems (`Contrast.UI`):** `MainMenuUI.cs`, `GameControlUI.cs`, `GameOverui.cs`, `LevelEditorUI.cs`, `PlatformColorSubComponent.cs`, `LevelEditorPaletteItem.cs`.
9. **Camera Control (`Contrast.Camera`):** `CameraFollow.cs`.
10. **Test Harnesses (`Assets/Editor`, `Tests/`):** `EditorLevelTests.cs`, `PlayerPlatformAuditor.cs`, `Tests/ContrastTests/Program.cs`.
11. **Scene Layouts:** `SampleScene.unity`, `_Recovery/0.unity`.
12. **Level Datasets:** `SampleLevel.json`, `ContrastLevel.json`, `map2.json`, `TrajectoryShowcaseLevel.json`, test samples.

---

## 3. Unanalyzed / Uncertain Areas

- **Audio Implementation:** The project includes an `AudioListener` on `Main Camera`, but no active sound managers, sound effect triggers, or audio clips were found in `Assets/Script/`. It is currently unknown whether audio was planned or stripped.
- **Standalone Build Configurations:** Project builds have only been confirmed for Windows Standalone x86_64; mobile/WebGL configurations have not been tested or defined.

---

## 4. Potentially Stale Areas & Drift Monitoring

- **`Assets/Scenes/SampleScene.unity` Component Binding:**
  - `GameSystems` contains a missing MonoBehaviour pointing to legacy `Contrast.UI.UIManager` (GUID `797f4f9bbeb9abe48868f6ec62002cb9`).
  - *Status:* Benign debt. Documented in [11_KNOWN_ISSUES.md](11_KNOWN_ISSUES.md).
- **`Assets/_Recovery/0.unity`:**
  - Contains outdated references to `PlayerInputReader` and `PlayerStateMachine`.
  - *Status:* Inactive recovery file. Developers should ignore this scene in favor of `SampleScene.unity`.
- **Checkpoint Schema Migration:**
  - Older levels (`SampleLevel.json`) use an object for `"checkPoint"`, while modern levels use an array.
  - *Status:* Documented in [04_DATA_MODEL.md](04_DATA_MODEL.md).

---

## 5. Verification Sign-Off

- Automated Test Run: **91 / 91 PASSED** (via `dotnet run --project Tests/ContrastTests/ContrastTests.csproj`).
- Unity Batchmode Run: **58 / 58 PASSED** (via `EditorLevelTests.RunAllEditorTests`).
- Documentation Freshness: **CURRENT**.
